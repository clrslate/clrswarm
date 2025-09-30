using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using ClrSwarm.McpGateway.Service.Session;
using Moq;
using NUnit.Framework;

namespace ClrSwarm.McpGateway.Service.Tests;

[TestFixture]
public class SessionStoreTests : IDisposable
{
    private Mock<IDistributedCache> _distributedCacheMock = null!;
    private DistributedMemorySessionStore _sessionStore = null!;

    [SetUp]
    public void SetUp()
    {
        _distributedCacheMock = new Mock<IDistributedCache>();
        _sessionStore = new DistributedMemorySessionStore(_distributedCacheMock.Object, NullLogger<DistributedMemorySessionStore>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        _sessionStore.Dispose();
    }

    public void Dispose()
    {
        _sessionStore.Dispose();
    }

    [Test]
    public async Task GetAsync_ReturnsFromInMemoryCache_IfExists()
    {
        var sessionId = "s1";
        var target = "http://target";
        var cancellationToken = CancellationToken.None;

        await _sessionStore.SetAsync(sessionId, target, cancellationToken);

        var result = await _sessionStore.TryGetAsync(sessionId, cancellationToken);

        result.exists.Should().BeTrue();
        result.target.Should().Be(target);
    }

    [Test]
    public async Task GetAsync_ReturnsFromDistributedCache_IfNotInMemory()
    {
        var sessionId = "s2";
        var target = "http://target";
        var cancellationToken = CancellationToken.None;
        var sessionInfo = new SessionInfo(target, DateTime.UtcNow);
        var json = JsonSerializer.Serialize(sessionInfo);

        _distributedCacheMock
            .Setup(x => x.GetAsync(sessionId, cancellationToken))
            .ReturnsAsync(Encoding.UTF8.GetBytes(json));

        var result = await _sessionStore.TryGetAsync(sessionId, cancellationToken);

        result.exists.Should().BeTrue();
        result.target.Should().Be(target);
    }

    [Test]
    public async Task GetAsync_ReturnsFalse_IfNotInAnyCache()
    {
        var sessionId = "s3";
        var cancellationToken = CancellationToken.None;

        _distributedCacheMock
            .Setup(x => x.GetAsync(sessionId, cancellationToken))
            .Returns(Task.FromResult<byte[]?>(null));

        var (target, exists) = await _sessionStore.TryGetAsync(sessionId, cancellationToken);

        exists.Should().BeFalse();
        target.Should().BeNull();
    }

    [Test]
    public async Task SetAsync_StoresInBothCaches()
    {
        var sessionId = "s4";
        var target = "http://target";
        var cancellationToken = CancellationToken.None;

        await _sessionStore.SetAsync(sessionId, target, cancellationToken);

        var result = await _sessionStore.TryGetAsync(sessionId, cancellationToken);

        result.exists.Should().BeTrue();
        result.target.Should().Be(target);
    }

    [Test]
    public async Task RemoveAsync_RemovesFromBothCaches()
    {
        var sessionId = "s5";
        var target = "http://target";
        var cancellationToken = CancellationToken.None;

        await _sessionStore.SetAsync(sessionId, target, cancellationToken);
        await _sessionStore.RemoveAsync(sessionId, cancellationToken);

        _distributedCacheMock
            .Verify(x => x.RemoveAsync(sessionId, cancellationToken), Times.Once);

        var result = await _sessionStore.TryGetAsync(sessionId, cancellationToken);

        result.exists.Should().BeFalse();
        result.target.Should().BeNull();
    }
}
