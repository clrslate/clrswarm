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

﻿using ClrSlate.Mcp.KeyCloakServer.Tools;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace ClrSlate.Mcp.KeyCloakServer.Prompts;

[McpServerPromptType]
public class ComplexPromptType
{
    [McpServerPrompt(Name = "complex_prompt"), Description("A prompt with arguments")]
    public static IEnumerable<ChatMessage> ComplexPrompt(
        [Description("Temperature setting")] int temperature,
        [Description("Output style")] string? style = null)
    {
        return [
            new ChatMessage(ChatRole.User,$"This is a complex prompt with arguments: temperature={temperature}, style={style}"),
            new ChatMessage(ChatRole.Assistant, "I understand. You've provided a complex prompt with temperature and style arguments. How would you like me to proceed?"),
            new ChatMessage(ChatRole.User, [new DataContent(TinyImageTool.MCP_TINY_IMAGE)])
        ];
    }
}
