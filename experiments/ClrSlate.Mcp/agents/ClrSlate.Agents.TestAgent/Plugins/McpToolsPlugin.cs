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

using System.ComponentModel;
using System.Text.Json;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Server;

namespace ClrSlate.Agents.TestAgent.Plugins;

/// <summary>
/// Plugin that provides high-level access to MCP server tools for search, data management, and utility functions.
/// This plugin orchestrates calls to the underlying MCP tools (semanticSearch, searchPackages, etc.)
/// </summary>
public class McpToolsPlugin
{
    private readonly Kernel _kernel;

    public McpToolsPlugin(Kernel kernel)
    {
        _kernel = kernel;
    }

    [KernelFunction]
    [Description("Searches for packages in the catalog using semantic search. Great for finding packages by name, description, or functionality.")]
    public async Task<string> FindPackagesAsync(
        [Description("What you're looking for (e.g., 'azure storage', 'web development', 'authentication')")] string searchQuery,
        [Description("Maximum number of results to return (default: 10, max: 50)")] int maxResults = 10,
        [Description("Minimum similarity score for results (default: 0.3, range: 0.0-1.0)")] float minScore = 0.3f)
    {
        try
        {
            var function = _kernel.Plugins["Tools"]["searchPackages"];
            var result = await function.InvokeAsync(_kernel, new KernelArguments
            {
                ["query"] = searchQuery,
                ["limit"] = Math.Min(maxResults, 50),
                ["minScore"] = Math.Max(0.0f, Math.Min(1.0f, minScore))
            });

            return FormatSearchResults(result.ToString(), "packages", searchQuery);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                error = $"Package search failed: {ex.Message}",
                query = searchQuery,
                suggestions = "Try using different keywords or check if the MCP server is running"
            });
        }
    }

    [KernelFunction]
    [Description("Searches for activities in the catalog using semantic search. Use this to find specific activities or workflows.")]
    public async Task<string> FindActivitiesAsync(
        [Description("What you're looking for (e.g., 'deployment', 'testing', 'monitoring')")] string searchQuery,
        [Description("Maximum number of results to return (default: 10, max: 50)")] int maxResults = 10,
        [Description("Minimum similarity score for results (default: 0.3, range: 0.0-1.0)")] float minScore = 0.3f)
    {
        try
        {
            var function = _kernel.Plugins["Tools"]["searchActivities"];
            var result = await function.InvokeAsync(_kernel, new KernelArguments
            {
                ["query"] = searchQuery,
                ["limit"] = Math.Min(maxResults, 50),
                ["minScore"] = Math.Max(0.0f, Math.Min(1.0f, minScore))
            });

            return FormatSearchResults(result.ToString(), "activities", searchQuery);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                error = $"Activity search failed: {ex.Message}",
                query = searchQuery,
                suggestions = "Try using different keywords or check if the MCP server is running"
            });
        }
    }

    [KernelFunction]
    [Description("Performs a comprehensive search across both packages and activities using semantic search. Best for general searches when you're not sure what type of content you're looking for.")]
    public async Task<string> SearchCatalogAsync(
        [Description("What you're looking for across the entire catalog")] string searchQuery,
        [Description("Search type: 'semantic' (default), 'keyword', or 'hybrid'")] string searchType = "semantic",
        [Description("Maximum number of results to return (default: 15, max: 50)")] int maxResults = 15,
        [Description("Minimum similarity score for semantic search (default: 0.3)")] float minScore = 0.1f)
    {
        try
        {
            var function = _kernel.Plugins["Tools"]["semanticSearch"];
            var result = await function.InvokeAsync(_kernel, new KernelArguments
            {
                ["query"] = searchQuery,
                ["limit"] = Math.Min(maxResults, 50),
                ["searchType"] = searchType,
                ["entityType"] = (string?)null, // Search all types
                ["minScore"] = Math.Max(0.0f, Math.Min(1.0f, minScore))
            });

            return FormatSearchResults(result.ToString(), "catalog items", searchQuery, searchType);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                error = $"Catalog search failed: {ex.Message}",
                query = searchQuery,
                searchType = searchType,
                suggestions = "Try using different keywords, search type, or check if the MCP server is running"
            });
        }
    }


    [KernelFunction]
    [Description("Populates the vector database with catalog data to enable semantic search. Use this to refresh or initialize the search database.")]
    public async Task<string> InitializeDatabaseAsync(
        [Description("Whether to recreate the collection (deletes existing data)")] bool recreateCollection = false,
        [Description("Maximum number of records to process (0 = all, default: 25 for testing)")] int maxRecords = 25)
    {
        try
        {
            var function = _kernel.Plugins["Tools"]["populateVectorDatabase"];
            var result = await function.InvokeAsync(_kernel, new KernelArguments
            {
                ["recreateCollection"] = recreateCollection,
                ["maxRecords"] = maxRecords
            });

            var resultString = result.ToString();

            // Parse and format the result for better readability
            try
            {
                var jsonResult = JsonSerializer.Deserialize<JsonElement>(resultString);
                return JsonSerializer.Serialize(new
                {
                    status = "Database initialization completed",
                    details = jsonResult,
                    message = recreateCollection ?
                        "Database was recreated and populated with fresh data" :
                        "Database was populated with additional data",
                    recordsProcessed = maxRecords == 0 ? "all available" : maxRecords.ToString()
                }, new JsonSerializerOptions { WriteIndented = true });
            }
            catch
            {
                return resultString; // Return as-is if parsing fails
            }
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                error = $"Database initialization failed: {ex.Message}",
                recreateCollection = recreateCollection,
                maxRecords = maxRecords,
                suggestions = "Check if the MCP server is running and the catalog API is accessible"
            });
        }
    }

    [KernelFunction]
    [Description("Tests the connection to the MCP server by sending an echo message")]
    public async Task<string> TestConnectionAsync(
        [Description("Message to echo back (default: 'connection test')")] string message = "connection test")
    {
        try
        {
            var function = _kernel.Plugins["Tools"]["Echo"];
            var result = await function.InvokeAsync(_kernel, new KernelArguments
            {
                ["message"] = message
            });

            return JsonSerializer.Serialize(new
            {
                status = "success",
                message = "MCP server connection is working",
                echo = result.ToString(),
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                status = "error",
                message = "MCP server connection failed",
                error = ex.Message,
                suggestions = "Check if the MCP server is running on the expected port"
            });
        }
    }


    [KernelFunction]
    [Description("Gets all packages from the catalog. Call this method to get all packages available.")]
    public async Task<string> GetAllPackagesAsync()
    {
        try
        {
            var function = _kernel.Plugins["Tools"]["getAllPackages"];
            var result = await function.InvokeAsync(_kernel);
            return result.ToString();
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = $"Failed to get all packages: {ex.Message}" });
        }
    }

    [KernelFunction]
    [Description("Gets a package by its ID")]
    public async Task<string> GetPackageByIdAsync(
        [Description("The ID of the package to retrieve, for example azure, istio, helm")] string packageId)
    {
        try
        {
            var function = _kernel.Plugins["Tools"]["getPackage"];
            var result = await function.InvokeAsync(_kernel, new KernelArguments
            {
                ["packageId"] = packageId
            });
            return result.ToString();
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = $"Failed to get package: {ex.Message}", packageId });
        }
    }

    [McpServerTool(Name = "getActivity")]
    [Description("Retrieves details of a specific activity using its ID. Use this to understand the logic and metadata of an activity.\nUsage: { \"tool\": \"getActivity\", \"arguments\": { \"activityId\": \"your-activity-id\" } }")]
    public async Task<string> GetActivitiesOfPackageAsync(
        [Description("ID of the  package to retrieve activities for, e.g., 'mixed'")] string packageId)
    {
        try
        {
            var function = _kernel.Plugins["Tools"]["getActivitiesOfPackage"];
            var result = await function.InvokeAsync(_kernel, new KernelArguments
            {
                ["packageId"] = packageId
            });
            return result.ToString();
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = $"Failed to get activities: {ex.Message}", packageId });
        }
    }

    [KernelFunction]
    [Description("Gets an activity and its details by its ID, can alternatively be used to check if an activity exsists in our catalog store.")]
    public async Task<string> GetActivityByIdAsync(
        [Description("The ID of the activity to retrieve.")] string activityId)
    {
        try
        {
            var function = _kernel.Plugins["Tools"]["getActivity"];
            var result = await function.InvokeAsync(_kernel, new KernelArguments
            {
                ["activityId"] = activityId
            });
            return result.ToString();
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = $"Failed to get activity: {ex.Message}", activityId });
        }
    }
    [KernelFunction]
    [Description("Gets all activities from the catalog. Call this method to get all activities available.")]
    public async Task<string> GetAllActivitiesAsync()
    {
        try
        {
            var function = _kernel.Plugins["Tools"]["getAllActivities"];
            var result = await function.InvokeAsync(_kernel);
            return result.ToString();
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = $"Failed to get all packages: {ex.Message}" });
        }
    }
    private static string FormatSearchResults(string rawResult, string searchType, string query, string? searchMethod = null)
    {
        try
        {
            var jsonResult = JsonSerializer.Deserialize<JsonElement>(rawResult);

            var formattedResult = new
            {
                searchSummary = new
                {
                    query = query,
                    type = searchType,
                    method = searchMethod ?? "semantic",
                    timestamp = DateTime.UtcNow
                },
                results = jsonResult
            };

            return JsonSerializer.Serialize(formattedResult, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch
        {
            // If parsing fails, return the raw result with minimal formatting
            return JsonSerializer.Serialize(new
            {
                query = query,
                searchType = searchType,
                rawResults = rawResult
            });
        }
    }
}