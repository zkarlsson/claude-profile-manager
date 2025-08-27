using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace ClaudeProfileManager.Core.Security;

/// <summary>
/// Provides secure handling of credential data with automatic memory cleanup
/// </summary>
public static class SecureCredentialHandler
{
    /// <summary>
    /// Converts a string to SecureString
    /// Note: The original string cannot be reliably cleared from memory in .NET
    /// </summary>
    public static SecureString ToSecureString(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return new SecureString();

        var secureString = new SecureString();
        foreach (char c in plainText)
        {
            secureString.AppendChar(c);
        }
        secureString.MakeReadOnly();
        
        return secureString;
    }

    /// <summary>
    /// Converts SecureString back to string with automatic cleanup using 'using' pattern
    /// </summary>
    public static SecureCredentialScope GetCredential(SecureString secureString)
    {
        return new SecureCredentialScope(secureString);
    }

}

/// <summary>
/// Provides secure access to credential data with automatic cleanup
/// </summary>
public sealed class SecureCredentialScope : IDisposable
{
    private IntPtr _ptr = IntPtr.Zero;
    private readonly string? _credential;
    private bool _disposed;

    internal SecureCredentialScope(SecureString secureString)
    {
        if (secureString == null || secureString.Length == 0)
        {
            _credential = string.Empty;
            return;
        }

        try
        {
            _ptr = Marshal.SecureStringToBSTR(secureString);
            _credential = Marshal.PtrToStringBSTR(_ptr);
        }
        catch
        {
            if (_ptr != IntPtr.Zero)
            {
                Marshal.ZeroFreeBSTR(_ptr);
                _ptr = IntPtr.Zero;
            }
            throw;
        }
    }

    /// <summary>
    /// Gets the credential value. Only valid within the scope of this object.
    /// </summary>
    public string Value => _credential ?? string.Empty;

    /// <summary>
    /// Checks if the credential is null or empty
    /// </summary>
    public bool IsEmpty => string.IsNullOrEmpty(_credential);

    public void Dispose()
    {
        if (!_disposed && _ptr != IntPtr.Zero)
        {
            Marshal.ZeroFreeBSTR(_ptr);
            _ptr = IntPtr.Zero;
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}