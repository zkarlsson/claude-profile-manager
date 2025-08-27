using System.Runtime.Versioning;
using System.Text.Json;
using ClaudeProfileManager.Core.IO;
using ClaudeProfileManager.Core.Models;
using FluentAssertions;
using Xunit;

namespace ClaudeProfileManager.Tests.IO;

[SupportedOSPlatform("windows")]
public class OptimizedFileOperationsTests : IDisposable
{
    private readonly string _testDirectory;

    public OptimizedFileOperationsTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"OptimizedFileOperationsTests_{Guid.NewGuid():N}");
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
        
        // Clear caches after tests
        OptimizedFileOperations.ClearDirectoryCache();
        OptimizedFileOperations.ClearSecurityCache();
        
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void DirectoryExistsCached_WithExistingDirectory_ReturnsTrue()
    {
        // Arrange
        Directory.CreateDirectory(_testDirectory);

        // Act
        var result = OptimizedFileOperations.DirectoryExistsCached(_testDirectory);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void DirectoryExistsCached_WithNonExistentDirectory_ReturnsFalse()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDirectory, "nonexistent");

        // Act
        var result = OptimizedFileOperations.DirectoryExistsCached(nonExistentPath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CreateSecureDirectory_CreatesDirectoryWithSecurePermissions()
    {
        // Act
        var directoryInfo = OptimizedFileOperations.CreateSecureDirectory(_testDirectory);

        // Assert
        directoryInfo.Should().NotBeNull();
        Directory.Exists(_testDirectory).Should().BeTrue();
        
        // Verify cached
        OptimizedFileOperations.DirectoryExistsCached(_testDirectory).Should().BeTrue();
    }

    [Fact]
    public async Task WriteJsonAtomicAsync_WithValidData_WritesFileSecurely()
    {
        // Arrange
        Directory.CreateDirectory(_testDirectory);
        var testProfile = new Profile("test-profile", AuthMethod.Subscription);
        var filePath = Path.Combine(_testDirectory, "test-profile.json");
        var jsonOptions = new JsonSerializerOptions { WriteIndented = true };

        // Act
        var result = await OptimizedFileOperations.WriteJsonAtomicAsync(filePath, testProfile, jsonOptions);

        // Assert
        result.Should().BeTrue();
        File.Exists(filePath).Should().BeTrue();
        
        // Verify content
        var content = await File.ReadAllTextAsync(filePath);
        content.Should().Contain("test-profile");
    }

    [Fact]
    public async Task ReadJsonAsync_WithValidFile_ReturnsDeserializedObject()
    {
        // Arrange
        Directory.CreateDirectory(_testDirectory);
        var testProfile = new Profile("test-profile", AuthMethod.Subscription);
        var filePath = Path.Combine(_testDirectory, "test-profile.json");
        var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
        
        await OptimizedFileOperations.WriteJsonAtomicAsync(filePath, testProfile, jsonOptions);

        // Act
        var result = await OptimizedFileOperations.ReadJsonAsync<Profile>(filePath, jsonOptions);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("test-profile");
        result.AuthMethod.Should().Be(AuthMethod.Subscription);
    }

    [Fact]
    public async Task ReadJsonAsync_WithNonExistentFile_ReturnsNull()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "nonexistent.json");
        var jsonOptions = new JsonSerializerOptions();

        // Act
        var result = await OptimizedFileOperations.ReadJsonAsync<Profile>(filePath, jsonOptions);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task WriteTextAtomicAsync_WithValidContent_WritesFileSecurely()
    {
        // Arrange
        Directory.CreateDirectory(_testDirectory);
        var content = "test-content";
        var filePath = Path.Combine(_testDirectory, "test.txt");

        // Act
        await OptimizedFileOperations.WriteTextAtomicAsync(filePath, content);

        // Assert
        File.Exists(filePath).Should().BeTrue();
        var readContent = await File.ReadAllTextAsync(filePath);
        readContent.Should().Be(content);
    }

    [Fact]
    public async Task ReadTextAsync_WithValidFile_ReturnsContent()
    {
        // Arrange
        Directory.CreateDirectory(_testDirectory);
        var content = "test-content";
        var filePath = Path.Combine(_testDirectory, "test.txt");
        await File.WriteAllTextAsync(filePath, content);

        // Act
        var result = await OptimizedFileOperations.ReadTextAsync(filePath);

        // Assert
        result.Should().Be(content);
    }

    [Fact]
    public async Task ReadTextAsync_WithNonExistentFile_ReturnsNull()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "nonexistent.txt");

        // Act
        var result = await OptimizedFileOperations.ReadTextAsync(filePath);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void EnumerateJsonFiles_WithJsonFiles_ReturnsFileNames()
    {
        // Arrange
        Directory.CreateDirectory(_testDirectory);
        File.WriteAllText(Path.Combine(_testDirectory, "profile1.json"), "{}");
        File.WriteAllText(Path.Combine(_testDirectory, "profile2.json"), "{}");
        File.WriteAllText(Path.Combine(_testDirectory, ".hidden.json"), "{}"); // Should be skipped
        File.WriteAllText(Path.Combine(_testDirectory, "notjson.txt"), "{}"); // Should be skipped

        // Act
        var result = OptimizedFileOperations.EnumerateJsonFiles(_testDirectory).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain("profile1");
        result.Should().Contain("profile2");
        result.Should().NotContain(".hidden");
        result.Should().NotContain("notjson");
    }

    [Fact]
    public void GetSecureFileSecurity_ReturnsValidSecurityDescriptor()
    {
        // Act
        var security = OptimizedFileOperations.GetSecureFileSecurity();

        // Assert
        security.Should().NotBeNull();
    }

    [Fact]
    public void ClearCaches_ClearsAllCaches()
    {
        // Arrange - populate caches
        OptimizedFileOperations.DirectoryExistsCached(_testDirectory);
        OptimizedFileOperations.GetSecureFileSecurity();

        // Act
        OptimizedFileOperations.ClearDirectoryCache();
        OptimizedFileOperations.ClearSecurityCache();

        // Assert - should not throw and should work normally
        var result = OptimizedFileOperations.DirectoryExistsCached(_testDirectory);
        var security = OptimizedFileOperations.GetSecureFileSecurity();
        
        result.Should().BeFalse(); // Directory doesn't exist
        security.Should().NotBeNull();
    }
}