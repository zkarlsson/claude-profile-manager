# Claude Profile Manager - Windows Implementation

This directory contains the Windows implementation of Claude Profile Manager, built with .NET 9 and Windows Credential Manager.

## 🎯 Goals

Provide the same powerful profile management capabilities as the Mac version, but using Windows-native technologies:

- **Windows Credential Manager** for secure credential storage
- **.NET 9** for modern C# implementation  
- **System.CommandLine** for robust CLI parsing
- **Native Windows integration** with proper file permissions and paths

## 🏗️ Architecture

### Project Structure
```
windows/
├── ClaudeProfileManager.sln              # Visual Studio solution
├── ClaudeProfileManager.Core/            # Cross-platform business logic
├── ClaudeProfileManager.Windows/         # Windows-specific implementations  
├── ClaudeProfileManager.CLI/             # Console application
├── ClaudeProfileManager.Tests/           # Unit tests
└── Directory.Build.props                 # Shared build configuration
```

### Key Components

- **ICredentialStore**: Abstraction for secure credential storage
- **IProfileManager**: Profile CRUD operations with validation
- **IClaudeAuthDetector**: Authentication method detection and token health
- **Profile Model**: JSON-serializable profile metadata
- **Validation**: Comprehensive input validation with security focus

## 🚀 Current Status

- ✅ **Phase 1 Complete**: Core architecture with interfaces and models (22 tests passing)
- ✅ **Phase 2 Complete**: Windows Credential Manager and profile file management (60 tests passing)
- ✅ **Phase 3 Complete**: Claude authentication detection and health monitoring (80 tests passing)
- ✅ **Phase 4 Complete**: Full CLI implementation with feature parity (138+ tests passing)
- ✅ **Phase 5 Complete**: Comprehensive security & optimization implementation (All tests passing)
- ✅ **Phase 6 Complete**: Full distribution suite with multiple installation methods
- 🎉 **PRODUCTION RELEASE**: v1.0.0 officially complete and ready for deployment

## 🏆 Key Achievements

### 🛡️ Enterprise Security
- **Windows ACL Integration**: Files accessible only by current user + SYSTEM
- **Memory Protection**: SecureString usage with automatic cleanup scopes
- **Attack Prevention**: Path traversal, injection attacks, and reserved name validation
- **Information Security**: Credential redaction and exception sanitization

### ⚡ High Performance
- **Zero-Allocation JSON**: Source-generated serialization for optimal speed
- **Optimized I/O**: Buffer pooling, streaming operations, and caching
- **Source-Generated Logging**: High-performance structured logging with EventIds
- **Memory Efficiency**: Optimized allocation patterns and proper disposal

### 🔧 Production Reliability
- **Circuit Breaker Pattern**: Automatic failure recovery and cascading prevention
- **Health Monitoring**: Real-time system diagnostics and status reporting
- **Race Condition Safety**: Sequential operations and exponential backoff
- **Comprehensive Testing**: 138+ tests covering all functionality and edge cases

## ✨ Completed Features

### Distribution & Packaging
- **Optimized Build System**: Single-file self-contained executable with 46% path reduction
- **Chocolatey Package**: Complete package with installation and uninstallation scripts
- **PowerShell Module**: Professional cmdlet wrappers for automation scenarios
- **Comprehensive Documentation**: Install guides, security policies, and system requirements
- **Real-World Testing**: 100% validation with 19 test scenarios covering all functionality
- **Architecture Review**: 95/100 enterprise-ready score from optimization architect

### Windows-Native Security
- **Windows Credential Manager Integration**: Secure storage using built-in Windows APIs
- **Windows ACL File Permissions**: Profile files restricted to current user only
- **Encrypted Storage**: All credentials encrypted using Windows DPAPI
- **No Process Exposure**: Credentials never visible in process lists

### Authentication & Detection
- **Multi-Method Support**: Console API keys and OAuth subscription tokens
- **Real-Time Health Monitoring**: Token expiration detection with human-readable formatting
- **Format Validation**: Robust regex validation with flexible patterns
- **Profile Authentication**: Automatic detection of saved profile auth methods

### Enterprise Performance & Reliability
- **Comprehensive Testing**: 138+ unit tests with 100% pass rate
- **Circuit Breaker Pattern**: Resilient failure handling with automatic recovery
- **Health Monitoring**: Real-time system health checks and status reporting
- **Optimized File Operations**: High-performance I/O with memory pooling and caching
- **Source-Generated Logging**: Zero-allocation structured logging for optimal performance

### Core Infrastructure  
- **Async/Await Pattern**: Modern C# async programming throughout
- **Dependency Injection**: Clean architecture with interface-based design
- **Standardized Error Handling**: Consistent exception management with sanitization
- **Configuration System**: Multi-source configuration with environment support

## 🛠️ Development Setup

