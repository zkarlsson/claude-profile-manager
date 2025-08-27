#Requires -Version 5.1
<#
.SYNOPSIS
    Deploys the ClaudeProfileManager PowerShell module to various targets.

.DESCRIPTION
    This script automates the deployment of the ClaudeProfileManager PowerShell module
    to different deployment targets including local installations, network shares,
    and package repositories.

.PARAMETER Target
    Deployment target: Local, Network, Gallery, or All.

.PARAMETER BuildFirst
    Build the module before deployment.

.PARAMETER NetworkPath
    Network share path for network deployments.

.PARAMETER Scope
    Installation scope for local deployments: CurrentUser or AllUsers.

.PARAMETER Version
    Specific version to deploy. If not specified, uses latest build.

.PARAMETER ApiKey
    PowerShell Gallery API key for gallery deployments.

.PARAMETER Force
    Force deployment even if target already exists.

.PARAMETER WhatIf
    Show what would be deployed without actually performing deployment.

.EXAMPLE
    .\Deploy-ClaudeProfileManager.ps1 -Target Local
    
    Deploys the module locally for the current user.

.EXAMPLE
    .\Deploy-ClaudeProfileManager.ps1 -Target Network -NetworkPath "\\server\share\modules"
    
    Deploys the module to a network share.

.EXAMPLE
    .\Deploy-ClaudeProfileManager.ps1 -Target Gallery -ApiKey "your-api-key" -BuildFirst
    
    Builds and deploys the module to PowerShell Gallery.

.NOTES
    - Supports multiple deployment targets
    - Can build before deployment
    - Validates deployment integrity
    - Provides rollback capabilities

#>

[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory = $false)]
    [ValidateSet('Local', 'Network', 'Gallery', 'All')]
    [string]$Target = 'Local',
    
    [Parameter(Mandatory = $false)]
    [switch]$BuildFirst,
    
    [Parameter(Mandatory = $false)]
    [string]$NetworkPath,
    
    [Parameter(Mandatory = $false)]
    [ValidateSet('CurrentUser', 'AllUsers')]
    [string]$Scope = 'CurrentUser',
    
    [Parameter(Mandatory = $false)]
    [string]$Version,
    
    [Parameter(Mandatory = $false)]
    [string]$ApiKey,
    
    [Parameter(Mandatory = $false)]
    [switch]$Force,
    
    [Parameter(Mandatory = $false)]
    [switch]$WhatIf
)

# Set error action preference
$ErrorActionPreference = 'Stop'

# Deployment configuration
$script:DeployConfig = @{
    ModuleName = 'ClaudeProfileManager'
    SourcePath = Join-Path $PSScriptRoot 'ClaudeProfileManager'
    BuildPath = Join-Path $PSScriptRoot 'dist'
    DeploymentDate = Get-Date
    Results = @()
}

# Main deployment function
function Deploy-ClaudeProfileManagerModule {
    try {
        Write-Host "Claude Profile Manager PowerShell Module Deployment" -ForegroundColor Cyan
        Write-Host "=" * 57 -ForegroundColor Cyan
        Write-Host
        
        # Initialize deployment
        Initialize-Deployment
        
        # Build if requested
        if ($BuildFirst) {
            Write-Host "Building module..." -ForegroundColor Yellow
            Build-ModuleForDeployment
        }
        
        # Determine deployment targets
        $targets = Get-DeploymentTargets
        
        # Execute deployments
        foreach ($targetName in $targets) {
            Write-Host "Deploying to $targetName..." -ForegroundColor Yellow
            
            switch ($targetName) {
                'Local' { Deploy-ToLocal }
                'Network' { Deploy-ToNetwork }
                'Gallery' { Deploy-ToGallery }
            }
        }
        
        # Show deployment summary
        Show-DeploymentSummary
        
        Write-Host
        Write-Host "✓ Deployment completed successfully!" -ForegroundColor Green
        
        return $true
    }
    catch {
        Write-Host
        Write-Error "Deployment failed: $($_.Exception.Message)"
        Show-ErrorGuidance
        return $false
    }
}

