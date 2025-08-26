namespace ClaudeProfileManager.Core;

/// <summary>
/// Constants used throughout the Claude Profile Manager application.
/// </summary>
public static class Constants
{
    /// <summary>
    /// The default directory name for Claude settings (under user profile).
    /// </summary>
    public const string ClaudeDirectoryName = ".claude";

    /// <summary>
    /// The subdirectory name for profile storage.
    /// </summary>
    public const string ProfilesDirectoryName = "profiles";

    /// <summary>
    /// The filename for storing the current active profile.
    /// </summary>
    public const string CurrentProfileFileName = ".current";

    /// <summary>
    /// The filename for storing profile aliases.
    /// </summary>
    public const string AliasesFileName = ".aliases";

    /// <summary>
    /// The filename for audit logging (when enabled).
    /// </summary>
    public const string AuditLogFileName = ".audit.log";

    /// <summary>
    /// The file extension for profile metadata files.
    /// </summary>
    public const string ProfileFileExtension = ".json";

    /// <summary>
    /// The Windows Credential Manager service name for Claude Code console authentication.
    /// </summary>
    public const string ClaudeCodeServiceName = "Claude Code";

    /// <summary>
    /// The Windows Credential Manager service name for Claude Code subscription authentication.
    /// </summary>
    public const string ClaudeCodeCredentialsServiceName = "Claude Code-credentials";

    /// <summary>
    /// The Windows Credential Manager service name for Claude Profile Manager credential backups.
    /// </summary>
    public const string ProfileManagerServiceName = "Claude Profile Manager";

    /// <summary>
    /// Environment variable name for enabling debug logging.
    /// </summary>
    public const string DebugEnvironmentVariable = "CLAUDE_PROFILE_DEBUG";

    /// <summary>
    /// Environment variable name for enabling audit logging.
    /// </summary>
    public const string AuditLogEnvironmentVariable = "CLAUDE_PROFILE_LOG";

    /// <summary>
    /// Gets the full path to the Claude profiles directory.
    /// </summary>
    /// <returns>The full path to the profiles directory</returns>
    public static string GetProfilesDirectory()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(userProfile, ClaudeDirectoryName, ProfilesDirectoryName);
    }

    /// <summary>
    /// Gets the full path to a profile metadata file.
    /// </summary>
    /// <param name="profileName">The profile name</param>
    /// <returns>The full path to the profile file</returns>
    public static string GetProfileFilePath(string profileName)
    {
        return Path.Combine(GetProfilesDirectory(), profileName + ProfileFileExtension);
    }

    /// <summary>
    /// Gets the full path to the current profile file.
    /// </summary>
    /// <returns>The full path to the current profile file</returns>
    public static string GetCurrentProfileFilePath()
    {
        return Path.Combine(GetProfilesDirectory(), CurrentProfileFileName);
    }

    /// <summary>
    /// Gets the full path to the aliases file.
    /// </summary>
    /// <returns>The full path to the aliases file</returns>
    public static string GetAliasesFilePath()
    {
        return Path.Combine(GetProfilesDirectory(), AliasesFileName);
    }

    /// <summary>
    /// Gets the full path to the audit log file.
    /// </summary>
    /// <returns>The full path to the audit log file</returns>
    public static string GetAuditLogFilePath()
    {
        return Path.Combine(GetProfilesDirectory(), AuditLogFileName);
    }

    /// <summary>
    /// Checks if debug logging is enabled via environment variable.
    /// </summary>
    /// <returns>True if debug logging is enabled</returns>
    public static bool IsDebugEnabled()
    {
        return string.Equals(
            Environment.GetEnvironmentVariable(DebugEnvironmentVariable),
            "true",
            StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if audit logging is enabled via environment variable.
    /// </summary>
    /// <returns>True if audit logging is enabled</returns>
    public static bool IsAuditLogEnabled()
    {
        return string.Equals(
            Environment.GetEnvironmentVariable(AuditLogEnvironmentVariable),
            "true",
            StringComparison.OrdinalIgnoreCase);
    }
}