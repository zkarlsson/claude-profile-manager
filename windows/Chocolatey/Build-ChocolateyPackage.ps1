#Requires -Version 5.1
<#
.SYNOPSIS
    Builds the Claude Profile Manager Chocolatey package.

.DESCRIPTION
    This script builds and optionally tests the Chocolatey package for Claude Profile Manager.
    It handles application compilation, PowerShell module preparation, and package creation.

.PARAMETER Version
    Version number for the package. If not specified, uses version from project file.

.PARAMETER OutputPath
    Directory where the package (.nupkg) will be created. Defaults to .\dist.

.PARAMETER SkipBuild
    Skip building the .NET application and use existing binaries.

.PARAMETER SkipTest
    Skip testing the package installation.

.PARAMETER Configuration
    Build configuration (Debug or Release). Defaults to Release.

.PARAMETER Publish
    Publish the package to Chocolatey Community Repository.

.PARAMETER ApiKey
    Chocolatey API key for publishing.

.PARAMETER Force
    Force rebuild and overwrite existing package.

.EXAMPLE
    .\Build-ChocolateyPackage.ps1
    
    Builds the package with default settings.

.EXAMPLE
    .\Build-ChocolateyPackage.ps1 -Version "1.0.1" -Configuration Release
    
    Builds a specific version in Release configuration.

.EXAMPLE
    .\Build-ChocolateyPackage.ps1 -Publish -ApiKey "your-api-key"
    
    Builds and publishes the package to Chocolatey Community Repository.

.NOTES
    - Requires Chocolatey CLI (choco) to be installed
    - Requires .NET SDK for building the application
    - Creates a complete package with both CLI and PowerShell module

#>

[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory = $false)]
    [string]$Version,
    
    [Parameter(Mandatory = $false)]
    [string]$OutputPath = (Join-Path $PSScriptRoot "dist"),
    
    [Parameter(Mandatory = $false)]
    [switch]$SkipBuild,
    
    [Parameter(Mandatory = $false)]
    [switch]$SkipTest,
    
    [Parameter(Mandatory = $false)]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    
    [Parameter(Mandatory = $false)]
    [switch]$Publish,
    
    [Parameter(Mandatory = $false)]
    [string]$ApiKey,
    
    [Parameter(Mandatory = $false)]
    [switch]$Force
)

# Set error action preference
$ErrorActionPreference = 'Stop'

# Build configuration
$script:BuildConfig = @{
    PackageName = 'claude-profile-manager'
    ProjectRoot = Split-Path $PSScriptRoot -Parent
    PackageRoot = Join-Path $PSScriptRoot 'claude-profile-manager'
    OutputPath = $OutputPath
    Configuration = $Configuration
    BuildDate = Get-Date
}

# Main build function
function Build-ChocolateyPackage {
    try {
        Write-Host "Claude Profile Manager Chocolatey Package Builder" -ForegroundColor Cyan
        Write-Host "=" * 55 -ForegroundColor Cyan
        Write-Host
        
        # Initialize build
        Initialize-Build
        
        # Validate prerequisites
        Test-Prerequisites
        
        # Build .NET application if not skipped
        if (-not $SkipBuild) {
            Write-Host "Building .NET application..." -ForegroundColor Yellow
            Build-DotNetApplication
        }
        
        # Prepare package structure
        Write-Host "Preparing package structure..." -ForegroundColor Yellow
        Prepare-PackageStructure
        
        # Update package metadata
        Write-Host "Updating package metadata..." -ForegroundColor Yellow
        Update-PackageMetadata
        
        # Create package
        Write-Host "Creating Chocolatey package..." -ForegroundColor Yellow
        $packagePath = New-ChocolateyPackage
        
        # Test package if not skipped
        if (-not $SkipTest) {
            Write-Host "Testing package..." -ForegroundColor Yellow
            Test-ChocolateyPackage -PackagePath $packagePath
        }
        
        # Publish package if requested
        if ($Publish) {
            Write-Host "Publishing package..." -ForegroundColor Yellow
            Publish-ChocolateyPackage -PackagePath $packagePath
        }
        
        # Show build summary
        Show-BuildSummary -PackagePath $packagePath
        
        Write-Host
        Write-Host "✓ Chocolatey package build completed successfully!" -ForegroundColor Green
        
        return $packagePath
    }
    catch {
        Write-Host
        Write-Error "Chocolatey package build failed: $($_.Exception.Message)"
        return $null
    }
}