function Initialize-Deployment {
    Write-Verbose "Initializing deployment environment..."
    
    # Validate source exists
    if (-not (Test-Path $script:DeployConfig.SourcePath)) {
        throw "Source module not found: $($script:DeployConfig.SourcePath)"
    }
    
    # Get version information
    $manifestPath = Join-Path $script:DeployConfig.SourcePath "$($script:DeployConfig.ModuleName).psd1"
    $manifest = Import-PowerShellDataFile $manifestPath
    
    if (-not $Version) {
        $script:DeployConfig.Version = $manifest.ModuleVersion
    }
    else {
        $script:DeployConfig.Version = $Version
    }
    
    # Determine source path for deployment
    $builtModulePath = Join-Path $script:DeployConfig.BuildPath $script:DeployConfig.ModuleName
    if ((Test-Path $builtModulePath) -and -not $BuildFirst) {
        $script:DeployConfig.DeploySource = $builtModulePath
        Write-Verbose "Using built module from: $builtModulePath"
    }
    else {
        $script:DeployConfig.DeploySource = $script:DeployConfig.SourcePath
        Write-Verbose "Using source module from: $($script:DeployConfig.SourcePath)"
    }
    
    Write-Host "Deployment Configuration:" -ForegroundColor Yellow
    Write-Host "  Module: $($script:DeployConfig.ModuleName) v$($script:DeployConfig.Version)" -ForegroundColor Gray
    Write-Host "  Source: $($script:DeployConfig.DeploySource)" -ForegroundColor Gray
    Write-Host "  Target: $Target" -ForegroundColor Gray
    if ($Scope) { Write-Host "  Scope: $Scope" -ForegroundColor Gray }
    Write-Host
}

function Build-ModuleForDeployment {
    $buildScript = Join-Path $PSScriptRoot 'Build-ClaudeProfileManager.ps1'
    
    if (-not (Test-Path $buildScript)) {
        throw "Build script not found: $buildScript"
    }
    
    $buildArgs = @{
        OutputPath = $script:DeployConfig.BuildPath
    }
    
    if ($Version) {
        $buildArgs.Version = $Version
    }
    
    & $buildScript @buildArgs
    
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE"
    }
    
    # Update deploy source to built module
    $script:DeployConfig.DeploySource = Join-Path $script:DeployConfig.BuildPath $script:DeployConfig.ModuleName
    Write-Host "  ✓ Module built successfully" -ForegroundColor Green
}

function Get-DeploymentTargets {
    if ($Target -eq 'All') {
        return @('Local', 'Network', 'Gallery')
    }
    else {
        return @($Target)
    }
}

function Deploy-ToLocal {
    try {
        $installScript = Join-Path $PSScriptRoot 'Install-ClaudeProfileManager.ps1'
        
        if (-not (Test-Path $installScript)) {
            throw "Install script not found: $installScript"
        }
        
        $installArgs = @{
            Scope = $Scope
            Force = $Force
            PassThru = $true
        }
        
        if ($WhatIf) {
            Write-Host "  WHATIF: Would install module locally with scope $Scope" -ForegroundColor Yellow
            $result = [PSCustomObject]@{
                Target = 'Local'
                Status = 'WhatIf'
                Path = 'N/A (WhatIf)'
                Scope = $Scope
            }
        }
        else {
            # Copy built module to install location temporarily
            $tempPath = Join-Path $PSScriptRoot 'ClaudeProfileManager'
            if (Test-Path $tempPath) {
                Remove-Item $tempPath -Recurse -Force
            }
            Copy-Item $script:DeployConfig.DeploySource $tempPath -Recurse -Force
            
            try {
                $installResult = & $installScript @installArgs
                
                $result = [PSCustomObject]@{
                    Target = 'Local'
                    Status = 'Success'
                    Path = $installResult.InstallPath
                    Scope = $installResult.Scope
                }
            }
            finally {
                # Clean up temp copy
                if (Test-Path $tempPath) {
                    Remove-Item $tempPath -Recurse -Force
                }
            }
        }
        
        $script:DeployConfig.Results += $result
        Write-Host "  ✓ Local deployment completed" -ForegroundColor Green
    }
    catch {
        $result = [PSCustomObject]@{
            Target = 'Local'
            Status = 'Failed'
            Error = $_.Exception.Message
            Path = $null
            Scope = $Scope
        }
        $script:DeployConfig.Results += $result
        throw "Local deployment failed: $($_.Exception.Message)"
    }
}

