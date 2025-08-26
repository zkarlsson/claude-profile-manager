using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using ClaudeProfileManager.Core.Models;
using ClaudeProfileManager.Windows;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ClaudeProfileManager.Tests.Windows;

[SupportedOSPlatform("windows")]
public class WindowsProfileFileManagerTests : IDisposable
{
    private readonly WindowsProfileFileManager _manager;
    private readonly ILogger<WindowsProfileFileManager> _logger;
    private readonly string _testDirectory;

    public WindowsProfileFileManagerTests()
    {
        _logger = new TestLogger<WindowsProfileFileManager>();
        _testDirectory = Path.Combine(Path.GetTempPath(), $"ClaudeProfileTest_{Guid.NewGuid()}");
        _manager = new WindowsProfileFileManager(_logger, _testDirectory);
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
            // Ignore cleanup errors in tests
        }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task EnsureProfilesDirectoryAsync_CreatesDirectory_WhenNotExists()
    {
        // Act
        var result = await _manager.EnsureProfilesDirectoryAsync();

        // Assert
        result.Should().BeTrue();
        Directory.Exists(_testDirectory).Should().BeTrue();
    }

    [Fact]
    public async Task EnsureProfilesDirectoryAsync_SetsSecureACLs_OnCreation()
    {
        // Act
        var result = await _manager.EnsureProfilesDirectoryAsync();

        // Assert
        result.Should().BeTrue();
        
        var directoryInfo = new DirectoryInfo(_testDirectory);
        var security = directoryInfo.GetAccessControl();
        var rules = security.GetAccessRules(true, true, typeof(SecurityIdentifier));
        
        // Should have exactly one rule for current user
        rules.Count.Should().Be(1);
        
        var currentUser = WindowsIdentity.GetCurrent().User;
        var rule = rules.Cast<FileSystemAccessRule>().First();
        rule.IdentityReference.Should().Be(currentUser);
        rule.FileSystemRights.Should().HaveFlag(FileSystemRights.FullControl);
    }

    [Fact]
    public async Task SaveProfileMetadataAsync_SavesProfile_WithCorrectFormat()
    {
        // Arrange
        var profile = new Profile
        {
            Name = "test-profile",
            AuthMethod = AuthMethod.Console,
            Created = DateTime.UtcNow,
            LastUsed = DateTime.UtcNow
        };

        // Act
        var result = await _manager.SaveProfileMetadataAsync(profile);

        // Assert
        result.Should().BeTrue();
        
        var profilePath = Path.Combine(_testDirectory, "test-profile.json");
        File.Exists(profilePath).Should().BeTrue();
        
        var json = await File.ReadAllTextAsync(profilePath);
        json.Should().Contain("\"name\": \"test-profile\"");
        json.Should().Contain("\"auth_method\": \"Console\"");
    }

    [Fact]
    public async Task SaveProfileMetadataAsync_SetsSecurePermissions_OnFile()
    {
        // Arrange
        var profile = new Profile
        {
            Name = "secure-profile",
            AuthMethod = AuthMethod.Subscription,
            Created = DateTime.UtcNow,
            LastUsed = DateTime.UtcNow
        };

        // Act
        var result = await _manager.SaveProfileMetadataAsync(profile);

        // Assert
        result.Should().BeTrue();
        
        var profilePath = Path.Combine(_testDirectory, "secure-profile.json");
        var fileInfo = new FileInfo(profilePath);
        var security = fileInfo.GetAccessControl();
        var rules = security.GetAccessRules(true, true, typeof(SecurityIdentifier));
        
        // Should have exactly one rule for current user
        rules.Count.Should().Be(1);
        
        var currentUser = WindowsIdentity.GetCurrent().User;
        var rule = rules.Cast<FileSystemAccessRule>().First();
        rule.IdentityReference.Should().Be(currentUser);
        rule.FileSystemRights.Should().HaveFlag(FileSystemRights.ReadAndExecute);
        rule.FileSystemRights.Should().HaveFlag(FileSystemRights.Write);
    }

    [Fact]
    public async Task LoadProfileMetadataAsync_LoadsSavedProfile_Correctly()
    {
        // Arrange
        var originalProfile = new Profile
        {
            Name = "load-test",
            AuthMethod = AuthMethod.Console,
            Created = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            LastUsed = new DateTime(2025, 1, 2, 14, 30, 0, DateTimeKind.Utc)
        };
        await _manager.SaveProfileMetadataAsync(originalProfile);

        // Act
        var loadedProfile = await _manager.LoadProfileMetadataAsync("load-test");

        // Assert
        loadedProfile.Should().NotBeNull();
        loadedProfile!.Name.Should().Be("load-test");
        loadedProfile.AuthMethod.Should().Be(AuthMethod.Console);
        loadedProfile.Created.Should().Be(originalProfile.Created);
        loadedProfile.LastUsed.Should().Be(originalProfile.LastUsed);
    }

