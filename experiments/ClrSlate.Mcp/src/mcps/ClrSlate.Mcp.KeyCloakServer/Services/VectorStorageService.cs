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

using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using ClrSlate.Mcp.KeyCloakServer.Models;
using static Qdrant.Client.Grpc.Conditions;

namespace ClrSlate.Mcp.KeyCloakServer.Services;

public class VectorStorageService
{
    private readonly QdrantClient _qdrantClient;
    private readonly SearchConfiguration _config;
    private readonly ILogger<VectorStorageService> _logger;

    public VectorStorageService(
        QdrantClient qdrantClient,
        IOptions<SearchConfiguration> config,
        ILogger<VectorStorageService> logger)
    {
        _qdrantClient = qdrantClient;
        _config = config.Value;
        _logger = logger;
    }

    public async Task EnsureCollectionExistsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var collections = await _qdrantClient.ListCollectionsAsync(cancellationToken);
            var collectionExists = collections.Any(c => c == _config.QdrantCollection);

            if (!collectionExists)
            {
                _logger.LogInformation("Creating collection '{Collection}' with {Dimensions} dimensions",
                    _config.QdrantCollection, _config.VectorDimensions);

                await _qdrantClient.CreateCollectionAsync(
                    _config.QdrantCollection,
                    new VectorParams
                    {
                        Size = (ulong)_config.VectorDimensions,
                        Distance = Distance.Cosine
                    },
                    cancellationToken: cancellationToken);

                _logger.LogInformation("Successfully created collection '{Collection}'", _config.QdrantCollection);
            }
            else
            {
                _logger.LogInformation("Collection '{Collection}' already exists", _config.QdrantCollection);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring collection '{Collection}' exists", _config.QdrantCollection);
            throw;
        }
    }

    public async Task<bool> RecordExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            // Use same hash code approach as storage methods
            var pointId = (ulong)Math.Abs(id.GetHashCode());
            var points = await _qdrantClient.RetrieveAsync(
                _config.QdrantCollection,
                pointId,
                cancellationToken: cancellationToken);

            return points.Any();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error checking if record {Id} exists", id);
            return false;
        }
    }

    public async Task StoreEmbeddingAsync(EmbeddingRecord record, CancellationToken cancellationToken = default)
    {
        try
        {
            var pointId = (ulong)Math.Abs(record.Id.GetHashCode());
            var vector = record.Vector; // Keep as float[] - no conversion needed

            var payload = new Dictionary<string, Value>
            {
                ["id"] = record.Id,
                ["text"] = record.Text,
                ["entity_type"] = record.Metadata.EntityType,
                ["original_id"] = record.Metadata.OriginalId,
                ["name"] = record.Metadata.Name,
                ["description"] = record.Metadata.Description,
                ["tags"] = string.Join(",", record.Metadata.Tags)
            };

            if (!string.IsNullOrEmpty(record.Metadata.ParentId))
                payload["parent_id"] = record.Metadata.ParentId;

            if (!string.IsNullOrEmpty(record.Metadata.ActivityType))
                payload["activity_type"] = record.Metadata.ActivityType;

            if (!string.IsNullOrEmpty(record.Metadata.Version))
                payload["version"] = record.Metadata.Version;

            var point = new PointStruct
            {
                Id = pointId,
                Vectors = vector,
                Payload = { payload }
            };

            await _qdrantClient.UpsertAsync(
                _config.QdrantCollection,
                new[] { point },
                cancellationToken: cancellationToken);

            _logger.LogDebug("Successfully stored embedding for {EntityType} '{Name}' with ID {Id}",
                record.Metadata.EntityType, record.Metadata.Name, record.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing embedding record {Id}", record.Id);
            throw;
        }
    }

    public async Task StoreBatchEmbeddingsAsync(
        IEnumerable<EmbeddingRecord> records,
        CancellationToken cancellationToken = default)
    {
        var recordList = records.ToList();
        _logger.LogInformation("Storing {Count} embedding records in Qdrant", recordList.Count);

        var batches = recordList.Chunk(_config.BatchSize);

        foreach (var batch in batches)
        {
            var points = batch.Select(record =>
            {
                var pointId = (ulong)Math.Abs(record.Id.GetHashCode());
                var vector = record.Vector; // Keep as float[] - no conversion needed

                var payload = new Dictionary<string, Value>
                {
                    ["id"] = record.Id,
                    ["text"] = record.Text,
                    ["entity_type"] = record.Metadata.EntityType,
                    ["original_id"] = record.Metadata.OriginalId,
                    ["name"] = record.Metadata.Name,
                    ["description"] = record.Metadata.Description,
                    ["tags"] = string.Join(",", record.Metadata.Tags)
                };

                if (!string.IsNullOrEmpty(record.Metadata.ParentId))
                    payload["parent_id"] = record.Metadata.ParentId;

                if (!string.IsNullOrEmpty(record.Metadata.ActivityType))
                    payload["activity_type"] = record.Metadata.ActivityType;

                if (!string.IsNullOrEmpty(record.Metadata.Version))
                    payload["version"] = record.Metadata.Version;

                return new PointStruct
                {
                    Id = pointId,
                    Vectors = vector,
                    Payload = { payload }
                };
            }).ToArray();

            await _qdrantClient.UpsertAsync(
                _config.QdrantCollection,
                points,
                cancellationToken: cancellationToken);

            _logger.LogDebug("Stored batch of {BatchSize} embeddings in Qdrant with point IDs: {PointIds}",
                batch.Length, string.Join(", ", points.Select(p => p.Id)));

            // Small delay between batches
            await Task.Delay(100, cancellationToken);
        }

        _logger.LogInformation("Successfully stored all {Count} embedding records in Qdrant", recordList.Count);
    }

    public async Task<long> GetCollectionCountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Use GetAllRecordsAsync to get the actual count since GetCollectionInfoAsync seems unreliable
            var allRecords = await GetAllRecordsAsync(cancellationToken);
            var actualCount = allRecords.Count();

            _logger.LogDebug("Collection '{Collection}' contains {Count} vectors (via scroll API)", _config.QdrantCollection, actualCount);

            // Also try the collection info API for comparison
            try
            {
                var info = await _qdrantClient.GetCollectionInfoAsync(_config.QdrantCollection, cancellationToken);
                var infoCount = (long)info.VectorsCount;
                _logger.LogDebug("Collection info API reports {InfoCount} vectors vs scroll API {ActualCount}", infoCount, actualCount);
            }
            catch (Exception infoEx)
            {
                _logger.LogWarning(infoEx, "Collection info API failed, using scroll count");
            }

            return actualCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting collection count for '{Collection}'", _config.QdrantCollection);
            return 0;
        }
    }

    public async Task DeleteCollectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting collection '{Collection}'", _config.QdrantCollection);

            await _qdrantClient.DeleteCollectionAsync(_config.QdrantCollection);

            _logger.LogInformation("Successfully deleted collection '{Collection}'", _config.QdrantCollection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting collection '{Collection}'", _config.QdrantCollection);
            throw;
        }
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Use a simple collection list operation as health check since HealthCheckAsync may not exist
            await _qdrantClient.ListCollectionsAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Qdrant health check failed");
            return false;
        }
    }

    // Query methods for viewing stored data
    public async Task<IEnumerable<EmbeddingRecord>> GetAllRecordsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var scrollResult = await _qdrantClient.ScrollAsync(
                _config.QdrantCollection,
                limit: 1000,
                cancellationToken: cancellationToken);

            return scrollResult.Result.Select(ConvertPointToEmbeddingRecord);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all records");
            return Enumerable.Empty<EmbeddingRecord>();
        }
    }

    public async Task<EmbeddingRecord?> GetRecordByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            // Use same hash code approach as storage methods
            var pointId = (ulong)Math.Abs(id.GetHashCode());
            var points = await _qdrantClient.RetrieveAsync(
                _config.QdrantCollection,
                pointId,
                withPayload: true,
                withVectors: true,
                cancellationToken: cancellationToken);

            return points.FirstOrDefault() != null ? ConvertPointToEmbeddingRecord(points.First()) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving record {Id}", id);
            return null;
        }
    }

    public async Task<IEnumerable<EmbeddingRecord>> GetRecordsByTypeAsync(string entityType, CancellationToken cancellationToken = default)
    {
        try
        {
            // Use the correct filter syntax from the documentation
            var filter = Match("entity_type", new List<string>() { entityType });

            var scrollResult = await _qdrantClient.ScrollAsync(
                _config.QdrantCollection,
                filter: filter,
                limit: 1000,
                cancellationToken: cancellationToken);

            return scrollResult.Result.Select(ConvertPointToEmbeddingRecord);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving records by type {EntityType}", entityType);
            return Enumerable.Empty<EmbeddingRecord>();
        }
    }

    public async Task<Dictionary<string, int>> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var allRecords = await GetAllRecordsAsync(cancellationToken);
            var stats = allRecords
                .GroupBy(r => r.Metadata.EntityType)
                .ToDictionary(g => g.Key, g => g.Count());

            stats["Total"] = allRecords.Count();
            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving statistics");
            return new Dictionary<string, int> { ["Total"] = 0 };
        }
    }

    private EmbeddingRecord ConvertPointToEmbeddingRecord(RetrievedPoint point)
    {
        var payload = point.Payload;

        return new EmbeddingRecord
        {
            Id = payload.TryGetValue("id", out var idValue) ? idValue.StringValue : "",
            Text = payload.TryGetValue("text", out var textValue) ? textValue.StringValue : "",
            Vector = point.Vectors?.Vector?.Data?.Select(d => (float)d).ToArray() ?? Array.Empty<float>(),
            Metadata = new EmbeddingMetadata
            {
                EntityType = payload.TryGetValue("entity_type", out var typeValue) ? typeValue.StringValue : "",
                OriginalId = payload.TryGetValue("original_id", out var origIdValue) ? origIdValue.StringValue : "",
                Name = payload.TryGetValue("name", out var nameValue) ? nameValue.StringValue : "",
                Description = payload.TryGetValue("description", out var descValue) ? descValue.StringValue : "",
                ParentId = payload.TryGetValue("parent_id", out var parentValue) ? parentValue.StringValue : null,
                ActivityType = payload.TryGetValue("activity_type", out var actTypeValue) ? actTypeValue.StringValue : null,
                Version = payload.TryGetValue("version", out var versionValue) ? versionValue.StringValue : null,
                Tags = payload.TryGetValue("tags", out var tagsValue) ?
                    tagsValue.StringValue.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() :
                    new List<string>()
            }
        };
    }
}
