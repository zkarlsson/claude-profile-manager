namespace ClaudeProfileManager.Core.Resilience;

/// <summary>
/// Exception thrown when a circuit breaker is in the open state
/// </summary>
public class CircuitBreakerOpenException : Exception
{
    public CircuitBreakerOpenException() 
        : base("Circuit breaker is open. Operation not allowed.")
    {
    }
    
    public CircuitBreakerOpenException(string message) 
        : base(message)
    {
    }
    
    public CircuitBreakerOpenException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
    
    /// <summary>
    /// The name of the circuit breaker that is open
    /// </summary>
    public string? CircuitBreakerName { get; init; }
    
    /// <summary>
    /// When the circuit breaker will next attempt to transition to half-open
    /// </summary>
    public DateTime? NextAttemptTime { get; init; }
}