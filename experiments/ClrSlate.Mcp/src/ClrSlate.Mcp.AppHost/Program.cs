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

using ClrSlate.Aspire.Hosting.McpInspector;
using Elsa.Aspire.AppHost;
using Microsoft.Extensions.Configuration;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var keycloakConfig = builder.Configuration.GetSection("keycloak").Get<KeyCloakConfig>();

var username = builder.AddParameter("username", keycloakConfig.Admin.UserName);
var password = builder.AddParameter("password", keycloakConfig.Admin.Password, secret: true);

var keycloak = builder.AddKeycloak("keycloak", adminUsername: username, adminPassword: password)
    .WithDataVolume();
var qdrant = builder.AddQdrant("qdrant");
var ollama = builder.AddOllama("ollama", 11434)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume(); // Persist models across container restarts

var model = ollama.AddModel("nomic-embed-text");
var keycloakMcpServer = builder.AddProject<Projects.ClrSlate_Mcp_KeyCloakServer>("keycloak-mcp")
        .WithEnvironment("keycloak__userName", username)
        .WithEnvironment("keycloak__password", password)
        .WithReference(keycloak).WaitFor(keycloak)
        .WithReference(qdrant).WaitFor(qdrant)
        .WithReference(model).WaitFor(model)   // Reference the model for dependency management
        .WithReference(ollama).WaitFor(ollama);  // Reference the Ollama service for HTTP access
var a2aTestAgent = builder.AddProject<Projects.ClrSlate_Agents_TestAgent>("a2a-test-agent")
    .WithReference(keycloakMcpServer).WaitFor(keycloakMcpServer);

// Add specialized agents (ports configured in launchSettings.json)
//var searchAgent = builder.AddProject<Projects.ClrSlate_Agents_SearchAgent>("search-agent")
//    .WithReference(keycloakMcpServer).WaitFor(keycloakMcpServer);

//var dataAgent = builder.AddProject<Projects.ClrSlate_Agents_DataAgent>("data-agent")
//    .WithReference(keycloakMcpServer).WaitFor(keycloakMcpServer);

// Update playground to connect to available agents
var playground = builder.AddProject<Projects.McpClientPlayground>("playground")
    .WithEnvironment("AgentUrls", "http://localhost:5041")
    .WithReference(a2aTestAgent).WaitFor(a2aTestAgent)
    .WithReference(keycloakMcpServer).WaitFor(keycloakMcpServer);

builder.AddMcpInspector("mcp-inspector")
    .WithMcpServer(keycloakMcpServer);

builder.Build().Run();
