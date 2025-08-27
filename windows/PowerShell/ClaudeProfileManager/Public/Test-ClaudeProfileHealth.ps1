function Test-ClaudeProfileHealth {
    <#
    .SYNOPSIS
        Performs health checks on Claude Code CLI profiles and infrastructure.
    
    .DESCRIPTION
        The Test-ClaudeProfileHealth cmdlet runs comprehensive health checks on the 
        Claude Profile Manager system, including profile integrity, credential validation, 
        CLI connectivity, and system dependencies.
    
    .PARAMETER ProfileName
        Test health of a specific profile. If not specified, tests all profiles and system health.
    
    .PARAMETER Quick
        Perform only basic health checks for faster execution.
    
    .PARAMETER Detailed
        Include detailed diagnostic information in the output.
    
    .OUTPUTS
        ClaudeProfileManager.HealthCheck[]
        Returns an array of health check results with the following properties:
        - Name: The name of the health check
        - Status: Health status (Healthy, Degraded, Unhealthy)
        - Description: Description of the check result
        - Duration: Time taken to perform the check
        - Exception: Error details if the check failed
        - Timestamp: When the check was performed
    
    .EXAMPLE
        Test-ClaudeProfileHealth
        
        Runs all health checks on the system and profiles.
    
    .EXAMPLE
        Test-ClaudeProfileHealth -ProfileName "work"
        
        Tests the health of the "work" profile specifically.
    
    .EXAMPLE
        Test-ClaudeProfileHealth -Quick
        
        Performs basic health checks for quick system validation.
    
    .EXAMPLE
        $healthResults = Test-ClaudeProfileHealth -Detailed
        $unhealthy = $healthResults | Where-Object { $_.Status -eq 'Unhealthy' }
        if ($unhealthy) {
            Write-Warning "Found $($unhealthy.Count) unhealthy components"
        }
        
        Runs detailed health checks and reports any unhealthy components.
    
    .NOTES
        - Health checks include CLI availability, credential store access, profile integrity
        - Use this cmdlet to diagnose issues with profile management
        - Detailed mode provides additional diagnostic information for troubleshooting
    
    .LINK
        Get-ClaudeProfile
        Get-ClaudeProfileStatus
    #>
    
    [CmdletBinding()]
    [OutputType([PSCustomObject[]])]
    param(
        [Parameter(
            Mandatory = $false,
            Position = 0,
            ValueFromPipelineByPropertyName = $true,
            HelpMessage = "Test health of a specific profile"
        )]
        [ValidateNotNullOrEmpty()]
        [string]$ProfileName,
        
        [Parameter(Mandatory = $false)]
        [switch]$Quick,
        
        [Parameter(Mandatory = $false)]
        [switch]$Detailed
    )
    
    begin {
        Write-Verbose "Starting Test-ClaudeProfileHealth"
        if ($ProfileName) {
            Write-Verbose "Testing specific profile: $ProfileName"
        }
        elseif ($Quick) {
            Write-Verbose "Performing quick health checks"
        }
        else {
            Write-Verbose "Performing comprehensive health checks"
        }
    }
    
    process {
        try {
            $healthResults = @()
            
            # Basic CLI availability check
            Write-Verbose "Checking CLI availability"
            $cliHealthStart = Get-Date
            try {
                $cliPath = Get-ClaudeProfileCLIPath
                $cliVersion = Invoke-ClaudeProfileCLI -Arguments @('--version') -ThrowOnError:$false
                
                $healthResults += [PSCustomObject]@{
                    PSTypeName = 'ClaudeProfileManager.HealthCheck'
                    Name = 'CLI Availability'
                    Status = 'Healthy'
                    Description = "CLI found at $cliPath"
                    Duration = (Get-Date) - $cliHealthStart
                    Exception = $null
                    Timestamp = Get-Date
                }
                
                if ($Detailed -and $cliVersion) {
                    Write-Verbose "CLI Version: $cliVersion"
                }
            }
            catch {
                $healthResults += [PSCustomObject]@{
                    PSTypeName = 'ClaudeProfileManager.HealthCheck'
                    Name = 'CLI Availability'
                    Status = 'Unhealthy'
                    Description = "CLI not available or not functional"
                    Duration = (Get-Date) - $cliHealthStart
                    Exception = $_.Exception.Message
                    Timestamp = Get-Date
                }
            }
            
            # If CLI is not available, skip other checks
            if ($healthResults[-1].Status -eq 'Unhealthy') {
                Write-Warning "CLI is not available, skipping additional health checks"
                return $healthResults
            }
            
            # Profile-specific health check
            if ($ProfileName) {
                Write-Verbose "Testing profile: $ProfileName"
                $profileHealthStart = Get-Date
                try {
                    $profile = Get-ClaudeProfile -Name $ProfileName -ErrorAction Stop
                    
                    $status = 'Healthy'
                    $description = "Profile '$ProfileName' is accessible"
                    
                    # Check token health if available
                    if ($profile.TokenHealth) {
                        if ($profile.TokenHealth -match 'expired|invalid') {
                            $status = 'Degraded'
                            $description += ", but token health shows: $($profile.TokenHealth)"
                        }
                        elseif ($profile.TokenHealth -match 'expires soon') {
                            $status = 'Degraded'
                            $description += ", token $($profile.TokenHealth)"
                        }
                    }
                    
                    $healthResults += [PSCustomObject]@{
                        PSTypeName = 'ClaudeProfileManager.HealthCheck'
                        Name = "Profile: $ProfileName"
                        Status = $status
                        Description = $description
                        Duration = (Get-Date) - $profileHealthStart
                        Exception = $null
                        Timestamp = Get-Date
                    }
                }
                catch {
                    $healthResults += [PSCustomObject]@{
                        PSTypeName = 'ClaudeProfileManager.HealthCheck'
                        Name = "Profile: $ProfileName"
                        Status = 'Unhealthy'
                        Description = "Profile '$ProfileName' is not accessible"
                        Duration = (Get-Date) - $profileHealthStart
                        Exception = $_.Exception.Message
                        Timestamp = Get-Date
                    }
                }
            }
            else {
                # System-wide health checks
                if (-not $Quick) {
                    # Test profile listing
                    Write-Verbose "Testing profile enumeration"
                    $profileListStart = Get-Date
                    try {
                        $profiles = Get-ClaudeProfile -ErrorAction Stop
                        $profileCount = if ($profiles) { $profiles.Count } else { 0 }
                        
                        $healthResults += [PSCustomObject]@{
                            PSTypeName = 'ClaudeProfileManager.HealthCheck'
                            Name = 'Profile Enumeration'
                            Status = 'Healthy'
                            Description = "Successfully listed $profileCount profiles"
                            Duration = (Get-Date) - $profileListStart
                            Exception = $null
                            Timestamp = Get-Date
                        }
                        
                        # Test each profile's token health
                        if ($profiles -and $Detailed) {
                            foreach ($profile in $profiles) {
                                $profileTokenStart = Get-Date
                                $status = 'Healthy'
                                $description = "Profile '$($profile.Name)' credentials accessible"
                                
                                if ($profile.TokenHealth) {
                                    if ($profile.TokenHealth -match 'expired|invalid') {
                                        $status = 'Unhealthy'
                                        $description = "Profile '$($profile.Name)' has expired/invalid credentials"
                                    }
                                    elseif ($profile.TokenHealth -match 'expires soon') {
                                        $status = 'Degraded' 
                                        $description = "Profile '$($profile.Name)' credentials expire soon"
                                    }
                                }
                                
                                $healthResults += [PSCustomObject]@{
                                    PSTypeName = 'ClaudeProfileManager.HealthCheck'
                                    Name = "Profile: $($profile.Name)"
                                    Status = $status
                                    Description = $description
                                    Duration = (Get-Date) - $profileTokenStart
                                    Exception = $null
                                    Timestamp = Get-Date
                                }
                            }
                        }
                    }
                    catch {
                        $healthResults += [PSCustomObject]@{
                            PSTypeName = 'ClaudeProfileManager.HealthCheck'
                            Name = 'Profile Enumeration'
                            Status = 'Unhealthy'
                            Description = "Failed to list profiles"
                            Duration = (Get-Date) - $profileListStart
                            Exception = $_.Exception.Message
                            Timestamp = Get-Date
                        }
                    }
                    
                    # Test current profile detection
                    Write-Verbose "Testing current profile detection"
                    $currentProfileStart = Get-Date
                    try {
                        $currentProfile = Get-CurrentClaudeProfile -ErrorAction SilentlyContinue
                        $status = if ($currentProfile) { 'Healthy' } else { 'Degraded' }
                        $description = if ($currentProfile) { 
                            "Current profile: $($currentProfile.Name)" 
                        } else { 
                            "No current profile set" 
                        }
                        
                        $healthResults += [PSCustomObject]@{
                            PSTypeName = 'ClaudeProfileManager.HealthCheck'
                            Name = 'Current Profile Detection'
                            Status = $status
                            Description = $description
                            Duration = (Get-Date) - $currentProfileStart
                            Exception = $null
                            Timestamp = Get-Date
                        }
                    }
                    catch {
                        $healthResults += [PSCustomObject]@{
                            PSTypeName = 'ClaudeProfileManager.HealthCheck'
                            Name = 'Current Profile Detection'
                            Status = 'Unhealthy'
                            Description = "Failed to detect current profile"
                            Duration = (Get-Date) - $currentProfileStart
                            Exception = $_.Exception.Message
                            Timestamp = Get-Date
                        }
                    }
                }
            }
            
            # Summary
            $healthySummary = $healthResults | Where-Object { $_.Status -eq 'Healthy' }
            $degradedSummary = $healthResults | Where-Object { $_.Status -eq 'Degraded' }
            $unhealthySummary = $healthResults | Where-Object { $_.Status -eq 'Unhealthy' }
            
            Write-Verbose "Health check summary: $($healthySummary.Count) healthy, $($degradedSummary.Count) degraded, $($unhealthySummary.Count) unhealthy"
            
            if ($unhealthySummary) {
                Write-Warning "Found $($unhealthySummary.Count) unhealthy components"
            }
            elseif ($degradedSummary) {
                Write-Warning "Found $($degradedSummary.Count) degraded components"
            }
            else {
                Write-Host "✓ All health checks passed" -ForegroundColor Green
            }
            
            return $healthResults
        }
        catch {
            $errorMessage = "Failed to perform health checks: $($_.Exception.Message)"
            Write-Error $errorMessage -Category InvalidOperation -ErrorAction Stop
        }
    }
    
    end {
        Write-Verbose "Test-ClaudeProfileHealth completed"
    }
}