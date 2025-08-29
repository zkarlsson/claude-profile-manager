function Save-ClaudeProfile {
    <#
    .SYNOPSIS
        Saves the current Claude Code CLI credentials as a named profile.
    
    .DESCRIPTION
        The Save-ClaudeProfile cmdlet captures the current Claude Code CLI authentication 
        credentials and saves them as a named profile for later use. This is a PowerShell
        wrapper around the claude-profile-manager CLI tool.
    
    .PARAMETER Name
        The name for the profile. If not specified, uses the current active profile.
    
    .PARAMETER Aliases
        Optional array of aliases for quick access to this profile.
    
    .OUTPUTS
        None. Success/failure is indicated through Write-Host messages.
    
    .EXAMPLE
        Save-ClaudeProfile -Name "work"
        
        Saves the current credentials as a profile named "work".
    
    .EXAMPLE
        Save-ClaudeProfile -Name "personal" -Aliases @("p", "home")
        
        Saves the current credentials as "personal" with aliases "p" and "home".
    
    .EXAMPLE
        Save-ClaudeProfile
        
        Saves credentials to the current active profile (requires existing profile).
    
    .NOTES
        This is a thin wrapper around claude-profile-manager.exe CLI tool.
    
    .LINK
        Get-ClaudeProfile
        Switch-ClaudeProfile
        Remove-ClaudeProfile
    #>
    
    [CmdletBinding()]
    param(
        [Parameter(Position = 0)]
        [string]$Name,
        
        [Parameter()]
        [string[]]$Aliases = @()
    )
    
    try {
        # Get CLI path
        $cliPath = Get-ClaudeProfileCLIPath
        
        # Build arguments
        $args = @('save')
        if ($Name) {
            $args += $Name
        }
        if ($Aliases.Count -gt 0) {
            $args += '--aliases'
            $args += $Aliases
        }
        
        # Execute CLI
        Write-Verbose "Executing: $cliPath $($args -join ' ')"
        $result = & $cliPath @args 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host $result -ForegroundColor Green
        } else {
            throw "CLI command failed: $result"
        }
    }
    catch {
        Write-Error "Failed to save Claude profile: $($_.Exception.Message)"
        throw
    }
}