function Get-ClaudeProfileCLIPath {
    <#
    .SYNOPSIS
        Locates the Claude Profile Manager CLI executable.
    
    .DESCRIPTION
        Searches for the Claude Profile Manager CLI executable in common locations
        and returns the full path if found. Caches the result for performance.
    
    .OUTPUTS
        System.String
        Returns the full path to the CLI executable, or throws if not found.
    
    .EXAMPLE
        $cliPath = Get-ClaudeProfileCLIPath
        Write-Host "CLI found at: $cliPath"
    #>
    
    [CmdletBinding()]
    [OutputType([string])]
    param()
    
    # Return cached path if available
    if ($script:CLIExecutablePath -and (Test-Path $script:CLIExecutablePath)) {
        return $script:CLIExecutablePath
    }
    
    Write-Verbose "Searching for Claude Profile Manager CLI executable..."
    
    # Search locations in priority order
    $searchPaths = @(
        # Local development build
        (Join-Path $script:ModuleRoot "..\ClaudeProfileManager.CLI\bin\Debug\net9.0\ClaudeProfileManager.CLI.exe"),
        (Join-Path $script:ModuleRoot "..\ClaudeProfileManager.CLI\bin\Release\net9.0\ClaudeProfileManager.CLI.exe"),
        
        # Installed location relative to module
        (Join-Path $script:ModuleRoot "..\bin\ClaudeProfileManager.CLI.exe"),
        
        # System PATH
        "ClaudeProfileManager.CLI.exe",
        
        # Common installation directories
        "$env:ProgramFiles\ClaudeProfileManager\ClaudeProfileManager.CLI.exe",
        "$env:ProgramFiles(x86)\ClaudeProfileManager\ClaudeProfileManager.CLI.exe",
        "$env:LOCALAPPDATA\Programs\ClaudeProfileManager\ClaudeProfileManager.CLI.exe",
        
        # Chocolatey installation
        "$env:ChocolateyInstall\bin\ClaudeProfileManager.CLI.exe",
        
        # User profile bin directory  
        "$env:USERPROFILE\.local\bin\ClaudeProfileManager.CLI.exe",
        "$env:USERPROFILE\bin\ClaudeProfileManager.CLI.exe"
    )
    
    foreach ($path in $searchPaths) {
        $resolvedPath = $null
        
        try {
            if ([System.IO.Path]::IsPathRooted($path)) {
                # Absolute path - test directly
                if (Test-Path $path -PathType Leaf) {
                    $resolvedPath = $path
                }
            }
            else {
                # Relative path or command name - use Get-Command
                $command = Get-Command $path -ErrorAction SilentlyContinue -CommandType Application
                if ($command) {
                    $resolvedPath = $command.Source
                }
            }
            
            if ($resolvedPath) {
                Write-Verbose "Found CLI executable at: $resolvedPath"
                
                # Verify it's actually our CLI by checking version
                try {
                    $versionOutput = & $resolvedPath --version 2>&1
                    if ($versionOutput -match "ClaudeProfileManager|Claude Profile Manager") {
                        # Cache the path for future use
                        $script:CLIExecutablePath = $resolvedPath
                        return $resolvedPath
                    }
                    else {
                        Write-Verbose "Found executable at $resolvedPath but version check failed: $versionOutput"
                    }
                }
                catch {
                    Write-Verbose "Found executable at $resolvedPath but version check threw exception: $($_.Exception.Message)"
                }
            }
        }
        catch {
            Write-Verbose "Error checking path $path : $($_.Exception.Message)"
        }
    }
    
    # If we get here, CLI was not found
    $errorMessage = @"
Claude Profile Manager CLI executable not found. Please ensure it is installed and available in one of these locations:

Development locations:
- $script:ModuleRoot\..\ClaudeProfileManager.CLI\bin\Debug\net9.0\ClaudeProfileManager.CLI.exe
- $script:ModuleRoot\..\ClaudeProfileManager.CLI\bin\Release\net9.0\ClaudeProfileManager.CLI.exe

System PATH or installation directories:
- $env:ProgramFiles\ClaudeProfileManager\ClaudeProfileManager.CLI.exe
- $env:LOCALAPPDATA\Programs\ClaudeProfileManager\ClaudeProfileManager.CLI.exe

To build from source:
1. Navigate to the windows directory
2. Run: dotnet build --configuration Release
3. The executable will be available in ClaudeProfileManager.CLI\bin\Release\net9.0\

To install via Chocolatey (when available):
choco install claude-profile-manager
"@
    
    throw [System.IO.FileNotFoundException]::new($errorMessage)
}