#Requires -Version 3.0

$ErrorActionPreference = 'Stop'

# Package information
$packageName = 'claude-profile-manager'
$toolsDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$packageVersion = $env:ChocolateyPackageVersion

# Installation paths
$installLocation = Join-Path $env:ProgramFiles 'ClaudeProfileManager'
$appPath = Join-Path $toolsDir 'app'
$powerShellPath = Join-Path $toolsDir 'powershell'

Write-Host "Installing $packageName version $packageVersion" -ForegroundColor Green

try {
    # Create installation directory
    Write-Host "Creating installation directory at $installLocation"
    if (-not (Test-Path $installLocation)) {
        New-Item -ItemType Directory -Path $installLocation -Force | Out-Null
    }

    # Copy application files
    Write-Host "Installing application files..."
    if (Test-Path $appPath) {
        Copy-Item -Path "$appPath\*" -Destination $installLocation -Recurse -Force
        Write-Host "  ✓ Application files installed"
    } else {
        Write-Warning "Application files not found at $appPath"
    }

    # Install PowerShell module
    Write-Host "Installing PowerShell module..."
    $powerShellModulePath = $null
    
    # Determine PowerShell module path
    if ($PSVersionTable.PSVersion.Major -ge 6) {
        # PowerShell Core
        $powerShellModulePath = Join-Path ([Environment]::GetFolderPath('ProgramFiles')) 'PowerShell\Modules'
    } else {
        # Windows PowerShell
        $powerShellModulePath = Join-Path ([Environment]::GetFolderPath('ProgramFiles')) 'WindowsPowerShell\Modules'
    }
    
    $moduleInstallPath = Join-Path $powerShellModulePath 'ClaudeProfileManager'
    
    if (Test-Path $powerShellPath) {
        # Create module directory
        if (-not (Test-Path $moduleInstallPath)) {
            New-Item -ItemType Directory -Path $moduleInstallPath -Force | Out-Null
        }
        
        # Copy PowerShell module files
        Copy-Item -Path "$powerShellPath\ClaudeProfileManager\*" -Destination $moduleInstallPath -Recurse -Force
        Write-Host "  ✓ PowerShell module installed to $moduleInstallPath"
    } else {
        Write-Warning "PowerShell module files not found at $powerShellPath"
    }

    # Add to PATH if not already present
    Write-Host "Configuring system PATH..."
    $currentPath = [Environment]::GetEnvironmentVariable('PATH', 'Machine')
    if ($currentPath -notlike "*$installLocation*") {
        $newPath = "$currentPath;$installLocation"
        [Environment]::SetEnvironmentVariable('PATH', $newPath, 'Machine')
        
        # Update current session PATH
        $env:PATH = "$env:PATH;$installLocation"
        Write-Host "  ✓ Added to system PATH"
    } else {
        Write-Host "  ✓ Already in system PATH"
    }

    # Create desktop shortcut (optional)
    $createDesktopShortcut = $env:ChocolateyPackageParameters -like '*CreateDesktopShortcut*'
    if ($createDesktopShortcut) {
        Write-Host "Creating desktop shortcut..."
        $desktopPath = [Environment]::GetFolderPath('CommonDesktopDirectory')
        $shortcutPath = Join-Path $desktopPath 'Claude Profile Manager.lnk'
        $exePath = Join-Path $installLocation 'ClaudeProfileManager.Windows.exe'
        
        if (Test-Path $exePath) {
            $shell = New-Object -ComObject WScript.Shell
            $shortcut = $shell.CreateShortcut($shortcutPath)
            $shortcut.TargetPath = $exePath
            $shortcut.WorkingDirectory = $installLocation
            $shortcut.Description = 'Claude Profile Manager - Authentication Profile Management'
            $shortcut.Save()
            Write-Host "  ✓ Desktop shortcut created"
        }
    }

    # Create start menu entry
    Write-Host "Creating start menu entry..."
    $startMenuPath = Join-Path ([Environment]::GetFolderPath('CommonPrograms')) 'Claude Profile Manager'
    if (-not (Test-Path $startMenuPath)) {
        New-Item -ItemType Directory -Path $startMenuPath -Force | Out-Null
    }
    
    $exePath = Join-Path $installLocation 'ClaudeProfileManager.Windows.exe'
    if (Test-Path $exePath) {
        $shortcutPath = Join-Path $startMenuPath 'Claude Profile Manager.lnk'
        $shell = New-Object -ComObject WScript.Shell
        $shortcut = $shell.CreateShortcut($shortcutPath)
        $shortcut.TargetPath = $exePath
        $shortcut.WorkingDirectory = $installLocation
        $shortcut.Description = 'Claude Profile Manager - Authentication Profile Management'
        $shortcut.Save()
        Write-Host "  ✓ Start menu entry created"
        
        # Create PowerShell shortcut
        $psShortcutPath = Join-Path $startMenuPath 'Claude Profile Manager (PowerShell).lnk'
        $psShortcut = $shell.CreateShortcut($psShortcutPath)
        $psShortcut.TargetPath = 'powershell.exe'
        $psShortcut.Arguments = '-NoExit -Command "Import-Module ClaudeProfileManager; Write-Host \"Claude Profile Manager PowerShell Module loaded. Type Get-Help about_ClaudeProfileManager for help.\" -ForegroundColor Green"'
        $psShortcut.WorkingDirectory = $installLocation
        $psShortcut.Description = 'Claude Profile Manager PowerShell Console'
        $psShortcut.Save()
        Write-Host "  ✓ PowerShell console shortcut created"
    }

    # Verify installation
    Write-Host "Verifying installation..."
    $exePath = Join-Path $installLocation 'ClaudeProfileManager.Windows.exe'
    if (Test-Path $exePath) {
        try {
            # Test application execution
            $version = & $exePath --version 2>$null
            if ($LASTEXITCODE -eq 0) {
                Write-Host "  ✓ Application installation verified: $version"
            } else {
                Write-Warning "Application may not be properly installed (exit code: $LASTEXITCODE)"
            }
        } catch {
            Write-Warning "Could not verify application installation: $($_.Exception.Message)"
        }
    }

    # Test PowerShell module
    try {
        Import-Module ClaudeProfileManager -Force -ErrorAction Stop
        $moduleInfo = Get-Module ClaudeProfileManager
        if ($moduleInfo) {
            Write-Host "  ✓ PowerShell module installation verified: v$($moduleInfo.Version)"
            Remove-Module ClaudeProfileManager -Force
        }
    } catch {
        Write-Warning "Could not verify PowerShell module installation: $($_.Exception.Message)"
    }

    # Success message
    Write-Host ""
    Write-Host "✓ Claude Profile Manager installed successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Getting Started:" -ForegroundColor Cyan
    Write-Host "  Command Line: claude-profile-manager --help" -ForegroundColor Gray
    Write-Host "  PowerShell:   Import-Module ClaudeProfileManager" -ForegroundColor Gray
    Write-Host "  Help:         Get-Help about_ClaudeProfileManager" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Quick Commands:" -ForegroundColor Cyan
    Write-Host "  Save profile:    claude-profile-manager save work" -ForegroundColor Gray
    Write-Host "  List profiles:   claude-profile-manager list" -ForegroundColor Gray
    Write-Host "  Switch profile:  claude-profile-manager switch work" -ForegroundColor Gray
    Write-Host "  Current profile: claude-profile-manager current" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Note: Restart your terminal/PowerShell session to use the new PATH." -ForegroundColor Yellow

} catch {
    Write-Error "Installation failed: $($_.Exception.Message)"
    throw
}