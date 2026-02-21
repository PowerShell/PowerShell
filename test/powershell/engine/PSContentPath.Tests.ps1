# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Get-PSContentPath and Set-PSContentPath cmdlet tests" -tags "CI" {
    BeforeAll {
        # Backup any existing config files
        # Config now defaults to LocalAppData instead of Documents on Windows
        $configPath = if ($IsWindows) {
            Join-Path $env:LOCALAPPDATA 'PowerShell' 'powershell.config.json'
        } else {
            Join-Path $HOME '.config' 'powershell' 'powershell.config.json'
        }

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
        if ($script:configBackup) {
            Set-Content -Path $configPath -Value $script:configBackup -Force
        } elseif (Test-Path $configPath) {
            Remove-Item $configPath -Force -ErrorAction SilentlyContinue
        }
    }

    AfterEach {
        # Clean up any test config files created during tests
        if (Test-Path $configPath) {
            if ($script:configBackup) {
                Set-Content -Path $configPath -Value $script:configBackup -Force
            } else {
                Remove-Item $configPath -Force -ErrorAction SilentlyContinue
            }
        }
    }

    Context "Get-PSContentPath default behavior" {
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

            Set-PSContentPath -Path $customPath -WarningAction SilentlyContinue

            # Config file should now exist
            Test-Path $configPath | Should -Be $true

            # Verify custom path is stored
            $config = Get-Content $configPath -Raw | ConvertFrom-Json
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
            if (Test-Path $configPath) {
                Remove-Item $configPath -Force
            }

            $customPath = if ($IsWindows) { "$env:TEMP\TestPath" } else { "/tmp/TestPath" }

            Set-PSContentPath -Path $customPath -WhatIf

            # Config file should NOT be created with -WhatIf
            Test-Path $configPath | Should -Be $false
        }
    }
}
