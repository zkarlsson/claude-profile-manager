function Switch-ClaudeProfile {
    <#
    .SYNOPSIS
        Switches the active Claude Code CLI profile to the specified profile.
    
    .DESCRIPTION
        The Switch-ClaudeProfile cmdlet changes the active authentication profile 
        for Claude Code CLI. This is a PowerShell wrapper around the 
        claude-profile-manager CLI tool.
    
    .PARAMETER Name
        The name of the profile to switch to. This can be either a profile name or an alias.
    
    .OUTPUTS
        None. Success/failure is indicated through Write-Host messages.
    
    .EXAMPLE
        Switch-ClaudeProfile -Name "work"
        
        Switches to the "work" profile.
    
    .EXAMPLE
        Switch-ClaudeProfile "personal"
        
        Switches to the "personal" profile using positional parameter.
    
    .NOTES
        This is a thin wrapper around claude-profile-manager.exe CLI tool.
        You may need to restart Claude Code CLI after switching profiles.
    
    .LINK
        Save-ClaudeProfile
        Get-ClaudeProfile
        Remove-ClaudeProfile
    #>
    
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true, Position = 0)]
        [string]$Name
    )
    
    try {
        # Get CLI path
        $cliPath = Get-ClaudeProfileCLIPath
        
        # Build arguments
        $args = @('switch', $Name)
        
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
        Write-Error "Failed to switch Claude profile: $($_.Exception.Message)"
        throw
    }
}