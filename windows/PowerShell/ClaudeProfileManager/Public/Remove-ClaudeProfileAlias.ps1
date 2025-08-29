function Remove-ClaudeProfileAlias {
    <#
    .SYNOPSIS
        Removes a Claude profile alias.
    
    .DESCRIPTION
        The Remove-ClaudeProfileAlias cmdlet removes an existing alias for a Claude Code CLI 
        profile. This is a PowerShell wrapper around the claude-profile-manager CLI tool.
    
    .PARAMETER Alias
        The name of the alias to remove.
    
    .PARAMETER Force
        Remove the alias without prompting for confirmation.
    
    .OUTPUTS
        None. Success/failure is indicated through Write-Host messages.
    
    .EXAMPLE
        Remove-ClaudeProfileAlias -Alias "old-alias"
        
        Removes the "old-alias" after confirming the operation.
    
    .EXAMPLE
        Remove-ClaudeProfileAlias -Alias "temp" -Force
        
        Removes the "temp" alias without confirmation.
    
    .NOTES
        This is a thin wrapper around claude-profile-manager.exe CLI tool.
    
    .LINK
        Set-ClaudeProfileAlias
        Get-ClaudeProfileAlias
    #>
    
    [CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'Medium')]
    param(
        [Parameter(Mandatory = $true, Position = 0)]
        [string]$Alias,
        
        [Parameter()]
        [switch]$Force
    )
    
    try {
        # Get CLI path
        $cliPath = Get-ClaudeProfileCLIPath
        
        # Build arguments
        $args = @('unalias', $Alias)
        if ($Force) {
            $args += '--force'
        }
        
        # Confirm the action
        if ($Force -or $PSCmdlet.ShouldProcess($Alias, "Remove Claude Profile Alias")) {
            # Execute CLI
            Write-Verbose "Executing: $cliPath $($args -join ' ')"
            $result = & $cliPath @args 2>&1
            
            if ($LASTEXITCODE -eq 0) {
                Write-Host $result -ForegroundColor Green
            } else {
                throw "CLI command failed: $result"
            }
        }
    }
    catch {
        Write-Error "Failed to remove Claude profile alias: $($_.Exception.Message)"
        throw
    }
}