using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace ClaudeProfileManager.Core.Resilience;

/// <summary>
/// Manages multiple circuit breakers for different operations
/// </summary>
public class CircuitBreakerManager
{
    private readonly ConcurrentDictionary<string, object> _circuitBreakers = new();
    private readonly ILoggerFactory _loggerFactory;
    private readonly CircuitBreakerConfiguration _defaultConfiguration;

    public CircuitBreakerManager(ILoggerFactory loggerFactory, CircuitBreakerConfiguration? defaultConfiguration = null)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _defaultConfiguration = defaultConfiguration ?? new CircuitBreakerConfiguration();
    }

    /// <summary>
    /// Gets or creates a circuit breaker for the specified operation
    /// </summary>
    /// <typeparam name="T">The return type of the protected operation</typeparam>
    /// <param name="name">Unique name for the circuit breaker</param>
    /// <param name="configuration">Optional custom configuration</param>
    /// <returns>Circuit breaker instance</returns>
    public CircuitBreaker<T> GetCircuitBreaker<T>(string name, CircuitBreakerConfiguration? configuration = null)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Circuit breaker name cannot be null or empty", nameof(name));

        return (CircuitBreaker<T>)_circuitBreakers.GetOrAdd(name, _ =>
        {
            var config = configuration ?? _defaultConfiguration;
            var logger = _loggerFactory.CreateLogger<CircuitBreaker<T>>();
            return new CircuitBreaker<T>(name, config, logger);
        });
    }

    /// <summary>
    /// Gets metrics for all circuit breakers
    /// </summary>
    /// <returns>Dictionary of circuit breaker metrics</returns>
    public Dictionary<string, CircuitBreakerMetrics> GetAllMetrics()
    {
        var metrics = new Dictionary<string, CircuitBreakerMetrics>();

        foreach (var kvp in _circuitBreakers)
        {
            if (kvp.Value is CircuitBreaker<bool> boolBreaker)
            {
                metrics[kvp.Key] = boolBreaker.GetMetrics();
            }
            else if (kvp.Value is CircuitBreaker<string> stringBreaker)
            {
                metrics[kvp.Key] = stringBreaker.GetMetrics();
            }
            // Add more type checks as needed based on usage patterns
        }

        return metrics;
    }

    /// <summary>
    /// Resets all circuit breakers to closed state
    /// </summary>
    public void ResetAll()
    {
        foreach (var kvp in _circuitBreakers)
        {
            if (kvp.Value is CircuitBreaker<bool> boolBreaker)
            {
                boolBreaker.Reset();
            }
            else if (kvp.Value is CircuitBreaker<string> stringBreaker)
            {
                stringBreaker.Reset();
            }
            // Add more type checks as needed
        }
    }

    /// <summary>
    /// Removes a circuit breaker by name
    /// </summary>
    /// <param name="name">Name of the circuit breaker to remove</param>
    /// <returns>True if the circuit breaker was removed</returns>
    public bool RemoveCircuitBreaker(string name)
    {
        return _circuitBreakers.TryRemove(name, out _);
    }

    /// <summary>
    /// Gets the names of all registered circuit breakers
    /// </summary>
    /// <returns>List of circuit breaker names</returns>
    public IReadOnlyList<string> GetCircuitBreakerNames()
    {
        return _circuitBreakers.Keys.ToList();
    }
}