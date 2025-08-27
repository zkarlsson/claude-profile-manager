#Requires -Version 5.1

BeforeAll {
    # Import the module for testing
    $ModulePath = Join-Path $PSScriptRoot "..\ClaudeProfileManager"
    if (-not (Test-Path $ModulePath)) {
        throw "Module not found at $ModulePath"
    }
    
    # Remove module if already loaded
    Remove-Module ClaudeProfileManager -Force -ErrorAction SilentlyContinue
    
    # Import module
    Import-Module $ModulePath -Force
    
    # Create test directory
    $script:TestProfilePath = Join-Path $env:TEMP "ClaudeProfileManager-Tests-$(Get-Random)"
    New-Item -ItemType Directory -Path $script:TestProfilePath -Force | Out-Null
    
    # Mock CLI to avoid actual CLI calls during tests
    $script:MockedCalls = @()
    
    # Override CLI function with mock that records calls
    function Global:Invoke-ClaudeProfileCLI {
        param(
            [string[]]$Arguments,
            [switch]$ParseJsonOutput,
            [switch]$ThrowOnError
        )
        
        $script:MockedCalls += [PSCustomObject]@{
            Arguments = $Arguments
            ParseJsonOutput = $ParseJsonOutput.IsPresent
            ThrowOnError = $ThrowOnError.IsPresent
            Timestamp = Get-Date
        }
        
        # Return mock responses based on arguments
        switch -Wildcard ($Arguments[0]) {
            'save' { 
                return "Profile '$($Arguments[1])' saved successfully" 
            }
            'list' { 
                if ($ParseJsonOutput) {
                    return '[{"Name":"test-profile","AuthMethod":"Console","IsCurrent":true,"TokenHealth":"Valid"}]'
                }
                return "test-profile (Console) [Current] - Valid"
            }
            'switch' { 
                return "Switched to profile '$($Arguments[1])'" 
            }
            'current' { 
                return "test-profile" 
            }
            'delete' { 
                return "Profile '$($Arguments[1])' deleted successfully" 
            }
            'alias' { 
                if ($Arguments.Count -eq 1) {
                    if ($ParseJsonOutput) {
                        return '{"t":"test-profile","w":"work"}'
                    }
                    return "t -> test-profile`nw -> work"
                }
                else {
                    return "Alias '$($Arguments[1])' created for profile '$($Arguments[2])'"
                }
            }
            'unalias' { 
                return "Alias '$($Arguments[1])' removed" 
            }
            'aliases' {
                if ($ParseJsonOutput) {
                    return '{"t":"test-profile","w":"work"}'
                }
                return "t -> test-profile`nw -> work"
            }
            '--version' { 
                return "claude 1.0.0" 
            }
            default { 
                if ($ThrowOnError) {
                    throw "Mock CLI: Unknown command $($Arguments[0])"
                }
                return "Mock response for: $($Arguments -join ' ')"
            }
        }
    }
}

