using namespace System.Management.Automation

# Custom validation attribute for profile names
class ValidateProfileNameAttribute : ValidateArgumentsAttribute {
    [void] Validate([object] $arguments, [EngineIntrinsics] $engineIntrinsics) {
        $profileName = [string] $arguments
        
        # Use the validation function
        if (-not (Test-ProfileNameValidation -ProfileName $profileName)) {
            throw [ValidationMetadataException]::new(
                "The profile name '$profileName' is not valid. Profile names must be 1-50 characters long, " +
                "contain only letters, numbers, dots, hyphens, and underscores, cannot start or end with " +
                "special characters, and cannot use reserved names."
            )
        }
    }
}

# Custom validation attribute for alias names  
class ValidateAliasNameAttribute : ValidateArgumentsAttribute {
    [void] Validate([object] $arguments, [EngineIntrinsics] $engineIntrinsics) {
        $aliasName = [string] $arguments
        
        # Use the validation function
        if (-not (Test-AliasNameValidation -AliasName $aliasName)) {
            throw [ValidationMetadataException]::new(
                "The alias name '$aliasName' is not valid. Alias names must be 1-30 characters long, " +
                "contain only letters, numbers, hyphens, and underscores, must start with a letter, " +
                "and cannot use reserved names or PowerShell keywords."
            )
        }
    }
}

# Custom validation attribute for arrays of profile names
class ValidateProfileNamesAttribute : ValidateArgumentsAttribute {
    [void] Validate([object] $arguments, [EngineIntrinsics] $engineIntrinsics) {
        $profileNames = [string[]] $arguments
        
        foreach ($profileName in $profileNames) {
            if (-not (Test-ProfileNameValidation -ProfileName $profileName)) {
                throw [ValidationMetadataException]::new(
                    "The profile name '$profileName' in the array is not valid. Profile names must be 1-50 characters long, " +
                    "contain only letters, numbers, dots, hyphens, and underscores, cannot start or end with " +
                    "special characters, and cannot use reserved names."
                )
            }
        }
    }
}

# Custom validation attribute for arrays of alias names
class ValidateAliasNamesAttribute : ValidateArgumentsAttribute {
    [void] Validate([object] $arguments, [EngineIntrinsics] $engineIntrinsics) {
        $aliasNames = [string[]] $arguments
        
        foreach ($aliasName in $aliasNames) {
            if (-not (Test-AliasNameValidation -AliasName $aliasName)) {
                throw [ValidationMetadataException]::new(
                    "The alias name '$aliasName' in the array is not valid. Alias names must be 1-30 characters long, " +
                    "contain only letters, numbers, hyphens, and underscores, must start with a letter, " +
                    "and cannot use reserved names or PowerShell keywords."
                )
            }
        }
    }
}