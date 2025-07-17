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
public sealed class DataIngestionTool
{
    [McpServerTool(Name = "populateVectorDatabase")]
    [Description("Fetches catalog data from the API and populates the vector database with embeddings for semantic search")]
    public static async Task<string> PopulateVectorDatabase(
        IMcpServer server,
        [Description("Whether to recreate the collection (deletes existing data)")] bool recreateCollection = false,
        [Description("Maximum number of records to process (0 = all)")] int maxRecords = 25)
    {
        try
        {
            var catalogApiService = server.Services.GetService<CatalogApiService>();
            var embeddingService = server.Services.GetService<EmbeddingService>();
            var vectorStorageService = server.Services.GetService<VectorStorageService>();

            if (catalogApiService == null || embeddingService == null || vectorStorageService == null)
            {
                return JsonSerializer.Serialize(new { error = "Required services not available" });
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var results = new
            {
                status = "success",
                message = "Database population completed",
                timing = new { },
                statistics = new { },
                steps = new List<string>()
            };

            var steps = new List<string>();

            // Step 1: Ensure collection exists (or recreate if requested)
            steps.Add("Ensuring vector collection exists...");
            if (recreateCollection)
            {
                try
                {
                    await vectorStorageService.DeleteCollectionAsync();
                    steps.Add("Deleted existing collection");
                }
                catch (Exception ex)
                {
                    steps.Add($"Note: Could not delete collection (might not exist): {ex.Message}");
                }
            }

            await vectorStorageService.EnsureCollectionExistsAsync();
            steps.Add("Vector collection ready");

            // Step 2: Check embedding service
            steps.Add("Checking embedding service...");
            await embeddingService.EnsureModelAvailableAsync();
            steps.Add("Embedding service ready");

            // Step 3: Fetch packages
            steps.Add("Fetching packages from catalog API...");
            var packages = await catalogApiService.GetAllPackagesAsync();
            steps.Add($"Fetched {packages.Count} packages");

            // Step 4: Fetch activities
            steps.Add("Fetching activities from catalog API...");
            var activities = await catalogApiService.GetAllActivitiesAsync();
            steps.Add($"Fetched {activities.Count} activities");

            // Step 5: Process packages
            var packageRecords = new List<EmbeddingRecord>();
            steps.Add("Processing packages for embedding...");

            foreach (var package in packages)
            {
                var text = $"{package.Name} - {package.Description}";
                if (!string.IsNullOrEmpty(package.Readme))
                {
                    text += $" {package.Readme.Substring(0, Math.Min(package.Readme.Length, 500))}";
                }

                var record = new EmbeddingRecord
                {
                    Id = $"package_{package.Id}",
                    Text = text,
                    Metadata = new EmbeddingMetadata
                    {
                        EntityType = "package",
                        OriginalId = package.Id,
                        Name = package.Name,
                        Description = package.Description,
                        Tags = package.Tags,
                        Version = package.Version,
                        SourceUrl = $"https://store.beta.clrslate.app/packages/{package.Id}",
                        IndexedAt = DateTime.UtcNow
                    }
                };

                packageRecords.Add(record);
            }

            // Step 6: Process activities
            var activityRecords = new List<EmbeddingRecord>();
            steps.Add("Processing activities for embedding...");

            foreach (var activity in activities)
            {
                var text = $"{activity.Name} - {activity.Description}";
                if (!string.IsNullOrEmpty(activity.Documentation))
                {
                    text += $" {activity.Documentation.Substring(0, Math.Min(activity.Documentation.Length, 500))}";
                }

                var record = new EmbeddingRecord
                {
                    Id = $"activity_{activity.Id}",
                    Text = text,
                    Metadata = new EmbeddingMetadata
                    {
                        EntityType = "activity",
                        OriginalId = activity.Id,
                        Name = activity.Name,
                        Description = activity.Description,
                        ParentId = activity.ParentId,
                        Tags = activity.Tags,
                        ActivityType = activity.Type,
                        SourceUrl = $"https://store.beta.clrslate.app/activities/{activity.Id}",
                        IndexedAt = DateTime.UtcNow
                    }
                };

                activityRecords.Add(record);
            }

            // Step 7: Limit records if specified
            var allRecords = packageRecords.Concat(activityRecords).ToList();
            if (maxRecords > 0 && allRecords.Count > maxRecords)
            {
                allRecords = allRecords.Take(maxRecords).ToList();
                steps.Add($"Limited to {maxRecords} records for faster processing");
            }

            steps.Add($"Processing {allRecords.Count} records...");

            // Step 8: Check for existing records to avoid duplicates
            var newRecords = new List<EmbeddingRecord>();
            if (!recreateCollection)
            {
                steps.Add("Checking for existing records...");
                foreach (var record in allRecords)
                {
                    var exists = await vectorStorageService.RecordExistsAsync(record.Id);
                    if (!exists)
                    {
                        newRecords.Add(record);
                    }
                }
                steps.Add($"Found {newRecords.Count} new records to process");
            }
            else
            {
                newRecords = allRecords;
            }

            if (newRecords.Count == 0)
            {
                steps.Add("No new records to process");
                stopwatch.Stop();

                var finalStats = await vectorStorageService.GetStatisticsAsync();
                return JsonSerializer.Serialize(new
                {
                    status = "success",
                    message = "No new records to process",
                    timing = new
                    {
                        totalTimeMs = stopwatch.ElapsedMilliseconds,
                        totalTimeSeconds = stopwatch.Elapsed.TotalSeconds
                    },
                    statistics = new
                    {
                        totalRecords = 0,
                        packageCount = packageRecords.Count,
                        activityCount = activityRecords.Count,
                        databaseStats = finalStats
                    },
                    steps = steps
                }, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }

            // Step 9: Generate embeddings in smaller batches
            steps.Add($"Generating embeddings for {newRecords.Count} records...");
            var batchSize = 5; // Very small batches to avoid timeout
            var processedCount = 0;

            // Use timeout-aware processing
            var timeoutAfter = TimeSpan.FromMinutes(2); // 2 minute timeout per operation
            var startTime = DateTime.UtcNow;

            for (int i = 0; i < newRecords.Count; i += batchSize)
            {
                // Check if we're approaching timeout
                if (DateTime.UtcNow - startTime > timeoutAfter)
                {
                    steps.Add($"Stopping early to avoid timeout. Processed {processedCount}/{newRecords.Count} records");
                    break;
                }

                var batch = newRecords.Skip(i).Take(batchSize).ToList();

                try
                {
                    // Use CancellationToken with timeout
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

                    var texts = batch.Select(r => r.Text).ToList();
                    var embeddings = await embeddingService.GenerateBatchEmbeddingsAsync(texts);

                    for (int j = 0; j < batch.Count; j++)
                    {
                        batch[j].Vector = embeddings[j];
                    }

                    await vectorStorageService.StoreBatchEmbeddingsAsync(batch, cts.Token);
                    processedCount += batch.Count;

                    if (processedCount % 10 == 0 || processedCount == newRecords.Count)
                    {
                        steps.Add($"Processed {processedCount}/{newRecords.Count} records");
                    }
                }
                catch (OperationCanceledException)
                {
                    steps.Add($"Batch timeout at {processedCount}/{newRecords.Count} records");
                    break;
                }
                catch (Exception ex)
                {
                    steps.Add($"Error processing batch at {processedCount}: {ex.Message}");
                    // Continue with next batch
                }
            }

            stopwatch.Stop();

            // Step 10: Get final statistics
            var stats = await vectorStorageService.GetStatisticsAsync();
            steps.Add("Database population completed successfully!");

            return JsonSerializer.Serialize(new
            {
                status = "success",
                message = "Database population completed",
                timing = new
                {
                    totalTimeMs = stopwatch.ElapsedMilliseconds,
                    totalTimeSeconds = stopwatch.Elapsed.TotalSeconds
                },
                statistics = new
                {
                    processedRecords = newRecords.Count,
                    totalPackagesAvailable = packageRecords.Count,
                    totalActivitiesAvailable = activityRecords.Count,
                    databaseStats = stats
                },
                steps = steps
            }, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                status = "error",
                error = ex.Message,
                stackTrace = ex.StackTrace,
                timestamp = DateTime.UtcNow
            });
        }
    }

    [McpServerTool(Name = "getIngestionProgress")]
    [Description("Shows how many records are available vs how many are already indexed")]
    public static async Task<string> GetIngestionProgress(IMcpServer server)
    {
        try
        {
            var catalogApiService = server.Services.GetService<CatalogApiService>();
            var vectorStorageService = server.Services.GetService<VectorStorageService>();

            if (catalogApiService == null || vectorStorageService == null)
            {
                return JsonSerializer.Serialize(new { error = "Required services not available" });
            }

            // Get available data
            var packages = await catalogApiService.GetAllPackagesAsync();
            var activities = await catalogApiService.GetAllActivitiesAsync();
            var totalAvailable = packages.Count + activities.Count;

            // Get indexed data
            var stats = await vectorStorageService.GetStatisticsAsync();
            var totalIndexed = stats.ContainsKey("Total") ? stats["Total"] : 0;

            var remaining = totalAvailable - totalIndexed;
            var progressPercent = totalAvailable > 0 ? (double)totalIndexed / totalAvailable * 100 : 0;

            return JsonSerializer.Serialize(new
            {
                status = "success",
                progress = new
                {
                    totalAvailable = totalAvailable,
                    totalIndexed = totalIndexed,
                    remaining = remaining,
                    progressPercent = Math.Round(progressPercent, 1)
                },
                breakdown = new
                {
                    packagesAvailable = packages.Count,
                    activitiesAvailable = activities.Count,
                    packagesIndexed = stats.ContainsKey("package") ? stats["package"] : 0,
                    activitiesIndexed = stats.ContainsKey("activity") ? stats["activity"] : 0
                },
                recommendation = remaining > 50 ?
                    "Use 'populateIncrementally' for safe processing" :
                    remaining > 20 ?
                    "Use 'populateMediumBatch' for medium batch" :
                    remaining > 0 ?
                    "Use 'quickPopulateDatabase' to finish" :
                    "Database is fully populated!"
            }, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
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

    [McpServerTool(Name = "getDatabaseStatus")]
    [Description("Gets the current status and statistics of the vector database")]
    public static async Task<string> GetDatabaseStatus(IMcpServer server)
    {
        try
        {
            var vectorStorageService = server.Services.GetService<VectorStorageService>();
            if (vectorStorageService == null)
            {
                return JsonSerializer.Serialize(new { error = "Vector storage service not available" });
            }

            var isHealthy = await vectorStorageService.IsHealthyAsync();
            var stats = await vectorStorageService.GetStatisticsAsync();
            var recordCount = await vectorStorageService.GetCollectionCountAsync();

            return JsonSerializer.Serialize(new
            {
                status = "success",
                healthy = isHealthy,
                recordCount = recordCount,
                statistics = stats,
                timestamp = DateTime.UtcNow
            }, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
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

    [McpServerTool(Name = "quickPopulateDatabase")]
    [Description("Quickly populates the vector database with a small subset of data for testing")]
    public static async Task<string> QuickPopulateDatabase(IMcpServer server)
    {
        return await PopulateVectorDatabase(server, recreateCollection: false, maxRecords: 10);
    }

    [McpServerTool(Name = "populateIncrementally")]
    [Description("Populates the database in very small increments to avoid timeouts (5 records at a time)")]
    public static async Task<string> PopulateIncrementally(IMcpServer server)
    {
        return await PopulateVectorDatabase(server, recreateCollection: false, maxRecords: 5);
    }

    [McpServerTool(Name = "populateMediumBatch")]
    [Description("Populates the database with a medium batch size (15 records)")]
    public static async Task<string> PopulateMediumBatch(IMcpServer server)
    {
        return await PopulateVectorDatabase(server, recreateCollection: false, maxRecords: 15);
    }

    [McpServerTool(Name = "clearVectorDatabase")]
    [Description("Clears all data from the vector database (use with caution)")]
    public static async Task<string> ClearVectorDatabase(IMcpServer server)
    {
        try
        {
            var vectorStorageService = server.Services.GetService<VectorStorageService>();
            if (vectorStorageService == null)
            {
                return JsonSerializer.Serialize(new { error = "Vector storage service not available" });
            }

            await vectorStorageService.DeleteCollectionAsync();

            return JsonSerializer.Serialize(new
            {
                status = "success",
                message = "Vector database cleared successfully",
                timestamp = DateTime.UtcNow
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