function Initialize-Build {
    Write-Verbose "Initializing build environment..."
    
    # Create output directory
    if (-not (Test-Path $script:BuildConfig.OutputPath)) {
        New-Item -ItemType Directory -Path $script:BuildConfig.OutputPath -Force | Out-Null
    }
    
    # Determine version
    if (-not $Version) {
        # Get version from project file
        $projectFile = Join-Path $script:BuildConfig.ProjectRoot "ClaudeProfileManager.Windows\ClaudeProfileManager.Windows.csproj"
        if (Test-Path $projectFile) {
            $projectXml = [xml](Get-Content $projectFile)
            $Version = $projectXml.Project.PropertyGroup.Version
            if (-not $Version) {
                $Version = "1.0.0"
                Write-Warning "Could not determine version from project file, using default: $Version"
            }
        } else {
            $Version = "1.0.0"
            Write-Warning "Project file not found, using default version: $Version"
        }
    }
    
    $script:BuildConfig.Version = $Version
    
    Write-Host "Build Configuration:" -ForegroundColor Yellow
    Write-Host "  Package Name: $($script:BuildConfig.PackageName)" -ForegroundColor Gray
    Write-Host "  Version: $($script:BuildConfig.Version)" -ForegroundColor Gray
    Write-Host "  Configuration: $($script:BuildConfig.Configuration)" -ForegroundColor Gray
    Write-Host "  Output Path: $($script:BuildConfig.OutputPath)" -ForegroundColor Gray
    Write-Host "  Project Root: $($script:BuildConfig.ProjectRoot)" -ForegroundColor Gray
    Write-Host
}

function Test-Prerequisites {
    # Check for Chocolatey
    try {
        $chocoVersion = choco --version 2>$null
        Write-Verbose "Using Chocolatey version: $chocoVersion"
    }
    catch {
        throw "Chocolatey CLI (choco) is required but not found. Install from https://chocolatey.org/install"
    }
    
    # Check for .NET SDK if not skipping build
    if (-not $SkipBuild) {
        try {
            $dotnetVersion = dotnet --version 2>$null
            Write-Verbose "Using .NET SDK version: $dotnetVersion"
        }
        catch {
            throw ".NET SDK is required for building but not found. Install from https://dotnet.microsoft.com/download"
        }
    }
    
    # Validate package structure
    $nuspecPath = Join-Path $script:BuildConfig.PackageRoot "$($script:BuildConfig.PackageName).nuspec"
    if (-not (Test-Path $nuspecPath)) {
        throw "Package specification not found: $nuspecPath"
    }
    
    Write-Verbose "✓ All prerequisites validated"
}

function Build-DotNetApplication {
    $projectFile = Join-Path $script:BuildConfig.ProjectRoot "ClaudeProfileManager.Windows\ClaudeProfileManager.Windows.csproj"
    
    if (-not (Test-Path $projectFile)) {
        throw "Project file not found: $projectFile"
    }
    
    # Clean previous build
    $publishPath = Join-Path $script:BuildConfig.ProjectRoot "ClaudeProfileManager.Windows\bin\$($script:BuildConfig.Configuration)\net9.0\publish"
    if ((Test-Path $publishPath) -and $Force) {
        Remove-Item $publishPath -Recurse -Force
    }
    
    # Build and publish
    $buildArgs = @(
        'publish'
        $projectFile
        '--configuration', $script:BuildConfig.Configuration
        '--runtime', 'win-x64'
        '--self-contained', 'false'
        '--output', $publishPath
        '--verbosity', 'minimal'
    )
    
    Write-Verbose "Building application: dotnet $($buildArgs -join ' ')"
    
    & dotnet @buildArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE"
    }
    
    # Verify build output
    $exePath = Join-Path $publishPath "ClaudeProfileManager.Windows.exe"
    if (-not (Test-Path $exePath)) {
        throw "Build output not found: $exePath"
    }
    
    Write-Host "  ✓ .NET application built successfully" -ForegroundColor Green
}

