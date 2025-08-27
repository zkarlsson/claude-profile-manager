using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.Extensions.Logging;

namespace ClaudeProfileManager.Windows.Security;

/// <summary>
/// Manages Windows Access Control Lists (ACLs) for secure file and directory permissions.
/// Provides enhanced security by restricting access to sensitive files to the current user only.
/// </summary>
[SupportedOSPlatform("windows")]
public class WindowsAclManager
{
    private readonly ILogger<WindowsAclManager> _logger;

    public WindowsAclManager(ILogger<WindowsAclManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Sets restrictive ACLs on a file to allow access only to the current user.
    /// Removes all inherited permissions and grants full control only to the owner.
    /// </summary>
    /// <param name="filePath">Path to the file to secure</param>
    /// <returns>True if successful, false otherwise</returns>
    public bool SecureFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        try
        {
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("File not found: {FilePath}", filePath);
                return false;
            }

            // Get the current user's security identifier
            var currentUser = WindowsIdentity.GetCurrent();
            var userSid = currentUser.User;
            
            if (userSid == null)
            {
                _logger.LogError("Unable to get current user SID");
                return false;
            }

            // Create a new FileSecurity object
            var fileSecurity = new FileSecurity();

            // Disable inheritance and remove all inherited rules
            fileSecurity.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);

            // Grant full control to the current user only
            var userAccessRule = new FileSystemAccessRule(
                userSid,
                FileSystemRights.FullControl,
                AccessControlType.Allow);
            
            fileSecurity.AddAccessRule(userAccessRule);

            // Optionally add SYSTEM account for system operations (configurable)
            var systemSid = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
            var systemAccessRule = new FileSystemAccessRule(
                systemSid,
                FileSystemRights.FullControl,
                AccessControlType.Allow);
            
            fileSecurity.AddAccessRule(systemAccessRule);

            // Apply the security settings to the file
            var fileInfo = new FileInfo(filePath);
            fileInfo.SetAccessControl(fileSecurity);

