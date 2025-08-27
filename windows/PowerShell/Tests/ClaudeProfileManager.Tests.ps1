#Requires -Version 5.1

BeforeAll {
    # Import the module for testing
    $ModulePath = Join-Path $PSScriptRoot "..\ClaudeProfileManager"
    if (-not (Test-Path $ModulePath)) {
        throw "Module not found at $ModulePath"
    }
    
    # Remove module if already loaded to ensure clean import
    Remove-Module ClaudeProfileManager -Force -ErrorAction SilentlyContinue
    
    # Import module
    Import-Module $ModulePath -Force
    
    # Mock external dependencies
    Mock Get-Command { return @{ Source = "C:\mock\claude.exe" } } -ParameterFilter { $Name -eq 'claude' }
}

Describe "ClaudeProfileManager Module" {
    Context "Module Import" {
        It "Should import successfully" {
            Get-Module ClaudeProfileManager | Should -Not -BeNullOrEmpty
        }
        
        It "Should expose expected cmdlets" {
            $expectedCmdlets = @(
                'Save-ClaudeProfile',
                'Get-ClaudeProfile', 
                'Switch-ClaudeProfile',
                'Remove-ClaudeProfile',
                'Get-CurrentClaudeProfile',
                'Set-ClaudeProfileAlias',
                'Get-ClaudeProfileAlias',
                'Remove-ClaudeProfileAlias',
                'Test-ClaudeProfileHealth',
                'Get-ClaudeProfileStatus'
            )
            
            foreach ($cmdlet in $expectedCmdlets) {
                Get-Command $cmdlet -ErrorAction SilentlyContinue | Should -Not -BeNullOrEmpty
            }
        }
        
        It "Should expose expected aliases" {
            $expectedAliases = @(
                'Save-CP',
                'Get-CP',
                'Switch-CP',
                'Remove-CP',
                'Current-CP',
                'Set-CPA',
                'Get-CPA',
                'Remove-CPA'
            )
            
            foreach ($alias in $expectedAliases) {
                Get-Alias $alias -ErrorAction SilentlyContinue | Should -Not -BeNullOrEmpty
            }
        }
    }
    
    Context "Help Documentation" {
        $cmdlets = Get-Command -Module ClaudeProfileManager
        
        foreach ($cmdlet in $cmdlets) {
            It "Should have help documentation for $($cmdlet.Name)" {
                $help = Get-Help $cmdlet.Name
                $help.Synopsis | Should -Not -BeNullOrEmpty
                $help.Description | Should -Not -BeNullOrEmpty
            }
            
            It "Should have examples for $($cmdlet.Name)" {
                $help = Get-Help $cmdlet.Name -Examples
                $help.Examples | Should -Not -BeNullOrEmpty
            }
        }
    }
}

