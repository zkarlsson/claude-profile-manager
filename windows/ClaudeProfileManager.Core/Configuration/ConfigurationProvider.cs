using System.Text.Json;
using ClaudeProfileManager.Core.Json;

namespace ClaudeProfileManager.Core.Configuration;

/// <summary>
/// Provides application configuration from multiple sources
/// </summary>
public static class ConfigurationProvider
{
    private static readonly Lazy<ApplicationConfiguration> _defaultConfiguration = new(CreateDefaultConfiguration);
    private static ApplicationConfiguration? _overriddenConfiguration;
    private static readonly JsonSerializerOptions _configJsonOptions = new() { WriteIndented = true };
    
    /// <summary>
    /// Gets the current application configuration
    /// </summary>
    public static ApplicationConfiguration Current => _overriddenConfiguration ?? _defaultConfiguration.Value;
    
    /// <summary>
    /// Sets a custom configuration (primarily for testing)
    /// </summary>
    /// <param name="configuration">The custom configuration</param>
    public static void SetConfiguration(ApplicationConfiguration configuration)
    {
        _overriddenConfiguration = configuration;
    }
    
    /// <summary>
    /// Resets to default configuration
    /// </summary>
    public static void ResetToDefault()
    {
        _overriddenConfiguration = null;
    }
    
    /// <summary>
    /// Loads configuration from a JSON file if it exists
    /// </summary>
    /// <param name="configFilePath">Path to the configuration file</param>
    /// <returns>True if configuration was loaded successfully</returns>
    public static async Task<bool> TryLoadFromFileAsync(string configFilePath)
    {
        if (!File.Exists(configFilePath))
            return false;
            
        try
        {
            var jsonBytes = await File.ReadAllBytesAsync(configFilePath);
            var config = JsonSerializer.Deserialize<ApplicationConfiguration>(jsonBytes);
                
            if (config != null)
            {
                _overriddenConfiguration = config;
                return true;
            }
        }
        catch
        {
            // Ignore configuration load errors - fall back to defaults
        }
        
        return false;
    }
    
    /// <summary>
    /// Saves current configuration to a JSON file
    /// </summary>
    /// <param name="configFilePath">Path to save the configuration file</param>
    /// <returns>True if configuration was saved successfully</returns>
    public static async Task<bool> TrySaveToFileAsync(string configFilePath)
    {
        try
        {
            var directory = Path.GetDirectoryName(configFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(Current, _configJsonOptions);
                
            await File.WriteAllBytesAsync(configFilePath, jsonBytes);
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Gets the default configuration file path
    /// </summary>
    /// <returns>Path to the default configuration file</returns>
    public static string GetDefaultConfigFilePath()
    {
        return Path.Combine(Constants.GetProfilesDirectory(), "config.json");
    }
    
    private static ApplicationConfiguration CreateDefaultConfiguration()
    {
        return new ApplicationConfiguration();
    }
}