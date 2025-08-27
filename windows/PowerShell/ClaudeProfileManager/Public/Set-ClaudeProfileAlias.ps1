function Set-ClaudeProfileAlias {
    <#
    .SYNOPSIS
        Creates or updates an alias for a Claude Code CLI profile.
    
    .DESCRIPTION
        The Set-ClaudeProfileAlias cmdlet creates a shortcut (alias) for a Claude Code CLI 
        profile, allowing you to reference the profile by a shorter or more convenient name.
        
        Aliases can be used anywhere a profile name is expected, such as with 
        Switch-ClaudeProfile or other profile management cmdlets.
    
    .PARAMETER Alias
        The alias name to create. Must be a valid identifier (no spaces or special characters).
    
    .PARAMETER ProfileName
        The name of the existing profile to alias.
    
    .PARAMETER Force
        Overwrite an existing alias without prompting.
    
    .PARAMETER PassThru
        Return the created alias object.
    
    .OUTPUTS
        None by default. ClaudeProfileManager.Alias when -PassThru is specified.
    
    .EXAMPLE
        Set-ClaudeProfileAlias -Alias "w" -ProfileName "work"
        
        Creates an alias "w" that points to the "work" profile.
    
    .EXAMPLE
        Set-ClaudeProfileAlias -Alias "p" -ProfileName "personal" -Force
        
        Creates or overwrites the alias "p" to point to "personal".
    
    .EXAMPLE
        $alias = Set-ClaudeProfileAlias -Alias "dev" -ProfileName "development" -PassThru
        Write-Host "Created alias '$($alias.Alias)' -> '$($alias.ProfileName)'"
        
        Creates an alias and returns the alias object.
    
    .NOTES
        - The target profile must exist before creating an alias
        - Alias names must be valid identifiers (letters, numbers, hyphens, underscores)
        - Aliases can be overwritten with the -Force parameter
        - Use Get-ClaudeProfileAlias to see existing aliases
    
    .LINK
        Get-ClaudeProfileAlias
        Remove-ClaudeProfileAlias
        Switch-ClaudeProfile
    #>
    
    [CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'Low')]
    [OutputType([void], ParameterSetName = 'Default')]
    [OutputType([PSCustomObject], ParameterSetName = 'PassThru')]
    param(
        [Parameter(
            Mandatory = $true,
            Position = 0,
            ValueFromPipelineByPropertyName = $true,
            HelpMessage = "The alias name to create"
        )]
        [ValidateNotNullOrEmpty()]
        [ValidateLength(1, 50)]
        [ValidatePattern('^[a-zA-Z0-9_-]+$')]  # Valid identifier characters only
        [string]$Alias,
        
        [Parameter(
            Mandatory = $true,
            Position = 1,
            ValueFromPipelineByPropertyName = $true,
            HelpMessage = "The name of the profile to alias"
        )]
        [ValidateNotNullOrEmpty()]
        [string]$ProfileName,
        
        [Parameter(Mandatory = $false)]
        [switch]$Force,
        
        [Parameter(Mandatory = $false, ParameterSetName = 'PassThru')]
        [switch]$PassThru
    )
    
    begin {
        Write-Verbose "Starting Set-ClaudeProfileAlias"
    }
    
    process {
        try {
            Write-Verbose "Creating alias '$Alias' for profile '$ProfileName'"
            
            # Validate that the target profile exists
            $targetProfile = Get-ClaudeProfile -Name $ProfileName -ErrorAction SilentlyContinue
            if (-not $targetProfile) {
                throw "Profile '$ProfileName' not found. Use Get-ClaudeProfile to see available profiles."
            }
            
            # Check if alias already exists (unless Force is specified)
            if (-not $Force) {
                try {
                    $existingAliases = Invoke-ClaudeProfileCLI -Arguments @('aliases') -ParseJsonOutput -ThrowOnError:$false
                    if ($existingAliases -and $existingAliases.$Alias) {
                        $existingTarget = $existingAliases.$Alias
                        if ($existingTarget -ne $ProfileName) {
                            $message = "Alias '$Alias' already exists and points to '$existingTarget'. Use -Force to overwrite."
                            throw $message
                        }
                        else {
                            Write-Verbose "Alias '$Alias' already points to '$ProfileName', no change needed"
                        }
                    }
                }
                catch {
                    if ($_.Exception.Message -match "already exists") {
                        throw  # Re-throw our custom message
                    }
                    Write-Verbose "Could not check existing aliases: $($_.Exception.Message)"
                }
            }
            
            # Confirm the action
            if ($PSCmdlet.ShouldProcess("$Alias -> $ProfileName", "Create Claude Profile Alias")) {
                # Build CLI arguments
                $cliArgs = @('alias', $Alias, $ProfileName)
                
                if ($Force) {
                    $cliArgs += '--force'
                }
                
                Write-Verbose "Executing CLI alias command"
                $output = Invoke-ClaudeProfileCLI -Arguments $cliArgs -ThrowOnError
                
                Write-Host "✓ Created alias '$Alias' -> '$ProfileName'" -ForegroundColor Green
                
                # Return alias object if requested
                if ($PassThru) {
                    Write-Verbose "PassThru requested, returning alias object"
                    return [PSCustomObject]@{
                        PSTypeName = 'ClaudeProfileManager.Alias'
                        Alias = $Alias
                        ProfileName = $ProfileName
                    }
                }
            }
        }
        catch {
            $errorMessage = "Failed to create alias '$Alias': $($_.Exception.Message)"
            Write-Error $errorMessage -Category InvalidOperation -ErrorAction Stop
        }
    }
    
    end {
        Write-Verbose "Set-ClaudeProfileAlias completed"
    }
}