using System.Runtime.Versioning;
using ClaudeProfileManager.Core.Models;
using ClaudeProfileManager.Windows;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ClaudeProfileManager.Tests.Windows;

[SupportedOSPlatform("windows")]
public class WindowsClaudeAuthDetectorTests
{
    private readonly WindowsClaudeAuthDetector _detector;
    private readonly TestCredentialStore _claudeCredentialStore;
    private readonly TestCredentialStore _profileManagerCredentialStore;
    private readonly ILogger<WindowsClaudeAuthDetector> _logger;

    public WindowsClaudeAuthDetectorTests()
    {
        _logger = new TestLogger<WindowsClaudeAuthDetector>();
        _claudeCredentialStore = new TestCredentialStore();
        _profileManagerCredentialStore = new TestCredentialStore();
        
        _detector = new WindowsClaudeAuthDetector(
            _logger,
            _claudeCredentialStore,
            _profileManagerCredentialStore);
    }

    [Fact]
    public async Task DetectCurrentAuthMethodAsync_ReturnsConsole_WhenConsoleApiKeyExists()
    {
        // Arrange
        var apiKey = "sk-ant-api03-" + new string('A', 95);
        await _claudeCredentialStore.SaveCredentialAsync("", apiKey, "Claude Code");

        // Act
        var result = await _detector.DetectCurrentAuthMethodAsync();

        // Assert
        result.Should().Be(AuthMethod.Console);
    }

    [Fact]
    public async Task DetectCurrentAuthMethodAsync_ReturnsSubscription_WhenSubscriptionTokenExists()
    {
        // Arrange
        var token = """{"claudeAiOauth": {"expiresAt": 1735689600000}}""";
        await _claudeCredentialStore.SaveCredentialAsync("", token, "Claude Code-credentials");

        // Act
        var result = await _detector.DetectCurrentAuthMethodAsync();

        // Assert
        result.Should().Be(AuthMethod.Subscription);
    }

    [Fact]
    public async Task DetectCurrentAuthMethodAsync_ReturnsConsole_WhenBothExist()
    {
        // Arrange - Console takes precedence
        var apiKey = "sk-ant-api03-" + new string('A', 95);
        var token = """{"claudeAiOauth": {"expiresAt": 1735689600000}}""";
        await _claudeCredentialStore.SaveCredentialAsync("", apiKey, "Claude Code");
        await _claudeCredentialStore.SaveCredentialAsync("", token, "Claude Code-credentials");

        // Act
        var result = await _detector.DetectCurrentAuthMethodAsync();

        // Assert
        result.Should().Be(AuthMethod.Console);
    }

    [Fact]
    public async Task DetectCurrentAuthMethodAsync_ReturnsNone_WhenNoCredentialsExist()
    {
        // Act
        var result = await _detector.DetectCurrentAuthMethodAsync();

        // Assert
        result.Should().Be(AuthMethod.None);
    }

    [Fact]
    public async Task DetectProfileAuthMethodAsync_ReturnsConsole_WhenProfileHasConsoleKey()
    {
        // Arrange
        var apiKey = "sk-ant-api03-" + new string('B', 95);
        await _profileManagerCredentialStore.SaveCredentialAsync("work-console", apiKey);

        // Act
        var result = await _detector.DetectProfileAuthMethodAsync("work");

        // Assert
        result.Should().Be(AuthMethod.Console);
    }

    [Fact]
    public async Task DetectProfileAuthMethodAsync_ReturnsSubscription_WhenProfileHasToken()
    {
        // Arrange
        var token = """{"claudeAiOauth": {"expiresAt": 1735689600000}}""";
        await _profileManagerCredentialStore.SaveCredentialAsync("personal-subscription", token);

        // Act
        var result = await _detector.DetectProfileAuthMethodAsync("personal");

        // Assert
        result.Should().Be(AuthMethod.Subscription);
    }

