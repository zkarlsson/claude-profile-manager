function Get-ClaudeProfileStatus {
    <#
    .SYNOPSIS
        Gets comprehensive status information about Claude Code CLI profiles.
    
    .DESCRIPTION
        The Get-ClaudeProfileStatus cmdlet provides a detailed overview of the Claude 
        Profile Manager system, including all profiles, their health status, current 
        profile, aliases, and system information.
    
    .PARAMETER IncludeSystemInfo
        Include system diagnostic information such as CLI version and configuration paths.
    
    .PARAMETER IncludeAliases
        Include alias information in the status report.
    
    .OUTPUTS
        ClaudeProfileManager.Status
        Returns a comprehensive status object with the following properties:
        - CurrentProfile: The currently active profile
        - Profiles: All available profiles with health status
        - Aliases: All defined aliases (if requested)
        - SystemInfo: System diagnostic information (if requested)
        - Summary: Overall system health summary
    
    .EXAMPLE
        Get-ClaudeProfileStatus
        
        Gets basic status information about profiles and current state.
    
    .EXAMPLE
        Get-ClaudeProfileStatus -IncludeSystemInfo -IncludeAliases
        
        Gets comprehensive status including system diagnostics and aliases.
    
    .EXAMPLE
        $status = Get-ClaudeProfileStatus
        Write-Host "Current profile: $($status.CurrentProfile.Name)"
        Write-Host "Total profiles: $($status.Profiles.Count)"
        
        Gets status and displays key metrics.
    
    .NOTES
        - Provides a comprehensive overview of the profile management system
        - Useful for troubleshooting and system health monitoring
        - Combines information from multiple other cmdlets into a single view
    
    .LINK
        Get-ClaudeProfile
        Get-CurrentClaudeProfile
        Test-ClaudeProfileHealth
    #>
    
    [CmdletBinding()]
    [OutputType([PSCustomObject])]
    param(
        [Parameter(Mandatory = $false)]
        [switch]$IncludeSystemInfo,
        
        [Parameter(Mandatory = $false)]
        [switch]$IncludeAliases
    )
    
    begin {
        Write-Verbose "Starting Get-ClaudeProfileStatus"
    }
    
    process {
        try {
            $statusStart = Get-Date
            
            # Get current profile
            Write-Verbose "Getting current profile information"
            $currentProfile = Get-CurrentClaudeProfile -Detailed -ErrorAction SilentlyContinue
            
            # Get all profiles
            Write-Verbose "Getting all profiles"
            $allProfiles = Get-ClaudeProfile -ErrorAction SilentlyContinue
            if (-not $allProfiles) {
                $allProfiles = @()
            }
            
            # Get system health
            Write-Verbose "Performing basic health checks"
            $healthResults = Test-ClaudeProfileHealth -Quick -ErrorAction SilentlyContinue
            if (-not $healthResults) {
                $healthResults = @()
            }
            
            # Build summary
            $healthySummary = $healthResults | Where-Object { $_.Status -eq 'Healthy' }
            $degradedSummary = $healthResults | Where-Object { $_.Status -eq 'Degraded' }
            $unhealthySummary = $healthResults | Where-Object { $_.Status -eq 'Unhealthy' }
            
            $overallStatus = 'Healthy'
            if ($unhealthySummary) {
                $overallStatus = 'Unhealthy'
            }
            elseif ($degradedSummary) {
                $overallStatus = 'Degraded'
            }
            
            # Create base status object
            $status = [PSCustomObject]@{
                PSTypeName = 'ClaudeProfileManager.Status'
                CurrentProfile = $currentProfile
                Profiles = $allProfiles
                ProfileCount = $allProfiles.Count
                OverallStatus = $overallStatus
                HealthChecks = $healthResults
                Summary = [PSCustomObject]@{
                    OverallStatus = $overallStatus
                    ProfileCount = $allProfiles.Count
                    CurrentProfile = if ($currentProfile) { $currentProfile.Name } else { 'None' }
                    HealthyComponents = $healthySummary.Count
                    DegradedComponents = $degradedSummary.Count
                    UnhealthyComponents = $unhealthySummary.Count
                    LastChecked = Get-Date
                }
                Timestamp = Get-Date
                Duration = (Get-Date) - $statusStart
            }
            
            # Add aliases if requested
            if ($IncludeAliases) {
                Write-Verbose "Including alias information"
                try {
                    $aliases = Get-ClaudeProfileAlias -ErrorAction SilentlyContinue
                    if (-not $aliases) {
                        $aliases = @()
                    }
                    $status | Add-Member -MemberType NoteProperty -Name 'Aliases' -Value $aliases
                    $status | Add-Member -MemberType NoteProperty -Name 'AliasCount' -Value $aliases.Count
                }
                catch {
                    Write-Verbose "Could not retrieve alias information: $($_.Exception.Message)"
                    $status | Add-Member -MemberType NoteProperty -Name 'Aliases' -Value @()
                    $status | Add-Member -MemberType NoteProperty -Name 'AliasCount' -Value 0
                }
            }
            
            # Add system info if requested
            if ($IncludeSystemInfo) {
                Write-Verbose "Including system diagnostic information"
                try {
                    $cliPath = Get-ClaudeProfileCLIPath -ErrorAction SilentlyContinue
                    $cliVersion = $null
                    
                    if ($cliPath) {
                        try {
                            $cliVersion = Invoke-ClaudeProfileCLI -Arguments @('--version') -ThrowOnError:$false -ErrorAction SilentlyContinue
                        }
                        catch {
                            Write-Verbose "Could not get CLI version: $($_.Exception.Message)"
                        }
                    }
                    
                    $systemInfo = [PSCustomObject]@{
                        CLIPath = $cliPath
                        CLIVersion = $cliVersion
                        CLIAvailable = ($null -ne $cliPath)
                        PowerShellVersion = $PSVersionTable.PSVersion.ToString()
                        Platform = $PSVersionTable.Platform
                        OS = $PSVersionTable.OS
                        ProfilesPath = if ($env:USERPROFILE) { Join-Path $env:USERPROFILE '.claude\profiles' } else { '~/.claude/profiles' }
                    }
                    
                    $status | Add-Member -MemberType NoteProperty -Name 'SystemInfo' -Value $systemInfo
                }
                catch {
                    Write-Verbose "Could not retrieve system information: $($_.Exception.Message)"
                    $status | Add-Member -MemberType NoteProperty -Name 'SystemInfo' -Value $null
                }
            }
            
            # Display summary
            Write-Verbose "Status check completed in $($status.Duration.TotalMilliseconds)ms"
            
            if ($status.OverallStatus -eq 'Healthy') {
                Write-Host "✓ Claude Profile Manager is healthy" -ForegroundColor Green
            }
            elseif ($status.OverallStatus -eq 'Degraded') {
                Write-Warning "Claude Profile Manager has degraded components"
            }
            else {
                Write-Warning "Claude Profile Manager has unhealthy components"
            }
            
            Write-Host "Profiles: $($status.ProfileCount), Current: $($status.Summary.CurrentProfile)" -ForegroundColor Cyan
            
            if ($IncludeAliases -and $status.Aliases) {
                Write-Host "Aliases: $($status.AliasCount)" -ForegroundColor Cyan
            }
            
            return $status
        }
        catch {
            $errorMessage = "Failed to get Claude profile status: $($_.Exception.Message)"
            Write-Error $errorMessage -Category InvalidOperation -ErrorAction Stop
        }
    }
    
    end {
        Write-Verbose "Get-ClaudeProfileStatus completed"
    }
}