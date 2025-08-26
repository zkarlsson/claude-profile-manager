using ClaudeProfileManager.Core.Interfaces;

namespace ClaudeProfileManager.Tests;

/// <summary>
/// In-memory credential store for testing purposes.
/// </summary>
public sealed class TestCredentialStore : ICredentialStore
{
    private readonly Dictionary<string, string> _credentials = new();

    public Task<bool> SaveCredentialAsync(string profileName, string credential, string serviceType = "Claude Profile Manager")
    {
        ArgumentNullException.ThrowIfNull(profileName);
        ArgumentNullException.ThrowIfNull(credential);
        ArgumentNullException.ThrowIfNull(serviceType);
        
        var key = string.IsNullOrEmpty(profileName) ? serviceType : $"{profileName}@{serviceType}";
        _credentials[key] = credential;
        return Task.FromResult(true);
    }

    public Task<string?> GetCredentialAsync(string profileName, string serviceType = "Claude Profile Manager")
    {
        ArgumentNullException.ThrowIfNull(profileName);
        ArgumentNullException.ThrowIfNull(serviceType);
        
        var key = string.IsNullOrEmpty(profileName) ? serviceType : $"{profileName}@{serviceType}";
        _credentials.TryGetValue(key, out var credential);
        return Task.FromResult(credential);
    }

    public Task<bool> DeleteCredentialAsync(string profileName, string serviceType = "Claude Profile Manager")
    {
        ArgumentNullException.ThrowIfNull(profileName);
        ArgumentNullException.ThrowIfNull(serviceType);
        
        var key = string.IsNullOrEmpty(profileName) ? serviceType : $"{profileName}@{serviceType}";
        var existed = _credentials.Remove(key);
        return Task.FromResult(existed);
    }

    public Task<IEnumerable<string>> ListProfilesAsync(string serviceType = "Claude Profile Manager")
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        
        var profiles = _credentials.Keys
            .Where(k => k.EndsWith($"@{serviceType}", StringComparison.Ordinal))
            .Select(k => k.Substring(0, k.LastIndexOf($"@{serviceType}", StringComparison.Ordinal)))
            .AsEnumerable();
            
        return Task.FromResult(profiles);
    }

    /// <summary>
    /// Clears all stored credentials (test helper method).
    /// </summary>
    public void Clear()
    {
        _credentials.Clear();
    }

    /// <summary>
    /// Gets the number of stored credentials (test helper method).
    /// </summary>
    public int Count => _credentials.Count;
}