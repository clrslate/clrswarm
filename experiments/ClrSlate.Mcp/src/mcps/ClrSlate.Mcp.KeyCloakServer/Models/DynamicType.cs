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

﻿using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace ClrSlate.Mcp.KeyCloakServer.Models;

public record ResourceWrapper
{
    [YamlMember(Order = 0)]
    [JsonPropertyName("apiVersion")]
    public string ApiVersion { get; set; } = default!;

    [YamlMember(Order = 1)]
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = default!;
}


public record ResourceMetadata
{
    public string Name { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    [JsonPropertyName("icon")] public string? Icon { get; set; }
    [JsonPropertyName("color")] public string? Color { get; set; }
    [JsonPropertyName("tags")] public string[]? Tags { get; set; } = [];
    [JsonPropertyName("labels")] public IDictionary<string, string> Labels { get; set; } = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
    [JsonPropertyName("annotations")] public IDictionary<string, string> Annotations { get; set; } = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

    public string? GetLabel(string key) => Labels.TryGetValue(key, out var value) ? value : null;
    public string GetLabelOrDefault(string key, string defaultValue) => Labels.TryGetValue(key, out var value) ? value : defaultValue;
}

public record ResourceWrapperWithMetadata<TMetadata> : ResourceWrapper
    where TMetadata : ResourceMetadata
{
    [YamlMember(Order = 2)]
    [JsonPropertyName("metadata")]
    public TMetadata Metadata { get; set; } = default!;

    public string GetIdentifier() => $"{Kind.ToLower()}.{Metadata.Name.ToLower()}";
}

public record ResourceWrapperWithMetadata : ResourceWrapper
{
    [YamlMember(Order = 2)]
    [JsonPropertyName("metadata")]
    public ResourceMetadata Metadata { get; set; } = default!;

    public string GetIdentifier() => $"{Kind.ToLower()}.{Metadata.Name.ToLower()}";
}

public record DynamicType : DynamicType<Dictionary<string, object>>
{
    public DynamicType() : this(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)) { }
    public DynamicType(Dictionary<string, object> specifications) : base() => Specifications = specifications;
}

public record DynamicType<TResource> : ResourceWrapperWithMetadata
{
    [YamlMember(Order = 3)]
    [JsonPropertyName("spec")]
    public TResource Specifications { get; set; } = default!;
}
public record ModelDefinition : DynamicType<ModelSpecification>
{
    public const string TypeName = nameof(ModelDefinition);
    public ModelDefinition()
    {
        ApiVersion = "core.clrslate.io";
        Kind = "ModelDefinition";
    }
}

public class ModelSpecification
{
    [JsonPropertyName("schema")] public InputSchema Schema { get; set; } = default!;
    [JsonPropertyName("relations")] public Dictionary<string, ModelRelation>? Relations { get; set; }
    [JsonPropertyName("mirrored")] public Dictionary<string, MirroredProperty>? MirroredProperty { get; set; }
}

public record InputSchema
{
    /// <summary>
    /// Defines the properties of an object type schema.
    /// Only applicable when Type is object. Maps property names to their specifications.
    /// </summary>
    public Dictionary<string, JsonSchemaPropertySpec>? Properties { get; init; }

    /// <summary>
    /// List of property names that are required in this schema.
    /// Only applicable when Type is object. These properties must be present in valid instances.
    /// </summary>
    public List<string>? Required { get; init; }
}
public class ModelRelation
{
    /// <summary>
    /// The type of the property.
    /// If not specified, defaults to String.
    /// </summary>
    [JsonPropertyName("type")] public string Type { get; set; } = "object";

    /// <summary>
    /// Provides a detailed explanation of the property's purpose and usage.
    /// Used for documentation and code generation purposes.
    /// </summary>
    [JsonPropertyName("description")] public string? Description { get; init; }

    /// <summary>
    /// A human-readable title for the property.
    /// Used in UI generation and documentation to display a friendly name.
    /// </summary>
    [JsonPropertyName("title")] public string? Title { get; init; }


    /// <summary>
    /// Indicates whether this property is required in the parent schema.
    /// When true, the property must be present in valid instances.
    /// </summary>
    [JsonPropertyName("required")] public bool? Required { get; init; }

    /// <summary>
    /// Specifies the format of the value (e.g., date-time, email, uri).
    /// Used for additional validation beyond the basic type checking.
    /// See SchemaFormats class for available format constants.
    /// </summary>
    [JsonPropertyName("format")] public string? Format { get; init; }

    [JsonPropertyName("many")] public bool Many { get; set; }

    /// <summary>
    /// Defines a fixed set of allowed values for the property.
    /// </summary>
    [JsonPropertyName("specifications")] public ModelRelationSpecification Specifications { get; init; } = new();
}
public class MirroredProperty
{
    /// <summary>
    /// The type of the property.
    /// If not specified, defaults to String.
    /// </summary>
    [JsonPropertyName("type")] public string Type { get; set; } = "object";

