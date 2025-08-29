function Get-CurrentClaudeProfile {
    <#
    .SYNOPSIS
        Gets the currently active Claude profile.
    
    .DESCRIPTION
        The Get-CurrentClaudeProfile cmdlet shows which Claude Code CLI profile
        is currently active. This is a PowerShell wrapper around the 
        claude-profile-manager CLI tool.
    
    .OUTPUTS
        Current profile name displayed in console.
    
    .EXAMPLE
        Get-CurrentClaudeProfile
        
        Shows the currently active profile.
    
    .NOTES
        This is a thin wrapper around claude-profile-manager.exe CLI tool.
    
    .LINK
        Save-ClaudeProfile
        Switch-ClaudeProfile
        Get-ClaudeProfile
    #>
    
    [CmdletBinding()]
    param()
    
    try {
        # Get CLI path
        $cliPath = Get-ClaudeProfileCLIPath
        
        # Build arguments
        $args = @('current')
        
        # Execute CLI
        Write-Verbose "Executing: $cliPath $($args -join ' ')"
        $result = & $cliPath @args 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host $result -ForegroundColor Cyan
        } else {
            throw "CLI command failed: $result"
        }
    }
    catch {
        Write-Error "Failed to get current Claude profile: $($_.Exception.Message)"
        throw
    }
}