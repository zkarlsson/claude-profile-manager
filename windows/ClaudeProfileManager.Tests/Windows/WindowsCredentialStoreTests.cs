using ClaudeProfileManager.Windows;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClaudeProfileManager.Tests.Windows;

public class WindowsCredentialStoreTests : IDisposable
{
    private readonly WindowsCredentialStore _credentialStore;
    private readonly List<string> _testProfilesToCleanup = new();
    private const string TestServiceType = "Claude Profile Manager Test";

    public WindowsCredentialStoreTests()
    {
        var logger = new NullLogger<WindowsCredentialStore>();
        _credentialStore = new WindowsCredentialStore(logger);
    }

    [Fact]
    public async Task SaveAndGetCredential_WithValidData_ShouldSucceed()
    {
        // Arrange
        var profileName = "test-profile-" + Guid.NewGuid().ToString("N")[..8];
        var credential = "test-credential-value";
        _testProfilesToCleanup.Add(profileName);

        // Act
        var saveResult = await _credentialStore.SaveCredentialAsync(profileName, credential, TestServiceType);
        var retrievedCredential = await _credentialStore.GetCredentialAsync(profileName, TestServiceType);

        // Assert
        saveResult.Should().BeTrue();
        retrievedCredential.Should().Be(credential);
    }

    [Fact]
    public async Task GetCredential_WithNonExistentProfile_ShouldReturnNull()
    {
        // Arrange
        var profileName = "non-existent-profile-" + Guid.NewGuid().ToString("N")[..8];

        // Act
        var result = await _credentialStore.GetCredentialAsync(profileName, TestServiceType);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteCredential_WithExistingProfile_ShouldSucceed()
    {
        // Arrange
        var profileName = "test-delete-profile-" + Guid.NewGuid().ToString("N")[..8];
        var credential = "test-credential-to-delete";
        await _credentialStore.SaveCredentialAsync(profileName, credential, TestServiceType);

        // Act
        var deleteResult = await _credentialStore.DeleteCredentialAsync(profileName, TestServiceType);
        var retrievedCredential = await _credentialStore.GetCredentialAsync(profileName, TestServiceType);

        // Assert
        deleteResult.Should().BeTrue();
        retrievedCredential.Should().BeNull();
    }

    [Fact]
    public async Task DeleteCredential_WithNonExistentProfile_ShouldReturnTrue()
    {
        // Arrange
        var profileName = "non-existent-delete-profile-" + Guid.NewGuid().ToString("N")[..8];

        // Act
        var result = await _credentialStore.DeleteCredentialAsync(profileName, TestServiceType);

        // Assert
        result.Should().BeTrue(); // Deleting non-existent credential should be considered success
    }

    [Fact]
    public async Task ListProfiles_WithMultipleProfiles_ShouldReturnAllProfiles()
    {
        // Arrange
        var profile1 = "test-list-profile1-" + Guid.NewGuid().ToString("N")[..8];
        var profile2 = "test-list-profile2-" + Guid.NewGuid().ToString("N")[..8];
        var credential1 = "credential1";
        var credential2 = "credential2";

        _testProfilesToCleanup.AddRange(new[] { profile1, profile2 });

        await _credentialStore.SaveCredentialAsync(profile1, credential1, TestServiceType);
        await _credentialStore.SaveCredentialAsync(profile2, credential2, TestServiceType);

        // Act
        var profiles = await _credentialStore.ListProfilesAsync(TestServiceType);

        // Assert
        profiles.Should().Contain(profile1);
        profiles.Should().Contain(profile2);
        profiles.Count().Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task SaveCredential_WithLongCredential_ShouldSucceed()
    {
        // Arrange
        var profileName = "test-long-credential-" + Guid.NewGuid().ToString("N")[..8];
        var longCredential = new string('A', 2048); // 2KB credential
        _testProfilesToCleanup.Add(profileName);

        // Act
        var saveResult = await _credentialStore.SaveCredentialAsync(profileName, longCredential, TestServiceType);
        var retrievedCredential = await _credentialStore.GetCredentialAsync(profileName, TestServiceType);

        // Assert
        saveResult.Should().BeTrue();
        retrievedCredential.Should().Be(longCredential);
    }

    [Fact]
    public async Task SaveCredential_WithUnicodeCharacters_ShouldSucceed()
    {
        // Arrange
        var profileName = "test-unicode-" + Guid.NewGuid().ToString("N")[..8];
        var unicodeCredential = "测试-credential-🔐-éñ";
        _testProfilesToCleanup.Add(profileName);

        // Act
        var saveResult = await _credentialStore.SaveCredentialAsync(profileName, unicodeCredential, TestServiceType);
        var retrievedCredential = await _credentialStore.GetCredentialAsync(profileName, TestServiceType);

        // Assert
        saveResult.Should().BeTrue();
        retrievedCredential.Should().Be(unicodeCredential);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task SaveCredential_WithInvalidProfileName_ShouldHandleGracefully(string? invalidProfileName)
    {
        // Arrange
        var credential = "test-credential";

        // Act & Assert
        if (string.IsNullOrEmpty(invalidProfileName))
        {
            var saveResult = await _credentialStore.SaveCredentialAsync(invalidProfileName!, credential, TestServiceType);
            saveResult.Should().BeFalse(); // Should fail gracefully
        }
    }

    [Fact]
    public async Task SaveCredential_OverwriteExisting_ShouldUpdateCredential()
    {
        // Arrange
        var profileName = "test-overwrite-" + Guid.NewGuid().ToString("N")[..8];
        var originalCredential = "original-credential";
        var updatedCredential = "updated-credential";
        _testProfilesToCleanup.Add(profileName);

        // Act
        await _credentialStore.SaveCredentialAsync(profileName, originalCredential, TestServiceType);
        var overwriteResult = await _credentialStore.SaveCredentialAsync(profileName, updatedCredential, TestServiceType);
        var retrievedCredential = await _credentialStore.GetCredentialAsync(profileName, TestServiceType);

        // Assert
        overwriteResult.Should().BeTrue();
        retrievedCredential.Should().Be(updatedCredential);
        retrievedCredential.Should().NotBe(originalCredential);
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