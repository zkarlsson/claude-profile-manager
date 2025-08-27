#Requires -Version 5.1
<#
.SYNOPSIS
    Test runner for ClaudeProfileManager PowerShell module.

.DESCRIPTION
    Runs comprehensive tests for the ClaudeProfileManager module including unit tests,
    integration tests, and validation tests. Supports different test modes and output formats.

.PARAMETER TestType
    Type of tests to run: Unit, Integration, All, or Validation.

.PARAMETER OutputFormat
    Output format: Console, JUnit, NUnit, or All.

.PARAMETER OutputPath
    Directory where test results will be saved.

.PARAMETER PassThru
    Return test results object.

.PARAMETER Detailed
    Show detailed test output.

.PARAMETER Tag
    Run tests with specific tags only.

.PARAMETER ExcludeTag
    Exclude tests with specific tags.

.EXAMPLE
    .\Test-ClaudeProfileManager.ps1
    
    Runs all tests with console output.

.EXAMPLE
    .\Test-ClaudeProfileManager.ps1 -TestType Unit -OutputFormat JUnit -OutputPath ".\TestResults"
    
    Runs unit tests and saves JUnit results to TestResults directory.

.EXAMPLE
    .\Test-ClaudeProfileManager.ps1 -Tag "Fast" -Detailed
    
    Runs only fast tests with detailed output.

.NOTES
    - Requires Pester module for testing
    - Automatically installs Pester if not available
    - Creates test results directory if it doesn't exist

#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [ValidateSet('Unit', 'Integration', 'All', 'Validation')]
    [string]$TestType = 'All',
    
    [Parameter(Mandatory = $false)]
    [ValidateSet('Console', 'JUnit', 'NUnit', 'All')]
    [string]$OutputFormat = 'Console',
    
    [Parameter(Mandatory = $false)]
    [string]$OutputPath = (Join-Path $PSScriptRoot "TestResults"),
    
    [Parameter(Mandatory = $false)]
    [switch]$PassThru,
    
    [Parameter(Mandatory = $false)]
    [switch]$Detailed,
    
    [Parameter(Mandatory = $false)]
    [string[]]$Tag,
    
    [Parameter(Mandatory = $false)]
    [string[]]$ExcludeTag
)

# Set error action preference
$ErrorActionPreference = 'Stop'

# Test configuration
$script:TestConfig = @{
    ModulePath = Join-Path $PSScriptRoot "..\ClaudeProfileManager"
    TestPath = $PSScriptRoot
    OutputPath = $OutputPath
    StartTime = Get-Date
}

# Main test function
function Invoke-ClaudeProfileManagerTests {
    try {
        Write-Host "Claude Profile Manager PowerShell Module Test Runner" -ForegroundColor Cyan
        Write-Host "=" * 58 -ForegroundColor Cyan
        Write-Host
        
        # Initialize test environment
        Initialize-TestEnvironment
        
        # Install Pester if needed
        Ensure-PesterModule
        
        # Determine test files
        $testFiles = Get-TestFiles -TestType $TestType
        
        # Configure Pester
        $pesterConfig = New-PesterConfiguration -TestFiles $testFiles
        
        # Run tests
        Write-Host "Running tests..." -ForegroundColor Yellow
        $testResults = Invoke-Pester -Configuration $pesterConfig
        
        # Process results
        Process-TestResults -Results $testResults
        
        # Show summary
        Show-TestSummary -Results $testResults
        
        if ($PassThru) {
            return $testResults
        }
        
        # Return exit code based on test results
        return $(if ($testResults.FailedCount -eq 0) { 0 } else { 1 })
    }
    catch {
        Write-Error "Test execution failed: $($_.Exception.Message)"
        return 1
    }
}

function Initialize-TestEnvironment {
    Write-Verbose "Initializing test environment..."
    
    # Validate module exists
    if (-not (Test-Path $script:TestConfig.ModulePath)) {
        throw "Module not found at $($script:TestConfig.ModulePath)"
    }
    
    # Create output directory
    if ($OutputFormat -ne 'Console' -and -not (Test-Path $script:TestConfig.OutputPath)) {
        New-Item -ItemType Directory -Path $script:TestConfig.OutputPath -Force | Out-Null
        Write-Verbose "Created test results directory: $($script:TestConfig.OutputPath)"
    }
    
    # Clean up any existing module imports
    Remove-Module ClaudeProfileManager -Force -ErrorAction SilentlyContinue
    
    Write-Host "Test Configuration:" -ForegroundColor Yellow
    Write-Host "  Module Path: $($script:TestConfig.ModulePath)" -ForegroundColor Gray
    Write-Host "  Test Path: $($script:TestConfig.TestPath)" -ForegroundColor Gray
    Write-Host "  Test Type: $TestType" -ForegroundColor Gray
    Write-Host "  Output Format: $OutputFormat" -ForegroundColor Gray
    if ($OutputFormat -ne 'Console') {
        Write-Host "  Output Path: $($script:TestConfig.OutputPath)" -ForegroundColor Gray
    }
    Write-Host
}

