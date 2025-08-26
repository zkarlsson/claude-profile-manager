using System.Text.RegularExpressions;

namespace ClaudeProfileManager.Core.Models;

/// <summary>
/// Provides validation for profile names and aliases.
/// </summary>
public static class ProfileValidation
{
    /// <summary>
    /// Maximum allowed length for profile names.
    /// </summary>
    public const int MaxProfileNameLength = 50;

    /// <summary>
    /// Regex pattern for valid profile names (alphanumeric, dash, underscore only).
    /// </summary>
    private static readonly Regex ValidNamePattern = new(@"^[a-zA-Z0-9_-]+$", RegexOptions.Compiled);

    /// <summary>
    /// Reserved profile names that cannot be used.
    /// </summary>
    private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ".", "..", "current", "aliases", "audit", "tmp", "temp", "con", "prn", "aux", "nul",
        "com1", "com2", "com3", "com4", "com5", "com6", "com7", "com8", "com9",
        "lpt1", "lpt2", "lpt3", "lpt4", "lpt5", "lpt6", "lpt7", "lpt8", "lpt9"
    };

    /// <summary>
    /// Validates a profile name according to the rules.
    /// </summary>
    /// <param name="name">The profile name to validate</param>
    /// <returns>A validation result with success status and error message</returns>
    public static ValidationResult ValidateProfileName(string name)
    {
        // Check if name is null or empty
        if (string.IsNullOrWhiteSpace(name))
        {
            return ValidationResult.Failure("Profile name cannot be empty");
        }

        // Check length
        if (name.Length > MaxProfileNameLength)
        {
            return ValidationResult.Failure($"Profile name cannot exceed {MaxProfileNameLength} characters");
        }

        // Check for valid characters
        if (!ValidNamePattern.IsMatch(name))
        {
            return ValidationResult.Failure("Profile name can only contain letters, numbers, dashes, and underscores");
        }

        // Check for names starting with dots (hidden files)
        if (name.StartsWith('.'))
        {
            return ValidationResult.Failure("Profile name cannot start with a dot");
        }

        // Check for reserved names
        if (ReservedNames.Contains(name))
        {
            return ValidationResult.Failure($"'{name}' is a reserved profile name");
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates an alias name (follows same rules as profile names).
    /// </summary>
    /// <param name="aliasName">The alias name to validate</param>
    /// <returns>A validation result with success status and error message</returns>
    public static ValidationResult ValidateAliasName(string aliasName)
    {
        return ValidateProfileName(aliasName);
    }

    /// <summary>
    /// Validates that a credential appears to be the correct format for the specified auth method.
    /// </summary>
    /// <param name="credential">The credential to validate</param>
    /// <param name="authMethod">The expected authentication method</param>
    /// <returns>A validation result with success status and error message</returns>
    public static ValidationResult ValidateCredentialFormat(string credential, AuthMethod authMethod)
    {
        if (string.IsNullOrEmpty(credential))
        {
            return ValidationResult.Failure("Credential cannot be empty");
        }

        return authMethod switch
        {
            AuthMethod.Console => ValidateConsoleApiKey(credential),
            AuthMethod.Subscription => ValidateSubscriptionToken(credential),
            AuthMethod.None => ValidationResult.Failure("Cannot validate credentials for 'None' auth method"),
            AuthMethod.Unknown => ValidationResult.Failure("Cannot validate credentials for 'Unknown' auth method"),
            _ => ValidationResult.Failure($"Unsupported auth method: {authMethod}")
        };
    }

    /// <summary>
    /// Validates that a string appears to be a valid Console API key.
    /// </summary>
    private static ValidationResult ValidateConsoleApiKey(string apiKey)
    {
        // Console API keys should be 108 characters long and start with "sk-ant-api01-"
        if (apiKey.Length != 108)
        {
            return ValidationResult.Failure("Console API key should be 108 characters long");
        }

        if (!apiKey.StartsWith("sk-ant-api01-", StringComparison.Ordinal))
        {
            return ValidationResult.Failure("Console API key should start with 'sk-ant-api01-'");
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates that a string appears to be a valid subscription token.
    /// </summary>
    private static ValidationResult ValidateSubscriptionToken(string token)
    {
        // Subscription tokens should be JSON objects containing claudeAiOauth
        if (!token.TrimStart().StartsWith('{') || !token.TrimEnd().EndsWith('}'))
        {
            return ValidationResult.Failure("Subscription token should be a JSON object");
        }

        if (!token.Contains("claudeAiOauth"))
        {
            return ValidationResult.Failure("Subscription token should contain 'claudeAiOauth' field");
        }

        return ValidationResult.Success();
    }
}

/// <summary>
/// Represents the result of a validation operation.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Indicates whether the validation was successful.
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// The error message if validation failed, null if successful.
    /// </summary>
    public string? ErrorMessage { get; }

    private ValidationResult(bool isValid, string? errorMessage = null)
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static ValidationResult Success() => new(true);

    /// <summary>
    /// Creates a failed validation result with an error message.
    /// </summary>
    /// <param name="errorMessage">The error message describing why validation failed</param>
    public static ValidationResult Failure(string errorMessage) => new(false, errorMessage);

    /// <summary>
    /// Implicitly converts a ValidationResult to a boolean.
    /// </summary>
    public static implicit operator bool(ValidationResult result) => result.IsValid;
}