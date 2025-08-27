using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using ClaudeProfileManager.Windows.Security;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ClaudeProfileManager.Tests.Security;

[SupportedOSPlatform("windows")]
public class WindowsAclManagerTests : IDisposable
{
    private readonly WindowsAclManager _aclManager;
    private readonly ILogger<WindowsAclManager> _logger;
    private readonly List<string> _testFilesToCleanup = new();
    private readonly List<string> _testDirectoriesToCleanup = new();

    public WindowsAclManagerTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => { });
        _logger = loggerFactory.CreateLogger<WindowsAclManager>();
        _aclManager = new WindowsAclManager(_logger);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new WindowsAclManager(null!));
    }

    [Fact]
    public void SecureFile_WithNullFilePath_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _aclManager.SecureFile(null!));
        Assert.Throws<ArgumentException>(() => _aclManager.SecureFile(""));
        Assert.Throws<ArgumentException>(() => _aclManager.SecureFile("   "));
    }

    [Fact]
    public void SecureFile_WithNonExistentFile_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act
        var result = _aclManager.SecureFile(nonExistentFile);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void SecureFile_WithValidFile_ShouldReturnTrueAndSecureFile()
    {
        // Arrange
        var testFile = CreateTestFile();

        // Act
        var result = _aclManager.SecureFile(testFile);

        // Assert
        result.Should().BeTrue();
        _aclManager.IsFileSecure(testFile).Should().BeTrue();
    }

    [Fact]
    public void IsFileSecure_WithSecuredFile_ShouldReturnTrue()
    {
        // Arrange
        var testFile = CreateTestFile();
        _aclManager.SecureFile(testFile);

        // Act
        var result = _aclManager.IsFileSecure(testFile);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsFileSecure_WithUnsecuredFile_ShouldReturnFalse()
    {
        // Arrange
        var testFile = CreateTestFile();
        // Don't secure it, leave default permissions

        // Act
        var result = _aclManager.IsFileSecure(testFile);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void SecureDirectory_WithNullDirectoryPath_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _aclManager.SecureDirectory(null!));
        Assert.Throws<ArgumentException>(() => _aclManager.SecureDirectory(""));
        Assert.Throws<ArgumentException>(() => _aclManager.SecureDirectory("   "));
    }

    [Fact]
    public void SecureDirectory_WithNonExistentDirectory_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act
        var result = _aclManager.SecureDirectory(nonExistentDir);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void SecureDirectory_WithValidDirectory_ShouldReturnTrueAndSecureDirectory()
    {
        // Arrange
        var testDir = CreateTestDirectory();

        // Act
        var result = _aclManager.SecureDirectory(testDir);

        // Assert
        result.Should().BeTrue();
        
        // Verify the directory is secured by checking ACL
        var dirInfo = new DirectoryInfo(testDir);
        var dirSecurity = dirInfo.GetAccessControl();
        dirSecurity.AreAccessRulesProtected.Should().BeTrue("Directory should have inheritance disabled");
    }

    [Fact]
    public void CreateSecureDirectory_WithNewDirectory_ShouldCreateAndSecure()
    {
        // Arrange
        var newDirPath = Path.Combine(Path.GetTempPath(), "test-secure-dir-" + Guid.NewGuid().ToString());
        _testDirectoriesToCleanup.Add(newDirPath);

        // Act
        var result = _aclManager.CreateSecureDirectory(newDirPath);

        // Assert
        result.Should().BeTrue();
        Directory.Exists(newDirPath).Should().BeTrue();
        
        // Verify the directory is secured
        var dirInfo = new DirectoryInfo(newDirPath);
        var dirSecurity = dirInfo.GetAccessControl();
        dirSecurity.AreAccessRulesProtected.Should().BeTrue("Directory should have inheritance disabled");
    }

    [Fact]
    public void CreateSecureDirectory_WithExistingDirectory_ShouldSecureExisting()
    {
        // Arrange
        var existingDir = CreateTestDirectory();

        // Act
        var result = _aclManager.CreateSecureDirectory(existingDir);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void GetEffectivePermissions_WithSecuredFile_ShouldReturnFullControl()
    {
        // Arrange
        var testFile = CreateTestFile();
        _aclManager.SecureFile(testFile);

        // Act
        var permissions = _aclManager.GetEffectivePermissions(testFile);

        // Assert
        permissions.Should().NotBeNull();
        permissions.Should().HaveFlag(FileSystemRights.FullControl);
    }

    [Fact]
    public void GetEffectivePermissions_WithNonExistentFile_ShouldReturnNull()
    {
        // Arrange
        var nonExistentFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act
        var permissions = _aclManager.GetEffectivePermissions(nonExistentFile);

        // Assert
        permissions.Should().BeNull();
    }

    [Fact]
    public void RemoveUnauthorizedAccess_WithSecuredFile_ShouldReturnTrue()
    {
        // Arrange
        var testFile = CreateTestFile();
        _aclManager.SecureFile(testFile);

        // Act
        var result = _aclManager.RemoveUnauthorizedAccess(testFile);

        // Assert
        result.Should().BeTrue();
        _aclManager.IsFileSecure(testFile).Should().BeTrue();
    }

    [Fact]
    public void SecureFile_WithFileInSubdirectory_ShouldHandleComplexPaths()
    {
        // Arrange
        var testDir = CreateTestDirectory();
        var subDir = Path.Combine(testDir, "subdir");
        Directory.CreateDirectory(subDir);
        
        var testFile = Path.Combine(subDir, "test-file.txt");
        File.WriteAllText(testFile, "test content");
        _testFilesToCleanup.Add(testFile);

        // Act
        var result = _aclManager.SecureFile(testFile);

        // Assert
        result.Should().BeTrue();
        _aclManager.IsFileSecure(testFile).Should().BeTrue();
    }

    [Fact]
    public void SecureDirectory_WithApplyToSubItems_ShouldSecureAllContents()
    {
        // Arrange
        var testDir = CreateTestDirectory();
        var subDir = Path.Combine(testDir, "subdir");
        Directory.CreateDirectory(subDir);
        
        var testFile = Path.Combine(testDir, "test-file.txt");
        File.WriteAllText(testFile, "test content");
        _testFilesToCleanup.Add(testFile);

        var subFile = Path.Combine(subDir, "sub-file.txt");
        File.WriteAllText(subFile, "sub content");
        _testFilesToCleanup.Add(subFile);

        // Act
        var result = _aclManager.SecureDirectory(testDir, applyToSubItems: true);

        // Assert
        result.Should().BeTrue();
        
        // Main file should be secured
        _aclManager.IsFileSecure(testFile).Should().BeTrue("Main file should be secured");
        
        // Sub-file should be secured
        _aclManager.IsFileSecure(subFile).Should().BeTrue("Sub-file should be secured");
    }

    [Fact]
    public void SecureFile_ShouldOnlyAllowCurrentUserAndSystem()
    {
        // Arrange
        var testFile = CreateTestFile();

        // Act
        var result = _aclManager.SecureFile(testFile);

        // Assert
        result.Should().BeTrue();

        // Verify ACL contains only expected SIDs
        var fileInfo = new FileInfo(testFile);
        var fileSecurity = fileInfo.GetAccessControl();
        var rules = fileSecurity.GetAccessRules(true, false, typeof(SecurityIdentifier));

        var currentUserSid = WindowsIdentity.GetCurrent().User;
        var systemSid = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);

        foreach (FileSystemAccessRule rule in rules)
        {
            var sid = rule.IdentityReference as SecurityIdentifier;
            if (sid != null)
            {
                // Rule should be for current user or SYSTEM only  
                if (currentUserSid != null)
                {
                    (sid.Equals(currentUserSid) || sid.Equals(systemSid)).Should().BeTrue(
                        $"Unexpected SID in ACL: {sid.Value}");
                }
            }
        }
    }

    private string CreateTestFile()
    {
        var testFile = Path.Combine(Path.GetTempPath(), "test-acl-file-" + Guid.NewGuid().ToString() + ".txt");
        File.WriteAllText(testFile, "Test content for ACL testing");
        _testFilesToCleanup.Add(testFile);
        return testFile;
    }

    private string CreateTestDirectory()
    {
        var testDir = Path.Combine(Path.GetTempPath(), "test-acl-dir-" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDir);
        _testDirectoriesToCleanup.Add(testDir);
        return testDir;
    }

    public void Dispose()
    {
        // Clean up test files
        foreach (var file in _testFilesToCleanup)
        {
            try
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
            catch
            {
                // Best effort cleanup
            }
        }

        // Clean up test directories
        foreach (var dir in _testDirectoriesToCleanup)
        {
            try
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, recursive: true);
                }
            }
            catch
            {
                // Best effort cleanup
            }
        }
        
        GC.SuppressFinalize(this);
    }
}