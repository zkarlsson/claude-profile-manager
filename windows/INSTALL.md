# Claude Profile Manager for Windows - Installation Guide

## System Requirements

### Minimum Requirements
- **Operating System**: Windows 10 version 1903 (build 18362) or later
- **Architecture**: x64 (64-bit)
- **Memory**: 128 MB available RAM
- **Disk Space**: 100 MB free disk space
- **Permissions**: Standard user account (no administrator privileges required)

### Recommended Requirements
- **Operating System**: Windows 11 or Windows Server 2019/2022
- **Architecture**: x64 (64-bit)
- **Memory**: 256 MB available RAM
- **Disk Space**: 200 MB free disk space
- **Claude Code CLI**: Latest version installed and configured

## Installation Methods

### Method 1: Chocolatey Package Manager (Recommended)

```powershell
# Install Chocolatey (if not already installed)
Set-ExecutionPolicy Bypass -Scope Process -Force
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072
iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))

# Install Claude Profile Manager
choco install claude-profile-manager
```

### Method 2: Portable Installation

1. Download the portable executable `claude-profile-manager.exe`
2. Create a folder: `C:\Tools\ClaudeProfileManager\`
3. Copy the executable to this folder
4. Add the folder to your system PATH (optional)

## Post-Installation Setup

### 1. Verify Installation

Open Command Prompt or PowerShell and run:

```cmd
claude-profile-manager --version
```

Expected output:
```
Claude Profile Manager for Windows v1.0.0.0
Secure authentication profile management for Claude Code CLI
```

### 2. First Profile Setup

If you have Claude Code CLI configured, save your first profile:

```cmd
claude-profile-manager save my-profile
```

## Installation Locations

### Chocolatey Installation
- **Executable**: `C:\ProgramData\chocolatey\lib\claude-profile-manager\tools\claude-profile-manager.exe`
- **Shim**: `C:\ProgramData\chocolatey\bin\claude-profile-manager.exe` (in PATH)

### User Data Locations
- **Profiles**: `%USERPROFILE%\.claude\profiles\`
- **Credentials**: Windows Credential Manager (secure storage)
- **Configuration**: `%USERPROFILE%\.claude\config\`

## Security and Permissions

### Windows Credential Manager Integration
- Credentials are stored securely in Windows Credential Manager
- No plain text storage of sensitive information
- Automatic encryption using Windows security APIs
- Per-user credential isolation

### File System Permissions
- Profile metadata stored with user-only access (no administrator required)
- ACL-based security for profile directories
- Automatic cleanup on profile deletion

### Network and Firewall
- No inbound network connections required
- Outbound HTTPS connections to Claude APIs only
- No additional firewall configuration needed

## Troubleshooting Common Installation Issues

### Issue: "Application failed to start"
**Solution**: Verify .NET 9.0 runtime is available or use self-contained installation

### Issue: "Command not found" after installation
**Solution**: 
1. Restart command prompt/PowerShell
2. Verify PATH environment variable includes installation directory
3. Use full path to executable

### Issue: "Profile directory creation failed"
**Solution**: 
1. Ensure `%USERPROFILE%\.claude` directory exists
2. Check write permissions to user profile directory
3. Run `claude-profile-manager list` to verify profile system is working

## Uninstallation

### Chocolatey Installation
```powershell
choco uninstall claude-profile-manager
```

### Portable Installation
1. Delete the executable and its directory
2. Remove PATH entries (if added)

### Complete Cleanup (Optional)
To remove all user data:
```cmd
# Remove profile directory (WARNING: This deletes all profiles)
rmdir /s "%USERPROFILE%\.claude\profiles"

# Remove credentials from Windows Credential Manager
# Use Windows Credential Manager GUI to remove "Claude Profile Manager" entries
```

## Support and Documentation

- **Quick Help**: `claude-profile-manager --help`
- **List Profiles**: `claude-profile-manager list`
- **Current Profile**: `claude-profile-manager current`
- **GitHub Repository**: [Claude Profile Manager](https://github.com/derekspelledcorrectly/claude-profile-manager)
- **Issue Reporting**: [GitHub Issues](https://github.com/derekspelledcorrectly/claude-profile-manager/issues)

## Enterprise Deployment

### Silent Installation
```powershell
# Chocolatey silent installation
choco install claude-profile-manager -y
```

### Registry Settings
Enterprise administrators can pre-configure settings via registry:
- Key: `HKLM\Software\ClaudeProfileManager`
- Values: Configuration options for default behavior

For detailed enterprise deployment guidance, see the Administrator's Guide.