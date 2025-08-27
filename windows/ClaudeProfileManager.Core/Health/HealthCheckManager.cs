using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace ClaudeProfileManager.Core.Health;

/// <summary>
/// Manages and orchestrates health checks across the application.
/// </summary>
public class HealthCheckManager
{
    private readonly ConcurrentDictionary<string, IHealthCheck> _healthChecks = new();
    private readonly ILogger<HealthCheckManager> _logger;
    private readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(30);

    public HealthCheckManager(ILogger<HealthCheckManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Registers a health check.
    /// </summary>
    /// <param name="healthCheck">The health check to register</param>
    /// <returns>True if registered successfully, false if already exists</returns>
    public bool RegisterHealthCheck(IHealthCheck healthCheck)
    {
        ArgumentNullException.ThrowIfNull(healthCheck);
        
        var added = _healthChecks.TryAdd(healthCheck.Name, healthCheck);
        if (added)
        {
            _logger.LogInformation("Registered health check '{HealthCheckName}'", healthCheck.Name);
        }
        else
        {
            _logger.LogWarning("Health check '{HealthCheckName}' already registered", healthCheck.Name);
        }
        
        return added;
    }

    /// <summary>
    /// Unregisters a health check.
    /// </summary>
    /// <param name="name">Name of the health check to remove</param>
    /// <returns>True if removed, false if not found</returns>
    public bool UnregisterHealthCheck(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        
        var removed = _healthChecks.TryRemove(name, out _);
        if (removed)
        {
            _logger.LogInformation("Unregistered health check '{HealthCheckName}'", name);
        }
        
        return removed;
    }

    /// <summary>
    /// Gets the names of all registered health checks.
    /// </summary>
    public IReadOnlyList<string> GetHealthCheckNames()
    {
        return _healthChecks.Keys.ToList();
    }

    /// <summary>
    /// Executes a specific health check by name.
    /// </summary>
    /// <param name="name">Name of the health check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health check result with timing information</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (!_healthChecks.TryGetValue(name, out var healthCheck))
        {
            return HealthCheckResult.Unhealthy($"Health check '{name}' not found");
        }

        return await ExecuteHealthCheckAsync(healthCheck, cancellationToken);
    }

    /// <summary>
    /// Executes all registered health checks.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of health check results</returns>
    public async Task<Dictionary<string, HealthCheckResult>> CheckAllHealthAsync(CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, HealthCheckResult>();
        
        if (_healthChecks.IsEmpty)
        {
            _logger.LogWarning("No health checks registered");
            return results;
        }

        _logger.LogInformation("Executing {Count} health checks", _healthChecks.Count);

        var tasks = _healthChecks.Values.Select(async healthCheck =>
        {
            var result = await ExecuteHealthCheckAsync(healthCheck, cancellationToken);
            return new { Name = healthCheck.Name, Result = result };
        });

        var completedTasks = await Task.WhenAll(tasks);
        
        foreach (var task in completedTasks)
        {
            results[task.Name] = task.Result;
        }

        var healthyCount = results.Values.Count(r => r.Status == HealthStatus.Healthy);
        var degradedCount = results.Values.Count(r => r.Status == HealthStatus.Degraded);
        var unhealthyCount = results.Values.Count(r => r.Status == HealthStatus.Unhealthy);

        _logger.LogInformation(
            "Health check summary: {Healthy} healthy, {Degraded} degraded, {Unhealthy} unhealthy",
            healthyCount, degradedCount, unhealthyCount);

        return results;
    }

    /// <summary>
    /// Gets an overall health status based on all registered health checks.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Overall health status</returns>
    public async Task<HealthStatus> GetOverallHealthStatusAsync(CancellationToken cancellationToken = default)
    {
        var results = await CheckAllHealthAsync(cancellationToken);
        
        if (results.Count == 0)
        {
            return HealthStatus.Unhealthy; // No health checks is considered unhealthy
        }

        // If any health check is unhealthy, overall status is unhealthy
        if (results.Values.Any(r => r.Status == HealthStatus.Unhealthy))
        {
            return HealthStatus.Unhealthy;
        }

        // If any health check is degraded, overall status is degraded
        if (results.Values.Any(r => r.Status == HealthStatus.Degraded))
        {
            return HealthStatus.Degraded;
        }

        // All health checks are healthy
        return HealthStatus.Healthy;
    }

    private async Task<HealthCheckResult> ExecuteHealthCheckAsync(IHealthCheck healthCheck, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(_defaultTimeout);

            _logger.LogDebug("Executing health check '{HealthCheckName}'", healthCheck.Name);
            
            var result = await healthCheck.CheckHealthAsync(timeoutCts.Token);
            stopwatch.Stop();
            
            // Add duration to result
            var resultWithTiming = new HealthCheckResult
            {
                Status = result.Status,
                Description = result.Description,
                Exception = result.Exception,
                Data = new Dictionary<string, object>(result.Data),
                Duration = stopwatch.Elapsed
            };

            _logger.LogDebug(
                "Health check '{HealthCheckName}' completed: {Status} in {Duration}ms",
                healthCheck.Name, result.Status, stopwatch.ElapsedMilliseconds);

            return resultWithTiming;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                "Health check '{HealthCheckName}' was cancelled after {Duration}ms",
                healthCheck.Name, stopwatch.ElapsedMilliseconds);
                
            return HealthCheckResult.Unhealthy(
                "Health check was cancelled",
                data: new Dictionary<string, object> { ["Duration"] = stopwatch.Elapsed });
        }
        catch (OperationCanceledException) // Timeout
        {
            stopwatch.Stop();
            _logger.LogWarning(
                "Health check '{HealthCheckName}' timed out after {Duration}ms",
                healthCheck.Name, stopwatch.ElapsedMilliseconds);
                
            return HealthCheckResult.Unhealthy(
                $"Health check timed out after {_defaultTimeout.TotalSeconds}s",
                data: new Dictionary<string, object> { ["Duration"] = stopwatch.Elapsed, ["Timeout"] = _defaultTimeout });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Health check '{HealthCheckName}' threw an exception after {Duration}ms",
                healthCheck.Name, stopwatch.ElapsedMilliseconds);
                
            return HealthCheckResult.Unhealthy(
                $"Health check threw an exception: {ex.Message}",
                ex,
                new Dictionary<string, object> { ["Duration"] = stopwatch.Elapsed });
        }
    }
}