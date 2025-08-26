using ClaudeProfileManager.Core.Models;

namespace ClaudeProfileManager.Core.Interfaces;

/// <summary>
/// Detects and analyzes Claude Code CLI authentication methods and status.
/// </summary>
public interface IClaudeAuthDetector
{
    /// <summary>
    /// Detects the current authentication method used by Claude Code.
    /// </summary>
    /// <returns>The detected authentication method</returns>
    Task<AuthMethod> DetectCurrentAuthMethodAsync();

    /// <summary>
    /// Detects the authentication method for a specific profile.
    /// </summary>
    /// <param name="profileName">The profile name to check</param>
    /// <returns>The authentication method for the profile</returns>
    Task<AuthMethod> DetectProfileAuthMethodAsync(string profileName);

    /// <summary>
    /// Gets the current console API key from Claude Code's credential storage.
    /// </summary>
    /// <returns>The API key if found, null otherwise</returns>
    Task<string?> GetConsoleApiKeyAsync();

    /// <summary>
    /// Gets the current subscription token from Claude Code's credential storage.
    /// </summary>
    /// <returns>The subscription token if found, null otherwise</returns>
    Task<string?> GetSubscriptionTokenAsync();

    /// <summary>
    /// Saves a console API key to Claude Code's credential storage.
    /// </summary>
    /// <param name="apiKey">The API key to save</param>
    /// <returns>True if saved successfully</returns>
    Task<bool> SaveConsoleApiKeyAsync(string apiKey);

    /// <summary>
    /// Saves a subscription token to Claude Code's credential storage.
    /// </summary>
    /// <param name="token">The subscription token to save</param>
    /// <returns>True if saved successfully</returns>
    Task<bool> SaveSubscriptionTokenAsync(string token);

    /// <summary>
    /// Analyzes the health status of a subscription token.
    /// </summary>
    /// <param name="token">The subscription token to analyze</param>
    /// <returns>A human-readable status description</returns>
    Task<string> GetTokenHealthAsync(string token);

    /// <summary>
    /// Validates that a credential appears to be the correct format for the specified auth method.
    /// </summary>
    /// <param name="credential">The credential to validate</param>
    /// <param name="authMethod">The expected authentication method</param>
    /// <returns>True if the credential format is valid</returns>
    bool ValidateCredentialFormat(string credential, AuthMethod authMethod);
}