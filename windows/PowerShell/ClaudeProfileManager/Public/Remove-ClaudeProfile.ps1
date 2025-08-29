function Remove-ClaudeProfile {
    <#
    .SYNOPSIS
        Removes a Claude profile and its associated credentials.
    
    .DESCRIPTION
        The Remove-ClaudeProfile cmdlet deletes a saved Claude Code CLI profile
        and its associated credentials from the system. This is a PowerShell
        wrapper around the claude-profile-manager CLI tool.
    
    .PARAMETER Name
        The name of the profile to remove.
    
    .PARAMETER Force
        Skip confirmation prompt.
    
    .OUTPUTS
        None. Success/failure is indicated through Write-Host messages.
    
    .EXAMPLE
        Remove-ClaudeProfile -Name "old-work"
        
        Removes the "old-work" profile with confirmation prompt.
    
    .EXAMPLE
        Remove-ClaudeProfile "temp" -Force
        
        Removes the "temp" profile without confirmation.
    
    .NOTES
        This is a thin wrapper around claude-profile-manager.exe CLI tool.
    
    .LINK
        Save-ClaudeProfile
        Get-ClaudeProfile
        Switch-ClaudeProfile
    #>
    
    [CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
    param(
        [Parameter(Mandatory = $true, Position = 0)]
        [string]$Name,
        
        [Parameter()]
        [switch]$Force
    )
    
    try {
        # Get CLI path
        $cliPath = Get-ClaudeProfileCLIPath
        
        # Build arguments
        $args = @('delete', $Name)
        if ($Force) {
            $args += '--force'
        }
        
        # Confirm the action
        if ($Force -or $PSCmdlet.ShouldProcess($Name, "Remove Claude Profile")) {
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
        Write-Error "Failed to remove Claude profile: $($_.Exception.Message)"
        throw
    }
}