    [Fact]
    public async Task LoadProfileMetadataAsync_ReturnsNull_WhenProfileNotFound()
    {
        // Act
        var profile = await _manager.LoadProfileMetadataAsync("nonexistent");

        // Assert
        profile.Should().BeNull();
    }

    [Fact]
    public async Task DeleteProfileMetadataAsync_DeletesProfile_Successfully()
    {
        // Arrange
        var profile = new Profile
        {
            Name = "delete-test",
            AuthMethod = AuthMethod.Subscription,
            Created = DateTime.UtcNow,
            LastUsed = DateTime.UtcNow
        };
        await _manager.SaveProfileMetadataAsync(profile);
        
        var profilePath = Path.Combine(_testDirectory, "delete-test.json");
        File.Exists(profilePath).Should().BeTrue();

        // Act
        var result = await _manager.DeleteProfileMetadataAsync("delete-test");

        // Assert
        result.Should().BeTrue();
        File.Exists(profilePath).Should().BeFalse();
    }

    [Fact]
    public async Task ListProfilesAsync_ReturnsAllProfiles_ExcludingHiddenFiles()
    {
        // Arrange
        await _manager.SaveProfileMetadataAsync(new Profile { Name = "profile1", AuthMethod = AuthMethod.Console });
        await _manager.SaveProfileMetadataAsync(new Profile { Name = "profile2", AuthMethod = AuthMethod.Subscription });
        await _manager.SaveProfileMetadataAsync(new Profile { Name = "profile3", AuthMethod = AuthMethod.None });
        
        // Create a hidden file that should be ignored
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, ".hidden.json"), "{}");

        // Act
        var profiles = await _manager.ListProfilesAsync();

        // Assert
        profiles.Should().HaveCount(3);
        profiles.Should().Contain("profile1");
        profiles.Should().Contain("profile2");
        profiles.Should().Contain("profile3");
        profiles.Should().NotContain(".hidden");
    }

    [Fact]
    public async Task SaveCurrentProfileAsync_SavesCurrentProfile_Successfully()
    {
        // Arrange
        await _manager.EnsureProfilesDirectoryAsync();

        // Act
        var result = await _manager.SaveCurrentProfileAsync("work-profile");

        // Assert
        result.Should().BeTrue();
        
        var currentPath = Path.Combine(_testDirectory, ".current");
        File.Exists(currentPath).Should().BeTrue();
        
        var content = await File.ReadAllTextAsync(currentPath);
        content.Should().Be("work-profile");
    }

    [Fact]
    public async Task GetCurrentProfileAsync_ReturnsCurrentProfile_WhenSet()
    {
        // Arrange
        await _manager.SaveCurrentProfileAsync("active-profile");

        // Act
        var currentProfile = await _manager.GetCurrentProfileAsync();

        // Assert
        currentProfile.Should().Be("active-profile");
    }

    [Fact]
    public async Task GetCurrentProfileAsync_ReturnsNull_WhenNotSet()
    {
        // Act
        var currentProfile = await _manager.GetCurrentProfileAsync();

        // Assert
        currentProfile.Should().BeNull();
    }

    [Fact]
    public async Task SaveAndLoadAliases_WorksCorrectly()
    {
        // Arrange
        var aliases = new Dictionary<string, string>
        {
            { "w", "work" },
            { "p", "personal" },
            { "api", "console" }
        };

        // Act - Save
        var saveResult = await _manager.SaveAliasesAsync(aliases);
        
        // Act - Load
        var loadedAliases = await _manager.LoadAliasesAsync();

        // Assert
        saveResult.Should().BeTrue();
        loadedAliases.Should().HaveCount(3);
        loadedAliases["w"].Should().Be("work");
        loadedAliases["p"].Should().Be("personal");
        loadedAliases["api"].Should().Be("console");
    }

    [Fact]
    public async Task LoadAliasesAsync_ReturnsEmptyDictionary_WhenNoAliasesFile()
    {
        // Act
        var aliases = await _manager.LoadAliasesAsync();

        // Assert
        aliases.Should().NotBeNull();
        aliases.Should().BeEmpty();
    }

    [Fact]
    public async Task AtomicOperations_PreventPartialWrites()
    {
        // Arrange
        var profile = new Profile
        {
            Name = "atomic-test",
            AuthMethod = AuthMethod.Console,
            Created = DateTime.UtcNow,
            LastUsed = DateTime.UtcNow
        };

        // Act
        var result = await _manager.SaveProfileMetadataAsync(profile);

        // Assert
        result.Should().BeTrue();
        
        // Temp file should not exist after successful save
        var tempPath = Path.Combine(_testDirectory, "atomic-test.json.tmp");
        File.Exists(tempPath).Should().BeFalse();
        
        // Actual file should exist
        var profilePath = Path.Combine(_testDirectory, "atomic-test.json");
        File.Exists(profilePath).Should().BeTrue();
    }

    private sealed class TestLogger<T> : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            // Test logger - outputs to test output if needed
        }
    }
}