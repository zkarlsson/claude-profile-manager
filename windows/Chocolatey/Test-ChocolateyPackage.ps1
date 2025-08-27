#Requires -Version 5.1
<#
.SYNOPSIS
    Tests the Claude Profile Manager Chocolatey package.

.DESCRIPTION
    This script performs comprehensive testing of the Chocolatey package including
    installation, functionality verification, and cleanup testing. It can be used
    for quality assurance before publishing to Chocolatey Community Repository.

.PARAMETER PackagePath
    Path to the .nupkg file to test. If not specified, looks for latest package in dist folder.

.PARAMETER TestEnvironment
    Testing environment: Local (uses local package file) or Remote (installs from Chocolatey).

.PARAMETER SkipInstallation
    Skip the installation test (assumes package is already installed).

.PARAMETER SkipUninstallation
    Skip the uninstallation test (leaves package installed).

.PARAMETER Detailed
    Show detailed test output and diagnostics.

.PARAMETER CleanupFirst
    Remove any existing installation before testing.

.EXAMPLE
    .\Test-ChocolateyPackage.ps1
    
    Tests the latest package with default settings.

.EXAMPLE
    .\Test-ChocolateyPackage.ps1 -PackagePath ".\dist\claude-profile-manager.1.0.0.nupkg" -Detailed
    
    Tests a specific package with detailed output.

.EXAMPLE
    .\Test-ChocolateyPackage.ps1 -TestEnvironment Remote -CleanupFirst
    
    Tests the published package from Chocolatey Community Repository.

.NOTES
    - Requires Administrator privileges for installation testing
    - Creates temporary test profiles during testing
    - Automatically cleans up test data unless -SkipUninstallation is used

#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$PackagePath,
    
    [Parameter(Mandatory = $false)]
    [ValidateSet('Local', 'Remote')]
    [string]$TestEnvironment = 'Local',
    
    [Parameter(Mandatory = $false)]
    [switch]$SkipInstallation,
    
    [Parameter(Mandatory = $false)]
    [switch]$SkipUninstallation,
    
    [Parameter(Mandatory = $false)]
    [switch]$Detailed,
    
    [Parameter(Mandatory = $false)]
    [switch]$CleanupFirst
)

# Set error action preference
$ErrorActionPreference = 'Stop'

# Test configuration
$script:TestConfig = @{
    PackageName = 'claude-profile-manager'
    TestStartTime = Get-Date
    TestResults = @()
    InstallLocation = Join-Path $env:ProgramFiles 'ClaudeProfileManager'
    TestProfiles = @('test-profile-1', 'test-profile-2', 'chocolatey-test')
}

# Main test function
function Test-ChocolateyPackage {
    try {
        Write-Host "Claude Profile Manager Chocolatey Package Tester" -ForegroundColor Cyan
        Write-Host "=" * 54 -ForegroundColor Cyan
        Write-Host
        
        # Check prerequisites
        Test-Prerequisites
        
        # Initialize test environment
        Initialize-TestEnvironment
        
        # Cleanup first if requested
        if ($CleanupFirst) {
            Write-Host "Performing initial cleanup..." -ForegroundColor Yellow
            Cleanup-PreviousInstallation
        }
        
        # Test installation
        if (-not $SkipInstallation) {
            Write-Host "Testing package installation..." -ForegroundColor Yellow
            Test-PackageInstallation
        }
        
        # Test functionality
        Write-Host "Testing package functionality..." -ForegroundColor Yellow
        Test-PackageFunctionality
        
        # Test uninstallation
        if (-not $SkipUninstallation) {
            Write-Host "Testing package uninstallation..." -ForegroundColor Yellow
            Test-PackageUninstallation
        }
        
        # Show test results
        Show-TestResults
        
        $failedTests = $script:TestResults | Where-Object { $_.Status -eq 'Failed' }
        if ($failedTests.Count -eq 0) {
            Write-Host
            Write-Host "✓ All tests passed successfully!" -ForegroundColor Green
            return 0
        } else {
            Write-Host
            Write-Host "✗ $($failedTests.Count) tests failed!" -ForegroundColor Red
            return 1
        }
    }
    catch {
        Write-Host
        Write-Error "Test execution failed: $($_.Exception.Message)"
        return 1
    }
    finally {
        # Cleanup test data
        Cleanup-TestData
    }
}

function Test-Prerequisites {
    Write-Verbose "Checking test prerequisites..."
    
    # Check for Administrator privileges
    $currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
    $isAdmin = $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    
    if (-not $isAdmin) {
        throw "Administrator privileges are required for package testing"
    }
    
    # Check for Chocolatey
    try {
        $chocoVersion = choco --version 2>$null
        Write-Verbose "Using Chocolatey version: $chocoVersion"
    }
    catch {
        throw "Chocolatey CLI (choco) is required but not found"
    }
    
    Add-TestResult -Name "Prerequisites Check" -Status "Passed" -Details "Administrator privileges and Chocolatey CLI available"
}

