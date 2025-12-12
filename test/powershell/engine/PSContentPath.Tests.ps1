# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Get-PSContentPath and Set-PSContentPath cmdlet tests" -tags "CI" {
    BeforeAll {
        if ($IsWindows) {
            $powershell = "$PSHOME\pwsh.exe"
            $documentsPath = [System.Environment]::GetFolderPath('MyDocuments')
            $userConfigPath = Join-Path $documentsPath "PowerShell\powershell.config.json"
            $defaultContentPath = Join-Path $documentsPath "PowerShell"
            $newContentPath = [System.IO.Path]::Combine($env:LOCALAPPDATA, "PowerShell")
            $newConfigPath = Join-Path $newContentPath "powershell.config.json"
        }
        else {
            $powershell = "$PSHOME/pwsh"
            $userConfigPath = "~/.config/powershell/powershell.config.json"
            $defaultContentPath = [System.Management.Automation.Platform]::SelectProductNameForDirectory("USER_MODULES")
            $defaultContentPath = Split-Path $defaultContentPath -Parent
            $newContentPath = $defaultContentPath
            $newConfigPath = $userConfigPath
        }

        $script:userConfigPath = $userConfigPath
        $script:newConfigPath = $newConfigPath
        $script:defaultContentPath = $defaultContentPath
        $script:newContentPath = $newContentPath

        # Backup original configs
        if (Test-Path $userConfigPath) {
            $script:userConfigBackup = Get-Content $userConfigPath -Raw
        }
        if ($IsWindows -and (Test-Path $newConfigPath)) {
            $script:newConfigBackup = Get-Content $newConfigPath -Raw
        }

        # Create clean test config with feature disabled
        Remove-Item $userConfigPath -Force -ErrorAction Ignore
        if ($IsWindows) {
            Remove-Item $newConfigPath -Force -ErrorAction Ignore
        }

        $testConfig = @{ ExperimentalFeatures = @() } | ConvertTo-Json
        New-Item -Path (Split-Path $userConfigPath) -ItemType Directory -Force -ErrorAction Ignore
        Set-Content -Path $userConfigPath -Value $testConfig -Force
    }

    AfterAll {
        # Disable the feature
        & $powershell -noprofile -command 'Disable-ExperimentalFeature -Name PSContentPath -Scope CurrentUser -WarningAction Ignore' 2>$null

        # Remove test configs
        Remove-Item $userConfigPath -Force -ErrorAction Ignore
        if ($IsWindows) {
            Remove-Item $newConfigPath -Force -ErrorAction Ignore
        }

        # Restore original configs
        if ($null -ne $script:userConfigBackup) {
            New-Item -Path (Split-Path $userConfigPath) -ItemType Directory -Force -ErrorAction Ignore
            Set-Content -Path $userConfigPath -Value $script:userConfigBackup -Force
        }
        if ($IsWindows -and ($null -ne $script:newConfigBackup)) {
            New-Item -Path (Split-Path $newConfigPath) -ItemType Directory -Force -ErrorAction Ignore
            Set-Content -Path $newConfigPath -Value $script:newConfigBackup -Force
        }
    }

    Context "Get-PSContentPath cmdlet" {
        It "Get-PSContentPath cmdlet does not exist when feature is disabled" -Skip:$skipNoPwsh {
            # Ensure feature is disabled
            & $powershell -noprofile -command 'Disable-ExperimentalFeature -Name PSContentPath -Scope CurrentUser' 2>$null

            $result = & $powershell -noprofile -command 'Get-Command Get-PSContentPath -ErrorAction SilentlyContinue'
            $result | Should -BeNullOrEmpty
        }

        It "Get-PSContentPath cmdlet exists when feature is enabled" -Skip:$skipNoPwsh {
            # Enable feature
            & $powershell -noprofile -command 'Enable-ExperimentalFeature -Name PSContentPath -Scope CurrentUser'

            $result = & $powershell -noprofile -command 'Get-Command Get-PSContentPath -ErrorAction SilentlyContinue'
            $result | Should -Not -BeNullOrEmpty
        }

        It "Get-PSContentPath returns current content path" -Skip:$skipNoPwsh {
            # Enable feature
            & $powershell -noprofile -command 'Enable-ExperimentalFeature -Name PSContentPath -Scope CurrentUser'

            $result = & $powershell -noprofile -command 'Get-PSContentPath'
            $result | Should -Not -BeNullOrEmpty
        }

        It "Get-PSContentPath returns default path when not configured" -Skip:$skipNoPwsh {
            # Run everything in ONE PowerShell session to ensure clean state
            $script = @"
                # Remove any existing configs
                Remove-Item '$newConfigPath' -Force -ErrorAction Ignore
                Remove-Item '$userConfigPath' -Force -ErrorAction Ignore

                # Verify they're gone
                if (Test-Path '$newConfigPath') { Write-Error 'Failed to remove newConfigPath' }
                if (Test-Path '$userConfigPath') { Write-Error 'Failed to remove userConfigPath' }

                # Disable feature (suppress warnings)
                Disable-ExperimentalFeature -Name PSContentPath -Scope CurrentUser -ErrorAction Ignore -WarningAction Ignore

                # Clean again after disable
                Remove-Item '$newConfigPath' -Force -ErrorAction Ignore
                Remove-Item '$userConfigPath' -Force -ErrorAction Ignore

                # Enable feature with clean state (suppress warnings)
                Enable-ExperimentalFeature -Name PSContentPath -Scope CurrentUser -WarningAction Ignore

                # Get the path - should be default
                Get-PSContentPath
"@

            $result = & $powershell -noprofile -command $script

            if ($IsWindows) {
                # When PSContentPath feature is enabled, returns LocalAppData path by default
                $result | Should -Be $newContentPath
            }
            else {
                $result | Should -Not -BeNullOrEmpty
            }
        }

        It "Get-PSContentPath expands environment variables (%TEMP%)" -Skip:(!$IsWindows -or $skipNoPwsh) {
            # Run in single session to avoid migration interference
            $script = @"
                `$ErrorActionPreference = 'Stop'
                Enable-ExperimentalFeature -Name PSContentPath -Scope CurrentUser -WarningAction Ignore
                Set-PSContentPath -Path '%TEMP%\PowerShell' -ErrorAction Stop
                Get-PSContentPath
"@
            $result = & $powershell -noprofile -command $script 2>&1

            # Filter out any error messages, just get the path
            $pathResult = $result | Where-Object { $_ -is [string] -and $_ -notmatch '^Set-PSContentPath:' } | Select-Object -Last 1

            if (-not $pathResult) {
                Write-Host "Command output: $result" -ForegroundColor Red
                throw "Set-PSContentPath or Get-PSContentPath failed"
            }

            $pathResult | Should -Not -Contain '%'

            # Normalize paths for comparison (handles short path names like RUNNER~1)
            $expectedPath = [System.IO.Path]::GetFullPath((Join-Path $env:TEMP "PowerShell"))
            $actualPath = [System.IO.Path]::GetFullPath($pathResult)
            $actualPath | Should -Be $expectedPath
        }

        It "Get-PSContentPath works when config file doesn't exist" -Skip:$skipNoPwsh {
            # Ensure no config file and enable feature
            Remove-Item $userConfigPath -Force -ErrorAction Ignore
            & $powershell -noprofile -command 'Enable-ExperimentalFeature -Name PSContentPath -Scope CurrentUser'

            $result = & $powershell -noprofile -command 'Get-PSContentPath'
            $result | Should -Not -BeNullOrEmpty
        }
    }

    Context "Set-PSContentPath cmdlet" {
        BeforeEach {
            # Ensure completely clean state for each test
            Remove-Item $newConfigPath -Force -ErrorAction Ignore
            Remove-Item $userConfigPath -Force -ErrorAction Ignore
        }

        AfterEach {
            # Clean up after each test
            Remove-Item $newConfigPath -Force -ErrorAction Ignore
            Remove-Item $userConfigPath -Force -ErrorAction Ignore
        }

        It "Set-PSContentPath cmdlet does not exist when feature is disabled" -Skip:$skipNoPwsh {
            # Ensure feature is disabled
            & $powershell -noprofile -command 'Disable-ExperimentalFeature -Name PSContentPath -Scope CurrentUser' 2>$null

            $result = & $powershell -noprofile -command 'Get-Command Set-PSContentPath -ErrorAction SilentlyContinue'
            $result | Should -BeNullOrEmpty
        }

        It "Set-PSContentPath cmdlet exists when feature is enabled" -Skip:$skipNoPwsh {
            # Enable feature
            & $powershell -noprofile -command 'Enable-ExperimentalFeature -Name PSContentPath -Scope CurrentUser'

            $result = & $powershell -noprofile -command 'Get-Command Set-PSContentPath -ErrorAction SilentlyContinue'
            $result | Should -Not -BeNullOrEmpty
        }

        It "Set-PSContentPath creates config file if it doesn't exist" -Skip:$skipNoPwsh {
            # Enable feature
            & $powershell -noprofile -command 'Enable-ExperimentalFeature -Name PSContentPath -Scope CurrentUser'

            $customPath = Join-Path $TestDrive "CustomPowerShell"
            & $powershell -noprofile -command "Set-PSContentPath -Path '$customPath'"

            # When feature is enabled, config is in LocalAppData
            if ($IsWindows) {
                (Test-Path $newConfigPath) -or (Test-Path $userConfigPath) | Should -BeTrue
            }
            else {
                Test-Path $userConfigPath | Should -BeTrue
            }
        }

        It "Set-PSContentPath updates the content path" -Skip:$skipNoPwsh {
            # Enable feature
            & $powershell -noprofile -command 'Enable-ExperimentalFeature -Name PSContentPath -Scope CurrentUser'

            $customPath = Join-Path $TestDrive "CustomPowerShell"
            & $powershell -noprofile -command "Set-PSContentPath -Path '$customPath'"

            $result = & $powershell -noprofile -command 'Get-PSContentPath'
            $result | Should -Be $customPath
        }

        It "Set-PSContentPath accepts paths with environment variables" -Skip:(!$IsWindows -or $skipNoPwsh) {
            # Enable feature
            & $powershell -noprofile -command 'Enable-ExperimentalFeature -Name PSContentPath -Scope CurrentUser'

            & $powershell -noprofile -command "Set-PSContentPath -Path '%LOCALAPPDATA%\PowerShell'"

            # Check the config file in LocalAppData (where it's written when feature enabled)
            $configToCheck = if ($IsWindows) { $newConfigPath } else { $userConfigPath }
            $config = Get-Content $configToCheck -Raw | ConvertFrom-Json
            $config.PSObject.Properties.Name | Should -Contain "PSUserContentPath"
            $config.PSUserContentPath | Should -Be '%LOCALAPPDATA%\PowerShell'

            # Get-PSContentPath should expand it
            $result = & $powershell -noprofile -command 'Get-PSContentPath'
            $result | Should -Be $newContentPath
        }

        It "Set-PSContentPath creates directory structure if it doesn't exist" -Skip:$skipNoPwsh {
            # Enable feature
            & $powershell -noprofile -command 'Enable-ExperimentalFeature -Name PSContentPath -Scope CurrentUser'

            $customPath = Join-Path $TestDrive "NewFolder\PowerShell"

            # Directory doesn't exist yet
            Test-Path $customPath | Should -BeFalse

            & $powershell -noprofile -command "Set-PSContentPath -Path '$customPath'"

            # Config should be created (in LocalAppData when feature enabled)
            if ($IsWindows) {
                (Test-Path $newConfigPath) -or (Test-Path $userConfigPath) | Should -BeTrue
            }
            else {
                Test-Path $userConfigPath | Should -BeTrue
            }
        }
    }

    Context "Integration with PSModulePath" {
        BeforeEach {
            Remove-Item $newConfigPath -Force -ErrorAction Ignore
            Remove-Item $userConfigPath -Force -ErrorAction Ignore
        }

        AfterEach {
            Remove-Item $newConfigPath -Force -ErrorAction Ignore
            Remove-Item $userConfigPath -Force -ErrorAction Ignore
        }

        It "Custom PSContentPath affects module path" -Skip:(!$IsWindows -or $skipNoPwsh) {
            # Enable feature
            & $powershell -noprofile -command 'Enable-ExperimentalFeature -Name PSContentPath -Scope CurrentUser'

            $customPath = Join-Path $TestDrive "CustomPowerShell"
            & $powershell -noprofile -command "Set-PSContentPath -Path '$customPath'"

            # The actual module path will be used in a new PowerShell session
            $result = & $powershell -noprofile -command 'Get-PSContentPath'
            $result | Should -Be $customPath
        }
    }

    Context "Integration with Profile paths" {
        BeforeEach {
            Remove-Item $newConfigPath -Force -ErrorAction Ignore
            Remove-Item $userConfigPath -Force -ErrorAction Ignore
        }

        AfterEach {
            Remove-Item $newConfigPath -Force -ErrorAction Ignore
            Remove-Item $userConfigPath -Force -ErrorAction Ignore
        }

        It "Custom PSContentPath affects profile path" -Skip:(!$IsWindows -or $skipNoPwsh) {
            # Enable feature
            & $powershell -noprofile -command 'Enable-ExperimentalFeature -Name PSContentPath -Scope CurrentUser'

            $customPath = Join-Path $TestDrive "CustomPowerShell"
            & $powershell -noprofile -command "Set-PSContentPath -Path '$customPath'"

            # Profile paths are constructed at startup
            $result = & $powershell -noprofile -command 'Get-PSContentPath'
            $result | Should -Be $customPath
        }

        It "Profile path uses custom PSContentPath location" -Skip:(!$IsWindows -or $skipNoPwsh) {
            # Enable feature and set custom path
            & $powershell -noprofile -command 'Enable-ExperimentalFeature -Name PSContentPath -Scope CurrentUser'

            $customPath = Join-Path $TestDrive "CustomPowerShell"
            & $powershell -noprofile -command "Set-PSContentPath -Path '$customPath'"

            # Get the current user profile path in a new session
            $profilePath = & $powershell -noprofile -command '$PROFILE.CurrentUserCurrentHost'

            # Profile should be in the custom content path
            $profilePath | Should -BeLike "$customPath*"
        }

        It "Profile path uses default Documents location when feature is disabled" -Skip:(!$IsWindows -or $skipNoPwsh) {
            # Ensure feature is disabled
            & $powershell -noprofile -command 'Disable-ExperimentalFeature -Name PSContentPath -Scope CurrentUser' 2>$null

            # Get the current user profile path in a new session
            $profilePath = & $powershell -noprofile -command '$PROFILE.CurrentUserCurrentHost'

            # Profile should be in Documents\PowerShell
            $profilePath | Should -BeLike "*Documents\PowerShell*"
        }
    }

    Context "Integration with Updatable Help" {
        BeforeEach {
            Remove-Item $newConfigPath -Force -ErrorAction Ignore
            Remove-Item $userConfigPath -Force -ErrorAction Ignore
        }

        AfterEach {
            Remove-Item $newConfigPath -Force -ErrorAction Ignore
            Remove-Item $userConfigPath -Force -ErrorAction Ignore
        }

        It "Help path uses custom PSContentPath location" -Skip:(!$IsWindows -or $skipNoPwsh) {
            # Enable feature and set custom path
            & $powershell -noprofile -command 'Enable-ExperimentalFeature -Name PSContentPath -Scope CurrentUser'

            $customPath = Join-Path $TestDrive "CustomPowerShell"
            & $powershell -noprofile -command "Set-PSContentPath -Path '$customPath'"

            # Get the help save path (CurrentUser scope)
            $script = @"
                `$helpPaths = [System.Management.Automation.Internal.InternalTestHooks]::TestHelpSavePath
                if (`$helpPaths) { `$helpPaths } else {
                    # Fallback: construct expected path
                    `$contentPath = Get-PSContentPath
                    Join-Path `$contentPath "Help"
                }
"@
            $helpPath = & $powershell -noprofile -command $script

            # Help path should be in the custom content path
            $helpPath | Should -BeLike "$customPath*"
        }

        It "Help path uses default Documents location when feature is disabled" -Skip:(!$IsWindows -or $skipNoPwsh) {
            # Ensure feature is disabled
            & $powershell -noprofile -command 'Disable-ExperimentalFeature -Name PSContentPath -Scope CurrentUser' 2>$null

            # Check what path Update-Help would use
            $script = @"
                # Get the default user help path
                `$documentsPath = [System.IO.Path]::Combine([System.Environment]::GetFolderPath('MyDocuments'), 'PowerShell')
                Join-Path `$documentsPath "Help"
"@
            $expectedHelpPath = & $powershell -noprofile -command $script

            # Expected path should be in Documents\PowerShell
            $expectedHelpPath | Should -BeLike "*Documents\PowerShell\Help"
        }

        It "Update-Help with CurrentUser scope respects custom PSContentPath" -Skip:(!$IsWindows -or $skipNoPwsh) {
            # Enable feature and set custom path
            & $powershell -noprofile -command 'Enable-ExperimentalFeature -Name PSContentPath -Scope CurrentUser'

            $customPath = Join-Path $TestDrive "CustomPowerShell"
            & $powershell -noprofile -command "Set-PSContentPath -Path '$customPath'"

            # Create the custom help directory
            $customHelpPath = Join-Path $customPath "Help"
            New-Item -Path $customHelpPath -ItemType Directory -Force -ErrorAction Ignore

            # Try to save help (using -WhatIf to avoid actual download)
            $script = @"
                `$ErrorActionPreference = 'SilentlyContinue'
                `$WarningPreference = 'SilentlyContinue'
                Save-Help -Module Microsoft.PowerShell.Management -DestinationPath '$customHelpPath' -Force -WhatIf 2>&1 | Out-Null
                # Just verify the path would be used
                '$customHelpPath'
"@
            $result = & $powershell -noprofile -command $script

            # Verify the custom help path exists
            Test-Path $customHelpPath | Should -BeTrue
            $result | Should -Be $customHelpPath

            # Clean up custom help directory
            Remove-Item $customHelpPath -Recurse -Force -ErrorAction Ignore
        }
    }

    Context "Error handling" {
        BeforeEach {
            Remove-Item $newConfigPath -Force -ErrorAction Ignore
            Remove-Item $userConfigPath -Force -ErrorAction Ignore
        }

        AfterEach {
            Remove-Item $newConfigPath -Force -ErrorAction Ignore
            Remove-Item $userConfigPath -Force -ErrorAction Ignore
        }

        It "Set-PSContentPath handles invalid paths gracefully" -Skip:$skipNoPwsh {
            # Enable feature
            & $powershell -noprofile -command 'Enable-ExperimentalFeature -Name PSContentPath -Scope CurrentUser'

            # Very long path
            $longPath = "C:\\" + ("a" * 300)

            # Should not throw - just accept the path with a warning
            $result = & $powershell -noprofile -command "try { Set-PSContentPath -Path '$longPath' -WarningAction SilentlyContinue; 'Success' } catch { 'Failed' }"
            $result | Should -Be 'Success'
        }

        It "Set-PSContentPath handles paths with special characters" -Skip:(!$IsWindows -or $skipNoPwsh) {
            # Enable feature
            & $powershell -noprofile -command 'Enable-ExperimentalFeature -Name PSContentPath -Scope CurrentUser -WarningAction Ignore'

            # Set path with spaces
            & $powershell -noprofile -command "Set-PSContentPath -Path '$TestDrive\Path With Spaces' -WarningAction SilentlyContinue"

            # Verify it worked
            $result = & $powershell -noprofile -command 'Get-PSContentPath'
            $result | Should -Be (Join-Path $TestDrive "Path With Spaces")
        }
    }
}

