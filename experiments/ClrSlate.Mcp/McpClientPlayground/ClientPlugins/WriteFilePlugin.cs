using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McpClientPlayground.ClientPlugins;
public class WriteFilePlugin
{
    private const string TargetDirectory = @"C:\Users\ankit\Chanagble\JobSpec\PublicAvailable\New folder\clrswarm\experiments\ClrSlate.Mcp\McpClientPlayground\GeneratedWorkflows";

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

