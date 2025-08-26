using System.Runtime.Versioning;
using System.Text.Json;
using System.Text.RegularExpressions;
using ClaudeProfileManager.Core;
using ClaudeProfileManager.Core.Interfaces;
using ClaudeProfileManager.Core.Models;
using Microsoft.Extensions.Logging;

namespace ClaudeProfileManager.Windows;

[SupportedOSPlatform("windows")]
public partial class WindowsClaudeAuthDetector : IClaudeAuthDetector
{
    private readonly ICredentialStore _claudeCredentialStore;
    private readonly ICredentialStore _profileManagerCredentialStore;
    private readonly ILogger<WindowsClaudeAuthDetector> _logger;

    public WindowsClaudeAuthDetector(
        ILogger<WindowsClaudeAuthDetector> logger,
        ICredentialStore? claudeCredentialStore = null,
        ICredentialStore? profileManagerCredentialStore = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Use provided credential stores or create new ones
        _claudeCredentialStore = claudeCredentialStore ?? 
            CreateDefaultCredentialStore();
        
        _profileManagerCredentialStore = profileManagerCredentialStore ??
            CreateDefaultCredentialStore();
    }

    private static WindowsCredentialStore CreateDefaultCredentialStore()
    {
        // Create a null logger for the credential store to avoid dependency issues
        var credentialStoreLogger = new NullLogger<WindowsCredentialStore>();
        return new WindowsCredentialStore(credentialStoreLogger);
    }

