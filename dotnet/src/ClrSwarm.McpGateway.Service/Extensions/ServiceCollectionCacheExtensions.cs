using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace ClrSwarm.McpGateway.Service.Extensions;

public static class ServiceCollectionCacheExtensions
{
    /// <summary>
    /// Registers a distributed cache based on the "cache" configuration section.
    /// Providers: inMemory (default), redis, cosmosDb.
    /// </summary>
    public static IServiceCollection AddDistributedCache(this IServiceCollection services, IConfiguration configuration, TokenCredential? credential = null)
    {
        var cacheSection = configuration.GetSection("cache");
        var provider = (cacheSection["provider"] ?? "inMemory").Trim().ToLowerInvariant();

        if (provider == "cosmosdb")
        {
            var settings = cacheSection.GetSection("settings");
            var connectionString = settings["ConnectionString"]; // optional when using AAD
            var endpoint = settings["AccountEndpoint"]; // required if using AAD
            var databaseName = settings["DatabaseName"] ?? throw new InvalidOperationException("Cache cosmos settings: DatabaseName missing.");
            var containerName = settings["ContainerName"] ?? "CacheContainer";

            if (string.IsNullOrEmpty(connectionString) && credential == null)
            {
                throw new InvalidOperationException("CosmosDb cache selected without ConnectionString; a TokenCredential must be supplied.");
            }

            services.AddCosmosCache(options =>
            {
                options.ContainerName = containerName;
                options.DatabaseName = databaseName;
                options.CreateIfNotExists = true;
                options.ClientBuilder = string.IsNullOrEmpty(connectionString)
                    ? new CosmosClientBuilder(endpoint, credential!)
                    : new CosmosClientBuilder(connectionString);
            });
        }
        else if (provider == "redis")
        {
            var settings = cacheSection.GetSection("settings");
            var redisConnection = settings["ConnectionString"] ?? settings["redisConnection"]; // allow either key
            if (!string.IsNullOrWhiteSpace(redisConnection))
            {
                services.AddStackExchangeRedisCache(opt =>
                {
                    opt.Configuration = redisConnection;
                    opt.InstanceName = settings["InstanceName"] ?? "mcp-gateway:";
                });
            }
            else
            {
                services.AddDistributedMemoryCache();
            }
        }
        else // inMemory default
        {
            services.AddDistributedMemoryCache();
        }
        return services;
    }
}
