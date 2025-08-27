#Requires -Version 5.1
<#
.SYNOPSIS
    Installs the ClaudeProfileManager PowerShell module.

.DESCRIPTION
    This script installs the ClaudeProfileManager PowerShell module for managing
    Claude Code CLI authentication profiles on Windows. It handles both user-scope
    and system-wide installations with proper error handling and validation.

.PARAMETER Scope
    Installation scope: CurrentUser or AllUsers. Defaults to CurrentUser.
    AllUsers requires Administrator privileges.

.PARAMETER Force
    Force installation even if the module is already installed.

.PARAMETER PassThru
    Return installation information after completion.

.PARAMETER Verbose
    Show detailed installation progress.

.EXAMPLE
    .\Install-ClaudeProfileManager.ps1
    
    Installs the module for the current user.

.EXAMPLE
    .\Install-ClaudeProfileManager.ps1 -Scope AllUsers -Force
    
    Installs the module system-wide, overwriting any existing installation.

.EXAMPLE
    $result = .\Install-ClaudeProfileManager.ps1 -PassThru
    Write-Host "Installed to: $($result.InstallPath)"
    
    Installs the module and returns installation details.

.NOTES
    - Requires PowerShell 5.1 or newer
    - AllUsers scope requires Administrator privileges
    - Automatically creates module directories if they don't exist
    - Validates module structure before installation
    - Creates PowerShell module path entries if needed

#>

[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory = $false)]
    [ValidateSet('CurrentUser', 'AllUsers')]
    [string]$Scope = 'CurrentUser',
    
    [Parameter(Mandatory = $false)]
    [switch]$Force,
    
    [Parameter(Mandatory = $false)]
    [switch]$PassThru,
    
    [Parameter(Mandatory = $false)]
    [switch]$Verbose
)

# Set error action preference
$ErrorActionPreference = 'Stop'

# Enable verbose output if requested
if ($Verbose) {
    $VerbosePreference = 'Continue'
}

