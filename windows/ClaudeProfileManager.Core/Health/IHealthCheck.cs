namespace ClaudeProfileManager.Core.Health;

/// <summary>
/// Represents the result of a health check operation.
/// </summary>
public class HealthCheckResult
{
    public HealthStatus Status { get; init; }
    public string? Description { get; init; }
    public string? Exception { get; init; }
    public Dictionary<string, object> Data { get; init; } = new();
    public TimeSpan Duration { get; init; }

    public static HealthCheckResult Healthy(string? description = null, Dictionary<string, object>? data = null) =>
        new() { Status = HealthStatus.Healthy, Description = description, Data = data ?? new() };

    public static HealthCheckResult Degraded(string? description = null, Dictionary<string, object>? data = null) =>
        new() { Status = HealthStatus.Degraded, Description = description, Data = data ?? new() };

    public static HealthCheckResult Unhealthy(string? description = null, Exception? exception = null, Dictionary<string, object>? data = null) =>
        new() 
        { 
            Status = HealthStatus.Unhealthy, 
            Description = description, 
            Exception = exception?.Message,
            Data = data ?? new() 
        };
}

/// <summary>
/// Represents the health status of a component.
/// </summary>
public enum HealthStatus
{
    /// <summary>
    /// The component is healthy and functioning normally.
    /// </summary>
    Healthy,
    
    /// <summary>
    /// The component is functioning but with reduced performance or capabilities.
    /// </summary>
    Degraded,
    
    /// <summary>
    /// The component is not functioning correctly.
    /// </summary>
    Unhealthy
}

/// <summary>
/// Interface for health check implementations.
/// </summary>
public interface IHealthCheck
{
    /// <summary>
    /// Gets the name of this health check.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Performs the health check asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health check result</returns>
    Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default);
}