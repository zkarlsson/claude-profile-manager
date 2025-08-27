#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Creates a Windows installer for Claude Profile Manager using PowerShell-based packaging.

.DESCRIPTION
    Generates a professional Windows installer package without requiring WiX Toolset:
    - Self-extracting archive with installation logic
    - Windows registry integration
    - Start Menu and Desktop shortcuts
    - PATH environment variable management
    - Proper uninstall support

.PARAMETER Configuration
    Build configuration (Release, Debug). Default: Release

.PARAMETER InstallPath
    Default installation path. Default: C:\Program Files\ClaudeProfileManager

.PARAMETER CreateDesktopShortcut
    Create desktop shortcut during installation

.PARAMETER AddToPath
    Add installation directory to system PATH

.PARAMETER Sign
    Digitally sign the installer

.EXAMPLE
    .\Create-WindowsInstaller.ps1
    .\Create-WindowsInstaller.ps1 -CreateDesktopShortcut -AddToPath -Sign
#>

[CmdletBinding()]
param(
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release",
    
    [string]$InstallPath = "C:\Program Files\ClaudeProfileManager",
    
    [switch]$CreateDesktopShortcut,
    
    [switch]$AddToPath,
    
    [switch]$Sign
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Configuration
$ScriptRoot = $PSScriptRoot
$ProjectRoot = Split-Path $ScriptRoot -Parent
$DistDir = Join-Path $ProjectRoot "dist"
$OutputDir = Join-Path $ProjectRoot "packages\installer"
$TempDir = Join-Path $env:TEMP "claude-profile-manager-installer"

$ProductName = "Claude Profile Manager"
$ProductVersion = "1.0.0"
$ProductPublisher = "Claude Profile Manager Team"
$ProductGuid = "{12345678-1234-5678-9ABC-123456789012}"

Write-Host "🏗️  Creating Windows Installer for $ProductName" -ForegroundColor Cyan
Write-Host "Version: $ProductVersion" -ForegroundColor Yellow
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow

# Clean and prepare directories
if (Test-Path $TempDir) {
    Remove-Item $TempDir -Recurse -Force
}
New-Item -ItemType Directory -Path $TempDir -Force | Out-Null

if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

# Verify application files
Write-Host "🔍 Verifying application files..." -ForegroundColor Yellow

$RequiredFiles = @(
    "claude-profile-manager.exe"
)

foreach ($File in $RequiredFiles) {
    $FilePath = Join-Path $DistDir $File
    if (-not (Test-Path $FilePath)) {
        Write-Error "❌ Required file not found: $FilePath"
        exit 1
    }
}

Write-Host "  ✓ All application files verified" -ForegroundColor Green

# Create installer payload directory
$PayloadDir = Join-Path $TempDir "Payload"
New-Item -ItemType Directory -Path $PayloadDir -Force | Out-Null

# Copy application files
Write-Host "📦 Preparing installer payload..." -ForegroundColor Yellow

Copy-Item "$DistDir\*" $PayloadDir -Recurse -Force

# Copy documentation
$DocsDir = Join-Path $PayloadDir "docs"
New-Item -ItemType Directory -Path $DocsDir -Force | Out-Null

$DocumentationFiles = @(
    "INSTALL.md",
    "LICENSE", 
    "SECURITY.md",
    "CHANGELOG.md",
    "SYSTEM-REQUIREMENTS.md",
    "THIRD-PARTY-NOTICES",
    "README.md"
)

foreach ($DocFile in $DocumentationFiles) {
    $SourcePath = Join-Path $ProjectRoot $DocFile
    if (Test-Path $SourcePath) {
        Copy-Item $SourcePath $DocsDir -Force
    }
}

Write-Host "  ✓ Payload prepared: $(Get-ChildItem $PayloadDir -Recurse | Measure-Object | Select-Object -ExpandProperty Count) files" -ForegroundColor Green

# Create installation script
Write-Host "🔧 Creating installation logic..." -ForegroundColor Yellow

$InstallScript = @"
#!/usr/bin/env pwsh
# Claude Profile Manager Windows Installer
# Auto-generated installation script

param(
    [string]`$InstallPath = "$InstallPath",
    [switch]`$Silent,
    [switch]`$CreateDesktopShortcut = `$$CreateDesktopShortcut,
    [switch]`$AddToPath = `$$AddToPath,
    [switch]`$Uninstall
)

Set-StrictMode -Version Latest
`$ErrorActionPreference = "Stop"

# Product Information
`$ProductName = "$ProductName"
`$ProductVersion = "$ProductVersion"
`$ProductPublisher = "$ProductPublisher"
`$ProductGuid = "$ProductGuid"

# Registry paths
`$UninstallRegPath = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\`$ProductGuid"
`$ProductRegPath = "HKLM:\SOFTWARE\`$ProductPublisher\`$ProductName"

function Write-InstallLog {
    param([string]`$Message, [string]`$Level = "INFO")
    if (-not `$Silent) {
        `$Color = switch (`$Level) {
            "ERROR" { "Red" }
            "WARN"  { "Yellow" }
            "INFO"  { "White" }
            "SUCCESS" { "Green" }
        }
        Write-Host "`$Message" -ForegroundColor `$Color
    }
}

function Test-AdminRights {
    return ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")
}

function Install-Application {
    Write-InstallLog "🏗️  Installing `$ProductName v`$ProductVersion" "INFO"
    
    # Check admin rights
    if (-not (Test-AdminRights)) {
        Write-InstallLog "❌ Administrator privileges required for installation" "ERROR"
        exit 1
    }
    
    try {
        # Create installation directory
        if (-not (Test-Path `$InstallPath)) {
            Write-InstallLog "Creating installation directory: `$InstallPath" "INFO"
            New-Item -ItemType Directory -Path `$InstallPath -Force | Out-Null
        }
        
        # Copy application files
        Write-InstallLog "Copying application files..." "INFO"
        `$PayloadPath = Join-Path `$PSScriptRoot "Payload"
        Copy-Item "`$PayloadPath\*" `$InstallPath -Recurse -Force
        
        # Create registry entries
        Write-InstallLog "Creating registry entries..." "INFO"
        
        # Product registry
        if (-not (Test-Path `$ProductRegPath)) {
            New-Item -Path `$ProductRegPath -Force | Out-Null
        }
        Set-ItemProperty -Path `$ProductRegPath -Name "InstallPath" -Value `$InstallPath
        Set-ItemProperty -Path `$ProductRegPath -Name "Version" -Value `$ProductVersion
        Set-ItemProperty -Path `$ProductRegPath -Name "ExePath" -Value (Join-Path `$InstallPath "claude-profile-manager.exe")
        
        # Uninstall registry
        if (-not (Test-Path `$UninstallRegPath)) {
            New-Item -Path `$UninstallRegPath -Force | Out-Null
        }
        Set-ItemProperty -Path `$UninstallRegPath -Name "DisplayName" -Value `$ProductName
        Set-ItemProperty -Path `$UninstallRegPath -Name "DisplayVersion" -Value `$ProductVersion
        Set-ItemProperty -Path `$UninstallRegPath -Name "Publisher" -Value `$ProductPublisher
        Set-ItemProperty -Path `$UninstallRegPath -Name "InstallLocation" -Value `$InstallPath
        Set-ItemProperty -Path `$UninstallRegPath -Name "UninstallString" -Value "powershell.exe -ExecutionPolicy Bypass -File `"`$(Join-Path `$InstallPath 'Uninstall.ps1')`""
        Set-ItemProperty -Path `$UninstallRegPath -Name "NoModify" -Value 1 -Type DWord
        Set-ItemProperty -Path `$UninstallRegPath -Name "NoRepair" -Value 1 -Type DWord
        
        # Create uninstall script
        `$UninstallScript = Join-Path `$InstallPath "Uninstall.ps1"
        `$UninstallScriptContent = @"
#!/usr/bin/env pwsh
# Claude Profile Manager Uninstaller

Set-StrictMode -Version Latest
`$ErrorActionPreference = "Stop"

Write-Host "Uninstalling `$ProductName..." -ForegroundColor Yellow

# Remove from PATH if present
`$CurrentPath = [Environment]::GetEnvironmentVariable("PATH", "Machine")
if (`$CurrentPath -like "*`$InstallPath*") {
    `$NewPath = (`$CurrentPath -split ';' | Where-Object { `$_ -ne "`$InstallPath" }) -join ';'
    [Environment]::SetEnvironmentVariable("PATH", `$NewPath, "Machine")
    Write-Host "Removed from system PATH" -ForegroundColor Green
}

# Remove shortcuts
`$DesktopShortcut = Join-Path ([Environment]::GetFolderPath("CommonDesktopDirectory")) "`$ProductName.lnk"
if (Test-Path `$DesktopShortcut) {
    Remove-Item `$DesktopShortcut -Force
    Write-Host "Removed desktop shortcut" -ForegroundColor Green
}

`$StartMenuShortcut = Join-Path ([Environment]::GetFolderPath("CommonStartMenu")) "Programs\`$ProductName.lnk"
if (Test-Path `$StartMenuShortcut) {
    Remove-Item `$StartMenuShortcut -Force
    Write-Host "Removed start menu shortcut" -ForegroundColor Green
}

# Remove registry entries
if (Test-Path "`$UninstallRegPath") {
    Remove-Item "`$UninstallRegPath" -Recurse -Force
    Write-Host "Removed uninstall registry entry" -ForegroundColor Green
}

if (Test-Path "`$ProductRegPath") {
    Remove-Item "`$ProductRegPath" -Recurse -Force
    Write-Host "Removed product registry entry" -ForegroundColor Green
}

Write-Host "✓ `$ProductName uninstalled successfully" -ForegroundColor Green
Write-Host "Note: User profile data in %USERPROFILE%\.claude remains untouched" -ForegroundColor Yellow

# Self-delete the installation directory
Write-Host "Scheduling installation directory cleanup..." -ForegroundColor Yellow
Start-Process -FilePath "cmd.exe" -ArgumentList "/c", "timeout /t 3 >nul & rmdir /s /q `"`$InstallPath`"" -WindowStyle Hidden
"@
        `$UninstallScriptContent | Out-File -FilePath `$UninstallScript -Encoding UTF8
        
        # Add to PATH if requested
        if (`$AddToPath) {
            Write-InstallLog "Adding to system PATH..." "INFO"
            `$CurrentPath = [Environment]::GetEnvironmentVariable("PATH", "Machine")
            if (`$CurrentPath -notlike "*`$InstallPath*") {
                `$NewPath = `$CurrentPath + ";" + `$InstallPath
                [Environment]::SetEnvironmentVariable("PATH", `$NewPath, "Machine")
                Write-InstallLog "Added to system PATH" "SUCCESS"
            }
        }
        
        # Create desktop shortcut if requested
        if (`$CreateDesktopShortcut) {
            Write-InstallLog "Creating desktop shortcut..." "INFO"
            `$WScriptShell = New-Object -ComObject WScript.Shell
            `$DesktopPath = [Environment]::GetFolderPath("CommonDesktopDirectory")
            `$Shortcut = `$WScriptShell.CreateShortcut((Join-Path `$DesktopPath "`$ProductName.lnk"))
            `$Shortcut.TargetPath = Join-Path `$InstallPath "claude-profile-manager.exe"
            `$Shortcut.Arguments = "--help"
            `$Shortcut.WorkingDirectory = `$InstallPath
            `$Shortcut.Description = "Claude Profile Manager - Secure authentication profile management"
            `$Shortcut.Save()
            Write-InstallLog "Desktop shortcut created" "SUCCESS"
        }
        
        # Create start menu shortcut
        Write-InstallLog "Creating start menu shortcut..." "INFO"
        `$WScriptShell = New-Object -ComObject WScript.Shell
        `$StartMenuPath = [Environment]::GetFolderPath("CommonStartMenu")
        `$Shortcut = `$WScriptShell.CreateShortcut((Join-Path `$StartMenuPath "Programs\`$ProductName.lnk"))
        `$Shortcut.TargetPath = Join-Path `$InstallPath "claude-profile-manager.exe"
        `$Shortcut.Arguments = "--help"
        `$Shortcut.WorkingDirectory = `$InstallPath
        `$Shortcut.Description = "Claude Profile Manager - Secure authentication profile management"
        `$Shortcut.Save()
        
        Write-InstallLog "✓ Installation completed successfully!" "SUCCESS"
        Write-InstallLog "Installed to: `$InstallPath" "INFO"
        Write-InstallLog "Run 'claude-profile-manager --help' to get started" "INFO"
        
        return 0
        
    } catch {
        Write-InstallLog "❌ Installation failed: `$(`$_.Exception.Message)" "ERROR"
        return 1
    }
}

function Uninstall-Application {
    Write-InstallLog "🗑️  Uninstalling `$ProductName" "INFO"
    
    # Check admin rights
    if (-not (Test-AdminRights)) {
        Write-InstallLog "❌ Administrator privileges required for uninstallation" "ERROR"
        exit 1
    }
    
    # Find installation path from registry
    if (Test-Path `$ProductRegPath) {
        `$InstallPath = Get-ItemProperty -Path `$ProductRegPath -Name "InstallPath" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty "InstallPath"
        
        if (`$InstallPath -and (Test-Path `$InstallPath)) {
            `$UninstallScript = Join-Path `$InstallPath "Uninstall.ps1"
            if (Test-Path `$UninstallScript) {
                & `$UninstallScript
                return `$LASTEXITCODE
            }
        }
    }
    
    Write-InstallLog "❌ Installation not found or already removed" "WARN"
    return 1
}

# Main execution
if (`$Uninstall) {
    exit (Uninstall-Application)
} else {
    exit (Install-Application)
}
"@

$InstallScriptPath = Join-Path $TempDir "Install.ps1"
$InstallScript | Out-File -FilePath $InstallScriptPath -Encoding UTF8

Write-Host "  ✓ Installation script created" -ForegroundColor Green

# Create self-extracting installer
Write-Host "📦 Creating self-extracting installer..." -ForegroundColor Yellow

$InstallerScript = @"
#!/usr/bin/env pwsh
# Claude Profile Manager Windows Installer Package
# Self-extracting installer with embedded payload

param(
    [string]`$InstallPath,
    [switch]`$Silent,
    [switch]`$CreateDesktopShortcut,
    [switch]`$AddToPath,
    [switch]`$Uninstall,
    [switch]`$Help
)

if (`$Help) {
    Write-Host @"
Claude Profile Manager Windows Installer

USAGE:
    .\claude-profile-manager-installer.ps1 [OPTIONS]

OPTIONS:
    -InstallPath <path>         Custom installation directory (default: C:\Program Files\ClaudeProfileManager)
    -CreateDesktopShortcut      Create desktop shortcut
    -AddToPath                  Add to system PATH environment variable
    -Silent                     Silent installation (no interactive prompts)
    -Uninstall                  Uninstall the application
    -Help                       Show this help message

EXAMPLES:
    # Interactive installation
    .\claude-profile-manager-installer.ps1
    
    # Silent installation with shortcuts and PATH
    .\claude-profile-manager-installer.ps1 -Silent -CreateDesktopShortcut -AddToPath
    
    # Custom installation directory
    .\claude-profile-manager-installer.ps1 -InstallPath "D:\Tools\ClaudeProfileManager"
    
    # Uninstall
    .\claude-profile-manager-installer.ps1 -Uninstall

For more information, visit: https://github.com/derekspelledcorrectly/claude-profile-manager
"@
    return
}

Set-StrictMode -Version Latest
`$ErrorActionPreference = "Stop"

# Extract embedded payload
`$TempExtractPath = Join-Path `$env:TEMP "claude-profile-manager-extract-`$(Get-Random)"
New-Item -ItemType Directory -Path `$TempExtractPath -Force | Out-Null

try {
    Write-Host "Extracting installer payload..." -ForegroundColor Yellow
    
    # Find the payload marker in this script
    `$ScriptContent = Get-Content `$PSCommandPath -Raw
    `$PayloadStart = `$ScriptContent.IndexOf("# PAYLOAD_START")
    
    if (`$PayloadStart -eq -1) {
        Write-Error "Installer payload not found. Corrupted installer."
        exit 1
    }
    
    # Extract base64 payload (this will be replaced with actual payload)
    `$PayloadData = `$ScriptContent.Substring(`$PayloadStart + "# PAYLOAD_START".Length).Trim()
    `$PayloadBytes = [Convert]::FromBase64String(`$PayloadData)
    
    # Decompress payload
    Add-Type -AssemblyName System.IO.Compression
    `$PayloadStream = New-Object System.IO.MemoryStream(,`$PayloadBytes)
    `$GzipStream = New-Object System.IO.Compression.GzipStream(`$PayloadStream, [System.IO.Compression.CompressionMode]::Decompress)
    `$OutputBuffer = New-Object byte[](4096)
    `$OutputPath = Join-Path `$TempExtractPath "payload.zip"
    `$FileStream = [System.IO.File]::Create(`$OutputPath)
    
    while ((`$BytesRead = `$GzipStream.Read(`$OutputBuffer, 0, `$OutputBuffer.Length)) -gt 0) {
        `$FileStream.Write(`$OutputBuffer, 0, `$BytesRead)
    }
    
    `$FileStream.Close()
    `$GzipStream.Close()
    `$PayloadStream.Close()
    
    # Extract ZIP
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::ExtractToDirectory(`$OutputPath, `$TempExtractPath)
    
    # Run installer
    `$InstallScript = Join-Path `$TempExtractPath "Install.ps1"
    if (-not (Test-Path `$InstallScript)) {
        Write-Error "Installation script not found in payload"
        exit 1
    }
    
    # Build arguments
    `$InstallArgs = @()
    if (`$InstallPath) { `$InstallArgs += "-InstallPath", `$InstallPath }
    if (`$Silent) { `$InstallArgs += "-Silent" }
    if (`$CreateDesktopShortcut) { `$InstallArgs += "-CreateDesktopShortcut" }
    if (`$AddToPath) { `$InstallArgs += "-AddToPath" }
    if (`$Uninstall) { `$InstallArgs += "-Uninstall" }
    
    & `$InstallScript @InstallArgs
    `$ExitCode = `$LASTEXITCODE
    
} finally {
    # Cleanup
    if (Test-Path `$TempExtractPath) {
        Remove-Item `$TempExtractPath -Recurse -Force -ErrorAction SilentlyContinue
    }
}

exit `$ExitCode

# PAYLOAD_START
"@

# Create ZIP payload
Write-Host "  Creating compressed payload..." -ForegroundColor Gray

$ZipPath = Join-Path $TempDir "payload.zip"
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($TempDir, $ZipPath, "Optimal", $false)

# Compress with GZIP
$ZipBytes = [System.IO.File]::ReadAllBytes($ZipPath)
$CompressedStream = New-Object System.IO.MemoryStream
$GzipStream = New-Object System.IO.Compression.GzipStream($CompressedStream, [System.IO.Compression.CompressionMode]::Compress)
$GzipStream.Write($ZipBytes, 0, $ZipBytes.Length)
$GzipStream.Close()
$CompressedBytes = $CompressedStream.ToArray()
$CompressedStream.Close()

# Base64 encode
$PayloadBase64 = [Convert]::ToBase64String($CompressedBytes)

# Create final installer
$FinalInstallerPath = Join-Path $OutputDir "claude-profile-manager-installer-$ProductVersion.ps1"
($InstallerScript + $PayloadBase64) | Out-File -FilePath $FinalInstallerPath -Encoding UTF8

Write-Host "  ✓ Self-extracting installer created" -ForegroundColor Green

# Create batch wrapper for easier execution
$BatchWrapper = @"
@echo off
setlocal

REM Claude Profile Manager Installer Wrapper
REM This batch file launches the PowerShell installer

echo Claude Profile Manager Windows Installer
echo ========================================
echo.

REM Check for admin rights
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo This installer requires administrator privileges.
    echo Please run as administrator.
    echo.
    pause
    exit /b 1
)

REM Launch PowerShell installer
powershell.exe -ExecutionPolicy Bypass -File "%~dp0claude-profile-manager-installer-$ProductVersion.ps1" %*

pause
"@

$BatchWrapperPath = Join-Path $OutputDir "claude-profile-manager-installer-$ProductVersion.bat"
$BatchWrapper | Out-File -FilePath $BatchWrapperPath -Encoding ASCII

Write-Host "  ✓ Batch wrapper created" -ForegroundColor Green

# Calculate file sizes
$InstallerSize = (Get-Item $FinalInstallerPath).Length
$InstallerSizeMB = [Math]::Round($InstallerSize / 1MB, 2)

Write-Host "  📄 Installer: $(Split-Path $FinalInstallerPath -Leaf) (${InstallerSizeMB} MB)" -ForegroundColor Gray
Write-Host "  📄 Wrapper: $(Split-Path $BatchWrapperPath -Leaf)" -ForegroundColor Gray

# Digital signing (if requested)
if ($Sign) {
    Write-Host "🔐 Digitally signing installer..." -ForegroundColor Yellow
    
    # Note: This would require actual signing certificate
    Write-Warning "⚠️  Digital signing not implemented - requires code signing certificate"
}

# Create installation instructions
$Instructions = @"
# Claude Profile Manager Windows Installer

## Installation Methods

### Method 1: PowerShell Installer (Recommended)
Run as administrator:
``````powershell
.\claude-profile-manager-installer-$ProductVersion.ps1
``````

### Method 2: Batch Wrapper
Double-click: `claude-profile-manager-installer-$ProductVersion.bat`

### Method 3: Silent Installation
``````powershell
.\claude-profile-manager-installer-$ProductVersion.ps1 -Silent -AddToPath -CreateDesktopShortcut
``````

## Installation Options
- `-InstallPath <path>` : Custom installation directory
- `-AddToPath` : Add to system PATH
- `-CreateDesktopShortcut` : Create desktop shortcut
- `-Silent` : No interactive prompts

## Uninstallation
``````powershell
.\claude-profile-manager-installer-$ProductVersion.ps1 -Uninstall
``````

Or use Windows Add/Remove Programs.

## Requirements
- Windows 10/11 with Administrator privileges
- PowerShell 5.1+ (built into Windows)

## File Verification
- Installer Size: ${InstallerSizeMB} MB
- Created: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
- Version: $ProductVersion
"@

$InstructionsPath = Join-Path $OutputDir "INSTALLATION-INSTRUCTIONS.md"
$Instructions | Out-File -FilePath $InstructionsPath -Encoding UTF8

# Cleanup temp directory
Remove-Item $TempDir -Recurse -Force

# Display summary
Write-Host ""
Write-Host "🎉 Windows Installer created successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "📦 Installer Outputs:" -ForegroundColor Cyan
Write-Host "  PowerShell Installer: $FinalInstallerPath" -ForegroundColor White
Write-Host "  Batch Wrapper: $BatchWrapperPath" -ForegroundColor White
Write-Host "  Instructions: $InstructionsPath" -ForegroundColor White
Write-Host ""
Write-Host "✅ Ready for distribution!" -ForegroundColor Green

return @{
    InstallerPath = $FinalInstallerPath
    WrapperPath = $BatchWrapperPath
    InstructionsPath = $InstructionsPath
    InstallerSize = $InstallerSizeMB
    Success = $true
}