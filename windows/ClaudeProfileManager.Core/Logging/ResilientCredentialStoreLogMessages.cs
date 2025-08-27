using Microsoft.Extensions.Logging;

namespace ClaudeProfileManager.Core.Logging;

/// <summary>
/// High-performance, source-generated logging messages for ResilientCredentialStore operations.
/// Uses LoggerMessage source generator for optimal performance and type safety.
/// </summary>
internal static partial class ResilientCredentialStoreLogMessages
{
    // Debug level messages for circuit breaker protected operations
    [LoggerMessage(
        EventId = 4001,
        Level = LogLevel.Debug,
        Message = "Executing SaveCredential operation for profile '{ProfileName}' through circuit breaker")]
    public static partial void ExecutingSaveCredential(this ILogger logger, string profileName);

    [LoggerMessage(
        EventId = 4002,
        Level = LogLevel.Debug,
        Message = "Executing GetCredential operation for profile '{ProfileName}' through circuit breaker")]
    public static partial void ExecutingGetCredential(this ILogger logger, string profileName);

    [LoggerMessage(
        EventId = 4003,
        Level = LogLevel.Debug,
        Message = "Executing DeleteCredential operation for profile '{ProfileName}' through circuit breaker")]
    public static partial void ExecutingDeleteCredential(this ILogger logger, string profileName);

    [LoggerMessage(
        EventId = 4004,
        Level = LogLevel.Debug,
        Message = "Executing ListProfiles operation through circuit breaker for service '{ServiceType}'")]
    public static partial void ExecutingListProfiles(this ILogger logger, string serviceType);

    [LoggerMessage(
        EventId = 4005,
        Level = LogLevel.Debug,
        Message = "Executing SaveSecureCredential operation for profile '{ProfileName}' through circuit breaker")]
    public static partial void ExecutingSaveSecureCredential(this ILogger logger, string profileName);

    [LoggerMessage(
        EventId = 4006,
        Level = LogLevel.Debug,
        Message = "Executing GetSecureCredential operation for profile '{ProfileName}' through circuit breaker")]
    public static partial void ExecutingGetSecureCredential(this ILogger logger, string profileName);
}