using FluentAssertions;
using Microsoft.Extensions.Logging;
using ClrSwarm.McpGateway.Management.Contracts;
using ClrSwarm.McpGateway.Management.Deployment;
using ClrSwarm.McpGateway.Management.Service;
using Moq;

namespace ClrSwarm.McpGateway.Management.Tests;

[TestFixture]
public class AdapterRichResultProviderTests
{
    private Mock<IAdapterDeploymentManager> _deploymentManagerMock = null!;
    private Mock<ILogger<AdapterManagementService>> _loggerMock = null!;
    private AdapterRichResultProvider _provider = null!;

    [SetUp]
    public void SetUp()
    {
        _deploymentManagerMock = new Mock<IAdapterDeploymentManager>();
        _loggerMock = new Mock<ILogger<AdapterManagementService>>();
        _provider = new AdapterRichResultProvider(_deploymentManagerMock.Object, _loggerMock.Object);
    }

    [Test]
    public async Task GetAdapterLogsAsync_ShouldReturnLogs()
    {
        _deploymentManagerMock.Setup(x => x.GetDeploymentLogsAsync("adapter1", 0, It.IsAny<CancellationToken>())).ReturnsAsync("log-output");

        var result = await _provider.GetAdapterLogsAsync("adapter1", 0, CancellationToken.None);

        result.Should().Be("log-output");
    }

    [Test]
    public async Task GetAdapterStatusAsync_ShouldReturnStatus()
    {
        var status = new AdapterStatus { ReplicaStatus = "Healthy" };
        _deploymentManagerMock.Setup(x => x.GetDeploymentStatusAsync("adapter1", It.IsAny<CancellationToken>())).ReturnsAsync(status);

        var result = await _provider.GetAdapterStatusAsync("adapter1", CancellationToken.None);

        result.Should().Be(status);
    }
}