# Main installation function
function Install-ClaudeProfileManagerModule {
    param(
        [string]$Scope,
        [bool]$Force,
        [bool]$PassThru
    )
    
    try {
        Write-Host "Claude Profile Manager PowerShell Module Installer" -ForegroundColor Cyan
        Write-Host "=" * 55 -ForegroundColor Cyan
        Write-Host
        
        # Validate prerequisites
        Write-Verbose "Validating installation prerequisites..."
        Test-InstallationPrerequisites -Scope $Scope
        
        # Get installation paths
        $installPath = Get-ModuleInstallPath -Scope $Scope
        $sourcePath = Join-Path $PSScriptRoot "ClaudeProfileManager"
        
        Write-Host "Installation Details:" -ForegroundColor Yellow
        Write-Host "  Source Path: $sourcePath" -ForegroundColor Gray
        Write-Host "  Install Path: $installPath" -ForegroundColor Gray
        Write-Host "  Scope: $Scope" -ForegroundColor Gray
        Write-Host
        
        # Validate source module
        Write-Verbose "Validating source module structure..."
        Test-SourceModuleStructure -SourcePath $sourcePath
        
        # Check if module is already installed
        $existingModule = Get-Module -Name ClaudeProfileManager -ListAvailable -ErrorAction SilentlyContinue
        if ($existingModule -and -not $Force) {
            Write-Warning "ClaudeProfileManager module is already installed at:"
            $existingModule | ForEach-Object { Write-Warning "  $($_.ModuleBase)" }
            Write-Warning "Use -Force to overwrite existing installation"
            return $false
        }
        
        # Create installation directory
        Write-Verbose "Creating installation directory..."
        if (-not (Test-Path $installPath)) {
            New-Item -ItemType Directory -Path $installPath -Force | Out-Null
            Write-Verbose "Created directory: $installPath"
        }
        
        # Copy module files
        Write-Host "Installing module files..." -ForegroundColor Yellow
        Copy-ModuleFiles -SourcePath $sourcePath -DestinationPath $installPath -Force:$Force
        
        # Validate installation
        Write-Verbose "Validating installation..."
        Test-ModuleInstallation -InstallPath $installPath
        
        # Update PowerShell module path if needed
        Update-PSModulePath -Scope $Scope
        
        Write-Host
        Write-Host "✓ Claude Profile Manager module installed successfully!" -ForegroundColor Green
        Write-Host "  Location: $installPath" -ForegroundColor Green
        
        # Test module import
        Write-Verbose "Testing module import..."
        try {
            Import-Module ClaudeProfileManager -Force -ErrorAction Stop
            Write-Host "✓ Module import test successful" -ForegroundColor Green
            
            # Show available commands
            $commands = Get-Command -Module ClaudeProfileManager
            Write-Host "  Available commands: $($commands.Count)" -ForegroundColor Green
            
            # Remove the test import
            Remove-Module ClaudeProfileManager -ErrorAction SilentlyContinue
        }
        catch {
            Write-Warning "Module was installed but import test failed: $($_.Exception.Message)"
        }
        
        Write-Host
        Write-Host "Getting Started:" -ForegroundColor Cyan
        Write-Host "  1. Import-Module ClaudeProfileManager" -ForegroundColor Gray
        Write-Host "  2. Get-Help about_ClaudeProfileManager" -ForegroundColor Gray
        Write-Host "  3. Save-ClaudeProfile -Name 'work'" -ForegroundColor Gray
        
        # Return installation info if requested
        if ($PassThru) {
            return [PSCustomObject]@{
                ModuleName = 'ClaudeProfileManager'
                Version = (Import-PowerShellDataFile (Join-Path $installPath 'ClaudeProfileManager.psd1')).ModuleVersion
                InstallPath = $installPath
                Scope = $Scope
                InstallDate = Get-Date
                CommandCount = $commands.Count
            }
        }
        
        return $true
    }
    catch {
        Write-Error "Installation failed: $($_.Exception.Message)"
        return $false
    }
}

function Test-InstallationPrerequisites {
    param([string]$Scope)
    
    # Check PowerShell version
    if ($PSVersionTable.PSVersion.Major -lt 5) {
        throw "PowerShell 5.1 or newer is required. Current version: $($PSVersionTable.PSVersion)"
    }
    
    # Check for Administrator privileges if AllUsers scope
    if ($Scope -eq 'AllUsers') {
        $currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
        $isAdmin = $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
        
        if (-not $isAdmin) {
            throw "Administrator privileges are required for AllUsers installation scope"
        }
    }
    
    # Check if Claude Code CLI is available
    try {
        $cliPath = Get-Command claude -ErrorAction Stop
        Write-Verbose "Found Claude Code CLI at: $($cliPath.Source)"
    }
    catch {
        Write-Warning "Claude Code CLI not found in PATH. The module will still install but may not function properly without the CLI."
    }
}

function Get-ModuleInstallPath {
    param([string]$Scope)
    
    $psModulePath = $env:PSModulePath -split [System.IO.Path]::PathSeparator
    
    if ($Scope -eq 'AllUsers') {
        # Find system-wide module path
        $systemPath = $psModulePath | Where-Object { 
            $_ -like "*Program Files*" -or $_ -like "*ProgramFiles*" 
        } | Select-Object -First 1
        
        if (-not $systemPath) {
            $systemPath = Join-Path $env:ProgramFiles "WindowsPowerShell\Modules"
        }
        
        return Join-Path $systemPath "ClaudeProfileManager"
    }
    else {
        # Current user module path
        $userPath = Join-Path ([Environment]::GetFolderPath('MyDocuments')) "WindowsPowerShell\Modules"
        
        # For PowerShell Core, use different path
        if ($PSVersionTable.PSVersion.Major -ge 6) {
            $userPath = Join-Path ([Environment]::GetFolderPath('MyDocuments')) "PowerShell\Modules"
        }
        
        return Join-Path $userPath "ClaudeProfileManager"
    }
}

