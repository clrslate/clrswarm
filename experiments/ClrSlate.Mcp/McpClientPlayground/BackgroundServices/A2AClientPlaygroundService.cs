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

using McpClientPlayground.A2AServices;
using McpClientPlayground.Models;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace McpClientPlayground.BackgroundServices;

internal class A2AClientPlaygroundService(OpenAiOptions aiOptions, ILogger<A2AClientPlaygroundService> logger) : IHostedService
{
    private readonly string agentUrls = "http://localhost:5041";
    public async Task StartAsync(CancellationToken cancellationToken) {

        // Create the Host agent
        var hostAgent = new HostClientAgent(logger);
        await hostAgent.InitializeAgentAsync("azure/o4-mini", "https://litellm.beta.clrslate.app", aiOptions.ApiKey, agentUrls!.Split(";"));
        AgentThread thread = new ChatHistoryAgentThread();
        try {
            while (true) {
                // Get user message
                Console.Write("\nUser (:q or quit to exit): ");
                string? message = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(message)) {
                    Console.WriteLine("Request cannot be empty.");
                    continue;
                }

                if (message == ":q" || message == "quit") {
                    break;
                }

                await foreach (AgentResponseItem<ChatMessageContent> response in hostAgent.Agent!.InvokeAsync(message, thread)) {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"\nAgent: {response.Message.Content}");
                    Console.ResetColor();

                    thread = response.Thread;
                }
            }
        }
        catch (Exception ex) {
            logger.LogError(ex, "An error occurred while running the A2AClient");
            return;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Playground Hosted Service is stop initiated...");
        return Task.CompletedTask;
    }
}
