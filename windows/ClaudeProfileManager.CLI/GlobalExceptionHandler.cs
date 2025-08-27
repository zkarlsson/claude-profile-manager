using System.ComponentModel;
using ClaudeProfileManager.Core.Security;
using Microsoft.Extensions.Logging;

namespace ClaudeProfileManager.CLI;

/// <summary>
/// Global exception handler for the CLI application
/// </summary>
public static class GlobalExceptionHandler
{
    private static ILogger? _logger;
    private static bool _isDevelopment;

    /// <summary>
    /// Initializes the global exception handler
    /// </summary>
    public static void Initialize(ILogger logger, bool isDevelopment = false)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _isDevelopment = isDevelopment;

        // Handle unhandled exceptions
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        
        // Handle unhandled task exceptions
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    /// <summary>
    /// Handles and sanitizes an exception, returning a user-friendly message
    /// </summary>
    public static string HandleException(Exception exception, string operation = "Operation")
    {
        var sanitizedException = ExceptionSanitizer.Sanitize(exception, _isDevelopment);
        
        // Log the full sanitized details for diagnostics
        _logger?.LogError("Exception during {Operation}: {Details}", 
            operation, 
            _isDevelopment ? sanitizedException.GetDetailedMessage() : sanitizedException.GetSimpleMessage());

        // Return user-friendly message
        return ExceptionSanitizer.GetUserFriendlyMessage(exception, operation);
    }

    /// <summary>
    /// Determines if an exception contains sensitive information
    /// </summary>
    public static bool ContainsSensitiveInformation(Exception exception)
    {
        return ExceptionSanitizer.ContainsSensitiveInformation(exception);
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            var userMessage = HandleException(exception, "Application");
            
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"Fatal error: {userMessage}");
            Console.ResetColor();
            
            if (_isDevelopment && ContainsSensitiveInformation(exception))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Error.WriteLine("Warning: This error contained sensitive information that has been sanitized in logs.");
                Console.ResetColor();
            }
        }
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        e.SetObserved(); // Mark as observed to prevent app termination
        
        if (e.Exception != null)
        {
            var userMessage = HandleException(e.Exception, "Background task");
            
            _logger?.LogError("Unobserved task exception: {Message}", userMessage);
        }
    }
}