function Ensure-PesterModule {
    try {
        Import-Module Pester -Force -ErrorAction Stop
        $pesterVersion = (Get-Module Pester).Version
        Write-Verbose "Using Pester version: $pesterVersion"
        
        # Check if we have a compatible version (5.0+)
        if ($pesterVersion.Major -lt 5) {
            Write-Warning "Pester version $pesterVersion detected. Version 5.0+ is recommended for best results."
        }
    }
    catch {
        Write-Host "Pester module not found. Installing..." -ForegroundColor Yellow
        try {
            Install-Module Pester -Force -SkipPublisherCheck -Scope CurrentUser
            Import-Module Pester -Force
            Write-Host "✓ Pester installed successfully" -ForegroundColor Green
        }
        catch {
            throw "Failed to install Pester: $($_.Exception.Message)"
        }
    }
}

function Get-TestFiles {
    param([string]$TestType)
    
    $testFiles = @()
    
    switch ($TestType) {
        'Unit' {
            $testFiles += Join-Path $script:TestConfig.TestPath "ClaudeProfileManager.Tests.ps1"
        }
        'Integration' {
            $testFiles += Join-Path $script:TestConfig.TestPath "Integration.Tests.ps1"
        }
        'Validation' {
            $testFiles += Join-Path $script:TestConfig.TestPath "Validation.Tests.ps1"
        }
        'All' {
            $testFiles = Get-ChildItem -Path $script:TestConfig.TestPath -Filter "*.Tests.ps1" | 
                         Select-Object -ExpandProperty FullName
        }
    }
    
    # Validate test files exist
    $missingFiles = $testFiles | Where-Object { -not (Test-Path $_) }
    if ($missingFiles) {
        Write-Warning "Some test files were not found:"
        $missingFiles | ForEach-Object { Write-Warning "  $_" }
        $testFiles = $testFiles | Where-Object { Test-Path $_ }
    }
    
    if ($testFiles.Count -eq 0) {
        throw "No test files found for test type: $TestType"
    }
    
    Write-Verbose "Test files to execute:"
    $testFiles | ForEach-Object { Write-Verbose "  $_" }
    
    return $testFiles
}

function New-PesterConfiguration {
    param([string[]]$TestFiles)
    
    $config = New-PesterConfiguration
    
    # Set test discovery
    $config.Run.Path = $TestFiles
    
    # Set output verbosity
    if ($Detailed) {
        $config.Output.Verbosity = 'Detailed'
    }
    else {
        $config.Output.Verbosity = 'Normal'
    }
    
    # Set tags
    if ($Tag) {
        $config.Filter.Tag = $Tag
    }
    
    if ($ExcludeTag) {
        $config.Filter.ExcludeTag = $ExcludeTag
    }
    
    # Configure test results output
    if ($OutputFormat -in @('JUnit', 'All')) {
        $config.TestResult.Enabled = $true
        $config.TestResult.OutputFormat = 'JUnitXml'
        $config.TestResult.OutputPath = Join-Path $script:TestConfig.OutputPath "TestResults.xml"
    }
    
    if ($OutputFormat -in @('NUnit', 'All')) {
        # Pester 5 doesn't have native NUnit support, so we'll use JUnit
        Write-Verbose "NUnit format not directly supported in Pester 5, using JUnit format"
    }
    
    # Enable code coverage if available
    try {
        $config.CodeCoverage.Enabled = $true
        $config.CodeCoverage.Path = Join-Path $script:TestConfig.ModulePath "*.ps*1"
        $config.CodeCoverage.OutputFormat = 'JaCoCo'
        $config.CodeCoverage.OutputPath = Join-Path $script:TestConfig.OutputPath "coverage.xml"
    }
    catch {
        Write-Verbose "Code coverage configuration not available in this Pester version"
    }
    
    return $config
}

