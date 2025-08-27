# Changelog

All notable changes to Claude Profile Manager for Windows will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-08-26

### Added

#### Core Features
- **Profile Management**: Save, list, switch, delete authentication profiles
- **Alias Support**: Create and manage aliases for profiles
- **Current Profile Tracking**: Track and display currently active profile
- **Health Monitoring**: System health checks and diagnostics
- **Status Reporting**: Comprehensive system status information

#### Windows Integration
- **Windows Credential Manager**: Secure credential storage using Windows APIs
- **ACL Security**: File system permissions with user-only access
- **OAuth Token Support**: Claude subscription token management and health monitoring
- **Console API Support**: Claude console API key management
- **Platform Targeting**: Windows-specific implementation with proper platform attributes

#### Security Features
- **Input Validation**: Comprehensive validation against injection attacks
- **Exception Sanitization**: Automatic credential removal from logs and errors
- **Secure Memory Handling**: SecureString support and automatic cleanup
- **Information Disclosure Prevention**: Sanitized logging and error handling
- **Enterprise Security**: ACL management and permission verification

#### Performance Optimizations
- **JSON Source Generation**: Optimized serialization using source generators
- **Memory Pooling**: ArrayPool usage for reduced allocations
- **Circuit Breaker**: Resilience patterns for fault tolerance
- **Optimized I/O**: Streaming operations and cached security descriptors
- **ReadyToRun**: AOT compilation for faster startup

#### Enterprise Features
- **MSI Installer Support**: Windows Installer framework integration
- **Chocolatey Package**: Package manager distribution
- **PowerShell Module**: PowerShell cmdlet wrappers for automation
- **Silent Installation**: Unattended installation support
- **Registry Integration**: System-wide configuration support

#### Developer Experience
- **Comprehensive Testing**: Full unit and integration test coverage
- **Documentation**: Extensive XML documentation and guides
- **Build Automation**: Automated build and packaging scripts
- **Single-File Distribution**: Self-contained executable deployment
- **Path Optimization**: Short paths for compatibility

### Technical Implementation

#### Architecture
- **Clean Architecture**: Separation of Core, Windows, CLI, and Test layers
- **Dependency Injection**: Proper DI container with interface abstractions
- **SOLID Principles**: Interface-based design with clear responsibilities
- **Error Handling**: Standardized exception handling and recovery

#### Dependencies
- **.NET 9.0**: Latest runtime with performance improvements
- **Microsoft.Extensions**: Logging, DI, and configuration
- **System.CommandLine**: Modern command-line parsing
- **Windows API**: P/Invoke integration for credential management

#### Build Configuration
- **Single-File Publishing**: Self-contained deployment
- **Platform Targeting**: Windows x64 optimization
- **Version Management**: Consistent versioning across assemblies
- **MSI Properties**: Product metadata for Windows Installer

### Security

#### Credential Protection
- All credentials encrypted using Windows Data Protection API (DPAPI)
- Per-user credential isolation with no cross-user access
- Automatic credential cleanup on profile deletion
- No plain text storage of sensitive information

#### File System Security
- Profile metadata protected with restrictive ACLs
- User-only access with SYSTEM account support
- Automatic permission verification and repair
- Secure temporary file handling

#### Input Security
- Profile name validation against Windows reserved names
- Path traversal prevention and sanitization
- Command injection protection
- Length restrictions to prevent buffer overflow

#### Information Security
- Automatic credential pattern detection and removal
- Secure logging with sanitization
- Exception message sanitization
- No sensitive data exposure in process lists

### Known Limitations

- **Windows Only**: Requires Windows 10 1903+ or Windows Server 2019+
- **x64 Architecture**: 64-bit Windows systems only
- **.NET Runtime**: Self-contained but still requires Windows x64 compatibility
- **Single User**: Per-user installation and profile management

### Installation and Deployment

#### System Requirements
- Windows 10 version 1903 (build 18362) or later
- x64 (64-bit) architecture
- 128 MB available RAM (256 MB recommended)
- 100 MB free disk space

#### Installation Methods
- MSI installer for standard Windows deployment
- Chocolatey package for package manager users
- Portable executable for minimal installations
- PowerShell module for automation scenarios

#### Post-Installation
- Automatic health check verification
- Profile directory creation with proper permissions
- Windows Credential Manager integration setup
- PATH environment variable configuration (optional)

### Breaking Changes

None - Initial release.

### Deprecated

None - Initial release.

### Removed

None - Initial release.

### Fixed

None - Initial release.

---

## Release Notes

### Version 1.0.0 - Stable Release

This is the first stable release of Claude Profile Manager for Windows. The application has been thoroughly tested and is ready for production use in both personal and enterprise environments.

**Key Highlights:**
- Enterprise-grade security with Windows Credential Manager integration
- Performance-optimized architecture with modern .NET 9.0 features  
- Comprehensive Windows ecosystem support (MSI, Chocolatey, PowerShell)
- Full compatibility with Claude Code CLI authentication methods
- Self-contained deployment requiring no additional runtime installation

**Upgrade Path:**
This is the initial release, so no upgrade procedures are necessary.

**Support:**
- GitHub Issues: Report bugs and feature requests
- Documentation: Comprehensive guides and API documentation
- Security: Responsible disclosure process for vulnerabilities

---

**For detailed technical documentation, see:**
- [Installation Guide](INSTALL.md)
- [Security Policy](SECURITY.md)  
- [README](README.md)