using System.Collections.Concurrent;
using ClaudeProfileManager.Core.Security;
using Microsoft.Extensions.Logging;

namespace ClaudeProfileManager.Core.Logging;

/// <summary>
/// A logging provider that automatically sanitizes exceptions in log messages
/// </summary>
public sealed class SanitizedLoggingProvider : ILoggerProvider
{
    private readonly ILoggerProvider _innerProvider;
    private readonly bool _isDevelopment;
    private readonly ConcurrentDictionary<string, SanitizedLogger> _loggers = new();
    private bool _disposed;

    public SanitizedLoggingProvider(ILoggerProvider innerProvider, bool isDevelopment = false)
    {
        _innerProvider = innerProvider ?? throw new ArgumentNullException(nameof(innerProvider));
        _isDevelopment = isDevelopment;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name =>
        {
            var innerLogger = _innerProvider.CreateLogger(name);
            return new SanitizedLogger(innerLogger, _isDevelopment);
        });
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _loggers.Clear();
            _innerProvider.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// A logger that automatically sanitizes exceptions before passing them to the inner logger
/// </summary>
internal sealed class SanitizedLogger : ILogger
{
    private readonly ILogger _innerLogger;
    private readonly bool _isDevelopment;

    public SanitizedLogger(ILogger innerLogger, bool isDevelopment)
    {
        _innerLogger = innerLogger ?? throw new ArgumentNullException(nameof(innerLogger));
        _isDevelopment = isDevelopment;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => _innerLogger.BeginScope(state);

    public bool IsEnabled(LogLevel logLevel)
        => _innerLogger.IsEnabled(logLevel);

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        // If there's an exception, sanitize it
        if (exception != null)
        {
            var sanitizedException = ExceptionSanitizer.Sanitize(exception, _isDevelopment);
            
            // Create a new formatter that includes sanitized exception info
            var sanitizedFormatter = (TState s, Exception? ex) =>
            {
                var originalMessage = formatter(s, null); // Format without the original exception
                var exceptionInfo = _isDevelopment 
                    ? sanitizedException.GetDetailedMessage()
                    : sanitizedException.GetSimpleMessage();
                
                return $"{originalMessage} | Exception: {exceptionInfo}";
            };

            // Log without the original exception to prevent it from being logged elsewhere
            _innerLogger.Log(logLevel, eventId, state, null, sanitizedFormatter);
        }
        else
        {
            // No exception, pass through normally
            _innerLogger.Log(logLevel, eventId, state, exception, formatter);
        }
    }
}