# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Import-Module HelpersCommon

Describe "Enable-ExperimentalFeature and Disable-ExperimentalFeature tests" -tags "Feature","RequireAdminOnWindows" {

    BeforeAll {
        $pwsh = "$PSHOME/pwsh"
        $systemConfigPath = (Get-PowerShellConfiguration -Scope AllUsers).Path
        $userConfigPath = (Get-PowerShellConfiguration -Scope CurrentUser).Path

        # The legacy $PSHOME config may exist (e.g., preview builds).
        # Back it up so tests start clean. Also track it separately because
        # writes go to the new platform location, not the legacy path.
        $legacyConfigPath = Join-Path $PSHOME "powershell.config.json"
        $legacyConfigExists = $false
        if (($legacyConfigPath -ne $systemConfigPath) -and (Test-Path $legacyConfigPath)) {
            $legacyConfigExists = $true
            Move-Item $legacyConfigPath "$legacyConfigPath.backup" -Force -ErrorAction SilentlyContinue
        }

        $systemConfigDir = Split-Path $systemConfigPath
        if (!(Test-Path $systemConfigDir)) {
            $null = New-Item -ItemType Directory -Path $systemConfigDir -Force -ErrorAction SilentlyContinue
        }

        $userConfigDir = Split-Path $userConfigPath
        if (!(Test-Path $userConfigDir)) {
            $null = New-Item -ItemType Directory -Path $userConfigDir -Force -ErrorAction SilentlyContinue
        }

        $systemConfigExists = $false
        if (Test-Path $systemConfigPath) {
            $systemConfigExists = $true
            Move-Item $systemConfigPath "$systemConfigPath.backup" -Force -ErrorAction SilentlyContinue
        }

        $userConfigExists = $false
        if (Test-Path $userConfigPath) {
            $userConfigExists = $true
            Move-Item $userConfigPath "$userConfigPath.backup" -Force -ErrorAction SilentlyContinue
        }

        $testModulePath = Join-Path -Path $PSScriptRoot -ChildPath "assets"
        $originalModulePath = $env:PSModulePath
        $env:PSModulePath = $testModulePath
    }

    AfterAll {
        if ($legacyConfigExists) {
            Move-Item "$legacyConfigPath.backup" $legacyConfigPath -Force -ErrorAction SilentlyContinue
        }

        if ($systemConfigExists) {
            Move-Item "$systemConfigPath.backup" $systemConfigPath -Force -ErrorAction SilentlyContinue
        }

        if ($userConfigExists) {
            Move-Item "$userConfigPath.backup" $userConfigPath -Force -ErrorAction SilentlyContinue
        }

        $env:PSModulePath = $originalModulePath
    }

    AfterEach {
        Remove-Item $systemConfigPath -Force -ErrorAction SilentlyContinue
        Remove-Item $userConfigPath -Force -ErrorAction SilentlyContinue
        if ($legacyConfigPath -ne $systemConfigPath) {
            Remove-Item $legacyConfigPath -Force -ErrorAction SilentlyContinue
        }
    }

    It "Enable-ExperimentalFeature will enable Experimental Feature for scope: <scope>" -TestCases @(
        @{ scope = "AllUsers" },
        @{ scope = "CurrentUser" }
    ) {
        param ($scope)

        if (!(Test-CanWriteToSystemConfigDir) -and $scope -eq "AllUsers") {
            return
        }

        $feature = & $pwsh -noprofile -output xml -command Get-ExperimentalFeature ExpTest.FeatureOne
        $feature.Enabled | Should -BeFalse -Because "All Experimental Features disabled when no config file"
        $feature = & $pwsh -noprofile -output xml -command Enable-ExperimentalFeature ExpTest.FeatureOne -Scope $scope -WarningAction SilentlyContinue
        $feature | Should -BeNullOrEmpty -Because "No object is output to pipeline on success"
        $feature = & $pwsh -noprofile -output xml -command Get-ExperimentalFeature ExpTest.FeatureOne
        $feature.Enabled | Should -BeTrue -Because "The experimental feature is now enabled"
    }

    It "Disable-ExperimentalFeature will disable Experimental Feature for scope: <scope>" -TestCases @(
        @{ scope = "AllUsers"   ; configPath = $systemConfigPath },
        @{ scope = "CurrentUser"; configPath = $userConfigPath }
    ) {
        param ($scope, $configPath)

        if (!(Test-CanWriteToSystemConfigDir) -and $scope -eq "AllUsers") {
            return
        }

        '{"ExperimentalFeatures":["ExpTest.FeatureOne"]}' > $configPath
        $feature = & $pwsh -noprofile -output xml -command Get-ExperimentalFeature ExpTest.FeatureOne
        $feature.Enabled | Should -BeTrue -Because "Test config should enable ExpTest.FeatureOne"
        $feature = & $pwsh -noprofile -output xml -command Disable-ExperimentalFeature ExpTest.FeatureOne -Scope $scope -WarningAction SilentlyContinue
        $feature | Should -BeNullOrEmpty -Because "No object is output to pipeline on success"
        $feature = & $pwsh -noprofile -output xml -command Get-ExperimentalFeature ExpTest.FeatureOne
        $feature.Enabled | Should -BeFalse -Because "The experimental feature is now disabled"
    }

    It "<cmdlet> will output warning message" -TestCases @(
        @{ cmdlet = "Enable-ExperimentalFeature" },
        @{ cmdlet = "Disable-Experimentalfeature" }
    ) {
        param ($cmdlet)

        & $cmdlet ExpTest.FeatureOne -WarningVariable warning -WarningAction SilentlyContinue
        $warning | Should -Not -BeNullOrEmpty -Because "A warning message is always given indicating restart is required"
    }

    It "Multiple features enabled will only output one warning message for <cmdlet>" -TestCases @(
        @{ cmdlet = "Enable-ExperimentalFeature" },
        @{ cmdlet = "Disable-Experimentalfeature" }
    ) {
        param ($cmdlet)

        Get-ExperimentalFeature | & $cmdlet -WarningAction SilentlyContinue -WarningVariable warning
        $warning | Should -HaveCount 1
    }
}
