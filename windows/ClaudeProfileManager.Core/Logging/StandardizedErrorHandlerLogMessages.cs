using Microsoft.Extensions.Logging;

namespace ClaudeProfileManager.Core.Logging;

/// <summary>
/// High-performance, source-generated logging messages for StandardizedErrorHandler.
/// Uses LoggerMessage source generator for optimal performance and type safety.
/// </summary>
internal static partial class StandardizedErrorHandlerLogMessages
{
    // Error level messages
    [LoggerMessage(
        EventId = 5001,
        Level = LogLevel.Error,
        Message = "Operation failed: {ExceptionType} - {Message}. Context: {Context}")]
    public static partial void OperationFailed(this ILogger logger, string exceptionType, string message, string context);

    [LoggerMessage(
        EventId = 5002,
        Level = LogLevel.Error,
        Message = "Win32 operation failed: {Operation}. Error code: {ErrorCode}. Context: {Context}")]
    public static partial void Win32OperationFailed(this ILogger logger, string operation, int errorCode, string context);

    // Warning level messages
    [LoggerMessage(
        EventId = 5101,
        Level = LogLevel.Warning,
        Message = "Validation failed for {Operation}: {ValidationError}. Input: {Input}")]
    public static partial void ValidationFailed(this ILogger logger, string operation, string? validationError, string? input);
}