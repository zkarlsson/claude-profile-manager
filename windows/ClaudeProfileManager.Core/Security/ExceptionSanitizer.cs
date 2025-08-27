using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ClaudeProfileManager.Core.Configuration;

namespace ClaudeProfileManager.Core.Security;

/// <summary>
/// Provides exception sanitization to prevent information disclosure through error messages
/// </summary>
public static partial class ExceptionSanitizer
{
    // Sensitive information patterns
    [GeneratedRegex(@"sk-ant-[A-Za-z0-9_-]+", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex ApiKeyPattern();

    [GeneratedRegex(@"Bearer\s+[A-Za-z0-9._-]+", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex BearerTokenPattern();

    [GeneratedRegex(@"""claudeAiOauth"":\s*\{[^}]*\}", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex OAuthTokenPattern();

    [GeneratedRegex(@"(password|pwd|secret|key|token|credential)[""'\s]*[:=][""'\s]*[^""'\s,}]+", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex CredentialPattern();

    // File path patterns
    [GeneratedRegex(@"C:\\Users\\[^\\]+", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex UserPathPattern();

    [GeneratedRegex(@"[A-Za-z]:\\(?!Users\\)[^\s""']+", RegexOptions.Compiled)]
    private static partial Regex WindowsPathPattern();

    [GeneratedRegex(@"/[^""'\s,}]*", RegexOptions.Compiled)]
    private static partial Regex UnixPathPattern();

    // Stack trace sensitive information
    [GeneratedRegex(@"at\s+[A-Za-z0-9_.]+\.[A-Za-z0-9_<>]+\([^)]*\)\s+in\s+[^\r\n]+", RegexOptions.Compiled)]
    private static partial Regex StackTraceFilePattern();

    /// <summary>
    /// Sanitizes an exception for safe logging and user display
    /// </summary>
    /// <param name="exception">The exception to sanitize</param>
    /// <param name="isDevelopment">Whether to preserve debugging information</param>
    /// <returns>A sanitized exception with sensitive information removed</returns>
    public static SanitizedExceptionData Sanitize(Exception exception, bool isDevelopment = false)
    {
        if (exception == null)
            return new SanitizedExceptionData("Unknown error occurred", "UnknownException", null);

        var sanitizedMessage = SanitizeMessage(exception.Message);
        var sanitizedStackTrace = isDevelopment ? SanitizeStackTrace(exception.StackTrace) : null;
        var sanitizedException = exception.GetType().Name;

        // Handle inner exceptions
        SanitizedExceptionData? innerException = null;
        if (exception.InnerException != null)
        {
            innerException = Sanitize(exception.InnerException, isDevelopment);
        }

        return new SanitizedExceptionData(sanitizedMessage, sanitizedException, sanitizedStackTrace, innerException);
    }

    /// <summary>
    /// Sanitizes an exception message for safe display
    /// </summary>
    public static string SanitizeMessage(string? message)
    {
        if (string.IsNullOrEmpty(message))
            return "An error occurred";

        var sanitized = message;

        // Remove credential information - specific patterns first, then generic
        sanitized = ApiKeyPattern().Replace(sanitized, "[REDACTED-API-KEY]");
        sanitized = BearerTokenPattern().Replace(sanitized, "[REDACTED-BEARER-TOKEN]");
        sanitized = OAuthTokenPattern().Replace(sanitized, "[REDACTED-OAUTH-TOKEN]");
        
        // Apply generic credential pattern last (excludes already-replaced text)
        sanitized = CredentialPattern().Replace(sanitized, match =>
        {
            // Skip if already redacted
            if (match.Value.Contains("[REDACTED"))
                return match.Value;
                
            var separatorIndex = Math.Max(match.Value.IndexOf(':', StringComparison.Ordinal), match.Value.IndexOf('=', StringComparison.Ordinal));
            if (separatorIndex >= 0)
            {
                var prefix = match.Value.Substring(0, separatorIndex + 1);
                return $"{prefix} [REDACTED]";
            }
            return "[REDACTED]";
        });

        // Sanitize file paths
        sanitized = UserPathPattern().Replace(sanitized, @"C:\Users\[USER]");
        sanitized = WindowsPathPattern().Replace(sanitized, "[WINDOWS-PATH]");
        sanitized = UnixPathPattern().Replace(sanitized, "[UNIX-PATH]");

        return sanitized;
    }

    /// <summary>
    /// Sanitizes a stack trace for development debugging while removing sensitive paths
    /// </summary>
    public static string? SanitizeStackTrace(string? stackTrace)
    {
        if (string.IsNullOrEmpty(stackTrace))
            return null;

        var sanitized = stackTrace;

        // Replace full file paths in stack traces with relative paths
        sanitized = StackTraceFilePattern().Replace(sanitized, match =>
        {
            var parts = match.Value.Split(" in ");
            if (parts.Length == 2)
            {
                var methodPart = parts[0];
                var filePath = parts[1];
                
                // Extract just the filename from the full path
                var fileName = Path.GetFileName(filePath);
                return $"{methodPart} in {fileName}";
            }
            return match.Value;
        });

        // Remove user-specific paths
        sanitized = UserPathPattern().Replace(sanitized, @"C:\Users\[USER]");

        return sanitized;
    }

    /// <summary>
    /// Gets a user-friendly error message based on exception type and context
    /// </summary>
    public static string GetUserFriendlyMessage(Exception exception, string context = "")
    {
        var contextPrefix = string.IsNullOrEmpty(context) ? "" : $"{context}: ";

        return exception switch
        {
            ArgumentNullException => $"{contextPrefix}Required information is missing",
            ArgumentException => $"{contextPrefix}Invalid input provided",
            UnauthorizedAccessException => $"{contextPrefix}Access denied. Please check permissions",
            FileNotFoundException => $"{contextPrefix}Required file not found",
            DirectoryNotFoundException => $"{contextPrefix}Required directory not found",
            IOException => $"{contextPrefix}File operation failed",
            InvalidOperationException => $"{contextPrefix}Operation cannot be completed in current state",
            TimeoutException => $"{contextPrefix}Operation timed out",
            NotSupportedException => $"{contextPrefix}Operation not supported",
            System.ComponentModel.Win32Exception => $"{contextPrefix}Windows system error occurred",
            _ => $"{contextPrefix}An unexpected error occurred"
        };
    }

    /// <summary>
    /// Creates a development-friendly error summary with sanitized details
    /// </summary>
    public static string CreateDevelopmentErrorSummary(Exception exception, string operation)
    {
        var sanitizedException = Sanitize(exception, isDevelopment: true);
        var summary = new StringBuilder(ConfigurationProvider.Current.Performance.ErrorSummaryStringBuilderCapacity);
        
        summary.AppendLine(CultureInfo.InvariantCulture, $"Operation: {operation}");
        summary.AppendLine(CultureInfo.InvariantCulture, $"Exception Type: {sanitizedException.ExceptionType}");
        summary.AppendLine(CultureInfo.InvariantCulture, $"Message: {sanitizedException.Message}");
        
        if (!string.IsNullOrEmpty(sanitizedException.StackTrace))
        {
            summary.AppendLine("Stack Trace:");
            summary.AppendLine(sanitizedException.StackTrace);
        }

        if (sanitizedException.InnerException != null)
        {
            summary.AppendLine(CultureInfo.InvariantCulture, $"Inner Exception: {sanitizedException.InnerException.ExceptionType}");
            summary.AppendLine(CultureInfo.InvariantCulture, $"Inner Message: {sanitizedException.InnerException.Message}");
        }

        return summary.ToString();
    }

    /// <summary>
    /// Determines if an exception contains potentially sensitive information
    /// </summary>
    public static bool ContainsSensitiveInformation(Exception exception)
    {
        if (exception == null) return false;

        var message = exception.Message ?? "";
        var stackTrace = exception.StackTrace ?? "";

        // Check for credential patterns
        if (ApiKeyPattern().IsMatch(message) || 
            BearerTokenPattern().IsMatch(message) || 
            OAuthTokenPattern().IsMatch(message) ||
            CredentialPattern().IsMatch(message))
        {
            return true;
        }

        // Check for sensitive paths
        if (UserPathPattern().IsMatch(message) || UserPathPattern().IsMatch(stackTrace))
        {
            return true;
        }

        // Recursively check inner exceptions
        if (exception.InnerException != null)
        {
            return ContainsSensitiveInformation(exception.InnerException);
        }

        return false;
    }
}

/// <summary>
/// Represents sanitized exception data safe for logging and user display
/// </summary>
public sealed record SanitizedExceptionData(
    string Message,
    string ExceptionType,
    string? StackTrace = null,
    SanitizedExceptionData? InnerException = null)
{
    /// <summary>
    /// Converts the sanitized exception to a user-friendly string
    /// </summary>
    public override string ToString()
    {
        var result = new StringBuilder(ConfigurationProvider.Current.Performance.ExceptionStringBuilderCapacity);
        result.AppendLine(CultureInfo.InvariantCulture, $"{ExceptionType}: {Message}");
        
        if (!string.IsNullOrEmpty(StackTrace))
        {
            result.AppendLine(StackTrace);
        }
        
        if (InnerException != null)
        {
            result.AppendLine(CultureInfo.InvariantCulture, $"Inner Exception: {InnerException}");
        }
        
        return result.ToString();
    }

    /// <summary>
    /// Gets a simple error message suitable for end users
    /// </summary>
    public string GetSimpleMessage() => Message;

    /// <summary>
    /// Gets detailed information suitable for logging (still sanitized)
    /// </summary>
    public string GetDetailedMessage()
    {
        var details = new StringBuilder(ConfigurationProvider.Current.Performance.DetailedMessageStringBuilderCapacity);
        details.AppendLine(CultureInfo.InvariantCulture, $"[{ExceptionType}] {Message}");
        
        if (InnerException != null)
        {
            details.AppendLine(CultureInfo.InvariantCulture, $"Caused by: [{InnerException.ExceptionType}] {InnerException.Message}");
        }
        
        return details.ToString().Trim();
    }
}