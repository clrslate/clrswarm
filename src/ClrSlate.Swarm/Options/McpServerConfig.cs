using System.ComponentModel.DataAnnotations;

namespace ClrSlate.Swarm.Options;

public class McpServerConfig
{
    [Required]
    public string Transport { get; set; } = string.Empty;

    // For stdio transport
    public string? Command { get; set; }
    public string[]? Args { get; set; }
    public Dictionary<string, string?>? Env { get; set; }

    // For SSE/HTTP transport
    public string? Endpoint { get; set; }
    public string? Name { get; set; }

    public bool IsStdioTransport => Transport.Equals("stdio", StringComparison.OrdinalIgnoreCase);
    public bool IsSseTransport => Transport.Equals("sse", StringComparison.OrdinalIgnoreCase);
}