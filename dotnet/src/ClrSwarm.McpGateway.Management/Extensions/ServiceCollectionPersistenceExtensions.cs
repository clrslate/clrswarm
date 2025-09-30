using System;
using Azure.Core; // for TokenCredential
using ClrSwarm.McpGateway.Management.Store;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Driver;
using Microsoft.Extensions.Logging;

namespace ClrSwarm.McpGateway.Management.Extensions;

public static class ServiceCollectionPersistenceExtensions
{
    /// <summary>
    /// Registers <see cref="IAdapterResourceStore"/> implementation based on configuration section "persistence".
    /// Supported providers: inMemory (default), cosmosDb, mongoDb.
    /// If using cosmosDb without a connection string, a <see cref="TokenCredential"/> must be provided.
    /// </summary>
    public static IServiceCollection AddAdapterResourcePersistence(this IServiceCollection services, IConfiguration configuration, TokenCredential? credential = null)
    {
        var persistenceSection = configuration.GetSection("persistence");
        var providerValue = persistenceSection["provider"] ?? "inMemory";
        var provider = providerValue.Trim().ToLowerInvariant();

        if (provider == "cosmosdb")
        {
            services.AddSingleton<IAdapterResourceStore>(sp =>
            {
                var settings = persistenceSection.GetSection("settings");
                var connectionString = settings["ConnectionString"]; // optional when using AAD
                var accountEndpoint = settings["AccountEndpoint"]; // needed when using credential
                var databaseName = settings["DatabaseName"] ?? throw new InvalidOperationException("Cosmos settings: DatabaseName missing.");
                CosmosClient cosmosClient;
                if (!string.IsNullOrEmpty(connectionString))
                {
                    cosmosClient = new CosmosClient(connectionString);
                }
                else
                {
                    if (credential is null)
                        throw new InvalidOperationException("CosmosDb selected without ConnectionString; a TokenCredential must be supplied.");
                    cosmosClient = new CosmosClient(accountEndpoint, credential);
                }
                return new CosmosAdapterResourceStore(cosmosClient, databaseName, "AdapterContainer", sp.GetRequiredService<ILogger<CosmosAdapterResourceStore>>());
            });
        }
        else if (provider == "mongodb")
        {
            services.AddSingleton<IAdapterResourceStore>(sp =>
            {
                var settings = persistenceSection.GetSection("settings");
                var connectionString = settings["ConnectionString"] ?? throw new InvalidOperationException("Mongo settings: ConnectionString missing.");
                var databaseName = settings["DatabaseName"] ?? throw new InvalidOperationException("Mongo settings: DatabaseName missing.");
                var collectionName = settings["CollectionName"] ?? "AdapterResources";
                var mongoClient = new MongoClient(connectionString);
                return new MongoAdapterResourceStore(mongoClient, databaseName, collectionName, sp.GetRequiredService<ILogger<MongoAdapterResourceStore>>());
            });
            services.AddDistributedMemoryCache();
        }
        else // inMemory default
        {
            services.AddSingleton<IAdapterResourceStore, InMemoryAdapterResourceStore>();
            services.AddDistributedMemoryCache();
        }

        return services;
    }
}
