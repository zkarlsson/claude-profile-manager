using System.Runtime.Versioning;
using ClaudeProfileManager.Core.Interfaces;
using ClaudeProfileManager.Core.Models;
using ClaudeProfileManager.Core.Services;
using ClaudeProfileManager.Core.Validation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClaudeProfileManager.Windows;

[SupportedOSPlatform("windows")]
public class WindowsProfileManager : IProfileManager
{
    private readonly ICredentialStore _credentialStore;
    private readonly WindowsProfileFileManager _profileFileManager;
    private readonly IClaudeAuthDetector _authDetector;
    private readonly ILogger<WindowsProfileManager> _logger;

    public WindowsProfileManager(
        ILogger<WindowsProfileManager> logger,
        ICredentialStore? credentialStore = null,
        WindowsProfileFileManager? profileFileManager = null,
        IClaudeAuthDetector? authDetector = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Use provided dependencies or create default implementations
        _credentialStore = credentialStore ?? CreateDefaultCredentialStore();
        _profileFileManager = profileFileManager ?? CreateDefaultProfileFileManager();
        _authDetector = authDetector ?? CreateDefaultAuthDetector();
    }

    public async Task<bool> SaveProfileAsync(string profileName, IEnumerable<string>? aliases = null)
    {
        // Validate profile name
        var profileValidation = InputValidator.ValidateProfileName(profileName);
        if (!profileValidation.IsValid)
        {
            _logger.LogWarning("Invalid profile name: {Error}", profileValidation.ErrorMessage);
            return false;
        }
        profileName = profileValidation.Value;

        try
        {
            _logger.LogInformation("Saving profile: {ProfileName}", profileName);

            // Detect current authentication method
            var authMethod = await _authDetector.DetectCurrentAuthMethodAsync();
            if (authMethod == AuthMethod.None)
            {
                _logger.LogWarning("No active authentication detected for profile {ProfileName}", profileName);
                return false;
            }

            // Get current credentials based on auth method
            string? credentials = authMethod switch
            {
                AuthMethod.Console => await _authDetector.GetConsoleApiKeyAsync(),
                AuthMethod.Subscription => await _authDetector.GetSubscriptionTokenAsync(),
                _ => null
            };

            if (string.IsNullOrEmpty(credentials))
            {
                _logger.LogWarning("Could not retrieve current credentials for profile {ProfileName}", profileName);
                return false;
            }

            // Store credentials in profile manager credential store
            var credentialKey = GetCredentialKey(profileName, authMethod);
            var credentialSaved = await _credentialStore.SaveCredentialAsync(credentialKey, credentials);
            if (!credentialSaved)
            {
                _logger.LogError("Failed to save credentials for profile {ProfileName}", profileName);
                return false;
            }

            // Create profile metadata
            var profile = new Profile
            {
                Name = profileName,
                AuthMethod = authMethod,
                Created = DateTime.UtcNow,
                LastUsed = DateTime.UtcNow
            };

            // Save profile metadata
            var metadataSaved = await _profileFileManager.SaveProfileMetadataAsync(profile);
            if (!metadataSaved)
            {
                _logger.LogError("Failed to save profile metadata for {ProfileName}", profileName);
                
                // Cleanup credentials if metadata save failed
                await _credentialStore.DeleteCredentialAsync(credentialKey);
                return false;
            }

            // Add any provided aliases with validation
            if (aliases != null)
            {
                var aliasDict = await _profileFileManager.LoadAliasesAsync();
                foreach (var alias in aliases.Where(a => !string.IsNullOrWhiteSpace(a)))
                {
                    var aliasValidation = InputValidator.ValidateAliasName(alias);
                    if (!aliasValidation.IsValid)
                    {
                        _logger.LogWarning("Invalid alias '{Alias}' for profile {ProfileName}: {Error}", 
                            InputValidator.SanitizeForLogging(alias), profileName, aliasValidation.ErrorMessage);
                        continue; // Skip invalid aliases but don't fail the entire operation
                    }
                    aliasDict[aliasValidation.Value] = profileName;
                }
                await _profileFileManager.SaveAliasesAsync(aliasDict);
            }

            _logger.LogInformation("Successfully saved profile: {ProfileName} with {AuthMethod} authentication", 
                profileName, authMethod);
            return true;
        }
        catch (Exception ex)
        {
            return StandardizedErrorHandler.HandleException(_logger, ex, "SaveProfile", $"ProfileName: {profileName}");
        }
    }

