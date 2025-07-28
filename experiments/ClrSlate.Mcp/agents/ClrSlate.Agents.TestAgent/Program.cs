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
using ModelContextProtocol.Client;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();
builder.Services.AddHttpClient().AddRequestTimeouts().AddLogging();

// Add CORS services to allow any origin, method, and header
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();
var credential = builder.Configuration["LiteLlm:Token"] ?? throw new InvalidOperationException("Missing configuration: GitHubModels:Token. See the README for details.");
var openAiOptions = new OpenAiOptions
{
    ApiKey = credential
};

// Use CORS middleware
app.UseCors();


A2AHostAgent? catalogAgent = await HostAgentFactory.CreateChatCompletionHostAgentAsync(
            "CATALOG",
            "CatalogAgent",
            """"
            You are the CatalogAgent. You are expected to respond using tool calls wherever possible instead of replying in natural language.            
            When returning tool calls, always return in structured output, dont sumarize yourself.
            Always reference IDs of packages and activities that you are including in your response.
            """",
            openAiOptions);

            

app.MapA2A(catalogAgent!.TaskManager!, "");

// Ensure MCP clients are cleaned up on application shutdown
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(() =>
{
    HostAgentFactory.CleanupMcpClients();
});

app.Run();
