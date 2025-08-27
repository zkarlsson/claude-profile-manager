#
# Module manifest for module 'ClaudeProfileManager'
#
# Generated on: $(Get-Date)
#

@{

# Script module or binary module file associated with this manifest.
RootModule = 'ClaudeProfileManager.psm1'

# Version number of this module.
ModuleVersion = '1.0.0'

# Supported PSEditions
CompatiblePSEditions = @('Desktop', 'Core')

# ID used to uniquely identify this module
GUID = 'a8b3c4d5-e6f7-8901-2345-6789abcdef01'

# Author of this module
Author = 'Claude Profile Manager Team'

# Company or vendor of this module
CompanyName = 'Claude Profile Manager'

# Copyright statement for this module
Copyright = '(c) 2025 Claude Profile Manager. All rights reserved.'

# Description of the functionality provided by this module
Description = 'PowerShell module for managing Claude Code CLI authentication profiles on Windows. Provides native PowerShell cmdlets for profile management, credential switching, and alias management with enterprise-grade security.'

# Minimum version of the PowerShell engine required by this module
PowerShellVersion = '5.1'

# Name of the PowerShell host required by this module
# PowerShellHostName = ''

# Minimum version of the PowerShell host required by this module
# PowerShellHostVersion = ''

# Minimum version of Microsoft .NET Framework required by this module. This prerequisite is valid for the PowerShell Desktop edition only.
DotNetFrameworkVersion = '4.7.2'

# Minimum version of the common language runtime (CLR) required by this module. This prerequisite is valid for the PowerShell Desktop edition only.
# CLRVersion = ''

# Processor architecture (None, X86, Amd64) required by this module
ProcessorArchitecture = 'Amd64'

# Modules that must be imported into the global environment prior to importing this module
# RequiredModules = @()

# Assemblies that must be loaded prior to importing this module
# RequiredAssemblies = @()

# Script files (.ps1) that are run in the caller's environment prior to importing this module.
ScriptsToProcess = @('Classes\ValidateProfileName.ps1')

# Type files (.ps1xml) to be loaded when importing this module
# TypesToProcess = @()

# Format files (.ps1xml) to be loaded when importing this module
FormatsToProcess = @('ClaudeProfileManager.Format.ps1xml')

# Modules to import as nested modules of the module specified in RootModule/ModuleToProcess
# NestedModules = @()

# Functions to export from this module, for best performance, do not use wildcards and do not delete the entry, use an empty array if there are no functions to export.
FunctionsToExport = @(
    # Profile Management
    'Save-ClaudeProfile',
    'Get-ClaudeProfile',
    'Switch-ClaudeProfile',
    'Remove-ClaudeProfile',
    'Get-CurrentClaudeProfile',
    
    # Alias Management
    'Set-ClaudeProfileAlias',
    'Get-ClaudeProfileAlias',
    'Remove-ClaudeProfileAlias',
    
    # Health and Diagnostics
    'Test-ClaudeProfileHealth',
    'Get-ClaudeProfileStatus'
)

# Cmdlets to export from this module, for best performance, do not use wildcards and do not delete the entry, use an empty array if there are no cmdlets to export.
CmdletsToExport = @()

# Variables to export from this module
VariablesToExport = @()

# Aliases to export from this module, for best performance, do not use wildcards and do not delete the entry, use an empty array if there are no aliases to export.
AliasesToExport = @(
    'Save-CP',
    'Get-CP', 
    'Switch-CP',
    'Remove-CP',
    'Current-CP',
    'Set-CPA',
    'Get-CPA',
    'Remove-CPA'
)

# DSC resources to export from this module
# DscResourcesToExport = @()

# List of all modules packaged with this module
# ModuleList = @()

# List of all files packaged with this module
FileList = @(
    'ClaudeProfileManager.psd1',
    'ClaudeProfileManager.psm1',
    'ClaudeProfileManager.Format.ps1xml',
    'Classes\ValidateProfileName.ps1',
    'Private\Invoke-ClaudeProfileCLI.ps1',
    'Private\Get-ClaudeProfileCLIPath.ps1',
    'Private\ConvertTo-PowerShellObject.ps1',
    'Private\Test-ProfileNameValidation.ps1',
    'Private\Test-AliasNameValidation.ps1',
    'Public\Save-ClaudeProfile.ps1',
    'Public\Get-ClaudeProfile.ps1',
    'Public\Switch-ClaudeProfile.ps1',
    'Public\Remove-ClaudeProfile.ps1',
    'Public\Get-CurrentClaudeProfile.ps1',
    'Public\Set-ClaudeProfileAlias.ps1',
    'Public\Get-ClaudeProfileAlias.ps1',
    'Public\Remove-ClaudeProfileAlias.ps1',
    'Public\Test-ClaudeProfileHealth.ps1',
    'Public\Get-ClaudeProfileStatus.ps1',
    'en-US\about_ClaudeProfileManager.help.txt'
)

# Private data to pass to the module specified in RootModule/ModuleToProcess. This may also contain a PSData hashtable with additional module metadata used by PowerShell.
PrivateData = @{

    PSData = @{

        # Tags applied to this module. These help with module discovery in online galleries.
        Tags = @('Claude', 'AI', 'CLI', 'Profile', 'Authentication', 'Windows', 'Credential', 'Management')

        # A URL to the license for this module.
        LicenseUri = 'https://github.com/anthropics/claude-profile-manager/blob/main/LICENSE'

        # A URL to the main website for this project.
        ProjectUri = 'https://github.com/anthropics/claude-profile-manager'

        # A URL to an icon representing this module.
        # IconUri = ''

        # ReleaseNotes of this module
        ReleaseNotes = @'
# Claude Profile Manager PowerShell Module v1.0.0

## Features
- Native PowerShell cmdlets for Claude Code CLI profile management
- Enterprise-grade security with Windows Credential Manager integration  
- Profile switching with automatic credential management
- Alias management for convenient profile access
- Health monitoring and diagnostics
- Comprehensive help and documentation

## Requirements
- Windows 10/11
- PowerShell 5.1 or PowerShell Core 6+
- Claude Code CLI installed and configured
- .NET 9 Runtime

## Installation
Install-Module -Name ClaudeProfileManager -Scope CurrentUser

## Usage
Save-ClaudeProfile -Name "work" -Aliases "w", "office"
Get-ClaudeProfile
Switch-ClaudeProfile -Name "work"
Get-CurrentClaudeProfile

For detailed help: Get-Help Save-ClaudeProfile -Full
'@

        # Prerelease string of this module
        # Prerelease = ''

        # Flag to indicate whether the module requires explicit user acceptance for install/update/save
        # RequireLicenseAcceptance = $false

        # External dependent modules of this module
        # ExternalModuleDependencies = @()

    } # End of PSData hashtable

} # End of PrivateData hashtable

# HelpInfo URI of this module
HelpInfoURI = 'https://github.com/anthropics/claude-profile-manager/wiki/PowerShell'

# Default prefix for commands exported from this module. Override with Import-Module -Prefix.
# DefaultCommandPrefix = ''

}