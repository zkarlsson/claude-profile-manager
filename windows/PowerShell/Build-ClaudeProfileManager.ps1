#Requires -Version 5.1
<#
.SYNOPSIS
    Builds and packages the ClaudeProfileManager PowerShell module.

.DESCRIPTION
    This script validates, builds, and packages the ClaudeProfileManager PowerShell 
    module for distribution. It performs comprehensive validation, creates distribution
    packages, and optionally publishes to PowerShell Gallery.

.PARAMETER OutputPath
    Directory where the build output will be created. Defaults to .\dist.

.PARAMETER Version
    Override the module version. If not specified, uses version from manifest.

.PARAMETER SkipTests
    Skip running tests during the build process.

.PARAMETER CreateZip
    Create a ZIP archive of the module for distribution.

.PARAMETER Publish
    Publish the module to PowerShell Gallery (requires API key).

.PARAMETER ApiKey
    PowerShell Gallery API key for publishing.

.PARAMETER WhatIf
    Show what would be done without actually performing the build.

.EXAMPLE
    .\Build-ClaudeProfileManager.ps1
    
    Builds the module with default settings.

.EXAMPLE
    .\Build-ClaudeProfileManager.ps1 -OutputPath "C:\Builds" -CreateZip
    
    Builds the module to a custom location and creates a ZIP archive.

.EXAMPLE
    .\Build-ClaudeProfileManager.ps1 -Publish -ApiKey "your-api-key"
    
    Builds and publishes the module to PowerShell Gallery.

.NOTES
    - Validates module structure and manifest
    - Runs tests if not skipped
    - Creates clean distribution package
    - Optionally publishes to PowerShell Gallery

#>

[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory = $false)]
    [string]$OutputPath = (Join-Path $PSScriptRoot "dist"),
    
    [Parameter(Mandatory = $false)]
    [string]$Version,
    
    [Parameter(Mandatory = $false)]
    [switch]$SkipTests,
    
    [Parameter(Mandatory = $false)]
    [switch]$CreateZip,
    
    [Parameter(Mandatory = $false)]
    [switch]$Publish,
    
    [Parameter(Mandatory = $false)]
    [string]$ApiKey,
    
    [Parameter(Mandatory = $false)]
    [switch]$WhatIf
)

# Set error action preference
$ErrorActionPreference = 'Stop'

# Build configuration
$script:BuildConfig = @{
    ModuleName = 'ClaudeProfileManager'
    SourcePath = Join-Path $PSScriptRoot 'ClaudeProfileManager'
    OutputPath = $OutputPath
    Version = $Version
    BuildDate = Get-Date
}

# Main build function
function Build-ClaudeProfileManagerModule {
    try {
        Write-Host "Claude Profile Manager PowerShell Module Builder" -ForegroundColor Cyan
        Write-Host "=" * 53 -ForegroundColor Cyan
        Write-Host
        
        # Initialize build
        Initialize-Build
        
        # Validate source
        Write-Host "Validating source module..." -ForegroundColor Yellow
        Test-SourceModule
        
        # Run tests if not skipped
        if (-not $SkipTests) {
            Write-Host "Running tests..." -ForegroundColor Yellow
            Invoke-ModuleTests
        }
        else {
            Write-Host "Skipping tests (as requested)" -ForegroundColor Yellow
        }
        
        # Build module
        Write-Host "Building module..." -ForegroundColor Yellow
        Build-Module
        
        # Create ZIP if requested
        if ($CreateZip) {
            Write-Host "Creating ZIP archive..." -ForegroundColor Yellow
            New-ModuleZip
        }
        
        # Publish if requested
        if ($Publish) {
            Write-Host "Publishing module..." -ForegroundColor Yellow
            Publish-Module
        }
        
        # Show build summary
        Show-BuildSummary
        
        Write-Host
        Write-Host "✓ Build completed successfully!" -ForegroundColor Green
        
        return $true
    }
    catch {
        Write-Host
        Write-Error "Build failed: $($_.Exception.Message)"
        return $false
    }
}