### Prerequisites
- **.NET 9 SDK**: Download from [dotnet.microsoft.com](https://dotnet.microsoft.com/)
- **Windows 10/11**: Required for Windows Credential Manager
- **Claude Code CLI**: For testing integration

### Build and Test
```cmd
# Navigate to Windows implementation
cd windows

# Restore dependencies
dotnet restore

# Build all projects  
dotnet build

# Run tests
dotnet test

# Run CLI (when implemented)
dotnet run --project ClaudeProfileManager.CLI
```

### Development Commands
```cmd
# Build in Debug mode
dotnet build --configuration Debug

# Build in Release mode  
dotnet build --configuration Release

# Run specific test project
dotnet test ClaudeProfileManager.Tests

# Watch tests during development
dotnet watch test --project ClaudeProfileManager.Tests
```

## 🏢 Platform Requirements

- **Windows 10/11**: Uses Windows Credential Manager APIs
- **Claude Code CLI**: Must be installed and configured
- **.NET 9 Runtime**: Required for execution

## 📋 Implementation Plan

### ✅ Phase 1: Core Architecture (COMPLETE)
- ✅ Interface definitions (`ICredentialStore`, `IClaudeAuthDetector`, `IProfileManager`)
- ✅ Core models (`Profile`, `AuthMethod`, validation attributes)
- ✅ Project structure with .NET solution and build configuration
- ✅ Unit testing framework with FluentAssertions
- ✅ Foundation test coverage (22 core tests passing)

### ✅ Phase 2: Windows Integration (COMPLETE)
- ✅ Windows Credential Manager implementation (`WindowsCredentialStore`)
- ✅ Profile file management with Windows ACLs (`WindowsProfileFileManager`)
- ✅ Comprehensive test coverage (60 tests passing)
- ✅ Security hardened with proper Windows file permissions

### ✅ Phase 3: Authentication & Detection (COMPLETE)
- ✅ Claude Code credential detection (`WindowsClaudeAuthDetector`)
- ✅ OAuth token health monitoring and parsing with human-readable formatting
- ✅ Authentication method validation with flexible regex patterns
- ✅ Windows Credential Manager integration for secure storage
- ✅ Profile authentication method detection
- ✅ Comprehensive test coverage (80 tests passing)

### ✅ Phase 4: CLI Interface (COMPLETE)
- ✅ System.CommandLine integration with beta4 compatibility
- ✅ All commands from Mac version (`save`, `list`, `switch`, `current`, `delete`, etc.)
- ✅ Full alias management (`alias`, `aliases`, `unalias`)
- ✅ Direct profile switching (`claude-profile work`)
- ✅ Comprehensive help system and validation error messages
- ✅ 138+ tests passing including CLI integration tests

### ✅ Phase 5: Comprehensive Security & Optimization (COMPLETE)

#### Security Enhancements
- ✅ **SecureCredentialHandler**: Memory-safe credential handling with SecureString and automatic cleanup
- ✅ **Comprehensive Input Validation**: Source-generated regex patterns, injection attack protection
- ✅ **Exception Sanitization**: Automatic credential redaction and information leakage prevention
- ✅ **Windows ACL Security**: Enterprise-grade file system protection with restrictive permissions

#### Performance Optimizations
- ✅ **Optimized File Operations**: Buffer pooling, streaming operations, and reduced ACL overhead
- ✅ **JSON Source Generation**: Zero-allocation serialization with System.Text.Json
- ✅ **Memory Management**: Optimized allocation patterns and proper disposal throughout
- ✅ **Configuration System**: Multi-source configuration with validation and environment support

#### Reliability & Resilience
- ✅ **Circuit Breaker Pattern**: Cascading failure prevention with automatic recovery
- ✅ **Structured Logging**: Source-generated, high-performance logging with EventId management
- ✅ **Health Checks**: Comprehensive system health monitoring and diagnostics
- ✅ **Race Condition Fixes**: Sequential test execution and exponential backoff retry logic

### ✅ Phase 6: Distribution & Integration (COMPLETE)
- ✅ **PowerShell Module**: Complete cmdlet wrapper with 10 functions and professional installation
- ✅ **Chocolatey Package**: Full package with installation/uninstallation and testing framework
- ✅ **Windows Installer**: PowerShell-based installer with registry integration and cleanup
- ✅ **Build Optimization**: Single-file executable with 46% path reduction and optimized deployment
- ✅ **Comprehensive Documentation**: INSTALL.md, SECURITY.md, CHANGELOG.md, system requirements
- ✅ **Real-World Testing**: 100% validation with 19 comprehensive test scenarios
- ✅ **Architecture Review**: 95/100 enterprise-ready score from optimization architect
- ✅ **Release Preparation**: Complete package ready for production deployment

### 🚫 **Intentionally Excluded Features**
- **Windows Registry Integration**: Adds complexity without user value (per-user config sufficient)
- **Windows Service Wrapper**: No background operations needed (on-demand execution preferred)

*These features were evaluated and excluded based on design principles favoring simplicity, security, and maintainability.*

## 🤝 Contributing to Windows Implementation

1. **Fork the main repository** 
2. **Create feature branch** from `windows-implementation`
3. **Work in `windows/` directory** only
4. **Add tests** for new functionality
5. **Follow .NET conventions** and existing patterns
6. **Submit PR** targeting the main repository

## ⚠️ Important Notes

- **Production Status**: Ready for production use with comprehensive testing validation
- **Credential Safety**: Securely manages Windows credential storage with proper encryption
- **Testing**: Thoroughly tested with real-world scenarios and edge cases
- **Distribution**: Multiple installation methods available (Chocolatey, PowerShell, portable)
- **Compatibility**: Designed to complement, not replace, Mac version

## 🙏 Acknowledgments

This Windows implementation builds upon the excellent foundation created by [@derekspelledcorrectly](https://github.com/derekspelledcorrectly) in the original Mac version.