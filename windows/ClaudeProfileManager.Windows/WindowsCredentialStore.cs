using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using ClaudeProfileManager.Core;
using ClaudeProfileManager.Core.Interfaces;
using ClaudeProfileManager.Core.Resilience;
using ClaudeProfileManager.Core.Security;
using ClaudeProfileManager.Core.Services;
using ClaudeProfileManager.Windows.Logging;
using Microsoft.Extensions.Logging;

namespace ClaudeProfileManager.Windows;

/// <summary>
/// Windows implementation of ICredentialStore using Windows Credential Manager.
/// </summary>
public class WindowsCredentialStore : ISecureCredentialStore
{
    private readonly ILogger<WindowsCredentialStore> _logger;

    public WindowsCredentialStore(ILogger<WindowsCredentialStore> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<bool> SaveCredentialAsync(string profileName, string credential, string serviceType = Constants.ProfileManagerServiceName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(profileName))
            {
                _logger.ProfileNameNullOrEmpty();
                return Task.FromResult(false);
            }
            
            if (string.IsNullOrEmpty(credential))
            {
                _logger.CredentialNullOrEmpty();
                return Task.FromResult(false);
            }
            
            _logger.SavingCredential(profileName, serviceType);

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
                    StandardizedErrorHandler.HandleWin32Error(_logger, "CredWrite", error, $"ProfileName: {profileName}");
                    return Task.FromResult(false);
                }

                _logger.CredentialSaved(profileName);
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
            StandardizedErrorHandler.HandleException(_logger, ex, "SaveCredential", $"ProfileName: {profileName}");
            return Task.FromResult(false);
        }
    }

    /// <inheritdoc />
    public Task<string?> GetCredentialAsync(string profileName, string serviceType = Constants.ProfileManagerServiceName)
    {
        try
        {
            _logger.RetrievingCredential(profileName, serviceType);

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
                        
                        _logger.CredentialRetrieved(profileName);
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
                StandardizedErrorHandler.HandleWin32Error(_logger, "CredRead", error, $"ProfileName: {profileName}");
            }
            else
            {
                _logger.CredentialNotFound(profileName);
            }

            return Task.FromResult<string?>(null);
        }
        catch (Exception ex)
        {
            StandardizedErrorHandler.HandleException<WindowsCredentialStore, string>(_logger, ex, "GetCredential", $"ProfileName: {profileName}");
            return Task.FromResult<string?>(null);
        }
    }

    /// <inheritdoc />
    public Task<bool> DeleteCredentialAsync(string profileName, string serviceType = Constants.ProfileManagerServiceName)
    {
        try
        {
            _logger.DeletingCredential(profileName, serviceType);

            var target = GetTargetName(profileName, serviceType);
            
            var result = CredDelete(target, CRED_TYPE.GENERIC, 0);
            if (!result)
            {
                var error = Marshal.GetLastWin32Error();
                if (error == ERROR_NOT_FOUND)
                {
                    _logger.CredentialAlreadyDeleted(profileName);
                    return Task.FromResult(true); // Treat "not found" as success for deletion
                }
                
                StandardizedErrorHandler.HandleWin32Error(_logger, "CredDelete", error, $"ProfileName: {profileName}");
                return Task.FromResult(false);
            }

            _logger.CredentialDeleted(profileName);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            StandardizedErrorHandler.HandleException(_logger, ex, "DeleteCredential", $"ProfileName: {profileName}");
            return Task.FromResult(false);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> ListProfilesAsync(string serviceType = Constants.ProfileManagerServiceName)
    {
        try
        {
            _logger.ListingProfiles(serviceType);

            var filter = string.Concat(serviceType, ":*");
            var profiles = new List<string>();

            // Retry logic to handle Windows Credential Manager timing issues
            var maxRetries = 3;
            var retryDelayMs = 100;
            
            for (int retry = 0; retry <= maxRetries; retry++)
            {
                if (CredEnumerate(filter, 0, out var count, out var credentialsPtr))
                {
                    try
                    {
                        profiles.Clear(); // Clear any previous results
                        
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
                    
                    break; // Success, exit retry loop
                }
                else
                {
                    var error = Marshal.GetLastWin32Error();
                    if (error == ERROR_NOT_FOUND)
                    {
                        // No credentials found is not an error for enumeration
                        _logger.NoCredentialsFound(serviceType);
                        break;
                    }
                    
                    if (retry < maxRetries)
                    {
                        _logger.EnumerationRetrying(retry + 1, error);
                        await Task.Delay(retryDelayMs * (retry + 1)); // Exponential backoff
                    }
                    else
                    {
                        StandardizedErrorHandler.HandleWin32Error(_logger, "CredEnumerate", error, $"ServiceType: {serviceType}");
                    }
                }
            }

            _logger.ProfilesFound(profiles.Count, serviceType);
            return profiles.AsEnumerable();
        }
        catch (Exception ex)
        {
            StandardizedErrorHandler.HandleException<WindowsCredentialStore, IEnumerable<string>>(_logger, ex, "ListProfiles", $"ServiceType: {serviceType}");
            return Enumerable.Empty<string>();
        }
    }

    private static string GetTargetName(string profileName, string serviceType)
    {
        return string.Concat(serviceType, ":", profileName);
    }

    private static string? ExtractProfileNameFromTarget(string targetName, string serviceType)
    {
        var prefix = string.Concat(serviceType, ":");
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

    #region Secure Credential Methods

    public async Task<bool> SaveSecureCredentialAsync(string profileName, SecureString secureCredential, string serviceType = Constants.ProfileManagerServiceName)
    {
        using var credentialScope = SecureCredentialHandler.GetCredential(secureCredential);
        return await SaveCredentialAsync(profileName, credentialScope.Value, serviceType);
    }

    public async Task<SecureCredentialScope?> GetSecureCredentialAsync(string profileName, string serviceType = Constants.ProfileManagerServiceName)
    {
        var credential = await GetCredentialAsync(profileName, serviceType);
        if (string.IsNullOrEmpty(credential))
            return null;

        var secureString = SecureCredentialHandler.ToSecureString(credential);
        return SecureCredentialHandler.GetCredential(secureString);
    }

    #endregion
}