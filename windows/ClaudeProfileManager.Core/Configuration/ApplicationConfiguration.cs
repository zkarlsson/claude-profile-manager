namespace ClaudeProfileManager.Core.Configuration;

/// <summary>
/// Application-wide configuration settings
/// </summary>
public class ApplicationConfiguration
{
    /// <summary>
    /// Buffer sizes for various I/O operations
    /// </summary>
    public BufferConfiguration Buffers { get; set; } = new();
    
    /// <summary>
    /// Performance tuning settings
    /// </summary>
    public PerformanceConfiguration Performance { get; set; } = new();
    
    /// <summary>
    /// Security-related settings
    /// </summary>
    public SecurityConfiguration Security { get; set; } = new();
    
    /// <summary>
    /// Logging configuration
    /// </summary>
    public LoggingConfiguration Logging { get; set; } = new();
    
    /// <summary>
    /// Path configuration
    /// </summary>
    public PathConfiguration Paths { get; set; } = new();
}

/// <summary>
/// Buffer size configuration for I/O operations
/// </summary>
public class BufferConfiguration
{
    /// <summary>
    /// Buffer size for JSON file operations (default: 16KB)
    /// </summary>
    public int JsonFileBufferSize { get; set; } = 16 * 1024;
    
    /// <summary>
    /// Buffer size for text file operations (default: 4KB)
    /// </summary>
    public int TextFileBufferSize { get; set; } = 4 * 1024;
    
    /// <summary>
    /// Buffer size for JSON serialization operations (default: 16KB)
    /// </summary>
    public int JsonSerializationBufferSize { get; set; } = 16 * 1024;
}

/// <summary>
/// Performance tuning configuration
/// </summary>
public class PerformanceConfiguration
{
    /// <summary>
    /// Default capacity for StringBuilder in development error summaries (default: 512)
    /// </summary>
    public int ErrorSummaryStringBuilderCapacity { get; set; } = 512;
    
    /// <summary>
    /// Default capacity for StringBuilder in exception ToString (default: 256)
    /// </summary>
    public int ExceptionStringBuilderCapacity { get; set; } = 256;
    
    /// <summary>
    /// Default capacity for StringBuilder in detailed messages (default: 128)
    /// </summary>
    public int DetailedMessageStringBuilderCapacity { get; set; } = 128;
    
    /// <summary>
    /// Default JSON size estimate for development mode (default: 1024)
    /// </summary>
    public int DefaultJsonSizeDevelopment { get; set; } = 1024;
    
    /// <summary>
    /// Default JSON size estimate for production mode (default: 512)
    /// </summary>
    public int DefaultJsonSizeProduction { get; set; } = 512;
}

/// <summary>
/// Security configuration settings
/// </summary>
public class SecurityConfiguration
{
    /// <summary>
    /// Maximum length for input sanitization in logs (default: 100)
    /// </summary>
    public int MaxInputLengthForLogging { get; set; } = 100;
    
    /// <summary>
    /// Enable automatic credential cleanup on disposal (default: true)
    /// </summary>
    public bool AutoCleanupCredentials { get; set; } = true;
    
    /// <summary>
    /// Enable exception sanitization in logs (default: true)
    /// </summary>
    public bool EnableExceptionSanitization { get; set; } = true;
}

/// <summary>
/// Logging configuration settings
/// </summary>
public class LoggingConfiguration
{
    /// <summary>
    /// Enable debug logging based on environment variable (computed at runtime)
    /// </summary>
    public static bool IsDebugEnabled => Constants.IsDebugEnabled();
    
    /// <summary>
    /// Enable audit logging based on environment variable (computed at runtime)
    /// </summary>
    public static bool IsAuditLogEnabled => Constants.IsAuditLogEnabled();
    
    /// <summary>
    /// Enable structured logging with operation scopes (default: true)
    /// </summary>
    public bool EnableStructuredLogging { get; set; } = true;
    
    /// <summary>
    /// Maximum length of log messages before truncation (default: 2000)
    /// </summary>
    public int MaxLogMessageLength { get; set; } = 2000;
}

/// <summary>
/// Path configuration settings
/// </summary>
public class PathConfiguration
{
    /// <summary>
    /// Custom profiles directory path (if null, uses default from Constants)
    /// </summary>
    public string? CustomProfilesDirectory { get; set; }
    
    /// <summary>
    /// Gets the effective profiles directory path
    /// </summary>
    public string GetProfilesDirectory()
    {
        return CustomProfilesDirectory ?? Constants.GetProfilesDirectory();
    }
    
    /// <summary>
    /// Enable secure directory permissions (default: true)
    /// </summary>
    public bool EnableSecureDirectoryPermissions { get; set; } = true;
}