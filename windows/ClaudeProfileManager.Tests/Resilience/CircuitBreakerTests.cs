using ClaudeProfileManager.Core.Resilience;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ClaudeProfileManager.Tests.Resilience;

public class CircuitBreakerTests
{
    private readonly ILogger<CircuitBreakerTests> _logger;
    private readonly CircuitBreakerConfiguration _testConfig;

    public CircuitBreakerTests()
    {
        _logger = LoggerFactory.Create(builder => { }).CreateLogger<CircuitBreakerTests>();
        _testConfig = new CircuitBreakerConfiguration
        {
            FailureThreshold = 2,
            RecoveryTimeout = TimeSpan.FromMilliseconds(100),
            SuccessThreshold = 2,
            HalfOpenMaxConcurrency = 1
        };
    }

    [Fact]
    public void CircuitBreaker_InitialState_ShouldBeClosed()
    {
        // Arrange & Act
        var circuitBreaker = new CircuitBreaker<bool>("test", _testConfig, _logger);

        // Assert
        circuitBreaker.State.Should().Be(CircuitBreakerState.Closed);
    }

    [Fact]
    public async Task Execute_WithSuccessfulOperation_ShouldReturnResult()
    {
        // Arrange
        var circuitBreaker = new CircuitBreaker<string>("test", _testConfig, _logger);
        const string expectedResult = "success";

        // Act
        var result = await circuitBreaker.ExecuteAsync(() => Task.FromResult(expectedResult));

        // Assert
        result.Should().Be(expectedResult);
        circuitBreaker.State.Should().Be(CircuitBreakerState.Closed);
    }

    [Fact]
    public async Task Execute_WithFailures_ShouldOpenCircuit()
    {
        // Arrange
        var circuitBreaker = new CircuitBreaker<bool>("test", _testConfig, _logger);

        // Act - Cause failures to exceed threshold
        for (int i = 0; i < _testConfig.FailureThreshold; i++)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => circuitBreaker.ExecuteAsync(() => throw new InvalidOperationException("Test failure")));
        }

        // Assert
        circuitBreaker.State.Should().Be(CircuitBreakerState.Open);
    }

    [Fact]
    public async Task Execute_WhenCircuitOpen_ShouldThrowCircuitBreakerException()
    {
        // Arrange
        var circuitBreaker = new CircuitBreaker<bool>("test", _testConfig, _logger);

        // Cause circuit to open
        for (int i = 0; i < _testConfig.FailureThreshold; i++)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => circuitBreaker.ExecuteAsync(() => throw new InvalidOperationException("Test failure")));
        }

        // Act & Assert
        var exception = await Assert.ThrowsAsync<CircuitBreakerOpenException>(
            () => circuitBreaker.ExecuteAsync(() => Task.FromResult(true)));
        
        exception.CircuitBreakerName.Should().Be("test");
        exception.NextAttemptTime.Should().HaveValue();
    }

    [Fact]
    public async Task Execute_AfterRecoveryTimeout_ShouldTransitionToHalfOpen()
    {
        // Arrange
        var circuitBreaker = new CircuitBreaker<bool>("test", _testConfig, _logger);

        // Cause circuit to open
        for (int i = 0; i < _testConfig.FailureThreshold; i++)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => circuitBreaker.ExecuteAsync(() => throw new InvalidOperationException("Test failure")));
        }

        // Wait for recovery timeout
        await Task.Delay(_testConfig.RecoveryTimeout.Add(TimeSpan.FromMilliseconds(10)));

        // Act - This should transition to half-open
        var result = await circuitBreaker.ExecuteAsync(() => Task.FromResult(true));

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task Execute_InHalfOpenWithSuccesses_ShouldTransitionToClosed()
    {
        // Arrange
        var circuitBreaker = new CircuitBreaker<bool>("test", _testConfig, _logger);

        // Cause circuit to open
        for (int i = 0; i < _testConfig.FailureThreshold; i++)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => circuitBreaker.ExecuteAsync(() => throw new InvalidOperationException("Test failure")));
        }

        // Wait for recovery timeout
        await Task.Delay(_testConfig.RecoveryTimeout.Add(TimeSpan.FromMilliseconds(10)));

        // Act - Execute enough successful operations to close circuit
        for (int i = 0; i < _testConfig.SuccessThreshold; i++)
        {
            var result = await circuitBreaker.ExecuteAsync(() => Task.FromResult(true));
            result.Should().BeTrue();
        }

        // Assert
        circuitBreaker.State.Should().Be(CircuitBreakerState.Closed);
    }

    [Fact]
    public async Task Reset_ShouldSetStateToClosed()
    {
        // Arrange
        var circuitBreaker = new CircuitBreaker<bool>("test", _testConfig, _logger);

        // Cause some failures (but not enough to open)
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => circuitBreaker.ExecuteAsync(() => throw new InvalidOperationException("Test failure")));

        // Act
        circuitBreaker.Reset();

        // Assert
        circuitBreaker.State.Should().Be(CircuitBreakerState.Closed);
        var metrics = circuitBreaker.GetMetrics();
        metrics.FailureCount.Should().Be(0);
        metrics.SuccessCount.Should().Be(0);
    }

    [Fact]
    public void GetMetrics_ShouldReturnCurrentMetrics()
    {
        // Arrange
        var circuitBreaker = new CircuitBreaker<bool>("test-metrics", _testConfig, _logger);

        // Act
        var metrics = circuitBreaker.GetMetrics();

        // Assert
        metrics.Should().NotBeNull();
        metrics.Name.Should().Be("test-metrics");
        metrics.State.Should().Be(CircuitBreakerState.Closed);
        metrics.FailureCount.Should().Be(0);
        metrics.SuccessCount.Should().Be(0);
    }
}