    public async Task<Profile?> GetProfileAsync(string profileName)
    {
        // Validate profile name (but allow it through for alias resolution)
        var profileValidation = InputValidator.ValidateUserInput(profileName, "Profile name");
        if (!profileValidation.IsValid)
        {
            StandardizedErrorHandler.LogValidationError(_logger, "GetProfile", profileValidation.ErrorMessage, profileName);
            return null;
        }
        profileName = profileValidation.Value;

        try
        {
            // Resolve any alias to actual profile name
            var resolvedName = await ResolveAliasAsync(profileName);
            
            // Load profile metadata
            var profile = await _profileFileManager.LoadProfileMetadataAsync(resolvedName);
            if (profile == null)
            {
                _logger.LogInformation("Profile not found: {ProfileName}", profileName);
                return null;
            }

            // Enhance profile with credential health information if it's a subscription token
            if (profile.AuthMethod == AuthMethod.Subscription)
            {
                var credentialKey = GetCredentialKey(resolvedName, profile.AuthMethod);
                var credentials = await _credentialStore.GetCredentialAsync(credentialKey);
                if (!string.IsNullOrEmpty(credentials))
                {
                    var tokenHealth = await _authDetector.GetTokenHealthAsync(credentials);
                    // Token health could be stored in profile metadata or returned separately
                    // For now, we'll just log it
                    _logger.LogDebug("Token health for {ProfileName}: {Health}", resolvedName, tokenHealth);
                }
            }

            return profile;
        }
        catch (Exception ex)
        {
            return StandardizedErrorHandler.HandleException<WindowsProfileManager, Profile>(_logger, ex, "GetProfile", $"ProfileName: {profileName}");
        }
    }

    public async Task<IEnumerable<Profile>> ListProfilesAsync()
    {
        try
        {
            var profileNames = await _profileFileManager.ListProfilesAsync();
            var profiles = new List<Profile>();

            foreach (var name in profileNames)
            {
                var profile = await _profileFileManager.LoadProfileMetadataAsync(name);
                if (profile != null)
                {
                    profiles.Add(profile);
                }
            }

            _logger.LogInformation("Retrieved {Count} profiles", profiles.Count);
            return profiles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while listing profiles");
            return Enumerable.Empty<Profile>();
        }
    }

