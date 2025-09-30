
using ClrSwarm.McpGateway.Management.Contracts;
using ClrSwarm.McpGateway.Management.Deployment;
using Microsoft.Extensions.Logging;

namespace ClrSwarm.McpGateway.Management.Service;

public class AdapterRichResultProvider(IAdapterDeploymentManager deploymentManager, ILogger<AdapterManagementService> logger) : IAdapterRichResultProvider
{
    private readonly IAdapterDeploymentManager _deploymentManager = deploymentManager ?? throw new ArgumentNullException(nameof(deploymentManager));
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public Task<string> GetAdapterLogsAsync(string name, int instance, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Start to get deployment log for /adapters/{name}.", name);
        return _deploymentManager.GetDeploymentLogsAsync(name, instance, cancellationToken);
    }

    public Task<AdapterStatus> GetAdapterStatusAsync(string name, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Start to get deployment status for /adapters/{name}.", name);
        return _deploymentManager.GetDeploymentStatusAsync(name, cancellationToken);
    }
}
