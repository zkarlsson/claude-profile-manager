using System.Buffers;
using System.Text;
using System.Text.Json;
using ClaudeProfileManager.Core.Configuration;
using ClaudeProfileManager.Core.Models;

namespace ClaudeProfileManager.Core.Json;

/// <summary>
/// High-performance JSON operations using source generation and memory optimization
/// </summary>
public static class OptimizedJsonOperations
{
    // Reusable buffer pool to reduce allocations
    private static readonly ArrayPool<byte> BytePool = ArrayPool<byte>.Shared;
    
    /// <summary>
    /// Efficiently serializes an object to JSON using source generation
    /// </summary>
    public static async Task<Memory<byte>> SerializeToUtf8BytesAsync<T>(T value, bool isDevelopment = false, CancellationToken cancellationToken = default)
    {
        var options = JsonOptionsProvider.ForEnvironment(isDevelopment);
        
        // Use memory stream for efficient byte handling
        using var stream = new MemoryStream();
        await JsonSerializer.SerializeAsync<T>(stream, value, options, cancellationToken);
        
        return stream.ToArray().AsMemory();
    }
    
    /// <summary>
    /// Efficiently deserializes JSON from UTF-8 bytes using source generation
    /// </summary>
    public static T? DeserializeFromUtf8Bytes<T>(ReadOnlyMemory<byte> utf8Json, bool isDevelopment = false)
    {
        var options = JsonOptionsProvider.ForEnvironment(isDevelopment);
        
        // Deserialize directly from ReadOnlyMemory to avoid stream allocation
        return JsonSerializer.Deserialize<T>(utf8Json.Span, options);
    }
    
    /// <summary>
    /// Efficiently serializes to stream with source generation and buffer pooling
    /// </summary>
    public static async Task SerializeToStreamAsync<T>(Stream stream, T value, bool isDevelopment = false, CancellationToken cancellationToken = default)
    {
        var options = JsonOptionsProvider.ForEnvironment(isDevelopment);
        
        // Use Utf8JsonWriter for maximum performance
        var bufferSize = ConfigurationProvider.Current.Buffers.JsonSerializationBufferSize;
        var buffer = BytePool.Rent(bufferSize);
        
        try
        {
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions
            {
                Indented = isDevelopment,
                Encoder = options.Encoder
            });
            
            JsonSerializer.Serialize<T>(writer, value, options);
            await writer.FlushAsync(cancellationToken);
        }
        finally
        {
            BytePool.Return(buffer);
        }
    }
    
    /// <summary>
    /// Efficiently deserializes from stream with source generation
    /// </summary>
    public static async Task<T?> DeserializeFromStreamAsync<T>(Stream stream, bool isDevelopment = false, CancellationToken cancellationToken = default)
    {
        var options = JsonOptionsProvider.ForEnvironment(isDevelopment);
        return await JsonSerializer.DeserializeAsync<T>(stream, options, cancellationToken);
    }
    
    /// <summary>
    /// Optimized Profile serialization with type-specific optimizations
    /// </summary>
    public static async Task<Memory<byte>> SerializeProfileAsync(Profile profile, bool isDevelopment = false, CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream();
        
        if (isDevelopment)
        {
            await JsonSerializer.SerializeAsync(stream, profile, ProfileJsonContextPretty.Default.Profile, cancellationToken);
        }
        else
        {
            await JsonSerializer.SerializeAsync(stream, profile, ProfileJsonContext.Default.Profile, cancellationToken);
        }
        
        return stream.ToArray().AsMemory();
    }
    
    /// <summary>
    /// Optimized Profile deserialization with type-specific optimizations  
    /// </summary>
    public static Profile? DeserializeProfile(ReadOnlyMemory<byte> utf8Json, bool isDevelopment = false)
    {
        // Deserialize directly from ReadOnlyMemory to avoid stream allocation
        if (isDevelopment)
        {
            return JsonSerializer.Deserialize(utf8Json.Span, ProfileJsonContextPretty.Default.Profile);
        }
        else
        {
            return JsonSerializer.Deserialize(utf8Json.Span, ProfileJsonContext.Default.Profile);
        }
    }
    
    /// <summary>
    /// Optimized Dictionary serialization for aliases
    /// </summary>
    public static async Task<Memory<byte>> SerializeAliasesAsync(Dictionary<string, string> aliases, bool isDevelopment = false, CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream();
        
        if (isDevelopment)
        {
            await JsonSerializer.SerializeAsync(stream, aliases, ProfileJsonContextPretty.Default.DictionaryStringString, cancellationToken);
        }
        else
        {
            await JsonSerializer.SerializeAsync(stream, aliases, ProfileJsonContext.Default.DictionaryStringString, cancellationToken);
        }
        
        return stream.ToArray().AsMemory();
    }
    
    /// <summary>
    /// Optimized Dictionary deserialization for aliases
    /// </summary>
    public static Dictionary<string, string>? DeserializeAliases(ReadOnlyMemory<byte> utf8Json, bool isDevelopment = false)
    {
        // Deserialize directly from ReadOnlyMemory to avoid stream allocation
        if (isDevelopment)
        {
            return JsonSerializer.Deserialize(utf8Json.Span, ProfileJsonContextPretty.Default.DictionaryStringString);
        }
        else
        {
            return JsonSerializer.Deserialize(utf8Json.Span, ProfileJsonContext.Default.DictionaryStringString);
        }
    }
    
    /// <summary>
    /// Validates JSON content without full deserialization (for performance)
    /// </summary>
    public static bool IsValidJson(ReadOnlySpan<byte> utf8Json)
    {
        try
        {
            var reader = new Utf8JsonReader(utf8Json);
            while (reader.Read()) { } // Parse without allocating
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
    
    /// <summary>
    /// Gets estimated JSON size for pre-allocation optimization
    /// </summary>
    public static int EstimateJsonSize<T>(T value, bool isDevelopment = false)
    {
        // Quick estimation based on type and development mode
        return value switch
        {
            Profile p => EstimateProfileSize(p, isDevelopment),
            Dictionary<string, string> dict => EstimateAliasesSize(dict, isDevelopment),
            _ => isDevelopment ? ConfigurationProvider.Current.Performance.DefaultJsonSizeDevelopment : ConfigurationProvider.Current.Performance.DefaultJsonSizeProduction
        };
    }
    
    private static int EstimateProfileSize(Profile profile, bool isDevelopment)
    {
        // Base JSON structure size
        var baseSize = 200;
        
        // Add size for strings
        baseSize += (profile.Name?.Length ?? 0) * 2;
        baseSize += profile.Aliases.Sum(a => a.Length * 2);
        
        // Add formatting overhead if development mode
        if (isDevelopment)
            baseSize += 100;
            
        return baseSize;
    }
    
    private static int EstimateAliasesSize(Dictionary<string, string> aliases, bool isDevelopment)
    {
        var baseSize = 50; // JSON object overhead
        
        foreach (var kvp in aliases)
        {
            baseSize += (kvp.Key.Length + kvp.Value.Length) * 2 + 10; // Keys, values, quotes, commas
        }
        
        if (isDevelopment)
            baseSize += aliases.Count * 4; // Indentation overhead
            
        return baseSize;
    }
}