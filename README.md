# Claude Profile Manager

A robust authentication profile management system for Claude Code CLI that allows seamless switching between different Claude authentication methods.

> **✅ Production Ready for Windows** | Originally created for macOS by [@derekspelledcorrectly](https://github.com/derekspelledcorrectly/claude-profile-manager)

## 🎯 Overview

Claude Profile Manager simplifies working with multiple Claude authentication profiles by providing secure storage and seamless switching between:
- **Console API Keys** (static API keys)
- **Subscription OAuth Tokens** (browser-authenticated tokens)

Originally designed for macOS, this Windows implementation provides the same powerful functionality using Windows-native technologies for optimal security and performance.

## 🚀 Platform Support

| Platform | Status | Implementation | Secure Storage |
|----------|---------|---------------|----------------|
| **macOS** | ✅ Production Ready | Bash + jq | macOS Keychain |
| **Windows** | ✅ Production Ready | .NET 9 + C# | Windows Credential Manager |

## ⭐ Key Features

### 🔒 **Secure Profile Management**
- **Save profiles** with current Claude authentication
- **List profiles** with health status and metadata
- **Switch profiles** instantly between different authentications
- **Delete profiles** with complete cleanup
- **Alias support** for convenient profile naming

### 🛡️ **Security First**
- **Windows**: Credentials encrypted using Windows DPAPI via Credential Manager
- **macOS**: Credentials stored in encrypted macOS Keychain
- **No plain text storage** of sensitive authentication data
- **Per-user isolation** - profiles never shared between users
- **Automatic credential sanitization** in logs and errors

### 🔧 **Advanced Features**
- **Authentication detection** - automatically identifies credential types
- **Token health monitoring** - real-time expiration tracking for subscription tokens
- **Auto-save functionality** - prevents credential loss during profile switches
- **Comprehensive validation** - input sanitization and security hardening
- **Multiple installation methods** - choose what works best for your environment

## 📦 Installation

### Windows Installation

#### Method 1: Chocolatey (Recommended)
```powershell
choco install claude-profile-manager
```

#### Method 2: PowerShell Module
```powershell
Install-Module ClaudeProfileManager
```

#### Method 3: Portable
Download `claude-profile-manager.exe` and run directly - no installation required.

### macOS Installation

#### Method 1: Homebrew (Recommended)
```bash
brew tap derekspelledcorrectly/claude-tools
brew install claude-profile-manager
```

#### Method 2: Manual Installation
```bash
git clone https://github.com/derekspelledcorrectly/claude-profile-manager.git
cp claude-profile-manager/bin/claude-profile /usr/local/bin/
```

## 🎮 Usage

### Basic Commands

The commands are identical across both platforms:

```bash
# Save your current authentication as a profile
claude-profile-manager save work

# Save without specifying name (uses current profile)
claude-profile-manager save

# List all profiles with status
claude-profile-manager list

# Switch to a different profile  
claude-profile-manager switch personal

# Show currently active profile
claude-profile-manager current

# Delete a profile
claude-profile-manager delete old-profile
```

### Advanced Usage

```bash
# Save profile with aliases
claude-profile-manager save work --aliases w,office

# List profiles with detailed information
claude-profile-manager list --detailed

# Create and use aliases
claude-profile-manager alias api console
claude-profile-manager switch api

# System health and status
claude-profile-manager health
claude-profile-manager status
```

### PowerShell Integration (Windows)

```powershell
# PowerShell cmdlets available
Save-ClaudeProfile -Name "work"
Get-ClaudeProfile | Where-Object { $_.AuthMethod -eq "Subscription" }
Switch-ClaudeProfile -Name "personal"
Get-CurrentClaudeProfile
```

## 🏗️ Architecture

### Windows Implementation
- **.NET 9** with modern C# performance optimizations
- **Windows Credential Manager** for secure, encrypted credential storage
- **Windows ACL** file permissions for profile metadata security
- **Circuit breaker patterns** for resilient operation
- **Source-generated JSON** for optimal serialization performance
- **Comprehensive input validation** against injection attacks

### macOS Implementation  
- **Bash** with jq for robust JSON processing
- **macOS Keychain Services** for secure credential storage
- **File system permissions** with user-only access
- **Comprehensive error handling** and validation
- **OAuth token analysis** with expiration detection

## 🔐 Security

### Authentication Methods Supported

#### Console API Keys
- Static API keys from Claude Console
- Stored securely in platform credential store
- Manual rotation workflow

#### Subscription OAuth Tokens  
- Browser-authenticated OAuth tokens
- Real-time health monitoring and expiration detection
- Auto-save functionality to prevent credential loss
- Token refresh handling (where supported)

### Security Features

- **✅ Encrypted Storage**: All credentials encrypted using platform-native APIs
- **✅ Process Isolation**: Credentials never visible in process lists or logs
- **✅ Input Validation**: Comprehensive protection against injection attacks
- **✅ Exception Sanitization**: Automatic credential removal from error messages
- **✅ File Permissions**: Restrictive ACLs/permissions on all profile files
- **✅ Memory Protection**: Secure memory handling with automatic cleanup

## 📊 System Requirements

### Windows
- **OS**: Windows 10 version 1903 or later, Windows 11, Windows Server 2019+
- **Architecture**: x64 (64-bit)
- **Memory**: 128 MB available (256 MB recommended)
- **Storage**: 100 MB free space
- **Dependencies**: None (self-contained executable)

### macOS  
- **OS**: macOS 10.15 (Catalina) or later
- **Dependencies**: jq (auto-installed via Homebrew)
- **Storage**: 50 MB free space

### Both Platforms
- **Claude Code CLI**: Must be installed and configured
- **Permissions**: Standard user account (no admin required for basic usage)

## 🎯 Use Cases

### Individual Developers
- Switch between personal and work Claude accounts
- Manage multiple API keys for different projects
- Quick profile switching for different development contexts

### Enterprise Teams
- Shared deployment via package managers
- Consistent authentication management across teams
- Audit trail and security compliance

### Advanced Users
- Automation via PowerShell cmdlets (Windows) or shell scripts (macOS)
- Integration with development workflows
- Custom profile management solutions

## ⚠️ Important Notes

### Security Considerations
- **This tool directly modifies your Claude authentication data**
- **Always test with non-production credentials first**
- **Keep backups of important authentication setups**
- **Credentials are stored in your user's secure credential store only**

### Claude Code CLI Integration
- **Restart Claude Code CLI** after switching profiles for changes to take effect
- **Both platforms integrate** with the same Claude Code CLI installation
- **Profile isolation** ensures clean separation between different authentications

### Enterprise Deployment
- **Group Policy support** for Windows MSI deployment
- **Silent installation options** for automated deployment
- **No system-wide modifications** required for basic functionality

## 🛠️ Development

### Building from Source

#### Windows
```powershell
# Prerequisites: .NET 9 SDK
git clone https://github.com/zkarlsson/claude-profile-manager-windows.git
cd claude-profile-manager-windows/windows
dotnet build --configuration Release
```

#### macOS
```bash
# Prerequisites: bash, jq
git clone https://github.com/derekspelledcorrectly/claude-profile-manager.git
cd claude-profile-manager
# No build step required - pure bash implementation
```

### Testing
```powershell
# Windows: Comprehensive test suite
dotnet test

# macOS: Built-in validation
claude-profile-manager health
```

## 📚 Documentation

### Complete Documentation Available
- **[Installation Guide](INSTALL.md)** - Detailed setup instructions
- **[Security Policy](SECURITY.md)** - Security architecture and practices
- **[System Requirements](SYSTEM-REQUIREMENTS.md)** - Compatibility information
- **[Release Notes](RELEASE-NOTES.md)** - Version history and changes

### Getting Help
```bash
# Command-line help
claude-profile-manager --help

# System diagnostics  
claude-profile-manager health

# Current status
claude-profile-manager status
```

## 🤝 Contributing

### For Windows Implementation
1. Fork this repository
2. Create feature branch from `windows-implementation`
3. Work in `windows/` directory
4. Add tests for new functionality
5. Follow .NET conventions and existing patterns
6. Submit PR with comprehensive testing

### For macOS Implementation
See the original repository: [@derekspelledcorrectly/claude-profile-manager](https://github.com/derekspelledcorrectly/claude-profile-manager)

## 📈 Project Status

### Windows Implementation
- ✅ **v1.0.0 Production Ready** (August 2025)
- ✅ **Complete feature parity** with macOS version
- ✅ **Enterprise-grade security** and Windows integration
- ✅ **Multiple distribution channels** (installer, Chocolatey, PowerShell, portable)
- ✅ **Comprehensive testing** with real-world validation

### macOS Implementation
- ✅ **Production Ready** with active maintenance
- ✅ **Full-featured bash implementation** 
- ✅ **Homebrew distribution** available

## 🆘 Support

### Community Support
- **GitHub Issues**: Report bugs and request features
- **GitHub Discussions**: Community help and best practices
- **Documentation**: Comprehensive guides included

### Enterprise Support
- Professional deployment assistance available
- Custom integration consulting
- Training and best practices workshops

## 🙏 Acknowledgments

### Original macOS Implementation
This Windows version builds upon the excellent foundation created by **[@derekspelledcorrectly](https://github.com/derekspelledcorrectly)** in the original macOS implementation. The core concepts, user experience design, and security principles established in the Mac version provided the blueprint for this Windows implementation.

### Windows Implementation
The Windows version was developed to provide the same powerful functionality while leveraging Windows-native technologies for optimal security, performance, and integration with the Windows ecosystem.

## 📄 License

MIT License - see [LICENSE](LICENSE) file for details.

Both the original macOS implementation and this Windows implementation are released under the same permissive MIT license, enabling broad usage in both personal and commercial environments.

---

**🚀 Ready to get started?** 

Choose your platform and follow the installation instructions above. Whether you're on Windows or macOS, Claude Profile Manager provides the same powerful, secure profile management experience tailored to your operating system's strengths.