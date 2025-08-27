using System.Text.RegularExpressions;

namespace ClaudeProfileManager.Core.Validation;

/// <summary>
/// Provides comprehensive input validation and sanitization for security
/// </summary>
public static partial class InputValidator
{
    // Profile name validation: alphanumeric, hyphens, underscores, dots (1-64 chars)
    [GeneratedRegex(@"^[a-zA-Z0-9._-]{1,64}$", RegexOptions.Compiled)]
    private static partial Regex ProfileNamePattern();

    // Alias name validation: alphanumeric, hyphens, underscores (1-32 chars)
    [GeneratedRegex(@"^[a-zA-Z0-9_-]{1,32}$", RegexOptions.Compiled)]
    private static partial Regex AliasNamePattern();

    // Path traversal detection
    [GeneratedRegex(@"(\.\.[\\/]|[\\/]\.\.[\\/]|[\\/]\.\.$|^\.\.[\\/])", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex PathTraversalPattern();

    // Command injection patterns
    [GeneratedRegex(@"[;&|`$(){}[\]<>\\]", RegexOptions.Compiled)]
    private static partial Regex CommandInjectionPattern();

    // SQL injection patterns
    [GeneratedRegex(@"('(''|[^'])*')|(;)|(--|#)|(\/\*|\*\/)|(\b(ALTER|CREATE|DELETE|DROP|EXEC|INSERT|MERGE|SELECT|UPDATE|UNION)\b)", 
        RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex SqlInjectionPattern();

    /// <summary>
    /// Validates a profile name for security and format requirements
    /// </summary>
    public static ValidationResult ValidateProfileName(string? profileName)
    {
        if (string.IsNullOrWhiteSpace(profileName))
        {
            return ValidationResult.Invalid("Profile name cannot be null or empty");
        }

        profileName = profileName.Trim();

        if (profileName.Length > 64)
        {
            return ValidationResult.Invalid("Profile name cannot exceed 64 characters");
        }

        if (profileName.Length == 0)
        {
            return ValidationResult.Invalid("Profile name cannot be empty after trimming");
        }

        if (!ProfileNamePattern().IsMatch(profileName))
        {
            return ValidationResult.Invalid("Profile name contains invalid characters. Only letters, numbers, dots, hyphens, and underscores are allowed");
        }

        // Check for reserved names
        if (IsReservedName(profileName))
        {
            return ValidationResult.Invalid($"'{profileName}' is a reserved name and cannot be used");
        }

        // Check for path traversal
        if (PathTraversalPattern().IsMatch(profileName))
        {
            return ValidationResult.Invalid("Profile name contains path traversal patterns");
        }

        return ValidationResult.Valid(profileName);
    }

    /// <summary>
    /// Validates an alias name for security and format requirements
    /// </summary>
    public static ValidationResult ValidateAliasName(string? aliasName)
    {
        if (string.IsNullOrWhiteSpace(aliasName))
        {
            return ValidationResult.Invalid("Alias name cannot be null or empty");
        }

        aliasName = aliasName.Trim();

        if (aliasName.Length > 32)
        {
            return ValidationResult.Invalid("Alias name cannot exceed 32 characters");
        }

        if (aliasName.Length == 0)
        {
            return ValidationResult.Invalid("Alias name cannot be empty after trimming");
        }

        if (!AliasNamePattern().IsMatch(aliasName))
        {
            return ValidationResult.Invalid("Alias name contains invalid characters. Only letters, numbers, hyphens, and underscores are allowed");
        }

        // Check for reserved names
        if (IsReservedName(aliasName))
        {
            return ValidationResult.Invalid($"'{aliasName}' is a reserved name and cannot be used");
        }

        // Check for command names (aliases shouldn't conflict with commands)
        if (IsCommandName(aliasName))
        {
            return ValidationResult.Invalid($"'{aliasName}' conflicts with a command name and cannot be used as an alias");
        }

        return ValidationResult.Valid(aliasName);
    }

    /// <summary>
    /// Validates and sanitizes user input for potential security threats
    /// </summary>
    public static ValidationResult ValidateUserInput(string? input, string fieldName = "Input")
    {
        if (string.IsNullOrEmpty(input))
        {
            return ValidationResult.Valid(string.Empty);
        }

        // Check for path traversal
        if (PathTraversalPattern().IsMatch(input))
        {
            return ValidationResult.Invalid($"{fieldName} contains path traversal patterns");
        }

        // Check for command injection
        if (CommandInjectionPattern().IsMatch(input))
        {
            return ValidationResult.Invalid($"{fieldName} contains potentially dangerous characters");
        }

        // Check for SQL injection
        if (SqlInjectionPattern().IsMatch(input))
        {
            return ValidationResult.Invalid($"{fieldName} contains SQL injection patterns");
        }

        // Check for excessive length
        if (input.Length > 1024)
        {
            return ValidationResult.Invalid($"{fieldName} exceeds maximum length of 1024 characters");
        }

        return ValidationResult.Valid(input.Trim());
    }

    /// <summary>
    /// Sanitizes a string for safe logging (removes potentially sensitive information)
    /// </summary>
    public static string SanitizeForLogging(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return "[empty]";
        }

        // Mask potential credentials, tokens, or keys
        if (input.Contains("sk-ant-", StringComparison.OrdinalIgnoreCase) ||
            input.Contains("Bearer", StringComparison.OrdinalIgnoreCase) ||
            input.Contains("token", StringComparison.OrdinalIgnoreCase) ||
            input.Contains("password", StringComparison.OrdinalIgnoreCase) ||
            input.Contains("key", StringComparison.OrdinalIgnoreCase))
        {
            return "[REDACTED]";
        }

        // For long strings, truncate and indicate truncation
        if (input.Length > 100)
        {
            return $"{input[..97]}...";
        }

        return input;
    }

    private static bool IsReservedName(string name)
    {
        var reservedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CON", "PRN", "AUX", "NUL", // Windows reserved names
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
            "current", "aliases", "config", "settings", // Application reserved names
            "admin", "root", "system", "default"
        };

        return reservedNames.Contains(name);
    }

    private static bool IsCommandName(string name)
    {
        var commandNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "save", "s", "list", "ls", "switch", "sw", "current", "cur",
            "delete", "del", "rm", "alias", "aliases", "unalias",
            "help", "version"
        };

        return commandNames.Contains(name);
    }
}

/// <summary>
/// Represents the result of input validation
/// </summary>
public sealed class ValidationResult
{
    public bool IsValid { get; private init; }
    public string? ErrorMessage { get; private init; }
    public string Value { get; private init; } = string.Empty;

    private ValidationResult() { }

    public static ValidationResult Valid(string value) => new()
    {
        IsValid = true,
        Value = value ?? string.Empty
    };

    public static ValidationResult Invalid(string errorMessage) => new()
    {
        IsValid = false,
        ErrorMessage = errorMessage
    };

    public void ThrowIfInvalid()
    {
        if (!IsValid)
        {
            throw new ArgumentException(ErrorMessage);
        }
    }
}