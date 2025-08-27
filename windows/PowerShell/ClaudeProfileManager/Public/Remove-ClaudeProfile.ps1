function Remove-ClaudeProfile {
    <#
    .SYNOPSIS
        Removes a Claude Code CLI profile and its associated credentials.
    
    .DESCRIPTION
        The Remove-ClaudeProfile cmdlet permanently deletes a saved profile and 
        removes its credentials from Windows Credential Manager. This operation 
        cannot be undone.
        
        If the profile being removed is currently active, you will be prompted 
        to confirm the operation as it will leave no active profile.
    
    .PARAMETER Name
        The name of the profile to remove.
    
    .PARAMETER Force
        Remove the profile without prompting for confirmation.
    
    .PARAMETER PassThru
        Return information about the removed profile.
    
    .OUTPUTS
        None by default. Profile information when -PassThru is specified.
    
    .EXAMPLE
        Remove-ClaudeProfile -Name "old-work"
        
        Removes the "old-work" profile after confirming the operation.
    
    .EXAMPLE
        Remove-ClaudeProfile -Name "temp-profile" -Force
        
        Removes the "temp-profile" profile without confirmation.
    
    .EXAMPLE
        Get-ClaudeProfile | Where-Object { $_.LastUsed -lt (Get-Date).AddMonths(-6) } | Remove-ClaudeProfile -Force
        
        Removes all profiles that haven't been used in the last 6 months.
    
    .EXAMPLE
        $removed = Remove-ClaudeProfile -Name "deprecated" -Force -PassThru
        Write-Host "Removed profile: $($removed.Name) (was $($removed.AuthMethod))"
        
        Removes a profile and returns information about what was removed.
    
    .NOTES
        - This operation permanently deletes the profile and its credentials
        - If removing the currently active profile, no profile will be active afterward
        - Credentials are securely removed from Windows Credential Manager
        - Associated aliases are also removed when a profile is deleted
    
    .LINK
        Save-ClaudeProfile
        Get-ClaudeProfile
        Switch-ClaudeProfile
    #>
    
    [CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
    [OutputType([void], ParameterSetName = 'Default')]
    [OutputType([PSCustomObject], ParameterSetName = 'PassThru')]
    param(
        [Parameter(
            Mandatory = $true,
            Position = 0,
            ValueFromPipeline = $true,
            ValueFromPipelineByPropertyName = $true,
            HelpMessage = "The name of the profile to remove"
        )]
        [ValidateNotNullOrEmpty()]
        [string]$Name,
        
        [Parameter(Mandatory = $false)]
        [switch]$Force,
        
        [Parameter(Mandatory = $false, ParameterSetName = 'PassThru')]
        [switch]$PassThru
    )
    
    begin {
        Write-Verbose "Starting Remove-ClaudeProfile"
    }
    
    process {
        try {
            Write-Verbose "Attempting to remove profile: $Name"
            
            # Get profile information before removal (for PassThru and validation)
            $profileToRemove = Get-ClaudeProfile -Name $Name -ErrorAction SilentlyContinue
            
            if (-not $profileToRemove) {
                Write-Warning "Profile '$Name' not found"
                return
            }
            
            # Check if this is the current profile
            $isCurrentProfile = $profileToRemove.IsCurrent
            if ($isCurrentProfile) {
                Write-Warning "Profile '$Name' is currently active. Removing it will leave no active profile."
            }
            
            # Determine confirm preference
            $confirmPreference = 'High'
            if ($Force) {
                $confirmPreference = 'None'
            }
            elseif ($isCurrentProfile) {
                $confirmPreference = 'High'
            }
            
            # Build confirmation message
            $confirmMessage = "Remove profile '$Name'"
            if ($isCurrentProfile) {
                $confirmMessage += " (currently active)"
            }
            $confirmMessage += " and its stored credentials"
            
            # Confirm the action
            if ($PSCmdlet.ShouldProcess($confirmMessage, "Remove Claude Profile", $confirmPreference)) {
                # Build CLI arguments
                $cliArgs = @('delete', $Name)
                
                if ($Force) {
                    $cliArgs += '--force'
                }
                
                Write-Verbose "Executing CLI delete command"
                $output = Invoke-ClaudeProfileCLI -Arguments $cliArgs -ThrowOnError
                
                Write-Host "✓ Successfully removed profile '$Name'" -ForegroundColor Green
                
                # Show warning if this was the current profile
                if ($isCurrentProfile) {
                    Write-Warning "No profile is now active. Use Switch-ClaudeProfile to activate a profile."
                }
                
                # Return profile information if requested
                if ($PassThru) {
                    Write-Verbose "PassThru requested, returning removed profile information"
                    return $profileToRemove
                }
            }
            else {
                Write-Verbose "User cancelled profile removal"
            }
        }
        catch {
            $errorMessage = "Failed to remove Claude profile '$Name': $($_.Exception.Message)"
            Write-Error $errorMessage -Category InvalidOperation -ErrorAction Stop
        }
    }
    
    end {
        Write-Verbose "Remove-ClaudeProfile completed"
    }
}