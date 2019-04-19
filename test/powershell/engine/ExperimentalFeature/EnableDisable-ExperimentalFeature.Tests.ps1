# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Import-Module HelpersCommon

Describe "Enable-ExperimentalFeature and Disable-ExperimentalFeature tests" -tags "Feature","RequireAdminOnWindows" {

    BeforeAll {
        $systemConfigPath = "$PSHOME/powershell.config.json"
        if ($IsWindows) {
            $userConfigPath = "~/Documents/powershell/powershell.config.json"
        }
        else {
            $userConfigPath = "~/.config/powershell/powershell.config.json"
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
    }

    It "Enable-ExperimentalFeature will enable Experimental Feature for scope: <scope>" -TestCases @(
        @{ scope = "AllUsers" },
        @{ scope = "CurrentUser" }
    ) {
        param ($scope)

        if (!(Test-CanWriteToPsHome) -and $scope -eq "AllUsers") {
            return
        }

        $feature = pwsh -noprofile -output xml -command Get-ExperimentalFeature ExpTest.FeatureOne
        $feature.Enabled | Should -BeFalse -Because "All Experimental Features disabled when no config file"
        $feature = pwsh -noprofile -output xml -command Enable-ExperimentalFeature ExpTest.FeatureOne -Scope $scope -WarningAction SilentlyContinue
        $feature | Should -BeNullOrEmpty -Because "No object is output to pipeline on success"
        $feature = pwsh -noprofile -output xml -command Get-ExperimentalFeature ExpTest.FeatureOne
        $feature.Enabled | Should -BeTrue -Because "The experimental feature is now enabled"
    }

    It "Disable-ExperimentalFeature will disable Experimental Feature for scope: <scope>" -TestCases @(
        @{ scope = "AllUsers"   ; configPath = $systemConfigPath },
        @{ scope = "CurrentUser"; configPath = $userConfigPath }
    ) {
        param ($scope, $configPath)

        if (!(Test-CanWriteToPsHome) -and $scope -eq "AllUsers") {
            return
        }

        '{"ExperimentalFeatures":["ExpTest.FeatureOne"]}' > $configPath
        $feature = pwsh -noprofile -output xml -command Get-ExperimentalFeature ExpTest.FeatureOne
        $feature.Enabled | Should -BeTrue -Because "Test config should enable ExpTest.FeatureOne"
        $feature = pwsh -noprofile -output xml -command Disable-ExperimentalFeature ExpTest.FeatureOne -Scope $scope -WarningAction SilentlyContinue
        $feature | Should -BeNullOrEmpty -Because "No object is output to pipeline on success"
        $feature = pwsh -noprofile -output xml -command Get-ExperimentalFeature ExpTest.FeatureOne
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
}
