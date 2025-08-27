#Requires -Version 3.0

$ErrorActionPreference = 'Stop'

# Package information
$packageName = 'claude-profile-manager'

# Installation paths
$installLocation = Join-Path $env:ProgramFiles 'ClaudeProfileManager'

Write-Host "Uninstalling $packageName" -ForegroundColor Yellow

try {
    # Remove from PowerShell session if loaded
    try {
        Remove-Module ClaudeProfileManager -Force -ErrorAction SilentlyContinue
        Write-Host "  ✓ Removed from current PowerShell session"
    } catch {
        # Ignore errors - module might not be loaded
    }

    # Remove PowerShell module
    Write-Host "Removing PowerShell module..."
    $moduleLocations = @()
    
    # PowerShell Core path
    $psCorePath = Join-Path ([Environment]::GetFolderPath('ProgramFiles')) 'PowerShell\Modules\ClaudeProfileManager'
    if (Test-Path $psCorePath) {
        $moduleLocations += $psCorePath
    }
    
    # Windows PowerShell path
    $winPsPath = Join-Path ([Environment]::GetFolderPath('ProgramFiles')) 'WindowsPowerShell\Modules\ClaudeProfileManager'
    if (Test-Path $winPsPath) {
        $moduleLocations += $winPsPath
    }
    
    foreach ($modulePath in $moduleLocations) {
        try {
            Remove-Item -Path $modulePath -Recurse -Force
            Write-Host "  ✓ Removed PowerShell module from $modulePath"
        } catch {
            Write-Warning "Could not remove PowerShell module from $modulePath`: $($_.Exception.Message)"
        }
    }

    # Remove from PATH
    Write-Host "Removing from system PATH..."
    $currentPath = [Environment]::GetEnvironmentVariable('PATH', 'Machine')
    if ($currentPath -like "*$installLocation*") {
        # Remove the path (handle different separator scenarios)
        $newPath = $currentPath -replace [regex]::Escape(";$installLocation"), "" -replace [regex]::Escape("$installLocation;"), "" -replace [regex]::Escape("$installLocation"), ""
        [Environment]::SetEnvironmentVariable('PATH', $newPath, 'Machine')
        Write-Host "  ✓ Removed from system PATH"
    } else {
        Write-Host "  ✓ Not found in system PATH"
    }

    # Remove start menu entries
    Write-Host "Removing start menu entries..."
    $startMenuPath = Join-Path ([Environment]::GetFolderPath('CommonPrograms')) 'Claude Profile Manager'
    if (Test-Path $startMenuPath) {
        try {
            Remove-Item -Path $startMenuPath -Recurse -Force
            Write-Host "  ✓ Start menu entries removed"
        } catch {
            Write-Warning "Could not remove start menu entries: $($_.Exception.Message)"
        }
    } else {
        Write-Host "  ✓ No start menu entries found"
    }

    # Remove desktop shortcut if it exists
    Write-Host "Removing desktop shortcut..."
    $desktopPath = [Environment]::GetFolderPath('CommonDesktopDirectory')
    $shortcutPath = Join-Path $desktopPath 'Claude Profile Manager.lnk'
    if (Test-Path $shortcutPath) {
        try {
            Remove-Item -Path $shortcutPath -Force
            Write-Host "  ✓ Desktop shortcut removed"
        } catch {
            Write-Warning "Could not remove desktop shortcut: $($_.Exception.Message)"
        }
    } else {
        Write-Host "  ✓ No desktop shortcut found"
    }

    # Remove application files
    Write-Host "Removing application files..."
    if (Test-Path $installLocation) {
        try {
            # Stop any running processes first
            $processName = 'ClaudeProfileManager.Windows'
            $runningProcesses = Get-Process -Name $processName -ErrorAction SilentlyContinue
            if ($runningProcesses) {
                Write-Host "  Stopping running processes..."
                $runningProcesses | Stop-Process -Force
                Start-Sleep -Seconds 2
            }

            Remove-Item -Path $installLocation -Recurse -Force
            Write-Host "  ✓ Application files removed from $installLocation"
        } catch {
            Write-Warning "Could not remove all application files from $installLocation`: $($_.Exception.Message)"
            Write-Warning "You may need to manually remove the directory after restarting."
        }
    } else {
        Write-Host "  ✓ Application directory not found"
    }

    # Ask about user data removal
    $removeUserData = $false
    if ([Environment]::UserInteractive) {
        Write-Host ""
        Write-Host "User profile data is stored in:" -ForegroundColor Cyan
        Write-Host "  %USERPROFILE%\.claude\profiles" -ForegroundColor Gray
        Write-Host ""
        $response = Read-Host "Remove user profile data? (y/N)"
        $removeUserData = $response -match '^[Yy]'
    } else {
        # Check for package parameters
        $removeUserData = $env:ChocolateyPackageParameters -like '*RemoveUserData*'
    }

    if ($removeUserData) {
        Write-Host "Removing user profile data..."
        $userProfilePath = Join-Path $env:USERPROFILE '.claude'
        if (Test-Path $userProfilePath) {
            try {
                Remove-Item -Path $userProfilePath -Recurse -Force
                Write-Host "  ✓ User profile data removed"
            } catch {
                Write-Warning "Could not remove user profile data: $($_.Exception.Message)"
            }
        } else {
            Write-Host "  ✓ No user profile data found"
        }
    } else {
        Write-Host "  ✓ User profile data preserved"
    }

    # Success message
    Write-Host ""
    Write-Host "✓ Claude Profile Manager uninstalled successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Notes:" -ForegroundColor Cyan
    Write-Host "  - Restart your terminal/PowerShell session to clear PATH changes" -ForegroundColor Gray
    Write-Host "  - User profile data was $(if ($removeUserData) { 'removed' } else { 'preserved' })" -ForegroundColor Gray
    
    if (-not $removeUserData) {
        Write-Host "  - To remove user data manually: Remove-Item '$env:USERPROFILE\.claude' -Recurse -Force" -ForegroundColor Gray
    }

} catch {
    Write-Error "Uninstallation failed: $($_.Exception.Message)"
    
    # Provide manual cleanup instructions
    Write-Host ""
    Write-Host "Manual cleanup may be required:" -ForegroundColor Yellow
    Write-Host "  1. Remove directory: $installLocation" -ForegroundColor Gray
    Write-Host "  2. Remove from PATH: Control Panel > System > Advanced > Environment Variables" -ForegroundColor Gray
    Write-Host "  3. Remove PowerShell module: Uninstall-Module ClaudeProfileManager" -ForegroundColor Gray
    Write-Host "  4. Remove start menu: $([Environment]::GetFolderPath('CommonPrograms'))\Claude Profile Manager" -ForegroundColor Gray
    
    throw
}