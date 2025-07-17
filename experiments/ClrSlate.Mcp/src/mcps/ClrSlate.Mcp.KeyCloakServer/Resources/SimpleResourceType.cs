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

﻿using ClrSlate.Mcp.KeyCloakServer.Models;
using ClrSlate.Mcp.KeyCloakServer.Tools;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using OpenTelemetry.Resources;
using System.ComponentModel;

namespace ClrSlate.Mcp.KeyCloakServer.Resources;

[McpServerResourceType]
public class SimpleResourceType
{
    [McpServerResource(Name = "direct_resource", UriTemplate = "test://direct-resources", Title = "Direct Text Resource", MimeType = "text/plain")]
    [Description("A direct text resource")]
    public static string DirectTextResource() => "This is a direct resource";

    [McpServerResource(Name = "resources", UriTemplate = "clrslate://resources", Title = "ClrSlate Resources", MimeType = "text/yaml")]
    [Description("A direct text resource")]
    public static IEnumerable<ResourceContents> GetClrSlateResource()
    {
        var realmModel = new ModelDefinition { 
            Metadata = new ResourceMetadata {
                Name = "keycloak.model.reaml",
                Title = "Keycloak Realm",
                Description = "Keycloak Realm"
            },
            Specifications = new ModelSpecification {
                Schema = new InputSchema {
                    Properties = new Dictionary<string, JsonSchemaPropertySpec> {
                        { "realmId", new JsonSchemaPropertySpec { Type = "string", Title = "Realm Id", Description = "The name of the Keycloak realm" } },
                        { "enabled", new JsonSchemaPropertySpec { Type = "boolean", Title = "Enabled", Description = "Whether the realm is enabled", Default = true } }
                    }
                }
            }
        };
        var clientModelDefinition = new ModelDefinition {
            Metadata = new ResourceMetadata {
                Name = "keycloak.model.client",
                Title = "Keycloak Client",
                Description = "Keycloak Client"
            },
            Specifications = new ModelSpecification {
                Schema = new InputSchema {
                    Properties = new Dictionary<string, JsonSchemaPropertySpec> {
                        { "clientId", new JsonSchemaPropertySpec { Type = "string", Title = "Client Id", Description = "Client Identifier in KeyCloak" } },
                        { "realm", new JsonSchemaPropertySpec { Type = "string", Title = "Parent Realm", Description = "Parent Realm for the client" } },
                    }
                }
            }
        };

        //yield return new UriContent($"clrslate://resources/realm", "application/yaml");
        //yield return new UriContent($"clrslate://resources/client", "application/yaml");

        yield return new TextResourceContents {
            Text = realmModel.ToYaml(),
            MimeType = "application/yaml",
            Uri = $"clrslate://resources/realm"
        };
        yield return new TextResourceContents {
            Text = clientModelDefinition.ToYaml(),
            MimeType = "application/yaml",
            Uri = $"clrslate://resources/client"
        };
    }

    [McpServerResource(Name = "template_resources", UriTemplate = "clrslate://resources/{id}", Title = "Template Resource")]
    [Description("A template resource with a numeric ID")]
    public static ResourceContents TemplateResource(RequestContext<ReadResourceRequestParams> requestContext, int id)
    {
        var index = id - 1;
        if ((uint)index >= ResourceGenerator.Resources.Count)
        {
            throw new NotSupportedException($"Unknown resource: {requestContext.Params?.Uri}");
        }

        var resource = ResourceGenerator.Resources[index];
        return resource.MimeType == "text/plain" ?
            new TextResourceContents
            {
                Text = resource.Description!,
                MimeType = resource.MimeType,
                Uri = resource.Uri,
            } :
            new BlobResourceContents
            {
                Blob = resource.Description!,
                MimeType = resource.MimeType,
                Uri = resource.Uri,
            };
    }
}