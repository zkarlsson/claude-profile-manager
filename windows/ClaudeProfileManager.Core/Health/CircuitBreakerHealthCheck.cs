using ClaudeProfileManager.Core.Resilience;
using Microsoft.Extensions.Logging;

namespace ClaudeProfileManager.Core.Health;

/// <summary>
/// Health check for monitoring circuit breaker states across the application.
/// Reports degraded status if any circuit breakers are open, and provides metrics.
/// </summary>
public class CircuitBreakerHealthCheck : IHealthCheck
{
    private readonly CircuitBreakerManager _circuitBreakerManager;
    private readonly ILogger<CircuitBreakerHealthCheck> _logger;

    public CircuitBreakerHealthCheck(
        CircuitBreakerManager circuitBreakerManager, 
        ILogger<CircuitBreakerHealthCheck> logger)
    {
        _circuitBreakerManager = circuitBreakerManager ?? throw new ArgumentNullException(nameof(circuitBreakerManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string Name => "circuit-breakers";

    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Checking circuit breaker health");
            
            var metrics = _circuitBreakerManager.GetAllMetrics();
            var data = new Dictionary<string, object>
            {
                ["CircuitBreakerCount"] = metrics.Count,
                ["CircuitBreakers"] = metrics.ToDictionary(kvp => kvp.Key, kvp => new
                {
                    kvp.Value.State,
                    kvp.Value.FailureCount,
                    kvp.Value.SuccessCount,
                    kvp.Value.LastFailureTime,
                    kvp.Value.HalfOpenConcurrentCalls
                })
            };

            if (metrics.Count == 0)
            {
                return Task.FromResult(HealthCheckResult.Healthy("No circuit breakers registered", data));
            }

            var openCircuitBreakers = metrics.Where(kvp => kvp.Value.State == CircuitBreakerState.Open).ToList();
            var halfOpenCircuitBreakers = metrics.Where(kvp => kvp.Value.State == CircuitBreakerState.HalfOpen).ToList();
            var closedCircuitBreakers = metrics.Where(kvp => kvp.Value.State == CircuitBreakerState.Closed).ToList();

            data["OpenCount"] = openCircuitBreakers.Count;
            data["HalfOpenCount"] = halfOpenCircuitBreakers.Count;
            data["ClosedCount"] = closedCircuitBreakers.Count;

            if (openCircuitBreakers.Count > 0)
            {
                var openNames = openCircuitBreakers.Select(kvp => kvp.Key).ToList();
                data["OpenCircuitBreakers"] = openNames;
                
                _logger.LogWarning("Found {Count} open circuit breakers: {Names}", 
                    openCircuitBreakers.Count, string.Join(", ", openNames));

                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"{openCircuitBreakers.Count} circuit breaker(s) are open: {string.Join(", ", openNames)}", 
                    data: data));
            }

            if (halfOpenCircuitBreakers.Count > 0)
            {
                var halfOpenNames = halfOpenCircuitBreakers.Select(kvp => kvp.Key).ToList();
                data["HalfOpenCircuitBreakers"] = halfOpenNames;
                
                _logger.LogInformation("Found {Count} half-open circuit breakers: {Names}", 
                    halfOpenCircuitBreakers.Count, string.Join(", ", halfOpenNames));

                return Task.FromResult(HealthCheckResult.Degraded(
                    $"{halfOpenCircuitBreakers.Count} circuit breaker(s) are half-open (testing recovery): {string.Join(", ", halfOpenNames)}", 
                    data: data));
            }

            // All circuit breakers are closed (healthy)
            var totalFailures = metrics.Values.Sum(m => m.FailureCount);
            var totalSuccesses = metrics.Values.Sum(m => m.SuccessCount);
            
            data["TotalFailures"] = totalFailures;
            data["TotalSuccesses"] = totalSuccesses;

            // Check for recent failures that might indicate issues
            var recentFailures = metrics.Values
                .Where(m => m.FailureCount > 0 && m.LastFailureTime > DateTime.MinValue && 
                           DateTime.UtcNow - m.LastFailureTime < TimeSpan.FromMinutes(5))
                .ToList();

            if (recentFailures.Count > 0)
            {
                data["RecentFailures"] = recentFailures.Count;
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"All circuit breakers are closed, but {recentFailures.Count} have recent failures", 
                    data: data));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                $"All {metrics.Count} circuit breaker(s) are healthy (closed with {totalSuccesses} total successes)", 
                data: data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Circuit breaker health check failed");
            
            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"Circuit breaker health check failed: {ex.Message}",
                ex,
                new Dictionary<string, object>()));
        }
    }
}