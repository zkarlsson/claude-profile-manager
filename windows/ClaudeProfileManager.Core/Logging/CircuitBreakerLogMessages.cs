using Microsoft.Extensions.Logging;

namespace ClaudeProfileManager.Core.Logging;

/// <summary>
/// High-performance, source-generated logging messages for CircuitBreaker operations.
/// Uses LoggerMessage source generator for optimal performance and type safety.
/// </summary>
internal static partial class CircuitBreakerLogMessages
{
    // Information level messages
    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Information,
        Message = "Circuit breaker '{CircuitBreakerName}' manually reset")]
    public static partial void CircuitBreakerReset(this ILogger logger, string circuitBreakerName);

    [LoggerMessage(
        EventId = 3002,
        Level = LogLevel.Information,
        Message = "Circuit breaker '{CircuitBreakerName}' transitioning from Open to HalfOpen")]
    public static partial void CircuitBreakerTransitioningToHalfOpen(this ILogger logger, string circuitBreakerName);

    [LoggerMessage(
        EventId = 3003,
        Level = LogLevel.Information,
        Message = "Circuit breaker '{CircuitBreakerName}' closed after {SuccessCount} successful operations")]
    public static partial void CircuitBreakerClosed(this ILogger logger, string circuitBreakerName, int successCount);

    // Warning level messages
    [LoggerMessage(
        EventId = 3101,
        Level = LogLevel.Warning,
        Message = "Circuit breaker '{CircuitBreakerName}' opened after failure in HalfOpen state. Exception: {ExceptionType}")]
    public static partial void CircuitBreakerOpenedFromHalfOpen(this ILogger logger, string circuitBreakerName, string exceptionType);

    [LoggerMessage(
        EventId = 3102,
        Level = LogLevel.Warning,
        Message = "Circuit breaker '{CircuitBreakerName}' opened after {FailureCount} consecutive failures. Exception: {ExceptionType}")]
    public static partial void CircuitBreakerOpenedFromClosed(this ILogger logger, string circuitBreakerName, int failureCount, string exceptionType);

    // Debug level messages
    [LoggerMessage(
        EventId = 3201,
        Level = LogLevel.Debug,
        Message = "Circuit breaker '{CircuitBreakerName}' failure count reset after successful operation")]
    public static partial void CircuitBreakerFailureCountReset(this ILogger logger, string circuitBreakerName);

    [LoggerMessage(
        EventId = 3202,
        Level = LogLevel.Debug,
        Message = "Circuit breaker '{CircuitBreakerName}' failure count: {FailureCount}/{FailureThreshold}")]
    public static partial void CircuitBreakerFailureCount(this ILogger logger, string circuitBreakerName, int failureCount, int failureThreshold);
}