function Test-SourceModuleStructure {
    param([string]$SourcePath)
    
    if (-not (Test-Path $SourcePath)) {
        throw "Source module directory not found: $SourcePath"
    }
    
    $requiredFiles = @(
        'ClaudeProfileManager.psd1',
        'ClaudeProfileManager.psm1',
        'ClaudeProfileManager.Format.ps1xml'
    )
    
    foreach ($file in $requiredFiles) {
        $filePath = Join-Path $SourcePath $file
        if (-not (Test-Path $filePath)) {
            throw "Required module file not found: $file"
        }
    }
    
    # Validate manifest
    try {
        $manifest = Import-PowerShellDataFile (Join-Path $SourcePath 'ClaudeProfileManager.psd1')
        Write-Verbose "Module version: $($manifest.ModuleVersion)"
    }
    catch {
        throw "Invalid module manifest: $($_.Exception.Message)"
    }
}

function Copy-ModuleFiles {
    param(
        [string]$SourcePath,
        [string]$DestinationPath, 
        [bool]$Force
    )
    
    # Remove existing installation if Force is specified
    if ($Force -and (Test-Path $DestinationPath)) {
        Write-Verbose "Removing existing installation..."
        Remove-Item $DestinationPath -Recurse -Force
    }
    
    # Copy module files
    Write-Verbose "Copying module files..."
    Copy-Item -Path $SourcePath -Destination $DestinationPath -Recurse -Force
    
    Write-Verbose "Module files copied successfully"
}

function Test-ModuleInstallation {
    param([string]$InstallPath)
    
    $manifestPath = Join-Path $InstallPath 'ClaudeProfileManager.psd1'
    
    if (-not (Test-Path $manifestPath)) {
        throw "Installation validation failed: Module manifest not found"
    }
    
    try {
        $manifest = Import-PowerShellDataFile $manifestPath
        Write-Verbose "Installation validated: Module version $($manifest.ModuleVersion)"
    }
    catch {
        throw "Installation validation failed: $($_.Exception.Message)"
    }
}

function Update-PSModulePath {
    param([string]$Scope)
    
    $currentPaths = $env:PSModulePath -split [System.IO.Path]::PathSeparator
    $moduleBasePath = Split-Path (Get-ModuleInstallPath -Scope $Scope) -Parent
    
    if ($moduleBasePath -notin $currentPaths) {
        Write-Verbose "Adding module path to PSModulePath: $moduleBasePath"
        
        if ($Scope -eq 'AllUsers') {
            # Update system environment variable
            $systemPath = [Environment]::GetEnvironmentVariable('PSModulePath', 'Machine')
            if ($systemPath -notlike "*$moduleBasePath*") {
                [Environment]::SetEnvironmentVariable('PSModulePath', 
                    "$systemPath;$moduleBasePath", 'Machine')
            }
        }
        else {
            # Update user environment variable
            $userPath = [Environment]::GetEnvironmentVariable('PSModulePath', 'User')
            if ($userPath -notlike "*$moduleBasePath*") {
                [Environment]::SetEnvironmentVariable('PSModulePath', 
                    "$userPath;$moduleBasePath", 'User')
            }
        }
        
        # Update current session
        $env:PSModulePath = "$env:PSModulePath;$moduleBasePath"
    }
}

# Main execution
if ($PSCmdlet.ShouldProcess("ClaudeProfileManager Module", "Install")) {
    $result = Install-ClaudeProfileManagerModule -Scope $Scope -Force:$Force -PassThru:$PassThru
    
    if ($PassThru -and $result) {
        Write-Output $result
    }
    
    exit $(if ($result -eq $true -or $result -is [PSCustomObject]) { 0 } else { 1 })
}
else {
    Write-Host "Installation cancelled by user" -ForegroundColor Yellow
    exit 1
}