    public async Task<bool> SwitchToProfileAsync(string profileName)
    {
        if (string.IsNullOrWhiteSpace(profileName))
        {
            _logger.LogWarning("Profile name cannot be null or empty");
            return false;
        }

        try
        {
            _logger.LogInformation("Switching to profile: {ProfileName}", profileName);

            // Resolve any alias to actual profile name
            var resolvedName = await ResolveAliasAsync(profileName);
            
            // Get the profile to switch to
            var profile = await _profileFileManager.LoadProfileMetadataAsync(resolvedName);
            if (profile == null)
            {
                _logger.LogWarning("Profile not found: {ProfileName}", profileName);
                return false;
            }

            // Save current active credentials before switching (auto-save functionality)
            await AutoSaveCurrentCredentialsAsync();

            // Get stored credentials for the target profile
            var credentialKey = GetCredentialKey(resolvedName, profile.AuthMethod);
            var credentials = await _credentialStore.GetCredentialAsync(credentialKey);
            if (string.IsNullOrEmpty(credentials))
            {
                _logger.LogWarning("No credentials found for profile {ProfileName}", resolvedName);
                return false;
            }

            // Restore credentials to Claude Code's credential store
            var restored = profile.AuthMethod switch
            {
                AuthMethod.Console => await _authDetector.SaveConsoleApiKeyAsync(credentials),
                AuthMethod.Subscription => await _authDetector.SaveSubscriptionTokenAsync(credentials),
                _ => false
            };

            if (!restored)
            {
                _logger.LogError("Failed to restore credentials for profile {ProfileName}", resolvedName);
                return false;
            }

            // Update current profile tracking
            var currentSet = await _profileFileManager.SaveCurrentProfileAsync(resolvedName);
            if (!currentSet)
            {
                _logger.LogError("Failed to set current profile to {ProfileName}", resolvedName);
                return false;
            }

            // Update last used timestamp
            profile.LastUsed = DateTime.UtcNow;
            await _profileFileManager.SaveProfileMetadataAsync(profile);

            _logger.LogInformation("Successfully switched to profile: {ProfileName}", resolvedName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while switching to profile {ProfileName}", profileName);
            return false;
        }
    }

    public async Task<string?> GetCurrentProfileAsync()
    {
        try
        {
            var currentProfile = await _profileFileManager.GetCurrentProfileAsync();
            _logger.LogDebug("Current profile: {ProfileName}", currentProfile ?? "None");
            return currentProfile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while getting current profile");
            return null;
        }
    }

    public async Task<bool> DeleteProfileAsync(string profileName)
    {
        if (string.IsNullOrWhiteSpace(profileName))
        {
            _logger.LogWarning("Profile name cannot be null or empty");
            return false;
        }

        try
        {
            _logger.LogInformation("Deleting profile: {ProfileName}", profileName);

            // Resolve any alias to actual profile name
            var resolvedName = await ResolveAliasAsync(profileName);

            // Get profile to determine auth method for credential cleanup
            var profile = await _profileFileManager.LoadProfileMetadataAsync(resolvedName);
            if (profile == null)
            {
                _logger.LogWarning("Profile not found: {ProfileName}", profileName);
                return false;
            }

            // Delete stored credentials
            var credentialKey = GetCredentialKey(resolvedName, profile.AuthMethod);
            await _credentialStore.DeleteCredentialAsync(credentialKey);

            // Delete profile metadata
            var metadataDeleted = await _profileFileManager.DeleteProfileMetadataAsync(resolvedName);

            // Remove any aliases pointing to this profile
            var aliases = await _profileFileManager.LoadAliasesAsync();
            // Remove aliases more efficiently without LINQ allocations
            var keysToRemove = new List<string>();
            foreach (var kvp in aliases)
            {
                if (kvp.Value == resolvedName)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            foreach (var key in keysToRemove)
            {
                aliases.Remove(key);
            }
            if (keysToRemove.Count > 0)
            {
                await _profileFileManager.SaveAliasesAsync(aliases);
            }

            // Clear current profile if this was the active one
            var currentProfile = await _profileFileManager.GetCurrentProfileAsync();
            if (currentProfile == resolvedName)
            {
                await _profileFileManager.SaveCurrentProfileAsync("");
            }

            _logger.LogInformation("Successfully deleted profile: {ProfileName}", resolvedName);
            return metadataDeleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while deleting profile {ProfileName}", profileName);
            return false;
        }
    }

    public async Task<bool> AddAliasAsync(string aliasName, string profileName)
    {
        // Validate alias name
        var aliasValidation = InputValidator.ValidateAliasName(aliasName);
        if (!aliasValidation.IsValid)
        {
            _logger.LogWarning("Invalid alias name: {Error}", aliasValidation.ErrorMessage);
            return false;
        }
        aliasName = aliasValidation.Value;

        // Validate profile name
        var profileValidation = InputValidator.ValidateUserInput(profileName, "Profile name");
        if (!profileValidation.IsValid)
        {
            StandardizedErrorHandler.LogValidationError(_logger, "SaveProfile", profileValidation.ErrorMessage, profileName);
            return false;
        }
        profileName = profileValidation.Value;

        try
        {
            // Verify the target profile exists
            var profile = await _profileFileManager.LoadProfileMetadataAsync(profileName);
            if (profile == null)
            {
                _logger.LogWarning("Target profile not found: {ProfileName}", profileName);
                return false;
            }

            // Load current aliases
            var aliases = await _profileFileManager.LoadAliasesAsync();

            // Check if alias already exists
            if (aliases.ContainsKey(aliasName))
            {
                _logger.LogWarning("Alias already exists: {AliasName}", aliasName);
                return false;
            }

            // Add the new alias
            aliases[aliasName] = profileName;
            var saved = await _profileFileManager.SaveAliasesAsync(aliases);

            if (saved)
            {
                _logger.LogInformation("Added alias: {AliasName} -> {ProfileName}", aliasName, profileName);
            }

            return saved;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while adding alias {AliasName} for profile {ProfileName}", 
                aliasName, profileName);
            return false;
        }
    }

    public async Task<bool> RemoveAliasAsync(string aliasName)
    {
        // Validate alias name
        var aliasValidation = InputValidator.ValidateUserInput(aliasName, "Alias name");
        if (!aliasValidation.IsValid)
        {
            _logger.LogWarning("Invalid alias name: {Error}", aliasValidation.ErrorMessage);
            return false;
        }
        aliasName = aliasValidation.Value;

        try
        {
            var aliases = await _profileFileManager.LoadAliasesAsync();

            if (!aliases.TryGetValue(aliasName, out var targetProfile))
            {
                _logger.LogWarning("Alias not found: {AliasName}", aliasName);
                return false;
            }
            aliases.Remove(aliasName);
            
            var saved = await _profileFileManager.SaveAliasesAsync(aliases);

            if (saved)
            {
                _logger.LogInformation("Removed alias: {AliasName} (was pointing to {ProfileName})", 
                    aliasName, targetProfile);
            }

            return saved;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while removing alias {AliasName}", aliasName);
            return false;
        }
    }

    public async Task<Dictionary<string, string>> ListAliasesAsync()
    {
        try
        {
            var aliases = await _profileFileManager.LoadAliasesAsync();
            _logger.LogDebug("Retrieved {Count} aliases", aliases.Count);
            return aliases;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while listing aliases");
            return new Dictionary<string, string>();
        }
    }

    public async Task<string> ResolveAliasAsync(string nameOrAlias)
    {
        if (string.IsNullOrWhiteSpace(nameOrAlias))
        {
            return nameOrAlias ?? "";
        }

        try
        {
            var aliases = await _profileFileManager.LoadAliasesAsync();
            
            if (aliases.TryGetValue(nameOrAlias, out var resolvedName))
            {
                _logger.LogDebug("Resolved alias {AliasName} to {ProfileName}", nameOrAlias, resolvedName);
                return resolvedName;
            }

            // Not an alias, return the original name
            return nameOrAlias;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while resolving alias {NameOrAlias}", nameOrAlias);
            return nameOrAlias;
        }
    }

    private async Task AutoSaveCurrentCredentialsAsync()
    {
        try
        {
            var currentProfile = await _profileFileManager.GetCurrentProfileAsync();
            if (string.IsNullOrEmpty(currentProfile))
            {
                return; // No current profile to save
            }

            var profile = await _profileFileManager.LoadProfileMetadataAsync(currentProfile);
            if (profile?.AuthMethod == AuthMethod.Subscription)
            {
                // Only auto-save subscription tokens as they can refresh
                var currentToken = await _authDetector.GetSubscriptionTokenAsync();
                if (!string.IsNullOrEmpty(currentToken))
                {
                    var credentialKey = GetCredentialKey(currentProfile, AuthMethod.Subscription);
                    var existingToken = await _credentialStore.GetCredentialAsync(credentialKey);
                    
                    if (currentToken != existingToken)
                    {
                        await _credentialStore.SaveCredentialAsync(credentialKey, currentToken);
                        _logger.LogInformation("Auto-saved refreshed credentials for profile: {ProfileName}", currentProfile);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to auto-save current credentials");
        }
    }

    private static string GetCredentialKey(string profileName, AuthMethod authMethod)
    {
        return authMethod switch
        {
            AuthMethod.Console => $"{profileName}-console",
            AuthMethod.Subscription => $"{profileName}-subscription",
            _ => throw new ArgumentException($"Unsupported auth method: {authMethod}")
        };
    }

    private static WindowsCredentialStore CreateDefaultCredentialStore()
    {
        var logger = new NullLogger<WindowsCredentialStore>();
        return new WindowsCredentialStore(logger);
    }

    private static WindowsProfileFileManager CreateDefaultProfileFileManager()
    {
        var logger = new NullLogger<WindowsProfileFileManager>();
        return new WindowsProfileFileManager(logger);
    }

    private static WindowsClaudeAuthDetector CreateDefaultAuthDetector()
    {
        var logger = new NullLogger<WindowsClaudeAuthDetector>();
        return new WindowsClaudeAuthDetector(logger);
    }
}