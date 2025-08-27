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

### Method 1: MSI Installer (Recommended)

1. Download the MSI installer from the official release
2. Double-click `claude-profile-manager-1.0.0.msi`
3. Follow the installation wizard
4. The application will be installed to `C:\Program Files\ClaudeProfileManager\`
5. A desktop shortcut will be created (optional)

### Method 2: Chocolatey Package Manager

```powershell
# Install Chocolatey (if not already installed)
Set-ExecutionPolicy Bypass -Scope Process -Force
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072
iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))

# Install Claude Profile Manager
choco install claude-profile-manager
```

### Method 3: Portable Installation

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

### 2. Health Check

Verify system integration:

```cmd
claude-profile-manager health
```

Expected output:
```
Health Check Results:

✓ Application: Running normally
✓ Profile System: 0 profiles loaded
✓ Current Profile: None set

✓ All health checks passed
```

### 3. First Profile Setup

If you have Claude Code CLI configured, save your first profile:

```cmd
claude-profile-manager save my-profile
```

## Installation Locations

### MSI Installation
- **Executable**: `C:\Program Files\ClaudeProfileManager\claude-profile-manager.exe`
- **Shortcuts**: Desktop and Start Menu (optional)
- **Registry**: HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall\ClaudeProfileManager

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

### Issue: "Access denied" during installation
**Solution**: Run installer as administrator or use portable version

### Issue: "Command not found" after installation
**Solution**: 
1. Restart command prompt/PowerShell
2. Verify PATH environment variable includes installation directory
3. Use full path to executable

### Issue: "Profile directory creation failed"
**Solution**: 
1. Ensure `%USERPROFILE%\.claude` directory exists
2. Check write permissions to user profile directory
3. Run `claude-profile-manager health` for detailed diagnostics

## Uninstallation

### MSI Installation
1. Use Windows "Add or Remove Programs"
2. Find "Claude Profile Manager" in the list
3. Click "Uninstall" and follow prompts

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
- **Health Check**: `claude-profile-manager health`
- **System Status**: `claude-profile-manager status`
- **GitHub Repository**: [Claude Profile Manager](https://github.com/derekspelledcorrectly/claude-profile-manager)
- **Issue Reporting**: [GitHub Issues](https://github.com/derekspelledcorrectly/claude-profile-manager/issues)

## Enterprise Deployment

### Group Policy Support
The MSI installer supports enterprise deployment via Group Policy Software Installation.

### Silent Installation
```cmd
# MSI silent installation
msiexec /i claude-profile-manager-1.0.0.msi /quiet

# Chocolatey silent installation
choco install claude-profile-manager -y
```

### Registry Settings
Enterprise administrators can pre-configure settings via registry:
- Key: `HKLM\Software\ClaudeProfileManager`
- Values: Configuration options for default behavior

For detailed enterprise deployment guidance, see the Administrator's Guide.