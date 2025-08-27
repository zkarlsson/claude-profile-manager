function Switch-ClaudeProfile {
    <#
    .SYNOPSIS
        Switches the active Claude Code CLI profile to the specified profile.
    
    .DESCRIPTION
        The Switch-ClaudeProfile cmdlet changes the active authentication profile 
        for Claude Code CLI. It loads the credentials from the specified profile 
        and makes them active for subsequent Claude Code CLI operations.
        
        After switching profiles, you may need to restart your Claude Code CLI 
        session to pick up the new credentials.
    
    .PARAMETER Name
        The name of the profile to switch to. This can be either a profile name or an alias.
    
    .PARAMETER PassThru
        Return the switched-to profile object.
    
    .OUTPUTS
        None by default. ClaudeProfileManager.Profile when -PassThru is specified.
    
    .EXAMPLE
        Switch-ClaudeProfile -Name "work"
        
        Switches to the "work" profile.
    
    .EXAMPLE
        Switch-ClaudeProfile -Name "p"
        
        Switches to the profile associated with alias "p".
    
    .EXAMPLE
        $currentProfile = Switch-ClaudeProfile -Name "personal" -PassThru
        Write-Host "Switched to: $($currentProfile.Name) ($($currentProfile.AuthMethod))"
        
        Switches profiles and returns the profile object for further processing.
    
    .EXAMPLE
        Get-ClaudeProfile | Where-Object { $_.LastUsed -lt (Get-Date).AddDays(-7) } | Switch-ClaudeProfile
        
        Switches to profiles that haven't been used in the last 7 days (pipeline input).
    
    .NOTES
        - You may need to restart Claude Code CLI after switching profiles
        - The profile name can be either a direct profile name or an alias
        - Switching updates the LastUsed timestamp for the profile
        - The previous profile's credentials are automatically saved before switching
    
    .LINK
        Save-ClaudeProfile
        Get-ClaudeProfile
        Get-CurrentClaudeProfile
    #>
    
    [CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'Low')]
    [OutputType([void], ParameterSetName = 'Default')]
    [OutputType([PSCustomObject], ParameterSetName = 'PassThru')]
    param(
        [Parameter(
            Mandatory = $true,
            Position = 0,
            ValueFromPipeline = $true,
            ValueFromPipelineByPropertyName = $true,
            HelpMessage = "The name of the profile to switch to"
        )]
        [ValidateNotNullOrEmpty()]
        [string]$Name,
        
        [Parameter(Mandatory = $false, ParameterSetName = 'PassThru')]
        [switch]$PassThru
    )
    
    begin {
        Write-Verbose "Starting Switch-ClaudeProfile"
    }
    
    process {
        try {
            Write-Verbose "Switching to profile: $Name"
            
            # Check if the profile exists (either as profile name or alias)
            $allProfiles = Get-ClaudeProfile -ErrorAction SilentlyContinue
            $targetProfile = $allProfiles | Where-Object { $_.Name -eq $Name }
            
            if (-not $targetProfile) {
                # Check if it's an alias
                try {
                    $aliasOutput = Invoke-ClaudeProfileCLI -Arguments @('aliases') -ParseJsonOutput -ThrowOnError:$false
                    if ($aliasOutput -and $aliasOutput.$Name) {
                        $actualProfileName = $aliasOutput.$Name
                        Write-Verbose "Resolved alias '$Name' to profile '$actualProfileName'"
                        $targetProfile = $allProfiles | Where-Object { $_.Name -eq $actualProfileName }
                        $Name = $actualProfileName  # Use the actual profile name for CLI command
                    }
                }
                catch {
                    Write-Verbose "Could not check aliases: $($_.Exception.Message)"
                }
            }
            
            if (-not $targetProfile) {
                throw "Profile '$Name' not found. Use Get-ClaudeProfile to see available profiles."
            }
            
            # Confirm the action
            if ($PSCmdlet.ShouldProcess($Name, "Switch Claude Profile")) {
                # Build CLI arguments
                $cliArgs = @('switch', $Name)
                
                Write-Verbose "Executing CLI switch command"
                $output = Invoke-ClaudeProfileCLI -Arguments $cliArgs -ThrowOnError
                
                Write-Host "✓ Switched to profile '$Name'" -ForegroundColor Green
                
                # Show helpful reminder about restarting CLI
                Write-Host "💡 You may need to restart Claude Code CLI to use the new credentials" -ForegroundColor Cyan
                
                # Return profile object if requested
                if ($PassThru) {
                    Write-Verbose "PassThru requested, returning switched profile"
                    return $targetProfile
                }
            }
        }
        catch {
            $errorMessage = "Failed to switch to Claude profile '$Name': $($_.Exception.Message)"
            Write-Error $errorMessage -Category InvalidOperation -ErrorAction Stop
        }
    }
    
    end {
        Write-Verbose "Switch-ClaudeProfile completed"
    }
}