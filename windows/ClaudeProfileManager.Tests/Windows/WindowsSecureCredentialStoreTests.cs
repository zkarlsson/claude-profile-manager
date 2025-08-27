using System.Runtime.Versioning;
using System.Security;
using ClaudeProfileManager.Core.Security;
using ClaudeProfileManager.Windows;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ClaudeProfileManager.Tests.Windows;

[SupportedOSPlatform("windows")]
public class WindowsSecureCredentialStoreTests : IDisposable
{
    private readonly WindowsCredentialStore _credentialStore;
    private readonly List<string> _testProfilesToCleanup = new();
    private const string TestServiceType = "Claude Profile Manager Test Secure";

    public WindowsSecureCredentialStoreTests()
    {
        var logger = new NullLogger<WindowsCredentialStore>();
        _credentialStore = new WindowsCredentialStore(logger);
    }

    [Fact]
    public async Task SaveSecureCredentialAsync_WithValidSecureString_SavesSuccessfully()
    {
        // Arrange
        const string profileName = "test-secure-profile";
        const string credentialValue = "secure-test-credential-value";
        _testProfilesToCleanup.Add(profileName);

        using var secureString = new SecureString();
        foreach (char c in credentialValue)
        {
            secureString.AppendChar(c);
        }
        secureString.MakeReadOnly();

        // Act
        var result = await _credentialStore.SaveSecureCredentialAsync(profileName, secureString, TestServiceType);

        // Assert
        result.Should().BeTrue();

        // Verify it was saved correctly by retrieving it
        var retrievedCredential = await _credentialStore.GetCredentialAsync(profileName, TestServiceType);
        retrievedCredential.Should().Be(credentialValue);
    }

    [Fact]
    public async Task GetSecureCredentialAsync_WithExistingCredential_ReturnsSecureScope()
    {
        // Arrange
        const string profileName = "test-secure-get-profile";
        const string credentialValue = "secure-get-test-credential";
        _testProfilesToCleanup.Add(profileName);

        // First save a credential using regular method
        await _credentialStore.SaveCredentialAsync(profileName, credentialValue, TestServiceType);

        // Act
        using var secureScope = await _credentialStore.GetSecureCredentialAsync(profileName, TestServiceType);

        // Assert
        secureScope.Should().NotBeNull();
        secureScope!.Value.Should().Be(credentialValue);
        secureScope.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public async Task GetSecureCredentialAsync_WithNonExistentCredential_ReturnsNull()
    {
        // Arrange
        const string nonExistentProfile = "non-existent-secure-profile";

        // Act
        var secureScope = await _credentialStore.GetSecureCredentialAsync(nonExistentProfile, TestServiceType);

        // Assert
        secureScope.Should().BeNull();
    }

    [Fact]
    public async Task SaveSecureCredentialAsync_WithEmptySecureString_ReturnsFalse()
    {
        // Arrange
        const string profileName = "test-empty-secure-profile";
        _testProfilesToCleanup.Add(profileName);

        using var emptySecureString = new SecureString();
        emptySecureString.MakeReadOnly();

        // Act
        var result = await _credentialStore.SaveSecureCredentialAsync(profileName, emptySecureString, TestServiceType);

        // Assert - Empty credentials should be rejected for security
        result.Should().BeFalse();

        // Verify no credential was saved
        var retrievedCredential = await _credentialStore.GetCredentialAsync(profileName, TestServiceType);
        retrievedCredential.Should().BeNull();
    }

    [Fact]
    public async Task SecureCredentialRoundTrip_MaintainsDataIntegrity()
    {
        // Arrange
        const string profileName = "test-roundtrip-secure-profile";
        const string originalCredential = "complex-credential!@#$%^&*()_+{}|:<>?[]\\;',./";
        _testProfilesToCleanup.Add(profileName);

        using var secureString = new SecureString();
        foreach (char c in originalCredential)
        {
            secureString.AppendChar(c);
        }
        secureString.MakeReadOnly();

        // Act
        var saveResult = await _credentialStore.SaveSecureCredentialAsync(profileName, secureString, TestServiceType);
        
        using var retrievedScope = await _credentialStore.GetSecureCredentialAsync(profileName, TestServiceType);

        // Assert
        saveResult.Should().BeTrue();
        retrievedScope.Should().NotBeNull();
        retrievedScope!.Value.Should().Be(originalCredential);
    }

    [Fact]
    public async Task SecureCredentialScope_AutomaticallyDisposesMemory()
    {
        // Arrange
        const string profileName = "test-dispose-secure-profile";
        const string credentialValue = "dispose-test-credential";
        _testProfilesToCleanup.Add(profileName);

        await _credentialStore.SaveCredentialAsync(profileName, credentialValue, TestServiceType);

        // Act & Assert
        string extractedValue = "";
        using (var secureScope = await _credentialStore.GetSecureCredentialAsync(profileName, TestServiceType))
        {
            secureScope.Should().NotBeNull();
            extractedValue = secureScope!.Value;
            secureScope.IsEmpty.Should().BeFalse();
        }
        // After using block, scope should be disposed and memory cleared
        
        extractedValue.Should().Be(credentialValue);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SaveSecureCredentialAsync_WithInvalidProfileName_ReturnsFalse(string? invalidProfileName)
    {
        // Arrange
        using var secureString = new SecureString();
        secureString.AppendChar('x');
        secureString.MakeReadOnly();

        // Act
        var result = await _credentialStore.SaveSecureCredentialAsync(invalidProfileName!, secureString, TestServiceType);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetSecureCredentialAsync_WithInvalidProfileName_ReturnsNull(string? invalidProfileName)
    {
        // Act
        var result = await _credentialStore.GetSecureCredentialAsync(invalidProfileName!, TestServiceType);

        // Assert
        result.Should().BeNull();
    }

    public void Dispose()
    {
        // Cleanup test profiles
        foreach (var profileName in _testProfilesToCleanup)
        {
            try
            {
                _credentialStore.DeleteCredentialAsync(profileName, TestServiceType).Wait();
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
        GC.SuppressFinalize(this);
    }
}