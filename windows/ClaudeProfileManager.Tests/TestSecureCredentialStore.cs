using System.Security;
using ClaudeProfileManager.Core;
using ClaudeProfileManager.Core.Interfaces;
using ClaudeProfileManager.Core.Security;

namespace ClaudeProfileManager.Tests;

/// <summary>
/// In-memory secure credential store for testing purposes.
/// </summary>
public sealed class TestSecureCredentialStore : ISecureCredentialStore
{
    private readonly Dictionary<string, string> _credentials = new();
    
    /// <summary>
    /// Gets or sets an exception to throw during operations (for testing failure scenarios).
    /// </summary>
    public Exception? ShouldThrowException { get; set; }

    public Task<bool> SaveCredentialAsync(string profileName, string credential, string serviceType = Constants.ProfileManagerServiceName)
    {
        if (ShouldThrowException != null)
            throw ShouldThrowException;
            
        ArgumentNullException.ThrowIfNull(profileName);
        ArgumentNullException.ThrowIfNull(credential);
        ArgumentNullException.ThrowIfNull(serviceType);
        
        var key = string.IsNullOrEmpty(profileName) ? serviceType : $"{profileName}@{serviceType}";
        _credentials[key] = credential;
        return Task.FromResult(true);
    }

    public Task<string?> GetCredentialAsync(string profileName, string serviceType = Constants.ProfileManagerServiceName)
    {
        if (ShouldThrowException != null)
            throw ShouldThrowException;
            
        ArgumentNullException.ThrowIfNull(profileName);
        ArgumentNullException.ThrowIfNull(serviceType);
        
        var key = string.IsNullOrEmpty(profileName) ? serviceType : $"{profileName}@{serviceType}";
        _credentials.TryGetValue(key, out var credential);
        return Task.FromResult(credential);
    }

    public Task<bool> DeleteCredentialAsync(string profileName, string serviceType = Constants.ProfileManagerServiceName)
    {
        if (ShouldThrowException != null)
            throw ShouldThrowException;
            
        ArgumentNullException.ThrowIfNull(profileName);
        ArgumentNullException.ThrowIfNull(serviceType);
        
        var key = string.IsNullOrEmpty(profileName) ? serviceType : $"{profileName}@{serviceType}";
        var existed = _credentials.Remove(key);
        return Task.FromResult(existed);
    }

    public Task<IEnumerable<string>> ListProfilesAsync(string serviceType = Constants.ProfileManagerServiceName)
    {
        if (ShouldThrowException != null)
            throw ShouldThrowException;
            
        ArgumentNullException.ThrowIfNull(serviceType);
        
        var profiles = _credentials.Keys
            .Where(k => k.EndsWith($"@{serviceType}", StringComparison.Ordinal))
            .Select(k => k.Substring(0, k.LastIndexOf($"@{serviceType}", StringComparison.Ordinal)))
            .AsEnumerable();
            
        return Task.FromResult(profiles);
    }

    public async Task<bool> SaveSecureCredentialAsync(string profileName, SecureString secureCredential, string serviceType = Constants.ProfileManagerServiceName)
    {
        if (ShouldThrowException != null)
            throw ShouldThrowException;
            
        using var credentialScope = SecureCredentialHandler.GetCredential(secureCredential);
        return await SaveCredentialAsync(profileName, credentialScope.Value, serviceType);
    }

    public async Task<SecureCredentialScope?> GetSecureCredentialAsync(string profileName, string serviceType = Constants.ProfileManagerServiceName)
    {
        if (ShouldThrowException != null)
            throw ShouldThrowException;
            
        var credential = await GetCredentialAsync(profileName, serviceType);
        if (string.IsNullOrEmpty(credential))
            return null;

        var secureString = SecureCredentialHandler.ToSecureString(credential);
        return SecureCredentialHandler.GetCredential(secureString);
    }

    /// <summary>
    /// Clears all stored credentials (test helper method).
    /// </summary>
    public void Clear()
    {
        _credentials.Clear();
        ShouldThrowException = null;
    }

    /// <summary>
    /// Gets the number of stored credentials (test helper method).
    /// </summary>
    public int Count => _credentials.Count;
}