function Initialize-TestEnvironment {
    Write-Verbose "Initializing test environment..."
    
    # Determine package path
    if (-not $PackagePath -and $TestEnvironment -eq 'Local') {
        $distPath = Join-Path $PSScriptRoot "dist"
        if (Test-Path $distPath) {
            $latestPackage = Get-ChildItem $distPath -Filter "*.nupkg" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
            if ($latestPackage) {
                $PackagePath = $latestPackage.FullName
                Write-Verbose "Using latest package: $PackagePath"
            }
        }
        
        if (-not $PackagePath) {
            throw "No package path specified and no packages found in dist folder"
        }
    }
    
    $script:TestConfig.PackagePath = $PackagePath
    $script:TestConfig.TestEnvironment = $TestEnvironment
    
    Write-Host "Test Configuration:" -ForegroundColor Yellow
    Write-Host "  Environment: $TestEnvironment" -ForegroundColor Gray
    if ($TestEnvironment -eq 'Local') {
        Write-Host "  Package: $PackagePath" -ForegroundColor Gray
    }
    Write-Host "  Install Location: $($script:TestConfig.InstallLocation)" -ForegroundColor Gray
    Write-Host
}

function Cleanup-PreviousInstallation {
    try {
        # Check if package is installed
        $installedPackage = choco list --local-only $script:TestConfig.PackageName --exact 2>$null
        if ($installedPackage -match $script:TestConfig.PackageName) {
            Write-Host "  Removing existing installation..." -ForegroundColor Gray
            choco uninstall $script:TestConfig.PackageName -y --remove-dependencies
        }
        
        # Remove any remaining files
        if (Test-Path $script:TestConfig.InstallLocation) {
            Remove-Item $script:TestConfig.InstallLocation -Recurse -Force
        }
        
        Add-TestResult -Name "Initial Cleanup" -Status "Passed" -Details "Previous installation removed"
    }
    catch {
        Add-TestResult -Name "Initial Cleanup" -Status "Failed" -Details $_.Exception.Message
    }
}

function Test-PackageInstallation {
    try {
        Write-Host "  Installing package..." -ForegroundColor Gray
        
        if ($TestEnvironment -eq 'Local') {
            # Install from local package file
            choco install $script:TestConfig.PackagePath -f -y
        } else {
            # Install from Chocolatey Community Repository
            choco install $script:TestConfig.PackageName -y
        }
        
        if ($LASTEXITCODE -ne 0) {
            throw "Package installation failed with exit code $LASTEXITCODE"
        }
        
        # Verify installation
        Test-InstallationFiles
        Test-PathConfiguration
        Test-StartMenuEntries
        
        Add-TestResult -Name "Package Installation" -Status "Passed" -Details "Package installed successfully"
    }
    catch {
        Add-TestResult -Name "Package Installation" -Status "Failed" -Details $_.Exception.Message
    }
}

function Test-InstallationFiles {
    # Check main executable
    $exePath = Join-Path $script:TestConfig.InstallLocation "ClaudeProfileManager.Windows.exe"
    if (-not (Test-Path $exePath)) {
        throw "Main executable not found at $exePath"
    }
    
    # Test executable version
    try {
        $version = & $exePath --version 2>$null
        if ($LASTEXITCODE -ne 0) {
            throw "Executable version check failed"
        }
        Write-Verbose "Executable version: $version"
    }
    catch {
        throw "Executable is not functional: $($_.Exception.Message)"
    }
    
    # Check PowerShell module
    try {
        Import-Module ClaudeProfileManager -Force -ErrorAction Stop
        $moduleInfo = Get-Module ClaudeProfileManager
        if (-not $moduleInfo) {
            throw "PowerShell module not loaded properly"
        }
        Write-Verbose "PowerShell module version: v$($moduleInfo.Version)"
        Remove-Module ClaudeProfileManager
    }
    catch {
        throw "PowerShell module test failed: $($_.Exception.Message)"
    }
    
    Add-TestResult -Name "Installation Files" -Status "Passed" -Details "All files installed and functional"
}

function Test-PathConfiguration {
    $currentPath = [Environment]::GetEnvironmentVariable('PATH', 'Machine')
    if ($currentPath -notlike "*$($script:TestConfig.InstallLocation)*") {
        throw "Installation directory not found in system PATH"
    }
    
    # Test command availability in new session
    $testScript = {
        try {
            claude-profile-manager --version 2>$null
            return $LASTEXITCODE
        } catch {
            return -1
        }
    }
    
    $result = Start-Job -ScriptBlock $testScript | Wait-Job | Receive-Job
    Remove-Job *
    
    if ($result -ne 0) {
        throw "Command not available in PATH (exit code: $result)"
    }
    
    Add-TestResult -Name "PATH Configuration" -Status "Passed" -Details "Command available system-wide"
}