function Process-TestResults {
    param($Results)
    
    # Save additional output formats if requested
    if ($OutputFormat -in @('All', 'JUnit') -and $Results.TestResult) {
        $junitPath = Join-Path $script:TestConfig.OutputPath "TestResults.xml"
        Write-Verbose "Test results saved to: $junitPath"
    }
    
    # Generate HTML report if possible
    if ($OutputFormat -eq 'All') {
        try {
            $htmlPath = Join-Path $script:TestConfig.OutputPath "TestReport.html"
            Export-TestResultsToHtml -Results $Results -OutputPath $htmlPath
            Write-Verbose "HTML report saved to: $htmlPath"
        }
        catch {
            Write-Verbose "Could not generate HTML report: $($_.Exception.Message)"
        }
    }
}

function Export-TestResultsToHtml {
    param($Results, $OutputPath)
    
    $html = @"
<!DOCTYPE html>
<html>
<head>
    <title>Claude Profile Manager Test Results</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; }
        .header { background: #2b5797; color: white; padding: 15px; border-radius: 5px; }
        .summary { background: #f8f9fa; padding: 15px; border-radius: 5px; margin: 10px 0; }
        .success { color: #28a745; }
        .failure { color: #dc3545; }
        .warning { color: #ffc107; }
        table { width: 100%; border-collapse: collapse; margin: 10px 0; }
        th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
        th { background: #f8f9fa; }
    </style>
</head>
<body>
    <div class="header">
        <h1>Claude Profile Manager Test Results</h1>
        <p>Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')</p>
    </div>
    
    <div class="summary">
        <h2>Summary</h2>
        <p><strong>Total Tests:</strong> $($Results.TotalCount)</p>
        <p><strong class="success">Passed:</strong> $($Results.PassedCount)</p>
        <p><strong class="failure">Failed:</strong> $($Results.FailedCount)</p>
        <p><strong class="warning">Skipped:</strong> $($Results.SkippedCount)</p>
        <p><strong>Duration:</strong> $([math]::Round($Results.Duration.TotalSeconds, 2)) seconds</p>
    </div>
    
    <h2>Test Details</h2>
    <table>
        <tr>
            <th>Test</th>
            <th>Result</th>
            <th>Duration</th>
            <th>Error</th>
        </tr>
"@
    
    foreach ($test in $Results.Tests) {
        $resultClass = switch ($test.Result) {
            'Passed' { 'success' }
            'Failed' { 'failure' }
            default { 'warning' }
        }
        
        $error = if ($test.ErrorRecord) { $test.ErrorRecord.Exception.Message } else { '' }
        $duration = [math]::Round($test.Duration.TotalMilliseconds, 0)
        
        $html += @"
        <tr>
            <td>$($test.Name)</td>
            <td class="$resultClass">$($test.Result)</td>
            <td>${duration}ms</td>
            <td>$error</td>
        </tr>
"@
    }
    
    $html += @"
    </table>
</body>
</html>
"@
    
    $html | Set-Content -Path $OutputPath -Encoding UTF8
}

function Show-TestSummary {
    param($Results)
    
    $duration = $Results.Duration.TotalSeconds
    
    Write-Host
    Write-Host "Test Summary:" -ForegroundColor Cyan
    Write-Host "  Total Tests: $($Results.TotalCount)" -ForegroundColor Gray
    
    if ($Results.PassedCount -gt 0) {
        Write-Host "  ✓ Passed: $($Results.PassedCount)" -ForegroundColor Green
    }
    
    if ($Results.FailedCount -gt 0) {
        Write-Host "  ✗ Failed: $($Results.FailedCount)" -ForegroundColor Red
    }
    
    if ($Results.SkippedCount -gt 0) {
        Write-Host "  - Skipped: $($Results.SkippedCount)" -ForegroundColor Yellow
    }
    
    Write-Host "  Duration: $([math]::Round($duration, 2)) seconds" -ForegroundColor Gray
    
    # Show coverage information if available
    if ($Results.CodeCoverage) {
        $coveragePercent = [math]::Round(($Results.CodeCoverage.CoveredPercent), 2)
        $coverageColor = if ($coveragePercent -ge 80) { 'Green' } elseif ($coveragePercent -ge 60) { 'Yellow' } else { 'Red' }
        Write-Host "  Coverage: $coveragePercent%" -ForegroundColor $coverageColor
    }
    
    Write-Host
    
    if ($Results.FailedCount -eq 0) {
        Write-Host "✓ All tests passed!" -ForegroundColor Green
    }
    else {
        Write-Host "✗ Some tests failed. See output above for details." -ForegroundColor Red
    }
}

# Main execution
$exitCode = Invoke-ClaudeProfileManagerTests
exit $exitCode