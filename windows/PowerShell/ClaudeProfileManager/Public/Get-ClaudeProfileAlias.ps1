function Get-ClaudeProfileAlias {
    <#
    .SYNOPSIS
        Retrieves Claude Code CLI profile aliases.
    
    .DESCRIPTION
        The Get-ClaudeProfileAlias cmdlet lists all defined aliases for Claude Code CLI 
        profiles, or gets information about a specific alias. Aliases provide shortcuts 
        for profile names and can be used anywhere a profile name is expected.
    
    .PARAMETER Alias
        The name of a specific alias to retrieve. If not specified, all aliases are returned.
    
    .PARAMETER ProfileName
        Filter aliases by the profile they point to.
    
    .OUTPUTS
        ClaudeProfileManager.Alias[]
        Returns an array of alias objects with the following properties:
        - Alias: The alias name
        - ProfileName: The profile name the alias points to
    
    .EXAMPLE
        Get-ClaudeProfileAlias
        
        Lists all defined aliases and their target profiles.
    
    .EXAMPLE
        Get-ClaudeProfileAlias -Alias "w"
        
        Gets information about the "w" alias specifically.
    
    .EXAMPLE
        Get-ClaudeProfileAlias -ProfileName "work"
        
        Gets all aliases that point to the "work" profile.
    
    .EXAMPLE
        Get-ClaudeProfileAlias | Sort-Object Alias
        
        Lists all aliases sorted alphabetically.
    
    .NOTES
        - Returns properly typed PowerShell objects for pipeline processing
        - Empty result if no aliases are defined or found
        - Alias names are case-sensitive
    
    .LINK
        Set-ClaudeProfileAlias
        Remove-ClaudeProfileAlias
        Switch-ClaudeProfile
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
            HelpMessage = "The name of the alias to retrieve"
        )]
        [ValidateNotNullOrEmpty()]
        [string]$Alias,
        
        [Parameter(
            Mandatory = $false,
            ParameterSetName = 'ByProfile',
            ValueFromPipelineByPropertyName = $true,
            HelpMessage = "Filter aliases by the profile they point to"
        )]
        [ValidateNotNullOrEmpty()]
        [string]$ProfileName
    )
    
    begin {
        Write-Verbose "Starting Get-ClaudeProfileAlias"
        if ($Alias) {
            Write-Verbose "Retrieving specific alias: $Alias"
        }
        elseif ($ProfileName) {
            Write-Verbose "Retrieving aliases for profile: $ProfileName"
        }
        else {
            Write-Verbose "Retrieving all aliases"
        }
    }
    
    process {
        try {
            Write-Verbose "Executing CLI aliases command"
            
            # Get aliases from CLI
            $rawOutput = Invoke-ClaudeProfileCLI -Arguments @('aliases') -ParseJsonOutput -ThrowOnError:$false
            
            if (-not $rawOutput) {
                Write-Verbose "No aliases found"
                return @()
            }
            
            # Convert to PowerShell objects
            $aliases = ConvertTo-PowerShellObject -InputObject $rawOutput -ObjectType 'Alias'
            
            if (-not $aliases -or $aliases.Count -eq 0) {
                Write-Verbose "No aliases found after conversion"
                return @()
            }
            
            # Filter results based on parameters
            if ($Alias) {
                Write-Verbose "Filtering to specific alias: $Alias"
                $aliases = $aliases | Where-Object { $_.Alias -eq $Alias }
                
                if (-not $aliases) {
                    Write-Warning "Alias '$Alias' not found"
                    return @()
                }
            }
            elseif ($ProfileName) {
                Write-Verbose "Filtering to profile: $ProfileName"
                $aliases = $aliases | Where-Object { $_.ProfileName -eq $ProfileName }
                
                if (-not $aliases) {
                    Write-Verbose "No aliases found for profile '$ProfileName'"
                    return @()
                }
            }
            
            # Return results
            return $aliases
        }
        catch {
            # Handle the case where no aliases exist gracefully
            if ($_.Exception.Message -match "No aliases defined" -or 
                $_.Exception.Message -match "not found") {
                Write-Verbose "No aliases are currently defined"
                return @()
            }
            
            $errorMessage = "Failed to retrieve Claude profile aliases: $($_.Exception.Message)"
            Write-Error $errorMessage -Category InvalidOperation -ErrorAction Stop
        }
    }
    
    end {
        Write-Verbose "Get-ClaudeProfileAlias completed"
    }
}