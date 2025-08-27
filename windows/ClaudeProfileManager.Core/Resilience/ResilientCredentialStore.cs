using System.Security;
using ClaudeProfileManager.Core.Interfaces;
using ClaudeProfileManager.Core.Logging;
using ClaudeProfileManager.Core.Security;
using Microsoft.Extensions.Logging;

namespace ClaudeProfileManager.Core.Resilience;

/// <summary>
/// Resilient wrapper for ISecureCredentialStore that provides circuit breaker protection
/// </summary>
public class ResilientCredentialStore : ISecureCredentialStore
{
    private readonly ISecureCredentialStore _innerStore;
    private readonly CircuitBreakerManager _circuitBreakerManager;
    private readonly ILogger<ResilientCredentialStore> _logger;

    public ResilientCredentialStore(
        ISecureCredentialStore innerStore,
        CircuitBreakerManager circuitBreakerManager,
        ILogger<ResilientCredentialStore> logger)
    {
        _innerStore = innerStore ?? throw new ArgumentNullException(nameof(innerStore));
        _circuitBreakerManager = circuitBreakerManager ?? throw new ArgumentNullException(nameof(circuitBreakerManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<bool> SaveCredentialAsync(string profileName, string credential, string serviceType = Constants.ProfileManagerServiceName)
    {
        var circuitBreaker = _circuitBreakerManager.GetCircuitBreaker<bool>(
            $"SaveCredential_{serviceType}", 
            CreateCredentialOperationConfig());

        return await circuitBreaker.ExecuteAsync(async () =>
        {
            _logger.ExecutingSaveCredential(profileName);
            return await _innerStore.SaveCredentialAsync(profileName, credential, serviceType);
        });
    }

    /// <inheritdoc />
    public async Task<string?> GetCredentialAsync(string profileName, string serviceType = Constants.ProfileManagerServiceName)
    {
        var circuitBreaker = _circuitBreakerManager.GetCircuitBreaker<string?>(
            $"GetCredential_{serviceType}",
            CreateCredentialOperationConfig());

        return await circuitBreaker.ExecuteAsync(async () =>
        {
            _logger.ExecutingGetCredential(profileName);
            return await _innerStore.GetCredentialAsync(profileName, serviceType);
        });
    }

    /// <inheritdoc />
    public async Task<bool> DeleteCredentialAsync(string profileName, string serviceType = Constants.ProfileManagerServiceName)
    {
        var circuitBreaker = _circuitBreakerManager.GetCircuitBreaker<bool>(
            $"DeleteCredential_{serviceType}",
            CreateCredentialOperationConfig());

        return await circuitBreaker.ExecuteAsync(async () =>
        {
            _logger.ExecutingDeleteCredential(profileName);
            return await _innerStore.DeleteCredentialAsync(profileName, serviceType);
        });
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> ListProfilesAsync(string serviceType = Constants.ProfileManagerServiceName)
    {
        var circuitBreaker = _circuitBreakerManager.GetCircuitBreaker<IEnumerable<string>>(
            $"ListProfiles_{serviceType}",
            CreateCredentialOperationConfig());

        return await circuitBreaker.ExecuteAsync(async () =>
        {
            _logger.ExecutingListProfiles(serviceType);
            return await _innerStore.ListProfilesAsync(serviceType);
        });
    }

    /// <inheritdoc />
    public async Task<bool> SaveSecureCredentialAsync(string profileName, SecureString secureCredential, string serviceType = Constants.ProfileManagerServiceName)
    {
        var circuitBreaker = _circuitBreakerManager.GetCircuitBreaker<bool>(
            $"SaveSecureCredential_{serviceType}",
            CreateCredentialOperationConfig());

        return await circuitBreaker.ExecuteAsync(async () =>
        {
            _logger.ExecutingSaveSecureCredential(profileName);
            return await _innerStore.SaveSecureCredentialAsync(profileName, secureCredential, serviceType);
        });
    }

    /// <inheritdoc />
    public async Task<SecureCredentialScope?> GetSecureCredentialAsync(string profileName, string serviceType = Constants.ProfileManagerServiceName)
    {
        var circuitBreaker = _circuitBreakerManager.GetCircuitBreaker<SecureCredentialScope?>(
            $"GetSecureCredential_{serviceType}",
            CreateCredentialOperationConfig());

        return await circuitBreaker.ExecuteAsync(async () =>
        {
            _logger.ExecutingGetSecureCredential(profileName);
            return await _innerStore.GetSecureCredentialAsync(profileName, serviceType);
        });
    }

    /// <summary>
    /// Gets circuit breaker metrics for monitoring credential store operations
    /// </summary>
    public Dictionary<string, CircuitBreakerMetrics> GetCircuitBreakerMetrics()
    {
        return _circuitBreakerManager.GetAllMetrics();
    }

    /// <summary>
    /// Resets all circuit breakers (for testing/emergency use)
    /// </summary>
    public void ResetCircuitBreakers()
    {
        _circuitBreakerManager.ResetAll();
    }

    private static CircuitBreakerConfiguration CreateCredentialOperationConfig()
    {
        return new CircuitBreakerConfiguration
        {
            // Credential operations are critical but should fail fast if Windows Credential Manager is having issues
            FailureThreshold = 3,           // Open circuit after 3 consecutive failures
            RecoveryTimeout = TimeSpan.FromSeconds(10), // Try recovery after 10 seconds
            SuccessThreshold = 2,           // Need 2 successes to fully close circuit
            HalfOpenMaxConcurrency = 1      // Only allow 1 operation when testing recovery
        };
    }
}