using ModelContextProtocol.Protocol;

public interface IMcpClientManager
{
    Task<IEnumerable<Tool>> GetAllToolsAsync(CancellationToken cancellationToken = default);
    Task<CallToolResult> CallToolAsync(string toolName, IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken = default);
    Task InitializeAsync();
    ValueTask DisposeAsync();
}
