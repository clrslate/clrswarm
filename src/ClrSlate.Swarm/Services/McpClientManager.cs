using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Microsoft.Extensions.Options;

namespace ClrSlate.Swarm.Services;

internal class McpClientManager : IMcpClientManager, IAsyncDisposable
{
    private readonly McpServersConfiguration _config;
    private readonly ILogger<McpClientManager> _logger;
    private readonly Dictionary<string, IMcpClient> _clients = new();
    private readonly Dictionary<string, string> _toolToServerMap = new();
    private bool _initialized = false;

    public McpClientManager(IOptions<McpServersConfiguration> config, ILogger<McpClientManager> logger)
    {
        _config = config.Value;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        if (_initialized) return;

        var defaultOptions = new McpClientOptions {
            ClientInfo = new() { Name = "McpGateway", Version = "1.0.0" }
        };

        foreach (var (serverName, serverConfig) in _config.McpServers) {
            try {
                IMcpClient client;

                if (serverConfig.IsStdioTransport) {
                    var transportOptions = new StdioClientTransportOptions {
                        Command = serverConfig.Command!,
                        Arguments = serverConfig.Args ?? Array.Empty<string>(),
                        Name = serverName,
                        EnvironmentVariables = serverConfig.Env ?? new Dictionary<string, string?>()
                    };

                    client = await McpClientFactory.CreateAsync(
                        new StdioClientTransport(transportOptions),
                        defaultOptions);
                }
                else if (serverConfig.IsSseTransport) {
                    var transportOptions = new SseClientTransportOptions {
                        Endpoint = new Uri(serverConfig.Endpoint!),
                        Name = serverConfig.Name ?? serverName
                    };

                    client = await McpClientFactory.CreateAsync(
                        new SseClientTransport(transportOptions),
                        defaultOptions);
                }
                else {
                    _logger.LogWarning("Unsupported transport type '{Transport}' for server '{ServerName}'",
                        serverConfig.Transport, serverName);
                    continue;
                }

                _clients[serverName] = client;

                // Get tools and map them to the server
                var tools = await client.ListToolsAsync();
                foreach (var tool in tools) {
                    _toolToServerMap[tool.Name] = serverName;
                }

                _logger.LogInformation("Successfully initialized MCP server '{ServerName}' with {ToolCount} tools",
                    serverName, tools.Count);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Failed to initialize MCP server '{ServerName}'", serverName);
            }
        }

        _initialized = true;
    }

    public async Task<IEnumerable<Tool>> GetAllToolsAsync(CancellationToken cancellationToken = default)
    {
        var allTools = new List<Tool>();

        foreach (var (serverName, client) in _clients) {
            try {
                var toolInfos = await client.ListToolsAsync();
                var tools = toolInfos.Select(t => t.ProtocolTool);
                allTools.AddRange(tools);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Failed to get tools from server '{ServerName}'", serverName);
            }
        }

        return allTools;
    }

    public async Task<CallToolResult> CallToolAsync(string toolName, IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken = default)
    {
        if (!_toolToServerMap.TryGetValue(toolName, out var serverName)) {
            throw new InvalidOperationException($"Tool '{toolName}' not found in any configured server");
        }

        if (!_clients.TryGetValue(serverName, out var client)) {
            throw new InvalidOperationException($"Server '{serverName}' is not available");
        }

        return await client.CallToolAsync(toolName, arguments: arguments, cancellationToken: cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var client in _clients.Values) {
            if (client is IAsyncDisposable asyncDisposable) {
                await asyncDisposable.DisposeAsync();
            }
            else if (client is IDisposable disposable) {
                disposable.Dispose();
            }
        }
        _clients.Clear();
        _toolToServerMap.Clear();
    }
}