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

// Copyright (c) Microsoft. All rights reserved.

using McpClientPlayground.ClientPlugins;
using McpClientPlayground.Models;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.A2A;
using OpenAI;
using SharpA2A.Core;
using System.ClientModel;

namespace McpClientPlayground.A2AServices;

internal sealed class HostClientAgent
{
    internal HostClientAgent(ILogger logger)
    {
        this._logger = logger;
    }
    internal async Task InitializeAgentAsync(string modelId, string endpoint, string apiKey, string[] agentUrls)
    {
        try
        {
            this._logger.LogInformation("Initializing Semantic Kernel agent with model: {ModelId}", modelId);

            // Connect to the remote agents via A2A
            var createAgentTasks = agentUrls.Select(agentUrl => this.CreateAgentAsync(agentUrl));
            var agents = await Task.WhenAll(createAgentTasks);
            var agentFunctions = agents.Select(agent => AgentKernelFunctionFactory.CreateFromAgent(agent)).ToList();
            var agentPlugin = KernelPluginFactory.CreateFromFunctions("AgentPlugin", agentFunctions);

            // Define the Host agent
            var builder = Kernel.CreateBuilder();
            var openAiClient = new OpenAIClient(new ApiKeyCredential(apiKey), new OpenAIClientOptions
            {
                Endpoint = new Uri(endpoint)
            });
            builder.AddOpenAIChatCompletion(modelId, openAiClient);
            builder.Plugins.Add(agentPlugin);
            builder.Plugins.AddFromObject(new WriteFilePlugin());
            var kernel = builder.Build();
            kernel.FunctionInvocationFilters.Add(new ConsoleOutputFunctionInvocationFilter());
            this.Agent = new ChatCompletionAgent()
            {
                Kernel = kernel,
                Name = "HostClient",
                Instructions =
    """"
    # Clrslate Assistant
    -Description 
    You are ClrSlate Assistant designed to convert user queries into meaningful workflows. 
    -IMPORTANT TO DOs
    Make sure that all activities included in the workflow are verified, you can check that by tools. 

    1. Understand the user's infrastructure requirements.
    3. Create a valid, sequential workflow. Make sure all activities exist using the mcp tools.
    4. Once the workflow is created, show it in the JSON format specified below.
    5. Ask the user for confirmation: "Do you want me to write this workflow to disk?".
    6. Only after user confirmation, write the workflow to disk using the WriteFile tool.
     

    -Workflow Output Format
    When constructing workflows, use the following JSON format to ensure clarity and consistency:
    Here the placeholder <activity-id> should be a real id, 
    """
                json
    {
      "name": "<setup-xyz>",
      "nodes": [
        {
          "activityId": "<activity-id>"
        }
      ],
      "edges":[
        {
          "source": "<source-activity-id>",
          "target": "<target-activity-id>"
        }
      ]  
    }
    """

    """"
,
                Arguments = new KernelArguments(new PromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() }),
            };
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Failed to initialize HostClientAgent");
            throw;
        }
    }

    /// <summary>
    /// The associated <see cref="Agent"/>
    /// </summary>
    public Agent? Agent { get; private set; }

    #region private
    private readonly ILogger _logger;

    private async Task<A2AAgent> CreateAgentAsync(string agentUri)
    {
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(agentUri),
            Timeout = TimeSpan.FromSeconds(60)
        };

        var client = new A2AClient(httpClient);
        var cardResolver = new A2ACardResolver(httpClient);
        var agentCard = await cardResolver.GetAgentCardAsync();

        return new A2AAgent(client, agentCard!);
    }
    #endregion
}

internal sealed class ConsoleOutputFunctionInvocationFilter() : IFunctionInvocationFilter
{
    private static string IndentMultilineString(string multilineText, int indentLevel = 1, int spacesPerIndent = 4)
    {
        // Create the indentation string
        var indentation = new string(' ', indentLevel * spacesPerIndent);

        // Split the text into lines, add indentation, and rejoin
        char[] NewLineChars = { '\r', '\n' };
        string[] lines = multilineText.Split(NewLineChars, StringSplitOptions.None);

        return string.Join(Environment.NewLine, lines.Select(line => indentation + line));
    }
    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;

        Console.WriteLine($"\nCalling Agent {context.Function.Name} with arguments:");
        Console.ForegroundColor = ConsoleColor.Gray;

        foreach (var kvp in context.Arguments)
        {
            Console.WriteLine(IndentMultilineString($"  {kvp.Key}: {kvp.Value}"));
        }

        await next(context);

        if (context.Result.GetValue<object>() is ChatMessageContent[] chatMessages)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;

            Console.WriteLine($"Response from Agent {context.Function.Name}:");
            foreach (var message in chatMessages)
            {
                Console.ForegroundColor = ConsoleColor.Gray;

                Console.WriteLine(IndentMultilineString($"{message}"));
            }
        }
        Console.ResetColor();
    }
}