function Prepare-PackageStructure {
    $toolsPath = Join-Path $script:BuildConfig.PackageRoot "tools"
    
    # Clean tools directory if it exists
    if ((Test-Path $toolsPath) -and $Force) {
        Get-ChildItem $toolsPath -Exclude "*.ps1" | Remove-Item -Recurse -Force
    }
    
    # Create app directory and copy application files
    $appPath = Join-Path $toolsPath "app"
    if (-not (Test-Path $appPath)) {
        New-Item -ItemType Directory -Path $appPath -Force | Out-Null
    }
    
    $publishPath = Join-Path $script:BuildConfig.ProjectRoot "ClaudeProfileManager.Windows\bin\$($script:BuildConfig.Configuration)\net9.0\publish"
    if (Test-Path $publishPath) {
        Copy-Item -Path "$publishPath\*" -Destination $appPath -Recurse -Force -Exclude "*.pdb"
        Write-Verbose "Application files copied to package"
    }
    
    # Create PowerShell directory and copy module files
    $psPath = Join-Path $toolsPath "powershell"
    if (-not (Test-Path $psPath)) {
        New-Item -ItemType Directory -Path $psPath -Force | Out-Null
    }
    
    $powerShellSource = Join-Path $script:BuildConfig.ProjectRoot "PowerShell"
    if (Test-Path $powerShellSource) {
        Copy-Item -Path $powerShellSource -Destination $psPath -Recurse -Force -Exclude "Tests", "dist", "*.Tests.ps1"
        Write-Verbose "PowerShell module files copied to package"
    }
    
    Write-Host "  ✓ Package structure prepared" -ForegroundColor Green
}

function Update-PackageMetadata {
    $nuspecPath = Join-Path $script:BuildConfig.PackageRoot "$($script:BuildConfig.PackageName).nuspec"
    
    # Read and update nuspec
    [xml]$nuspec = Get-Content $nuspecPath
    $nuspec.package.metadata.version = $script:BuildConfig.Version
    
    # Update release notes with build information
    $buildInfo = @"
Built on $($script:BuildConfig.BuildDate.ToString('yyyy-MM-dd HH:mm:ss'))
Configuration: $($script:BuildConfig.Configuration)
"@
    
    # Save updated nuspec
    $nuspec.Save($nuspecPath)
    
    Write-Host "  ✓ Package metadata updated (v$($script:BuildConfig.Version))" -ForegroundColor Green
}

function New-ChocolateyPackage {
    $packageDir = Split-Path $script:BuildConfig.PackageRoot -Parent
    $packageName = "$($script:BuildConfig.PackageName).$($script:BuildConfig.Version).nupkg"
    $packagePath = Join-Path $script:BuildConfig.OutputPath $packageName
    
    # Remove existing package if Force is specified
    if ((Test-Path $packagePath) -and $Force) {
        Remove-Item $packagePath -Force
    }
    
    # Create package
    Push-Location $script:BuildConfig.PackageRoot
    try {
        & choco pack --outputdirectory $script:BuildConfig.OutputPath
        if ($LASTEXITCODE -ne 0) {
            throw "Package creation failed with exit code $LASTEXITCODE"
        }
    }
    finally {
        Pop-Location
    }
    
    # Verify package was created
    if (-not (Test-Path $packagePath)) {
        throw "Package was not created at expected location: $packagePath"
    }
    
    $packageSize = (Get-Item $packagePath).Length / 1MB
    Write-Host "  ✓ Package created: $packageName ($([Math]::Round($packageSize, 2)) MB)" -ForegroundColor Green
    
    return $packagePath
}