    private sealed class NullLogger<T> : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => false;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            // Do nothing - null logger
        }
    }

    public async Task<AuthMethod> DetectCurrentAuthMethodAsync()
    {
        try
        {
            // Check for Console API key first (simpler check)
            var consoleApiKey = await GetConsoleApiKeyAsync();
            if (!string.IsNullOrEmpty(consoleApiKey))
            {
                _logger.LogInformation("Detected Console API authentication method");
                return AuthMethod.Console;
            }

            // Check for Subscription token
            var subscriptionToken = await GetSubscriptionTokenAsync();
            if (!string.IsNullOrEmpty(subscriptionToken))
            {
                _logger.LogInformation("Detected Subscription authentication method");
                return AuthMethod.Subscription;
            }

            _logger.LogInformation("No authentication method detected");
            return AuthMethod.None;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect current authentication method");
            return AuthMethod.None;
        }
    }

    public async Task<AuthMethod> DetectProfileAuthMethodAsync(string profileName)
    {
        if (string.IsNullOrEmpty(profileName))
        {
            _logger.LogWarning("Profile name is null or empty");
            return AuthMethod.None;
        }

        try
        {
            // Check for Console API key in profile manager store
            var consoleApiKey = await _profileManagerCredentialStore.GetCredentialAsync($"{profileName}-console");
            if (!string.IsNullOrEmpty(consoleApiKey))
            {
                _logger.LogInformation("Profile {ProfileName} uses Console authentication", profileName);
                return AuthMethod.Console;
            }

            // Check for Subscription token in profile manager store
            var subscriptionToken = await _profileManagerCredentialStore.GetCredentialAsync($"{profileName}-subscription");
            if (!string.IsNullOrEmpty(subscriptionToken))
            {
                _logger.LogInformation("Profile {ProfileName} uses Subscription authentication", profileName);
                return AuthMethod.Subscription;
            }

            _logger.LogInformation("Profile {ProfileName} has no stored authentication", profileName);
            return AuthMethod.None;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect authentication method for profile {ProfileName}", profileName);
            return AuthMethod.None;
        }
    }

    public async Task<string?> GetConsoleApiKeyAsync()
    {
        try
        {
            var apiKey = await _claudeCredentialStore.GetCredentialAsync("", Constants.ClaudeCodeServiceName);
            
            if (!string.IsNullOrEmpty(apiKey))
            {
                _logger.LogInformation("Console API key found in Claude Code credential storage");
                return apiKey;
            }

            _logger.LogInformation("No Console API key found in Claude Code credential storage");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Console API key");
            return null;
        }
    }

    public async Task<string?> GetSubscriptionTokenAsync()
    {
        try
        {
            var token = await _claudeCredentialStore.GetCredentialAsync("", Constants.ClaudeCodeCredentialsServiceName);
            
            if (!string.IsNullOrEmpty(token))
            {
                _logger.LogInformation("Subscription token found in Claude Code credential storage");
                return token;
            }

            _logger.LogInformation("No Subscription token found in Claude Code credential storage");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Subscription token");
            return null;
        }
    }

    public async Task<bool> SaveConsoleApiKeyAsync(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("API key is null or empty");
            return false;
        }

        if (!ValidateCredentialFormat(apiKey, AuthMethod.Console))
        {
            _logger.LogWarning("Invalid Console API key format");
            return false;
        }

        try
        {
            var result = await _claudeCredentialStore.SaveCredentialAsync("", apiKey, Constants.ClaudeCodeServiceName);
            
            if (result)
            {
                _logger.LogInformation("Console API key saved successfully");
            }
            else
            {
                _logger.LogWarning("Failed to save Console API key");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while saving Console API key");
            return false;
        }
    }

    public async Task<bool> SaveSubscriptionTokenAsync(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("Subscription token is null or empty");
            return false;
        }

        if (!ValidateCredentialFormat(token, AuthMethod.Subscription))
        {
            _logger.LogWarning("Invalid Subscription token format");
            return false;
        }

        try
        {
            var result = await _claudeCredentialStore.SaveCredentialAsync("", token, Constants.ClaudeCodeCredentialsServiceName);
            
            if (result)
            {
                _logger.LogInformation("Subscription token saved successfully");
            }
            else
            {
                _logger.LogWarning("Failed to save Subscription token");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while saving Subscription token");
            return false;
        }
    }

    public async Task<string> GetTokenHealthAsync(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return "invalid";
        }

        // Ensure method is properly async
        await Task.CompletedTask;

        try
        {
            // First try to parse as JSON
            var jsonDoc = JsonDocument.Parse(token);
            
            // Check if it's a valid OAuth token format
            if (!OAuthTokenRegex().IsMatch(token))
            {
                _logger.LogDebug("Token does not match OAuth format");
                return "invalid format";
            }
            if (!jsonDoc.RootElement.TryGetProperty("claudeAiOauth", out var oauthElement))
            {
                _logger.LogDebug("Token missing claudeAiOauth property");
                return "invalid structure";
            }

            if (!oauthElement.TryGetProperty("expiresAt", out var expiresAtElement))
            {
                _logger.LogDebug("Token missing expiresAt property");
                return "valid";  // Token exists but no expiration info
            }

            if (!expiresAtElement.TryGetInt64(out var expiresAtMs))
            {
                _logger.LogDebug("Invalid expiresAt format");
                return "valid";  // Token exists but expiration format unclear
            }

            // Convert milliseconds to DateTime
            var expiresAt = DateTimeOffset.FromUnixTimeMilliseconds(expiresAtMs).DateTime;
            var now = DateTime.UtcNow;

            if (expiresAt <= now)
            {
                var expiredAgo = now - expiresAt;
                return FormatExpiredTime(expiredAgo);
            }
            else
            {
                var expiresIn = expiresAt - now;
                return FormatExpiresInTime(expiresIn);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogDebug(ex, "Failed to parse token as JSON");
            return "invalid json";
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Exception while analyzing token health");
            return "valid";  // Fallback to valid if we can't determine
        }
    }

    public bool ValidateCredentialFormat(string credential, AuthMethod authMethod)
    {
        if (string.IsNullOrEmpty(credential))
        {
            return false;
        }

        return authMethod switch
        {
            AuthMethod.Console => ValidateConsoleApiKeyFormat(credential),
            AuthMethod.Subscription => ValidateSubscriptionTokenFormat(credential),
            AuthMethod.None => false,
            _ => false
        };
    }

    private static bool ValidateConsoleApiKeyFormat(string apiKey)
    {
        // Console API keys start with "sk-ant-api03-" followed by base64-like characters
        // Example: sk-ant-api03-abcd1234...
        return ConsoleApiKeyRegex().IsMatch(apiKey);
    }

    private static bool ValidateSubscriptionTokenFormat(string token)
    {
        try
        {
            // Subscription tokens are JSON objects containing claudeAiOauth
            var jsonDoc = JsonDocument.Parse(token);
            return jsonDoc.RootElement.TryGetProperty("claudeAiOauth", out _);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string FormatExpiresInTime(TimeSpan timeSpan)
    {
        if (timeSpan.TotalDays >= 1)
        {
            var days = (int)timeSpan.TotalDays;
            var hours = timeSpan.Hours;
            return hours > 0 ? $"expires in {days}d {hours}h" : $"expires in {days}d";
        }
        else if (timeSpan.TotalHours >= 1)
        {
            var hours = (int)timeSpan.TotalHours;
            var minutes = timeSpan.Minutes;
            if (hours <= 2)
            {
                return minutes > 0 ? $"expires soon ({hours}h {minutes}m)" : $"expires soon ({hours}h)";
            }
            return minutes > 0 ? $"expires in {hours}h {minutes}m" : $"expires in {hours}h";
        }
        else if (timeSpan.TotalMinutes >= 1)
        {
            var minutes = (int)timeSpan.TotalMinutes;
            return $"expires soon ({minutes}m)";
        }
        else
        {
            return "expires very soon";
        }
    }

    private static string FormatExpiredTime(TimeSpan timeSpan)
    {
        if (timeSpan.TotalDays >= 1)
        {
            var days = (int)timeSpan.TotalDays;
            var hours = timeSpan.Hours;
            return hours > 0 ? $"expired {days}d {hours}h ago" : $"expired {days}d ago";
        }
        else if (timeSpan.TotalHours >= 1)
        {
            var hours = (int)timeSpan.TotalHours;
            var minutes = timeSpan.Minutes;
            return minutes > 0 ? $"expired {hours}h {minutes}m ago" : $"expired {hours}h ago";
        }
        else if (timeSpan.TotalMinutes >= 1)
        {
            var minutes = (int)timeSpan.TotalMinutes;
            return $"expired {minutes}m ago";
        }
        else
        {
            return "expired recently";
        }
    }

    // Source-generated regex patterns for better performance
    [GeneratedRegex(@"^sk-ant-api03-[A-Za-z0-9_-]{80,100}$", RegexOptions.Compiled)]
    private static partial Regex ConsoleApiKeyRegex();

    [GeneratedRegex(@"^\{.*claudeAiOauth.*\}$", RegexOptions.Compiled | RegexOptions.Singleline)]
    private static partial Regex OAuthTokenRegex();
}