#Requires -Version 5.1
<#
.SYNOPSIS
    Uninstalls the ClaudeProfileManager PowerShell module.

.DESCRIPTION
    This script removes the ClaudeProfileManager PowerShell module from the system.
    It can remove installations from both CurrentUser and AllUsers scopes, with
    options to preserve user data and configuration.

.PARAMETER Scope
    Uninstallation scope: CurrentUser, AllUsers, or All. Defaults to CurrentUser.
    AllUsers requires Administrator privileges.

.PARAMETER PreserveData
    Keep user profile data and configuration files during uninstallation.

.PARAMETER Force
    Force uninstallation without confirmation prompts.

.PARAMETER PassThru
    Return uninstallation information after completion.

.EXAMPLE
    .\Uninstall-ClaudeProfileManager.ps1
    
    Uninstalls the module for the current user with confirmation prompts.

.EXAMPLE
    .\Uninstall-ClaudeProfileManager.ps1 -Scope AllUsers -Force
    
    Uninstalls the system-wide module without confirmation.

.EXAMPLE
    .\Uninstall-ClaudeProfileManager.ps1 -Scope All -PreserveData
    
    Uninstalls from all scopes but preserves user profile data.

.NOTES
    - Requires PowerShell 5.1 or newer
    - AllUsers scope requires Administrator privileges
    - User profile data is preserved by default
    - Removes module from PowerShell module paths

#>

