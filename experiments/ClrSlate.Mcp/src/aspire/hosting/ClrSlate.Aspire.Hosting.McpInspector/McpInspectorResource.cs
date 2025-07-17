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

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace ClrSlate.Aspire.Hosting.McpInspector;

/// <summary>
/// Resource for the MCP Inspector server.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class McpInspectorResource(string name) : ExecutableResource(name, "npx", "")
{
    internal readonly string ConfigPath = Path.GetTempFileName();

    /// <summary>
    /// The name of the client endpoint.
    /// </summary>
    public const string ClientEndpointName = "client";

    private EndpointReference? _clientEndpoint;

    /// <summary>
    /// Gets the client endpoint reference for the MCP Inspector.
    /// </summary>
    public EndpointReference ClientEndpoint => _clientEndpoint ??= new(this, ClientEndpointName);

    /// <summary>
    /// The name of the server proxy endpoint.
    /// </summary>
    public const string ServerProxyEndpointName = "server-proxy";

    private EndpointReference? _serverProxyEndpoint;

    /// <summary>
    /// Gets the server proxy endpoint reference for the MCP Inspector.
    /// </summary>
    public EndpointReference ServerProxyEndpoint => _serverProxyEndpoint ??= new(this, ServerProxyEndpointName);

    /// <summary>
    /// Gets the version of the MCP Inspector.
    /// </summary>
    public const string InspectorVersion = "0.15.0";

    private readonly List<McpServerMetadata> _mcpServers = [];

    private McpServerMetadata? _defaultMcpServer;

    /// <summary>
    /// List of MCP server resources that this inspector is aware of.
    /// </summary>
    public IReadOnlyList<McpServerMetadata> McpServers => _mcpServers;

    /// <summary>
    /// Gets the default MCP server resource.
    /// </summary>
    public McpServerMetadata? DefaultMcpServer => _defaultMcpServer;

    internal void AddMcpServer(IResourceWithEndpoints mcpServer, bool isDefault, McpTransportType transportType)
    {
        if (_mcpServers.Any(s => s.Name == mcpServer.Name))
        {
            throw new InvalidOperationException($"The MCP server {mcpServer.Name} is already added to the MCP Inspector resource.");
        }

        McpServerMetadata item = new(
            mcpServer.Name,
            mcpServer.GetEndpoint("http") ?? throw new InvalidOperationException($"The MCP server {mcpServer.Name} must have a 'http' endpoint defined."),
            transportType);

        _mcpServers.Add(item);

        if (isDefault)
        {
            _defaultMcpServer = item;
        }
    }
}
