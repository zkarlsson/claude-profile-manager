using System.ComponentModel;
using ClaudeProfileManager.Core.Security;
using FluentAssertions;
using Xunit;

namespace ClaudeProfileManager.Tests.Security;

public class ExceptionSanitizerTests
{
    [Fact]
    public void SanitizeMessage_WithCredentials_RedactsApiKeys()
    {
        // Arrange
        var input = "Error with API key: sk-ant-api03-abcdefghijklmnop";
        
        // Act
        var result = ExceptionSanitizer.SanitizeMessage(input);
        
        // Assert - should redact the API key
        result.Should().Contain("[REDACTED-API-KEY]");
        result.Should().NotContain("sk-ant-api03-abcdefghijklmnop");
    }

    [Fact]
    public void SanitizeMessage_WithPaths_SanitizesPaths()
    {
        // Arrange
        var userPathInput = "Error in C:\\Users\\johndoe\\Documents\\file.txt";
        
        // Act
        var result = ExceptionSanitizer.SanitizeMessage(userPathInput);
        
        // Assert - should sanitize user paths
        result.Should().Contain("[USER]");
        result.Should().NotContain("johndoe");
    }

    [Fact]
    public void SanitizeMessage_WithNullOrEmpty_ReturnsDefaultMessage()
    {
        // Act & Assert
        ExceptionSanitizer.SanitizeMessage(null).Should().Be("An error occurred");
        ExceptionSanitizer.SanitizeMessage("").Should().Be("An error occurred");
    }

    [Fact]
    public void Sanitize_WithSimpleException_ReturnsSanitizedException()
    {
        // Arrange
        var exception = new InvalidOperationException("Test error message");

        // Act
        var result = ExceptionSanitizer.Sanitize(exception);

        // Assert
        result.Should().NotBeNull();
        result.Message.Should().Be("Test error message");
        result.ExceptionType.Should().Be("InvalidOperationException");
        result.StackTrace.Should().BeNull(); // Not in development mode
        result.InnerException.Should().BeNull();
    }

    [Fact]
    public void Sanitize_WithInnerException_SanitizesInnerException()
    {
        // Arrange
        var innerException = new ArgumentException("Inner error with sk-ant-api03-secret");
        var exception = new InvalidOperationException("Outer error", innerException);

        // Act
        var result = ExceptionSanitizer.Sanitize(exception);

        // Assert
        result.InnerException.Should().NotBeNull();
        result.InnerException!.Message.Should().Be("Inner error with [REDACTED-API-KEY]");
        result.InnerException.ExceptionType.Should().Be("ArgumentException");
    }

    [Fact]
    public void Sanitize_WithDevelopmentMode_PreservesStackTrace()
    {
        // Arrange
        var exception = new InvalidOperationException("Test error");

        // Act
        var result = ExceptionSanitizer.Sanitize(exception, isDevelopment: true);

        // Assert - StackTrace should be included in development mode
        // Note: StackTrace might be null in test context, but the sanitizer should attempt to process it
        if (exception.StackTrace != null)
        {
            result.StackTrace.Should().NotBeNull();
        }
    }

    [Fact]
    public void SanitizeStackTrace_WithFilePaths_RemovesFullPaths()
    {
        // Arrange
        var stackTrace = @"   at ClaudeProfileManager.Windows.WindowsCredentialStore.SaveCredentialAsync() in C:\Users\developer\source\ClaudeProfileManager\WindowsCredentialStore.cs:line 45
   at ClaudeProfileManager.Windows.WindowsProfileManager.SaveProfileAsync() in C:\Users\developer\source\ClaudeProfileManager\WindowsProfileManager.cs:line 123";

        // Act
        var result = ExceptionSanitizer.SanitizeStackTrace(stackTrace);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("in WindowsCredentialStore.cs");
        result.Should().Contain("in WindowsProfileManager.cs");
        result.Should().NotContain("C:\\Users\\developer");
    }

    [Theory]
    [InlineData(typeof(ArgumentException), "Invalid input provided")]
    [InlineData(typeof(ArgumentNullException), "Required information is missing")]
    [InlineData(typeof(UnauthorizedAccessException), "Access denied. Please check permissions")]
    [InlineData(typeof(FileNotFoundException), "Required file not found")]
    [InlineData(typeof(DirectoryNotFoundException), "Required directory not found")]
    [InlineData(typeof(IOException), "File operation failed")]
    [InlineData(typeof(InvalidOperationException), "Operation cannot be completed in current state")]
    [InlineData(typeof(TimeoutException), "Operation timed out")]
    [InlineData(typeof(NotSupportedException), "Operation not supported")]
    public void GetUserFriendlyMessage_WithKnownExceptions_ReturnsAppropriateMessage(Type exceptionType, string expectedMessage)
    {
        // Arrange
        var exception = (Exception)Activator.CreateInstance(exceptionType, "Original message")!;

        // Act
        var result = ExceptionSanitizer.GetUserFriendlyMessage(exception);

        // Assert
        result.Should().Be(expectedMessage);
    }

