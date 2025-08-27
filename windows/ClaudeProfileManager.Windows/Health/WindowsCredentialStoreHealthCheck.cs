using ClaudeProfileManager.Core.Health;
using ClaudeProfileManager.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace ClaudeProfileManager.Windows.Health;

/// <summary>
/// Health check for Windows Credential Store functionality.
/// Tests basic credential operations to ensure the Windows Credential Manager is accessible.
/// </summary>
public class WindowsCredentialStoreHealthCheck : IHealthCheck
{
    private readonly ISecureCredentialStore _credentialStore;
    private readonly ILogger<WindowsCredentialStoreHealthCheck> _logger;
    private const string TestServiceType = "Claude-Profile-Manager-Health-Check";
    private const string TestProfileName = "health-check-test";
    private const string TestCredential = "health-check-credential-12345";

    public WindowsCredentialStoreHealthCheck(
        ISecureCredentialStore credentialStore, 
        ILogger<WindowsCredentialStoreHealthCheck> logger)
    {
        _credentialStore = credentialStore ?? throw new ArgumentNullException(nameof(credentialStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string Name => "windows-credential-store";

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>();
        
        try
        {
            // Test 1: Save a test credential
            _logger.LogDebug("Testing credential save operation");
            var saveStartTime = DateTime.UtcNow;
            var saveResult = await _credentialStore.SaveCredentialAsync(TestProfileName, TestCredential, TestServiceType);
            var saveDuration = DateTime.UtcNow - saveStartTime;
            data["SaveDurationMs"] = saveDuration.TotalMilliseconds;
            
            if (!saveResult)
            {
                return HealthCheckResult.Unhealthy("Failed to save test credential to Windows Credential Manager", data: data);
            }

            // Test 2: Retrieve the test credential
            _logger.LogDebug("Testing credential retrieval operation");
            var getStartTime = DateTime.UtcNow;
            var retrievedCredential = await _credentialStore.GetCredentialAsync(TestProfileName, TestServiceType);
            var getDuration = DateTime.UtcNow - getStartTime;
            data["GetDurationMs"] = getDuration.TotalMilliseconds;
            
            if (retrievedCredential != TestCredential)
            {
                await CleanupTestCredentialAsync(); // Attempt cleanup
                return HealthCheckResult.Unhealthy(
                    $"Retrieved credential does not match saved credential. Expected: '{TestCredential}', Got: '{retrievedCredential}'", 
                    data: data);
            }

            // Test 3: List profiles (should include our test profile)
            _logger.LogDebug("Testing profile listing operation");
            var listStartTime = DateTime.UtcNow;
            var profiles = await _credentialStore.ListProfilesAsync(TestServiceType);
            var listDuration = DateTime.UtcNow - listStartTime;
            data["ListDurationMs"] = listDuration.TotalMilliseconds;
            data["ProfileCount"] = profiles.Count();
            
            if (!profiles.Contains(TestProfileName))
            {
                await CleanupTestCredentialAsync(); // Attempt cleanup
                return HealthCheckResult.Unhealthy(
                    $"Test profile '{TestProfileName}' not found in profile list", 
                    data: data);
            }

            // Test 4: Delete the test credential
            _logger.LogDebug("Testing credential deletion operation");
            var deleteStartTime = DateTime.UtcNow;
            var deleteResult = await _credentialStore.DeleteCredentialAsync(TestProfileName, TestServiceType);
            var deleteDuration = DateTime.UtcNow - deleteStartTime;
            data["DeleteDurationMs"] = deleteDuration.TotalMilliseconds;
            
            if (!deleteResult)
            {
                await CleanupTestCredentialAsync(); // Attempt cleanup
                return HealthCheckResult.Unhealthy("Failed to delete test credential from Windows Credential Manager", data: data);
            }

            // Test 5: Verify deletion (credential should no longer exist)
            var verifyStartTime = DateTime.UtcNow;
            var verifyCredential = await _credentialStore.GetCredentialAsync(TestProfileName, TestServiceType);
            var verifyDuration = DateTime.UtcNow - verifyStartTime;
            data["VerifyDurationMs"] = verifyDuration.TotalMilliseconds;
            
            if (verifyCredential != null)
            {
                await CleanupTestCredentialAsync(); // Attempt cleanup
                return HealthCheckResult.Unhealthy(
                    "Test credential still exists after deletion", 
                    data: data);
            }

            // Calculate performance metrics
            var totalDuration = saveDuration + getDuration + listDuration + deleteDuration + verifyDuration;
            data["TotalDurationMs"] = totalDuration.TotalMilliseconds;

            // Determine health status based on performance
            if (totalDuration.TotalMilliseconds > 5000) // 5 seconds
            {
                return HealthCheckResult.Degraded(
                    $"Windows Credential Manager operations are slow (took {totalDuration.TotalMilliseconds:F0}ms)", 
                    data);
            }
            else if (totalDuration.TotalMilliseconds > 10000) // 10 seconds
            {
                return HealthCheckResult.Unhealthy(
                    $"Windows Credential Manager operations are very slow (took {totalDuration.TotalMilliseconds:F0}ms)", 
                    data: data);
            }

            return HealthCheckResult.Healthy(
                $"Windows Credential Manager is healthy (all operations completed in {totalDuration.TotalMilliseconds:F0}ms)", 
                data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Windows Credential Store health check failed");
            
            // Attempt cleanup even if the main health check failed
            await CleanupTestCredentialAsync();
            
            return HealthCheckResult.Unhealthy(
                $"Windows Credential Store health check failed: {ex.Message}",
                ex,
                data);
        }
    }

    /// <summary>
    /// Attempts to clean up the test credential if it still exists.
    /// This is a best-effort cleanup that doesn't throw exceptions.
    /// </summary>
    private async Task CleanupTestCredentialAsync()
    {
        try
        {
            _logger.LogDebug("Attempting to clean up test credential");
            await _credentialStore.DeleteCredentialAsync(TestProfileName, TestServiceType);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clean up test credential during health check");
            // Don't throw - cleanup is best effort
        }
    }
}