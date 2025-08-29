function Get-ClaudeProfile {
    <#
    .SYNOPSIS
        Lists all Claude profiles or gets a specific profile.
    
    .DESCRIPTION
        The Get-ClaudeProfile cmdlet lists all saved Claude Code CLI profiles
        or retrieves information about a specific profile. This is a PowerShell
        wrapper around the claude-profile-manager CLI tool.
    
    .PARAMETER Name
        The name of a specific profile to retrieve. If not specified, lists all profiles.
    
    .OUTPUTS
        Profile information displayed in console.
    
    .EXAMPLE
        Get-ClaudeProfile
        
        Lists all saved profiles with their status.
    
    .EXAMPLE
        Get-ClaudeProfile -Name "work"
        
        Gets information about the "work" profile specifically.
    
    .NOTES
        This is a thin wrapper around claude-profile-manager.exe CLI tool.
    
    .LINK
        Save-ClaudeProfile
        Switch-ClaudeProfile
        Remove-ClaudeProfile
    #>
    
    [CmdletBinding()]
    param(
        [Parameter(Position = 0)]
        [string]$Name
    )
    
    try {
        # Get CLI path
        $cliPath = Get-ClaudeProfileCLIPath
        
        # Build arguments - always use list command for simplicity
        $args = @('list')
        
        # Execute CLI
        Write-Verbose "Executing: $cliPath $($args -join ' ')"
        $result = & $cliPath @args 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            if ($Name) {
                # Filter output for specific profile if requested
                $lines = $result -split "`n"
                $filtered = $lines | Where-Object { $_ -match "^\s*➤?\s*$([regex]::Escape($Name))" }
                if ($filtered) {
                    # Show header and matching profile
                    Write-Host $lines[0] # Header
                    Write-Host $filtered
                } else {
                    Write-Warning "Profile '$Name' not found"
                }
            } else {
                Write-Host $result
            }
        } else {
            throw "CLI command failed: $result"
        }
    }
    catch {
        Write-Error "Failed to get Claude profile: $($_.Exception.Message)"
        throw
    }
}