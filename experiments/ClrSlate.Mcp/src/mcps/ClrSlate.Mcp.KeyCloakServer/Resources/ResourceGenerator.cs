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

using ModelContextProtocol.Protocol;

namespace ClrSlate.Mcp.KeyCloakServer.Resources;

static class ResourceGenerator
{
    private static readonly List<Resource> _resources = Enumerable.Range(1, 100).Select(i =>
        {
            var uri = $"test://template/resource/{i}";
            if (i % 2 != 0)
            {
                return new Resource
                {
                    Uri = uri,
                    Name = $"Resource {i}",
                    MimeType = "text/plain",
                    Description = $"Resource {i}: This is a plaintext resource"
                };
            }
            else
            {
                var buffer = System.Text.Encoding.UTF8.GetBytes($"Resource {i}: This is a base64 blob");
                return new Resource
                {
                    Uri = uri,
                    Name = $"Resource {i}",
                    MimeType = "application/octet-stream",
                    Description = Convert.ToBase64String(buffer)
                };
            }
        }).ToList();

    public static IReadOnlyList<Resource> Resources => _resources;
}