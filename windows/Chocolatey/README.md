# Claude Profile Manager - Chocolatey Package

This directory contains the Chocolatey package for Claude Profile Manager, providing easy installation and management on Windows systems.

## Package Overview

The `claude-profile-manager` Chocolatey package provides a complete Windows installation of Claude Profile Manager, including:

- **CLI Application**: Command-line interface for profile management
- **PowerShell Module**: Native PowerShell integration with comprehensive cmdlets
- **System Integration**: PATH configuration, Start Menu entries, and shortcuts
- **Enterprise Support**: Silent installation and uninstallation capabilities

## Installation

### Standard Installation

```powershell
choco install claude-profile-manager
```

### With Options

```powershell
# Install with desktop shortcut
choco install claude-profile-manager --params "CreateDesktopShortcut"

# Silent installation (enterprise)
choco install claude-profile-manager -y

# Install specific version
choco install claude-profile-manager --version 1.0.0
```

## Package Parameters

The package supports the following parameters:

- `CreateDesktopShortcut` - Creates a desktop shortcut during installation

Example:
```powershell
choco install claude-profile-manager --params "CreateDesktopShortcut"
```

## Uninstallation

### Standard Uninstallation

```powershell
choco uninstall claude-profile-manager
```

### With Options

```powershell
# Remove user data during uninstallation
choco uninstall claude-profile-manager --params "RemoveUserData"

# Force uninstallation
choco uninstall claude-profile-manager -f
```

## What Gets Installed

### Application Files
- Installed to: `%ProgramFiles%\ClaudeProfileManager\`
- Main executable: `ClaudeProfileManager.Windows.exe`
- Added to system PATH for global access

### PowerShell Module
- Installed to: `%ProgramFiles%\[Windows]PowerShell\Modules\ClaudeProfileManager\`
- Available system-wide for all users
- Compatible with both Windows PowerShell 5.1 and PowerShell Core 6+

### System Integration
- Start Menu entries:
  - `Claude Profile Manager` (GUI application)
  - `Claude Profile Manager (PowerShell)` (PowerShell console with module loaded)
- Optional desktop shortcut
- Environment PATH configuration

### User Data Location
User profiles and settings are stored in:
```
%USERPROFILE%\.claude\profiles\
```

This data is preserved during upgrades and uninstallation by default.

## Usage After Installation

### Command Line Interface
```cmd
# Save current credentials as a profile
claude-profile-manager save work

# List all profiles
claude-profile-manager list

# Switch to a profile
claude-profile-manager switch work

# Get current profile
claude-profile-manager current

# Get help
claude-profile-manager --help
```

### PowerShell Module
```powershell
# Import the module (auto-imported in new sessions)
Import-Module ClaudeProfileManager

# Use PowerShell cmdlets
Save-ClaudeProfile -Name "work" -Aliases "w", "office"
Get-ClaudeProfile
Switch-ClaudeProfile -Name "work"
Test-ClaudeProfileHealth

# Get comprehensive help
Get-Help about_ClaudeProfileManager
Get-Help Save-ClaudeProfile -Full
```

## Package Structure

```
claude-profile-manager/
├── claude-profile-manager.nuspec          # Package metadata and dependencies
├── tools/
│   ├── chocolateyinstall.ps1             # Installation script
│   ├── chocolateyuninstall.ps1           # Uninstallation script
│   ├── app/                               # .NET application files
│   │   ├── ClaudeProfileManager.Windows.exe
│   │   ├── *.dll                          # Dependencies
│   │   └── ...
│   └── powershell/                        # PowerShell module
│       └── ClaudeProfileManager/
│           ├── ClaudeProfileManager.psd1  # Module manifest
│           ├── ClaudeProfileManager.psm1  # Main module file
│           ├── Public/                    # Public cmdlets
│           ├── Private/                   # Internal functions
│           └── ...
```

## Building the Package

### Prerequisites
- Chocolatey CLI (`choco`)
- .NET 9 SDK
- PowerShell 5.1 or later

### Build Commands
```powershell
# Build with default settings
.\Build-ChocolateyPackage.ps1

# Build specific version
.\Build-ChocolateyPackage.ps1 -Version "1.0.1"

# Build and test
.\Build-ChocolateyPackage.ps1 -Configuration Release

# Build and publish (requires API key)
.\Build-ChocolateyPackage.ps1 -Publish -ApiKey "your-chocolatey-api-key"
```

### Build Process
1. **Validate Prerequisites**: Checks for required tools and dependencies
2. **Build .NET Application**: Compiles and publishes the CLI application
3. **Prepare Package Structure**: Copies application and PowerShell module files
4. **Update Metadata**: Sets version and build information
5. **Create Package**: Generates the `.nupkg` file using Chocolatey CLI
6. **Test Package**: Validates package structure and contents
7. **Optional Publishing**: Uploads to Chocolatey Community Repository

## Dependencies

The package automatically handles the following dependencies:

- **.NET 9 Runtime**: Required for the CLI application
- **PowerShell Core**: Recommended for best PowerShell experience (optional)

These are declared as Chocolatey package dependencies and will be installed automatically if not present.

## Enterprise Deployment

### Group Policy Installation
The package supports silent installation suitable for Group Policy deployment:

```cmd
choco install claude-profile-manager -y --no-progress
```

### Configuration Management
For enterprise environments, consider:

- **Silent Installation**: Use `-y` flag for automated deployment
- **Central Configuration**: Deploy configuration files to user profiles
- **Registry Settings**: Use Windows Registry for system-wide configuration
- **Security Policies**: Configure Windows ACLs for enhanced security

## Troubleshooting

### Common Issues

**Package Installation Fails**
- Ensure you have Administrator privileges
- Check that .NET 9 Runtime is available or will be installed
- Verify Chocolatey is up to date: `choco upgrade chocolatey`

**PowerShell Module Not Loading**
- Restart PowerShell session after installation
- Manually import: `Import-Module ClaudeProfileManager -Force`
- Check module path: `$env:PSModulePath`

**CLI Command Not Found**
- Restart command prompt/terminal after installation
- Check PATH environment variable includes: `%ProgramFiles%\ClaudeProfileManager`
- Try full path: `"%ProgramFiles%\ClaudeProfileManager\ClaudeProfileManager.Windows.exe"`

**Permission Issues**
- Ensure you have appropriate permissions for credential storage
- Run as different user if needed: `runas /user:domain\username cmd`

### Getting Help

1. **Package Issues**: Check Chocolatey community package page
2. **Application Issues**: Use built-in help: `claude-profile-manager --help`
3. **PowerShell Issues**: `Get-Help about_ClaudeProfileManager`
4. **General Support**: GitHub issues and discussions

## Version History

- **1.0.0**: Initial release with full CLI and PowerShell integration
- **Future**: Check package page for latest versions and release notes

## License

This package and Claude Profile Manager are distributed under the MIT License. See LICENSE file for details.