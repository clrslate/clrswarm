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

using ClrSlate.Mcp.KeyCloakServer.Services;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace ClrSlate.Mcp.KeyCloakServer.Tools;

[McpServerToolType]
public sealed class SemanticSearchTool
{
    [McpServerTool(Name = "semanticSearch")]
    [Description("Performs semantic search on catalog packages and activities using vector embeddings. Supports both semantic and keyword search modes.")]
    public static async Task<string> SemanticSearch(
        IMcpServer server,
        [Description("The search query text")] string query,
        [Description("Maximum number of results to return (default: 10, max: 50)")] int limit = 10,
        [Description("Search type: 'semantic' (default), 'keyword', or 'hybrid'")] string searchType = "semantic",
        [Description("Entity type filter: 'package', 'activity', or null for all")] string? entityType = null,
        [Description("Minimum similarity score for semantic search (default: 0.3)")] float minScore = 0.3f)
    {
        try
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(query))
            {
                return JsonSerializer.Serialize(new { error = "Query parameter is required" });
            }

            if (limit <= 0 || limit > 50)
            {
                return JsonSerializer.Serialize(new { error = "Limit must be between 1 and 50" });
            }

            if (minScore < 0 || minScore > 1)
            {
                return JsonSerializer.Serialize(new { error = "MinScore must be between 0 and 1" });
            }

            var validSearchTypes = new[] { "semantic", "keyword", "hybrid" };
            if (!validSearchTypes.Contains(searchType.ToLower()))
            {
                return JsonSerializer.Serialize(new { error = "SearchType must be 'semantic', 'keyword', or 'hybrid'" });
            }

            var validEntityTypes = new[] { "package", "activity" };
            if (!string.IsNullOrEmpty(entityType) && !validEntityTypes.Contains(entityType.ToLower()))
            {
                return JsonSerializer.Serialize(new { error = "EntityType must be 'package' or 'activity'" });
            }

            // Get the search service from DI
            var searchService = server.Services.GetService<SearchService>();
            if (searchService == null)
            {
                return JsonSerializer.Serialize(new { error = "Search service not available" });
            }

            // Perform search based on type
            object results = searchType.ToLower() switch
            {
                "semantic" => await searchService.SearchSimilarAsync(query, limit, minScore, entityType?.ToLower()),
                "keyword" => await searchService.KeywordSearchAsync(query, limit, entityType?.ToLower()),
                _ => await searchService.SearchSimilarAsync(query, limit, minScore, entityType?.ToLower())
            };

            return JsonSerializer.Serialize(results, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                error = $"Search failed: {ex.Message}",
                query = query,
                searchType = searchType,
                timestamp = DateTime.UtcNow
            });
        }
    }

    [McpServerTool(Name = "searchPackages")]
    [Description("Searches specifically for packages in the catalog using semantic search")]
    public static async Task<string> SearchPackages(
        IMcpServer server,
        [Description("The search query text")] string query,
        [Description("Maximum number of results to return (default: 10, max: 50)")] int limit = 10,
        [Description("Minimum similarity score (default: 0.3)")] float minScore = 0.3f)
    {
        return await SemanticSearch(server, query, limit, "semantic", "package", minScore);
    }

    [McpServerTool(Name = "searchActivities")]
    [Description("Searches specifically for activities in the catalog using semantic search")]
    public static async Task<string> SearchActivities(
        IMcpServer server,
        [Description("The search query text")] string query,
        [Description("Maximum number of results to return (default: 10, max: 50)")] int limit = 10,
        [Description("Minimum similarity score (default: 0.3)")] float minScore = 0.3f)
    {
        return await SemanticSearch(server, query, limit, "semantic", "activity", minScore);
    }

    [McpServerTool(Name = "keywordSearch")]
    [Description("Performs fast keyword-based search on catalog items without using embeddings")]
    public static async Task<string> KeywordSearch(
        IMcpServer server,
        [Description("The search query text")] string query,
        [Description("Maximum number of results to return (default: 10, max: 50)")] int limit = 10,
        [Description("Entity type filter: 'package', 'activity', or null for all")] string? entityType = null)
    {
        return await SemanticSearch(server, query, limit, "keyword", entityType, 0.0f);
    }
}