using ClaudeProfileManager.CLI;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ClaudeProfileManager.Tests;

public class GlobalExceptionHandlerTests
{
    [Fact]
    public void HandleException_WithSensitiveException_ReturnsSanitizedMessage()
    {
        // Arrange
        var logger = new TestLogger();
        GlobalExceptionHandler.Initialize(logger, isDevelopment: false);
        var exception = new InvalidOperationException("Failed to connect with API key: sk-ant-api03-secret123");

        // Act
        var result = GlobalExceptionHandler.HandleException(exception, "Authentication");

        // Assert
        result.Should().Be("Authentication: Operation cannot be completed in current state");
        result.Should().NotContain("sk-ant-api03-secret123");
    }

    [Fact]
    public void ContainsSensitiveInformation_WithApiKey_ReturnsTrue()
    {
        // Arrange
        var exception = new InvalidOperationException("Error: sk-ant-api03-secret");

        // Act
        var result = GlobalExceptionHandler.ContainsSensitiveInformation(exception);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ContainsSensitiveInformation_WithNormalMessage_ReturnsFalse()
    {
        // Arrange
        var exception = new FileNotFoundException("File not found");

        // Act
        var result = GlobalExceptionHandler.ContainsSensitiveInformation(exception);

        // Assert
        result.Should().BeFalse();
    }

    private sealed class TestLogger : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }
}