using System.Security;
using ClaudeProfileManager.Core.Security;

namespace ClaudeProfileManager.Core.Interfaces;

/// <summary>
/// Enhanced credential store interface with secure memory handling
/// </summary>
public interface ISecureCredentialStore : ICredentialStore
{
    /// <summary>
    /// Saves a credential using SecureString for memory protection
    /// </summary>
    /// <param name="profileName">Name of the profile or credential identifier</param>
    /// <param name="secureCredential">Credential data in SecureString format</param>
    /// <param name="serviceType">Service type identifier (default: "Claude Profile Manager")</param>
    /// <returns>True if the credential was saved successfully</returns>
    Task<bool> SaveSecureCredentialAsync(string profileName, SecureString secureCredential, string serviceType = "Claude Profile Manager");

    /// <summary>
    /// Retrieves a credential as a secure scope for automatic cleanup
    /// </summary>
    /// <param name="profileName">Name of the profile or credential identifier</param>
    /// <param name="serviceType">Service type identifier (default: "Claude Profile Manager")</param>
    /// <returns>SecureCredentialScope that automatically cleans up memory on disposal</returns>
    Task<SecureCredentialScope?> GetSecureCredentialAsync(string profileName, string serviceType = "Claude Profile Manager");
}