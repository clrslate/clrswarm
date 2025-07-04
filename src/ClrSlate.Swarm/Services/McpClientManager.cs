/*
 * Copyright 2025 ClrSlate Tech labs Private Limited
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using ClrSlate.Swarm.Abstractions;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;

namespace ClrSlate.Swarm.Services;

internal class McpClientManager : IMcpClientManager, IAsyncDisposable
{
    private readonly McpServersConfiguration _config;
    private readonly ILogger<McpClientManager> _logger;
    private readonly IMcpToolServiceFactory _toolServiceFactory;
    private readonly Dictionary<string, IMcpToolService> _toolServices = new();
    private readonly Dictionary<string, string> _toolToServerMap = new();
    private bool _initialized = false;

    public McpClientManager(IOptions<McpServersConfiguration> config, ILogger<McpClientManager> logger)
        : this(config, logger, new McpToolServiceFactory()) { }

    public McpClientManager(IOptions<McpServersConfiguration> config, ILogger<McpClientManager> logger, IMcpToolServiceFactory toolServiceFactory)
    {
        _config = config.Value;
        _logger = logger;
        _toolServiceFactory = toolServiceFactory;
    }

    public async Task InitializeAsync()
    {
        if (_initialized) return;

        foreach (var (serverName, serverConfig) in _config.McpServers)
        {
            try
            {
                var toolService = _toolServiceFactory.Create(serverConfig);
                _toolServices[serverName] = toolService;

                var tools = await toolService.ListToolsAsync();
                foreach (var tool in tools)
                {
                    _toolToServerMap[tool.Name] = serverName;
                }

                _logger.LogInformation("Successfully initialized MCP server '{ServerName}' with {ToolCount} tools", serverName, tools.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize MCP server '{ServerName}'", serverName);
            }
        }

        _initialized = true;
    }

    public async Task<IEnumerable<Tool>> GetAllToolsAsync(CancellationToken cancellationToken = default)
    {
        var allTools = new List<Tool>();

        foreach (var (serverName, toolService) in _toolServices)
        {
            try
            {
                var tools = await toolService.ListToolsAsync(cancellationToken);
                allTools.AddRange(tools);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get tools from server '{ServerName}'", serverName);
            }
        }

        return allTools;
    }

    public async Task<CallToolResult> CallToolAsync(string toolName, IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken = default)
    {
        if (!_toolToServerMap.TryGetValue(toolName, out var serverName))
        {
            throw new InvalidOperationException($"Tool '{toolName}' not found in any configured server");
        }

        if (!_toolServices.TryGetValue(serverName, out var toolService))
        {
            throw new InvalidOperationException($"Server '{serverName}' is not available");
        }

        return await toolService.CallToolAsync(toolName, arguments, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var toolService in _toolServices.Values)
        {
            await toolService.DisposeAsync();
        }
        _toolServices.Clear();
        _toolToServerMap.Clear();
    }
}