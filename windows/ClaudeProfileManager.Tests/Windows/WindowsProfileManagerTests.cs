using System.Runtime.Versioning;
using ClaudeProfileManager.Core.Interfaces;
using ClaudeProfileManager.Core.Models;
using ClaudeProfileManager.Windows;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ClaudeProfileManager.Tests.Windows;

[SupportedOSPlatform("windows")]
public class WindowsProfileManagerTests : IDisposable
{
    private readonly WindowsProfileManager _profileManager;
    private readonly TestCredentialStore _credentialStore;
    private readonly WindowsProfileFileManager _profileFileManager;
    private readonly TestClaudeAuthDetector _authDetector;
    private readonly ILogger<WindowsProfileManager> _logger;
    private readonly string _testDirectory;

    public WindowsProfileManagerTests()
    {
        _logger = new TestLogger<WindowsProfileManager>();
        _credentialStore = new TestCredentialStore();
        _testDirectory = Path.Combine(Path.GetTempPath(), $"ProfileManagerTest_{Guid.NewGuid()}");
        
        var fileManagerLogger = new TestLogger<WindowsProfileFileManager>();
        _profileFileManager = new WindowsProfileFileManager(fileManagerLogger, _testDirectory);
        
        _authDetector = new TestClaudeAuthDetector();
        
        _profileManager = new WindowsProfileManager(
            _logger,
            _credentialStore,
            _profileFileManager,
            _authDetector);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task SaveProfileAsync_SavesConsoleProfile_Successfully()
    {
        // Arrange
        var profileName = "test-console-profile";
        var apiKey = "sk-ant-api03-" + new string('A', 90);
        
        _authDetector.SetCurrentAuth(AuthMethod.Console, apiKey);

        // Act
        var result = await _profileManager.SaveProfileAsync(profileName);

        // Assert
        result.Should().BeTrue();
        
        // Verify credentials were saved
        var savedCredential = await _credentialStore.GetCredentialAsync($"{profileName}-console");
        savedCredential.Should().Be(apiKey);
        
        // Verify profile metadata was saved
        var profile = await _profileFileManager.LoadProfileMetadataAsync(profileName);
        profile.Should().NotBeNull();
        profile!.Name.Should().Be(profileName);
        profile.AuthMethod.Should().Be(AuthMethod.Console);
    }

    [Fact]
    public async Task SaveProfileAsync_SavesSubscriptionProfile_Successfully()
    {
        // Arrange
        var profileName = "test-subscription-profile";
        var token = """{"claudeAiOauth": {"expiresAt": 1735689600000}}""";
        
        _authDetector.SetCurrentAuth(AuthMethod.Subscription, token);

        // Act
        var result = await _profileManager.SaveProfileAsync(profileName);

        // Assert
        result.Should().BeTrue();
        
        // Verify credentials were saved
        var savedCredential = await _credentialStore.GetCredentialAsync($"{profileName}-subscription");
        savedCredential.Should().Be(token);
        
        // Verify profile metadata was saved
        var profile = await _profileFileManager.LoadProfileMetadataAsync(profileName);
        profile.Should().NotBeNull();
        profile!.AuthMethod.Should().Be(AuthMethod.Subscription);
    }

    [Fact]
    public async Task SaveProfileAsync_SavesAliases_Successfully()
    {
        // Arrange
        var profileName = "work-profile";
        var aliases = new[] { "w", "work" };
        var apiKey = "sk-ant-api03-" + new string('B', 90);
        
        _authDetector.SetCurrentAuth(AuthMethod.Console, apiKey);

        // Act
        var result = await _profileManager.SaveProfileAsync(profileName, aliases);

        // Assert
        result.Should().BeTrue();
        
        // Verify aliases were saved
        var savedAliases = await _profileFileManager.LoadAliasesAsync();
        savedAliases.Should().ContainKey("w");
        savedAliases.Should().ContainKey("work");
        savedAliases["w"].Should().Be(profileName);
        savedAliases["work"].Should().Be(profileName);
    }

    [Fact]
    public async Task SaveProfileAsync_ReturnsFalse_WhenNoAuthenticationDetected()
    {
        // Arrange
        _authDetector.SetCurrentAuth(AuthMethod.None, null);

        // Act
        var result = await _profileManager.SaveProfileAsync("test-profile");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SaveProfileAsync_ReturnsFalse_WhenProfileNameIsEmpty()
    {
        // Act
        var result = await _profileManager.SaveProfileAsync("");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetProfileAsync_ReturnsProfile_WhenExists()
    {
        // Arrange
        var profileName = "existing-profile";
        await SetupTestProfile(profileName, AuthMethod.Console);

        // Act
        var result = await _profileManager.GetProfileAsync(profileName);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be(profileName);
        result.AuthMethod.Should().Be(AuthMethod.Console);
    }

    [Fact]
    public async Task GetProfileAsync_ReturnsNull_WhenNotExists()
    {
        // Act
        var result = await _profileManager.GetProfileAsync("nonexistent-profile");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetProfileAsync_ResolvesAlias_Successfully()
    {
        // Arrange
        var profileName = "actual-profile";
        var alias = "alias-name";
        
        await SetupTestProfile(profileName, AuthMethod.Console);
        await _profileManager.AddAliasAsync(alias, profileName);

        // Act
        var result = await _profileManager.GetProfileAsync(alias);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be(profileName);
    }

    [Fact]
    public async Task ListProfilesAsync_ReturnsAllProfiles_Successfully()
    {
        // Arrange
        await SetupTestProfile("profile1", AuthMethod.Console);
        await SetupTestProfile("profile2", AuthMethod.Subscription);
        await SetupTestProfile("profile3", AuthMethod.Console);

        // Act
        var result = await _profileManager.ListProfilesAsync();

        // Assert
        var profiles = result.ToList();
        profiles.Should().HaveCount(3);
        profiles.Should().Contain(p => p.Name == "profile1");
        profiles.Should().Contain(p => p.Name == "profile2");
        profiles.Should().Contain(p => p.Name == "profile3");
    }

    [Fact]
    public async Task SwitchToProfileAsync_SwitchesConsoleProfile_Successfully()
    {
        // Arrange
        var profileName = "console-profile";
        var apiKey = "sk-ant-api03-" + new string('C', 90);
        
        await SetupTestProfile(profileName, AuthMethod.Console, apiKey);

        // Act
        var result = await _profileManager.SwitchToProfileAsync(profileName);

        // Assert
        result.Should().BeTrue();
        
        // Verify credentials were restored to auth detector
        _authDetector.ConsoleApiKey.Should().Be(apiKey);
        
        // Verify current profile was set
        var currentProfile = await _profileFileManager.GetCurrentProfileAsync();
        currentProfile.Should().Be(profileName);
    }

    [Fact]
    public async Task SwitchToProfileAsync_SwitchesSubscriptionProfile_Successfully()
    {
        // Arrange
        var profileName = "subscription-profile";
        var token = """{"claudeAiOauth": {"expiresAt": 1735689600000}}""";
        
        await SetupTestProfile(profileName, AuthMethod.Subscription, token);

        // Act
        var result = await _profileManager.SwitchToProfileAsync(profileName);

        // Assert
        result.Should().BeTrue();
        
        // Verify credentials were restored
        _authDetector.SubscriptionToken.Should().Be(token);
        
        // Verify current profile was set
        var currentProfile = await _profileFileManager.GetCurrentProfileAsync();
        currentProfile.Should().Be(profileName);
    }

    [Fact]
    public async Task SwitchToProfileAsync_ResolvesAlias_Successfully()
    {
        // Arrange
        var profileName = "real-profile";
        var alias = "shortcut";
        var apiKey = "sk-ant-api03-" + new string('D', 90);
        
        await SetupTestProfile(profileName, AuthMethod.Console, apiKey);
        await _profileManager.AddAliasAsync(alias, profileName);

        // Act
        var result = await _profileManager.SwitchToProfileAsync(alias);

        // Assert
        result.Should().BeTrue();
        
        var currentProfile = await _profileFileManager.GetCurrentProfileAsync();
        currentProfile.Should().Be(profileName); // Should resolve to actual profile name
    }

    [Fact]
    public async Task SwitchToProfileAsync_ReturnsFalse_WhenProfileNotFound()
    {
        // Act
        var result = await _profileManager.SwitchToProfileAsync("nonexistent-profile");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetCurrentProfileAsync_ReturnsCurrentProfile_WhenSet()
    {
        // Arrange
        var profileName = "current-profile";
        await _profileFileManager.SaveCurrentProfileAsync(profileName);

        // Act
        var result = await _profileManager.GetCurrentProfileAsync();

        // Assert
        result.Should().Be(profileName);
    }

    [Fact]
    public async Task GetCurrentProfileAsync_ReturnsNull_WhenNotSet()
    {
        // Act
        var result = await _profileManager.GetCurrentProfileAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteProfileAsync_DeletesProfile_Successfully()
    {
        // Arrange
        var profileName = "profile-to-delete";
        await SetupTestProfile(profileName, AuthMethod.Console);

        // Act
        var result = await _profileManager.DeleteProfileAsync(profileName);

        // Assert
        result.Should().BeTrue();
        
        // Verify profile metadata was deleted
        var profile = await _profileFileManager.LoadProfileMetadataAsync(profileName);
        profile.Should().BeNull();
        
        // Verify credentials were deleted
        var credentials = await _credentialStore.GetCredentialAsync($"{profileName}-console");
        credentials.Should().BeNull();
    }

    [Fact]
    public async Task DeleteProfileAsync_RemovesAliases_Successfully()
    {
        // Arrange
        var profileName = "profile-with-aliases";
        await SetupTestProfile(profileName, AuthMethod.Console);
        await _profileManager.AddAliasAsync("alias1", profileName);
        await _profileManager.AddAliasAsync("alias2", profileName);

        // Act
        var result = await _profileManager.DeleteProfileAsync(profileName);

        // Assert
        result.Should().BeTrue();
        
        // Verify aliases were removed
        var aliases = await _profileFileManager.LoadAliasesAsync();
        aliases.Should().NotContainKey("alias1");
        aliases.Should().NotContainKey("alias2");
    }

    [Fact]
    public async Task DeleteProfileAsync_ClearsCurrent_WhenDeletingCurrentProfile()
    {
        // Arrange
        var profileName = "current-profile-to-delete";
        await SetupTestProfile(profileName, AuthMethod.Console);
        await _profileFileManager.SaveCurrentProfileAsync(profileName);

        // Act
        var result = await _profileManager.DeleteProfileAsync(profileName);

        // Assert
        result.Should().BeTrue();
        
        // Verify current profile was cleared
        var currentProfile = await _profileFileManager.GetCurrentProfileAsync();
        currentProfile.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task AddAliasAsync_AddsAlias_Successfully()
    {
        // Arrange
        var profileName = "target-profile";
        var aliasName = "new-alias";
        await SetupTestProfile(profileName, AuthMethod.Console);

        // Act
        var result = await _profileManager.AddAliasAsync(aliasName, profileName);

        // Assert
        result.Should().BeTrue();
        
        // Verify alias was added
        var aliases = await _profileFileManager.LoadAliasesAsync();
        aliases.Should().ContainKey(aliasName);
        aliases[aliasName].Should().Be(profileName);
    }

    [Fact]
    public async Task AddAliasAsync_ReturnsFalse_WhenProfileNotFound()
    {
        // Act
        var result = await _profileManager.AddAliasAsync("alias", "nonexistent-profile");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task AddAliasAsync_ReturnsFalse_WhenAliasAlreadyExists()
    {
        // Arrange
        var profileName = "target-profile";
        var aliasName = "existing-alias";
        await SetupTestProfile(profileName, AuthMethod.Console);
        await _profileManager.AddAliasAsync(aliasName, profileName);

        // Act
        var result = await _profileManager.AddAliasAsync(aliasName, profileName);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveAliasAsync_RemovesAlias_Successfully()
    {
        // Arrange
        var profileName = "target-profile";
        var aliasName = "alias-to-remove";
        await SetupTestProfile(profileName, AuthMethod.Console);
        await _profileManager.AddAliasAsync(aliasName, profileName);

        // Act
        var result = await _profileManager.RemoveAliasAsync(aliasName);

        // Assert
        result.Should().BeTrue();
        
        // Verify alias was removed
        var aliases = await _profileFileManager.LoadAliasesAsync();
        aliases.Should().NotContainKey(aliasName);
    }

    [Fact]
    public async Task RemoveAliasAsync_ReturnsFalse_WhenAliasNotFound()
    {
        // Act
        var result = await _profileManager.RemoveAliasAsync("nonexistent-alias");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ListAliasesAsync_ReturnsAllAliases_Successfully()
    {
        // Arrange
        await SetupTestProfile("profile1", AuthMethod.Console);
        await SetupTestProfile("profile2", AuthMethod.Console);
        await _profileManager.AddAliasAsync("alias1", "profile1");
        await _profileManager.AddAliasAsync("alias2", "profile2");

        // Act
        var result = await _profileManager.ListAliasesAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().ContainKey("alias1");
        result.Should().ContainKey("alias2");
        result["alias1"].Should().Be("profile1");
        result["alias2"].Should().Be("profile2");
    }

    [Fact]
    public async Task ResolveAliasAsync_ResolvesAlias_Successfully()
    {
        // Arrange
        var profileName = "actual-profile";
        var aliasName = "alias-name";
        await SetupTestProfile(profileName, AuthMethod.Console);
        await _profileManager.AddAliasAsync(aliasName, profileName);

        // Act
        var result = await _profileManager.ResolveAliasAsync(aliasName);

        // Assert
        result.Should().Be(profileName);
    }

    [Fact]
    public async Task ResolveAliasAsync_ReturnsOriginalName_WhenNotAlias()
    {
        // Arrange
        var profileName = "not-an-alias";

        // Act
        var result = await _profileManager.ResolveAliasAsync(profileName);

        // Assert
        result.Should().Be(profileName);
    }

    private async Task SetupTestProfile(string profileName, AuthMethod authMethod, string? credentials = null)
    {
        // Create profile metadata
        var profile = new Profile
        {
            Name = profileName,
            AuthMethod = authMethod,
            Created = DateTime.UtcNow,
            LastUsed = DateTime.UtcNow
        };
        
        await _profileFileManager.SaveProfileMetadataAsync(profile);

        // Store credentials if provided
        if (credentials != null)
        {
            var credentialKey = authMethod switch
            {
                AuthMethod.Console => $"{profileName}-console",
                AuthMethod.Subscription => $"{profileName}-subscription",
                _ => throw new ArgumentException($"Unsupported auth method: {authMethod}")
            };
            
            await _credentialStore.SaveCredentialAsync(credentialKey, credentials);
        }
    }

    private sealed class TestClaudeAuthDetector : IClaudeAuthDetector
    {
        private AuthMethod _currentAuthMethod = AuthMethod.None;
        private string? _currentCredentials;

        public string? ConsoleApiKey { get; private set; }
        public string? SubscriptionToken { get; private set; }

        public void SetCurrentAuth(AuthMethod authMethod, string? credentials)
        {
            _currentAuthMethod = authMethod;
            _currentCredentials = credentials;
        }

        public Task<AuthMethod> DetectCurrentAuthMethodAsync()
        {
            return Task.FromResult(_currentAuthMethod);
        }

        public Task<AuthMethod> DetectProfileAuthMethodAsync(string profileName)
        {
            return Task.FromResult(AuthMethod.Console); // Simplified for tests
        }

        public Task<string?> GetConsoleApiKeyAsync()
        {
            return Task.FromResult(_currentAuthMethod == AuthMethod.Console ? _currentCredentials : null);
        }

        public Task<string?> GetSubscriptionTokenAsync()
        {
            return Task.FromResult(_currentAuthMethod == AuthMethod.Subscription ? _currentCredentials : null);
        }

        public Task<bool> SaveConsoleApiKeyAsync(string apiKey)
        {
            ConsoleApiKey = apiKey;
            return Task.FromResult(true);
        }

        public Task<bool> SaveSubscriptionTokenAsync(string token)
        {
            SubscriptionToken = token;
            return Task.FromResult(true);
        }

        public Task<string> GetTokenHealthAsync(string token)
        {
            return Task.FromResult("valid");
        }

        public bool ValidateCredentialFormat(string credential, AuthMethod authMethod)
        {
            return !string.IsNullOrEmpty(credential);
        }
    }

    private sealed class TestLogger<T> : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            // Test logger - silent
        }
    }
}