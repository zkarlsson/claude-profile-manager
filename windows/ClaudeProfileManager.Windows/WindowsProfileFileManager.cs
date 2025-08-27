using System.Runtime.Versioning;
using System.Text.Json;
using ClaudeProfileManager.Core.Configuration;
using ClaudeProfileManager.Core.IO;
using ClaudeProfileManager.Core.Json;
using ClaudeProfileManager.Core.Models;
using Microsoft.Extensions.Logging;

namespace ClaudeProfileManager.Windows;

[SupportedOSPlatform("windows")]
public class WindowsProfileFileManager
{
    private readonly ILogger<WindowsProfileFileManager> _logger;
    private readonly string _profilesDirectory;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly bool _isDevelopment;

    public WindowsProfileFileManager(ILogger<WindowsProfileFileManager> logger, string? profilesDirectory = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrEmpty(userProfile))
        {
            throw new InvalidOperationException("Could not determine user profile directory");
        }
        
        _profilesDirectory = profilesDirectory ?? ConfigurationProvider.Current.Paths.GetProfilesDirectory();
        
        // Check if we're in development mode
        _isDevelopment = LoggingConfiguration.IsDebugEnabled ||
                        Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") == "Development";
        
        // Use optimized JSON options based on environment
        _jsonOptions = JsonOptionsProvider.ForEnvironment(_isDevelopment);
    }

    public Task<bool> EnsureProfilesDirectoryAsync()
    {
        try
        {
            if (!OptimizedFileOperations.DirectoryExistsCached(_profilesDirectory))
            {
                _logger.LogInformation("Creating profiles directory: {Directory}", _profilesDirectory);
                OptimizedFileOperations.CreateSecureDirectory(_profilesDirectory);
                _logger.LogInformation("Profiles directory created with secure ACLs");
            }
            
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure profiles directory exists");
            return Task.FromResult(false);
        }
    }

    public async Task<bool> SaveProfileMetadataAsync(Profile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        
        try
        {
            if (!await EnsureProfilesDirectoryAsync())
            {
                return false;
            }
            
            var profilePath = GetProfilePath(profile.Name);
            await OptimizedFileOperations.WriteJsonAtomicAsync(profilePath, profile, _jsonOptions);
            
            _logger.LogInformation("Profile metadata saved: {ProfileName}", profile.Name);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save profile metadata for {ProfileName}", profile.Name);
            return false;
        }
    }

    public async Task<Profile?> LoadProfileMetadataAsync(string profileName)
    {
        if (string.IsNullOrEmpty(profileName))
        {
            _logger.LogWarning("Profile name is null or empty");
            return null;
        }
        
        try
        {
            var profilePath = GetProfilePath(profileName);
            var profile = await OptimizedFileOperations.ReadJsonAsync<Profile>(profilePath, _jsonOptions);
            
            if (profile != null)
            {
                _logger.LogInformation("Profile metadata loaded: {ProfileName}", profileName);
            }
            else
            {
                _logger.LogInformation("Profile file does not exist: {ProfileName}", profileName);
            }
            
            return profile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load profile metadata for {ProfileName}", profileName);
            return null;
        }
    }

    public Task<bool> DeleteProfileMetadataAsync(string profileName)
    {
        if (string.IsNullOrEmpty(profileName))
        {
            _logger.LogWarning("Profile name is null or empty");
            return Task.FromResult(false);
        }
        
        try
        {
            var profilePath = GetProfilePath(profileName);
            
            if (!File.Exists(profilePath))
            {
                _logger.LogInformation("Profile file does not exist: {ProfilePath}", profilePath);
                return Task.FromResult(true); // Consider it deleted
            }
            
            File.Delete(profilePath);
            _logger.LogInformation("Profile metadata deleted: {ProfileName}", profileName);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete profile metadata for {ProfileName}", profileName);
            return Task.FromResult(false);
        }
    }

    public Task<List<string>> ListProfilesAsync()
    {
        try
        {
            var profileFiles = OptimizedFileOperations.EnumerateJsonFiles(_profilesDirectory);
            var profileList = new List<string>(profileFiles);
            _logger.LogInformation("Found {Count} profiles", profileList.Count);
            return Task.FromResult(profileList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list profiles");
            return Task.FromResult(new List<string>());
        }
    }

    public async Task<bool> SaveCurrentProfileAsync(string profileName)
    {
        try
        {
            if (!await EnsureProfilesDirectoryAsync())
            {
                return false;
            }
            
            var currentPath = Path.Combine(_profilesDirectory, ".current");
            
            // If profile name is empty, clear the current profile by deleting the file
            if (string.IsNullOrEmpty(profileName))
            {
                if (File.Exists(currentPath))
                {
                    File.Delete(currentPath);
                    _logger.LogInformation("Cleared current profile");
                }
                return true;
            }
            
            await OptimizedFileOperations.WriteTextAtomicAsync(currentPath, profileName);
            
            _logger.LogInformation("Current profile saved: {ProfileName}", profileName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save current profile");
            return false;
        }
    }

    public async Task<string?> GetCurrentProfileAsync()
    {
        try
        {
            var currentPath = Path.Combine(_profilesDirectory, ".current");
            var profileName = (await OptimizedFileOperations.ReadTextAsync(currentPath))?.Trim();
            
            if (!string.IsNullOrEmpty(profileName))
            {
                _logger.LogInformation("Current profile: {ProfileName}", profileName);
            }
            else
            {
                _logger.LogInformation("No current profile set");
            }
            
            return profileName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current profile");
            return null;
        }
    }

    public async Task<Dictionary<string, string>> LoadAliasesAsync()
    {
        try
        {
            var aliasesPath = Path.Combine(_profilesDirectory, ".aliases");
            var aliases = await OptimizedFileOperations.ReadJsonAsync<Dictionary<string, string>>(aliasesPath, _jsonOptions)
                          ?? new Dictionary<string, string>();
            
            if (aliases.Count > 0)
            {
                _logger.LogInformation("Loaded {Count} aliases", aliases.Count);
            }
            else
            {
                _logger.LogInformation("No aliases file found");
            }
            
            return aliases;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load aliases");
            return new Dictionary<string, string>();
        }
    }

    public async Task<bool> SaveAliasesAsync(Dictionary<string, string> aliases)
    {
        ArgumentNullException.ThrowIfNull(aliases);
        
        try
        {
            if (!await EnsureProfilesDirectoryAsync())
            {
                return false;
            }
            
            var aliasesPath = Path.Combine(_profilesDirectory, ".aliases");
            await OptimizedFileOperations.WriteJsonAtomicAsync(aliasesPath, aliases, _jsonOptions);
            
            _logger.LogInformation("Saved {Count} aliases", aliases.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save aliases");
            return false;
        }
    }

    private string GetProfilePath(string profileName)
    {
        return Path.Combine(_profilesDirectory, $"{profileName}.json");
    }
}