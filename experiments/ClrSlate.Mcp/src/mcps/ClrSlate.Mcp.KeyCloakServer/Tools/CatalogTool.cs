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
using ClrSlate.Mcp.KeyCloakServer.Services;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace ClrSlate.Mcp.KeyCloakServer.Tools;

[McpServerToolType]
public sealed class CatalogAPITool
{
    [McpServerTool(Name = "getAllPackages")]
    [Description("Retrieves all available packages from the MCP catalog. Useful for listing or browsing available packages.\nUsage: { \"tool\": \"getAllPackages\" }")]
    public static async Task<string> GetAllPackages(IMcpServer server)
    {
        try
        {
            var catalogAPIService = server.Services.GetService<CatalogApiService>();
            if (catalogAPIService == null)
            {
                return JsonSerializer.Serialize(new { error = "Vector storage service not available" });
            }

            var packages =await catalogAPIService.GetAllPackagesAsync();

            return JsonSerializer.Serialize(packages, new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            }); 
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                status = "error",
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }
    
    [McpServerTool(Name = "getAllActivities")]
    [Description("Retrieves all available activities from the MCP catalog. Useful for listing or browsing available activities.\nUsage: { \"tool\": \"getAllActivites\" }")]
    public static async Task<string> GetAllActivities(IMcpServer server)
    {
        try
        {
            var catalogAPIService = server.Services.GetService<CatalogApiService>();
            if (catalogAPIService == null)
            {
                return JsonSerializer.Serialize(new { error = "Vector storage service not available" });
            }

            var packages =await catalogAPIService.GetAllActivitiesAsync();

            return JsonSerializer.Serialize(packages, new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            }); 
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                status = "error",
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }

    [McpServerTool(Name = "getPackage")]
    [Description("Fetches a specific package by its unique ID. Useful for inspecting a package's metadata and structure.\nUsage: { \"tool\": \"getPackage\", \"arguments\": { \"packageId\": \"your-package-id\" } }")]

    public static async Task<string> GetPackage(string packageId, IMcpServer server)
    {
        try
        {
            var catalogAPIService = server.Services.GetService<CatalogApiService>();
            if (catalogAPIService == null)
            {
                return JsonSerializer.Serialize(new { error = "Vector storage service not available" });
            }

            var package = await catalogAPIService.GetPackageAsync(packageId);

            return JsonSerializer.Serialize(package, new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                status = "error",
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }

    [McpServerTool(Name = "getActivitiesOfPackage")]
    [Description("Retrieves all activities within a specified package using its ID. Helps in exploring the actions defined in a package.\nUsage: { \"tool\": \"getActivitiesOfPackage\", \"arguments\": { \"packageId\": \"your-package-id\" } }")]
    public static async Task<string> GetAllActivitiesOfPackage(string packageId, IMcpServer server)
    {
        try
        {
            var catalogAPIService = server.Services.GetService<CatalogApiService>();
            var activities = await catalogAPIService.GetActivitiesForPackageAsync(packageId);
            if (catalogAPIService == null)
            {
                return JsonSerializer.Serialize(new { error = "CatalogApiService not available" });
            }

            object packages =await catalogAPIService.GetActivitiesForPackageAsync(packageId);

            return JsonSerializer.Serialize(packages, new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            }); 
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                status = "error",
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }

    [McpServerTool(Name = "getActivity")]
    [Description("Retrieves details of a specific activity using its ID. Use this to understand the logic and metadata of an activity.\nUsage: { \"tool\": \"getActivity\", \"arguments\": { \"activityId\": \"your-activity-id\" } }")]
    public static async Task<string> GetActivity(string activityId, IMcpServer server)
    {
        try
        {
            var catalogAPIService = server.Services.GetService<CatalogApiService>();
            if (catalogAPIService == null)
            {
                return JsonSerializer.Serialize(new { error = "Vector storage service not available" });
            }

            var activity =await catalogAPIService.GetActivityAsync(activityId);

            return JsonSerializer.Serialize(activity, new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            }); 
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                status = "error",
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }

    
}