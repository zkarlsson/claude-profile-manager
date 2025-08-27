using System.Text.Json;
using System.Text.Json.Serialization;
using ClaudeProfileManager.Core.Models;

namespace ClaudeProfileManager.Core.Json;

/// <summary>
/// Source-generated JSON serialization context for optimal performance
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = false, // Optimized for production - compact JSON
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    GenerationMode = JsonSourceGenerationMode.Default,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(Profile))]
[JsonSerializable(typeof(Dictionary<string, string>))] // For aliases
[JsonSerializable(typeof(List<Profile>))] // For collections
[JsonSerializable(typeof(AuthMethod))] // For enums
public partial class ProfileJsonContext : JsonSerializerContext
{
}

/// <summary>
/// Development context with pretty-printing enabled
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = true, // Pretty-printed for development
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    GenerationMode = JsonSourceGenerationMode.Default,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(Profile))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(List<Profile>))]
[JsonSerializable(typeof(AuthMethod))]
public partial class ProfileJsonContextPretty : JsonSerializerContext
{
}

/// <summary>
/// Optimized JSON serializer options provider with caching
/// </summary>
public static class JsonOptionsProvider
{
    private static readonly Lazy<JsonSerializerOptions> ProductionOptionsLazy = new(CreateProductionOptions);
    private static readonly Lazy<JsonSerializerOptions> DevelopmentOptionsLazy = new(CreateDevelopmentOptions);
    
    /// <summary>
    /// Gets optimized production JSON options (compact, source-generated)
    /// </summary>
    public static JsonSerializerOptions Production => ProductionOptionsLazy.Value;
    
    /// <summary>
    /// Gets development JSON options (pretty-printed, source-generated)
    /// </summary>
    public static JsonSerializerOptions Development => DevelopmentOptionsLazy.Value;
    
    /// <summary>
    /// Gets JSON options based on environment (development vs production)
    /// </summary>
    public static JsonSerializerOptions ForEnvironment(bool isDevelopment) 
        => isDevelopment ? Development : Production;
    
    private static JsonSerializerOptions CreateProductionOptions()
    {
        return new JsonSerializerOptions(ProfileJsonContext.Default.Options)
        {
            // Additional optimizations for production
            AllowTrailingCommas = false,
            PropertyNameCaseInsensitive = false,
            ReadCommentHandling = JsonCommentHandling.Disallow,
            UnknownTypeHandling = JsonUnknownTypeHandling.JsonElement,
            // Source generation context provides the rest
            TypeInfoResolver = ProfileJsonContext.Default
        };
    }
    
    private static JsonSerializerOptions CreateDevelopmentOptions()
    {
        return new JsonSerializerOptions(ProfileJsonContextPretty.Default.Options)
        {
            // More permissive for development/debugging
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            UnknownTypeHandling = JsonUnknownTypeHandling.JsonElement,
            // Pretty-printing context provides the rest
            TypeInfoResolver = ProfileJsonContextPretty.Default
        };
    }
}