function Test-StartMenuEntries {
    $startMenuPath = Join-Path ([Environment]::GetFolderPath('CommonPrograms')) 'Claude Profile Manager'
    
    if (-not (Test-Path $startMenuPath)) {
        throw "Start menu directory not found"
    }
    
    $expectedShortcuts = @(
        'Claude Profile Manager.lnk',
        'Claude Profile Manager (PowerShell).lnk'
    )
    
    foreach ($shortcut in $expectedShortcuts) {
        $shortcutPath = Join-Path $startMenuPath $shortcut
        if (-not (Test-Path $shortcutPath)) {
            throw "Start menu shortcut not found: $shortcut"
        }
    }
    
    Add-TestResult -Name "Start Menu Integration" -Status "Passed" -Details "Start menu entries created"
}

function Test-PackageFunctionality {
    # Test CLI functionality
    Test-CLIFunctionality
    
    # Test PowerShell module functionality
    Test-PowerShellFunctionality
    
    # Test integration features
    Test-IntegrationFeatures
}

function Test-CLIFunctionality {
    try {
        $exePath = Join-Path $script:TestConfig.InstallLocation "ClaudeProfileManager.Windows.exe"
        
        # Test help command
        $helpOutput = & $exePath --help 2>$null
        if ($LASTEXITCODE -ne 0 -or -not $helpOutput) {
            throw "Help command failed"
        }
        
        # Test version command
        $versionOutput = & $exePath --version 2>$null
        if ($LASTEXITCODE -ne 0 -or -not $versionOutput) {
            throw "Version command failed"
        }
        
        # Note: We can't test actual profile operations without Claude CLI being available
        # This is a limitation of the test environment
        
        Add-TestResult -Name "CLI Functionality" -Status "Passed" -Details "Basic CLI commands working"
    }
    catch {
        Add-TestResult -Name "CLI Functionality" -Status "Failed" -Details $_.Exception.Message
    }
}

function Test-PowerShellFunctionality {
    try {
        # Import module
        Import-Module ClaudeProfileManager -Force
        
        # Test cmdlet availability
        $expectedCmdlets = @(
            'Save-ClaudeProfile',
            'Get-ClaudeProfile',
            'Switch-ClaudeProfile',
            'Remove-ClaudeProfile',
            'Get-CurrentClaudeProfile',
            'Test-ClaudeProfileHealth',
            'Get-ClaudeProfileStatus'
        )
        
        foreach ($cmdlet in $expectedCmdlets) {
            $command = Get-Command $cmdlet -ErrorAction SilentlyContinue
            if (-not $command) {
                throw "Cmdlet not available: $cmdlet"
            }
        }
        
        # Test help system
        $help = Get-Help Save-ClaudeProfile
        if (-not $help -or -not $help.Synopsis) {
            throw "Help system not working properly"
        }
        
        # Test about help
        $aboutHelp = Get-Help about_ClaudeProfileManager
        if (-not $aboutHelp) {
            throw "About help topic not available"
        }
        
        Remove-Module ClaudeProfileManager
        Add-TestResult -Name "PowerShell Functionality" -Status "Passed" -Details "All cmdlets and help system working"
    }
    catch {
        Add-TestResult -Name "PowerShell Functionality" -Status "Failed" -Details $_.Exception.Message
        Remove-Module ClaudeProfileManager -Force -ErrorAction SilentlyContinue
    }
}

function Test-IntegrationFeatures {
    try {
        # Test that both CLI and PowerShell can coexist
        $exePath = Join-Path $script:TestConfig.InstallLocation "ClaudeProfileManager.Windows.exe"
        
        # Test CLI help
        $cliHelp = & $exePath --help 2>$null
        
        # Test PowerShell help
        Import-Module ClaudeProfileManager -Force
        $psHelp = Get-Help Save-ClaudeProfile
        Remove-Module ClaudeProfileManager
        
        if (-not $cliHelp -or -not $psHelp) {
            throw "Integration between CLI and PowerShell failed"
        }
        
        Add-TestResult -Name "Integration Features" -Status "Passed" -Details "CLI and PowerShell integration working"
    }
    catch {
        Add-TestResult -Name "Integration Features" -Status "Failed" -Details $_.Exception.Message
        Remove-Module ClaudeProfileManager -Force -ErrorAction SilentlyContinue
    }
}

