
namespace ClrSwarm.McpGateway.Management.Extensions;

public static class LoggingExtensions
{
    public static string? Sanitize(this object? logEntity) => logEntity?.ToString()?.Replace(Environment.NewLine, string.Empty).Replace("\t", string.Empty).Replace("\r", string.Empty);
}
