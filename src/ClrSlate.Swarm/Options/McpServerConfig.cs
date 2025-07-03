namespace ClrSlate.Swarm.Options;

public class McpServerConfig
{
    private string? _type = null;

    public string Type 
    {
        get => _type ??= GetDefaultTypeValue(); 
        set => _type = value; 
    }

    // For stdio transport
    public string? Command { get; set; }
    public string[]? Args { get; set; }
    public Dictionary<string, string?>? Env { get; set; }

    // For SSE/HTTP transport
    public string? Url { get; set; }
    public Dictionary<string, string?>? Headers { get; set; }
    public string? Name { get; set; }

    // Default Type in the getter and setter
    private string GetDefaultTypeValue()
    {
        if (!string.IsNullOrWhiteSpace(Command))
            return "stdio";
        if (!string.IsNullOrWhiteSpace(Url))
            return "http";
        return string.Empty;
    }

    public bool IsStdioTransport => Type.Equals("stdio", StringComparison.OrdinalIgnoreCase);
    public bool IsSseOrHttpTransport => Type.Equals("sse", StringComparison.OrdinalIgnoreCase) || Type.Equals("http", StringComparison.OrdinalIgnoreCase);

    public bool IsValid => IsStdioTransport
        ? !string.IsNullOrEmpty(Command)
        : IsSseOrHttpTransport && !string.IsNullOrEmpty(Url) && Uri.IsWellFormedUriString(Url, UriKind.Absolute);
}