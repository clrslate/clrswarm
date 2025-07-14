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

﻿using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.A2A;
using SharpA2A.Core;

namespace ClrSlate.Agents.TestAgent;

public static class HostAgentFactory
{
    internal static async Task<A2AHostAgent> CreateChatCompletionHostAgentAsync(
        string agentType,
        string name,
        string instructions,
        OpenAiOptions options,
        IEnumerable<KernelPlugin>? plugins = null)
    {
        var builder = Kernel.CreateBuilder();
        builder.AddAzureOpenAIChatCompletion(options.ModelId, options.Endpoint, options.ApiKey);
        if (plugins is not null) {
            foreach (var plugin in plugins) {
                builder.Plugins.Add(plugin);
            }
        }
        var kernel = builder.Build();

        var agent = new ChatCompletionAgent() {
            Kernel = kernel,
            Name = name,
            Instructions = instructions,
            Arguments = new KernelArguments(new PromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() }),
        };

        AgentCard agentCard = agentType.ToUpperInvariant() switch {
            "INVOICE" => GetInvoiceAgentCard(),
            "POLICY" => GetPolicyAgentCard(),
            "LOGISTICS" => GetLogisticsAgentCard(),
            _ => throw new ArgumentException($"Unsupported agent type: {agentType}"),
        };

        return new A2AHostAgent(agent, agentCard);
    }

    #region private
    private static AgentCard GetInvoiceAgentCard()
    {
        var capabilities = new AgentCapabilities() {
            Streaming = false,
            PushNotifications = false,
        };

        var invoiceQuery = new AgentSkill() {
            Id = "id_invoice_agent",
            Name = "InvoiceQuery",
            Description = "Handles requests relating to invoices.",
            Tags = ["invoice", "semantic-kernel"],
            Examples =
            [
                "List the latest invoices for Contoso.",
            ],
        };

        return new() {
            Name = "InvoiceAgent",
            Description = "Handles requests relating to invoices.",
            Version = "1.0.0",
            DefaultInputModes = ["text"],
            DefaultOutputModes = ["text"],
            Capabilities = capabilities,
            Skills = [invoiceQuery],
        };
    }

    private static AgentCard GetPolicyAgentCard()
    {
        var capabilities = new AgentCapabilities() {
            Streaming = false,
            PushNotifications = false,
        };

        var invoiceQuery = new AgentSkill() {
            Id = "id_policy_agent",
            Name = "PolicyAgent",
            Description = "Handles requests relating to policies and customer communications.",
            Tags = ["policy", "semantic-kernel"],
            Examples =
            [
                "What is the policy for short shipments?",
            ],
        };

        return new AgentCard() {
            Name = "PolicyAgent",
            Description = "Handles requests relating to policies and customer communications.",
            Version = "1.0.0",
            DefaultInputModes = ["text"],
            DefaultOutputModes = ["text"],
            Capabilities = capabilities,
            Skills = [invoiceQuery],
        };
    }

    private static AgentCard GetLogisticsAgentCard()
    {
        var capabilities = new AgentCapabilities() {
            Streaming = false,
            PushNotifications = false,
        };

        var invoiceQuery = new AgentSkill() {
            Id = "id_invoice_agent",
            Name = "LogisticsQuery",
            Description = "Handles requests relating to logistics.",
            Tags = ["logistics", "semantic-kernel"],
            Examples =
            [
                "What is the status for SHPMT-SAP-001",
            ],
        };

        return new AgentCard() {
            Name = "LogisticsAgent",
            Description = "Handles requests relating to logistics.",
            Version = "1.0.0",
            DefaultInputModes = ["text"],
            DefaultOutputModes = ["text"],
            Capabilities = capabilities,
            Skills = [invoiceQuery],
        };
    }
    #endregion
}

public record OpenAiOptions { 
    public string ModelId { get; set; } = "azure/o4-mini";
    public string Endpoint { get; set; } = "https://litellm.beta.clrslate.app";
    public string ApiKey { get; set; }
}