    /// <summary>
    /// Provides a detailed explanation of the property's purpose and usage.
    /// Used for documentation and code generation purposes.
    /// </summary>
    [JsonPropertyName("description")] public string? Description { get; init; }

    /// <summary>
    /// A human-readable title for the property.
    /// Used in UI generation and documentation to display a friendly name.
    /// </summary>
    [JsonPropertyName("title")] public string? Title { get; init; }

    /// <summary>
    /// Specifies the format of the value (e.g., date-time, email, uri).
    /// Used for additional validation beyond the basic type checking.
    /// See SchemaFormats class for available format constants.
    /// </summary>
    [JsonPropertyName("format")] public string? Format { get; init; }

    /// <summary>
    /// Mirrored property value template.
    /// </summary>
    [JsonPropertyName("value")] public string? Value { get; set; }

    public Dictionary<string, object> Specifications { get; init; } = [];


    private static readonly string[] ResourceFormats = ["resource", "secret"];
    public bool IsResource() => Type == "object" && ResourceFormats.Contains(Format);
}

public class ModelRelationSpecification
{
    /// <summary>
    /// The type of the property.
    /// If not specified, defaults to String.
    /// </summary>
    [JsonPropertyName("type")] public string? Type { get; set; }
}

public record JsonSchemaPropertySpec
{
    /// <summary>
    /// The type of the property.
    /// If not specified, defaults to String.
    /// </summary>
    public string Type { get; set; } = "string";

    /// <summary>
    /// Provides a detailed explanation of the property's purpose and usage.
    /// Used for documentation and code generation purposes.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// A human-readable title for the property.
    /// Used in UI generation and documentation to display a friendly name.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Specifies the default value for the property if none is provided.
    /// The type of the default value must match the specified Type property.
    /// </summary>
    public object? Default { get; init; }

    /// <summary>
    /// Indicates whether this property is required in the parent schema.
    /// When true, the property must be present in valid instances.
    /// </summary>
    public bool? Required { get; init; }

    /// <summary>
    /// Specifies the format of the value (e.g., date-time, email, uri).
    /// Used for additional validation beyond the basic type checking.
    /// See SchemaFormats class for available format constants.
    /// </summary>
    public string? Format { get; init; }

    /// <summary>
    /// Defines a fixed set of allowed values for the property.
    /// The property value must be one of these enumerated values to be valid.
    /// </summary>
    public string[]? Enum { get; init; }

    /// <summary>
    /// Regular expression pattern that the property's string value must match.
    /// Only applicable when Type is string.
    /// </summary>
    public string? Pattern { get; init; }

    /// <summary>
    /// Minimum length for string values.
    /// Only applicable when Type is string.
    /// </summary>
    public int? MinLength { get; init; }

    /// <summary>
    /// Maximum length for string values.
    /// Only applicable when Type is string.
    /// </summary>
    public int? MaxLength { get; init; }

    /// <summary>
    /// Minimum value for numeric properties.
    /// Only applicable when Type is number or integer.
    /// </summary>
    public decimal? Minimum { get; init; }

    /// <summary>
    /// Maximum value for numeric properties.
    /// Only applicable when Type is number or integer.
    /// </summary>
    public decimal? Maximum { get; init; }

    /// <summary>
    /// When true, the value must be greater than (not equal to) the minimum value.
    /// Only applicable when Type is number or integer and Minimum is set.
    /// </summary>
    public decimal? ExclusiveMinimum { get; init; }

    /// <summary>
    /// When true, the value must be less than (not equal to) the maximum value.
    /// Only applicable when Type is number or integer and Maximum is set.
    /// </summary>
    public decimal? ExclusiveMaximum { get; init; }

    /// <summary>
    /// Specifies that the value must be a multiple of this number.
    /// Only applicable when Type is number or integer.
    /// </summary>
    public decimal? MultipleOf { get; init; }

    /// <summary>
    /// Minimum number of items in an array.
    /// Only applicable when Type is array.
    /// </summary>
    public int? MinItems { get; init; }

    /// <summary>
    /// Maximum number of items in an array.
    /// Only applicable when Type is array.
    /// </summary>
    public int? MaxItems { get; init; }

    /// <summary>
    /// When true, all items in the array must be unique.
    /// Only applicable when Type is array.
    /// </summary>
    public bool? UniqueItems { get; init; }

    /// <summary>
    /// Defines the schema for items in an array.
    /// Only applicable when Type is array. Specifies the validation rules for array elements.
    /// </summary>
    public JsonSchemaPropertySpec? Items { get; init; }

    /// <summary>
    /// Controls whether additional properties are allowed in objects.
    /// Only applicable when Type is object. When false, no additional properties beyond those defined are allowed.
    /// </summary>
    public bool? AdditionalProperties { get; init; }

    public Dictionary<string, object> Specifications { get; init; } = [];

    private static readonly string[] ResourceFormats = ["resource", "secret"];
    public bool IsResource()
    {
        return Type == "object" && ResourceFormats.Contains(Format);
    }
}
