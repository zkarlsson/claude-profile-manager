using Microsoft.Extensions.Logging;
using ClaudeProfileManager.Core.Logging;
using ClaudeProfileManager.Core.Services;

namespace ClaudeProfileManager.Core.Resilience;

/// <summary>
/// Generic circuit breaker implementation for protecting against cascading failures
/// </summary>
/// <typeparam name="T">The return type of the protected operations</typeparam>
public class CircuitBreaker<T>
{
    private readonly CircuitBreakerConfiguration _configuration;
    private readonly ILogger _logger;
    private readonly string _name;
    
    private CircuitBreakerState _state = CircuitBreakerState.Closed;
    private int _failureCount;
    private int _successCount;
    private DateTime _lastFailureTime = DateTime.MinValue;
    private int _halfOpenConcurrentCalls;
    private readonly object _lock = new();

    public CircuitBreaker(string name, CircuitBreakerConfiguration configuration, ILogger logger)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Current state of the circuit breaker
    /// </summary>
    public CircuitBreakerState State
    {
        get
        {
            lock (_lock)
            {
                return _state;
            }
        }
    }

    /// <summary>
    /// Executes an operation with circuit breaker protection
    /// </summary>
    /// <param name="operation">The operation to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the operation</returns>
    public async Task<T> ExecuteAsync(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        ValidateStateAndTransition();

        lock (_lock)
        {
            if (_state == CircuitBreakerState.Open)
            {
                var nextAttemptTime = _lastFailureTime.Add(_configuration.RecoveryTimeout);
                throw new CircuitBreakerOpenException($"Circuit breaker '{_name}' is open")
                {
                    CircuitBreakerName = _name,
                    NextAttemptTime = nextAttemptTime
                };
            }

            if (_state == CircuitBreakerState.HalfOpen)
            {
                if (_halfOpenConcurrentCalls >= _configuration.HalfOpenMaxConcurrency)
                {
                    throw new CircuitBreakerOpenException($"Circuit breaker '{_name}' is half-open and at maximum concurrency")
                    {
                        CircuitBreakerName = _name
                    };
                }
                _halfOpenConcurrentCalls++;
            }
        }

        try
        {
            using var scope = _logger.BeginScope("CircuitBreaker: {CircuitBreakerName}", _name);
            var result = await operation();
            
            OnSuccess();
            return result;
        }
        catch (Exception ex)
        {
            OnFailure(ex);
            throw;
        }
        finally
        {
            lock (_lock)
            {
                if (_state == CircuitBreakerState.HalfOpen)
                {
                    _halfOpenConcurrentCalls--;
                }
            }
        }
    }

    /// <summary>
    /// Executes an operation with circuit breaker protection (synchronous version)
    /// </summary>
    /// <param name="operation">The operation to execute</param>
    /// <returns>Result of the operation</returns>
    public T Execute(Func<T> operation)
    {
        return ExecuteAsync(() => Task.FromResult(operation())).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Gets circuit breaker metrics for monitoring
    /// </summary>
    public CircuitBreakerMetrics GetMetrics()
    {
        lock (_lock)
        {
            return new CircuitBreakerMetrics
            {
                Name = _name,
                State = _state,
                FailureCount = _failureCount,
                SuccessCount = _successCount,
                LastFailureTime = _lastFailureTime,
                HalfOpenConcurrentCalls = _halfOpenConcurrentCalls
            };
        }
    }

    /// <summary>
    /// Manually resets the circuit breaker to closed state (for testing/emergency)
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _state = CircuitBreakerState.Closed;
            _failureCount = 0;
            _successCount = 0;
            _halfOpenConcurrentCalls = 0;
            _lastFailureTime = DateTime.MinValue;
            
            _logger.CircuitBreakerReset(_name);
        }
    }

    private void ValidateStateAndTransition()
    {
        lock (_lock)
        {
            if (_state == CircuitBreakerState.Open)
            {
                if (DateTime.UtcNow >= _lastFailureTime.Add(_configuration.RecoveryTimeout))
                {
                    _state = CircuitBreakerState.HalfOpen;
                    _successCount = 0;
                    _halfOpenConcurrentCalls = 0;
                    
                    _logger.CircuitBreakerTransitioningToHalfOpen(_name);
                }
            }
        }
    }

    private void OnSuccess()
    {
        lock (_lock)
        {
            _successCount++;

            if (_state == CircuitBreakerState.HalfOpen)
            {
                if (_successCount >= _configuration.SuccessThreshold)
                {
                    _state = CircuitBreakerState.Closed;
                    _failureCount = 0;
                    _successCount = 0;
                    
                    _logger.CircuitBreakerClosed(_name, _successCount);
                }
            }
            else if (_state == CircuitBreakerState.Closed)
            {
                // Reset failure count on successful operation in closed state
                if (_failureCount > 0)
                {
                    _failureCount = 0;
                    _logger.CircuitBreakerFailureCountReset(_name);
                }
            }
        }
    }

    private void OnFailure(Exception exception)
    {
        lock (_lock)
        {
            _failureCount++;
            _lastFailureTime = DateTime.UtcNow;

            if (_state == CircuitBreakerState.HalfOpen)
            {
                // Any failure in half-open state immediately opens the circuit
                _state = CircuitBreakerState.Open;
                _successCount = 0;
                
                _logger.CircuitBreakerOpenedFromHalfOpen(_name, exception.GetType().Name);
            }
            else if (_state == CircuitBreakerState.Closed)
            {
                if (_failureCount >= _configuration.FailureThreshold)
                {
                    _state = CircuitBreakerState.Open;
                    
                    _logger.CircuitBreakerOpenedFromClosed(_name, _failureCount, exception.GetType().Name);
                }
                else
                {
                    _logger.CircuitBreakerFailureCount(_name, _failureCount, _configuration.FailureThreshold);
                }
            }
        }
    }
}

/// <summary>
/// Circuit breaker metrics for monitoring and diagnostics
/// </summary>
public class CircuitBreakerMetrics
{
    public string Name { get; init; } = string.Empty;
    public CircuitBreakerState State { get; init; }
    public int FailureCount { get; init; }
    public int SuccessCount { get; init; }
    public DateTime LastFailureTime { get; init; }
    public int HalfOpenConcurrentCalls { get; init; }
}