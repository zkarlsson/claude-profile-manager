namespace ClaudeProfileManager.Core.Interfaces;

/// <summary>
/// Provides secure storage and retrieval of authentication credentials.
/// </summary>
public interface ICredentialStore
{
    /// <summary>
    /// Saves a credential to secure storage.
    /// </summary>
    /// <param name="profileName">The profile name to associate with the credential</param>
    /// <param name="credential">The credential data to store securely</param>
    /// <param name="serviceType">The service type (e.g., "Claude Code", "Claude Profile Manager")</param>
    /// <returns>True if the credential was saved successfully</returns>
    Task<bool> SaveCredentialAsync(string profileName, string credential, string serviceType = "Claude Profile Manager");

    /// <summary>
    /// Retrieves a credential from secure storage.
    /// </summary>
    /// <param name="profileName">The profile name associated with the credential</param>
    /// <param name="serviceType">The service type (e.g., "Claude Code", "Claude Profile Manager")</param>
    /// <returns>The credential data if found, null otherwise</returns>
    Task<string?> GetCredentialAsync(string profileName, string serviceType = "Claude Profile Manager");

    /// <summary>
    /// Deletes a credential from secure storage.
    /// </summary>
    /// <param name="profileName">The profile name associated with the credential</param>
    /// <param name="serviceType">The service type (e.g., "Claude Code", "Claude Profile Manager")</param>
    /// <returns>True if the credential was deleted successfully or didn't exist</returns>
    Task<bool> DeleteCredentialAsync(string profileName, string serviceType = "Claude Profile Manager");

    /// <summary>
    /// Lists all profile names that have stored credentials for the specified service.
    /// </summary>
    /// <param name="serviceType">The service type to search for</param>
    /// <returns>A collection of profile names with stored credentials</returns>
    Task<IEnumerable<string>> ListProfilesAsync(string serviceType = "Claude Profile Manager");
}