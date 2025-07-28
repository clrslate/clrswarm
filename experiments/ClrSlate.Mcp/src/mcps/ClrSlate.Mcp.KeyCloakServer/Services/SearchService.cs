/*
 * Copyright 2025 ClrSlate Tech labs Private Limited
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using ClrSlate.Mcp.KeyCloakServer.Models;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using static Qdrant.Client.Grpc.Conditions;

namespace ClrSlate.Mcp.KeyCloakServer.Services;

public class SearchService
{
    private readonly QdrantClient _qdrantClient;
    private readonly VectorStorageService _vectorStorage;
    private readonly EmbeddingService _embeddingService;
    private readonly SearchConfiguration _config;
    private readonly ILogger<SearchService> _logger;

    public SearchService(
        QdrantClient qdrantClient,
        VectorStorageService vectorStorage,
        EmbeddingService embeddingService,
        IOptions<SearchConfiguration> config,
        ILogger<SearchService> logger)
    {
        _qdrantClient = qdrantClient;
        _vectorStorage = vectorStorage;
        _embeddingService = embeddingService;
        _config = config.Value;
        _logger = logger;
    }

    public async Task<object> SearchSimilarAsync(
        string query,
        int limit = 10,
        float minScore = 0.3f,
        string? entityType = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Semantic search: '{Query}' (type: {EntityType}, limit: {Limit})",
                query, entityType ?? "all", limit);

            // Generate embedding for the search query
            var queryVector = await _embeddingService.GenerateEmbeddingAsync(query, cancellationToken);

            // Build filters if specified
            Filter? filter = null;
            if (!string.IsNullOrEmpty(entityType))
            {
                filter = Match("entity_type", new List<string> { entityType });
            }

            // Perform vector search
            var searchResult = await _qdrantClient.SearchAsync(
                _config.QdrantCollection,
                queryVector,
                filter: filter,
                limit: (ulong)limit,
                scoreThreshold: minScore,
                cancellationToken: cancellationToken);

            stopwatch.Stop();

            // Convert results
            var results = searchResult.Select(result => new
            {
                id = GetPayloadValue(result.Payload, "id"),
                name = GetPayloadValue(result.Payload, "name"),
                description = GetPayloadValue(result.Payload, "description"),
                entityType = GetPayloadValue(result.Payload, "entity_type"),
                tags = GetPayloadValue(result.Payload, "tags")
                    ?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    ?.ToList() ?? new List<string>(),
                activityType = GetPayloadValue(result.Payload, "activity_type"),
                version = GetPayloadValue(result.Payload, "version"),
                similarityScore = Math.Round(result.Score, 3),
                originalIndex = searchResult.ToList().IndexOf(result)
            })
            // Deduplicate by name + description combination
            .GroupBy(r => new { r.name, r.description })
            .Select(g => g.OrderByDescending(r => r.similarityScore).First())
            .OrderByDescending(r => r.similarityScore)
            .Select((result, index) => new
            {
                id = result.id,
                name = result.name,
                description = result.description,
                entityType = result.entityType,
                tags = result.tags,
                activityType = result.activityType,
                version = result.version,
                similarityScore = result.similarityScore,
                relevanceRank = index + 1
            })
            .ToList();

            var scores = results.Select(r => r.similarityScore).ToList();

            return new
            {
                query = query,
                searchType = "semantic",
                totalResults = results.Count,
                executionTimeMs = stopwatch.ElapsedMilliseconds,
                filters = new { entityType },
                scoringInfo = new
                {
                    maxScore = scores.Any() ? scores.Max() : 0,
                    minScore = scores.Any() ? scores.Min() : 0,
                    avgScore = scores.Any() ? Math.Round(scores.Average(), 3) : 0,
                    threshold = minScore
                },
                results = results
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in semantic search for query '{Query}'", query);
            throw;
        }
    }

    public async Task<object> KeywordSearchAsync(
        string query,
        int limit = 10,
        string? entityType = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Keyword search: '{Query}' (type: {EntityType}, limit: {Limit})",
                query, entityType ?? "all", limit);

            var queryTerms = query.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // Get filtered records from storage service
            IEnumerable<EmbeddingRecord> records;
            if (!string.IsNullOrEmpty(entityType))
            {
                records = await _vectorStorage.GetRecordsByTypeAsync(entityType, cancellationToken);
            }
            else
            {
                records = await _vectorStorage.GetAllRecordsAsync(cancellationToken);
            }

            // Score and rank results
            var scoredResults = records.Select(record =>
            {
                var searchText = $"{record.Metadata.Name} {record.Metadata.Description} {record.Text}".ToLower();
                var nameText = record.Metadata.Name.ToLower();

                var score = CalculateKeywordScore(queryTerms, searchText, nameText);
                var matchingTerms = queryTerms.Where(term => searchText.Contains(term)).ToList();

                return new
                {
                    record = record,
                    score = score,
                    matchingTerms = matchingTerms
                };
            })
            .Where(r => r.score > 0)
            .GroupBy(r => new { r.record.Metadata.Name, r.record.Metadata.Description })
            .Select(g => g.OrderByDescending(r => r.score).First())
            .OrderByDescending(r => r.score)
            .Take(limit)
            .ToList();

            stopwatch.Stop();

            // Format results
            var results = scoredResults.Select((item, index) => new
            {
                id = item.record.Id,
                name = item.record.Metadata.Name,
                description = item.record.Metadata.Description,
                entityType = item.record.Metadata.EntityType,
                tags = item.record.Metadata.Tags,
                activityType = item.record.Metadata.ActivityType,
                version = item.record.Metadata.Version,
                similarityScore = Math.Round(item.score, 3),
                relevanceRank = index + 1,
                matchingTerms = item.matchingTerms
            }).ToList();

            var scores = results.Select(r => r.similarityScore).ToList();

            return new
            {
                query = query,
                searchType = "keyword",
                totalResults = results.Count,
                executionTimeMs = stopwatch.ElapsedMilliseconds,
                filters = new { entityType },
                scoringInfo = new
                {
                    maxScore = scores.Any() ? scores.Max() : 0,
                    minScore = scores.Any() ? scores.Min() : 0,
                    avgScore = scores.Any() ? Math.Round(scores.Average(), 3) : 0,
                    searchTerms = queryTerms
                },
                results = results
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in keyword search for query '{Query}'", query);
            throw;
        }
    }

    // Helper methods
    private string? GetPayloadValue(Google.Protobuf.Collections.MapField<string, Value> payload, string key)
    {
        return payload.TryGetValue(key, out var value) ? value.StringValue : null;
    }

    private float CalculateKeywordScore(string[] queryTerms, string searchText, string nameText)
    {
        var score = 0f;
        var termCount = 0;

        foreach (var term in queryTerms)
        {
            if (searchText.Contains(term))
            {
                termCount++;
                score += 1f;

                // Boost if term appears in name
                if (nameText.Contains(term))
                    score += 1f;

                // Extra boost for exact name match
                if (nameText.Equals(term, StringComparison.OrdinalIgnoreCase))
                    score += 2f;
            }
        }

        // Normalize by number of terms found vs total terms
        var coverage = queryTerms.Length > 0 ? (float)termCount / queryTerms.Length : 0f;
        return score * coverage;
    }
}