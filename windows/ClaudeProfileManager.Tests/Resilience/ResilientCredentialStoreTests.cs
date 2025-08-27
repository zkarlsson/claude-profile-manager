using System.Security;
using ClaudeProfileManager.Core;
using ClaudeProfileManager.Core.Interfaces;
using ClaudeProfileManager.Core.Resilience;
using ClaudeProfileManager.Core.Security;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ClaudeProfileManager.Tests.Resilience;

public class ResilientCredentialStoreTests
{
    private readonly TestSecureCredentialStore _testInnerStore;
    private readonly CircuitBreakerManager _circuitBreakerManager;
    private readonly ILogger<ResilientCredentialStore> _logger;
    private readonly ResilientCredentialStore _resilientStore;
    
    private static readonly string[] ExpectedProfiles = ["profile1", "profile2"];

    public ResilientCredentialStoreTests()
    {
        _testInnerStore = new TestSecureCredentialStore();
        var loggerFactory = LoggerFactory.Create(builder => { });
        _circuitBreakerManager = new CircuitBreakerManager(loggerFactory);
        _logger = loggerFactory.CreateLogger<ResilientCredentialStore>();
        _resilientStore = new ResilientCredentialStore(_testInnerStore, _circuitBreakerManager, _logger);
    }

    [Fact]
    public async Task SaveCredentialAsync_WithSuccessfulOperation_ShouldReturnTrue()
    {
        // Arrange
        const string profileName = "test-profile";
        const string credential = "test-credential";

        // Act
        var result = await _resilientStore.SaveCredentialAsync(profileName, credential);

        // Assert
        result.Should().BeTrue();
        var savedCredential = await _testInnerStore.GetCredentialAsync(profileName);
        savedCredential.Should().Be(credential);
    }

    [Fact]
    public async Task GetCredentialAsync_WithSuccessfulOperation_ShouldReturnCredential()
    {
        // Arrange
        const string profileName = "test-profile";
        const string expectedCredential = "test-credential";
        await _testInnerStore.SaveCredentialAsync(profileName, expectedCredential);

        // Act
        var result = await _resilientStore.GetCredentialAsync(profileName);

        // Assert
        result.Should().Be(expectedCredential);
    }

    [Fact]
    public async Task DeleteCredentialAsync_WithSuccessfulOperation_ShouldReturnTrue()
    {
        // Arrange
        const string profileName = "test-profile";
        const string credential = "test-credential";
        await _testInnerStore.SaveCredentialAsync(profileName, credential);

        // Act
        var result = await _resilientStore.DeleteCredentialAsync(profileName);

        // Assert
        result.Should().BeTrue();
        var deletedCredential = await _testInnerStore.GetCredentialAsync(profileName);
        deletedCredential.Should().BeNull();
    }

    [Fact]
    public async Task ListProfilesAsync_WithSuccessfulOperation_ShouldReturnProfiles()
    {
        // Arrange
        await _testInnerStore.SaveCredentialAsync("profile1", "credential1");
        await _testInnerStore.SaveCredentialAsync("profile2", "credential2");

        // Act
        var result = await _resilientStore.ListProfilesAsync();

        // Assert
        result.Should().Contain(ExpectedProfiles);
    }

