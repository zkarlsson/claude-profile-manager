using Microsoft.Extensions.Logging;
using ClaudeProfileManager.Core.Logging;
using ClaudeProfileManager.Core.Security;
using ClaudeProfileManager.Core.Validation;

namespace ClaudeProfileManager.Core.Services;

/// <summary>
/// Provides standardized error handling across the application
/// </summary>
public static class StandardizedErrorHandler
{
    /// <summary>
    /// Handles exceptions with standardized logging and sanitization
    /// </summary>
    /// <typeparam name="T">The logger type</typeparam>
    /// <param name="logger">The logger instance</param>
    /// <param name="exception">The exception to handle</param>
    /// <param name="operation">The operation that failed</param>
    /// <param name="context">Additional context parameters</param>
    /// <returns>False for boolean operations</returns>
    public static bool HandleException<T>(ILogger<T> logger, Exception exception, string operation, params object[] context)
    {
        var sanitizedException = ExceptionSanitizer.Sanitize(exception);
        
        // Log with structured context
        using var logScope = logger.BeginScope("Operation: {Operation}", operation);
        logger.OperationFailed(sanitizedException.ExceptionType, sanitizedException.Message, string.Join(", ", context));
        
        return false;
    }
    
    /// <summary>
    /// Handles exceptions for operations that return nullable results
    /// </summary>
    /// <typeparam name="T">The logger type</typeparam>
    /// <typeparam name="TResult">The result type</typeparam>
    /// <param name="logger">The logger instance</param>
    /// <param name="exception">The exception to handle</param>
    /// <param name="operation">The operation that failed</param>
    /// <param name="context">Additional context parameters</param>
    /// <returns>Null for nullable operations</returns>
    public static TResult? HandleException<T, TResult>(ILogger<T> logger, Exception exception, string operation, params object[] context)
        where TResult : class
    {
        var sanitizedException = ExceptionSanitizer.Sanitize(exception);
        
        using var logScope = logger.BeginScope("Operation: {Operation}", operation);
        logger.OperationFailed(sanitizedException.ExceptionType, sanitizedException.Message, string.Join(", ", context));
        
        return null;
    }
    
    /// <summary>
    /// Logs validation errors with standardized format
    /// </summary>
    /// <typeparam name="T">The logger type</typeparam>
    /// <param name="logger">The logger instance</param>
    /// <param name="operation">The operation being validated</param>
    /// <param name="validationError">The validation error message</param>
    /// <param name="input">The invalid input (sanitized)</param>
    public static void LogValidationError<T>(ILogger<T> logger, string operation, string? validationError, string? input = null)
    {
        var sanitizedInput = input != null ? InputValidator.SanitizeForLogging(input) : "null";
        logger.ValidationFailed(operation, validationError, sanitizedInput);
    }
    
    /// <summary>
    /// Creates a standardized scope for operations
    /// </summary>
    /// <typeparam name="T">The logger type</typeparam>
    /// <param name="logger">The logger instance</param>
    /// <param name="operation">The operation name</param>
    /// <param name="context">Additional context parameters</param>
    /// <returns>A disposable scope</returns>
    public static IDisposable? CreateOperationScope<T>(ILogger<T> logger, string operation, params object[] context)
    {
        var contextString = context.Length > 0 ? string.Join(", ", context) : "none";
        return logger.BeginScope("Operation: {Operation}, Context: {Context}", operation, contextString);
    }
    
    /// <summary>
    /// Handles Win32 API errors with standardized logging
    /// </summary>
    /// <typeparam name="T">The logger type</typeparam>
    /// <param name="logger">The logger instance</param>
    /// <param name="operation">The Win32 operation that failed</param>
    /// <param name="errorCode">The Win32 error code</param>
    /// <param name="context">Additional context parameters</param>
    public static void HandleWin32Error<T>(ILogger<T> logger, string operation, int errorCode, params object[] context)
    {
        var contextString = context.Length > 0 ? string.Join(", ", context) : "none";
        logger.Win32OperationFailed(operation, errorCode, contextString);
    }
}