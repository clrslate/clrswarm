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

using McpClientPlayground.BackgroundServices;
using McpClientPlayground.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;
using OpenAI;
using System.ClientModel;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddUserSecrets(typeof(A2AClientPlaygroundService).Assembly);
builder.AddServiceDefaults();

var credential = builder.Configuration["LiteLlm:Token"] ?? throw new InvalidOperationException("Missing configuration: LiteLlm:Token. See the README for details.");
var openAiOptions = new OpenAiOptions {
    ApiKey = credential
};
builder.Services.AddSingleton(openAiOptions);

var openAIClientOptions = new OpenAIClientOptions() { Endpoint = new Uri(openAiOptions.Endpoint) };
var openAiClient = new OpenAIClient(new ApiKeyCredential(credential), openAIClientOptions);
builder.Services.AddOpenAIChatCompletion(openAiOptions.ModelId, openAiClient);
builder.Services.AddHostedService<A2AClientPlaygroundService>();
var host = builder.Build();

await host.StartAsync();
Console.WriteLine("Stopping the host application in 2 seconds...");
await Task.Delay(2000); // Simulate some work before stopping
await host.StopAsync();
Console.WriteLine("Host application has been successfully stopped.");

Console.ReadLine();
