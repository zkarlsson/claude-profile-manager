# Claude Profile Manager - Windows Installer

This directory contains the PowerShell-based Windows installer for Claude Profile Manager.

## Overview

The installer provides professional Windows deployment without requiring specialized tooling:

- **PowerShell-based installer** with embedded payload
- **Windows registry integration** for Add/Remove Programs
- **Optional PATH integration** for command-line access
- **Desktop shortcuts** and Start Menu integration
- **Proper uninstall support** with cleanup
- **Batch wrapper** for easy double-click installation

## Installer Architecture

### Files Created

```
packages/installer/
├── install-claude-profile-manager.ps1    # Main PowerShell installer
├── install-claude-profile-manager.bat    # Batch wrapper for easy execution
├── INSTALLATION-README.md                # User installation instructions
└── Payload/                             # Application files and documentation
    ├── claude-profile-manager.exe       # Main executable
    ├── *.pdb, *.xml                    # Debug symbols and documentation
    └── docs/                           # Complete documentation package
```

### Installation Process

1. **Admin Check**: Verifies administrator privileges
2. **File Deployment**: Copies payload to Program Files
3. **Registry Integration**: Creates uninstall entries
4. **Optional Features**: PATH integration, shortcuts
5. **Uninstaller Creation**: Self-contained removal script

## Usage

### Building the Installer

```powershell
# Create installer package
.\Simple-Installer.ps1

# Build specific configuration
.\Simple-Installer.ps1 -Configuration Release
```

### Installing the Application

**Method 1: Batch Launcher (Recommended)**
- Right-click `install-claude-profile-manager.bat`
- Select "Run as administrator"
- Follow prompts

**Method 2: PowerShell Direct**
```powershell
# Basic installation
.\install-claude-profile-manager.ps1

# With options
.\install-claude-profile-manager.ps1 -AddToPath -CreateDesktopShortcut

# Silent installation
.\install-claude-profile-manager.ps1 -AddToPath -Silent

# Custom directory
.\install-claude-profile-manager.ps1 -InstallPath "D:\Tools\ClaudeProfileManager"
```

### Uninstalling

```powershell
# Via installer
.\install-claude-profile-manager.ps1 -Uninstall

# Via Windows Add/Remove Programs
# Search for "Claude Profile Manager"
```

## Installation Options

| Option | Description | Default |
|--------|-------------|---------|
| `-InstallPath` | Custom installation directory | `C:\Program Files\ClaudeProfileManager` |
| `-AddToPath` | Add to system PATH | `$false` |
| `-CreateDesktopShortcut` | Create desktop shortcut | `$false` |
| `-Silent` | No interactive prompts | `$false` |
| `-Uninstall` | Uninstall application | `$false` |
| `-Help` | Show usage information | `$false` |

## Registry Integration

### Uninstall Entry
```
HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{12345678-1234-5678-9ABC-123456789012}
├── DisplayName: "Claude Profile Manager"
├── DisplayVersion: "1.0.0"
├── Publisher: "Claude Profile Manager Team"
├── InstallLocation: "C:\Program Files\ClaudeProfileManager"
└── UninstallString: "powershell.exe -ExecutionPolicy Bypass -File ..."
```

## Advantages Over MSI/WiX

### **Simplicity**
- No external dependencies (WiX, Visual Studio, etc.)
- Pure PowerShell implementation
- Easy to modify and customize

### **Accessibility**  
- Works on any Windows system with PowerShell 5.1+
- No special tooling required for building
- Easy to debug and troubleshoot

### **Flexibility**
- Custom installation logic
- Easy parameter handling
- Scriptable installation options

### **Size Efficiency**
- No installer framework overhead
- Direct file deployment
- Compact package size

## Enterprise Deployment

### Group Policy Deployment
```powershell
# Create deployment script
$InstallArgs = @(
    "-InstallPath", "C:\Program Files\ClaudeProfileManager"
    "-AddToPath"
    "-Silent"
)

& "\\server\share\install-claude-profile-manager.ps1" @InstallArgs
```

### SCCM Integration
- Package Type: Script
- Installation Command: `powershell.exe -ExecutionPolicy Bypass -File install-claude-profile-manager.ps1 -Silent -AddToPath`
- Uninstall Command: `powershell.exe -ExecutionPolicy Bypass -File install-claude-profile-manager.ps1 -Uninstall`

### PowerShell DSC
```powershell
Script ClaudeProfileManager {
    SetScript = {
        & "C:\Deploy\install-claude-profile-manager.ps1" -Silent -AddToPath
    }
    TestScript = {
        Test-Path "C:\Program Files\ClaudeProfileManager\claude-profile-manager.exe"
    }
    GetScript = { @{ Result = "ClaudeProfileManager" } }
}
```

## Security Considerations

### **Admin Privileges Required**
- Installation requires administrator rights
- Registry writes need elevated permissions
- PATH modifications require system access

### **Execution Policy**
- Installer bypasses execution policy for installation
- Uses `-ExecutionPolicy Bypass` parameter
- Safe for enterprise deployment

### **File Verification**
- All payload files copied with integrity checks
- Registry entries validated during creation
- Clean uninstallation with proper cleanup

## Troubleshooting

### **Common Issues**

**"Execution policy restricted"**
```powershell
# Solution: Use bypass parameter
powershell.exe -ExecutionPolicy Bypass -File install-claude-profile-manager.ps1
```

**"Access denied" during installation**
```
Solution: Run as administrator (required for registry writes)
```

**"Installation files not found"**
```
Solution: Ensure Payload directory exists alongside installer
```

### **Debug Installation**
```powershell
# Add verbose output
$VerbosePreference = "Continue"
.\install-claude-profile-manager.ps1 -Verbose

# Check installation status
Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{12345678-1234-5678-9ABC-123456789012}"
```

### **Manual Cleanup**
```powershell
# Remove installation directory
Remove-Item "C:\Program Files\ClaudeProfileManager" -Recurse -Force

# Remove registry entries  
Remove-Item "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{12345678-1234-5678-9ABC-123456789012}" -Recurse -Force

# Clean PATH variable
$path = [Environment]::GetEnvironmentVariable("PATH", "Machine")
$newPath = ($path -split ';' | Where-Object { $_ -ne "C:\Program Files\ClaudeProfileManager" }) -join ';'
[Environment]::SetEnvironmentVariable("PATH", $newPath, "Machine")
```

## Package Size

- **Total Package**: ~81 MB
- **Executable**: ~80.6 MB (self-contained .NET 9 app)
- **Documentation**: ~0.3 MB
- **Installer Logic**: ~0.1 MB

## Build Requirements

- **.NET 9 SDK**: For building the application
- **PowerShell 5.1+**: For installer creation
- **Windows 10/11**: Target platform

## Testing

### **Automated Testing**
```powershell
# Test installer creation
.\Simple-Installer.ps1

# Verify package integrity
Test-Path "packages\installer\install-claude-profile-manager.ps1"
Test-Path "packages\installer\Payload\claude-profile-manager.exe"
```

### **Manual Testing**
1. Create installer package
2. Test installation in clean VM
3. Verify all features work
4. Test uninstallation
5. Confirm clean removal

---

**Version**: 1.0.0  
**Last Updated**: 2025-08-26