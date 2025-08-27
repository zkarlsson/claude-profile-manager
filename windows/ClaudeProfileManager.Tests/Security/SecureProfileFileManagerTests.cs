using System.Runtime.Versioning;
using ClaudeProfileManager.Core.Models;
using ClaudeProfileManager.Windows.Security;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ClaudeProfileManager.Tests.Security;

[SupportedOSPlatform("windows")]
public class SecureProfileFileManagerTests : IDisposable
{
    private readonly SecureProfileFileManager _profileManager;
    private readonly WindowsAclManager _aclManager;
    private readonly ILogger<SecureProfileFileManager> _logger;
    private readonly string _testProfilesDirectory;

    public SecureProfileFileManagerTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => { });
        _logger = loggerFactory.CreateLogger<SecureProfileFileManager>();
        var aclLogger = loggerFactory.CreateLogger<WindowsAclManager>();
        _aclManager = new WindowsAclManager(aclLogger);
        
        // Use a unique test directory
        _testProfilesDirectory = Path.Combine(Path.GetTempPath(), "test-secure-profiles-" + Guid.NewGuid().ToString());
        _profileManager = new SecureProfileFileManager(_logger, _aclManager, _testProfilesDirectory);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new SecureProfileFileManager(null!, _aclManager, _testProfilesDirectory));
    }

    [Fact]
    public void Constructor_WithNullAclManager_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new SecureProfileFileManager(_logger, null!, _testProfilesDirectory));
    }

    [Fact]
    public void Constructor_ShouldCreateSecureDirectory()
    {
        // The constructor should have created the profiles directory
        Directory.Exists(_testProfilesDirectory).Should().BeTrue();
    }

    [Fact]
    public async Task SaveProfileAsync_WithValidProfile_ShouldReturnTrueAndSecureFile()
    {
        // Arrange
        var profileName = "test-profile";
        var profile = new Profile(profileName, AuthMethod.Console)
        {
            Created = DateTime.UtcNow,
            LastUsed = DateTime.UtcNow
        };

        // Act
        var result = await _profileManager.SaveProfileAsync(profileName, profile);

        // Assert
        result.Should().BeTrue();
        
        // Verify file exists and is secure
        var filePath = Path.Combine(_testProfilesDirectory, $"{profileName}.json");
        File.Exists(filePath).Should().BeTrue();
        _aclManager.IsFileSecure(filePath).Should().BeTrue();
    }

    [Fact]
    public async Task SaveProfileAsync_WithNullProfileName_ShouldThrowArgumentException()
    {
        // Arrange
        var profile = new Profile("test", AuthMethod.Console);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _profileManager.SaveProfileAsync(null!, profile));
    }

    [Fact]
    public async Task SaveProfileAsync_WithNullProfile_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _profileManager.SaveProfileAsync("test", null!));
    }

    [Fact]
    public async Task LoadProfileAsync_WithExistingProfile_ShouldReturnProfile()
    {
        // Arrange
        var profileName = "load-test-profile";
        var originalProfile = new Profile(profileName, AuthMethod.Console)
        {
            Created = DateTime.UtcNow,
            LastUsed = DateTime.UtcNow
        };

        await _profileManager.SaveProfileAsync(profileName, originalProfile);

        // Act
        var loadedProfile = await _profileManager.LoadProfileAsync(profileName);

        // Assert
        loadedProfile.Should().NotBeNull();
        loadedProfile!.Name.Should().Be(originalProfile.Name);
        loadedProfile.AuthMethod.Should().Be(originalProfile.AuthMethod);
    }

    [Fact]
    public async Task LoadProfileAsync_WithNonExistentProfile_ShouldReturnNull()
    {
        // Act
        var result = await _profileManager.LoadProfileAsync("non-existent-profile");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task LoadProfileAsync_WithNullProfileName_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _profileManager.LoadProfileAsync(null!));
    }

    [Fact]
    public async Task DeleteProfileAsync_WithExistingProfile_ShouldReturnTrueAndRemoveFile()
    {
        // Arrange
        var profileName = "delete-test-profile";
        var profile = new Profile(profileName, AuthMethod.Console);
        await _profileManager.SaveProfileAsync(profileName, profile);

        // Verify file exists before deletion
        var filePath = Path.Combine(_testProfilesDirectory, $"{profileName}.json");
        File.Exists(filePath).Should().BeTrue();

        // Act
        var result = await _profileManager.DeleteProfileAsync(profileName);

        // Assert
        result.Should().BeTrue();
        File.Exists(filePath).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteProfileAsync_WithNonExistentProfile_ShouldReturnTrue()
    {
        // Act
        var result = await _profileManager.DeleteProfileAsync("non-existent-profile");

        // Assert
        result.Should().BeTrue(); // Consider it successful if already gone
    }

    [Fact]
    public async Task ProfileExistsAsync_WithExistingProfile_ShouldReturnTrue()
    {
        // Arrange
        var profileName = "exists-test-profile";
        var profile = new Profile(profileName, AuthMethod.Console);
        await _profileManager.SaveProfileAsync(profileName, profile);

        // Act
        var result = await _profileManager.ProfileExistsAsync(profileName);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ProfileExistsAsync_WithNonExistentProfile_ShouldReturnFalse()
    {
        // Act
        var result = await _profileManager.ProfileExistsAsync("non-existent-profile");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ListProfilesAsync_WithMultipleProfiles_ShouldReturnAllProfiles()
    {
        // Arrange
        var profiles = new[]
        {
            new Profile("profile1", AuthMethod.Console),
            new Profile("profile2", AuthMethod.Console),
            new Profile("profile3", AuthMethod.Console)
        };

        foreach (var profile in profiles)
        {
            await _profileManager.SaveProfileAsync(profile.Name, profile);
        }

        // Act
        var result = await _profileManager.ListProfilesAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain("profile1");
        result.Should().Contain("profile2");
        result.Should().Contain("profile3");
    }

    [Fact]
    public async Task ListProfilesAsync_WithEmptyDirectory_ShouldReturnEmpty()
    {
        // Act
        var result = await _profileManager.ListProfilesAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SetCurrentProfileAsync_WithValidProfile_ShouldSaveCurrentFile()
    {
        // Arrange
        var profileName = "current-test-profile";

        // Act
        var result = await _profileManager.SetCurrentProfileAsync(profileName);

        // Assert
        result.Should().BeTrue();
        
        // Verify .current file exists and is secure
        var currentFilePath = Path.Combine(_testProfilesDirectory, ".current");
        File.Exists(currentFilePath).Should().BeTrue();
        _aclManager.IsFileSecure(currentFilePath).Should().BeTrue();
        
        // Verify content
        var currentProfile = await _profileManager.GetCurrentProfileAsync();
        currentProfile.Should().Be(profileName);
    }

    [Fact]
    public async Task SetCurrentProfileAsync_WithNullProfile_ShouldRemoveCurrentFile()
    {
        // Arrange - First set a current profile
        await _profileManager.SetCurrentProfileAsync("some-profile");
        var currentFilePath = Path.Combine(_testProfilesDirectory, ".current");
        File.Exists(currentFilePath).Should().BeTrue();

        // Act
        var result = await _profileManager.SetCurrentProfileAsync(null);

        // Assert
        result.Should().BeTrue();
        File.Exists(currentFilePath).Should().BeFalse();
    }

    [Fact]
    public async Task GetCurrentProfileAsync_WithNoCurrentFile_ShouldReturnNull()
    {
        // Act
        var result = await _profileManager.GetCurrentProfileAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SaveProfileAsync_ShouldUseAtomicWrite()
    {
        // Arrange
        var profileName = "atomic-test-profile";
        var profile = new Profile(profileName, AuthMethod.Console);

        // Act
        var result = await _profileManager.SaveProfileAsync(profileName, profile);

        // Assert
        result.Should().BeTrue();
        
        // Verify no temporary files remain
        var profileFiles = Directory.GetFiles(_testProfilesDirectory, "*.tmp");
        profileFiles.Should().BeEmpty("No temporary files should remain after atomic write");
    }

    [Fact]
    public async Task LoadProfileAsync_WithInsecureFile_ShouldSecureFileAutomatically()
    {
        // Arrange
        var profileName = "insecure-test-profile";
        var profile = new Profile(profileName, AuthMethod.Console);
        
        // Save profile normally first
        await _profileManager.SaveProfileAsync(profileName, profile);
        
        var filePath = Path.Combine(_testProfilesDirectory, $"{profileName}.json");
        
        // Manually make the file insecure by adding permissions for Everyone
        var fileInfo = new FileInfo(filePath);
        var fileSecurity = fileInfo.GetAccessControl();
        var everyoneSid = new System.Security.Principal.SecurityIdentifier(
            System.Security.Principal.WellKnownSidType.WorldSid, null);
        var rule = new System.Security.AccessControl.FileSystemAccessRule(
            everyoneSid, 
            System.Security.AccessControl.FileSystemRights.Read, 
            System.Security.AccessControl.AccessControlType.Allow);
        fileSecurity.AddAccessRule(rule);
        fileInfo.SetAccessControl(fileSecurity);

        // Verify file is now insecure
        _aclManager.IsFileSecure(filePath).Should().BeFalse("File should be insecure before loading");

        // Act
        var loadedProfile = await _profileManager.LoadProfileAsync(profileName);

        // Assert
        loadedProfile.Should().NotBeNull();
        
        // File should be secured automatically during load
        _aclManager.IsFileSecure(filePath).Should().BeTrue("File should be secured automatically during load");
    }

    [Fact]
    public async Task SaveProfileAsync_WithInvalidProfileName_ShouldThrowArgumentException()
    {
        // Arrange
        var profile = new Profile("test", AuthMethod.Console);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _profileManager.SaveProfileAsync("../invalid-path", profile));
        
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _profileManager.SaveProfileAsync("con", profile)); // Reserved Windows name
    }

    public void Dispose()
    {
        // Clean up test directory
        try
        {
            if (Directory.Exists(_testProfilesDirectory))
            {
                Directory.Delete(_testProfilesDirectory, recursive: true);
            }
        }
        catch
        {
            // Best effort cleanup
        }
        
        GC.SuppressFinalize(this);
    }
}