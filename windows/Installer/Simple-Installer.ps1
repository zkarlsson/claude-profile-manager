#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Simple Windows installer for Claude Profile Manager.

.DESCRIPTION
    Creates a straightforward installer package using PowerShell without complex nesting.

.EXAMPLE
    .\Simple-Installer.ps1
#>

[CmdletBinding()]
param(
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Configuration
$ScriptRoot = $PSScriptRoot
$ProjectRoot = Split-Path $ScriptRoot -Parent
$DistDir = Join-Path $ProjectRoot "dist"
$OutputDir = Join-Path $ProjectRoot "packages\installer"

$ProductName = "Claude Profile Manager"
$ProductVersion = "1.0.0"
$ProductPublisher = "Claude Profile Manager Team"

Write-Host "🏗️  Creating Simple Windows Installer" -ForegroundColor Cyan

# Ensure output directory exists
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

# Verify application files
if (-not (Test-Path (Join-Path $DistDir "claude-profile-manager.exe"))) {
    Write-Error "❌ Application not built. Run Build-Distribution.ps1 first."
    exit 1
}

# Create main installer script
$MainInstaller = @'
#!/usr/bin/env pwsh
# Claude Profile Manager Windows Installer
param(
    [string]$InstallPath = "C:\Program Files\ClaudeProfileManager",
    [switch]$AddToPath,
    [switch]$CreateDesktopShortcut,
    [switch]$Silent,
    [switch]$Uninstall,
    [switch]$Help
)

if ($Help) {
    Write-Host @"
Claude Profile Manager Windows Installer v1.0.0

USAGE:
    .\install-claude-profile-manager.ps1 [OPTIONS]

OPTIONS:
    -InstallPath <path>      Custom installation directory
    -AddToPath               Add to system PATH
    -CreateDesktopShortcut   Create desktop shortcut  
    -Silent                  Silent installation
    -Uninstall              Uninstall application
    -Help                   Show this help

EXAMPLES:
    .\install-claude-profile-manager.ps1
    .\install-claude-profile-manager.ps1 -AddToPath -CreateDesktopShortcut
    .\install-claude-profile-manager.ps1 -Uninstall

"@
    return
}

$ProductName = "Claude Profile Manager"
$ProductVersion = "1.0.0"
$ProductGuid = "{12345678-1234-5678-9ABC-123456789012}"

function Test-AdminRights {
    return ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")
}

function Install-Application {
    if (-not (Test-AdminRights)) {
        Write-Error "Administrator privileges required"
        return 1
    }
    
    Write-Host "Installing $ProductName to $InstallPath..." -ForegroundColor Green
    
    # Create installation directory
    if (-not (Test-Path $InstallPath)) {
        New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null
    }
    
    # Copy files from embedded payload directory
    $PayloadDir = Join-Path $PSScriptRoot "Payload"
    if (-not (Test-Path $PayloadDir)) {
        Write-Error "Installation files not found"
        return 1
    }
    
    Copy-Item "$PayloadDir\*" $InstallPath -Recurse -Force
    
    # Registry entries
    $RegPath = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\$ProductGuid"
    New-Item -Path $RegPath -Force | Out-Null
    Set-ItemProperty -Path $RegPath -Name "DisplayName" -Value $ProductName
    Set-ItemProperty -Path $RegPath -Name "DisplayVersion" -Value $ProductVersion
    Set-ItemProperty -Path $RegPath -Name "Publisher" -Value "Claude Profile Manager Team"
    Set-ItemProperty -Path $RegPath -Name "InstallLocation" -Value $InstallPath
    Set-ItemProperty -Path $RegPath -Name "UninstallString" -Value "powershell.exe -ExecutionPolicy Bypass -File `"$($InstallPath)\uninstall.ps1`""
    
    # Create uninstaller
    $UninstallScript = @"
Write-Host 'Uninstalling $ProductName...' -ForegroundColor Yellow

# Remove from PATH
`$path = [Environment]::GetEnvironmentVariable('PATH', 'Machine')
if (`$path -like '*$InstallPath*') {
    `$newPath = (`$path -split ';' | Where-Object { `$_ -ne '$InstallPath' }) -join ';'
    [Environment]::SetEnvironmentVariable('PATH', `$newPath, 'Machine')
}

# Remove shortcuts
`$desktop = [Environment]::GetFolderPath('CommonDesktopDirectory')
`$shortcut = Join-Path `$desktop '$ProductName.lnk'
if (Test-Path `$shortcut) { Remove-Item `$shortcut -Force }

# Remove registry
Remove-Item 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\$ProductGuid' -Recurse -Force -ErrorAction SilentlyContinue

Write-Host 'Uninstallation complete' -ForegroundColor Green

# Self-delete
Start-Process cmd -ArgumentList '/c timeout /t 2 >nul & rmdir /s /q `"$InstallPath`"' -WindowStyle Hidden
"@
    
    $UninstallScript | Out-File -FilePath (Join-Path $InstallPath "uninstall.ps1") -Encoding UTF8
    
    # Add to PATH if requested
    if ($AddToPath) {
        $currentPath = [Environment]::GetEnvironmentVariable("PATH", "Machine")
        if ($currentPath -notlike "*$InstallPath*") {
            $newPath = $currentPath + ";" + $InstallPath
            [Environment]::SetEnvironmentVariable("PATH", $newPath, "Machine")
            Write-Host "Added to system PATH" -ForegroundColor Green
        }
    }
    
    # Create desktop shortcut if requested
    if ($CreateDesktopShortcut) {
        $WScriptShell = New-Object -ComObject WScript.Shell
        $Desktop = [Environment]::GetFolderPath("CommonDesktopDirectory")
        $Shortcut = $WScriptShell.CreateShortcut((Join-Path $Desktop "$ProductName.lnk"))
        $Shortcut.TargetPath = Join-Path $InstallPath "claude-profile-manager.exe"
        $Shortcut.WorkingDirectory = $InstallPath
        $Shortcut.Description = "Claude Profile Manager"
        $Shortcut.Save()
        Write-Host "Desktop shortcut created" -ForegroundColor Green
    }
    
    Write-Host "Installation completed successfully!" -ForegroundColor Green
    Write-Host "Run 'claude-profile-manager --help' to get started" -ForegroundColor Yellow
    
    return 0
}

function Uninstall-Application {
    if (-not (Test-AdminRights)) {
        Write-Error "Administrator privileges required"
        return 1
    }
    
    # Find install path from registry
    $RegPath = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\$ProductGuid"
    if (Test-Path $RegPath) {
        $InstallPath = Get-ItemProperty -Path $RegPath -Name "InstallLocation" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty InstallLocation
        if ($InstallPath -and (Test-Path $InstallPath)) {
            $UninstallScript = Join-Path $InstallPath "uninstall.ps1"
            if (Test-Path $UninstallScript) {
                & $UninstallScript
                return 0
            }
        }
    }
    
    Write-Warning "Installation not found"
    return 1
}

# Main execution
if ($Uninstall) {
    exit (Uninstall-Application)
} else {
    exit (Install-Application)
}
'@

# Create installer with payload
$InstallerPath = Join-Path $OutputDir "install-claude-profile-manager.ps1"
$MainInstaller | Out-File -FilePath $InstallerPath -Encoding UTF8

Write-Host "✓ Main installer script created" -ForegroundColor Green

# Create payload directory structure
$PayloadPath = Join-Path $OutputDir "Payload"
if (Test-Path $PayloadPath) {
    Remove-Item $PayloadPath -Recurse -Force
}
New-Item -ItemType Directory -Path $PayloadPath -Force | Out-Null

# Copy application files
Copy-Item "$DistDir\*" $PayloadPath -Force

# Copy documentation
$DocsPath = Join-Path $PayloadPath "docs"
New-Item -ItemType Directory -Path $DocsPath -Force | Out-Null

$DocFiles = @("INSTALL.md", "LICENSE", "SECURITY.md", "CHANGELOG.md", "SYSTEM-REQUIREMENTS.md", "THIRD-PARTY-NOTICES", "README.md")
foreach ($DocFile in $DocFiles) {
    $SourcePath = Join-Path $ProjectRoot $DocFile
    if (Test-Path $SourcePath) {
        Copy-Item $SourcePath $DocsPath -Force
    }
}

Write-Host "✓ Payload directory created" -ForegroundColor Green

# Create batch wrapper for easier execution
$BatchWrapper = @"
@echo off
echo Claude Profile Manager Windows Installer
echo ========================================

net session >nul 2>&1
if %errorLevel% neq 0 (
    echo This installer requires administrator privileges.
    echo Please right-click and "Run as administrator"
    pause
    exit /b 1
)

powershell.exe -ExecutionPolicy Bypass -File "%~dp0install-claude-profile-manager.ps1" %*
if %errorLevel% equ 0 (
    echo.
    echo Installation completed successfully!
) else (
    echo.
    echo Installation failed.
)
pause
"@

$BatchPath = Join-Path $OutputDir "install-claude-profile-manager.bat"
$BatchWrapper | Out-File -FilePath $BatchPath -Encoding ASCII

Write-Host "✓ Batch wrapper created" -ForegroundColor Green

# Create installation instructions
$Instructions = @"
# Claude Profile Manager Windows Installation

## Quick Installation

1. **Right-click** `install-claude-profile-manager.bat` 
2. **Select** "Run as administrator"
3. **Follow** the prompts

## PowerShell Installation

Run as administrator:
``````powershell
.\install-claude-profile-manager.ps1
``````

## Installation Options

``````powershell
# With PATH and desktop shortcut
.\install-claude-profile-manager.ps1 -AddToPath -CreateDesktopShortcut

# Custom installation directory
.\install-claude-profile-manager.ps1 -InstallPath "C:\Tools\ClaudeProfileManager"

# Silent installation
.\install-claude-profile-manager.ps1 -AddToPath -Silent
``````

## Uninstallation

``````powershell
.\install-claude-profile-manager.ps1 -Uninstall
``````

Or use Windows Add/Remove Programs.

## Requirements

- Windows 10/11
- Administrator privileges
- PowerShell 5.1+ (included in Windows)

## Files Included

- claude-profile-manager.exe ($('{0:F2}' -f ((Get-Item (Join-Path $DistDir "claude-profile-manager.exe")).Length / 1MB)) MB)
- Complete documentation package
- Uninstaller

## Support

For help: ``claude-profile-manager --help``
For issues: https://github.com/derekspelledcorrectly/claude-profile-manager/issues
"@

$InstructionsPath = Join-Path $OutputDir "INSTALLATION-README.md"
$Instructions | Out-File -FilePath $InstructionsPath -Encoding UTF8

Write-Host "✓ Installation instructions created" -ForegroundColor Green

# Summary
$InstallerSize = (Get-Item $InstallerPath).Length + (Get-ChildItem $PayloadPath -Recurse | Measure-Object Length -Sum).Sum
$InstallerSizeMB = [Math]::Round($InstallerSize / 1MB, 2)

Write-Host ""
Write-Host "🎉 Simple Windows Installer Package Created!" -ForegroundColor Green
Write-Host ""
Write-Host "📦 Package Contents:" -ForegroundColor Cyan
Write-Host "  Main Installer: $(Split-Path $InstallerPath -Leaf)" -ForegroundColor White  
Write-Host "  Batch Launcher: $(Split-Path $BatchPath -Leaf)" -ForegroundColor White
Write-Host "  Payload Directory: Payload\" -ForegroundColor White
Write-Host "  Instructions: $(Split-Path $InstructionsPath -Leaf)" -ForegroundColor White
Write-Host ""
Write-Host "📏 Total Package Size: ${InstallerSizeMB} MB" -ForegroundColor Yellow
Write-Host ""
Write-Host "✅ Ready for distribution!" -ForegroundColor Green
Write-Host ""
Write-Host "To test installation:" -ForegroundColor Cyan
Write-Host "  1. Right-click install-claude-profile-manager.bat" -ForegroundColor Gray
Write-Host "  2. Select 'Run as administrator'" -ForegroundColor Gray
Write-Host "  3. Follow the prompts" -ForegroundColor Gray

return @{
    InstallerPath = $InstallerPath
    BatchPath = $BatchPath
    PayloadPath = $PayloadPath
    InstructionsPath = $InstructionsPath
    PackageSize = $InstallerSizeMB
    Success = $true
}