namespace ClaudeProfileManager.Core.Models;

/// <summary>
/// Represents the different authentication methods supported by Claude Code CLI.
/// </summary>
public enum AuthMethod
{
    /// <summary>
    /// No authentication configured.
    /// </summary>
    None,

    /// <summary>
    /// Console API key authentication (static API key).
    /// </summary>
    Console,

    /// <summary>
    /// Subscription OAuth token authentication (dynamic tokens with refresh).
    /// </summary>
    Subscription,

    /// <summary>
    /// Authentication method could not be determined.
    /// </summary>
    Unknown
}