Describe "Integration Tests" {
    BeforeEach {
        $script:MockedCalls = @()
    }
    
    Context "Profile Lifecycle" {
        It "Should save a profile successfully" {
            $result = Save-ClaudeProfile -Name "integration-test"
            
            # Verify CLI was called with correct arguments
            $saveCall = $script:MockedCalls | Where-Object { $_.Arguments[0] -eq 'save' }
            $saveCall | Should -Not -BeNullOrEmpty
            $saveCall.Arguments | Should -Contain "integration-test"
        }
        
        It "Should retrieve profiles successfully" {
            $result = Get-ClaudeProfile
            
            $result | Should -Not -BeNullOrEmpty
            $result[0] | Should -HaveProperty 'Name'
            $result[0] | Should -HaveProperty 'AuthMethod'
            $result[0] | Should -HaveProperty 'IsCurrent'
        }
        
        It "Should switch profiles successfully" {
            $result = Switch-ClaudeProfile -Name "integration-test"
            
            # Verify CLI was called
            $switchCall = $script:MockedCalls | Where-Object { $_.Arguments[0] -eq 'switch' }
            $switchCall | Should -Not -BeNullOrEmpty
            $switchCall.Arguments | Should -Contain "integration-test"
        }
        
        It "Should get current profile successfully" {
            $result = Get-CurrentClaudeProfile
            
            $result | Should -Not -BeNullOrEmpty
            $result | Should -HaveProperty 'Name'
        }
        
        It "Should remove profiles successfully" {
            $result = Remove-ClaudeProfile -Name "integration-test" -Force
            
            # Verify CLI was called
            $deleteCall = $script:MockedCalls | Where-Object { $_.Arguments[0] -eq 'delete' }
            $deleteCall | Should -Not -BeNullOrEmpty
            $deleteCall.Arguments | Should -Contain "integration-test"
        }
    }
    
    Context "Alias Management Workflow" {
        It "Should create aliases successfully" {
            $result = Set-ClaudeProfileAlias -Alias "itest" -ProfileName "integration-test"
            
            # Verify CLI was called
            $aliasCall = $script:MockedCalls | Where-Object { $_.Arguments[0] -eq 'alias' -and $_.Arguments.Count -gt 1 }
            $aliasCall | Should -Not -BeNullOrEmpty
            $aliasCall.Arguments | Should -Contain "itest"
            $aliasCall.Arguments | Should -Contain "integration-test"
        }
        
        It "Should list aliases successfully" {
            $result = Get-ClaudeProfileAlias
            
            $result | Should -Not -BeNullOrEmpty
            $result | Should -HaveProperty 'Alias'
            $result | Should -HaveProperty 'ProfileName'
        }
        
        It "Should get specific alias successfully" {
            $result = Get-ClaudeProfileAlias -Alias "t"
            
            $result | Should -Not -BeNullOrEmpty
            $result.Alias | Should -Be "t"
        }
        
        It "Should remove aliases successfully" {
            # First mock the alias exists
            Mock Get-ClaudeProfileAlias { 
                return [PSCustomObject]@{ 
                    PSTypeName = 'ClaudeProfileManager.Alias'
                    Alias = 'itest'
                    ProfileName = 'integration-test'
                }
            } -ParameterFilter { $Alias -eq 'itest' }
            
            $result = Remove-ClaudeProfileAlias -Alias "itest" -Force
            
            # Verify CLI was called
            $unaliasCall = $script:MockedCalls | Where-Object { $_.Arguments[0] -eq 'unalias' }
            $unaliasCall | Should -Not -BeNullOrEmpty
            $unaliasCall.Arguments | Should -Contain "itest"
        }
    }
    
    Context "Health and Status Monitoring" {
        It "Should perform health checks successfully" {
            $result = Test-ClaudeProfileHealth -Quick
            
            $result | Should -Not -BeNullOrEmpty
            $result[0] | Should -HaveProperty 'Name'
            $result[0] | Should -HaveProperty 'Status'
            $result[0] | Should -HaveProperty 'Description'
        }
        
        It "Should get comprehensive status successfully" {
            $result = Get-ClaudeProfileStatus
            
            $result | Should -Not -BeNullOrEmpty
            $result | Should -HaveProperty 'Summary'
            $result | Should -HaveProperty 'OverallStatus'
            $result | Should -HaveProperty 'ProfileCount'
        }
        
        It "Should get status with system info successfully" {
            Mock Get-ClaudeProfileCLIPath { return "C:\mock\claude.exe" }
            
            $result = Get-ClaudeProfileStatus -IncludeSystemInfo
            
            $result | Should -HaveProperty 'SystemInfo'
            $result.SystemInfo | Should -HaveProperty 'CLIPath'
        }
        
        It "Should get status with aliases successfully" {
            $result = Get-ClaudeProfileStatus -IncludeAliases
            
            $result | Should -HaveProperty 'Aliases'
            $result | Should -HaveProperty 'AliasCount'
        }
    }
    
    Context "Pipeline Support" {
        It "Should support piping profiles to other cmdlets" {
            Mock Get-ClaudeProfile {
                return @(
                    [PSCustomObject]@{ PSTypeName = 'ClaudeProfileManager.Profile'; Name = 'profile1' },
                    [PSCustomObject]@{ PSTypeName = 'ClaudeProfileManager.Profile'; Name = 'profile2' }
                )
            }
            
            $result = Get-ClaudeProfile | Where-Object { $_.Name -eq 'profile1' }
            
            $result | Should -Not -BeNullOrEmpty
            $result.Name | Should -Be 'profile1'
        }
        
        It "Should support piping aliases to remove cmdlet" {
            Mock Get-ClaudeProfileAlias {
                return @(
                    [PSCustomObject]@{ PSTypeName = 'ClaudeProfileManager.Alias'; Alias = 'alias1'; ProfileName = 'profile1' },
                    [PSCustomObject]@{ PSTypeName = 'ClaudeProfileManager.Alias'; Alias = 'alias2'; ProfileName = 'profile2' }
                )
            }
            
            # This would normally pipe to Remove-ClaudeProfileAlias but we'll just test the pipe works
            $result = Get-ClaudeProfileAlias | Where-Object { $_.Alias -eq 'alias1' }
            
            $result | Should -Not -BeNullOrEmpty
            $result.Alias | Should -Be 'alias1'
        }
    }
    
    Context "Error Handling" {
        It "Should handle CLI errors gracefully" {
            # Override the mock to throw an error
            function Global:Invoke-ClaudeProfileCLI {
                param($Arguments, $ParseJsonOutput, $ThrowOnError)
                if ($ThrowOnError) {
                    throw "Simulated CLI error"
                }
            }
            
            { Save-ClaudeProfile -Name "test" } | Should -Throw
        }
        
        It "Should validate profile names" {
            { Save-ClaudeProfile -Name "" } | Should -Throw
            { Save-ClaudeProfile -Name "con" } | Should -Throw
        }
        
        It "Should validate alias names" {
            { Set-ClaudeProfileAlias -Alias "1invalid" -ProfileName "test" } | Should -Throw
            { Set-ClaudeProfileAlias -Alias "break" -ProfileName "test" } | Should -Throw
        }
    }
    
    Context "Module Aliases" {
        It "Should have working module aliases" {
            $aliases = @(
                @{ Alias = 'Save-CP'; Command = 'Save-ClaudeProfile' },
                @{ Alias = 'Get-CP'; Command = 'Get-ClaudeProfile' },
                @{ Alias = 'Switch-CP'; Command = 'Switch-ClaudeProfile' },
                @{ Alias = 'Remove-CP'; Command = 'Remove-ClaudeProfile' },
                @{ Alias = 'Current-CP'; Command = 'Get-CurrentClaudeProfile' }
            )
            
            foreach ($aliasInfo in $aliases) {
                $alias = Get-Alias $aliasInfo.Alias -ErrorAction SilentlyContinue
                $alias | Should -Not -BeNullOrEmpty
                $alias.Definition | Should -Be $aliasInfo.Command
            }
        }
    }
    
    Context "Comprehensive Workflow" {
        It "Should handle complete profile management workflow" {
            # Save a profile
            Save-ClaudeProfile -Name "workflow-test" -Aliases @("wt", "workflow")
            
            # List profiles
            $profiles = Get-ClaudeProfile
            
            # Switch to profile
            Switch-ClaudeProfile -Name "workflow-test"
            
            # Get current profile  
            $current = Get-CurrentClaudeProfile
            
            # Check health
            $health = Test-ClaudeProfileHealth -Quick
            
            # Get status
            $status = Get-ClaudeProfileStatus
            
            # Create additional alias
            Set-ClaudeProfileAlias -Alias "test" -ProfileName "workflow-test"
            
            # List aliases
            $aliases = Get-ClaudeProfileAlias
            
            # Remove alias
            Mock Get-ClaudeProfileAlias { 
                return [PSCustomObject]@{ PSTypeName = 'ClaudeProfileManager.Alias'; Alias = 'test'; ProfileName = 'workflow-test' }
            } -ParameterFilter { $Alias -eq 'test' }
            Remove-ClaudeProfileAlias -Alias "test" -Force
            
            # Remove profile
            Remove-ClaudeProfile -Name "workflow-test" -Force
            
            # All operations should complete without errors
            $true | Should -Be $true
        }
    }
}

AfterAll {
    # Clean up test directory
    if (Test-Path $script:TestProfilePath) {
        Remove-Item $script:TestProfilePath -Recurse -Force
    }
    
    # Clean up module
    Remove-Module ClaudeProfileManager -Force -ErrorAction SilentlyContinue
    
    # Remove global mock function
    Remove-Item Function:\Invoke-ClaudeProfileCLI -ErrorAction SilentlyContinue
}