    [Fact]
    public async Task DetectProfileAuthMethodAsync_ReturnsNone_WhenProfileHasNoCredentials()
    {
        // Act
        var result = await _detector.DetectProfileAuthMethodAsync("nonexistent");

        // Assert
        result.Should().Be(AuthMethod.None);
    }

    [Fact]
    public async Task DetectProfileAuthMethodAsync_ReturnsNone_WhenProfileNameIsEmpty()
    {
        // Act
        var result = await _detector.DetectProfileAuthMethodAsync("");

        // Assert
        result.Should().Be(AuthMethod.None);
    }

    [Fact]
    public async Task GetConsoleApiKeyAsync_ReturnsApiKey_WhenExists()
    {
        // Arrange
        var expectedKey = "sk-ant-api03-" + new string('C', 95);
        await _claudeCredentialStore.SaveCredentialAsync("", expectedKey, "Claude Code");

        // Act
        var result = await _detector.GetConsoleApiKeyAsync();

        // Assert
        result.Should().Be(expectedKey);
    }

    [Fact]
    public async Task GetConsoleApiKeyAsync_ReturnsNull_WhenNotExists()
    {
        // Act
        var result = await _detector.GetConsoleApiKeyAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSubscriptionTokenAsync_ReturnsToken_WhenExists()
    {
        // Arrange
        var expectedToken = """{"claudeAiOauth": {"expiresAt": 1735689600000}}""";
        await _claudeCredentialStore.SaveCredentialAsync("", expectedToken, "Claude Code-credentials");

        // Act
        var result = await _detector.GetSubscriptionTokenAsync();

        // Assert
        result.Should().Be(expectedToken);
    }

    [Fact]
    public async Task GetSubscriptionTokenAsync_ReturnsNull_WhenNotExists()
    {
        // Act
        var result = await _detector.GetSubscriptionTokenAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SaveConsoleApiKeyAsync_SavesApiKey_WhenValid()
    {
        // Arrange
        var apiKey = "sk-ant-api03-" + new string('D', 95);

        // Act
        var result = await _detector.SaveConsoleApiKeyAsync(apiKey);

        // Assert
        result.Should().BeTrue();
        var stored = await _claudeCredentialStore.GetCredentialAsync("", "Claude Code");
        stored.Should().Be(apiKey);
    }

    [Fact]
    public async Task SaveConsoleApiKeyAsync_ReturnsFalse_WhenInvalidFormat()
    {
        // Arrange
        var invalidKey = "invalid-api-key";

        // Act
        var result = await _detector.SaveConsoleApiKeyAsync(invalidKey);

        // Assert
        result.Should().BeFalse();
        var stored = await _claudeCredentialStore.GetCredentialAsync("", "Claude Code");
        stored.Should().BeNull();
    }

    [Fact]
    public async Task SaveConsoleApiKeyAsync_ReturnsFalse_WhenEmpty()
    {
        // Act
        var result = await _detector.SaveConsoleApiKeyAsync("");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SaveSubscriptionTokenAsync_SavesToken_WhenValid()
    {
        // Arrange
        var token = """{"claudeAiOauth": {"expiresAt": 1735689600000}}""";

        // Act
        var result = await _detector.SaveSubscriptionTokenAsync(token);

        // Assert
        result.Should().BeTrue();
        var stored = await _claudeCredentialStore.GetCredentialAsync("", "Claude Code-credentials");
        stored.Should().Be(token);
    }

    [Fact]
    public async Task SaveSubscriptionTokenAsync_ReturnsFalse_WhenInvalidFormat()
    {
        // Arrange
        var invalidToken = """{"invalid": "format"}""";

        // Act
        var result = await _detector.SaveSubscriptionTokenAsync(invalidToken);

        // Assert
        result.Should().BeFalse();
        var stored = await _claudeCredentialStore.GetCredentialAsync("", "Claude Code-credentials");
        stored.Should().BeNull();
    }

    [Theory]
    [InlineData("sk-ant-api03-ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyz_-ABCDEFGHIJKLMNOPQRSTUV", true)] // Exact length (95 chars after prefix)
    [InlineData("sk-ant-api03-A", false)] // Too short
    [InlineData("sk-ant-api04-" + "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyz_-ABCDEFGHIJKLMNOPQRSTUV", false)] // Wrong prefix
    [InlineData("invalid-key", false)] // Completely wrong format
    public void ValidateCredentialFormat_Console_ValidatesCorrectly(string credential, bool expected)
    {
        // Act
        var result = _detector.ValidateCredentialFormat(credential, AuthMethod.Console);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("""{"claudeAiOauth": {"expiresAt": 1735689600000}}""", true)]
    [InlineData("""{"claudeAiOauth": {}}""", true)]
    [InlineData("""{"invalid": "format"}""", false)]
    [InlineData("not-json", false)]
    [InlineData("", false)]
    public void ValidateCredentialFormat_Subscription_ValidatesCorrectly(string credential, bool expected)
    {
        // Act
        var result = _detector.ValidateCredentialFormat(credential, AuthMethod.Subscription);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ValidateCredentialFormat_None_ReturnsFalse()
    {
        // Act
        var result = _detector.ValidateCredentialFormat("any-credential", AuthMethod.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetTokenHealthAsync_ReturnsExpiredMessage_WhenTokenExpired()
    {
        // Arrange - Token expired 2 hours ago
        var expiredTime = DateTimeOffset.UtcNow.AddHours(-2).ToUnixTimeMilliseconds();
        var token = "{\"claudeAiOauth\": {\"expiresAt\": " + expiredTime + "}}";

        // Act
        var result = await _detector.GetTokenHealthAsync(token);

        // Assert
        result.Should().Contain("expired");
        result.Should().MatchRegex(@"\d+h");
    }

    [Fact]
    public async Task GetTokenHealthAsync_ReturnsExpiresInMessage_WhenTokenValid()
    {
        // Arrange - Token expires in 5 days
        var futureTime = DateTimeOffset.UtcNow.AddDays(5).ToUnixTimeMilliseconds();
        var token = "{\"claudeAiOauth\": {\"expiresAt\": " + futureTime + "}}";

        // Act
        var result = await _detector.GetTokenHealthAsync(token);

        // Assert
        result.Should().Contain("expires in");
        result.Should().MatchRegex(@"\d+d");
    }

    [Fact]
    public async Task GetTokenHealthAsync_ReturnsExpiresSoon_WhenTokenExpiresSoon()
    {
        // Arrange - Token expires in 30 minutes
        var soonTime = DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeMilliseconds();
        var token = "{\"claudeAiOauth\": {\"expiresAt\": " + soonTime + "}}";

        // Act
        var result = await _detector.GetTokenHealthAsync(token);

        // Assert
        result.Should().Contain("expires soon");
        result.Should().MatchRegex(@"\d+m");
    }

    [Fact]
    public async Task GetTokenHealthAsync_ReturnsValid_WhenNoExpirationInfo()
    {
        // Arrange
        var token = """{"claudeAiOauth": {"accessToken": "token"}}""";

        // Act
        var result = await _detector.GetTokenHealthAsync(token);

        // Assert
        result.Should().Be("valid");
    }

    [Fact]
    public async Task GetTokenHealthAsync_ReturnsInvalidFormat_WhenNotOAuthToken()
    {
        // Arrange
        var token = """{"invalid": "format"}""";

        // Act
        var result = await _detector.GetTokenHealthAsync(token);

        // Assert
        result.Should().Be("invalid format");
    }

    [Fact]
    public async Task GetTokenHealthAsync_ReturnsInvalidJson_WhenNotJson()
    {
        // Arrange
        var token = "not-json-at-all";

        // Act
        var result = await _detector.GetTokenHealthAsync(token);

        // Assert
        result.Should().Be("invalid json");
    }

    [Fact]
    public async Task GetTokenHealthAsync_ReturnsInvalid_WhenTokenIsEmpty()
    {
        // Act
        var result = await _detector.GetTokenHealthAsync("");

        // Assert
        result.Should().Be("invalid");
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