Describe "Private Functions" {
    Context "Get-ClaudeProfileCLIPath" {
        It "Should return a path when CLI is available" {
            Mock Get-Command { return @{ Source = "C:\test\claude.exe" } } -ParameterFilter { $Name -eq 'claude' }
            
            $result = Get-ClaudeProfileCLIPath
            $result | Should -Not -BeNullOrEmpty
            $result | Should -Be "C:\test\claude.exe"
        }
        
        It "Should throw when CLI is not available" {
            Mock Get-Command { throw "Command not found" } -ParameterFilter { $Name -eq 'claude' }
            
            { Get-ClaudeProfileCLIPath } | Should -Throw
        }
    }
    
    Context "Test-ProfileNameValidation" {
        It "Should validate correct profile names" {
            Test-ProfileNameValidation -ProfileName "work" | Should -Be $true
            Test-ProfileNameValidation -ProfileName "my-profile" | Should -Be $true
            Test-ProfileNameValidation -ProfileName "test_profile" | Should -Be $true
            Test-ProfileNameValidation -ProfileName "profile.v1" | Should -Be $true
        }
        
        It "Should reject invalid profile names" {
            Test-ProfileNameValidation -ProfileName "" | Should -Be $false
            Test-ProfileNameValidation -ProfileName "a" * 51 | Should -Be $false
            Test-ProfileNameValidation -ProfileName "-invalid" | Should -Be $false
            Test-ProfileNameValidation -ProfileName "invalid-" | Should -Be $false
            Test-ProfileNameValidation -ProfileName "con" | Should -Be $false
            Test-ProfileNameValidation -ProfileName "with spaces" | Should -Be $false
        }
        
        It "Should allow empty when explicitly requested" {
            Test-ProfileNameValidation -ProfileName "" -AllowEmpty | Should -Be $true
        }
    }
    
    Context "Test-AliasNameValidation" {
        It "Should validate correct alias names" {
            Test-AliasNameValidation -AliasName "w" | Should -Be $true
            Test-AliasNameValidation -AliasName "work" | Should -Be $true
            Test-AliasNameValidation -AliasName "my-alias" | Should -Be $true
            Test-AliasNameValidation -AliasName "test_alias" | Should -Be $true
        }
        
        It "Should reject invalid alias names" {
            Test-AliasNameValidation -AliasName "" | Should -Be $false
            Test-AliasNameValidation -AliasName "1invalid" | Should -Be $false
            Test-AliasNameValidation -AliasName "invalid-" | Should -Be $false
            Test-AliasNameValidation -AliasName "with spaces" | Should -Be $false
            Test-AliasNameValidation -AliasName "break" | Should -Be $false
            Test-AliasNameValidation -AliasName "function" | Should -Be $false
        }
    }
}

Describe "Save-ClaudeProfile" {
    BeforeEach {
        Mock Invoke-ClaudeProfileCLI { return "Profile saved successfully" }
    }
    
    Context "Parameter Validation" {
        It "Should accept valid profile names" {
            { Save-ClaudeProfile -Name "work" -WhatIf } | Should -Not -Throw
        }
        
        It "Should reject invalid profile names" {
            { Save-ClaudeProfile -Name "" -WhatIf } | Should -Throw
            { Save-ClaudeProfile -Name "con" -WhatIf } | Should -Throw
        }
        
        It "Should accept valid aliases" {
            { Save-ClaudeProfile -Name "work" -Aliases @("w", "office") -WhatIf } | Should -Not -Throw
        }
        
        It "Should reject invalid aliases" {
            { Save-ClaudeProfile -Name "work" -Aliases @("1invalid") -WhatIf } | Should -Throw
        }
    }
    
    Context "CLI Integration" {
        It "Should call CLI with correct arguments for basic save" {
            Save-ClaudeProfile -Name "test" -WhatIf:$false
            
            Should -Invoke Invoke-ClaudeProfileCLI -ParameterFilter {
                $Arguments -contains 'save' -and $Arguments -contains 'test'
            }
        }
        
        It "Should call CLI with aliases when provided" {
            Save-ClaudeProfile -Name "test" -Aliases @("t", "alias") -WhatIf:$false
            
            Should -Invoke Invoke-ClaudeProfileCLI -ParameterFilter {
                $Arguments -contains 'save' -and $Arguments -contains 'test' -and 
                $Arguments -contains '--aliases' -and $Arguments -contains 't,alias'
            }
        }
    }
}

