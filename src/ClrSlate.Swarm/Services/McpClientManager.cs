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
using ClrSlate.Swarm.Extensions;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;

namespace ClrSlate.Swarm.Services;

internal class McpClientManager : IMcpClientManager, IAsyncDisposable
{
    private readonly McpServersConfiguration _config;
    private readonly ILogger<McpClientManager> _logger;
    private readonly IMcpToolServiceFactory _toolServiceFactory;
    private readonly LazyAsync<Dictionary<string, IMcpToolService>> _toolServices;
    private readonly LazyAsync<Dictionary<string, string>> _toolToServerMap;

    public McpClientManager(IOptions<McpServersConfiguration> config, ILogger<McpClientManager> logger, IMcpToolServiceFactory toolServiceFactory)
    {
        _config = config.Value;
        _logger = logger;
        _toolServiceFactory = toolServiceFactory;
        _toolServices = new LazyAsync<Dictionary<string, IMcpToolService>>(CreateToolServicesAsync);
        _toolToServerMap = new LazyAsync<Dictionary<string, string>>(CreateToolToServerMapAsync);
    }

    private Task<Dictionary<string, IMcpToolService>> CreateToolServicesAsync()
    {
        var toolServices = new Dictionary<string, IMcpToolService>();
        foreach (var (serverName, serverConfig) in _config.McpServers) {
            try {
                var toolService = _toolServiceFactory.Create(serverConfig);
                toolServices[serverName] = toolService;
                _logger.LogInformation("Successfully initialized MCP server '{ServerName}'", serverName);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Failed to initialize MCP server '{ServerName}'", serverName);
            }
        }
        return Task.FromResult(toolServices);
    }

    private async Task<Dictionary<string, string>> CreateToolToServerMapAsync()
    {
        var toolServices = await _toolServices.ValueAsync;
        var toolToServerMap = new Dictionary<string, string>();
        foreach (var (serverName, toolService) in toolServices) {
            try {
                var tools = await toolService.ListToolsAsync();
                foreach (var tool in tools) {
                    toolToServerMap[tool.Name] = serverName;
                }
                _logger.LogInformation("Mapped {ToolCount} tools for MCP server '{ServerName}'", tools.Count, serverName);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Failed to list tools from server '{ServerName}'", serverName);
            }
        }
        return toolToServerMap;
    }

    public async Task<IEnumerable<Tool>> GetAllToolsAsync(CancellationToken cancellationToken = default)
    {
        var allTools = new List<Tool>();
        var toolServices = await _toolServices.ValueAsync;
        foreach (var (serverName, toolService) in toolServices) {
            try {
                var tools = await toolService.ListToolsAsync(cancellationToken);
                allTools.AddRange(tools);
                Console.WriteLine($"{serverName}:");
                foreach (var tool in tools) {
                    Console.WriteLine($"""
                        {tool.Name}:
                            title: {tool.Title}
                            description: {tool.Description}
                    """);
                }
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Failed to get tools from server '{ServerName}'", serverName);
            }
        }
        return allTools;
    }

    public async Task<CallToolResult> CallToolAsync(string toolName, IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken = default)
    {
        var toolServices = await _toolServices.ValueAsync;
        var toolToServerMap = await _toolToServerMap.ValueAsync;
        if (!toolToServerMap.TryGetValue(toolName, out var serverName)) {
            throw new InvalidOperationException($"Tool '{toolName}' not found in any configured server");
        }

        if (!toolServices.TryGetValue(serverName, out var toolService)) {
            throw new InvalidOperationException($"Server '{serverName}' is not available");
        }

        return await toolService.CallToolAsync(toolName, arguments, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        var toolServices = await _toolServices.ValueAsync;
        foreach (var toolService in toolServices.Values) {
            await toolService.DisposeAsync();
        }
        toolServices.Clear();
        var toolToServerMap = await _toolToServerMap.ValueAsync;
        toolToServerMap.Clear();
    }
}