function Test-AliasNameValidation {
    <#
    .SYNOPSIS
        Validates Claude profile alias names according to security and naming conventions.
    
    .DESCRIPTION
        Performs comprehensive validation of alias names to ensure they meet
        security requirements and naming conventions for the Claude Profile Manager.
    
    .PARAMETER AliasName
        The alias name to validate.
    
    .OUTPUTS
        System.Boolean
        Returns $true if the alias name is valid, $false otherwise.
    
    .EXAMPLE
        Test-AliasNameValidation -AliasName "w"
        
        Returns $true if "w" is a valid alias name.
    
    .EXAMPLE
        Test-AliasNameValidation -AliasName "work-alias"
        
        Returns $true if "work-alias" is a valid alias name.
    #>
    
    [CmdletBinding()]
    [OutputType([bool])]
    param(
        [Parameter(Mandatory = $true)]
        [string]$AliasName
    )
    
    # Check for null or empty
    if ([string]::IsNullOrWhiteSpace($AliasName)) {
        Write-Verbose "Alias name cannot be null, empty, or whitespace only"
        return $false
    }
    
    # Length validation (1-30 characters, shorter than profile names)
    if ($AliasName.Length -lt 1 -or $AliasName.Length -gt 30) {
        Write-Verbose "Alias name must be between 1 and 30 characters long"
        return $false
    }
    
    # Character validation - allow letters, numbers, hyphens, and underscores only (no dots)
    if ($AliasName -notmatch '^[a-zA-Z0-9_-]+$') {
        Write-Verbose "Alias name can only contain letters, numbers, hyphens, and underscores"
        return $false
    }
    
    # Must start with a letter (for PowerShell compatibility)
    if ($AliasName -notmatch '^[a-zA-Z]') {
        Write-Verbose "Alias name must start with a letter"
        return $false
    }
    
    # Cannot end with hyphens or underscores
    if ($AliasName -match '[_-]$') {
        Write-Verbose "Alias name cannot end with hyphens or underscores"
        return $false
    }
    
    # Cannot contain consecutive special characters
    if ($AliasName -match '[_-]{2,}') {
        Write-Verbose "Alias name cannot contain consecutive hyphens or underscores"
        return $false
    }
    
    # Reserved alias names (case-insensitive)
    $reservedAliases = @(
        'all', 'current', 'default', 'list', 'help', 'version',
        'save', 'get', 'switch', 'remove', 'set', 'test', 'status'
    )
    
    if ($AliasName.ToLowerInvariant() -in $reservedAliases) {
        Write-Verbose "Alias name '$AliasName' is reserved and cannot be used"
        return $false
    }
    
    # Check against PowerShell reserved words
    $powerShellReserved = @(
        'break', 'continue', 'do', 'else', 'elseif', 'end', 'for', 'foreach',
        'function', 'if', 'in', 'param', 'process', 'return', 'switch', 
        'throw', 'trap', 'try', 'until', 'while', 'begin', 'catch', 'finally'
    )
    
    if ($AliasName.ToLowerInvariant() -in $powerShellReserved) {
        Write-Verbose "Alias name '$AliasName' conflicts with PowerShell reserved words"
        return $false
    }
    
    return $true
}