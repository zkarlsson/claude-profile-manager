using ClaudeProfileManager.Core.Models;

namespace ClaudeProfileManager.Core.Interfaces;

/// <summary>
/// Manages Claude authentication profiles and their metadata.
/// </summary>
public interface IProfileManager
{
    /// <summary>
    /// Creates a new profile with the current authentication credentials.
    /// </summary>
    /// <param name="profileName">The name of the profile to create</param>
    /// <param name="aliases">Optional aliases for the profile</param>
    /// <returns>True if the profile was created successfully</returns>
    Task<bool> SaveProfileAsync(string profileName, IEnumerable<string>? aliases = null);

    /// <summary>
    /// Retrieves a profile by name.
    /// </summary>
    /// <param name="profileName">The name of the profile to retrieve</param>
    /// <returns>The profile if found, null otherwise</returns>
    Task<Profile?> GetProfileAsync(string profileName);

    /// <summary>
    /// Lists all available profiles with their metadata.
    /// </summary>
    /// <returns>A collection of all profiles</returns>
    Task<IEnumerable<Profile>> ListProfilesAsync();

    /// <summary>
    /// Switches to the specified profile, making it the active authentication.
    /// </summary>
    /// <param name="profileName">The name of the profile to switch to</param>
    /// <returns>True if the switch was successful</returns>
    Task<bool> SwitchToProfileAsync(string profileName);

    /// <summary>
    /// Gets the currently active profile name.
    /// </summary>
    /// <returns>The name of the current profile, or null if no profile is active</returns>
    Task<string?> GetCurrentProfileAsync();

    /// <summary>
    /// Deletes a profile and its associated credentials.
    /// </summary>
    /// <param name="profileName">The name of the profile to delete</param>
    /// <returns>True if the profile was deleted successfully</returns>
    Task<bool> DeleteProfileAsync(string profileName);

    /// <summary>
    /// Adds an alias for an existing profile.
    /// </summary>
    /// <param name="aliasName">The alias name to create</param>
    /// <param name="profileName">The target profile name</param>
    /// <returns>True if the alias was added successfully</returns>
    Task<bool> AddAliasAsync(string aliasName, string profileName);

    /// <summary>
    /// Removes an alias.
    /// </summary>
    /// <param name="aliasName">The alias name to remove</param>
    /// <returns>True if the alias was removed successfully</returns>
    Task<bool> RemoveAliasAsync(string aliasName);

    /// <summary>
    /// Lists all defined aliases.
    /// </summary>
    /// <returns>A dictionary of alias names and their target profiles</returns>
    Task<Dictionary<string, string>> ListAliasesAsync();

    /// <summary>
    /// Resolves an alias to its target profile name.
    /// </summary>
    /// <param name="nameOrAlias">The profile name or alias to resolve</param>
    /// <returns>The resolved profile name</returns>
    Task<string> ResolveAliasAsync(string nameOrAlias);
}