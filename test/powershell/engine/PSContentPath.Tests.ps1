# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Get-PSContentPath and Set-PSContentPath cmdlet tests" -tags "CI" {
    BeforeAll {
        # Backup any existing config files
        $documentsConfigPath = if ($IsWindows) {
            Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'PowerShell' 'powershell.config.json'
        } else {
            Join-Path $HOME '.config' 'powershell' 'powershell.config.json'
        }

        if (Test-Path $documentsConfigPath) {
            $script:configBackup = Get-Content $documentsConfigPath -Raw
        } else {
            $script:configBackup = $null
        }
    }

    AfterAll {
        # Restore original config if it existed
        if ($script:configBackup) {
            Set-Content -Path $documentsConfigPath -Value $script:configBackup -Force
        } elseif (Test-Path $documentsConfigPath) {
            Remove-Item $documentsConfigPath -Force -ErrorAction SilentlyContinue
        }
    }

    AfterEach {
        # Clean up any test config files created during tests
        if (Test-Path $documentsConfigPath) {
            if ($script:configBackup) {
                Set-Content -Path $documentsConfigPath -Value $script:configBackup -Force
            } else {
                Remove-Item $documentsConfigPath -Force -ErrorAction SilentlyContinue
            }
        }
    }

    Context "Get-PSContentPath default behavior" {
        It "Get-PSContentPath returns default Documents path when not configured" {
            # This test only works if no config was present at session start
            # Skip if a config already exists (indicates a custom path was set)
            $skipTest = Test-Path $documentsConfigPath

            if (-not $skipTest) {
                $result = Get-PSContentPath

                # Default should be Documents\PowerShell on Windows, XDG on Unix
                if ($IsWindows) {
                    $expectedPath = Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'PowerShell'
                    $result | Should -Be $expectedPath
                } else {
                    # On Unix, should return XDG_DATA_HOME or ~/.local/share/powershell
                    $result | Should -Not -BeNullOrEmpty
                    $result | Should -BeLike '*powershell'
                }
            } else {
                Set-ItResult -Skipped -Because "Config file exists from previous test - PSContentPath is session-level"
            }
        }

        It "Get-PSContentPath returns path without creating config file" {
            # Ensure no config exists
            if (Test-Path $documentsConfigPath) {
                Remove-Item $documentsConfigPath -Force
            }

            $result = Get-PSContentPath
            $result | Should -Not -BeNullOrEmpty

            # Config file should NOT be created just by calling Get-PSContentPath
            Test-Path $documentsConfigPath | Should -Be $false
        }
    }

    Context "Set-PSContentPath custom path" {
        It "Set-PSContentPath creates config with custom path" {
            # Ensure no config exists
            if (Test-Path $documentsConfigPath) {
                Remove-Item $documentsConfigPath -Force
            }

            $customPath = if ($IsWindows) { "$env:TEMP\CustomPowerShell" } else { "/tmp/CustomPowerShell" }

            Set-PSContentPath -Path $customPath -WarningAction SilentlyContinue

            # Config file should now exist
            Test-Path $documentsConfigPath | Should -Be $true

            # Verify custom path is stored
            $config = Get-Content $documentsConfigPath -Raw | ConvertFrom-Json
            $config.PSUserContentPath | Should -Be $customPath
        }

        It "Set-PSContentPath expands environment variables on Windows" -Skip:(!$IsWindows) {
            Set-PSContentPath -Path '%TEMP%\PowerShell' -WarningAction SilentlyContinue

            $result = Get-PSContentPath
            $result | Should -Be "$env:TEMP\PowerShell"
            $result | Should -Not -BeLike '*%TEMP%*'
        }

        It "Set-PSContentPath validates path input" {
            { Set-PSContentPath -Path '' -WarningAction SilentlyContinue -ErrorAction Stop } | Should -Throw
        }

        It "Set-PSContentPath supports WhatIf" {
            if (Test-Path $documentsConfigPath) {
                Remove-Item $documentsConfigPath -Force
            }

            $customPath = if ($IsWindows) { "$env:TEMP\TestPath" } else { "/tmp/TestPath" }

            Set-PSContentPath -Path $customPath -WhatIf

            # Config file should NOT be created with -WhatIf
            Test-Path $documentsConfigPath | Should -Be $false
        }
    }
}