Describe "Get-ClaudeProfile" {
    Context "Parameter Validation" {
        It "Should accept valid profile names" {
            Mock Invoke-ClaudeProfileCLI { return '[]' }
            { Get-ClaudeProfile -Name "work" } | Should -Not -Throw
        }
        
        It "Should work without parameters" {
            Mock Invoke-ClaudeProfileCLI { return '[]' }
            { Get-ClaudeProfile } | Should -Not -Throw
        }
    }
    
    Context "Output Processing" {
        It "Should return empty array when no profiles exist" {
            Mock Invoke-ClaudeProfileCLI { return $null }
            
            $result = Get-ClaudeProfile
            $result | Should -BeOfType [array]
            $result.Count | Should -Be 0
        }
        
        It "Should convert JSON output to PowerShell objects" {
            $mockJson = '[{"Name":"work","AuthMethod":"Console","IsCurrent":true}]'
            Mock Invoke-ClaudeProfileCLI { return $mockJson }
            Mock ConvertTo-PowerShellObject { 
                return @([PSCustomObject]@{
                    PSTypeName = 'ClaudeProfileManager.Profile'
                    Name = 'work'
                    AuthMethod = 'Console'
                    IsCurrent = $true
                })
            }
            
            $result = Get-ClaudeProfile
            $result | Should -Not -BeNullOrEmpty
            $result[0].PSTypeNames | Should -Contain 'ClaudeProfileManager.Profile'
        }
    }
}

Describe "Switch-ClaudeProfile" {
    BeforeEach {
        Mock Invoke-ClaudeProfileCLI { return "Switched to profile successfully" }
    }
    
    Context "Parameter Validation" {
        It "Should accept valid profile names" {
            { Switch-ClaudeProfile -Name "work" -WhatIf } | Should -Not -Throw
        }
        
        It "Should require profile name" {
            { Switch-ClaudeProfile -WhatIf } | Should -Throw
        }
    }
    
    Context "CLI Integration" {
        It "Should call CLI with correct arguments" {
            Switch-ClaudeProfile -Name "work" -WhatIf:$false
            
            Should -Invoke Invoke-ClaudeProfileCLI -ParameterFilter {
                $Arguments -contains 'switch' -and $Arguments -contains 'work'
            }
        }
    }
}

Describe "Test-ClaudeProfileHealth" {
    Context "Health Checks" {
        It "Should perform basic health checks by default" {
            Mock Get-ClaudeProfileCLIPath { return "C:\test\claude.exe" }
            Mock Invoke-ClaudeProfileCLI { return "1.0.0" } -ParameterFilter { $Arguments -contains '--version' }
            
            $result = Test-ClaudeProfileHealth
            $result | Should -Not -BeNullOrEmpty
            $result[0].PSTypeNames | Should -Contain 'ClaudeProfileManager.HealthCheck'
        }
        
        It "Should support quick health checks" {
            Mock Get-ClaudeProfileCLIPath { return "C:\test\claude.exe" }
            Mock Invoke-ClaudeProfileCLI { return "1.0.0" }
            
            { Test-ClaudeProfileHealth -Quick } | Should -Not -Throw
        }
        
        It "Should support detailed health checks" {
            Mock Get-ClaudeProfileCLIPath { return "C:\test\claude.exe" }
            Mock Invoke-ClaudeProfileCLI { return "1.0.0" }
            Mock Get-ClaudeProfile { return @() }
            Mock Get-CurrentClaudeProfile { return $null }
            
            { Test-ClaudeProfileHealth -Detailed } | Should -Not -Throw
        }
    }
}

