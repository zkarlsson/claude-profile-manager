using System.Runtime.Versioning;
using System.Security;
using System.Text;
using System.Text.Json;
using ClaudeProfileManager.Core.IO;
using ClaudeProfileManager.Core.Models;
using ClaudeProfileManager.Core.Services;
using ClaudeProfileManager.Core.Json;
using Microsoft.Extensions.Logging;

namespace ClaudeProfileManager.Windows.Security;

/// <summary>
/// Secure implementation of profile file manager that applies Windows ACLs to protect profile files.
/// Ensures that profile configuration files can only be accessed by the current user.
/// </summary>
[SupportedOSPlatform("windows")]
public class SecureProfileFileManager
{
    private readonly ILogger<SecureProfileFileManager> _logger;
    private readonly WindowsAclManager _aclManager;
    private readonly string _profilesDirectory;
    private readonly JsonSerializerOptions _jsonOptions;

    public SecureProfileFileManager(
        ILogger<SecureProfileFileManager> logger,
        WindowsAclManager aclManager,
        string? profilesDirectory = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _aclManager = aclManager ?? throw new ArgumentNullException(nameof(aclManager));
        
        // Use provided directory or default to user's .claude directory
        _profilesDirectory = profilesDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".claude", "profiles");

        _jsonOptions = JsonOptionsProvider.ForEnvironment(false);

