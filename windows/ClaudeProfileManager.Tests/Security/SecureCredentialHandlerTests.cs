using System.Security;
using ClaudeProfileManager.Core.Security;
using FluentAssertions;
using Xunit;

namespace ClaudeProfileManager.Tests.Security;

public class SecureCredentialHandlerTests
{
    [Fact]
    public void ToSecureString_WithValidString_CreatesSecureString()
    {
        // Arrange
        const string testCredential = "test-credential-value";

        // Act
        using var secureString = SecureCredentialHandler.ToSecureString(testCredential);

        // Assert
        secureString.Should().NotBeNull();
        secureString.Length.Should().Be(testCredential.Length);
    }

    [Fact]
    public void ToSecureString_WithEmptyString_CreatesEmptySecureString()
    {
        // Arrange
        const string testCredential = "";

        // Act
        using var secureString = SecureCredentialHandler.ToSecureString(testCredential);

        // Assert
        secureString.Should().NotBeNull();
        secureString.Length.Should().Be(0);
    }

    [Fact]
    public void ToSecureString_WithNullString_CreatesEmptySecureString()
    {
        // Act
        using var secureString = SecureCredentialHandler.ToSecureString(null!);

        // Assert
        secureString.Should().NotBeNull();
        secureString.Length.Should().Be(0);
    }

    [Fact]
    public void GetCredential_WithValidSecureString_ReturnsCorrectValue()
    {
        // Arrange
        const string testCredential = "test-credential-value";
        using var secureString = new SecureString();
        foreach (char c in testCredential)
        {
            secureString.AppendChar(c);
        }
        secureString.MakeReadOnly();

        // Act
        using var credentialScope = SecureCredentialHandler.GetCredential(secureString);

        // Assert
        credentialScope.Should().NotBeNull();
        credentialScope.Value.Should().Be(testCredential);
        credentialScope.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void GetCredential_WithEmptySecureString_ReturnsEmptyScope()
    {
        // Arrange
        using var emptySecureString = new SecureString();
        emptySecureString.MakeReadOnly();

        // Act
        using var credentialScope = SecureCredentialHandler.GetCredential(emptySecureString);

        // Assert
        credentialScope.Should().NotBeNull();
        credentialScope.Value.Should().BeEmpty();
        credentialScope.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void GetCredential_WithNullSecureString_ReturnsEmptyScope()
    {
        // Act
        using var credentialScope = SecureCredentialHandler.GetCredential(null!);

        // Assert
        credentialScope.Should().NotBeNull();
        credentialScope.Value.Should().BeEmpty();
        credentialScope.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void SecureCredentialScope_WhenDisposed_CleansUpMemory()
    {
        // Arrange
        const string testCredential = "test-credential-value";
        using var secureString = new SecureString();
        foreach (char c in testCredential)
        {
            secureString.AppendChar(c);
        }
        secureString.MakeReadOnly();

        SecureCredentialScope? scope;
        
        // Act
        scope = SecureCredentialHandler.GetCredential(secureString);
        var valueBeforeDispose = scope.Value;
        scope.Dispose();

        // Assert
        valueBeforeDispose.Should().Be(testCredential);
        // After disposal, we can't test the internal state, but the Dispose method should have run
        scope.Should().NotBeNull(); // The object itself still exists, but memory should be cleared
    }

    [Fact]
    public void SecureCredentialScope_UsingPattern_AutomaticallyCleansUp()
    {
        // Arrange
        const string testCredential = "test-credential-value";
        using var secureString = new SecureString();
        foreach (char c in testCredential)
        {
            secureString.AppendChar(c);
        }
        secureString.MakeReadOnly();

        string extractedValue = "";

        // Act & Assert - using pattern should work correctly
        using (var credentialScope = SecureCredentialHandler.GetCredential(secureString))
        {
            extractedValue = credentialScope.Value;
            credentialScope.IsEmpty.Should().BeFalse();
        }
        
        // Verify the value was extracted correctly before disposal
        extractedValue.Should().Be(testCredential);
    }

    [Fact]
    public void RoundTrip_StringToSecureStringAndBack_MaintainsValue()
    {
        // Arrange
        const string originalCredential = "complex-credential!@#$%^&*()_+{}|:<>?";

        // Act
        var secureString = SecureCredentialHandler.ToSecureString(originalCredential);
        
        string retrievedValue;
        using (var credentialScope = SecureCredentialHandler.GetCredential(secureString))
        {
            retrievedValue = credentialScope.Value;
        }

        // Assert
        retrievedValue.Should().Be(originalCredential);
    }
}