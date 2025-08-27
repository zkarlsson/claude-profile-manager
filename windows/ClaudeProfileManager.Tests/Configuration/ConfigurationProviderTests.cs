using ClaudeProfileManager.Core.Configuration;
using FluentAssertions;
using Xunit;

namespace ClaudeProfileManager.Tests.Configuration;

public class ConfigurationProviderTests : IDisposable
{
    private readonly string _testConfigPath;

    public ConfigurationProviderTests()
    {
        _testConfigPath = Path.GetTempFileName();
        // Ensure we start with default configuration
        ConfigurationProvider.ResetToDefault();
    }

    public void Dispose()
    {
        ConfigurationProvider.ResetToDefault();
        if (File.Exists(_testConfigPath))
        {
            File.Delete(_testConfigPath);
        }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Current_WithDefaultConfiguration_ReturnsExpectedDefaults()
    {
        // Act
        var config = ConfigurationProvider.Current;

        // Assert
        config.Should().NotBeNull();
        config.Buffers.JsonFileBufferSize.Should().Be(16 * 1024);
        config.Buffers.TextFileBufferSize.Should().Be(4 * 1024);
        config.Performance.DefaultJsonSizeDevelopment.Should().Be(1024);
        config.Performance.DefaultJsonSizeProduction.Should().Be(512);
        config.Security.AutoCleanupCredentials.Should().BeTrue();
        config.Logging.EnableStructuredLogging.Should().BeTrue();
    }

    [Fact]
    public void SetConfiguration_WithCustomConfig_UpdatesCurrent()
    {
        // Arrange
        var customConfig = new ApplicationConfiguration
        {
            Buffers = { JsonFileBufferSize = 32 * 1024 },
            Performance = { DefaultJsonSizeDevelopment = 2048 },
            Security = { AutoCleanupCredentials = false }
        };

        // Act
        ConfigurationProvider.SetConfiguration(customConfig);

        // Assert
        var current = ConfigurationProvider.Current;
        current.Buffers.JsonFileBufferSize.Should().Be(32 * 1024);
        current.Performance.DefaultJsonSizeDevelopment.Should().Be(2048);
        current.Security.AutoCleanupCredentials.Should().BeFalse();
    }

    [Fact]
    public void ResetToDefault_AfterCustomConfiguration_RestoresDefaults()
    {
        // Arrange
        var customConfig = new ApplicationConfiguration
        {
            Buffers = { JsonFileBufferSize = 8 * 1024 }
        };
        ConfigurationProvider.SetConfiguration(customConfig);

        // Act
        ConfigurationProvider.ResetToDefault();

        // Assert
        ConfigurationProvider.Current.Buffers.JsonFileBufferSize.Should().Be(16 * 1024);
    }

    [Fact]
    public async Task TrySaveToFileAsync_WithValidPath_SavesSuccessfully()
    {
        // Arrange
        var customConfig = new ApplicationConfiguration
        {
            Buffers = { JsonFileBufferSize = 64 * 1024 }
        };
        ConfigurationProvider.SetConfiguration(customConfig);

        // Act
        var result = await ConfigurationProvider.TrySaveToFileAsync(_testConfigPath);

        // Assert
        result.Should().BeTrue();
        File.Exists(_testConfigPath).Should().BeTrue();
        
        var fileContent = await File.ReadAllTextAsync(_testConfigPath);
        fileContent.Should().Contain("65536"); // 64 * 1024
    }

    [Fact]
    public async Task TryLoadFromFileAsync_WithValidFile_LoadsSuccessfully()
    {
        // Arrange
        var originalConfig = new ApplicationConfiguration
        {
            Buffers = { JsonFileBufferSize = 128 * 1024 },
            Performance = { DefaultJsonSizeDevelopment = 4096 }
        };
        ConfigurationProvider.SetConfiguration(originalConfig);
        await ConfigurationProvider.TrySaveToFileAsync(_testConfigPath);
        
        // Reset to default to test loading
        ConfigurationProvider.ResetToDefault();
        ConfigurationProvider.Current.Buffers.JsonFileBufferSize.Should().Be(16 * 1024); // Verify reset

        // Act
        var result = await ConfigurationProvider.TryLoadFromFileAsync(_testConfigPath);

        // Assert
        result.Should().BeTrue();
        ConfigurationProvider.Current.Buffers.JsonFileBufferSize.Should().Be(128 * 1024);
        ConfigurationProvider.Current.Performance.DefaultJsonSizeDevelopment.Should().Be(4096);
    }

    [Fact]
    public async Task TryLoadFromFileAsync_WithNonExistentFile_ReturnsFalse()
    {
        // Act
        var result = await ConfigurationProvider.TryLoadFromFileAsync("non-existent-file.json");

        // Assert
        result.Should().BeFalse();
        // Configuration should remain unchanged
        ConfigurationProvider.Current.Buffers.JsonFileBufferSize.Should().Be(16 * 1024);
    }

    [Fact]
    public void GetDefaultConfigFilePath_ReturnsValidPath()
    {
        // Act
        var configPath = ConfigurationProvider.GetDefaultConfigFilePath();

        // Assert
        configPath.Should().NotBeNullOrEmpty();
        configPath.Should().EndWith("config.json");
        Path.IsPathRooted(configPath).Should().BeTrue();
    }
}