    [Fact]
    public async Task SaveCredentialAsync_WithConsecutiveFailures_ShouldOpenCircuitBreaker()
    {
        // Arrange
        const string profileName = "test-profile";
        const string credential = "test-credential";
        var serviceType = Constants.ProfileManagerServiceName;
        
        _testInnerStore.ShouldThrowException = new InvalidOperationException("Simulated failure");

        // Act & Assert - First 3 failures should be propagated
        for (int i = 0; i < 3; i++)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _resilientStore.SaveCredentialAsync(profileName, credential, serviceType));
        }

        // 4th attempt should throw CircuitBreakerOpenException
        var exception = await Assert.ThrowsAsync<CircuitBreakerOpenException>(
            () => _resilientStore.SaveCredentialAsync(profileName, credential, serviceType));
        
        exception.CircuitBreakerName.Should().Be($"SaveCredential_{serviceType}");
    }

    [Fact]
    public async Task SaveSecureCredentialAsync_WithSuccessfulOperation_ShouldReturnTrue()
    {
        // Arrange
        const string profileName = "test-profile";
        var secureCredential = new SecureString();
        secureCredential.AppendChar('t');
        secureCredential.AppendChar('e');
        secureCredential.AppendChar('s');
        secureCredential.AppendChar('t');
        secureCredential.MakeReadOnly();

        // Act
        var result = await _resilientStore.SaveSecureCredentialAsync(profileName, secureCredential);

        // Assert
        result.Should().BeTrue();
        
        secureCredential.Dispose();
    }

    [Fact]
    public async Task GetSecureCredentialAsync_WithSuccessfulOperation_ShouldReturnCredentialScope()
    {
        // Arrange
        const string profileName = "test-profile";
        const string credential = "test-credential";
        await _testInnerStore.SaveCredentialAsync(profileName, credential);

        // Act
        var result = await _resilientStore.GetSecureCredentialAsync(profileName);

        // Assert
        result.Should().NotBeNull();
        result?.Value.Should().Be(credential);
        result?.Dispose();
    }

    [Fact]
    public void GetCircuitBreakerMetrics_ShouldReturnMetrics()
    {
        // Act
        var metrics = _resilientStore.GetCircuitBreakerMetrics();

        // Assert
        metrics.Should().NotBeNull();
        metrics.Should().BeOfType<Dictionary<string, CircuitBreakerMetrics>>();
    }

    [Fact]
    public async Task MultipleOperations_ShouldUseDifferentCircuitBreakers()
    {
        // Arrange
        const string profileName = "test-profile";
        const string credential = "test-credential";
        
        // Act
        await _resilientStore.SaveCredentialAsync(profileName, credential);
        await _resilientStore.GetCredentialAsync(profileName);

        // Get metrics to verify separate circuit breakers were used
        var metrics = _resilientStore.GetCircuitBreakerMetrics();

        // Assert
        var saveCircuitBreakerKey = $"SaveCredential_{Constants.ProfileManagerServiceName}";
        var getCircuitBreakerKey = $"GetCredential_{Constants.ProfileManagerServiceName}";
        
        metrics.Should().ContainKey(saveCircuitBreakerKey);
        metrics.Should().ContainKey(getCircuitBreakerKey);
        metrics[saveCircuitBreakerKey].SuccessCount.Should().Be(1);
        metrics[getCircuitBreakerKey].SuccessCount.Should().Be(1);
    }

    [Fact]
    public async Task ResetCircuitBreakers_ShouldResetAllCircuitBreakers()
    {
        // Arrange - First create some circuit breaker activity
        _testInnerStore.ShouldThrowException = new InvalidOperationException("Test failure");

        // Generate some failures to create circuit breaker state
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _resilientStore.SaveCredentialAsync("test", "test"));

        // Act
        _resilientStore.ResetCircuitBreakers();

        // Assert
        var metrics = _resilientStore.GetCircuitBreakerMetrics();
        foreach (var metric in metrics.Values)
        {
            metric.State.Should().Be(CircuitBreakerState.Closed);
            metric.FailureCount.Should().Be(0);
            metric.SuccessCount.Should().Be(0);
        }
    }

    [Fact]
    public void Constructor_WithNullInnerStore_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new ResilientCredentialStore(null!, _circuitBreakerManager, _logger));
    }

    [Fact]
    public void Constructor_WithNullCircuitBreakerManager_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new ResilientCredentialStore(_testInnerStore, null!, _logger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new ResilientCredentialStore(_testInnerStore, _circuitBreakerManager, null!));
    }
}