function Remove-ClaudeProfileAlias {
    <#
    .SYNOPSIS
        Removes an alias for a Claude Code CLI profile.
    
    .DESCRIPTION
        The Remove-ClaudeProfileAlias cmdlet removes an existing alias for a Claude Code CLI 
        profile. The underlying profile is not affected, only the alias shortcut is removed.
    
    .PARAMETER Alias
        The name of the alias to remove.
    
    .PARAMETER Force
        Remove the alias without prompting for confirmation.
    
    .PARAMETER PassThru
        Return information about the removed alias.
    
    .OUTPUTS
        None by default. Alias information when -PassThru is specified.
    
    .EXAMPLE
        Remove-ClaudeProfileAlias -Alias "old-alias"
        
        Removes the "old-alias" after confirming the operation.
    
    .EXAMPLE
        Remove-ClaudeProfileAlias -Alias "temp" -Force
        
        Removes the "temp" alias without confirmation.
    
    .EXAMPLE
        Get-ClaudeProfileAlias -ProfileName "deprecated-profile" | Remove-ClaudeProfileAlias -Force
        
        Removes all aliases that point to the "deprecated-profile" profile.
    
    .EXAMPLE
        $removed = Remove-ClaudeProfileAlias -Alias "w" -Force -PassThru
        Write-Host "Removed alias '$($removed.Alias)' that pointed to '$($removed.ProfileName)'"
        
        Removes an alias and returns information about what was removed.
    
    .NOTES
        - Only the alias is removed; the underlying profile is not affected
        - Use Get-ClaudeProfileAlias to see existing aliases before removal
        - Multiple aliases can point to the same profile
    
    .LINK
        Set-ClaudeProfileAlias
        Get-ClaudeProfileAlias
        Get-ClaudeProfile
    #>
    
    [CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'Medium')]
    [OutputType([void], ParameterSetName = 'Default')]
    [OutputType([PSCustomObject], ParameterSetName = 'PassThru')]
    param(
        [Parameter(
            Mandatory = $true,
            Position = 0,
            ValueFromPipeline = $true,
            ValueFromPipelineByPropertyName = $true,
            HelpMessage = "The name of the alias to remove"
        )]
        [ValidateNotNullOrEmpty()]
        [string]$Alias,
        
        [Parameter(Mandatory = $false)]
        [switch]$Force,
        
        [Parameter(Mandatory = $false, ParameterSetName = 'PassThru')]
        [switch]$PassThru
    )
    
    begin {
        Write-Verbose "Starting Remove-ClaudeProfileAlias"
    }
    
    process {
        try {
            Write-Verbose "Attempting to remove alias: $Alias"
            
            # Get alias information before removal (for PassThru and validation)
            $aliasToRemove = Get-ClaudeProfileAlias -Alias $Alias -ErrorAction SilentlyContinue
            
            if (-not $aliasToRemove) {
                Write-Warning "Alias '$Alias' not found"
                return
            }
            
            Write-Verbose "Found alias '$Alias' pointing to '$($aliasToRemove.ProfileName)'"
            
            # Determine confirmation preference
            $confirmPreference = if ($Force) { 'None' } else { 'Medium' }
            
            # Build confirmation message
            $confirmMessage = "Remove alias '$Alias' (points to '$($aliasToRemove.ProfileName)')"
            
            # Confirm the action
            if ($PSCmdlet.ShouldProcess($confirmMessage, "Remove Claude Profile Alias", $confirmPreference)) {
                # Build CLI arguments
                $cliArgs = @('unalias', $Alias)
                
                if ($Force) {
                    $cliArgs += '--force'
                }
                
                Write-Verbose "Executing CLI unalias command"
                $output = Invoke-ClaudeProfileCLI -Arguments $cliArgs -ThrowOnError
                
                Write-Host "✓ Successfully removed alias '$Alias'" -ForegroundColor Green
                
                # Return alias information if requested
                if ($PassThru) {
                    Write-Verbose "PassThru requested, returning removed alias information"
                    return $aliasToRemove
                }
            }
            else {
                Write-Verbose "User cancelled alias removal"
            }
        }
        catch {
            $errorMessage = "Failed to remove Claude profile alias '$Alias': $($_.Exception.Message)"
            Write-Error $errorMessage -Category InvalidOperation -ErrorAction Stop
        }
    }
    
    end {
        Write-Verbose "Remove-ClaudeProfileAlias completed"
    }
}