function Test-PackageUninstallation {
    try {
        Write-Host "  Uninstalling package..." -ForegroundColor Gray
        
        # Uninstall package
        choco uninstall $script:TestConfig.PackageName -y
        
        if ($LASTEXITCODE -ne 0) {
            throw "Package uninstallation failed with exit code $LASTEXITCODE"
        }
        
        # Verify uninstallation
        Test-UninstallationCleanup
        
        Add-TestResult -Name "Package Uninstallation" -Status "Passed" -Details "Package uninstalled successfully"
    }
    catch {
        Add-TestResult -Name "Package Uninstallation" -Status "Failed" -Details $_.Exception.Message
    }
}

function Test-UninstallationCleanup {
    # Check that main installation is removed
    if (Test-Path $script:TestConfig.InstallLocation) {
        throw "Installation directory still exists after uninstallation"
    }
    
    # Check that PowerShell module is removed
    try {
        Import-Module ClaudeProfileManager -ErrorAction Stop
        throw "PowerShell module still available after uninstallation"
    }
    catch [System.IO.FileNotFoundException] {
        # Expected - module should not be found
    }
    catch {
        if ($_.Exception.Message -notmatch "not found|could not be loaded") {
            throw "Unexpected error testing PowerShell module removal: $($_.Exception.Message)"
        }
    }
    
    # Check PATH cleanup
    $currentPath = [Environment]::GetEnvironmentVariable('PATH', 'Machine')
    if ($currentPath -like "*$($script:TestConfig.InstallLocation)*") {
        throw "Installation directory still in system PATH after uninstallation"
    }
    
    # Check Start Menu cleanup
    $startMenuPath = Join-Path ([Environment]::GetFolderPath('CommonPrograms')) 'Claude Profile Manager'
    if (Test-Path $startMenuPath) {
        throw "Start menu entries still exist after uninstallation"
    }
    
    Add-TestResult -Name "Uninstallation Cleanup" -Status "Passed" -Details "All components properly removed"
}

function Add-TestResult {
    param(
        [string]$Name,
        [string]$Status,
        [string]$Details
    )
    
    $result = [PSCustomObject]@{
        Name = $Name
        Status = $Status
        Details = $Details
        Timestamp = Get-Date
    }
    
    $script:TestResults += $result
    
    $color = switch ($Status) {
        'Passed' { 'Green' }
        'Failed' { 'Red' }
        'Warning' { 'Yellow' }
        default { 'Gray' }
    }
    
    $symbol = switch ($Status) {
        'Passed' { '✓' }
        'Failed' { '✗' }
        'Warning' { '⚠' }
        default { '-' }
    }
    
    if ($Detailed) {
        Write-Host "    $symbol $Name`: $Details" -ForegroundColor $color
    } else {
        Write-Host "    $symbol $Name" -ForegroundColor $color
    }
}

function Show-TestResults {
    $duration = (Get-Date) - $script:TestConfig.TestStartTime
    
    $passedTests = $script:TestResults | Where-Object { $_.Status -eq 'Passed' }
    $failedTests = $script:TestResults | Where-Object { $_.Status -eq 'Failed' }
    $warningTests = $script:TestResults | Where-Object { $_.Status -eq 'Warning' }
    
    Write-Host
    Write-Host "Test Summary:" -ForegroundColor Cyan
    Write-Host "  Total Tests: $($script:TestResults.Count)" -ForegroundColor Gray
    Write-Host "  Passed: $($passedTests.Count)" -ForegroundColor Green
    if ($failedTests.Count -gt 0) {
        Write-Host "  Failed: $($failedTests.Count)" -ForegroundColor Red
    }
    if ($warningTests.Count -gt 0) {
        Write-Host "  Warnings: $($warningTests.Count)" -ForegroundColor Yellow
    }
    Write-Host "  Duration: $([Math]::Round($duration.TotalSeconds, 2)) seconds" -ForegroundColor Gray
    
    if ($failedTests.Count -gt 0) {
        Write-Host
        Write-Host "Failed Tests:" -ForegroundColor Red
        foreach ($test in $failedTests) {
            Write-Host "  ✗ $($test.Name): $($test.Details)" -ForegroundColor Red
        }
    }
}

function Cleanup-TestData {
    Write-Verbose "Cleaning up test data..."
    
    # Remove any test profiles that might have been created
    # Note: This would require Claude CLI to be available, which might not be the case in test environment
    
    try {
        # Remove any temporary test files
        $tempFiles = Get-ChildItem $env:TEMP -Filter "ChocolateyTest*" -ErrorAction SilentlyContinue
        foreach ($file in $tempFiles) {
            Remove-Item $file.FullName -Force -Recurse -ErrorAction SilentlyContinue
        }
    }
    catch {
        Write-Verbose "Test cleanup completed with minor issues: $($_.Exception.Message)"
    }
}

# Main execution
Write-Host "Starting Chocolatey package testing..." -ForegroundColor Cyan
$exitCode = Test-ChocolateyPackage
Write-Host "Testing completed." -ForegroundColor Cyan
exit $exitCode