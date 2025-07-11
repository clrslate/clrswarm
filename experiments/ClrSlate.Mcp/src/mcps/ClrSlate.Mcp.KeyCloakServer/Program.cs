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

﻿using ClrSlate.Mcp.KeyCloakServer.Prompts;
using ClrSlate.Mcp.KeyCloakServer.Resources;
using ClrSlate.Mcp.KeyCloakServer.Tools;
using ModelContextProtocol.Protocol;

var builder = WebApplication.CreateBuilder(args);
var mcpBuilder =  builder.Services.AddMcpServer(options => { 
        options.ServerInstructions = "This is a sample MCP server for KeyCloak integration. It provides tools and prompts for AI content generation.";
        options.ServerInfo = new Implementation {
            Name = "ClrSlateKeyCloakServer",
            Title = "ClrSlate MCP KeyCloak Server",
            Version = "1.0.0"
        };
    })    
    .WithHttpTransport(options => {
        options.Stateless = true;
    })
    .WithTools<EchoTool>()
    .WithTools<TinyImageTool>()
    .WithPrompts<SimplePromptType>()
    .WithPrompts<ComplexPromptType>()
    .WithResources<SimpleResourceType>()
    .WithCompleteHandler(async (ctx, ct) => {
        var exampleCompletions = new Dictionary<string, IEnumerable<string>>
        {
            { "style", ["casual", "formal", "technical", "friendly"] },
            { "temperature", ["0", "0.5", "0.7", "1.0"] },
            { "resourceId", ["1", "2", "3", "4", "5"] }
        };

        if (ctx.Params is not { } @params) {
            throw new NotSupportedException($"Params are required.");
        }

        var @ref = @params.Ref;
        var argument = @params.Argument;

        if (@ref is ResourceTemplateReference rtr) {
            var resourceId = rtr.Uri?.Split("/").Last();

            if (resourceId is null) {
                return new CompleteResult();
            }

            var values = exampleCompletions["resourceId"].Where(id => id.StartsWith(argument.Value));

            return new CompleteResult {
                Completion = new Completion { Values = [.. values], HasMore = false, Total = values.Count() }
            };
        }

        if (@ref is PromptReference pr) {
            if (!exampleCompletions.TryGetValue(argument.Name, out IEnumerable<string>? value)) {
                throw new NotSupportedException($"Unknown argument name: {argument.Name}");
            }

            var values = value.Where(value => value.StartsWith(argument.Value));
            return new CompleteResult {
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
