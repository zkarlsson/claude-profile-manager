function Test-ProfileNameValidation {
    <#
    .SYNOPSIS
        Validates Claude profile names according to security and naming conventions.
    
    .DESCRIPTION
        Performs comprehensive validation of profile names to ensure they meet
        security requirements and naming conventions for the Claude Profile Manager.
    
    .PARAMETER ProfileName
        The profile name to validate.
    
    .PARAMETER AllowEmpty
        Whether to allow empty or null profile names.
    
    .OUTPUTS
        System.Boolean
        Returns $true if the profile name is valid, $false otherwise.
    
    .EXAMPLE
        Test-ProfileNameValidation -ProfileName "work"
        
        Returns $true if "work" is a valid profile name.
    
    .EXAMPLE
        Test-ProfileNameValidation -ProfileName "" -AllowEmpty
        
        Returns $true since empty names are explicitly allowed.
    #>
    
    [CmdletBinding()]
    [OutputType([bool])]
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [string]$ProfileName,
        
        [Parameter(Mandatory = $false)]
        [switch]$AllowEmpty
    )
    
    # Allow empty if explicitly requested
    if ($AllowEmpty -and [string]::IsNullOrEmpty($ProfileName)) {
        return $true
    }
    
    # Check for null or empty
    if ([string]::IsNullOrWhiteSpace($ProfileName)) {
        Write-Verbose "Profile name cannot be null, empty, or whitespace only"
        return $false
    }
    
    # Length validation (1-50 characters)
    if ($ProfileName.Length -lt 1 -or $ProfileName.Length -gt 50) {
        Write-Verbose "Profile name must be between 1 and 50 characters long"
        return $false
    }
    
    # Character validation - allow letters, numbers, hyphens, underscores, and dots
    if ($ProfileName -notmatch '^[a-zA-Z0-9._-]+$') {
        Write-Verbose "Profile name can only contain letters, numbers, dots, hyphens, and underscores"
        return $false
    }
    
    # Cannot start or end with dots, hyphens, or underscores
    if ($ProfileName -match '^[._-]' -or $ProfileName -match '[._-]$') {
        Write-Verbose "Profile name cannot start or end with dots, hyphens, or underscores"
        return $false
    }
    
    # Cannot contain consecutive special characters
    if ($ProfileName -match '[._-]{2,}') {
        Write-Verbose "Profile name cannot contain consecutive dots, hyphens, or underscores"
        return $false
    }
    
    # Reserved names (case-insensitive)
    $reservedNames = @(
        'CON', 'PRN', 'AUX', 'NUL',
        'COM1', 'COM2', 'COM3', 'COM4', 'COM5', 'COM6', 'COM7', 'COM8', 'COM9',
        'LPT1', 'LPT2', 'LPT3', 'LPT4', 'LPT5', 'LPT6', 'LPT7', 'LPT8', 'LPT9',
        'current', 'default', 'temp', 'temporary', 'system', 'all'
    )
    
    if ($ProfileName.ToLowerInvariant() -in $reservedNames.ToLowerInvariant()) {
        Write-Verbose "Profile name '$ProfileName' is reserved and cannot be used"
        return $false
    }
    
    return $true
}