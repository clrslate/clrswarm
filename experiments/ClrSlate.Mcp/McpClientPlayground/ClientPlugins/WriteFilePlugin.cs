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



using Microsoft.SemanticKernel;
using System.ComponentModel;


namespace McpClientPlayground.ClientPlugins;
public class WriteFilePlugin
{
    private const string TargetDirectory = @"<absolute-path-to-store-workflows>";
    // eg. targetDirectory =@"C:\Users\GeneratedWorkflows";
    [KernelFunction]
    [Description("Writes content to a fixed directory. The filename must be provided.")]
    public string WriteFile(
        [Description("The content to write to the file.")] string content,
        [Description("The name of the file to write (e.g., output.txt).")] string fileName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be empty.");

            // Ensure directory exists
            Directory.CreateDirectory(TargetDirectory);

            var filePath = Path.Combine(TargetDirectory, fileName);

            File.WriteAllText(filePath, content);

            return $"✅ File successfully written to: {filePath}";
        }
        catch (Exception ex)
        {
            return $"❌ Error writing file: {ex.Message}";
        }
    }
}