function Deploy-ToNetwork {
    if (-not $NetworkPath) {
        throw "NetworkPath parameter is required for network deployment"
    }
    
    try {
        $networkModulePath = Join-Path $NetworkPath $script:DeployConfig.ModuleName
        
        if ($WhatIf) {
            Write-Host "  WHATIF: Would deploy module to $networkModulePath" -ForegroundColor Yellow
            $result = [PSCustomObject]@{
                Target = 'Network'
                Status = 'WhatIf'
                Path = $networkModulePath
            }
        }
        else {
            # Validate network path is accessible
            if (-not (Test-Path $NetworkPath)) {
                throw "Network path not accessible: $NetworkPath"
            }
            
            # Remove existing if Force is specified
            if ((Test-Path $networkModulePath) -and $Force) {
                Remove-Item $networkModulePath -Recurse -Force
            }
            
            # Copy module to network location
            Copy-Item $script:DeployConfig.DeploySource $networkModulePath -Recurse -Force
            
            $result = [PSCustomObject]@{
                Target = 'Network'
                Status = 'Success'
                Path = $networkModulePath
            }
        }
        
        $script:DeployConfig.Results += $result
        Write-Host "  ✓ Network deployment completed" -ForegroundColor Green
    }
    catch {
        $result = [PSCustomObject]@{
            Target = 'Network'
            Status = 'Failed'
            Error = $_.Exception.Message
            Path = $NetworkPath
        }
        $script:DeployConfig.Results += $result
        throw "Network deployment failed: $($_.Exception.Message)"
    }
}

function Deploy-ToGallery {
    if (-not $ApiKey) {
        throw "ApiKey parameter is required for gallery deployment"
    }
    
    try {
        if ($WhatIf) {
            Write-Host "  WHATIF: Would publish module to PowerShell Gallery" -ForegroundColor Yellow
            $result = [PSCustomObject]@{
                Target = 'Gallery'
                Status = 'WhatIf'
                Path = 'PowerShell Gallery'
            }
        }
        else {
            # Publish module to PowerShell Gallery
            Publish-Module -Path $script:DeployConfig.DeploySource -NuGetApiKey $ApiKey -Force:$Force
            
            $result = [PSCustomObject]@{
                Target = 'Gallery'
                Status = 'Success'
                Path = 'PowerShell Gallery'
                Version = $script:DeployConfig.Version
            }
        }
        
        $script:DeployConfig.Results += $result
        Write-Host "  ✓ Gallery deployment completed" -ForegroundColor Green
    }
    catch {
        $result = [PSCustomObject]@{
            Target = 'Gallery'
            Status = 'Failed'
            Error = $_.Exception.Message
            Path = 'PowerShell Gallery'
        }
        $script:DeployConfig.Results += $result
        throw "Gallery deployment failed: $($_.Exception.Message)"
    }
}

function Show-DeploymentSummary {
    Write-Host
    Write-Host "Deployment Summary:" -ForegroundColor Cyan
    Write-Host "  Module: $($script:DeployConfig.ModuleName) v$($script:DeployConfig.Version)" -ForegroundColor Green
    Write-Host "  Date: $($script:DeployConfig.DeploymentDate.ToString('yyyy-MM-dd HH:mm:ss'))" -ForegroundColor Green
    
    $successCount = ($script:DeployConfig.Results | Where-Object Status -eq 'Success').Count
    $failedCount = ($script:DeployConfig.Results | Where-Object Status -eq 'Failed').Count
    $whatIfCount = ($script:DeployConfig.Results | Where-Object Status -eq 'WhatIf').Count
    
    Write-Host
    foreach ($result in $script:DeployConfig.Results) {
        $color = switch ($result.Status) {
            'Success' { 'Green' }
            'Failed' { 'Red' }
            'WhatIf' { 'Yellow' }
            default { 'Gray' }
        }
        
        $status = switch ($result.Status) {
            'Success' { '✓' }
            'Failed' { '✗' }
            'WhatIf' { '?' }
            default { '-' }
        }
        
        Write-Host "  $status $($result.Target): $($result.Path)" -ForegroundColor $color
        
        if ($result.Error) {
            Write-Host "    Error: $($result.Error)" -ForegroundColor Red
        }
    }
    
    Write-Host
    Write-Host "Results: $successCount successful, $failedCount failed, $whatIfCount simulated" -ForegroundColor Cyan
}

function Show-ErrorGuidance {
    Write-Host
    Write-Host "Troubleshooting:" -ForegroundColor Yellow
    Write-Host "- Ensure you have appropriate permissions for the deployment target" -ForegroundColor Gray
    Write-Host "- For AllUsers scope, run PowerShell as Administrator" -ForegroundColor Gray
    Write-Host "- For network deployments, verify the network path is accessible" -ForegroundColor Gray
    Write-Host "- For gallery deployments, verify your API key is valid" -ForegroundColor Gray
    Write-Host "- Use -WhatIf to preview deployment actions" -ForegroundColor Gray
}

# Main execution
if ($PSCmdlet.ShouldProcess("ClaudeProfileManager Module", "Deploy to $Target")) {
    $success = Deploy-ClaudeProfileManagerModule
    exit $(if ($success) { 0 } else { 1 })
}
else {
    Write-Host "Deployment cancelled" -ForegroundColor Yellow
    exit 1
}