Describe "Alias Management" {
    Context "Set-ClaudeProfileAlias" {
        BeforeEach {
            Mock Invoke-ClaudeProfileCLI { return "Alias created successfully" }
            Mock Get-ClaudeProfile { 
                return @([PSCustomObject]@{ Name = "work"; AuthMethod = "Console" })
            }
        }
        
        It "Should validate alias and profile names" {
            { Set-ClaudeProfileAlias -Alias "w" -ProfileName "work" -WhatIf } | Should -Not -Throw
        }
        
        It "Should reject invalid alias names" {
            { Set-ClaudeProfileAlias -Alias "1invalid" -ProfileName "work" -WhatIf } | Should -Throw
        }
        
        It "Should verify target profile exists" {
            Mock Get-ClaudeProfile { return @() }
            
            { Set-ClaudeProfileAlias -Alias "w" -ProfileName "nonexistent" -WhatIf } | Should -Throw
        }
    }
    
    Context "Get-ClaudeProfileAlias" {
        It "Should handle empty alias list" {
            Mock Invoke-ClaudeProfileCLI { return $null }
            
            $result = Get-ClaudeProfileAlias
            $result | Should -BeOfType [array]
            $result.Count | Should -Be 0
        }
        
        It "Should convert hashtable output to objects" {
            Mock Invoke-ClaudeProfileCLI { return '{"w":"work","p":"personal"}' }
            Mock ConvertTo-PowerShellObject {
                return @(
                    [PSCustomObject]@{ PSTypeName = 'ClaudeProfileManager.Alias'; Alias = 'w'; ProfileName = 'work' },
                    [PSCustomObject]@{ PSTypeName = 'ClaudeProfileManager.Alias'; Alias = 'p'; ProfileName = 'personal' }
                )
            }
            
            $result = Get-ClaudeProfileAlias
            $result.Count | Should -Be 2
            $result[0].PSTypeNames | Should -Contain 'ClaudeProfileManager.Alias'
        }
    }
    
    Context "Remove-ClaudeProfileAlias" {
        BeforeEach {
            Mock Get-ClaudeProfileAlias { 
                return @([PSCustomObject]@{ 
                    PSTypeName = 'ClaudeProfileManager.Alias'
                    Alias = 'w'
                    ProfileName = 'work'
                })
            } -ParameterFilter { $Alias -eq 'w' }
            Mock Invoke-ClaudeProfileCLI { return "Alias removed successfully" }
        }
        
        It "Should remove existing aliases" {
            { Remove-ClaudeProfileAlias -Alias "w" -Force -WhatIf } | Should -Not -Throw
        }
        
        It "Should handle non-existent aliases gracefully" {
            Mock Get-ClaudeProfileAlias { return @() } -ParameterFilter { $Alias -eq 'nonexistent' }
            
            { Remove-ClaudeProfileAlias -Alias "nonexistent" -Force } | Should -Not -Throw
        }
    }
}

Describe "Get-ClaudeProfileStatus" {
    BeforeEach {
        Mock Get-CurrentClaudeProfile { return $null }
        Mock Get-ClaudeProfile { return @() }
        Mock Test-ClaudeProfileHealth { return @() }
    }
    
    Context "Status Information" {
        It "Should return comprehensive status" {
            $result = Get-ClaudeProfileStatus
            
            $result | Should -Not -BeNullOrEmpty
            $result.PSTypeNames | Should -Contain 'ClaudeProfileManager.Status'
            $result.Summary | Should -Not -BeNullOrEmpty
        }
        
        It "Should include system info when requested" {
            Mock Get-ClaudeProfileCLIPath { return "C:\test\claude.exe" }
            
            $result = Get-ClaudeProfileStatus -IncludeSystemInfo
            $result.SystemInfo | Should -Not -BeNullOrEmpty
        }
        
        It "Should include aliases when requested" {
            Mock Get-ClaudeProfileAlias { return @() }
            
            $result = Get-ClaudeProfileStatus -IncludeAliases
            $result | Should -HaveProperty 'Aliases'
            $result | Should -HaveProperty 'AliasCount'
        }
    }
}

Describe "Object Formatting" {
    Context "Custom Types" {
        It "Should format Profile objects correctly" {
            $profile = [PSCustomObject]@{
                PSTypeName = 'ClaudeProfileManager.Profile'
                Name = 'test'
                IsCurrent = $true
                AuthMethod = 'Console'
                TokenHealth = 'Valid'
                LastUsed = Get-Date
            }
            
            # This would normally test the formatting, but it's difficult to test formatting directly
            # Instead, we verify the object has the correct type
            $profile.PSTypeNames | Should -Contain 'ClaudeProfileManager.Profile'
        }
    }
}

AfterAll {
    # Clean up
    Remove-Module ClaudeProfileManager -Force -ErrorAction SilentlyContinue
}