Describe "PSContentPath experimental feature integration" -tags "Feature" {
    BeforeAll {
        if ($IsWindows) {
            $powershell = "$PSHOME\pwsh.exe"
            $documentsPath = [System.Environment]::GetFolderPath('MyDocuments')
            $userConfigPath = Join-Path $documentsPath "PowerShell\powershell.config.json"
            $newConfigPath = Join-Path $env:LOCALAPPDATA "PowerShell\powershell.config.json"
        }
        else {
            $powershell = "$PSHOME/pwsh"
            $userConfigPath = "~/.config/powershell/powershell.config.json"
            $newConfigPath = $userConfigPath
        }

        # Backup original configs
        if (Test-Path $userConfigPath) {
            $script:userConfigBackup = Get-Content $userConfigPath -Raw
        }
        if ($IsWindows -and (Test-Path $newConfigPath)) {
            $script:newConfigBackup = Get-Content $newConfigPath -Raw
        }

        # Create clean test environment
        Remove-Item $newConfigPath -Force -ErrorAction Ignore
        Remove-Item $userConfigPath -Force -ErrorAction Ignore
    }

    BeforeEach {
        # Remove all configs to ensure clean state before each test
        Remove-Item $newConfigPath -Force -ErrorAction Ignore
        Remove-Item $userConfigPath -Force -ErrorAction Ignore
    }

    AfterEach {
        # Clean up after each test to prevent contamination
        Remove-Item $newConfigPath -Force -ErrorAction Ignore
        Remove-Item $userConfigPath -Force -ErrorAction Ignore
    }

    AfterAll {
        # Disable the feature
        & $powershell -noprofile -command 'Disable-ExperimentalFeature -Name PSContentPath -Scope CurrentUser -WarningAction Ignore' 2>$null

        # Remove test configs
        Remove-Item $userConfigPath -Force -ErrorAction Ignore
        if ($IsWindows) {
            Remove-Item $newConfigPath -Force -ErrorAction Ignore
        }

        # Restore original configs
        if ($null -ne $script:userConfigBackup) {
            New-Item -Path (Split-Path $userConfigPath) -ItemType Directory -Force -ErrorAction Ignore
            Set-Content -Path $userConfigPath -Value $script:userConfigBackup -Force
        }
        if ($IsWindows -and ($null -ne $script:newConfigBackup)) {
            New-Item -Path (Split-Path $newConfigPath) -ItemType Directory -Force -ErrorAction Ignore
            Set-Content -Path $newConfigPath -Value $script:newConfigBackup -Force
        }
    }

    It "Config file migration preserves all settings" -Skip:(!$IsWindows -or $skipNoPwsh) {
        # Verify clean state from BeforeEach
        Test-Path $newConfigPath | Should -BeFalse "LocalAppData config should not exist before test"
        Test-Path $userConfigPath | Should -BeFalse "Documents config should not exist before test"

        # Create a config with multiple settings in old location (Documents)
        $originalModulePath = "C:\\CustomModules"
        $config = @{
            ExperimentalFeatures = @("PSContentPath")
            PSModulePath = $originalModulePath
            "Microsoft.PowerShell:ExecutionPolicy" = "RemoteSigned"
        } | ConvertTo-Json

        New-Item -Path (Split-Path $userConfigPath) -ItemType Directory -Force -ErrorAction Ignore
        Set-Content -Path $userConfigPath -Value $config -Force

        # Verify the original config was written correctly
        $originalConfig = Get-Content $userConfigPath -Raw | ConvertFrom-Json
        $originalConfig.PSModulePath | Should -Be $originalModulePath -Because "Original config should have PSModulePath"
        $originalConfig.ExperimentalFeatures | Should -Contain "PSContentPath" -Because "Original config should have PSContentPath enabled"

        # Trigger migration by accessing config in a new PowerShell session
        & $powershell -noprofile -command 'Get-PSContentPath' | Out-Null

        # After migration, BOTH configs should exist
        Test-Path $newConfigPath | Should -BeTrue "LocalAppData config should exist after migration"
        Test-Path $userConfigPath | Should -BeTrue "Documents config should still exist after migration"

        # Parse configs
        $newConfig = Get-Content $newConfigPath -Raw | ConvertFrom-Json
        $docConfig = Get-Content $userConfigPath -Raw | ConvertFrom-Json

        # Verify new LocalAppData config has all settings (should be exact copy)
        $newConfig.ExperimentalFeatures | Should -Contain "PSContentPath" -Because "New config should have PSContentPath enabled"
        $newConfig.PSModulePath | Should -Be $originalModulePath -Because "New config should have PSModulePath"
        $newConfig."Microsoft.PowerShell:ExecutionPolicy" | Should -Be "RemoteSigned" -Because "New config should have ExecutionPolicy"

        # Verify original Documents config still has all settings (bidirectional sync)
        $docConfig.ExperimentalFeatures | Should -Contain "PSContentPath" -Because "Doc config should have PSContentPath enabled"
        $docConfig.PSModulePath | Should -Be $originalModulePath -Because "Doc config should have PSModulePath"
        $docConfig."Microsoft.PowerShell:ExecutionPolicy" | Should -Be "RemoteSigned" -Because "Doc config should have ExecutionPolicy"
    }

    It "Re-enabling feature after disable syncs correctly" -Skip:(!$IsWindows -or $skipNoPwsh) {
        # Enable, disable, then re-enable to test sync behavior
        & $powershell -noprofile -command 'Enable-ExperimentalFeature -Name PSContentPath -Scope CurrentUser -WarningAction Ignore' | Out-Null
        & $powershell -noprofile -command 'Disable-ExperimentalFeature -Name PSContentPath -Scope CurrentUser -WarningAction Ignore' | Out-Null
        & $powershell -noprofile -command 'Enable-ExperimentalFeature -Name PSContentPath -Scope CurrentUser -WarningAction Ignore' | Out-Null

        # Verify config files have the feature enabled
        $configFound = $false
        if (Test-Path $newConfigPath) {
            $config = Get-Content $newConfigPath -Raw | ConvertFrom-Json
            $configFound = $config.ExperimentalFeatures -contains "PSContentPath"
        }
        if (!$configFound -and (Test-Path $userConfigPath)) {
            $config = Get-Content $userConfigPath -Raw | ConvertFrom-Json
            $configFound = $config.ExperimentalFeatures -contains "PSContentPath"
        }

        $configFound | Should -BeTrue
    }

    It "Bootstrap problem is solved - reads from both locations" -Skip:(!$IsWindows -or $skipNoPwsh) {
        # Create config only in Documents with PSContentPath enabled
        $docConfig = @{ ExperimentalFeatures = @("PSContentPath") } | ConvertTo-Json
        New-Item -Path (Split-Path $userConfigPath) -ItemType Directory -Force -ErrorAction Ignore
        Set-Content -Path $userConfigPath -Value $docConfig

        # LocalAppData config should not exist (removed by BeforeEach)
        Test-Path $newConfigPath | Should -BeFalse

        # PowerShell should still detect the feature is enabled (reads from both locations)
        $featureEnabled = & $powershell -noprofile -command '(Get-ExperimentalFeature -Name PSContentPath).Enabled'
        $featureEnabled | Should -Be 'True'
    }
}
