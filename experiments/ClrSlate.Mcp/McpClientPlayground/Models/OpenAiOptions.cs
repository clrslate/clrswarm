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

﻿namespace McpClientPlayground.Models;

public record OpenAiOptions
{
    public string ModelId { get; set; } = "azure/gpt-4.1";
    public string Endpoint { get; set; } = "https://litellm.beta.clrslate.app";
    public string ApiKey { get; set; }
}
