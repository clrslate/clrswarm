using ClrSwarm.McpGateway.Management.Contracts;
using ClrSwarm.McpGateway.Management.Extensions;
using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.Extensions.Logging;

namespace ClrSwarm.McpGateway.Management.Store;

/// <summary>
/// MongoDB implementation of <see cref="IAdapterResourceStore"/>.
/// </summary>
public class MongoAdapterResourceStore : IAdapterResourceStore
{
    private readonly IMongoClient _client;
    private readonly string _databaseName;
    private readonly string _collectionName;
    private readonly ILogger _logger;
    private IMongoCollection<AdapterResource> _collection = default!;

    public MongoAdapterResourceStore(IMongoClient client, string databaseName, string collectionName, ILogger<MongoAdapterResourceStore> logger)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentException.ThrowIfNullOrEmpty(databaseName);
        ArgumentException.ThrowIfNullOrEmpty(collectionName);

        _client = client;
        _databaseName = databaseName;
        _collectionName = collectionName;
        _logger = logger;
        _collection = _client.GetDatabase(_databaseName).GetCollection<AdapterResource>(_collectionName);
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var database = _client.GetDatabase(_databaseName);

        // Create collection if it does not exist.
        var filter = new BsonDocument("name", _collectionName);
        var collections = await database.ListCollectionsAsync(new ListCollectionsOptions { Filter = filter }, cancellationToken).ConfigureAwait(false);
        if (!await collections.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            _logger.LogInformation("Creating Mongo collection {collection} in database {db}.", _collectionName.Sanitize(), _databaseName.Sanitize());
            await database.CreateCollectionAsync(_collectionName, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        _collection = database.GetCollection<AdapterResource>(_collectionName);

        // Ensure unique index on Id (adapter name).
        var indexKeys = Builders<AdapterResource>.IndexKeys.Ascending(x => x.Id);
        var indexOptions = new CreateIndexOptions { Unique = true, Name = "ux_adapter_id" };
        var indexModel = new CreateIndexModel<AdapterResource>(indexKeys, indexOptions);
        try
        {
            await _collection.Indexes.CreateOneAsync(indexModel, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (MongoCommandException ex) when (ex.CodeName == "IndexOptionsConflict")
        {
            // Index already exists with different options; log and continue.
            _logger.LogWarning(ex, "Index creation conflict for collection {collection}.", _collectionName.Sanitize());
        }
    }

    public async Task<AdapterResource?> TryGetAsync(string name, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        var cursor = await _collection.FindAsync(x => x.Id == name, cancellationToken: cancellationToken).ConfigureAwait(false);
        return await cursor.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpsertAsync(AdapterResource adapter, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(adapter);
        adapter.LastUpdatedAt = DateTimeOffset.UtcNow; // keep last updated timestamp current
        var options = new ReplaceOptions { IsUpsert = true };
        await _collection.ReplaceOneAsync(x => x.Id == adapter.Id, adapter, options, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(string name, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        await _collection.DeleteOneAsync(x => x.Id == name, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<AdapterResource>> ListAsync(CancellationToken cancellationToken)
    {
        var cursor = await _collection.FindAsync(FilterDefinition<AdapterResource>.Empty, cancellationToken: cancellationToken).ConfigureAwait(false);
        return await cursor.ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
