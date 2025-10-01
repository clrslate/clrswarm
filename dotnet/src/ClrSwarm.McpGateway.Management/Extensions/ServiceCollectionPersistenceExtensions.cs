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
    /// MongoDb supports specifying either `ConnectionString` inline or `ConnectionStringName` to resolve from configuration connection strings.
    /// When using a connection string, the database name may be embedded; if not, settings:DatabaseName is required.
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

                // Resolve connection string either directly or by name
                var connectionStringName = settings["ConnectionStringName"];
                string? resolvedConnectionString = null;
                if (!string.IsNullOrWhiteSpace(connectionStringName))
                {
                    resolvedConnectionString = configuration.GetConnectionString(connectionStringName);
                    if (string.IsNullOrWhiteSpace(resolvedConnectionString))
                        throw new InvalidOperationException($"Mongo settings: Connection string named '{connectionStringName}' not found in configuration.");
                }
                else
                {
                    resolvedConnectionString = settings["ConnectionString"]; // inline legacy/direct value
                }

                if (string.IsNullOrWhiteSpace(resolvedConnectionString))
                    throw new InvalidOperationException("Mongo settings: ConnectionString or ConnectionStringName must be provided.");

                // Extract database name from connection string if present, else fallback to settings value.
                string? dbNameFromConn = null;
                try
                {
                    var mongoUrl = new MongoUrl(resolvedConnectionString);
                    dbNameFromConn = mongoUrl.DatabaseName; // may be null if not provided
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("Mongo settings: Invalid connection string format.", ex);
                }

                var databaseNameSetting = settings["DatabaseName"];
                var databaseName = !string.IsNullOrWhiteSpace(dbNameFromConn)
                    ? dbNameFromConn
                    : !string.IsNullOrWhiteSpace(databaseNameSetting)
                        ? databaseNameSetting
                        : throw new InvalidOperationException("Mongo settings: DatabaseName missing (not embedded in connection string and not provided in settings).");

                var collectionName = settings["CollectionName"] ?? "AdapterResources";
                var mongoClient = new MongoClient(resolvedConnectionString);
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
