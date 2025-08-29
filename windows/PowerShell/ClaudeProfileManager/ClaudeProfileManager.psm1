#
# ClaudeProfileManager PowerShell Module
# 
# This module provides PowerShell cmdlets for managing Claude Code CLI authentication profiles
# with enterprise-grade security and Windows-native integration.
#

#Requires -Version 5.1

# Module variables
$script:ModuleRoot = $PSScriptRoot
$script:CLIExecutablePath = $null

# Import validation classes (already loaded by ScriptsToProcess in manifest)
Write-Verbose "Validation classes loaded from ScriptsToProcess"

# Import private functions
$privateFunctions = Get-ChildItem -Path "$PSScriptRoot\Private" -Filter "*.ps1" -Recurse -ErrorAction SilentlyContinue
foreach ($function in $privateFunctions) {
    try {
        . $function.FullName
        Write-Verbose "Imported private function: $($function.BaseName)"
    }
    catch {
        Write-Error "Failed to import private function $($function.FullName): $($_.Exception.Message)"
    }
}

# Import public functions
$publicFunctions = Get-ChildItem -Path "$PSScriptRoot\Public" -Filter "*.ps1" -Recurse -ErrorAction SilentlyContinue
foreach ($function in $publicFunctions) {
    try {
        . $function.FullName
        Write-Verbose "Imported public function: $($function.BaseName)"
    }
    catch {
        Write-Error "Failed to import public function $($function.FullName): $($_.Exception.Message)"
    }
}

# Module initialization
Write-Verbose "Claude Profile Manager PowerShell Module loaded successfully"

# Set module-level aliases
New-Alias -Name 'Save-CP' -Value 'Save-ClaudeProfile' -Force -ErrorAction SilentlyContinue
New-Alias -Name 'Get-CP' -Value 'Get-ClaudeProfile' -Force -ErrorAction SilentlyContinue
New-Alias -Name 'Switch-CP' -Value 'Switch-ClaudeProfile' -Force -ErrorAction SilentlyContinue
New-Alias -Name 'Remove-CP' -Value 'Remove-ClaudeProfile' -Force -ErrorAction SilentlyContinue
New-Alias -Name 'Current-CP' -Value 'Get-CurrentClaudeProfile' -Force -ErrorAction SilentlyContinue
New-Alias -Name 'Set-CPA' -Value 'Set-ClaudeProfileAlias' -Force -ErrorAction SilentlyContinue
New-Alias -Name 'Get-CPA' -Value 'Get-ClaudeProfileAlias' -Force -ErrorAction SilentlyContinue
New-Alias -Name 'Remove-CPA' -Value 'Remove-ClaudeProfileAlias' -Force -ErrorAction SilentlyContinue

# Export module members
Export-ModuleMember -Function @(
    'Save-ClaudeProfile',
    'Get-ClaudeProfile',
    'Switch-ClaudeProfile', 
    'Remove-ClaudeProfile',
    'Get-CurrentClaudeProfile',
    'Set-ClaudeProfileAlias',
    'Get-ClaudeProfileAlias',
    'Remove-ClaudeProfileAlias'
) -Alias @(
    'Save-CP',
    'Get-CP',
    'Switch-CP', 
    'Remove-CP',
    'Current-CP',
    'Set-CPA',
    'Get-CPA',
    'Remove-CPA'
)

# Module cleanup on removal
$MyInvocation.MyCommand.ScriptBlock.Module.OnRemove = {
    Write-Verbose "Claude Profile Manager PowerShell Module unloaded"
}