        // Ensure the profiles directory exists with proper security
        InitializeSecureDirectory();
    }

    /// <summary>
    /// Loads a profile configuration from secure storage.
    /// </summary>
    public async Task<Profile?> LoadProfileAsync(string profileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileName);

        try
        {
            var filePath = GetProfileFilePath(profileName);
            
            if (!File.Exists(filePath))
            {
                _logger.LogDebug("Profile file not found: {ProfileName}", profileName);
                return null;
            }

            // Verify file security before reading
            if (!_aclManager.IsFileSecure(filePath))
            {
                _logger.LogWarning("Profile file has insecure permissions, securing it: {ProfileName}", profileName);
                _aclManager.SecureFile(filePath);
            }

            // Read the file securely
            var content = await OptimizedFileOperations.ReadTextAsync(filePath);
            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning("Profile file is empty: {ProfileName}", profileName);
                return null;
            }

            // Deserialize the profile configuration
            var profile = JsonSerializer.Deserialize<Profile>(content, _jsonOptions);
            
            if (profile == null)
            {
                _logger.LogWarning("Failed to deserialize profile: {ProfileName}", profileName);
                return null;
            }

            _logger.LogInformation("Successfully loaded secure profile: {ProfileName}", profileName);
            return profile;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Access denied when loading profile: {ProfileName}", profileName);
            return null;
        }
        catch (Exception ex)
        {
            using var scope = _logger.BeginScope("LoadProfile: ProfileName={ProfileName}", profileName);
            _logger.LogError(ex, "Failed to load profile");
            return null;
        }
    }

    /// <summary>
    /// Saves a profile configuration to secure storage.
    /// </summary>
    public async Task<bool> SaveProfileAsync(string profileName, Profile profile)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileName);
        ArgumentNullException.ThrowIfNull(profile);

        var filePath = GetProfileFilePath(profileName);

        try
        {
            
            // Serialize the profile configuration
            var content = JsonSerializer.Serialize(profile, _jsonOptions);
            
            // Write the file securely
            var tempFilePath = $"{filePath}.tmp";
            await OptimizedFileOperations.WriteTextAtomicAsync(tempFilePath, content);

            // Apply restrictive ACLs to the temporary file
            if (!_aclManager.SecureFile(tempFilePath))
            {
                _logger.LogWarning("Failed to secure temporary profile file: {TempFilePath}", tempFilePath);
                // Continue anyway, but log the security issue
            }

            // Atomic replace: move temp file to actual file
            if (File.Exists(filePath))
            {
                File.Replace(tempFilePath, filePath, null);
            }
            else
            {
                File.Move(tempFilePath, filePath);
            }

            // Ensure the final file has proper ACLs
            if (!_aclManager.SecureFile(filePath))
            {
                _logger.LogWarning("Failed to secure profile file after save: {FilePath}", filePath);
            }

            _logger.LogInformation("Successfully saved secure profile: {ProfileName}", profileName);
            return true;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Access denied when saving profile: {ProfileName}", profileName);
            return false;
        }
        catch (Exception ex)
        {
            using var scope = _logger.BeginScope("SaveProfile: ProfileName={ProfileName}", profileName);
            _logger.LogError(ex, "Failed to save profile");
            return false;
        }
    }

    /// <summary>
    /// Deletes a profile configuration from secure storage.
    /// </summary>
    public async Task<bool> DeleteProfileAsync(string profileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileName);

        try
        {
            var filePath = GetProfileFilePath(profileName);
            
            if (!File.Exists(filePath))
            {
                _logger.LogDebug("Profile file not found, nothing to delete: {ProfileName}", profileName);
                return true; // Consider it successful if already gone
            }

            // Securely delete the file (overwrite with zeros before deletion)
            await SecureDeleteFileAsync(filePath);
            
            _logger.LogInformation("Successfully deleted secure profile: {ProfileName}", profileName);
            return true;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Access denied when deleting profile: {ProfileName}", profileName);
            return false;
        }
        catch (Exception ex)
        {
            using var scope = _logger.BeginScope("DeleteProfile: ProfileName={ProfileName}", profileName);
            _logger.LogError(ex, "Failed to delete profile");
            return false;
        }
    }

    /// <summary>
    /// Lists all available profile names.
    /// </summary>
    public async Task<IEnumerable<string>> ListProfilesAsync()
    {
        try
        {
            if (!Directory.Exists(_profilesDirectory))
            {
                _logger.LogDebug("Profiles directory does not exist");
                return Enumerable.Empty<string>();
            }

            // Get all .json files in the profiles directory
            var profileFiles = Directory.GetFiles(_profilesDirectory, "*.json")
                .Select(Path.GetFileNameWithoutExtension)
                .Where(name => !string.IsNullOrWhiteSpace(name) && !name.StartsWith('.'))
                .ToList();

            // Verify security of each profile file
            foreach (var profileName in profileFiles)
            {
                var filePath = GetProfileFilePath(profileName!);
                if (!_aclManager.IsFileSecure(filePath))
                {
                    _logger.LogWarning("Profile file has insecure permissions: {ProfileName}", profileName);
                    // Optionally secure it automatically
                    _aclManager.SecureFile(filePath);
                }
            }

            _logger.LogDebug("Found {Count} secure profiles", profileFiles.Count);
            return await Task.FromResult<IEnumerable<string>>(profileFiles.Where(f => f != null)!);
        }
        catch (Exception ex)
        {
            using var scope = _logger.BeginScope("ListProfiles");
            _logger.LogError(ex, "Failed to list profiles");
            return Enumerable.Empty<string>();
        }
    }

    /// <summary>
    /// Checks if a profile exists in secure storage.
    /// </summary>
    public async Task<bool> ProfileExistsAsync(string profileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileName);

        try
        {
            var filePath = GetProfileFilePath(profileName);
            var exists = File.Exists(filePath);
            
            if (exists)
            {
                // Verify security while we're checking
                if (!_aclManager.IsFileSecure(filePath))
                {
                    _logger.LogWarning("Profile file exists but has insecure permissions: {ProfileName}", profileName);
                    _aclManager.SecureFile(filePath);
                }
            }

            return await Task.FromResult(exists);
        }
        catch (Exception ex)
        {
            using var scope = _logger.BeginScope("ProfileExists: ProfileName={ProfileName}", profileName);
            _logger.LogError(ex, "Failed to check if profile exists");
            return false;
        }
    }

    /// <summary>
    /// Gets the current active profile name from the .current file.
    /// </summary>
    public async Task<string?> GetCurrentProfileAsync()
    {
        try
        {
            var currentFilePath = Path.Combine(_profilesDirectory, ".current");
            
            if (!File.Exists(currentFilePath))
            {
                return null;
            }

            // Verify security of the .current file
            if (!_aclManager.IsFileSecure(currentFilePath))
            {
                _logger.LogWarning(".current file has insecure permissions, securing it");
                _aclManager.SecureFile(currentFilePath);
            }

            var currentProfile = await OptimizedFileOperations.ReadTextAsync(currentFilePath);
            return string.IsNullOrWhiteSpace(currentProfile) ? null : currentProfile.Trim();
        }
        catch (Exception ex)
        {
            using var scope = _logger.BeginScope("GetCurrentProfile");
            _logger.LogError(ex, "Failed to get current profile");
            return null;
        }
    }

    /// <summary>
    /// Sets the current active profile name in the .current file.
    /// </summary>
    public async Task<bool> SetCurrentProfileAsync(string? profileName)
    {
        try
        {
            var currentFilePath = Path.Combine(_profilesDirectory, ".current");
            
            if (string.IsNullOrWhiteSpace(profileName))
            {
                // Remove the .current file if no profile is active
                if (File.Exists(currentFilePath))
                {
                    await SecureDeleteFileAsync(currentFilePath);
                }
                return true;
            }

            // Write the current profile name
            await OptimizedFileOperations.WriteTextAtomicAsync(currentFilePath, profileName);
            
            // Secure the .current file
            if (!_aclManager.SecureFile(currentFilePath))
            {
                _logger.LogWarning("Failed to secure .current file");
            }

            return true;
        }
        catch (Exception ex)
        {
            using var scope = _logger.BeginScope("SetCurrentProfile: ProfileName={ProfileName}", profileName);
            _logger.LogError(ex, "Failed to set current profile");
            return false;
        }
    }

    private void InitializeSecureDirectory()
    {
        try
        {
            if (!_aclManager.CreateSecureDirectory(_profilesDirectory))
            {
                _logger.LogWarning("Failed to create secure profiles directory: {Directory}", _profilesDirectory);
            }
            else
            {
                _logger.LogInformation("Initialized secure profiles directory: {Directory}", _profilesDirectory);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize secure profiles directory: {Directory}", _profilesDirectory);
            // Continue anyway, individual operations will fail if directory is not accessible
        }
    }

    private string GetProfileFilePath(string profileName)
    {
        // Check for path separators to prevent path traversal
        if (profileName.Contains(Path.DirectorySeparatorChar) || 
            profileName.Contains(Path.AltDirectorySeparatorChar) || 
            profileName.Contains(".."))
        {
            throw new ArgumentException($"Invalid profile name: {profileName}");
        }

        // Sanitize the profile name to prevent path traversal
        var sanitizedName = Path.GetFileName(profileName);
        if (string.IsNullOrWhiteSpace(sanitizedName) || sanitizedName != profileName)
        {
            throw new ArgumentException($"Invalid profile name: {profileName}");
        }

        // Check for reserved Windows names
        var reservedNames = new[] { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" };
        if (reservedNames.Contains(sanitizedName.ToUpperInvariant()))
        {
            throw new ArgumentException($"Invalid profile name: {profileName}");
        }

        return Path.Combine(_profilesDirectory, $"{sanitizedName}.json");
    }

    private async Task SecureDeleteFileAsync(string filePath)
    {
        // Overwrite file with zeros before deletion for security
        if (File.Exists(filePath))
        {
            var fileInfo = new FileInfo(filePath);
            var fileSize = fileInfo.Length;
            
            if (fileSize > 0)
            {
                // Overwrite with zeros
                var zeros = new byte[Math.Min(fileSize, 4096)];
                using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Write, FileShare.None))
                {
                    for (long written = 0; written < fileSize; written += zeros.Length)
                    {
                        var bytesToWrite = (int)Math.Min(zeros.Length, fileSize - written);
                        await stream.WriteAsync(zeros.AsMemory(0, bytesToWrite));
                    }
                    await stream.FlushAsync();
                }
            }

            // Now delete the file
            File.Delete(filePath);
            _logger.LogDebug("Securely deleted file: {FilePath}", filePath);
        }
    }
}