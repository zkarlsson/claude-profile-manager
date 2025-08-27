using System.Collections.Concurrent;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using ClaudeProfileManager.Core.Configuration;
using ClaudeProfileManager.Core.Json;

namespace ClaudeProfileManager.Core.IO;

/// <summary>
/// Optimized file operations with caching, buffering, and reduced ACL overhead
/// </summary>
[SupportedOSPlatform("windows")]
public static class OptimizedFileOperations
{
    private static readonly ConcurrentDictionary<string, bool> DirectoryExistsCache = new();
    private static readonly ConcurrentDictionary<string, FileSecurity> SecureFileSecurityCache = new();
    private static readonly object SecurityCacheLock = new();

    /// <summary>
    /// Gets a cached secure file security descriptor for the current user
    /// </summary>
    public static FileSecurity GetSecureFileSecurity()
    {
        var currentUserSid = WindowsIdentity.GetCurrent().User?.Value ?? "";
        
        return SecureFileSecurityCache.GetOrAdd(currentUserSid, _ =>
        {
            lock (SecurityCacheLock)
            {
                if (SecureFileSecurityCache.TryGetValue(currentUserSid, out var cached))
                    return cached;

                var security = new FileSecurity();
                var currentUser = WindowsIdentity.GetCurrent();
                
                // Remove inheritance
                security.SetAccessRuleProtection(true, false);
                
                // Add read/write for current user only
                security.AddAccessRule(new FileSystemAccessRule(
                    currentUser.User!,
                    FileSystemRights.ReadAndExecute | FileSystemRights.Write,
                    AccessControlType.Allow));
                
                return security;
            }
        });
    }

    /// <summary>
    /// Gets a cached secure directory security descriptor for the current user
    /// </summary>
    public static DirectorySecurity GetSecureDirectorySecurity()
    {
        var security = new DirectorySecurity();
        var currentUser = WindowsIdentity.GetCurrent();
        
        // Remove inheritance
        security.SetAccessRuleProtection(true, false);
        
        // Add full control for current user only
        security.AddAccessRule(new FileSystemAccessRule(
            currentUser.User!,
            FileSystemRights.FullControl,
            InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
            PropagationFlags.None,
            AccessControlType.Allow));
        
        return security;
    }

    /// <summary>
    /// Efficiently checks if directory exists with caching
    /// </summary>
    public static bool DirectoryExistsCached(string path)
    {
        return DirectoryExistsCache.GetOrAdd(path, Directory.Exists);
    }

    /// <summary>
    /// Creates directory with secure permissions and caching
    /// </summary>
    public static DirectoryInfo CreateSecureDirectory(string path)
    {
        // Check actual existence first, don't use cache for this check
        if (Directory.Exists(path))
        {
            // Update cache with correct value and return
            DirectoryExistsCache.TryAdd(path, true);
            return new DirectoryInfo(path);
        }

        var directoryInfo = Directory.CreateDirectory(path);
        directoryInfo.SetAccessControl(GetSecureDirectorySecurity());
        
        // Update cache with correct value
        DirectoryExistsCache.AddOrUpdate(path, true, (_, _) => true);
        
        return directoryInfo;
    }

    /// <summary>
    /// Atomic write operation with secure permissions and optimized JSON serialization
    /// </summary>
    public static async Task<bool> WriteJsonAtomicAsync<T>(string filePath, T data, JsonSerializerOptions options, CancellationToken cancellationToken = default)
    {
        var tempPath = filePath + ".tmp";
        
        try
        {
            // Use provided JSON options for compatibility
            using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: ConfigurationProvider.Current.Buffers.JsonFileBufferSize))
            {
                await JsonSerializer.SerializeAsync<T>(fileStream, data, options, cancellationToken);
                await fileStream.FlushAsync(cancellationToken);
            } // Ensure stream is disposed before move
            
            // Set secure permissions on temp file
            var fileInfo = new FileInfo(tempPath);
            fileInfo.SetAccessControl(GetSecureFileSecurity());
            
            // Atomic move
            File.Move(tempPath, filePath, overwrite: true);
            
            return true;
        }
        catch
        {
            // Clean up temp file on failure
            try
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
            catch
            {
                // Ignore cleanup failures
            }
            throw;
        }
    }

    /// <summary>
    /// Optimized JSON deserialization with streaming and source generation
    /// </summary>
    public static async Task<T?> ReadJsonAsync<T>(string filePath, JsonSerializerOptions options, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
            return default;

        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: ConfigurationProvider.Current.Buffers.JsonFileBufferSize);
        return await JsonSerializer.DeserializeAsync<T>(fileStream, options, cancellationToken);
    }

    /// <summary>
    /// Atomic write operation for text with secure permissions
    /// </summary>
    public static async Task WriteTextAtomicAsync(string filePath, string content, CancellationToken cancellationToken = default)
    {
        var tempPath = filePath + ".tmp";
        
        try
        {
            // Use streaming write for better memory efficiency
            using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: ConfigurationProvider.Current.Buffers.TextFileBufferSize))
            using (var writer = new StreamWriter(fileStream, Encoding.UTF8))
            {
                await writer.WriteAsync(content.AsMemory(), cancellationToken);
            }
            
            // Set secure permissions
            var fileInfo = new FileInfo(tempPath);
            fileInfo.SetAccessControl(GetSecureFileSecurity());
            
            // Atomic move
            File.Move(tempPath, filePath, overwrite: true);
        }
        catch
        {
            // Clean up temp file on failure
            try
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
            catch
            {
                // Ignore cleanup failures
            }
            throw;
        }
    }

    /// <summary>
    /// Optimized text reading with proper encoding
    /// </summary>
    public static async Task<string?> ReadTextAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
            return null;

        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: ConfigurationProvider.Current.Buffers.TextFileBufferSize);
        using var reader = new StreamReader(fileStream, Encoding.UTF8);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    /// <summary>
    /// Optimized directory enumeration without loading all files into memory
    /// </summary>
    public static IEnumerable<string> EnumerateJsonFiles(string directoryPath, string searchPattern = "*.json")
    {
        if (!Directory.Exists(directoryPath))
            yield break;

        foreach (var filePath in Directory.EnumerateFiles(directoryPath, searchPattern, SearchOption.TopDirectoryOnly))
        {
            var fileName = Path.GetFileName(filePath);
            
            // Skip hidden files (starting with .)
            if (!fileName.StartsWith('.'))
            {
                var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                if (!string.IsNullOrEmpty(nameWithoutExtension))
                {
                    yield return nameWithoutExtension;
                }
            }
        }
    }

    /// <summary>
    /// Clears the directory existence cache (useful for testing)
    /// </summary>
    public static void ClearDirectoryCache()
    {
        DirectoryExistsCache.Clear();
    }

    /// <summary>
    /// Clears the security cache (useful for testing or user context changes)
    /// </summary>
    public static void ClearSecurityCache()
    {
        lock (SecurityCacheLock)
        {
            SecureFileSecurityCache.Clear();
        }
    }
}