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

using ClrSlate.Agents.TestAgent;
using ClrSlate.Agents.TestAgent.Plugins;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.A2A;
using SharpA2A.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();
builder.Services.AddHttpClient().AddLogging();

// Add CORS services to allow any origin, method, and header
builder.Services.AddCors(options => {
    options.AddDefaultPolicy(policy => {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();
var credential = builder.Configuration["LiteLlm:Token"] ?? throw new InvalidOperationException("Missing configuration: GitHubModels:Token. See the README for details.");
var openAiOptions = new OpenAiOptions {
    ApiKey = credential
};

// Use CORS middleware
app.UseCors();

IEnumerable<KernelPlugin> invoicePlugins = [KernelPluginFactory.CreateFromType<InvoiceQueryPlugin>()];

A2AHostAgent? hostAgent = await HostAgentFactory.CreateChatCompletionHostAgentAsync(
            "INVOICE",
            "InvoiceAgent",
            """
            You specialize in handling queries related to invoices.
            """,
            openAiOptions,
            invoicePlugins);
app.MapA2A(hostAgent!.TaskManager!, "");
app.Run();