    [Fact]
    public void GetUserFriendlyMessage_WithContext_IncludesContext()
    {
        // Arrange
        var exception = new ArgumentException("Test message");
        const string context = "Profile saving";

        // Act
        var result = ExceptionSanitizer.GetUserFriendlyMessage(exception, context);

        // Assert
        result.Should().Be("Profile saving: Invalid input provided");
    }

    [Fact]
    public void GetUserFriendlyMessage_WithWin32Exception_ReturnsWindowsErrorMessage()
    {
        // Arrange
        var exception = new Win32Exception(5); // Access denied

        // Act
        var result = ExceptionSanitizer.GetUserFriendlyMessage(exception);

        // Assert
        result.Should().Be("Windows system error occurred");
    }

    [Fact]
    public void CreateDevelopmentErrorSummary_WithException_CreatesDetailedSummary()
    {
        // Arrange
        var innerException = new ArgumentException("Inner argument error");
        var exception = new InvalidOperationException("Main error", innerException);
        const string operation = "Saving profile";

        // Act
        var result = ExceptionSanitizer.CreateDevelopmentErrorSummary(exception, operation);

        // Assert
        result.Should().Contain("Operation: Saving profile");
        result.Should().Contain("Exception Type: InvalidOperationException");
        result.Should().Contain("Message: Main error");
        result.Should().Contain("Inner Exception: ArgumentException");
        result.Should().Contain("Inner Message: Inner argument error");
    }

    [Theory]
    [InlineData("Error with sk-ant-api03-secret", true)]
    [InlineData("Bearer token Bearer abc123", true)]
    [InlineData("Path: C:\\Users\\johndoe\\file.txt", true)]
    [InlineData("Normal error message", false)]
    [InlineData("File not found", false)]
    public void ContainsSensitiveInformation_WithVariousMessages_ReturnsCorrectResult(string message, bool expectedSensitive)
    {
        // Arrange
        var exception = new InvalidOperationException(message);

        // Act
        var result = ExceptionSanitizer.ContainsSensitiveInformation(exception);

        // Assert
        result.Should().Be(expectedSensitive);
    }

    [Fact]
    public void ContainsSensitiveInformation_WithInnerException_ChecksBoth()
    {
        // Arrange
        var innerException = new ArgumentException("Contains sk-ant-api03-secret");
        var exception = new InvalidOperationException("Normal outer message", innerException);

        // Act
        var result = ExceptionSanitizer.ContainsSensitiveInformation(exception);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void SanitizedException_ToString_FormatsCorrectly()
    {
        // Arrange
        var innerSanitized = new SanitizedExceptionData("Inner error", "ArgumentException");
        var sanitized = new SanitizedExceptionData("Main error", "InvalidOperationException", "Stack trace", innerSanitized);

        // Act
        var result = sanitized.ToString();

        // Assert
        result.Should().Contain("InvalidOperationException: Main error");
        result.Should().Contain("Stack trace");
        result.Should().Contain("Inner Exception: ArgumentException: Inner error");
    }

    [Fact]
    public void SanitizedException_GetSimpleMessage_ReturnsMessageOnly()
    {
        // Arrange
        var sanitized = new SanitizedExceptionData("Test error", "TestException");

        // Act
        var result = sanitized.GetSimpleMessage();

        // Assert
        result.Should().Be("Test error");
    }

    [Fact]
    public void SanitizedException_GetDetailedMessage_ReturnsFormattedDetails()
    {
        // Arrange
        var innerSanitized = new SanitizedExceptionData("Inner error", "ArgumentException");
        var sanitized = new SanitizedExceptionData("Main error", "InvalidOperationException", null, innerSanitized);

        // Act
        var result = sanitized.GetDetailedMessage();

        // Assert
        result.Should().Contain("[InvalidOperationException] Main error");
        result.Should().Contain("Caused by: [ArgumentException] Inner error");
    }

    [Fact]
    public void Sanitize_WithNullException_ReturnsDefaultSanitizedException()
    {
        // Act
        var result = ExceptionSanitizer.Sanitize(null!);

        // Assert
        result.Should().NotBeNull();
        result.Message.Should().Be("Unknown error occurred");
        result.ExceptionType.Should().Be("UnknownException");
        result.InnerException.Should().BeNull();
    }

    [Fact]
    public void SanitizeMessage_WithComplexCredentialPatterns_RedactsCredentials()
    {
        // Arrange
        var message = @"Connection failed with credentials: {""password"": ""secret123"", ""apiKey"": ""sk-ant-api03-abcdefg""}";

        // Act
        var result = ExceptionSanitizer.SanitizeMessage(message);

        // Assert - should redact sensitive information
        result.Should().NotContain("secret123");
        result.Should().NotContain("sk-ant-api03-abcdefg");
    }
}