[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
param(
    [Parameter(Mandatory = $false)]
    [ValidateSet('CurrentUser', 'AllUsers', 'All')]
    [string]$Scope = 'CurrentUser',
    
    [Parameter(Mandatory = $false)]
    [switch]$PreserveData,
    
    [Parameter(Mandatory = $false)]
    [switch]$Force,
    
    [Parameter(Mandatory = $false)]
    [switch]$PassThru
)

# Set error action preference
$ErrorActionPreference = 'Stop'

# Main uninstallation function
function Uninstall-ClaudeProfileManagerModule {
    param(
        [string]$Scope,
        [bool]$PreserveData,
        [bool]$Force,
        [bool]$PassThru
    )
    
    try {
        Write-Host "Claude Profile Manager PowerShell Module Uninstaller" -ForegroundColor Cyan
        Write-Host "=" * 57 -ForegroundColor Cyan
        Write-Host
        
        $removedLocations = @()
        $scopes = if ($Scope -eq 'All') { @('CurrentUser', 'AllUsers') } else { @($Scope) }
        
        foreach ($currentScope in $scopes) {
            Write-Host "Processing scope: $currentScope" -ForegroundColor Yellow
            
            # Check prerequisites
            if ($currentScope -eq 'AllUsers') {
                Test-AdministratorPrivileges
            }
            
            # Find installed modules
            $installedModules = Find-InstalledModules -Scope $currentScope
            
            if ($installedModules.Count -eq 0) {
                Write-Host "  No ClaudeProfileManager module found in $currentScope scope" -ForegroundColor Gray
                continue
            }
            
            # Remove each installation
            foreach ($moduleInfo in $installedModules) {
                Write-Host "  Found installation: $($moduleInfo.Path)" -ForegroundColor Gray
                
                if ($PSCmdlet.ShouldProcess($moduleInfo.Path, "Remove ClaudeProfileManager Module")) {
                    Remove-ModuleInstallation -ModuleInfo $moduleInfo -Force:$Force
                    $removedLocations += $moduleInfo.Path
                    Write-Host "  ✓ Removed: $($moduleInfo.Path)" -ForegroundColor Green
                }
            }
        }
        
        # Remove module from current session if loaded
        Write-Verbose "Removing module from current session..."
        Remove-Module ClaudeProfileManager -Force -ErrorAction SilentlyContinue
        
        # Clean up user data if requested
        if (-not $PreserveData) {
            Remove-UserData -Force:$Force
        }
        else {
            Write-Host "User profile data preserved (use -PreserveData:`$false to remove)" -ForegroundColor Yellow
        }
        
        Write-Host
        if ($removedLocations.Count -gt 0) {
            Write-Host "✓ ClaudeProfileManager module uninstalled successfully!" -ForegroundColor Green
            Write-Host "  Removed from $($removedLocations.Count) location(s)" -ForegroundColor Green
        }
        else {
            Write-Host "No ClaudeProfileManager installations found to remove" -ForegroundColor Yellow
        }
        
        # Return uninstallation info if requested
        if ($PassThru) {
            return [PSCustomObject]@{
                ModuleName = 'ClaudeProfileManager'
                RemovedFrom = $removedLocations
                Scope = $Scope
                DataPreserved = $PreserveData
                UninstallDate = Get-Date
            }
        }
        
        return $removedLocations.Count -gt 0
    }
    catch {
        Write-Error "Uninstallation failed: $($_.Exception.Message)"
        return $false
    }
}

function Test-AdministratorPrivileges {
    $currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
    $isAdmin = $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    
    if (-not $isAdmin) {
        throw "Administrator privileges are required for AllUsers uninstallation scope"
    }
}

function Find-InstalledModules {
    param([string]$Scope)
    
    $moduleInfo = @()
    $searchPaths = @()
    
    if ($Scope -eq 'CurrentUser') {
        # User module paths
        $userPath1 = Join-Path ([Environment]::GetFolderPath('MyDocuments')) "WindowsPowerShell\Modules\ClaudeProfileManager"
        $userPath2 = Join-Path ([Environment]::GetFolderPath('MyDocuments')) "PowerShell\Modules\ClaudeProfileManager"
        $searchPaths = @($userPath1, $userPath2)
    }
    elseif ($Scope -eq 'AllUsers') {
        # System module paths
        $systemPath1 = Join-Path $env:ProgramFiles "WindowsPowerShell\Modules\ClaudeProfileManager"
        $systemPath2 = Join-Path $env:ProgramFiles "PowerShell\Modules\ClaudeProfileManager"
        $searchPaths = @($systemPath1, $systemPath2)
    }
    
    foreach ($path in $searchPaths) {
        if (Test-Path $path) {
            $manifestPath = Join-Path $path 'ClaudeProfileManager.psd1'
            if (Test-Path $manifestPath) {
                try {
                    $manifest = Import-PowerShellDataFile $manifestPath
                    $moduleInfo += [PSCustomObject]@{
                        Path = $path
                        Version = $manifest.ModuleVersion
                        Scope = $Scope
                    }
                }
                catch {
                    Write-Warning "Invalid module found at $path (corrupt manifest)"
                }
            }
        }
    }
    
    return $moduleInfo
}

function Remove-ModuleInstallation {
    param(
        [PSCustomObject]$ModuleInfo,
        [bool]$Force
    )
    
    if (-not (Test-Path $ModuleInfo.Path)) {
        Write-Verbose "Module path no longer exists: $($ModuleInfo.Path)"
        return
    }
    
    try {
        # Remove the module directory
        Remove-Item -Path $ModuleInfo.Path -Recurse -Force
        Write-Verbose "Removed module directory: $($ModuleInfo.Path)"
    }
    catch {
        if ($Force) {
            Write-Warning "Failed to remove $($ModuleInfo.Path): $($_.Exception.Message)"
        }
        else {
            throw "Failed to remove module installation: $($_.Exception.Message)"
        }
    }
}

function Remove-UserData {
    param([bool]$Force)
    
    $profilePath = Join-Path $env:USERPROFILE '.claude'
    
    if (Test-Path $profilePath) {
        $message = "Remove user profile data at $profilePath"
        
        if ($Force -or $PSCmdlet.ShouldProcess($profilePath, $message)) {
            try {
                Write-Host "Removing user profile data..." -ForegroundColor Yellow
                Remove-Item -Path $profilePath -Recurse -Force
                Write-Host "✓ User profile data removed" -ForegroundColor Green
            }
            catch {
                Write-Warning "Failed to remove user profile data: $($_.Exception.Message)"
                Write-Warning "You may need to manually remove: $profilePath"
            }
        }
    }
    else {
        Write-Verbose "No user profile data found at $profilePath"
    }
}

function Show-UninstallationSummary {
    param([string[]]$RemovedLocations)
    
    if ($RemovedLocations.Count -gt 0) {
        Write-Host
        Write-Host "Uninstallation Summary:" -ForegroundColor Cyan
        foreach ($location in $RemovedLocations) {
            Write-Host "  ✓ $location" -ForegroundColor Green
        }
    }
}

# Confirmation prompt if not forcing
if (-not $Force -and -not $WhatIfPreference) {
    Write-Host "This will uninstall the ClaudeProfileManager PowerShell module." -ForegroundColor Yellow
    
    if (-not $PreserveData) {
        Write-Host "This will also remove all user profile data and configuration." -ForegroundColor Red
        Write-Host "Use -PreserveData to keep user data during uninstallation." -ForegroundColor Yellow
    }
    
    Write-Host
    $response = Read-Host "Continue with uninstallation? (y/N)"
    
    if ($response -notlike 'y*' -and $response -notlike 'Y*') {
        Write-Host "Uninstallation cancelled by user" -ForegroundColor Yellow
        exit 1
    }
}

# Main execution
if ($PSCmdlet.ShouldProcess("ClaudeProfileManager Module", "Uninstall")) {
    $result = Uninstall-ClaudeProfileManagerModule -Scope $Scope -PreserveData:$PreserveData -Force:$Force -PassThru:$PassThru
    
    if ($PassThru -and $result) {
        Write-Output $result
    }
    
    exit $(if ($result -eq $true -or $result -is [PSCustomObject]) { 0 } else { 1 })
}
else {
    Write-Host "Uninstallation cancelled" -ForegroundColor Yellow
    exit 1
}