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

using ClrSlate.Mcp.KeyCloakServer.Models;
using ClrSlate.Mcp.KeyCloakServer.Prompts;
using ClrSlate.Mcp.KeyCloakServer.Resources;
using ClrSlate.Mcp.KeyCloakServer.Services;
using ClrSlate.Mcp.KeyCloakServer.Tools;
using ModelContextProtocol.Protocol;
using Qdrant.Client;

var builder = WebApplication.CreateBuilder(args);

// Configure search settings
builder.Services.Configure<SearchConfiguration>(
    builder.Configuration.GetSection(SearchConfiguration.SectionName));

// Add HTTP client for catalog API calls
builder.Services.AddHttpClient<CatalogApiService>(client =>
{
    client.BaseAddress = new Uri("https://store.beta.clrslate.app");
    client.DefaultRequestHeaders.Add("User-Agent", "ClrSlateKeyCloakServer/1.0");
});

// Add HTTP client for Ollama API calls
builder.Services.AddHttpClient("ollama", client =>
{
    client.BaseAddress = new Uri("http://localhost:11434");
    client.DefaultRequestHeaders.Add("User-Agent", "ClrSlateKeyCloakServer/1.0");
    client.Timeout = TimeSpan.FromMinutes(5);
});

// Add Qdrant client
builder.Services.AddSingleton<QdrantClient>(serviceProvider =>
{
    var logger = serviceProvider.GetRequiredService<ILogger<QdrantClient>>();
    var config = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<SearchConfiguration>>().Value;

    try
    {
        logger.LogInformation("Attempting to connect to Qdrant gRPC at {Host}:{Port}", config.QdrantHost, config.QdrantPort);
        return new QdrantClient(config.QdrantHost, config.QdrantPort);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to create QdrantClient. Make sure Qdrant is running on {Host}:{Port}", config.QdrantHost, config.QdrantPort);
        logger.LogWarning("Creating QdrantClient anyway - connection will be tested during service initialization");
        return new QdrantClient(config.QdrantHost, config.QdrantPort);
    }
});

// Register search services
builder.Services.AddSingleton<CatalogApiService>();
builder.Services.AddSingleton<EmbeddingService>();
builder.Services.AddSingleton<VectorStorageService>();
builder.Services.AddSingleton<SearchService>();

var mcpBuilder = builder.Services.AddMcpServer(options =>
{
    options.ServerInstructions = "This is a ClrSlate MCP server for KeyCloak integration with semantic search capabilities. It provides tools for searching catalog packages and activities using vector embeddings.";
    options.ServerInfo = new Implementation
    {
        Name = "ClrSlateKeyCloakServer",
        Title = "ClrSlate MCP KeyCloak Server with Semantic Search",
        Version = "1.1.0"
    };
})
    .WithHttpTransport(options =>
    {
        options.Stateless = true;
    })
    .WithTools<EchoTool>()
    .WithTools<TinyImageTool>()
    .WithTools<SemanticSearchTool>()
    .WithTools<DataIngestionTool>()
    .WithPrompts<SimplePromptType>()
    .WithPrompts<ComplexPromptType>()
    .WithResources<SimpleResourceType>()
    .WithCompleteHandler(async (ctx, ct) =>
    {
        var exampleCompletions = new Dictionary<string, IEnumerable<string>>
        {
            { "style", ["casual", "formal", "technical", "friendly"] },
            { "temperature", ["0", "0.5", "0.7", "1.0"] },
            { "resourceId", ["1", "2", "3", "4", "5"] }
        };

        if (ctx.Params is not { } @params)
        {
            throw new NotSupportedException($"Params are required.");
        }

        var @ref = @params.Ref;
        var argument = @params.Argument;

        if (@ref is ResourceTemplateReference rtr)
        {
            var resourceId = rtr.Uri?.Split("/").Last();

            if (resourceId is null)
            {
                return new CompleteResult();
            }

            var values = exampleCompletions["resourceId"].Where(id => id.StartsWith(argument.Value));

            return new CompleteResult
            {
                Completion = new Completion { Values = [.. values], HasMore = false, Total = values.Count() }
            };
        }

        if (@ref is PromptReference pr)
        {
            if (!exampleCompletions.TryGetValue(argument.Name, out IEnumerable<string>? value))
            {
                throw new NotSupportedException($"Unknown argument name: {argument.Name}");
            }

            var values = value.Where(value => value.StartsWith(argument.Value));
            return new CompleteResult
            {
                Completion = new Completion { Values = [.. values], HasMore = false, Total = values.Count() }
            };
        }

        throw new NotSupportedException($"Unknown reference type: {@ref.Type}");
    });

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();
var app = builder.Build();

app.MapMcp();

app.Run();
