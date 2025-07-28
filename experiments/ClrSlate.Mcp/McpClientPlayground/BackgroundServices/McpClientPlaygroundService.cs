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

﻿using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Client;

namespace McpClientPlayground.BackgroundServices;
internal class McpClientPlaygroundService : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var options = new SseClientTransportOptions {
            Endpoint = new Uri("http://localhost:58750"),
            Name = "StreamableHttpServer",
            TransportMode = HttpTransportMode.StreamableHttp
        };

        await using var client = await McpClientFactory.CreateAsync(
            new SseClientTransport(options),
            new McpClientOptions {
                ClientInfo = new() {
                    Name = "ClrCore",
                    Version = "1.0.0"
                }
            }
        );

        var tools = await client.ListToolsAsync(cancellationToken: cancellationToken);
        foreach (var tool in tools) {
            Console.WriteLine($"""
            {tool.Name}:
                title: {tool.Title}
                description: {tool.Description}
            """);
        }

        var resources = await client.ListResourcesAsync(cancellationToken);
        foreach(var resource in resources) {
            Console.WriteLine($"""
            {resource.Name}:
                title: {resource.Title}
                description: {resource.Description}
                uri: {resource.Uri}
                mimeType: {resource.MimeType}
            """);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Playground Hosted Service is stop initiated...");
        return Task.CompletedTask;
    }
}
