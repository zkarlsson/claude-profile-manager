using ClaudeProfileManager.Core.Health;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ClaudeProfileManager.Tests.Health;

public class HealthCheckManagerTests
{
    private readonly ILogger<HealthCheckManager> _logger;
    private readonly HealthCheckManager _healthCheckManager;

    public HealthCheckManagerTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => { });
        _logger = loggerFactory.CreateLogger<HealthCheckManager>();
        _healthCheckManager = new HealthCheckManager(_logger);
    }

    [Fact]
    public void RegisterHealthCheck_WithValidHealthCheck_ShouldReturnTrue()
    {
        // Arrange
        var healthCheck = new TestHealthCheck("test-check", HealthStatus.Healthy);

        // Act
        var result = _healthCheckManager.RegisterHealthCheck(healthCheck);

        // Assert
        result.Should().BeTrue();
        _healthCheckManager.GetHealthCheckNames().Should().Contain("test-check");
    }

    [Fact]
    public void RegisterHealthCheck_WithDuplicateName_ShouldReturnFalse()
    {
        // Arrange
        var healthCheck1 = new TestHealthCheck("test-check", HealthStatus.Healthy);
        var healthCheck2 = new TestHealthCheck("test-check", HealthStatus.Healthy);
        _healthCheckManager.RegisterHealthCheck(healthCheck1);

        // Act
        var result = _healthCheckManager.RegisterHealthCheck(healthCheck2);

        // Assert
        result.Should().BeFalse();
        _healthCheckManager.GetHealthCheckNames().Should().HaveCount(1);
    }

    [Fact]
    public void UnregisterHealthCheck_WithExistingHealthCheck_ShouldReturnTrue()
    {
        // Arrange
        var healthCheck = new TestHealthCheck("test-check", HealthStatus.Healthy);
        _healthCheckManager.RegisterHealthCheck(healthCheck);

        // Act
        var result = _healthCheckManager.UnregisterHealthCheck("test-check");

        // Assert
        result.Should().BeTrue();
        _healthCheckManager.GetHealthCheckNames().Should().BeEmpty();
    }

    [Fact]
    public void UnregisterHealthCheck_WithNonExistentHealthCheck_ShouldReturnFalse()
    {
        // Act
        var result = _healthCheckManager.UnregisterHealthCheck("non-existent");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CheckHealthAsync_WithExistingHealthyCheck_ShouldReturnHealthyResult()
    {
        // Arrange
        var healthCheck = new TestHealthCheck("test-check", HealthStatus.Healthy, "All systems go");
        _healthCheckManager.RegisterHealthCheck(healthCheck);

        // Act
        var result = await _healthCheckManager.CheckHealthAsync("test-check");

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Be("All systems go");
        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task CheckHealthAsync_WithNonExistentHealthCheck_ShouldReturnUnhealthyResult()
    {
        // Act
        var result = await _healthCheckManager.CheckHealthAsync("non-existent");

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("not found");
    }

    [Fact]
    public async Task CheckHealthAsync_WithExceptionThrowingHealthCheck_ShouldReturnUnhealthyResult()
    {
        // Arrange
        var healthCheck = new TestHealthCheck("failing-check", exception: new InvalidOperationException("Test failure"));
        _healthCheckManager.RegisterHealthCheck(healthCheck);

        // Act
        var result = await _healthCheckManager.CheckHealthAsync("failing-check");

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Exception.Should().Be("Test failure");
        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task CheckAllHealthAsync_WithMultipleHealthChecks_ShouldReturnAllResults()
    {
        // Arrange
        var healthyCheck = new TestHealthCheck("healthy-check", HealthStatus.Healthy);
        var degradedCheck = new TestHealthCheck("degraded-check", HealthStatus.Degraded);
        var unhealthyCheck = new TestHealthCheck("unhealthy-check", HealthStatus.Unhealthy);
        
        _healthCheckManager.RegisterHealthCheck(healthyCheck);
        _healthCheckManager.RegisterHealthCheck(degradedCheck);
        _healthCheckManager.RegisterHealthCheck(unhealthyCheck);

        // Act
        var results = await _healthCheckManager.CheckAllHealthAsync();

        // Assert
        results.Should().HaveCount(3);
        results["healthy-check"].Status.Should().Be(HealthStatus.Healthy);
        results["degraded-check"].Status.Should().Be(HealthStatus.Degraded);
        results["unhealthy-check"].Status.Should().Be(HealthStatus.Unhealthy);
        
        // All results should have timing information
        results.Values.Should().AllSatisfy(r => r.Duration.Should().BeGreaterThan(TimeSpan.Zero));
    }

    [Fact]
    public async Task CheckAllHealthAsync_WithNoHealthChecks_ShouldReturnEmptyResults()
    {
        // Act
        var results = await _healthCheckManager.CheckAllHealthAsync();

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetOverallHealthStatusAsync_WithAllHealthyChecks_ShouldReturnHealthy()
    {
        // Arrange
        var healthCheck1 = new TestHealthCheck("check1", HealthStatus.Healthy);
        var healthCheck2 = new TestHealthCheck("check2", HealthStatus.Healthy);
        _healthCheckManager.RegisterHealthCheck(healthCheck1);
        _healthCheckManager.RegisterHealthCheck(healthCheck2);

        // Act
        var status = await _healthCheckManager.GetOverallHealthStatusAsync();

        // Assert
        status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task GetOverallHealthStatusAsync_WithAnyUnhealthyCheck_ShouldReturnUnhealthy()
    {
        // Arrange
        var healthyCheck = new TestHealthCheck("healthy", HealthStatus.Healthy);
        var unhealthyCheck = new TestHealthCheck("unhealthy", HealthStatus.Unhealthy);
        _healthCheckManager.RegisterHealthCheck(healthyCheck);
        _healthCheckManager.RegisterHealthCheck(unhealthyCheck);

        // Act
        var status = await _healthCheckManager.GetOverallHealthStatusAsync();

        // Assert
        status.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task GetOverallHealthStatusAsync_WithAnyDegradedCheck_ShouldReturnDegraded()
    {
        // Arrange
        var healthyCheck = new TestHealthCheck("healthy", HealthStatus.Healthy);
        var degradedCheck = new TestHealthCheck("degraded", HealthStatus.Degraded);
        _healthCheckManager.RegisterHealthCheck(healthyCheck);
        _healthCheckManager.RegisterHealthCheck(degradedCheck);

        // Act
        var status = await _healthCheckManager.GetOverallHealthStatusAsync();

        // Assert
        status.Should().Be(HealthStatus.Degraded);
    }

    [Fact]
    public async Task GetOverallHealthStatusAsync_WithNoHealthChecks_ShouldReturnUnhealthy()
    {
        // Act
        var status = await _healthCheckManager.GetOverallHealthStatusAsync();

        // Assert
        status.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WithSlowHealthCheck_ShouldCompleteWithinTimeout()
    {
        // Arrange
        var slowHealthCheck = new TestHealthCheck("slow-check", HealthStatus.Healthy, delay: TimeSpan.FromMilliseconds(100));
        _healthCheckManager.RegisterHealthCheck(slowHealthCheck);

        // Act
        var result = await _healthCheckManager.CheckHealthAsync("slow-check");

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Duration.Should().BeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(90)); // Allow some tolerance
    }
}

/// <summary>
/// Test implementation of IHealthCheck for unit testing.
/// </summary>
internal sealed class TestHealthCheck : IHealthCheck
{
    private readonly HealthStatus _status;
    private readonly string? _description;
    private readonly Exception? _exception;
    private readonly TimeSpan _delay;

    public TestHealthCheck(string name, HealthStatus status = HealthStatus.Healthy, string? description = null, 
        Exception? exception = null, TimeSpan delay = default)
    {
        Name = name;
        _status = status;
        _description = description;
        _exception = exception;
        _delay = delay;
    }

    public string Name { get; }

    public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        if (_delay > TimeSpan.Zero)
        {
            await Task.Delay(_delay, cancellationToken);
        }

        if (_exception != null)
        {
            throw _exception;
        }

        return _status switch
        {
            HealthStatus.Healthy => HealthCheckResult.Healthy(_description),
            HealthStatus.Degraded => HealthCheckResult.Degraded(_description),
            HealthStatus.Unhealthy => HealthCheckResult.Unhealthy(_description),
            _ => HealthCheckResult.Unhealthy("Unknown status")
        };
    }
}