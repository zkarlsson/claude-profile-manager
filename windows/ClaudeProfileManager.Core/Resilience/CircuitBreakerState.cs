namespace ClaudeProfileManager.Core.Resilience;

/// <summary>
/// Represents the state of a circuit breaker
/// </summary>
public enum CircuitBreakerState
{
    /// <summary>
    /// Circuit is closed - normal operations
    /// </summary>
    Closed,
    
    /// <summary>
    /// Circuit is open - failing fast, not allowing operations
    /// </summary>
    Open,
    
    /// <summary>
    /// Circuit is half-open - allowing limited operations to test if service has recovered
    /// </summary>
    HalfOpen
}

/// <summary>
/// Configuration for circuit breaker behavior
/// </summary>
public class CircuitBreakerConfiguration
{
    /// <summary>
    /// Number of consecutive failures before opening the circuit (default: 5)
    /// </summary>
    public int FailureThreshold { get; set; } = 5;
    
    /// <summary>
    /// Time to wait before attempting to close the circuit after it opens (default: 30 seconds)
    /// </summary>
    public TimeSpan RecoveryTimeout { get; set; } = TimeSpan.FromSeconds(30);
    
    /// <summary>
    /// Number of successful operations in half-open state needed to close the circuit (default: 3)
    /// </summary>
    public int SuccessThreshold { get; set; } = 3;
    
    /// <summary>
    /// Maximum number of concurrent operations allowed in half-open state (default: 1)
    /// </summary>
    public int HalfOpenMaxConcurrency { get; set; } = 1;
}