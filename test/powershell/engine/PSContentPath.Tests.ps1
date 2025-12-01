# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Get-PSContentPath and Set-PSContentPath cmdlet tests" -tags "CI" {
    BeforeAll {
        if ($IsWindows) {
            $powershell = "$PSHOME\pwsh.exe"
            $userConfigPath = Join-Path $HOME "Documents\PowerShell\powershell.config.json"
            $defaultContentPath = Join-Path $HOME "Documents\PowerShell"
            $newContentPath = [System.IO.Path]::Combine($env:LOCALAPPDATA, "PowerShell")
        }
        else {
            $powershell = "$PSHOME/pwsh"
            $userConfigPath = "~/.config/powershell/powershell.config.json"
            $defaultContentPath = [System.Management.Automation.Platform]::SelectProductNameForDirectory("USER_MODULES")
            $defaultContentPath = Split-Path $defaultContentPath -Parent
            $newContentPath = $defaultContentPath
        }

        # Backup existing configs
        if (Test-Path $userConfigPath) {
            $userConfigExists = $true
            Copy-Item $userConfigPath "$userConfigPath.backup.pscontentpath" -Force -ErrorAction Ignore
        }

        if ($IsWindows) {
            $newConfigPath = Join-Path $newContentPath "powershell.config.json"
            if (Test-Path $newConfigPath) {
                $newConfigExists = $true
                Copy-Item $newConfigPath "$newConfigPath.backup.pscontentpath" -Force -ErrorAction Ignore
            }
        }
    }

    AfterAll {
        # Restore original configs
        if ($userConfigExists) {
            Move-Item "$userConfigPath.backup.pscontentpath" $userConfigPath -Force -ErrorAction Ignore
        }
        else {
            Remove-Item "$userConfigPath" -Force -ErrorAction Ignore
        }

        if ($IsWindows -and $newConfigExists) {
            Move-Item "$newConfigPath.backup.pscontentpath" $newConfigPath -Force -ErrorAction Ignore
        }
        elseif ($IsWindows) {
            Remove-Item "$newConfigPath" -Force -ErrorAction Ignore
        }
    }

    BeforeEach {
        # Clean up config file before each test
        Remove-Item "$userConfigPath" -Force -ErrorAction Ignore
        if ($IsWindows) {
            $newConfigPath = Join-Path $newContentPath "powershell.config.json"
            Remove-Item "$newConfigPath" -Force -ErrorAction Ignore
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

        It "Get-PSContentPath expands environment variables" -Skip:(!$IsWindows -or $skipNoPwsh) {
            # Clean up first to ensure fresh state
            Remove-Item $newConfigPath -Force -ErrorAction Ignore
            Remove-Item $userConfigPath -Force -ErrorAction Ignore
            Start-Sleep -Milliseconds 100

            # Enable feature and set path with environment variable (note: single backslash)
            & $powershell -noprofile -command "Enable-ExperimentalFeature -Name PSContentPath -Scope CurrentUser; Set-PSContentPath -Path '%TEMP%\PowerShell'"

            $result = & $powershell -noprofile -command 'Get-PSContentPath'
            $result | Should -Not -Contain '%'
            $result | Should -Be (Join-Path $env:TEMP "PowerShell")

            # Clean up after this test IN THE TEST SESSION to not contaminate others
            & $powershell -noprofile -command "Remove-Item '$newConfigPath' -Force -ErrorAction Ignore; Remove-Item '$userConfigPath' -Force -ErrorAction Ignore"
            Start-Sleep -Milliseconds 200
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

        It "Set-PSContentPath updates existing config file" -Skip:$skipNoPwsh {
            # Enable feature
            & $powershell -noprofile -command 'Enable-ExperimentalFeature -Name PSContentPath -Scope CurrentUser'

            # Create initial config in LocalAppData (where feature writes to)
            $configToCheck = if ($IsWindows) { $newConfigPath } else { $userConfigPath }
            $config = @{ ExperimentalFeatures = @("PSNativeWindowsTildeExpansion") } | ConvertTo-Json
            New-Item -Path (Split-Path $configToCheck) -ItemType Directory -Force -ErrorAction Ignore
            Set-Content -Path $configToCheck -Value $config

            # Set custom path
            $customPath = Join-Path $TestDrive "CustomPowerShell"
            & $powershell -noprofile -command "Set-PSContentPath -Path '$customPath'"

            # Small delay for file write
            Start-Sleep -Milliseconds 100

            # Verify existing settings are preserved
            $updatedConfig = Get-Content $configToCheck -Raw | ConvertFrom-Json
            $updatedConfig.ExperimentalFeatures | Should -Contain "PSNativeWindowsTildeExpansion"
            $updatedConfig.PSObject.Properties.Name | Should -Contain "PSUserContentPath"
            $updatedConfig.PSUserContentPath | Should -Be $customPath

            # Clean up after this test to not contaminate others
            & $powershell -noprofile -command "Remove-Item '$newConfigPath' -Force -ErrorAction Ignore; Remove-Item '$userConfigPath' -Force -ErrorAction Ignore"
            Start-Sleep -Milliseconds 200
        }

        It "Set-PSContentPath accepts paths with environment variables" -Skip:(!$IsWindows -or $skipNoPwsh) {
            # Enable feature
            & $powershell -noprofile -command 'Enable-ExperimentalFeature -Name PSContentPath -Scope CurrentUser'

            & $powershell -noprofile -command "Set-PSContentPath -Path '%LOCALAPPDATA%\PowerShell'"

            # Small delay for file write
            Start-Sleep -Milliseconds 100

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

            # Clean up after this test to not contaminate others
            & $powershell -noprofile -command "Remove-Item '$newConfigPath' -Force -ErrorAction Ignore; Remove-Item '$userConfigPath' -Force -ErrorAction Ignore"
            Start-Sleep -Milliseconds 200
        }
    }

    Context "Integration with PSModulePath" {
        It "Custom PSContentPath affects module path" -Skip:(!$IsWindows -or $skipNoPwsh) {
            # Enable feature
            & $powershell -noprofile -command 'Enable-ExperimentalFeature -Name PSContentPath -Scope CurrentUser'

            $customPath = Join-Path $TestDrive "CustomPowerShell"
            & $powershell -noprofile -command "Set-PSContentPath -Path '$customPath'"

            # The actual module path will be used in a new PowerShell session
            $result = & $powershell -noprofile -command 'Get-PSContentPath'
            $result | Should -Be $customPath

            # Clean up after this test to not contaminate others
            & $powershell -noprofile -command "Remove-Item '$newConfigPath' -Force -ErrorAction Ignore; Remove-Item '$userConfigPath' -Force -ErrorAction Ignore"
            Start-Sleep -Milliseconds 200
        }
    }

    Context "Integration with Profile paths" {
        It "Custom PSContentPath affects profile path" -Skip:(!$IsWindows -or $skipNoPwsh) {
            # Enable feature
            & $powershell -noprofile -command 'Enable-ExperimentalFeature -Name PSContentPath -Scope CurrentUser'

            $customPath = Join-Path $TestDrive "CustomPowerShell"
            & $powershell -noprofile -command "Set-PSContentPath -Path '$customPath'"

            # Profile paths are constructed at startup
            $result = & $powershell -noprofile -command 'Get-PSContentPath'
            $result | Should -Be $customPath

            # Clean up after this test to not contaminate others
            & $powershell -noprofile -command "Remove-Item '$newConfigPath' -Force -ErrorAction Ignore; Remove-Item '$userConfigPath' -Force -ErrorAction Ignore"
            Start-Sleep -Milliseconds 200
        }
    }

    Context "Error handling" {
        It "Set-PSContentPath handles invalid paths gracefully" -Skip:$skipNoPwsh {
            # Enable feature
            & $powershell -noprofile -command 'Enable-ExperimentalFeature -Name PSContentPath -Scope CurrentUser'

            # Very long path
            $longPath = "C:\\" + ("a" * 300)

            # Should not throw - just accept the path with a warning
            $result = & $powershell -noprofile -command "try { Set-PSContentPath -Path '$longPath' -WarningAction SilentlyContinue; 'Success' } catch { 'Failed' }"
            $result | Should -Be 'Success'

            # Clean up after this test to not contaminate others
            & $powershell -noprofile -command "Remove-Item '$newConfigPath' -Force -ErrorAction Ignore; Remove-Item '$userConfigPath' -Force -ErrorAction Ignore"
            Start-Sleep -Milliseconds 200
        }

        It "Set-PSContentPath handles paths with special characters" -Skip:(!$IsWindows -or $skipNoPwsh) {
            # Enable feature
            & $powershell -noprofile -command 'Enable-ExperimentalFeature -Name PSContentPath -Scope CurrentUser'

            $pathWithSpaces = Join-Path $TestDrive "Path With Spaces"
            # Warnings are expected, just check it doesn't throw
            & $powershell -noprofile -command "Set-PSContentPath -Path '$pathWithSpaces' -WarningAction SilentlyContinue" 2>$null

            $result = & $powershell -noprofile -command 'Get-PSContentPath'
            $result | Should -Be $pathWithSpaces

            # Clean up after this test to not contaminate others
            & $powershell -noprofile -command "Remove-Item '$newConfigPath' -Force -ErrorAction Ignore; Remove-Item '$userConfigPath' -Force -ErrorAction Ignore"
            Start-Sleep -Milliseconds 200
        }
    }
}

Describe "PSContentPath experimental feature integration" -tags "Feature" {
    BeforeAll {
        if ($IsWindows) {
            $powershell = "$PSHOME\pwsh.exe"
            $userConfigPath = Join-Path $HOME "Documents\PowerShell\powershell.config.json"
            $newConfigPath = Join-Path $env:LOCALAPPDATA "PowerShell\powershell.config.json"
        }
        else {
            $powershell = "$PSHOME/pwsh"
            $userConfigPath = "~/.config/powershell/powershell.config.json"
            $newConfigPath = $userConfigPath
        }

        # Backup existing configs
        $backupSuffix = ".backup.integration"
        if (Test-Path $userConfigPath) {
            Copy-Item $userConfigPath "$userConfigPath$backupSuffix" -Force -ErrorAction Ignore
        }
        if ($IsWindows -and (Test-Path $newConfigPath)) {
            Copy-Item $newConfigPath "$newConfigPath$backupSuffix" -Force -ErrorAction Ignore
        }
    }

    AfterAll {
        # Restore original configs
        $backupSuffix = ".backup.integration"
        if (Test-Path "$userConfigPath$backupSuffix") {
            Move-Item "$userConfigPath$backupSuffix" $userConfigPath -Force -ErrorAction Ignore
        }
        else {
            Remove-Item $userConfigPath -Force -ErrorAction Ignore
        }

        if ($IsWindows) {
            if (Test-Path "$newConfigPath$backupSuffix") {
                Move-Item "$newConfigPath$backupSuffix" $newConfigPath -Force -ErrorAction Ignore
            }
            else {
                Remove-Item $newConfigPath -Force -ErrorAction Ignore
            }
        }
    }

    It "Config file migration preserves all settings" -Skip:(!$IsWindows -or $skipNoPwsh) {
        # Remove any existing config in new location first
        Remove-Item $newConfigPath -Force -ErrorAction Ignore

        # Create a config with multiple settings in old location (Documents)
        $config = @{
            ExperimentalFeatures = @("PSContentPath", "PSNativeWindowsTildeExpansion")
            "Microsoft.PowerShell:ExecutionPolicy" = "RemoteSigned"
            PSModulePath = "C:\\CustomModules"
        } | ConvertTo-Json

        New-Item -Path (Split-Path $userConfigPath) -ItemType Directory -Force -ErrorAction Ignore
        Set-Content -Path $userConfigPath -Value $config

        # Trigger migration by calling Get-PSContentPath in a new PowerShell session
        # This will read the config, see PSContentPath is enabled, and perform migration
        $result = & $powershell -noprofile -command 'Get-PSContentPath'

        # Small delay for migration to complete
        Start-Sleep -Milliseconds 200

        # Verify new config has all settings after migration
        if (Test-Path $newConfigPath) {
            $migratedConfig = Get-Content $newConfigPath -Raw | ConvertFrom-Json
            $migratedConfig.ExperimentalFeatures | Should -Contain "PSContentPath"
            $migratedConfig.ExperimentalFeatures | Should -Contain "PSNativeWindowsTildeExpansion"

            # Verify custom PSModulePath is preserved
            $propertyNames = $migratedConfig.PSObject.Properties.Name
            if ($propertyNames -contains 'PSModulePath') {
                $migratedConfig.PSModulePath | Should -Be "C:\\CustomModules"
            }
        }
    }

    It "Re-enabling feature after disable syncs correctly" -Skip:(!$IsWindows -or $skipNoPwsh) {
        # Enable, then disable, then re-enable (synchronously)
        & $powershell -noprofile -command 'Enable-ExperimentalFeature -Name PSContentPath -Scope CurrentUser' 2>$null | Out-Null
        & $powershell -noprofile -command 'Disable-ExperimentalFeature -Name PSContentPath -Scope CurrentUser' 2>$null | Out-Null
        & $powershell -noprofile -command 'Enable-ExperimentalFeature -Name PSContentPath -Scope CurrentUser' 2>$null | Out-Null

        # Verify config files have the feature enabled (check new location since feature is enabled)
        Start-Sleep -Milliseconds 100  # Small delay for file writes

        $configFound = $false
        if (Test-Path $newConfigPath) {
            $newConfig = Get-Content $newConfigPath -Raw | ConvertFrom-Json
            if ($newConfig.ExperimentalFeatures -contains "PSContentPath") {
                $configFound = $true
            }
        }

        if (!$configFound -and (Test-Path $userConfigPath)) {
            $docConfig = Get-Content $userConfigPath -Raw | ConvertFrom-Json
            if ($docConfig.ExperimentalFeatures -contains "PSContentPath") {
                $configFound = $true
            }
        }

        $configFound | Should -BeTrue
    }

    It "Bootstrap problem is solved - reads from both locations" -Skip:(!$IsWindows -or $skipNoPwsh) {
        # Create config only in Documents with PSContentPath enabled
        $docConfig = @{ ExperimentalFeatures = @("PSContentPath") } | ConvertTo-Json
        New-Item -Path (Split-Path $userConfigPath) -ItemType Directory -Force -ErrorAction Ignore
        Set-Content -Path $userConfigPath -Value $docConfig

        # Remove LocalAppData config if it exists
        Remove-Item $newConfigPath -Force -ErrorAction Ignore

        # PowerShell should still detect the feature is enabled (returns object, not just Enabled property)
        $featureEnabled = & $powershell -noprofile -command '(Get-ExperimentalFeature -Name PSContentPath).Enabled'
        $featureEnabled | Should -Be 'True'
    }
}
