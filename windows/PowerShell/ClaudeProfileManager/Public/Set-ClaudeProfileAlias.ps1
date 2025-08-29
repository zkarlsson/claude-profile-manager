function Set-ClaudeProfileAlias {
    <#
    .SYNOPSIS
        Creates an alias for a Claude profile.
    
    .DESCRIPTION
        The Set-ClaudeProfileAlias cmdlet creates an alias for an existing
        Claude Code CLI profile. This is a PowerShell wrapper around the 
        claude-profile-manager CLI tool.
    
    .PARAMETER AliasName
        The alias name to create.
    
    .PARAMETER ProfileName
        The existing profile name to create an alias for.
    
    .OUTPUTS
        None. Success/failure is indicated through Write-Host messages.
    
    .EXAMPLE
        Set-ClaudeProfileAlias -AliasName "w" -ProfileName "work"
        
        Creates alias "w" for the "work" profile.
    
    .NOTES
        This is a thin wrapper around claude-profile-manager.exe CLI tool.
    
    .LINK
        Get-ClaudeProfileAlias
        Remove-ClaudeProfileAlias
    #>
    
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true, Position = 0)]
        [string]$AliasName,
        
        [Parameter(Mandatory = $true, Position = 1)]
        [string]$ProfileName
    )
    
    try {
        # Get CLI path
        $cliPath = Get-ClaudeProfileCLIPath
        
        # Build arguments
        $args = @('alias', $AliasName, $ProfileName)
        
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
        Write-Error "Failed to set Claude profile alias: $($_.Exception.Message)"
        throw
    }
}