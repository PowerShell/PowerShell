# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Import-Module HelpersCommon

Describe "Get-ExperimentalFeature Tests" -tags "Feature","RequireAdminOnWindows" {

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
        $testModuleManifestPath = Join-Path -Path $testModulePath "ExpTest" "ExpTest.psd1"
        $originalModulePath = $env:PSModulePath
        $env:PSModulePath = $testModulePath
    }

    AfterAll {
        if ($systemConfigExists -and (Test-CanWriteToPsHome)) {
            Move-Item "$systemConfigPath.backup" $systemConfigPath -Force -ErrorAction SilentlyContinue
        }

        if ($userConfigExists) {
            Move-Item "$userConfigPath.backup" $userConfigPath -Force -ErrorAction SilentlyContinue
        }

        $env:PSModulePath = $originalModulePath
    }

    AfterEach {
        if (Test-CanWriteToPsHome) {
            Remove-Item $systemConfigPath -Force -ErrorAction SilentlyContinue
        }

        Remove-Item $userConfigPath -Force -ErrorAction SilentlyContinue
    }

    Context "Feature disabled tests" {

        It "'Get-ExperimentalFeature' should return all available features from module path" {
            $features = pwsh -noprofile -output xml -command Get-ExperimentalFeature "ExpTest*"
            $features | Should -Not -BeNullOrEmpty
            $features[0].Name | Should -BeExactly "ExpTest.FeatureOne"
            $features[0].Enabled | Should -BeFalse
            $features[0].Source | Should -BeExactly $testModuleManifestPath

            $features[1].Name | Should -BeExactly "ExpTest.FeatureTwo"
            $features[1].Enabled | Should -BeFalse
            $features[1].Source | Should -BeExactly $testModuleManifestPath
        }

        It "'Get-ExperimentalFeature' pipeline input" {
            $features = pwsh -noprofile -output xml -command { "ExpTest.FeatureOne", "ExpTest.FeatureTwo" | Get-ExperimentalFeature }
            $features | Should -Not -BeNullOrEmpty
            $features[0].Name | Should -BeExactly "ExpTest.FeatureOne"
            $features[0].Enabled | Should -BeFalse
            $features[0].Source | Should -BeExactly $testModuleManifestPath

            $features[1].Name | Should -BeExactly "ExpTest.FeatureTwo"
            $features[1].Enabled | Should -BeFalse
            $features[1].Source | Should -BeExactly $testModuleManifestPath
        }
    }

    Context "Feature enabled tests" {
        BeforeEach {
            '{"ExperimentalFeatures":["ExpTest.FeatureOne"]}' > $userConfigPath
        }

        It "'Get-ExperimentalFeature' should return enabled features 'ExpTest.FeatureOne'" {
            pwsh -noprofile -command '$EnabledExperimentalFeatures.Count' | Should -Be 1
            $feature = pwsh -noprofile -output xml -command Get-ExperimentalFeature "ExpTest.FeatureOne"
            $feature | Should -Not -BeNullOrEmpty
            $feature.Enabled | Should -BeTrue
            $feature.Source | Should -BeExactly $testModuleManifestPath
        }

        It "'Get-ExperimentalFeature' should return all available features from module path" {
            $features = pwsh -noprofile -output xml -command Get-ExperimentalFeature "ExpTest*"
            $features | Should -Not -BeNullOrEmpty
            $features[0].Name | Should -BeExactly "ExpTest.FeatureOne"
            $features[0].Enabled | Should -BeTrue
            $features[0].Source | Should -BeExactly $testModuleManifestPath

            $features[1].Name | Should -BeExactly "ExpTest.FeatureTwo"
            $features[1].Enabled | Should -BeFalse
            $features[1].Source | Should -BeExactly $testModuleManifestPath
        }

        It "'Get-ExperimentalFeature' pipeline input" {
            $features = pwsh -noprofile -output xml -command  { "ExpTest.FeatureOne", "ExpTest.FeatureTwo" | Get-ExperimentalFeature }
            $features | Should -Not -BeNullOrEmpty
            $features[0].Name | Should -BeExactly "ExpTest.FeatureOne"
            $features[0].Enabled | Should -BeTrue
            $features[0].Source | Should -BeExactly $testModuleManifestPath

            $features[1].Name | Should -BeExactly "ExpTest.FeatureTwo"
            $features[1].Enabled | Should -BeFalse
            $features[1].Source | Should -BeExactly $testModuleManifestPath
        }
    }

    Context "User config takes precedence over system config" {
        It "Feature is enabled in user config only" -Skip:(!(Test-CanWriteToPsHome)) {
            '{"ExperimentalFeatures":["ExpTest.FeatureOne"]}' > $userConfigPath
            '{"ExperimentalFeatures":["ExpTest.FeatureTwo"]}' > $systemConfigPath

            $feature = pwsh -noprofile -output xml -command Get-ExperimentalFeature ExpTest.FeatureOne
            $feature.Enabled | Should -BeTrue -Because "FeatureOne is enabled in user config"
            $feature = pwsh -noprofile -output xml -command Get-ExperimentalFeature ExpTest.FeatureTwo
            $feature.Enabled | Should -BeFalse -Because "System config is not read when user config exists"
        }
    }
}
