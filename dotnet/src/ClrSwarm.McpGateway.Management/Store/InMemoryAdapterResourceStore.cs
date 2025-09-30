
using System.Collections.Concurrent;
using ClrSwarm.McpGateway.Management.Contracts;

namespace ClrSwarm.McpGateway.Management.Store;

public class InMemoryAdapterResourceStore : IAdapterResourceStore
{
    private readonly ConcurrentDictionary<string, AdapterResource> _store = new();

    public Task InitializeAsync(CancellationToken cancellationToken)
    {
        // No-op for in-memory store
        return Task.CompletedTask;
    }

    public Task<AdapterResource?> TryGetAsync(string name, CancellationToken cancellationToken)
    {
        _store.TryGetValue(name, out var resource);
        return Task.FromResult(resource);
    }

    public Task UpsertAsync(AdapterResource adapter, CancellationToken cancellationToken)
    {
        _store[adapter.Name] = adapter;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string name, CancellationToken cancellationToken)
    {
        _store.TryRemove(name, out _);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<AdapterResource>> ListAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IEnumerable<AdapterResource>>([.. _store.Values]);
    }
}
