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

using System.Text.Json;
using ClrSlate.Mcp.KeyCloakServer.Models;

namespace ClrSlate.Mcp.KeyCloakServer.Services;

public class CatalogApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CatalogApiService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public CatalogApiService(HttpClient httpClient, ILogger<CatalogApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<List<PackageEntity>> GetAllPackagesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching all packages from catalog API");

            var response = await _httpClient.GetAsync("https://store.beta.clrslate.app/api/catalog-packages", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<PackageDto>>>(json, _jsonOptions);

            if (apiResponse?.Data == null)
            {
                _logger.LogWarning("No packages returned from API");
                return new List<PackageEntity>();
            }

            var packages = apiResponse.Data.Select(dto => new PackageEntity
            {
                Id = dto.Id,
                Name = dto.Name,
                Description = dto.Description,
                ImageSrc = dto.ImageSrc,
                Tags = dto.Tags ?? new List<string>(),
                Readme = dto.Readme,
                Version = dto.Version
            }).ToList();

            _logger.LogInformation("Successfully fetched {Count} packages", packages.Count);
            return packages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching packages from catalog API");
            throw;
        }
    }    
    public async Task<PackageEntity> GetPackageAsync(string packageId,CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation($"Fetching package with package Id {packageId}");

            var response = await _httpClient.GetAsync($"https://store.beta.clrslate.app/api/catalog-packages?id={packageId}", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<PackageDto>>(json, _jsonOptions);

            if (apiResponse?.Data == null)
            {
                _logger.LogWarning("No package returned from API");
                return new PackageEntity();
            }
            var package = new PackageEntity
            {
                Id = apiResponse.Data.Id,
                Name = apiResponse.Data.Name,
                Description = apiResponse.Data.Description,
                ImageSrc = apiResponse.Data.ImageSrc,
                Tags = apiResponse.Data.Tags ?? new List<string>(),
                Readme = apiResponse.Data.Readme,
                Version = apiResponse.Data.Version
            };

            _logger.LogInformation($"Successfully fetched package with id : {packageId}" );
            return package;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching packages from catalog API");
            throw;
        }
    }

    public async Task<ActivityEntity> GetActivityAsync(string activityId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching activities for package {PackageId}", activityId);

            var response = await _httpClient.GetAsync($"https://store.beta.clrslate.app/api/catalog-actions?id={activityId}", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<ActivityDto>>(json, _jsonOptions);

            if (apiResponse?.Data == null)
            {
                _logger.LogInformation("No activities found for package {PackageId}", activityId);
                return new ActivityEntity();
            }
            var activity = new ActivityEntity()
            {
                Id = apiResponse.Data.Id,
                Name = apiResponse.Data.Name,
                Description = apiResponse.Data.Description,
                ImageSrc = apiResponse.Data.ImageSrc,
                Labels = apiResponse.Data.Labels,
                Tags = apiResponse.Data.Tags ?? new List<string>(),
                ParentId = apiResponse.Data.ParentId,
                Type = apiResponse.Data.Type,
                Documentation = apiResponse.Data.Documentation,
                Parameters = apiResponse.Data.Parameters?.Select(p => new ActivityParameter
                {
                    Name = p.Name,
                    Description = p.Description,
                    Type = p.Type,
                    Required = p.Required
                }).ToList() ?? new List<ActivityParameter>()
            };
           
            _logger.LogInformation("Successfully fetched activity {activityId}", activityId);
            return activity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching activities for activity {activityId}", activityId);
            throw;
        }
    }    
    public async Task<List<ActivityEntity>> GetActivitiesForPackageAsync(string packageId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching activities for package {PackageId}", packageId);

            var response = await _httpClient.GetAsync($"https://store.beta.clrslate.app/api/catalog-actions?parentId={packageId}", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<ActivityDto>>>(json, _jsonOptions);

            if (apiResponse?.Data == null)
            {
                _logger.LogInformation("No activities found for package {PackageId}", packageId);
                return new List<ActivityEntity>();
            }

            var activities = apiResponse.Data.Select(dto => new ActivityEntity
            {
                Id = dto.Id,
                Name = dto.Name,
                Description = dto.Description,
                ImageSrc = dto.ImageSrc,
                Tags = dto.Tags ?? new List<string>(),
                ParentId = dto.ParentId,
                Type = dto.Type,
                Documentation = dto.Documentation,
                Parameters = dto.Parameters?.Select(p => new ActivityParameter
                {
                    Name = p.Name,
                    Description = p.Description,
                    Type = p.Type,
                    Required = p.Required
                }).ToList() ?? new List<ActivityParameter>()
            }).ToList();

            _logger.LogInformation("Successfully fetched {Count} activities for package {PackageId}", activities.Count, packageId);
            return activities;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching activities for package {PackageId}", packageId);
            throw;
        }
    }

    public async Task<List<ActivityEntity>> GetAllActivitiesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching all activities from catalog API");

            var response = await _httpClient.GetAsync("https://store.beta.clrslate.app/api/catalog-actions", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<ActivityDto>>>(json, _jsonOptions);

            if (apiResponse?.Data == null)
            {
                _logger.LogWarning("No activities returned from API");
                return new List<ActivityEntity>();
            }

            var activities = apiResponse.Data.Select(dto => new ActivityEntity
            {
                Id = dto.Id,
                Name = dto.Name,
                Description = dto.Description,
                ImageSrc = dto.ImageSrc,
                Labels = dto.Labels,
                Tags = dto.Tags ?? new List<string>(),
                ParentId = dto.ParentId,
                Type = dto.Type,
                Documentation = dto.Documentation,
                Parameters = dto.Parameters?.Select(p => new ActivityParameter
                {
                    Name = p.Name,
                    Description = p.Description,
                    Type = p.Type,
                    Required = p.Required
                }).ToList() ?? new List<ActivityParameter>()
            }).ToList();

            _logger.LogInformation("Successfully fetched {Count} total activities", activities.Count);
            return activities;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all activities from catalog API");
            throw;
        }
    }
}

// DTOs for API responses
public class ApiResponse<T>
{
    public T? Data { get; set; }
    public string? BaseUrl { get; set; }
}

public class PackageDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageSrc { get; set; } = string.Empty;
    public List<string>? Tags { get; set; }
    public string? Readme { get; set; }
    public string? Version { get; set; }
}

public class ActivityDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageSrc { get; set; } = string.Empty;
    public ActivityLabelsDto? Labels { get; set; }
    public List<string>? Tags { get; set; }
    public string ParentId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Documentation { get; set; }
    public List<ActivityParameterDto>? Parameters { get; set; }
}
public class ActivityLabelsDto
{
    public string? Category { get; set; }
}
public class ActivityParameterDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool Required { get; set; }
}

// Entity models
public class PackageEntity
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageSrc { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public string? Readme { get; set; }
    public string? Version { get; set; }
}

public class ActivityEntity
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageSrc { get; set; } = string.Empty;
    public ActivityLabelsDto? Labels { get; set; }
    public List<string> Tags { get; set; } = new();
    public string ParentId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Documentation { get; set; }
    public List<ActivityParameter> Parameters { get; set; } = new();
}

public class ActivityParameter
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool Required { get; set; }
}