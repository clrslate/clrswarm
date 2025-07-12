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

﻿using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.System.Text.Json;

namespace ClrSlate.Mcp.KeyCloakServer;


public static partial class StringExtensions
{
    #region Yaml Extensions

    private static readonly IDeserializer _yamlDeSerializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .WithTypeConverter(new SystemTextJsonYamlTypeConverter())
        .WithTypeInspector(x => new SystemTextJsonTypeInspector(x))
        .IgnoreUnmatchedProperties()
        .Build();


    private static readonly ISerializer _yamlSerializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitDefaults | DefaultValuesHandling.OmitEmptyCollections)
            .WithTypeConverter(new SystemTextJsonYamlTypeConverter())
            .WithTypeInspector(x => new SystemTextJsonTypeInspector(x))
            .Build();

    public static string ToYaml<TValue>(this TValue value) => _yamlSerializer.Serialize(value);
    public static TValue FromYaml<TValue>(this string value)
    {
        try {
            return _yamlDeSerializer.Deserialize<TValue>(value);
        }
        catch (Exception ex) {
            return default;
        }
    }

    public static object? FromYaml(this string value) => _yamlDeSerializer.Deserialize(value);

    #endregion

    public static string ToBase64(this string value) => value.ToBytes().ToBase64String();
    public static string ToBase64String(this byte[] value) => Convert.ToBase64String(value);
    public static byte[] ToBytes(this string value) => Encoding.UTF8.GetBytes(value);
}

