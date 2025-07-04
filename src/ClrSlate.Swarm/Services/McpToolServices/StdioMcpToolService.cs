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

using ModelContextProtocol.Client;
using ClrSlate.Swarm.Options;
using ClrSlate.Swarm.Abstractions;
using ClrSlate.Swarm.Services.StdioCommandHandlers;

namespace ClrSlate.Swarm.Services.McpToolServices;

internal class StdioMcpToolService : McpToolServiceBase
{
    private readonly McpServerConfig _config;
    private readonly IStdioCommandHandlerFactory _handlerFactory;

    public StdioMcpToolService(McpServerConfig config, IStdioCommandHandlerFactory handlerFactory)
    {
        _config = config;
        _handlerFactory = handlerFactory;
    }

    protected override async Task<IMcpClient> CreateClientAsync()
    {
        //var handler = _handlerFactory.GetHandler(_config.Command);
        //if (handler != null)
        //{
        //    await handler.HandleAsync(_config.Args);
        //}

        var options = new StdioClientTransportOptions {
            Command = _config.Command!,
            Arguments = _config.Args ?? Array.Empty<string>(),
            Name = _config.Name ?? "StdioServer",
            EnvironmentVariables = _config.Env ?? new Dictionary<string, string?>()
        };

        return await McpClientFactory.CreateAsync(
            new StdioClientTransport(options),
            new McpClientOptions { ClientInfo = new() { Name = "McpGateway", Version = "1.0.0" } }
        );
    }
}