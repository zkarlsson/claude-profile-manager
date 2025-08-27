#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Builds optimized distribution packages for Claude Profile Manager Windows.

.DESCRIPTION
    Creates production-ready builds with optimized paths and packaging for:
    - Single-file executable
    - Chocolatey package
    - MSI installer preparation
    
.PARAMETER Configuration
    Build configuration (Release, Debug). Default: Release

.PARAMETER Clean
    Clean all build outputs before building

.PARAMETER SkipTests
    Skip running tests before building

.EXAMPLE
    .\Build-Distribution.ps1
    .\Build-Distribution.ps1 -Clean -Configuration Release
#>

[CmdletBinding()]
param(
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release",
    
    [switch]$Clean,
    
    [switch]$SkipTests
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Build paths - optimized for Windows distribution
$ProjectRoot = Split-Path -Path $PSScriptRoot -Parent
$WindowsProject = Join-Path $PSScriptRoot "ClaudeProfileManager.Windows"
$DistDir = Join-Path $PSScriptRoot "dist"
$PackageDir = Join-Path $PSScriptRoot "packages"

Write-Host "🏗️  Claude Profile Manager - Windows Distribution Build" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow
Write-Host "Project Root: $ProjectRoot" -ForegroundColor Yellow

# Clean if requested
if ($Clean) {
    Write-Host "🧹 Cleaning build outputs..." -ForegroundColor Yellow
    
    $CleanPaths = @(
        (Join-Path $WindowsProject "bin"),
        (Join-Path $WindowsProject "obj"),
        $DistDir,
        $PackageDir
    )
    
    foreach ($Path in $CleanPaths) {
        if (Test-Path $Path) {
            Remove-Item $Path -Recurse -Force
            Write-Host "  Removed: $Path" -ForegroundColor Gray
        }
    }
}

# Run tests unless skipped
if (-not $SkipTests) {
    Write-Host "🧪 Running tests..." -ForegroundColor Yellow
    
    try {
        dotnet test (Join-Path $ProjectRoot "ClaudeProfileManager.sln") --configuration $Configuration --verbosity minimal
        if ($LASTEXITCODE -ne 0) {
            throw "Tests failed with exit code $LASTEXITCODE"
        }
        Write-Host "✅ All tests passed" -ForegroundColor Green
    }
    catch {
        Write-Error "❌ Tests failed: $_"
        exit 1
    }
}

# Build optimized single-file executable
Write-Host "🔨 Building optimized executable..." -ForegroundColor Yellow

try {
    # Create dist directory
    if (-not (Test-Path $DistDir)) {
        New-Item -ItemType Directory -Path $DistDir -Force | Out-Null
    }
    
    # Build with optimizations
    $PublishArgs = @(
        "publish"
        $WindowsProject
        "--configuration", $Configuration
        "--runtime", "win-x64"
        "--self-contained", "true"
        "--output", $DistDir
        "/p:PublishSingleFile=true"
        "/p:PublishReadyToRun=true"
        "--verbosity", "minimal"
    )
    
    dotnet @PublishArgs
    
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE"
    }
    
    # Verify executable exists
    $ExePath = Join-Path $DistDir "claude-profile-manager.exe"
    if (-not (Test-Path $ExePath)) {
        throw "Expected executable not found at: $ExePath"
    }
    
    # Get file size for optimization reporting
    $FileSize = (Get-Item $ExePath).Length
    $FileSizeMB = [Math]::Round($FileSize / 1MB, 2)
    
    Write-Host "✅ Build successful!" -ForegroundColor Green
    Write-Host "  Executable: $ExePath" -ForegroundColor Gray
    Write-Host "  Size: ${FileSizeMB} MB" -ForegroundColor Gray
    
} catch {
    Write-Error "❌ Build failed: $_"
    exit 1
}

# Test the built executable
Write-Host "🧪 Testing built executable..." -ForegroundColor Yellow

try {
    $TestExe = Join-Path $DistDir "claude-profile-manager.exe"
    
    # Test version command
    $VersionOutput = & $TestExe --version 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Version command failed: $VersionOutput"
    }
    
    # Test health command
    $HealthOutput = & $TestExe health 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Health command failed: $HealthOutput"
    }
    
    Write-Host "✅ Executable tests passed" -ForegroundColor Green
    
} catch {
    Write-Error "❌ Executable test failed: $_"
    exit 1
}

# Prepare packages directory structure
Write-Host "📦 Preparing package structure..." -ForegroundColor Yellow

try {
    if (-not (Test-Path $PackageDir)) {
        New-Item -ItemType Directory -Path $PackageDir -Force | Out-Null
    }
    
    # Create Chocolatey package structure
    $ChocolateyDir = Join-Path $PackageDir "chocolatey"
    $ChocolateyToolsDir = Join-Path $ChocolateyDir "tools"
    
    if (Test-Path $ChocolateyDir) {
        Remove-Item $ChocolateyDir -Recurse -Force
    }
    
    New-Item -ItemType Directory -Path $ChocolateyToolsDir -Force | Out-Null
    
    # Copy executable to Chocolatey tools
    Copy-Item (Join-Path $DistDir "claude-profile-manager.exe") $ChocolateyToolsDir -Force
    
    # Copy Chocolatey package files
    $ChocolateySourceDir = Join-Path $PSScriptRoot "Chocolatey\claude-profile-manager"
    if (Test-Path $ChocolateySourceDir) {
        Copy-Item (Join-Path $ChocolateySourceDir "*") $ChocolateyDir -Recurse -Force -Exclude "tools"
    }
    
    # Create MSI preparation directory
    $MSIDir = Join-Path $PackageDir "msi"
    if (Test-Path $MSIDir) {
        Remove-Item $MSIDir -Recurse -Force
    }
    New-Item -ItemType Directory -Path $MSIDir -Force | Out-Null
    Copy-Item (Join-Path $DistDir "claude-profile-manager.exe") $MSIDir -Force
    
    Write-Host "✅ Package structure prepared" -ForegroundColor Green
    Write-Host "  Chocolatey: $ChocolateyDir" -ForegroundColor Gray
    Write-Host "  MSI Prep: $MSIDir" -ForegroundColor Gray
    
} catch {
    Write-Error "❌ Package preparation failed: $_"
    exit 1
}

# Display summary
Write-Host ""
Write-Host "🎉 Distribution build completed successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "📁 Build Outputs:" -ForegroundColor Cyan
Write-Host "  Optimized Executable: $(Join-Path $DistDir 'claude-profile-manager.exe')" -ForegroundColor White
Write-Host "  Chocolatey Package: $(Join-Path $PackageDir 'chocolatey')" -ForegroundColor White
Write-Host "  MSI Preparation: $(Join-Path $PackageDir 'msi')" -ForegroundColor White
Write-Host ""
Write-Host "📋 Next Steps:" -ForegroundColor Cyan
Write-Host "  1. Build Chocolatey package: choco pack $(Join-Path $PackageDir 'chocolatey\claude-profile-manager.nuspec')" -ForegroundColor White
Write-Host "  2. Create MSI installer from $(Join-Path $PackageDir 'msi')" -ForegroundColor White
Write-Host "  3. Test installation packages" -ForegroundColor White
Write-Host ""

# Return useful information
return @{
    ExecutablePath = Join-Path $DistDir "claude-profile-manager.exe"
    ChocolateyPackage = Join-Path $PackageDir "chocolatey"
    MSIPreparation = Join-Path $PackageDir "msi"
    Success = $true
}