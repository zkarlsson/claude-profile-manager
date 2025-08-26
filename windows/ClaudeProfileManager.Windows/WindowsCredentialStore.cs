using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using ClaudeProfileManager.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace ClaudeProfileManager.Windows;

/// <summary>
/// Windows implementation of ICredentialStore using Windows Credential Manager.
/// </summary>
public class WindowsCredentialStore : ICredentialStore
{
    private readonly ILogger<WindowsCredentialStore> _logger;

    public WindowsCredentialStore(ILogger<WindowsCredentialStore> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<bool> SaveCredentialAsync(string profileName, string credential, string serviceType = "Claude Profile Manager")
    {
        try
        {
            if (string.IsNullOrWhiteSpace(profileName))
            {
                _logger.LogError("Profile name cannot be null or empty");
                return Task.FromResult(false);
            }
            
            if (string.IsNullOrEmpty(credential))
            {
                _logger.LogError("Credential cannot be null or empty");
                return Task.FromResult(false);
            }
            
            _logger.LogDebug("Saving credential for profile '{ProfileName}' with service '{ServiceType}'", profileName, serviceType);

            var credentialBlob = Encoding.UTF8.GetBytes(credential);
            var target = GetTargetName(profileName, serviceType);

            var winCredential = new CREDENTIAL
            {
                Type = CRED_TYPE.GENERIC,
                TargetName = target,
                CredentialBlob = Marshal.AllocHGlobal(credentialBlob.Length),
                CredentialBlobSize = credentialBlob.Length,
                Persist = CRED_PERSIST.LOCAL_MACHINE,
                UserName = Environment.UserName,
                Comment = $"Claude Profile Manager credential for profile '{profileName}'"
            };

            try
            {
                Marshal.Copy(credentialBlob, 0, winCredential.CredentialBlob, credentialBlob.Length);
                
                var result = CredWrite(ref winCredential, 0);
                if (!result)
                {
                    var error = Marshal.GetLastWin32Error();
                    _logger.LogError("Failed to save credential. Win32 error: {Error}", error);
                    return Task.FromResult(false);
                }

                _logger.LogDebug("Successfully saved credential for profile '{ProfileName}'", profileName);
                return Task.FromResult(true);
            }
            finally
            {
                if (winCredential.CredentialBlob != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(winCredential.CredentialBlob);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while saving credential for profile '{ProfileName}'", profileName);
            return Task.FromResult(false);
        }
    }

    /// <inheritdoc />
    public Task<string?> GetCredentialAsync(string profileName, string serviceType = "Claude Profile Manager")
    {
        try
        {
            _logger.LogDebug("Retrieving credential for profile '{ProfileName}' with service '{ServiceType}'", profileName, serviceType);

            var target = GetTargetName(profileName, serviceType);
            
            if (CredRead(target, CRED_TYPE.GENERIC, 0, out var credentialPtr))
            {
                try
                {
                    var credential = Marshal.PtrToStructure<CREDENTIAL>(credentialPtr);
                    if (credential.CredentialBlob != IntPtr.Zero && credential.CredentialBlobSize > 0)
                    {
                        var credentialBytes = new byte[credential.CredentialBlobSize];
                        Marshal.Copy(credential.CredentialBlob, credentialBytes, 0, credential.CredentialBlobSize);
                        var result = Encoding.UTF8.GetString(credentialBytes);
                        
                        _logger.LogDebug("Successfully retrieved credential for profile '{ProfileName}'", profileName);
                        return Task.FromResult<string?>(result);
                    }
                }
                finally
                {
                    CredFree(credentialPtr);
                }
            }

            var error = Marshal.GetLastWin32Error();
            if (error != ERROR_NOT_FOUND)
            {
                _logger.LogError("Failed to retrieve credential. Win32 error: {Error}", error);
            }
            else
            {
                _logger.LogDebug("Credential not found for profile '{ProfileName}'", profileName);
            }

            return Task.FromResult<string?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while retrieving credential for profile '{ProfileName}'", profileName);
            return Task.FromResult<string?>(null);
        }
    }

    /// <inheritdoc />
    public Task<bool> DeleteCredentialAsync(string profileName, string serviceType = "Claude Profile Manager")
    {
        try
        {
            _logger.LogDebug("Deleting credential for profile '{ProfileName}' with service '{ServiceType}'", profileName, serviceType);

            var target = GetTargetName(profileName, serviceType);
            
            var result = CredDelete(target, CRED_TYPE.GENERIC, 0);
            if (!result)
            {
                var error = Marshal.GetLastWin32Error();
                if (error == ERROR_NOT_FOUND)
                {
                    _logger.LogDebug("Credential not found for profile '{ProfileName}' (already deleted)", profileName);
                    return Task.FromResult(true); // Treat "not found" as success for deletion
                }
                
                _logger.LogError("Failed to delete credential. Win32 error: {Error}", error);
                return Task.FromResult(false);
            }

            _logger.LogDebug("Successfully deleted credential for profile '{ProfileName}'", profileName);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while deleting credential for profile '{ProfileName}'", profileName);
            return Task.FromResult(false);
        }
    }

    /// <inheritdoc />
    public Task<IEnumerable<string>> ListProfilesAsync(string serviceType = "Claude Profile Manager")
    {
        try
        {
            _logger.LogDebug("Listing profiles for service '{ServiceType}'", serviceType);

            var filter = $"{serviceType}:*";
            var profiles = new List<string>();

            if (CredEnumerate(filter, 0, out var count, out var credentialsPtr))
            {
                try
                {
                    for (int i = 0; i < count; i++)
                    {
                        var credentialPtr = Marshal.ReadIntPtr(credentialsPtr, i * IntPtr.Size);
                        var credential = Marshal.PtrToStructure<CREDENTIAL>(credentialPtr);
                        
                        if (!string.IsNullOrEmpty(credential.TargetName))
                        {
                            var profileName = ExtractProfileNameFromTarget(credential.TargetName, serviceType);
                            if (!string.IsNullOrEmpty(profileName))
                            {
                                profiles.Add(profileName);
                            }
                        }
                    }
                }
                finally
                {
                    CredFree(credentialsPtr);
                }
            }

            _logger.LogDebug("Found {Count} profiles for service '{ServiceType}'", profiles.Count, serviceType);
            return Task.FromResult<IEnumerable<string>>(profiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while listing profiles for service '{ServiceType}'", serviceType);
            return Task.FromResult<IEnumerable<string>>(Enumerable.Empty<string>());
        }
    }

    private static string GetTargetName(string profileName, string serviceType)
    {
        return $"{serviceType}:{profileName}";
    }

    private static string? ExtractProfileNameFromTarget(string targetName, string serviceType)
    {
        var prefix = $"{serviceType}:";
        if (targetName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return targetName.Substring(prefix.Length);
        }
        return null;
    }

    #region Windows Credential Manager P/Invoke

    private const int ERROR_NOT_FOUND = 1168;

    [DllImport("Advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredRead(string target, CRED_TYPE type, int reservedFlag, out IntPtr credentialPtr);

    [DllImport("Advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredWrite([In] ref CREDENTIAL userCredential, [In] int flags);

    [DllImport("Advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredDelete(string target, CRED_TYPE type, int flags);

    [DllImport("Advapi32.dll", EntryPoint = "CredEnumerateW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredEnumerate(string? filter, int flags, out int count, out IntPtr pCredentials);

    [DllImport("Advapi32.dll")]
    private static extern bool CredFree([In] IntPtr buffer);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct CREDENTIAL
    {
        public int Flags;
        public CRED_TYPE Type;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string TargetName;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string Comment;
        public DateTime LastWritten;
        public int CredentialBlobSize;
        public IntPtr CredentialBlob;
        public CRED_PERSIST Persist;
        public int AttributeCount;
        public IntPtr Attributes;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string TargetAlias;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string UserName;
    }

    private enum CRED_TYPE : int
    {
        GENERIC = 1,
        DOMAIN_PASSWORD = 2,
        DOMAIN_CERTIFICATE = 3,
        DOMAIN_VISIBLE_PASSWORD = 4,
        GENERIC_CERTIFICATE = 5,
        DOMAIN_EXTENDED = 6,
        MAXIMUM = 7,
        MAXIMUM_EX = (MAXIMUM + 1000)
    }

    private enum CRED_PERSIST : int
    {
        SESSION = 1,
        LOCAL_MACHINE = 2,
        ENTERPRISE = 3
    }

    #endregion
}