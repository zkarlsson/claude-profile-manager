function Get-CurrentClaudeProfile {
    <#
    .SYNOPSIS
        Gets the currently active Claude Code CLI profile.
    
    .DESCRIPTION
        The Get-CurrentClaudeProfile cmdlet returns information about the currently 
        active Claude Code CLI profile. If no profile is currently active, it returns 
        null and displays a warning message.
    
    .PARAMETER Detailed
        Return detailed profile information including creation date, last used, and token health.
    
    .OUTPUTS
        ClaudeProfileManager.Profile or $null
        Returns the current profile object, or null if no profile is active.
    
    .EXAMPLE
        Get-CurrentClaudeProfile
        
        Shows the name of the currently active profile.
    
    .EXAMPLE
        $current = Get-CurrentClaudeProfile -Detailed
        if ($current) {
            Write-Host "Current profile: $($current.Name)"
            Write-Host "Authentication: $($current.AuthMethod)"
            Write-Host "Token health: $($current.TokenHealth)"
        }
        
        Gets detailed information about the current profile.
    
    .EXAMPLE
        if (-not (Get-CurrentClaudeProfile)) {
            Write-Host "No profile is active. Use Switch-ClaudeProfile to activate one."
        }
        
        Checks if any profile is currently active.
    
    .NOTES
        - Returns null if no profile is currently active
        - Use Switch-ClaudeProfile to activate a profile
        - The current profile is the one whose credentials are active in Claude Code CLI
    
    .LINK
        Switch-ClaudeProfile
        Get-ClaudeProfile
        Save-ClaudeProfile
    #>
    
    [CmdletBinding()]
    [OutputType([PSCustomObject], [System.Management.Automation.Internal.AutomationNull])]
    param(
        [Parameter(Mandatory = $false)]
        [switch]$Detailed
    )
    
    begin {
        Write-Verbose "Starting Get-CurrentClaudeProfile"
    }
    
    process {
        try {
            Write-Verbose "Getting current active profile"
            
            # Use the CLI to get the current profile name
            $currentProfileName = Invoke-ClaudeProfileCLI -Arguments @('current') -ThrowOnError:$false
            
            if (-not $currentProfileName -or $currentProfileName -match "No current profile") {
                Write-Verbose "No current profile is set"
                Write-Warning "No profile is currently active. Use Switch-ClaudeProfile to activate a profile."
                return $null
            }
            
            # Clean up the profile name (remove any extra whitespace or formatting)
            $currentProfileName = $currentProfileName.Trim()
            Write-Verbose "Current profile name: $currentProfileName"
            
            if ($Detailed) {
                # Get full profile information
                Write-Verbose "Getting detailed profile information"
                $currentProfile = Get-ClaudeProfile -Name $currentProfileName -ErrorAction SilentlyContinue
                
                if ($currentProfile) {
                    # Ensure the IsCurrent flag is set
                    $currentProfile.IsCurrent = $true
                    return $currentProfile
                }
                else {
                    Write-Warning "Current profile '$currentProfileName' was found but could not retrieve details"
                    # Return basic profile info
                    return [PSCustomObject]@{
                        PSTypeName = 'ClaudeProfileManager.Profile'
                        Name = $currentProfileName
                        AuthMethod = 'Unknown'
                        Created = $null
                        LastUsed = $null
                        IsCurrent = $true
                        TokenHealth = 'Unknown'
                        Aliases = @()
                    }
                }
            }
            else {
                # Return just the profile name as a simple object
                return [PSCustomObject]@{
                    PSTypeName = 'ClaudeProfileManager.Profile'
                    Name = $currentProfileName
                    IsCurrent = $true
                }
            }
        }
        catch {
            Write-Verbose "Error getting current profile: $($_.Exception.Message)"
            Write-Warning "Could not determine the current profile: $($_.Exception.Message)"
            return $null
        }
    }
    
    end {
        Write-Verbose "Get-CurrentClaudeProfile completed"
    }
}