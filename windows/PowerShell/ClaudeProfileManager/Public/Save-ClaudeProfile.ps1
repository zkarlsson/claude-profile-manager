function Save-ClaudeProfile {
    <#
    .SYNOPSIS
        Saves the current Claude Code CLI credentials as a named profile.
    
    .DESCRIPTION
        The Save-ClaudeProfile cmdlet captures the current Claude Code CLI authentication 
        credentials and saves them as a named profile for later use. This allows you to 
        switch between different Claude accounts or API configurations.
        
        The cmdlet automatically detects the authentication method (Console API or 
        Subscription OAuth) and securely stores the credentials in Windows Credential Manager.
    
    .PARAMETER Name
        The name for the profile. Must be a valid filename (no path separators or reserved names).
    
    .PARAMETER Aliases
        Optional array of aliases for quick access to this profile.
    
    .PARAMETER Force
        Overwrite an existing profile without prompting.
    
    .PARAMETER PassThru
        Return the created profile object.
    
    .OUTPUTS
        None by default. ClaudeProfileManager.Profile when -PassThru is specified.
    
    .EXAMPLE
        Save-ClaudeProfile -Name "work"
        
        Saves the current credentials as a profile named "work".
    
    .EXAMPLE
        Save-ClaudeProfile -Name "personal" -Aliases @("p", "home") -Force
        
        Saves the current credentials as "personal" with aliases "p" and "home", 
        overwriting any existing profile with the same name.
    
    .EXAMPLE
        $profile = Save-ClaudeProfile -Name "api-dev" -PassThru
        Write-Host "Created profile: $($profile.Name) using $($profile.AuthMethod)"
        
        Saves the profile and returns the profile object for further processing.
    
    .NOTES
        - Requires Claude Code CLI to be installed and authenticated
        - Credentials are stored securely in Windows Credential Manager
        - Profile names cannot contain path separators or Windows reserved names
        - Existing profiles can be overwritten with the -Force parameter
    
    .LINK
        Get-ClaudeProfile
        Switch-ClaudeProfile
        Remove-ClaudeProfile
    #>
    
    [CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'Medium')]
    [OutputType([void], ParameterSetName = 'Default')]
    [OutputType([PSCustomObject], ParameterSetName = 'PassThru')]
    param(
        [Parameter(
            Mandatory = $true,
            Position = 0,
            ValueFromPipeline = $false,
            ValueFromPipelineByPropertyName = $true,
            HelpMessage = "Enter the name for the profile"
        )]
        [ValidateNotNullOrEmpty()]
        [ValidateLength(1, 100)]
        [ValidatePattern('^[^<>:"/\\|?*]+$')]  # No invalid filename characters
        [ValidateScript({
            # Check for Windows reserved names
            $reservedNames = @('CON', 'PRN', 'AUX', 'NUL', 'COM1', 'COM2', 'COM3', 'COM4', 'COM5', 'COM6', 'COM7', 'COM8', 'COM9', 'LPT1', 'LPT2', 'LPT3', 'LPT4', 'LPT5', 'LPT6', 'LPT7', 'LPT8', 'LPT9')
            if ($reservedNames -contains $_.ToUpper()) {
                throw "Profile name '$_' is a reserved Windows name. Please choose a different name."
            }
            return $true
        })]
        [string]$Name,
        
        [Parameter(
            Mandatory = $false,
            ValueFromPipelineByPropertyName = $true,
            HelpMessage = "Optional aliases for quick access to this profile"
        )]
        [ValidateNotNull()]
        [string[]]$Aliases = @(),
        
        [Parameter(Mandatory = $false)]
        [switch]$Force,
        
        [Parameter(Mandatory = $false, ParameterSetName = 'PassThru')]
        [switch]$PassThru
    )
    
    begin {
        Write-Verbose "Starting Save-ClaudeProfile with Name='$Name'"
        if ($Aliases.Count -gt 0) {
            Write-Verbose "Aliases specified: $($Aliases -join ', ')"
        }
    }
    
    process {
        try {
            # Build CLI arguments
            $cliArgs = @('save', $Name)
            
            # Add aliases if specified
            if ($Aliases.Count -gt 0) {
                $cliArgs += '--aliases'
                $cliArgs += $Aliases
            }
            
            # Add force flag if specified
            if ($Force) {
                $cliArgs += '--force'
            }
            
            # Confirm the action
            if ($PSCmdlet.ShouldProcess($Name, "Save Claude Profile")) {
                Write-Verbose "Executing CLI with arguments: $($cliArgs -join ' ')"
                
                # Execute CLI command
                $output = Invoke-ClaudeProfileCLI -Arguments $cliArgs -ThrowOnError
                
                Write-Host "✓ Successfully saved profile '$Name'" -ForegroundColor Green
                
                # Return profile object if requested
                if ($PassThru) {
                    Write-Verbose "PassThru requested, retrieving saved profile"
                    $savedProfile = Get-ClaudeProfile -Name $Name -ErrorAction SilentlyContinue
                    if ($savedProfile) {
                        return $savedProfile
                    }
                    else {
                        Write-Warning "Profile was saved but could not be retrieved for PassThru"
                    }
                }
            }
        }
        catch {
            $errorMessage = "Failed to save Claude profile '$Name': $($_.Exception.Message)"
            Write-Error $errorMessage -Category InvalidOperation -ErrorAction Stop
        }
    }
    
    end {
        Write-Verbose "Save-ClaudeProfile completed"
    }
}