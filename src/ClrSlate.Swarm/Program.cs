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

using ClrSlate.Swarm.Services;
using ModelContextProtocol.Protocol;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Microsoft.AspNetCore.DataProtection;
using ClrSlate.Swarm.Abstractions;
using ClrSlate.Swarm.Services.StdioCommandHandlers;

var builder = WebApplication.CreateBuilder(args);

// Configure Data Protection to persist keys to a stable directory
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine("data","dataprotection-keys")));

// Configure MCP servers from appsettings
builder.Services.Configure<McpServersConfiguration>(
    builder.Configuration);

// Register the MCP client manager with all dependencies
builder.Services.AddSingleton<IMcpClientManager, McpClientManager>();
builder.Services.AddSingleton<McpClientManager>(); // If direct use is needed

// Register command handlers and factory for DI
builder.Services.AddSingleton<IStdioCommandHandler, NpxCommandHandler>();
builder.Services.AddSingleton<IStdioCommandHandlerFactory, StdioCommandHandlerFactory>();
builder.Services.AddSingleton<IMcpToolServiceFactory, McpToolServiceFactory>();

// Add health checks
builder.Services.AddHealthChecks();

var mcpServerBuilder = builder.Services.AddMcpServer()
    .WithHttpTransport(options => {
        options.Stateless = true;
    });

builder.Services.AddOpenTelemetry()
    .WithTracing(b => b.AddSource("*")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation())
    .WithMetrics(b => b.AddMeter("*")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation())
    .WithLogging()
    .UseOtlpExporter();


mcpServerBuilder.WithListToolsHandler(async (context, cancellationToken) => {
    var clientManager = context!.Services!.GetRequiredService<IMcpClientManager>();
    var tools = await clientManager.GetAllToolsAsync(cancellationToken);
    IList<Tool> toolsResult = tools.ToList();
    return new ListToolsResult { Tools = toolsResult };
});

mcpServerBuilder.WithCallToolHandler(async (context, cancellationToken) => {
    Dictionary<string, object?> inputArguments = [];
    if (context?.Params?.Arguments != null) {
        foreach (var arg in context?.Params?.Arguments!) {
            inputArguments.Add(arg.Key, arg.Value);
        }
    }

    var clientManager = context!.Services!.GetRequiredService<IMcpClientManager>();
    var result = await clientManager.CallToolAsync(context!.Params!.Name, arguments: inputArguments!, cancellationToken: cancellationToken);
    return result;
});

var app = builder.Build();

// Map liveness and readiness endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/ready");

var clientManager = app.Services.GetRequiredService<IMcpClientManager>();
await clientManager.InitializeAsync();

// Display available tools for debugging
var tools = await clientManager.GetAllToolsAsync();
foreach (var tool in tools) {
    Console.WriteLine($"""
        {tool.Name}:
            title: {tool.Title}
            description: {tool.Description}
        """);
}

app.MapMcp();

await app.RunAsync();
