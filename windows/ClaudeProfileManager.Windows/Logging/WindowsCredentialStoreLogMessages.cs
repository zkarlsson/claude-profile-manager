using Microsoft.Extensions.Logging;

namespace ClaudeProfileManager.Windows.Logging;

/// <summary>
/// High-performance, source-generated logging messages for WindowsCredentialStore.
/// Uses LoggerMessage source generator for optimal performance and type safety.
/// </summary>
internal static partial class WindowsCredentialStoreLogMessages
{
    // Error level messages
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Error,
        Message = "Profile name cannot be null or empty")]
    public static partial void ProfileNameNullOrEmpty(this ILogger logger);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Error,
        Message = "Credential cannot be null or empty")]
    public static partial void CredentialNullOrEmpty(this ILogger logger);

    // Debug level messages for save operations
    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Debug,
        Message = "Saving credential for profile '{ProfileName}' with service '{ServiceType}'")]
    public static partial void SavingCredential(this ILogger logger, string profileName, string serviceType);

    [LoggerMessage(
        EventId = 2002,
        Level = LogLevel.Debug,
        Message = "Successfully saved credential for profile '{ProfileName}'")]
    public static partial void CredentialSaved(this ILogger logger, string profileName);

    // Debug level messages for get operations
    [LoggerMessage(
        EventId = 2003,
        Level = LogLevel.Debug,
        Message = "Retrieving credential for profile '{ProfileName}' with service '{ServiceType}'")]
    public static partial void RetrievingCredential(this ILogger logger, string profileName, string serviceType);

    [LoggerMessage(
        EventId = 2004,
        Level = LogLevel.Debug,
        Message = "Successfully retrieved credential for profile '{ProfileName}'")]
    public static partial void CredentialRetrieved(this ILogger logger, string profileName);

    [LoggerMessage(
        EventId = 2005,
        Level = LogLevel.Debug,
        Message = "Credential not found for profile '{ProfileName}'")]
    public static partial void CredentialNotFound(this ILogger logger, string profileName);

    // Debug level messages for delete operations
    [LoggerMessage(
        EventId = 2006,
        Level = LogLevel.Debug,
        Message = "Deleting credential for profile '{ProfileName}' with service '{ServiceType}'")]
    public static partial void DeletingCredential(this ILogger logger, string profileName, string serviceType);

    [LoggerMessage(
        EventId = 2007,
        Level = LogLevel.Debug,
        Message = "Credential not found for profile '{ProfileName}' (already deleted)")]
    public static partial void CredentialAlreadyDeleted(this ILogger logger, string profileName);

    [LoggerMessage(
        EventId = 2008,
        Level = LogLevel.Debug,
        Message = "Successfully deleted credential for profile '{ProfileName}'")]
    public static partial void CredentialDeleted(this ILogger logger, string profileName);

    // Debug level messages for list operations
    [LoggerMessage(
        EventId = 2009,
        Level = LogLevel.Debug,
        Message = "Listing profiles for service '{ServiceType}'")]
    public static partial void ListingProfiles(this ILogger logger, string serviceType);

    [LoggerMessage(
        EventId = 2010,
        Level = LogLevel.Debug,
        Message = "No credentials found for service '{ServiceType}'")]
    public static partial void NoCredentialsFound(this ILogger logger, string serviceType);

    [LoggerMessage(
        EventId = 2011,
        Level = LogLevel.Debug,
        Message = "Enumeration attempt {Retry} failed with error {Error}, retrying...")]
    public static partial void EnumerationRetrying(this ILogger logger, int retry, int error);

    [LoggerMessage(
        EventId = 2012,
        Level = LogLevel.Debug,
        Message = "Found {Count} profiles for service '{ServiceType}'")]
    public static partial void ProfilesFound(this ILogger logger, int count, string serviceType);
}