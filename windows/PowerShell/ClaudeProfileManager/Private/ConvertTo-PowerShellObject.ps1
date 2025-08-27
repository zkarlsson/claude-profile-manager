function ConvertTo-PowerShellObject {
    <#
    .SYNOPSIS
        Converts CLI output to PowerShell objects with proper typing.
    
    .DESCRIPTION
        Takes raw CLI output and converts it to properly typed PowerShell objects
        for better integration with PowerShell pipelines and formatting.
    
    .PARAMETER InputObject
        The raw CLI output to convert.
    
    .PARAMETER ObjectType
        The type of object to create (Profile, Alias, Status, etc.).
    
    .OUTPUTS
        System.Object
        Returns typed PowerShell objects.
    
    .EXAMPLE
        $rawOutput = Get-Content profiles.json
        $profiles = ConvertTo-PowerShellObject -InputObject $rawOutput -ObjectType 'Profile'
    #>
    
    [CmdletBinding()]
    [OutputType([object])]
    param(
        [Parameter(Mandatory = $true, ValueFromPipeline = $true)]
        [object]$InputObject,
        
        [Parameter(Mandatory = $false)]
        [ValidateSet('Profile', 'Alias', 'Status', 'Health')]
        [string]$ObjectType = 'Profile'
    )
    
    process {
        switch ($ObjectType) {
            'Profile' {
                # Convert profile objects to PowerShell custom objects
                if ($InputObject -is [array]) {
                    return $InputObject | ForEach-Object {
                        [PSCustomObject]@{
                            PSTypeName = 'ClaudeProfileManager.Profile'
                            Name = $_.Name
                            AuthMethod = $_.AuthMethod
                            Created = if ($_.Created) { [DateTime]$_.Created } else { $null }
                            LastUsed = if ($_.LastUsed) { [DateTime]$_.LastUsed } else { $null }
                            IsCurrent = [bool]$_.IsCurrent
                            TokenHealth = $_.TokenHealth
                            Aliases = if ($_.Aliases) { [string[]]$_.Aliases } else { @() }
                        }
                    }
                }
                else {
                    return [PSCustomObject]@{
                        PSTypeName = 'ClaudeProfileManager.Profile'
                        Name = $InputObject.Name
                        AuthMethod = $InputObject.AuthMethod
                        Created = if ($InputObject.Created) { [DateTime]$InputObject.Created } else { $null }
                        LastUsed = if ($InputObject.LastUsed) { [DateTime]$InputObject.LastUsed } else { $null }
                        IsCurrent = [bool]$InputObject.IsCurrent
                        TokenHealth = $InputObject.TokenHealth
                        Aliases = if ($InputObject.Aliases) { [string[]]$InputObject.Aliases } else { @() }
                    }
                }
            }
            
            'Alias' {
                # Convert alias objects
                if ($InputObject -is [hashtable]) {
                    $aliases = @()
                    foreach ($key in $InputObject.Keys) {
                        $aliases += [PSCustomObject]@{
                            PSTypeName = 'ClaudeProfileManager.Alias'
                            Alias = $key
                            ProfileName = $InputObject[$key]
                        }
                    }
                    return $aliases
                }
                else {
                    return $InputObject
                }
            }
            
            'Status' {
                # Convert status objects
                return [PSCustomObject]@{
                    PSTypeName = 'ClaudeProfileManager.Status'
                    CurrentProfile = $InputObject.CurrentProfile
                    ProfileCount = [int]$InputObject.ProfileCount
                    AliasCount = [int]$InputObject.AliasCount
                    CLIVersion = $InputObject.CLIVersion
                    LastOperation = if ($InputObject.LastOperation) { [DateTime]$InputObject.LastOperation } else { $null }
                }
            }
            
            'Health' {
                # Convert health check objects  
                if ($InputObject -is [array]) {
                    return $InputObject | ForEach-Object {
                        [PSCustomObject]@{
                            PSTypeName = 'ClaudeProfileManager.HealthCheck'
                            Name = $_.Name
                            Status = $_.Status
                            Description = $_.Description
                            Duration = if ($_.Duration) { [TimeSpan]$_.Duration } else { [TimeSpan]::Zero }
                            Exception = $_.Exception
                            Timestamp = if ($_.Timestamp) { [DateTime]$_.Timestamp } else { [DateTime]::Now }
                        }
                    }
                }
                else {
                    return [PSCustomObject]@{
                        PSTypeName = 'ClaudeProfileManager.HealthCheck'
                        Name = $InputObject.Name
                        Status = $InputObject.Status  
                        Description = $InputObject.Description
                        Duration = if ($InputObject.Duration) { [TimeSpan]$InputObject.Duration } else { [TimeSpan]::Zero }
                        Exception = $InputObject.Exception
                        Timestamp = if ($InputObject.Timestamp) { [DateTime]$InputObject.Timestamp } else { [DateTime]::Now }
                    }
                }
            }
            
            default {
                return $InputObject
            }
        }
    }
}