function Test-ChocolateyPackage {
    param([string]$PackagePath)
    
    Write-Host "  Testing package structure..." -ForegroundColor Gray
    
    # Basic package validation
    try {
        # Test package can be read
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        $zip = [System.IO.Compression.ZipFile]::OpenRead($PackagePath)
        
        $expectedFiles = @(
            'claude-profile-manager.nuspec',
            'tools/chocolateyinstall.ps1',
            'tools/chocolateyuninstall.ps1'
        )
        
        $zipEntries = $zip.Entries | Select-Object -ExpandProperty FullName
        
        foreach ($expectedFile in $expectedFiles) {
            $found = $zipEntries | Where-Object { $_ -like "*$expectedFile" }
            if (-not $found) {
                throw "Expected file not found in package: $expectedFile"
            }
        }
        
        $zip.Dispose()
        
        Write-Host "  ✓ Package structure validation passed" -ForegroundColor Green
    }
    catch {
        throw "Package validation failed: $($_.Exception.Message)"
    }
    
    # Note: Full installation testing would require elevated privileges and could affect the system
    Write-Verbose "Note: Full installation testing skipped (would require elevation and system changes)"
}

function Publish-ChocolateyPackage {
    param([string]$PackagePath)
    
    if (-not $ApiKey) {
        throw "API key is required for publishing. Use -ApiKey parameter."
    }
    
    if ($PSCmdlet.ShouldProcess($PackagePath, "Publish to Chocolatey Community Repository")) {
        try {
            & choco push $PackagePath --api-key $ApiKey
            if ($LASTEXITCODE -ne 0) {
                throw "Package publishing failed with exit code $LASTEXITCODE"
            }
            
            Write-Host "  ✓ Package published to Chocolatey Community Repository" -ForegroundColor Green
        }
        catch {
            throw "Publishing failed: $($_.Exception.Message)"
        }
    }
    else {
        Write-Host "  Package publishing cancelled" -ForegroundColor Yellow
    }
}

function Show-BuildSummary {
    param([string]$PackagePath)
    
    Write-Host
    Write-Host "Build Summary:" -ForegroundColor Cyan
    Write-Host "  Package: $($script:BuildConfig.PackageName) v$($script:BuildConfig.Version)" -ForegroundColor Green
    Write-Host "  Location: $PackagePath" -ForegroundColor Green
    Write-Host "  Build Date: $($script:BuildConfig.BuildDate.ToString('yyyy-MM-dd HH:mm:ss'))" -ForegroundColor Green
    Write-Host "  Configuration: $($script:BuildConfig.Configuration)" -ForegroundColor Green
    
    if (Test-Path $PackagePath) {
        $packageSize = (Get-Item $PackagePath).Length
        Write-Host "  Size: $([Math]::Round($packageSize / 1MB, 2)) MB" -ForegroundColor Green
    }
    
    Write-Host
    Write-Host "Installation Commands:" -ForegroundColor Cyan
    Write-Host "  Test locally: choco install $PackagePath -f" -ForegroundColor Gray
    Write-Host "  Install from Chocolatey: choco install $($script:BuildConfig.PackageName)" -ForegroundColor Gray
    Write-Host
    Write-Host "Package Contents:" -ForegroundColor Cyan
    Write-Host "  • Claude Profile Manager CLI application (.NET 9)" -ForegroundColor Gray
    Write-Host "  • PowerShell module with comprehensive cmdlets" -ForegroundColor Gray
    Write-Host "  • Installation and uninstallation scripts" -ForegroundColor Gray
    Write-Host "  • System integration (PATH, Start Menu, shortcuts)" -ForegroundColor Gray
}

# Main execution
if ($PSCmdlet.ShouldProcess("Chocolatey Package", "Build")) {
    $packagePath = Build-ChocolateyPackage
    
    if ($packagePath) {
        Write-Output $packagePath
        exit 0
    } else {
        exit 1
    }
}
else {
    Write-Host "Build cancelled" -ForegroundColor Yellow
    exit 1
}