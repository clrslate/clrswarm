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

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.A2A;
using ModelContextProtocol.Client;
using SharpA2A.Core;
using System.Collections.Concurrent;

namespace ClrSlate.Agents.TestAgent;

public static class HostAgentFactory
{
    // Keep MCP clients alive to prevent disposal while agents are using them
    private static readonly ConcurrentDictionary<string, IMcpClient> _mcpClients = new();
    internal static async Task<A2AHostAgent> CreateChatCompletionHostAgentAsync(
        string agentType,
        string name,
        string instructions,
        OpenAiOptions options,
        IEnumerable<KernelPlugin>? plugins = null)
    {
        var builder = Kernel.CreateBuilder();
        builder.AddAzureOpenAIChatCompletion(options.ModelId, options.Endpoint, options.ApiKey);
        if (plugins is not null)
        {
            foreach (var plugin in plugins)
            {
                builder.Plugins.Add(plugin);
            }
        }
        var kernel = builder.Build();
        var mcpOptions = new SseClientTransportOptions
        {
            Endpoint = new Uri("http://localhost:58750"),
            Name = "StreamableHttpServer",
            TransportMode = HttpTransportMode.StreamableHttp
        };

        // Create or reuse MCP client - keep it alive for the agent's lifetime
        var clientKey = $"{mcpOptions.Endpoint}_{name}";
        if (!_mcpClients.TryGetValue(clientKey, out var client))
        {
            client = await McpClientFactory.CreateAsync(
                new SseClientTransport(mcpOptions),
                new McpClientOptions
                {
                    ClientInfo = new()
                    {
                        Name = "ClrCore",
                        Version = "1.0.0"
                    }
                }
            );
            _mcpClients.TryAdd(clientKey, client);
        }

        var tools = await client.ListToolsAsync();
        kernel.Plugins.AddFromFunctions("Tools", tools.Select(aiFunction => aiFunction.AsKernelFunction()));

        // Add the MCP tools plugin that provides high-level access to MCP server functionality
        var mcpToolsPlugin = KernelPluginFactory.CreateFromObject(new Plugins.McpToolsPlugin(kernel), "McpTools");
        kernel.Plugins.Add(mcpToolsPlugin);

        var agent = new ChatCompletionAgent()
        {
            Kernel = kernel,
            Name = name,
            Instructions = instructions,
            Arguments = new KernelArguments(new PromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() }),
        };

        AgentCard agentCard = agentType.ToUpperInvariant() switch
        {
            "GENERAL" => GetGeneralAgentCard(),
            "CATALOG" => GetCatalogAgentCard(),
            _ => throw new ArgumentException($"Unsupported agent type: {agentType}"),
        };

        return new A2AHostAgent(agent, agentCard);
    }

    /// <summary>
    /// Cleans up MCP client resources. Call this when shutting down the application.
    /// </summary>
    public static void CleanupMcpClients()
    {
        foreach (var client in _mcpClients.Values)
        {
            try
            {
                if (client is IDisposable disposableClient)
                {
                    disposableClient.Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error disposing MCP client: {ex.Message}");
            }
        }
        _mcpClients.Clear();
    }

    #region private
    private static AgentCard GetCatalogAgentCard()
    {
        var capabilities = new AgentCapabilities()
        {
            Streaming = false,
            PushNotifications = false,
        };

        var catalogSkill = new AgentSkill()
        {
            Id = "id_catalog_agent",
            Name = "CatalogAgent",
            Description = "Handles multiple types of queries including semantic search operations, data management, and MCP server tools.",
            Tags = ["general", "search", "data", "semantic-kernel", "mcp-tools", "vector-database"],
            Examples =
            [
                "Find a package named azure",
                "Search for web development packages",
                "Find deployment activities",
                "Populate the database incrementally",
                "Test the MCP server connection"
            ],
        };

        return new AgentCard()
        {
            Name = "CatalogAgent",
            Description = "",
            Version = "1.0.0",
            DefaultInputModes = ["text"],
            DefaultOutputModes = ["text"],
            Capabilities = capabilities,
            Skills = [catalogSkill],
        };
    }
 

    private static AgentCard GetGeneralAgentCard()
    {
        var capabilities = new AgentCapabilities()
        {
            Streaming = false,
            PushNotifications = false,
        };

        var generalSkill = new AgentSkill()
        {
            Id = "id_general_agent",
            Name = "GeneralAgent",
            Description = "Handles multiple types of queries including semantic search operations, invoice queries, data management, and MCP server tools.",
            Tags = ["general", "search", "invoice", "data", "semantic-kernel", "mcp-tools", "vector-database"],
            Examples =
            [
                "Find a package named azure",
                "Search for web development packages",
                "Find deployment activities",
                "Search the entire catalog for authentication",
                "Do a keyword search for docker container",
                "List the latest invoices for Contoso",
                "Populate the database incrementally",
                "Test the MCP server connection",
                "Get a sample image from the MCP server"
            ],
        };

        return new AgentCard()
        {
            Name = "GeneralAgent",
            Description = "General-purpose agent that handles search operations, invoice queries, and data management tasks.",
            Version = "1.0.0",
            DefaultInputModes = ["text"],
            DefaultOutputModes = ["text"],
            Capabilities = capabilities,
            Skills = [generalSkill],
        };
    }
    #endregion
}

public record OpenAiOptions
{
    public string ModelId { get; set; } = "azure/gpt-4.1";
    public string Endpoint { get; set; } = "https://litellm.beta.clrslate.app";
    public string ApiKey { get; set; }
}