            _logger.LogInformation("Successfully secured file: {FilePath}", filePath);
            return true;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Unauthorized access while securing file: {FilePath}", filePath);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to secure file: {FilePath}", filePath);
            return false;
        }
    }

    /// <summary>
    /// Sets restrictive ACLs on a directory to allow access only to the current user.
    /// Removes all inherited permissions and grants full control only to the owner.
    /// </summary>
    /// <param name="directoryPath">Path to the directory to secure</param>
    /// <param name="applyToSubItems">Whether to apply permissions to all files and subdirectories</param>
    /// <returns>True if successful, false otherwise</returns>
    public bool SecureDirectory(string directoryPath, bool applyToSubItems = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);

        try
        {
            if (!Directory.Exists(directoryPath))
            {
                _logger.LogWarning("Directory not found: {DirectoryPath}", directoryPath);
                return false;
            }

            // Get the current user's security identifier
            var currentUser = WindowsIdentity.GetCurrent();
            var userSid = currentUser.User;
            
            if (userSid == null)
            {
                _logger.LogError("Unable to get current user SID");
                return false;
            }

            // Create a new DirectorySecurity object
            var directorySecurity = new DirectorySecurity();

            // Disable inheritance and remove all inherited rules
            directorySecurity.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);

            // Define inheritance flags for directory permissions
            var inheritanceFlags = applyToSubItems 
                ? InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit
                : InheritanceFlags.None;

            // Grant full control to the current user only
            var userAccessRule = new FileSystemAccessRule(
                userSid,
                FileSystemRights.FullControl,
                inheritanceFlags,
                PropagationFlags.None,
                AccessControlType.Allow);
            
            directorySecurity.AddAccessRule(userAccessRule);

            // Optionally add SYSTEM account for system operations
            var systemSid = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
            var systemAccessRule = new FileSystemAccessRule(
                systemSid,
                FileSystemRights.FullControl,
                inheritanceFlags,
                PropagationFlags.None,
                AccessControlType.Allow);
            
            directorySecurity.AddAccessRule(systemAccessRule);

            // Apply the security settings to the directory
            var directoryInfo = new DirectoryInfo(directoryPath);
            directoryInfo.SetAccessControl(directorySecurity);

            _logger.LogInformation("Successfully secured directory: {DirectoryPath}", directoryPath);

            // Optionally apply to all existing subdirectories and files
            if (applyToSubItems)
            {
                ApplySecurityToSubItems(directoryPath, userSid, systemSid);
            }

            return true;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Unauthorized access while securing directory: {DirectoryPath}", directoryPath);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to secure directory: {DirectoryPath}", directoryPath);
            return false;
        }
    }

    /// <summary>
    /// Checks if a file has restrictive ACLs (accessible only by owner and SYSTEM).
    /// </summary>
    /// <param name="filePath">Path to the file to check</param>
    /// <returns>True if file has restrictive ACLs, false otherwise</returns>
    public bool IsFileSecure(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        try
        {
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("File not found: {FilePath}", filePath);
                return false;
            }

            var fileInfo = new FileInfo(filePath);
            var fileSecurity = fileInfo.GetAccessControl();
            
            // Get current user and system SIDs
            var currentUser = WindowsIdentity.GetCurrent();
            var userSid = currentUser.User;
            var systemSid = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
            var adminSid = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);

            // Check if inheritance is disabled
            if (!fileSecurity.AreAccessRulesProtected)
            {
                _logger.LogDebug("File has inherited permissions: {FilePath}", filePath);
                return false;
            }

            // Get all access rules
            var rules = fileSecurity.GetAccessRules(true, false, typeof(SecurityIdentifier));
            
            foreach (FileSystemAccessRule rule in rules)
            {
                var sid = rule.IdentityReference as SecurityIdentifier;
                if (sid == null) continue;

                // Allow only current user, SYSTEM, and Administrators
                if (userSid != null && !sid.Equals(userSid) && !sid.Equals(systemSid) && !sid.Equals(adminSid))
                {
                    _logger.LogDebug("File has permissions for unauthorized SID {Sid}: {FilePath}", sid.Value, filePath);
                    return false;
                }

                // Check for deny rules
                if (rule.AccessControlType == AccessControlType.Deny)
                {
                    _logger.LogDebug("File has deny rules: {FilePath}", filePath);
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check file security: {FilePath}", filePath);
            return false;
        }
    }

    /// <summary>
    /// Gets effective permissions for the current user on a file.
    /// </summary>
    /// <param name="filePath">Path to the file</param>
    /// <returns>FileSystemRights for the current user, or null if unable to determine</returns>
    public FileSystemRights? GetEffectivePermissions(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        try
        {
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("File not found: {FilePath}", filePath);
                return null;
            }

            var fileInfo = new FileInfo(filePath);
            var fileSecurity = fileInfo.GetAccessControl();
            var currentUser = WindowsIdentity.GetCurrent();
            
            FileSystemRights effectiveRights = 0;
            
            // Check for user-specific permissions
            var userRules = fileSecurity.GetAccessRules(true, true, typeof(SecurityIdentifier))
                .Cast<FileSystemAccessRule>()
                .Where(r => currentUser.Groups?.Contains(r.IdentityReference) == true || 
                           r.IdentityReference.Equals(currentUser.User));

            foreach (var rule in userRules)
            {
                if (rule.AccessControlType == AccessControlType.Allow)
                {
                    effectiveRights |= rule.FileSystemRights;
                }
                else if (rule.AccessControlType == AccessControlType.Deny)
                {
                    effectiveRights &= ~rule.FileSystemRights;
                }
            }

            _logger.LogDebug("Effective permissions for {FilePath}: {Permissions}", filePath, effectiveRights);
            return effectiveRights;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get effective permissions: {FilePath}", filePath);
            return null;
        }
    }

    /// <summary>
    /// Creates a directory with restrictive ACLs if it doesn't exist.
    /// </summary>
    /// <param name="directoryPath">Path to the directory to create</param>
    /// <returns>True if directory was created or already exists with proper security, false otherwise</returns>
    public bool CreateSecureDirectory(string directoryPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);

        try
        {
            if (Directory.Exists(directoryPath))
            {
                _logger.LogDebug("Directory already exists: {DirectoryPath}", directoryPath);
                return SecureDirectory(directoryPath, false);
            }

            // Create the directory
            Directory.CreateDirectory(directoryPath);
            _logger.LogInformation("Created directory: {DirectoryPath}", directoryPath);

            // Apply restrictive ACLs
            return SecureDirectory(directoryPath, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create secure directory: {DirectoryPath}", directoryPath);
            return false;
        }
    }

    private void ApplySecurityToSubItems(string directoryPath, SecurityIdentifier userSid, SecurityIdentifier systemSid)
    {
        try
        {
            // Apply to all subdirectories
            foreach (var subDir in Directory.GetDirectories(directoryPath))
            {
                SecureDirectory(subDir, true);
            }

            // Apply to all files
            foreach (var file in Directory.GetFiles(directoryPath))
            {
                SecureFile(file);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to apply security to some sub-items in: {DirectoryPath}", directoryPath);
            // Continue processing other items even if some fail
        }
    }

    /// <summary>
    /// Removes all ACL entries except for the current user and SYSTEM.
    /// </summary>
    /// <param name="filePath">Path to the file to clean up</param>
    /// <returns>True if successful, false otherwise</returns>
    public bool RemoveUnauthorizedAccess(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        try
        {
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("File not found: {FilePath}", filePath);
                return false;
            }

            var fileInfo = new FileInfo(filePath);
            var fileSecurity = fileInfo.GetAccessControl();
            
            // Get authorized SIDs
            var currentUser = WindowsIdentity.GetCurrent();
            var userSid = currentUser.User;
            var systemSid = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
            var adminSid = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);

            // Get all access rules
            var rules = fileSecurity.GetAccessRules(true, false, typeof(SecurityIdentifier))
                .Cast<FileSystemAccessRule>()
                .ToList();

            // Remove unauthorized rules
            foreach (var rule in rules)
            {
                var sid = rule.IdentityReference as SecurityIdentifier;
                if (sid != null && userSid != null && !sid.Equals(userSid) && !sid.Equals(systemSid) && !sid.Equals(adminSid))
                {
                    fileSecurity.RemoveAccessRule(rule);
                    _logger.LogInformation("Removed access for SID {Sid} from {FilePath}", sid.Value, filePath);
                }
            }

            // Apply the modified security settings
            fileInfo.SetAccessControl(fileSecurity);
            
            _logger.LogInformation("Successfully removed unauthorized access from: {FilePath}", filePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove unauthorized access: {FilePath}", filePath);
            return false;
        }
    }
}