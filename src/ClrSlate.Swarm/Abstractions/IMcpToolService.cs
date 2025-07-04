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

using ClrSlate.Swarm.Extensions;
using ClrSlate.Swarm.Options;
using ClrSlate.Swarm.Services;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace ClrSlate.Swarm.Abstractions;

public interface IMcpToolService : IAsyncDisposable
{
    Task<IReadOnlyList<Tool>> ListToolsAsync(CancellationToken cancellationToken = default);
    Task<CallToolResult> CallToolAsync(string toolName, IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken = default);
}

public abstract class McpToolServiceBase : IMcpToolService
{
    protected readonly LazyAsync<IMcpClient> _client;

    protected McpToolServiceBase()
    {
        _client = new LazyAsync<IMcpClient>(CreateClientAsync);
    }

    public virtual async Task<IReadOnlyList<Tool>> ListToolsAsync(CancellationToken cancellationToken = default)
        => await _client.Value.ListAllToolsAsync(cancellationToken);

    public virtual async Task<CallToolResult> CallToolAsync(string toolName, IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken = default)
        => await _client.Value.CallToolAsync(toolName, arguments, cancellationToken: cancellationToken);

    public virtual async ValueTask DisposeAsync()
    {
        if (_client.IsValueCreated)
            await _client.Value.DisposeAsync();
    }

    protected abstract Task<IMcpClient> CreateClientAsync();
}

public interface IMcpToolServiceFactory
{
    IMcpToolService Create(McpServerConfig config);
}
