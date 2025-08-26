using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json;
using ClaudeProfileManager.Core.Models;
using Microsoft.Extensions.Logging;

namespace ClaudeProfileManager.Windows;

[SupportedOSPlatform("windows")]
public class WindowsProfileFileManager
{
    private readonly ILogger<WindowsProfileFileManager> _logger;
    private readonly string _profilesDirectory;
    private readonly JsonSerializerOptions _jsonOptions;

    public WindowsProfileFileManager(ILogger<WindowsProfileFileManager> logger, string? profilesDirectory = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrEmpty(userProfile))
        {
            throw new InvalidOperationException("Could not determine user profile directory");
        }
        
        _profilesDirectory = profilesDirectory ?? Path.Combine(userProfile, ".claude", "profiles");
        
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public Task<bool> EnsureProfilesDirectoryAsync()
    {
        try
        {
            if (!Directory.Exists(_profilesDirectory))
            {
                _logger.LogInformation("Creating profiles directory: {Directory}", _profilesDirectory);
                
                var directoryInfo = Directory.CreateDirectory(_profilesDirectory);
                
                // Set Windows ACLs for security
                var directorySecurity = directoryInfo.GetAccessControl();
                var currentUser = WindowsIdentity.GetCurrent();
                
                // Remove inheritance
                directorySecurity.SetAccessRuleProtection(true, false);
                
                // Clear all existing rules
                var rules = directorySecurity.GetAccessRules(true, true, typeof(SecurityIdentifier));
                foreach (FileSystemAccessRule rule in rules)
                {
                    directorySecurity.RemoveAccessRule(rule);
                }
                
                // Add full control for current user only
                directorySecurity.AddAccessRule(new FileSystemAccessRule(
                    currentUser.User!,
                    FileSystemRights.FullControl,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.None,
                    AccessControlType.Allow));
                
                directoryInfo.SetAccessControl(directorySecurity);
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
            var tempPath = profilePath + ".tmp";
            
            // Write to temp file first (atomic operation)
            var json = JsonSerializer.Serialize(profile, _jsonOptions);
            await File.WriteAllTextAsync(tempPath, json);
            
            // Set secure permissions on temp file
            var fileInfo = new FileInfo(tempPath);
            var fileSecurity = fileInfo.GetAccessControl();
            var currentUser = WindowsIdentity.GetCurrent();
            
            // Remove inheritance
            fileSecurity.SetAccessRuleProtection(true, false);
            
            // Clear all existing rules
            var rules = fileSecurity.GetAccessRules(true, true, typeof(SecurityIdentifier));
            foreach (FileSystemAccessRule rule in rules)
            {
                fileSecurity.RemoveAccessRule(rule);
            }
            
            // Add read/write for current user only
            fileSecurity.AddAccessRule(new FileSystemAccessRule(
                currentUser.User!,
                FileSystemRights.ReadAndExecute | FileSystemRights.Write,
                AccessControlType.Allow));
            
            fileInfo.SetAccessControl(fileSecurity);
            
            // Atomic move
            File.Move(tempPath, profilePath, true);
            
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
            
            if (!File.Exists(profilePath))
            {
                _logger.LogInformation("Profile file does not exist: {ProfilePath}", profilePath);
                return null;
            }
            
            var json = await File.ReadAllTextAsync(profilePath);
            var profile = JsonSerializer.Deserialize<Profile>(json, _jsonOptions);
            
            if (profile != null)
            {
                _logger.LogInformation("Profile metadata loaded: {ProfileName}", profileName);
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
            if (!Directory.Exists(_profilesDirectory))
            {
                _logger.LogInformation("Profiles directory does not exist");
                return Task.FromResult(new List<string>());
            }
            
            var profileFiles = Directory.GetFiles(_profilesDirectory, "*.json")
                .Where(f => !Path.GetFileName(f).StartsWith('.'))
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .Where(n => !string.IsNullOrEmpty(n))
                .ToList();
            
            _logger.LogInformation("Found {Count} profiles", profileFiles.Count);
            return Task.FromResult(profileFiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list profiles");
            return Task.FromResult(new List<string>());
        }
    }

    public async Task<bool> SaveCurrentProfileAsync(string profileName)
    {
        if (string.IsNullOrEmpty(profileName))
        {
            _logger.LogWarning("Profile name is null or empty");
            return false;
        }
        
        try
        {
            if (!await EnsureProfilesDirectoryAsync())
            {
                return false;
            }
            
            var currentPath = Path.Combine(_profilesDirectory, ".current");
            var tempPath = currentPath + ".tmp";
            
            await File.WriteAllTextAsync(tempPath, profileName);
            
            // Set secure permissions
            var fileInfo = new FileInfo(tempPath);
            var fileSecurity = fileInfo.GetAccessControl();
            var currentUser = WindowsIdentity.GetCurrent();
            
            fileSecurity.SetAccessRuleProtection(true, false);
            
            var rules = fileSecurity.GetAccessRules(true, true, typeof(SecurityIdentifier));
            foreach (FileSystemAccessRule rule in rules)
            {
                fileSecurity.RemoveAccessRule(rule);
            }
            
            fileSecurity.AddAccessRule(new FileSystemAccessRule(
                currentUser.User!,
                FileSystemRights.ReadAndExecute | FileSystemRights.Write,
                AccessControlType.Allow));
            
            fileInfo.SetAccessControl(fileSecurity);
            
            File.Move(tempPath, currentPath, true);
            
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
            
            if (!File.Exists(currentPath))
            {
                _logger.LogInformation("No current profile set");
                return null;
            }
            
            var profileName = await File.ReadAllTextAsync(currentPath);
            profileName = profileName?.Trim();
            
            if (!string.IsNullOrEmpty(profileName))
            {
                _logger.LogInformation("Current profile: {ProfileName}", profileName);
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
            
            if (!File.Exists(aliasesPath))
            {
                _logger.LogInformation("No aliases file found");
                return new Dictionary<string, string>();
            }
            
            var json = await File.ReadAllTextAsync(aliasesPath);
            var aliases = JsonSerializer.Deserialize<Dictionary<string, string>>(json, _jsonOptions) 
                          ?? new Dictionary<string, string>();
            
            _logger.LogInformation("Loaded {Count} aliases", aliases.Count);
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
            var tempPath = aliasesPath + ".tmp";
            
            var json = JsonSerializer.Serialize(aliases, _jsonOptions);
            await File.WriteAllTextAsync(tempPath, json);
            
            // Set secure permissions
            var fileInfo = new FileInfo(tempPath);
            var fileSecurity = fileInfo.GetAccessControl();
            var currentUser = WindowsIdentity.GetCurrent();
            
            fileSecurity.SetAccessRuleProtection(true, false);
            
            var rules = fileSecurity.GetAccessRules(true, true, typeof(SecurityIdentifier));
            foreach (FileSystemAccessRule rule in rules)
            {
                fileSecurity.RemoveAccessRule(rule);
            }
            
            fileSecurity.AddAccessRule(new FileSystemAccessRule(
                currentUser.User!,
                FileSystemRights.ReadAndExecute | FileSystemRights.Write,
                AccessControlType.Allow));
            
            fileInfo.SetAccessControl(fileSecurity);
            
            File.Move(tempPath, aliasesPath, true);
            
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