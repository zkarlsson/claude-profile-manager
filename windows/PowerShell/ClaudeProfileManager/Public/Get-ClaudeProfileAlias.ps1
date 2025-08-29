function Get-ClaudeProfileAlias {
    <#
    .SYNOPSIS
        Lists Claude profile aliases.
    
    .DESCRIPTION
        The Get-ClaudeProfileAlias cmdlet lists all defined aliases for Claude Code CLI 
        profiles. This is a PowerShell wrapper around the claude-profile-manager CLI tool.
    
    .PARAMETER Alias
        The name of a specific alias to retrieve. If not specified, all aliases are returned.
    
    .OUTPUTS
        Alias information displayed in console.
    
    .EXAMPLE
        Get-ClaudeProfileAlias
        
        Lists all defined aliases and their target profiles.
    
    .NOTES
        This is a thin wrapper around claude-profile-manager.exe CLI tool.
    
    .LINK
        Set-ClaudeProfileAlias
        Remove-ClaudeProfileAlias
        Switch-ClaudeProfile
    #>
    
    [CmdletBinding()]
    param(
        [Parameter(Position = 0)]
        [string]$Alias
    )
    
    try {
        # Get CLI path
        $cliPath = Get-ClaudeProfileCLIPath
        
        # Build arguments
        $args = @('aliases')
        
        # Execute CLI
        Write-Verbose "Executing: $cliPath $($args -join ' ')"
        $result = & $cliPath @args 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            if ($Alias) {
                # Filter output for specific alias if requested
                $lines = $result -split "`n"
                $filtered = $lines | Where-Object { $_ -match "^\s*$([regex]::Escape($Alias))\s+" }
                if ($filtered) {
                    Write-Host $filtered
                } else {
                    Write-Warning "Alias '$Alias' not found"
                }
            } else {
                Write-Host $result
            }
        } else {
            throw "CLI command failed: $result"
        }
    }
    catch {
        Write-Error "Failed to get Claude profile aliases: $($_.Exception.Message)"
        throw
    }
}