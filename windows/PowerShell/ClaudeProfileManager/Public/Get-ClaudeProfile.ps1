function Get-ClaudeProfile {
    <#
    .SYNOPSIS
        Retrieves Claude Code CLI profiles with their status and metadata.
    
    .DESCRIPTION
        The Get-ClaudeProfile cmdlet lists all saved Claude Code CLI profiles or retrieves
        information about a specific profile. It shows authentication method, creation date,
        last used date, current status, token health, and associated aliases.
    
    .PARAMETER Name
        The name of a specific profile to retrieve. If not specified, all profiles are returned.
    
    .PARAMETER IncludeAliases
        Include alias information in the output. This shows which aliases point to each profile.
    
    .PARAMETER Current
        Return only the currently active profile.
    
    .OUTPUTS
        ClaudeProfileManager.Profile[]
        Returns an array of profile objects with the following properties:
        - Name: Profile name
        - AuthMethod: Authentication method (Console, Subscription, etc.)
        - Created: When the profile was created
        - LastUsed: When the profile was last used
        - IsCurrent: Whether this is the currently active profile
        - TokenHealth: Health status of the authentication token
        - Aliases: Array of aliases for this profile
    
    .EXAMPLE
        Get-ClaudeProfile
        
        Lists all saved profiles with their status information.
    
    .EXAMPLE
        Get-ClaudeProfile -Name "work"
        
        Retrieves information about the "work" profile specifically.
    
    .EXAMPLE
        Get-ClaudeProfile -Current
        
        Returns only the currently active profile.
    
    .EXAMPLE
        Get-ClaudeProfile | Where-Object { $_.AuthMethod -eq 'Subscription' }
        
        Gets all profiles that use OAuth subscription authentication.
    
    .EXAMPLE
        Get-ClaudeProfile | Sort-Object LastUsed -Descending | Select-Object -First 5
        
        Shows the 5 most recently used profiles.
    
    .NOTES
        - Returns properly typed PowerShell objects that work well with formatting and pipelines
        - The TokenHealth property shows expiration information for subscription profiles
        - IsCurrent indicates which profile is currently active in Claude Code CLI
        - Created and LastUsed are DateTime objects for easy sorting and filtering
    
    .LINK
        Save-ClaudeProfile
        Switch-ClaudeProfile
        Remove-ClaudeProfile
    #>
    
    [CmdletBinding(DefaultParameterSetName = 'All')]
    [OutputType([PSCustomObject[]])]
    param(
        [Parameter(
            Mandatory = $false,
            Position = 0,
            ParameterSetName = 'Specific',
            ValueFromPipeline = $true,
            ValueFromPipelineByPropertyName = $true,
            HelpMessage = "The name of the profile to retrieve"
        )]
        [ValidateNotNullOrEmpty()]
        [string]$Name,
        
        [Parameter(Mandatory = $false)]
        [switch]$IncludeAliases,
        
        [Parameter(
            Mandatory = $false,
            ParameterSetName = 'Current'
        )]
        [switch]$Current
    )
    
    begin {
        Write-Verbose "Starting Get-ClaudeProfile"
        if ($Name) {
            Write-Verbose "Retrieving specific profile: $Name"
        }
        elseif ($Current) {
            Write-Verbose "Retrieving current profile only"
        }
        else {
            Write-Verbose "Retrieving all profiles"
        }
    }
    
    process {
        try {
            # Build CLI arguments based on parameter set
            $cliArgs = @('list')
            
            if ($Name) {
                # Get specific profile (CLI doesn't have this directly, so we'll filter)
                Write-Verbose "Getting specific profile '$Name'"
            }
            elseif ($Current) {
                # Get current profile
                $currentOutput = Invoke-ClaudeProfileCLI -Arguments @('current') -ThrowOnError
                if ($currentOutput -and $currentOutput -ne "No current profile set.") {
                    # Get the current profile name and then get its details
                    $currentProfileName = $currentOutput.Trim()
                    Write-Verbose "Current profile is: $currentProfileName"
                    $Name = $currentProfileName
                }
                else {
                    Write-Verbose "No current profile is set"
                    return @()
                }
            }
            
            # Execute CLI command to get profiles
            Write-Verbose "Executing CLI list command"
            $rawOutput = Invoke-ClaudeProfileCLI -Arguments $cliArgs -ParseJsonOutput -ThrowOnError
            
            if (-not $rawOutput) {
                Write-Verbose "No profiles found"
                return @()
            }
            
            # Convert CLI output to PowerShell objects
            $profiles = ConvertTo-PowerShellObject -InputObject $rawOutput -ObjectType 'Profile'
            
            # Filter to specific profile if requested
            if ($Name) {
                $profiles = $profiles | Where-Object { $_.Name -eq $Name }
                if (-not $profiles) {
                    Write-Warning "Profile '$Name' not found"
                    return @()
                }
            }
            
            # Add alias information if requested
            if ($IncludeAliases) {
                Write-Verbose "Including alias information"
                try {
                    $aliasOutput = Invoke-ClaudeProfileCLI -Arguments @('aliases') -ParseJsonOutput -ThrowOnError:$false
                    if ($aliasOutput) {
                        $aliases = ConvertTo-PowerShellObject -InputObject $aliasOutput -ObjectType 'Alias'
                        
                        # Add aliases to each profile
                        foreach ($profile in $profiles) {
                            $profileAliases = $aliases | Where-Object { $_.ProfileName -eq $profile.Name } | Select-Object -ExpandProperty Alias
                            $profile.Aliases = if ($profileAliases) { [string[]]$profileAliases } else { @() }
                        }
                    }
                }
                catch {
                    Write-Verbose "Could not retrieve alias information: $($_.Exception.Message)"
                }
            }
            
            # Ensure consistent return type
            if ($profiles -is [array] -and $profiles.Count -eq 1 -and $Name) {
                return $profiles[0]
            }
            else {
                return $profiles
            }
        }
        catch {
            $errorMessage = "Failed to retrieve Claude profiles: $($_.Exception.Message)"
            Write-Error $errorMessage -Category InvalidOperation -ErrorAction Stop
        }
    }
    
    end {
        Write-Verbose "Get-ClaudeProfile completed"
    }
}