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
        (Join-Path $script:ModuleRoot "..\..\ClaudeProfileManager.CLI\bin\Debug\net9.0\win-x64\claude-profile-manager.exe"),
        (Join-Path $script:ModuleRoot "..\..\ClaudeProfileManager.CLI\bin\Release\net9.0\win-x64\claude-profile-manager.exe"),
        (Join-Path $script:ModuleRoot "..\..\dist\claude-profile-manager.exe"),
        
        # Installed location relative to module
        (Join-Path $script:ModuleRoot "..\bin\claude-profile-manager.exe"),
        
        # System PATH
        "claude-profile-manager.exe",
        
        # Common installation directories
        "$env:ProgramFiles\ClaudeProfileManager\claude-profile-manager.exe",
        "$env:ProgramFiles(x86)\ClaudeProfileManager\claude-profile-manager.exe",
        "$env:LOCALAPPDATA\Programs\ClaudeProfileManager\claude-profile-manager.exe",
        
        # Chocolatey installation
        "$env:ChocolateyInstall\bin\claude-profile-manager.exe",
        
        # User profile bin directory  
        "$env:USERPROFILE\.local\bin\claude-profile-manager.exe",
        "$env:USERPROFILE\bin\claude-profile-manager.exe"
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
- $script:ModuleRoot\..\..\ClaudeProfileManager.CLI\bin\Debug\net9.0\win-x64\claude-profile-manager.exe
- $script:ModuleRoot\..\..\ClaudeProfileManager.CLI\bin\Release\net9.0\win-x64\claude-profile-manager.exe
- $script:ModuleRoot\..\..\dist\claude-profile-manager.exe

System PATH or installation directories:
- $env:ProgramFiles\ClaudeProfileManager\claude-profile-manager.exe
- $env:LOCALAPPDATA\Programs\ClaudeProfileManager\claude-profile-manager.exe

To build from source:
1. Navigate to the windows directory
2. Run: dotnet publish --configuration Release -o dist
3. The executable will be available in dist\claude-profile-manager.exe

To install via Chocolatey:
choco install claude-profile-manager
"@
    
    throw [System.IO.FileNotFoundException]::new($errorMessage)
}