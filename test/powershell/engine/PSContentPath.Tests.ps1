# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Get-PSContentPath and Set-PSContentPath cmdlet tests" -tags "CI" {
    BeforeAll {
        # Backup any existing config files
        $configPath = (Get-PSContentPath).ConfigFile

        if (Test-Path $configPath) {
            $script:configBackup = Get-Content $configPath -Raw
        } else {
            $script:configBackup = $null
            # Ensure the parent directory exists for tests
            $configDir = Split-Path $configPath -Parent
            if (-not (Test-Path $configDir)) {
                $null = New-Item -Path $configDir -ItemType Directory -Force
            }
        }
    }

    AfterAll {
        # Restore original config if it existed
        if ($null -ne $script:configBackup) {
            Set-Content -Path $configPath -Value $script:configBackup -Force
        } elseif (Test-Path $configPath) {
            Remove-Item $configPath -Force -ErrorAction SilentlyContinue
        }
    }

    AfterEach {
        # Clean up any test config files created during tests
        if (Test-Path $configPath) {
            if ($null -ne $script:configBackup) {
                Set-Content -Path $configPath -Value $script:configBackup -Force
            } else {
                Remove-Item $configPath -Force -ErrorAction SilentlyContinue
            }
        }
    }

    Context "Get-PSContentPath default behavior" {
        It "Get-PSContentPath returns DirectoryInfo object with ConfigFile property" {
            $result = Get-PSContentPath

            # Should return a DirectoryInfo object
            $result.GetType().Name | Should -Be 'DirectoryInfo'

            # Should have a ConfigFile NoteProperty
            $result.PSObject.Properties['ConfigFile'] | Should -Not -BeNullOrEmpty
            $result.ConfigFile | Should -Not -BeNullOrEmpty
            $result.ConfigFile | Should -BeLike '*powershell.config.json'
        }

        It "Get-PSContentPath returns default Documents path when not configured" {
            # This test only works if no config was present at session start
            # Skip if a config already exists (indicates a custom path was set)
            $skipTest = Test-Path $configPath

            if (-not $skipTest) {
                $result = Get-PSContentPath

                # Default PSContentPath is Documents\PowerShell on Windows, XDG on Unix
                # Note: The config FILE is stored in LocalAppData, but the content PATH defaults to Documents
                if ($IsWindows) {
                    $expectedPath = Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'PowerShell'
                    $result.FullName | Should -Be $expectedPath
                } else {
                    # On Unix, should return XDG_DATA_HOME or ~/.local/share/powershell
                    $result.FullName | Should -Not -BeNullOrEmpty
                    $result.FullName | Should -BeLike '*powershell'
                }
            } else {
                Set-ItResult -Skipped -Because "Config file exists from previous test - PSContentPath is session-level"
                return
            }
        }

        It "Get-PSContentPath returns path without creating config file" {
            # Ensure no config exists
            if (Test-Path $configPath) {
                Remove-Item $configPath -Force
            }

            $result = Get-PSContentPath
            $result | Should -Not -BeNullOrEmpty

            # Config file should NOT be created just by calling Get-PSContentPath
            Test-Path $configPath | Should -Be $false
        }
    }

    Context "Set-PSContentPath custom path" {
        It "Set-PSContentPath creates config with custom path" {
            # Ensure no config exists
            if (Test-Path $configPath) {
                Remove-Item $configPath -Force
            }

            $customPath = if ($IsWindows) { "$env:TEMP\CustomPowerShell" } else { "/tmp/CustomPowerShell" }

            Set-PSContentPath -Path $customPath -WarningAction SilentlyContinue -Confirm:$false

            # Config file should now exist
            Test-Path $configPath | Should -Be $true

            # Verify custom path is stored
            $config = Get-Content $configPath -Raw | ConvertFrom-Json
            $config.PSUserContentPath | Should -Be $customPath
        }

        It "Set-PSContentPath expands environment variables on Windows" -Skip:(!$IsWindows) {
            Set-PSContentPath -Path '%TEMP%\PowerShell' -WarningAction SilentlyContinue -Confirm:$false

            $result = Get-PSContentPath
            # Normalize the expected path to handle short (8.3) vs long path names
            $expectedPath = [System.IO.Path]::GetFullPath("$env:TEMP\PowerShell")
            $result.FullName | Should -Be $expectedPath
            $result.FullName | Should -Not -BeLike '*%TEMP%*'
        }

        It "Set-PSContentPath validates path input" {
            { Set-PSContentPath -Path '' -WarningAction SilentlyContinue -Confirm:$false -ErrorAction Stop } | Should -Throw
        }

        It "Set-PSContentPath supports WhatIf" {
            if (Test-Path $configPath) {
                Remove-Item $configPath -Force
            }

            $customPath = if ($IsWindows) { "$env:TEMP\TestPath" } else { "/tmp/TestPath" }

            Set-PSContentPath -Path $customPath -WhatIf

            # Config file should NOT be created with -WhatIf
            Test-Path $configPath | Should -Be $false
        }
    }

    Context "Set-PSContentPath -Default" {
        It "Set-PSContentPath -Default resets config to platform default" {
            # First set a custom path
            $customPath = if ($IsWindows) { "$env:TEMP\CustomPowerShell" } else { "/tmp/CustomPowerShell" }
            Set-PSContentPath -Path $customPath -WarningAction SilentlyContinue -Confirm:$false

            # Verify it was set
            Test-Path $configPath | Should -Be $true
            $config = Get-Content $configPath -Raw | ConvertFrom-Json
            $config.PSUserContentPath | Should -Be $customPath

            # Reset to default
            Set-PSContentPath -Default -WarningAction SilentlyContinue -Confirm:$false

            # Verify the custom key was removed from config
            if (Test-Path $configPath) {
                $config = Get-Content $configPath -Raw | ConvertFrom-Json
                $config.PSObject.Properties['PSUserContentPath'] | Should -BeNullOrEmpty
            }
        }
    }

    Context "`$PSUserContentPath automatic variable" {
        It "`$PSUserContentPath should be a non-null string" {
            $PSUserContentPath | Should -Not -BeNullOrEmpty
            $PSUserContentPath | Should -BeOfType [string]
        }

        It "`$PSUserContentPath should match Get-PSContentPath in a clean session" {
            # Run in a fresh process to avoid interference from earlier Set-PSContentPath tests
            $pwsh = Join-Path $PSHOME 'pwsh'
            $result = & $pwsh -NoProfile -c '($PSUserContentPath -eq (Get-PSContentPath).FullName).ToString()'
            $result | Should -Be 'True'
        }

        It "`$PSUserContentPath should have ReadOnly and AllScope options" {
            $var = Get-Variable -Name PSUserContentPath -Scope Global
            $var.Options -band [System.Management.Automation.ScopedItemOptions]::ReadOnly | Should -Be ([System.Management.Automation.ScopedItemOptions]::ReadOnly)
            $var.Options -band [System.Management.Automation.ScopedItemOptions]::AllScope | Should -Be ([System.Management.Automation.ScopedItemOptions]::AllScope)
        }
    }

    Context "Legacy module path backward compatibility" {
        It "PSModulePath includes legacy Documents path when PSContentPath differs" {
            # Get current module path entries
            $modulePaths = $env:PSModulePath -split [System.IO.Path]::PathSeparator

            # Get the current personal module path (PSContentPath-based)
            $contentPath = (Get-PSContentPath).FullName
            $personalModulePath = Join-Path $contentPath "Modules"

            if ($IsWindows) {
                $legacyPath = Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'PowerShell' 'Modules'

                # If legacy and personal are different, both should be in PSModulePath
                if ($personalModulePath -ne $legacyPath) {
                    $modulePaths | Should -Contain $personalModulePath
                    $modulePaths | Should -Contain $legacyPath
                }
            }

            # Personal module path should always be in PSModulePath
            $modulePaths | Should -Contain $personalModulePath
        }
    }
}