function Initialize-Build {
    Write-Verbose "Initializing build environment..."
    
    # Validate source path
    if (-not (Test-Path $script:BuildConfig.SourcePath)) {
        throw "Source module not found: $($script:BuildConfig.SourcePath)"
    }
    
    # Load and validate manifest
    $manifestPath = Join-Path $script:BuildConfig.SourcePath "$($script:BuildConfig.ModuleName).psd1"
    if (-not (Test-Path $manifestPath)) {
        throw "Module manifest not found: $manifestPath"
    }
    
    $manifest = Import-PowerShellDataFile $manifestPath
    $script:BuildConfig.Manifest = $manifest
    
    # Use version from manifest if not overridden
    if (-not $script:BuildConfig.Version) {
        $script:BuildConfig.Version = $manifest.ModuleVersion
    }
    
    # Create output directory
    if (-not (Test-Path $script:BuildConfig.OutputPath)) {
        New-Item -ItemType Directory -Path $script:BuildConfig.OutputPath -Force | Out-Null
    }
    
    $script:BuildConfig.ModuleOutputPath = Join-Path $script:BuildConfig.OutputPath $script:BuildConfig.ModuleName
    
    Write-Host "Build Configuration:" -ForegroundColor Yellow
    Write-Host "  Module Name: $($script:BuildConfig.ModuleName)" -ForegroundColor Gray
    Write-Host "  Version: $($script:BuildConfig.Version)" -ForegroundColor Gray
    Write-Host "  Source: $($script:BuildConfig.SourcePath)" -ForegroundColor Gray
    Write-Host "  Output: $($script:BuildConfig.ModuleOutputPath)" -ForegroundColor Gray
    Write-Host
}

function Test-SourceModule {
    $manifestPath = Join-Path $script:BuildConfig.SourcePath "$($script:BuildConfig.ModuleName).psd1"
    
    # Test manifest syntax
    try {
        Test-ModuleManifest $manifestPath -ErrorAction Stop | Out-Null
        Write-Verbose "✓ Module manifest syntax is valid"
    }
    catch {
        throw "Module manifest validation failed: $($_.Exception.Message)"
    }
    
    # Validate required files
    $requiredFiles = @(
        "$($script:BuildConfig.ModuleName).psd1",
        "$($script:BuildConfig.ModuleName).psm1",
        "$($script:BuildConfig.ModuleName).Format.ps1xml"
    )
    
    foreach ($file in $requiredFiles) {
        $filePath = Join-Path $script:BuildConfig.SourcePath $file
        if (-not (Test-Path $filePath)) {
            throw "Required file not found: $file"
        }
    }
    
    # Validate public functions exist
    $publicPath = Join-Path $script:BuildConfig.SourcePath "Public"
    if (Test-Path $publicPath) {
        $publicFunctions = Get-ChildItem $publicPath -Filter "*.ps1"
        $exportedFunctions = $script:BuildConfig.Manifest.FunctionsToExport
        
        Write-Verbose "Found $($publicFunctions.Count) public functions, $($exportedFunctions.Count) exported"
    }
    
    Write-Verbose "✓ Source module validation completed"
}

function Invoke-ModuleTests {
    $testPath = Join-Path $PSScriptRoot "Tests"
    
    if (-not (Test-Path $testPath)) {
        Write-Warning "No tests directory found at $testPath - skipping tests"
        return
    }
    
    # Check if Pester is available
    try {
        Import-Module Pester -Force -ErrorAction Stop
        Write-Verbose "Using Pester for testing"
    }
    catch {
        Write-Warning "Pester module not found - skipping tests"
        return
    }
    
    # Run tests
    $testResults = Invoke-Pester -Path $testPath -PassThru -Quiet
    
    if ($testResults.FailedCount -gt 0) {
        throw "Tests failed: $($testResults.FailedCount) out of $($testResults.TotalCount) tests failed"
    }
    
    Write-Host "  ✓ All tests passed ($($testResults.TotalCount) tests)" -ForegroundColor Green
}

function Build-Module {
    # Remove existing build if it exists
    if (Test-Path $script:BuildConfig.ModuleOutputPath) {
        Remove-Item $script:BuildConfig.ModuleOutputPath -Recurse -Force
    }
    
    # Copy source to output
    Copy-Item -Path $script:BuildConfig.SourcePath -Destination $script:BuildConfig.ModuleOutputPath -Recurse -Force
    
    # Update version if overridden
    if ($script:BuildConfig.Version -ne $script:BuildConfig.Manifest.ModuleVersion) {
        Update-ModuleVersion
    }
    
    # Add build metadata
    Add-BuildMetadata
    
    # Validate built module
    $builtManifest = Join-Path $script:BuildConfig.ModuleOutputPath "$($script:BuildConfig.ModuleName).psd1"
    Test-ModuleManifest $builtManifest -ErrorAction Stop | Out-Null
    
    Write-Host "  ✓ Module built successfully" -ForegroundColor Green
}

