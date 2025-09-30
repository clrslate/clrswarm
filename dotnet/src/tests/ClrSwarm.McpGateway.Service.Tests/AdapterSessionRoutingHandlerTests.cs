using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using ClrSwarm.McpGateway.Service.Routing;
using ClrSwarm.McpGateway.Service.Session;
using Moq;

namespace ClrSwarm.McpGateway.Service.Tests;

[TestFixture]
public class AdapterSessionRoutingHandlerTests
{
    private Mock<IServiceNodeInfoProvider> _serviceNodeInfoProviderMock = null!;
    private Mock<IAdapterSessionStore> _sessionStoreMock = null!;
    private AdapterSessionRoutingHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _serviceNodeInfoProviderMock = new Mock<IServiceNodeInfoProvider>();
        _sessionStoreMock = new Mock<IAdapterSessionStore>();
        _handler = new AdapterSessionRoutingHandler(_serviceNodeInfoProviderMock.Object, _sessionStoreMock.Object, NullLogger<AdapterSessionRoutingHandler>.Instance);
    }

    [Test]
    public async Task GetNewSessionTargetAsync_ReturnsTargetAddress()
    {
        var adapterName = "adapter";
        var httpContext = new DefaultHttpContext();
        var cancellationToken = CancellationToken.None;
        var nodeAddresses = new Dictionary<string, string>
        {
            { "node1", "http://address1" },
            { "node2", "http://address2" }
        };

        _serviceNodeInfoProviderMock
            .Setup(x => x.GetNodeAddressesAsync(adapterName, cancellationToken))
            .ReturnsAsync(nodeAddresses);

        var result = await _handler.GetNewSessionTargetAsync(adapterName, httpContext, cancellationToken);

        nodeAddresses.Values.Should().Contain(result);
    }

    [Test]
    public async Task GetExistingSessionTargetAsync_WithValidSessionId_ReturnsTargetAddress()
    {
        var httpContext = new DefaultHttpContext();
        var sessionId = "abc123";
        var cancellationToken = CancellationToken.None;
        httpContext.Request.QueryString = new QueryString("?session_id=" + sessionId);

        _sessionStoreMock
            .Setup(x => x.TryGetAsync(sessionId, cancellationToken))
            .ReturnsAsync(("http://target", true));

        var result = await _handler.GetExistingSessionTargetAsync(httpContext, cancellationToken);

        result.Should().Be("http://target");
    }

    [Test]
    public async Task GetExistingSessionTargetAsync_WithMissingSessionId_ThrowsArgumentException()
    {
        var httpContext = new DefaultHttpContext();
        var cancellationToken = CancellationToken.None;

        Func<Task> act = () => _handler.GetExistingSessionTargetAsync(httpContext, cancellationToken);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Session id not found in the request.");
    }

    [Test]
    public async Task GetExistingSessionTargetAsync_WithInvalidSessionId_ThrowsArgumentException()
    {
        var httpContext = new DefaultHttpContext();
        var sessionId = "invalid";
        var cancellationToken = CancellationToken.None;
        httpContext.Request.QueryString = new QueryString("?session_id=" + sessionId);

        _sessionStoreMock
            .Setup(x => x.TryGetAsync(sessionId, cancellationToken))
            .ReturnsAsync((null, false));

        Func<Task> act = () => _handler.GetExistingSessionTargetAsync(httpContext, cancellationToken);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Session id is not valid, or has expired.");
    }
}
