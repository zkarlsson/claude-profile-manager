using Xunit;

namespace ClaudeProfileManager.Tests.Windows;

/// <summary>
/// Collection definition to ensure Windows Credential Store tests run sequentially.
/// This prevents race conditions when multiple tests access the Windows Credential Manager simultaneously.
/// </summary>
[CollectionDefinition("WindowsCredentialStore")]
public class WindowsCredentialStoreTestsDefinition : ICollectionFixture<WindowsCredentialStoreFixture>
{
}

/// <summary>
/// Fixture class for Windows Credential Store tests.
/// </summary>
public class WindowsCredentialStoreFixture
{
    // This fixture ensures tests in the collection run sequentially
}