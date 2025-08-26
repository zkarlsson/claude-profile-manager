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
- 🚧 **Phase 2 Next**: Windows Credential Manager integration
- 🎯 **Phase 3 Planned**: Full CLI implementation with feature parity

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

### Phase 2: Windows Integration
- [ ] Windows Credential Manager implementation
- [ ] Profile file management with Windows ACLs
- [ ] Claude Code credential detection
- [ ] Token health monitoring

### Phase 3: CLI Interface  
- [ ] System.CommandLine integration
- [ ] All commands from Mac version (`save`, `list`, `switch`, etc.)
- [ ] Alias management
- [ ] Help system and error handling

### Phase 4: Polish
- [ ] PowerShell module wrapper
- [ ] Chocolatey packaging  
- [ ] Windows installer
- [ ] Integration tests with real Claude Code

## 🤝 Contributing to Windows Implementation

1. **Fork the main repository** 
2. **Create feature branch** from `windows-implementation`
3. **Work in `windows/` directory** only
4. **Add tests** for new functionality
5. **Follow .NET conventions** and existing patterns
6. **Submit PR** targeting the main repository

## ⚠️ Important Notes

- **Development Status**: Not ready for production use
- **Credential Safety**: Will modify Windows credential storage
- **Testing**: Use test profiles, not production credentials
- **Compatibility**: Designed to complement, not replace, Mac version

## 🙏 Acknowledgments

This Windows implementation builds upon the excellent foundation created by [@derekspelledcorrectly](https://github.com/derekspelledcorrectly) in the original Mac version.