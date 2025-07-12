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

namespace ClrSlate.Mcp.KeyCloakServer.Models;

public class EmbeddingRecord
{
    public string Id { get; set; } = string.Empty;
    public float[] Vector { get; set; } = Array.Empty<float>();
    public EmbeddingMetadata Metadata { get; set; } = new();
    public string Text { get; set; } = string.Empty;
}

public class EmbeddingMetadata
{
    public string EntityType { get; set; } = string.Empty; // "package" or "activity"
    public string OriginalId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ParentId { get; set; } // null for packages, populated for activities
    public List<string> Tags { get; set; } = new();
    public string? ActivityType { get; set; } // only for activities
    public string? Version { get; set; } // only for packages
    public DateTime IndexedAt { get; set; } = DateTime.UtcNow;
    public string SourceUrl { get; set; } = string.Empty;
}