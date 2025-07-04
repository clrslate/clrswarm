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

using ClrSlate.Swarm.Options;
using ClrSlate.Swarm.Abstractions;
using ClrSlate.Swarm.Services.McpToolServices;

namespace ClrSlate.Swarm.Services;

public class McpToolServiceFactory : IMcpToolServiceFactory
{
    public IMcpToolService Create(McpServerConfig config)
    {
        if (config.IsStdioTransport)
            return new StdioMcpToolService(config);
        if (config.Type.Equals("sse", StringComparison.OrdinalIgnoreCase))
            return new SseMcpToolService(config);
        if (config.Type.Equals("http", StringComparison.OrdinalIgnoreCase))
            return new StreamableHttpMcpToolService(config);
        throw new NotSupportedException($"Unsupported transport type: {config.Type}");
    }
}