function Update-ModuleVersion {
    $manifestPath = Join-Path $script:BuildConfig.ModuleOutputPath "$($script:BuildConfig.ModuleName).psd1"
    
    # Read manifest content
    $manifestContent = Get-Content $manifestPath -Raw
    
    # Update version
    $manifestContent = $manifestContent -replace "ModuleVersion = '[^']*'", "ModuleVersion = '$($script:BuildConfig.Version)'"
    
    # Write back
    Set-Content -Path $manifestPath -Value $manifestContent -Encoding UTF8
    
    Write-Verbose "Updated module version to $($script:BuildConfig.Version)"
}

function Add-BuildMetadata {
    $metadataPath = Join-Path $script:BuildConfig.ModuleOutputPath "build-info.json"
    
    $buildInfo = @{
        BuildDate = $script:BuildConfig.BuildDate.ToString('o')
        Version = $script:BuildConfig.Version
        PowerShellVersion = $PSVersionTable.PSVersion.ToString()
        Platform = $PSVersionTable.Platform
        OS = $PSVersionTable.OS
        Builder = [Environment]::UserName
        Machine = [Environment]::MachineName
    }
    
    $buildInfo | ConvertTo-Json -Depth 2 | Set-Content $metadataPath -Encoding UTF8
    Write-Verbose "Added build metadata to $metadataPath"
}

function New-ModuleZip {
    $zipPath = Join-Path $script:BuildConfig.OutputPath "$($script:BuildConfig.ModuleName)-$($script:BuildConfig.Version).zip"
    
    # Remove existing ZIP
    if (Test-Path $zipPath) {
        Remove-Item $zipPath -Force
    }
    
    # Create ZIP archive
    Compress-Archive -Path $script:BuildConfig.ModuleOutputPath -DestinationPath $zipPath -Force
    
    $zipSize = (Get-Item $zipPath).Length / 1KB
    Write-Host "  ✓ ZIP archive created: $zipPath ($([Math]::Round($zipSize, 1)) KB)" -ForegroundColor Green
}

function Publish-Module {
    if (-not $ApiKey) {
        throw "API key is required for publishing. Use -ApiKey parameter."
    }
    
    if ($WhatIf) {
        Write-Host "  WHATIF: Would publish module to PowerShell Gallery" -ForegroundColor Yellow
        return
    }
    
    try {
        Publish-Module -Path $script:BuildConfig.ModuleOutputPath -NuGetApiKey $ApiKey -Force
        Write-Host "  ✓ Module published to PowerShell Gallery" -ForegroundColor Green
    }
    catch {
        throw "Publishing failed: $($_.Exception.Message)"
    }
}

function Show-BuildSummary {
    Write-Host
    Write-Host "Build Summary:" -ForegroundColor Cyan
    Write-Host "  Module: $($script:BuildConfig.ModuleName) v$($script:BuildConfig.Version)" -ForegroundColor Green
    Write-Host "  Output: $($script:BuildConfig.ModuleOutputPath)" -ForegroundColor Green
    Write-Host "  Build Date: $($script:BuildConfig.BuildDate.ToString('yyyy-MM-dd HH:mm:ss'))" -ForegroundColor Green
    
    if ($CreateZip) {
        $zipPath = Join-Path $script:BuildConfig.OutputPath "$($script:BuildConfig.ModuleName)-$($script:BuildConfig.Version).zip"
        if (Test-Path $zipPath) {
            Write-Host "  ZIP Archive: $zipPath" -ForegroundColor Green
        }
    }
    
    if ($Publish -and -not $WhatIf) {
        Write-Host "  Published: PowerShell Gallery" -ForegroundColor Green
    }
}

# Main execution
if ($PSCmdlet.ShouldProcess("ClaudeProfileManager Module", "Build")) {
    $success = Build-ClaudeProfileManagerModule
    exit $(if ($success) { 0 } else { 1 })
}
else {
    Write-Host "Build cancelled" -ForegroundColor Yellow
    exit 1
}