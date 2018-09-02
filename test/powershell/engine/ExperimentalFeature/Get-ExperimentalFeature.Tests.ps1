# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe "Get-ExperimentalFeature basic tests - Feature-Disabled" -tags "CI" {

    BeforeAll {
        $skipTest = $EnabledExperimentalFeatures.Contains('ExpTest.FeatureOne')

        if ($skipTest) {
            Write-Verbose "Test Suite Skipped. The test suite requires the experimental feature 'ExpTest.FeatureOne' to be disabled." -Verbose
            $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
            $PSDefaultParameterValues["it:skip"] = $true
        } else {
            Remove-Module -Name ExpTest -Force -ErrorAction SilentlyContinue
            $testModulePath = Join-Path -Path $PSScriptRoot -ChildPath "assets"
            $testModuleManifestPath = Join-Path -Path $testModulePath "ExpTest" "ExpTest.psd1"
            $originalModulePath = $env:PSModulePath
            $env:PSModulePath = $testModulePath
        }
    }

    AfterAll {
        if ($skipTest) {
            $global:PSDefaultParameterValues = $originalDefaultParameterValues
        } else {
            $env:PSModulePath = $originalModulePath
        }
    }

    It "'Get-ExperimentalFeature' should only return enabled features" {
        $EnabledExperimentalFeatures.Count | Should -Be 0
        Get-ExperimentalFeature | Should -BeNullOrEmpty
    }

    It "'Get-ExperimentalFeature -ListAvailable' should return all available features from module path" {
        $features = Get-ExperimentalFeature "ExpTest*" -ListAvailable
        $features | Should -Not -BeNullOrEmpty
        $features[0].Name | Should -BeExactly "ExpTest.FeatureOne"
        $features[0].Enabled | Should -Be $false
        $features[0].Source | Should -BeExactly $testModuleManifestPath

        $features[1].Name | Should -BeExactly "ExpTest.FeatureTwo"
        $features[1].Enabled | Should -Be $false
        $features[1].Source | Should -BeExactly $testModuleManifestPath
    }

    It "'Get-ExperimentalFeature -ListAvailable' pipeline input" {
        $features = "ExpTest.FeatureOne", "ExpTest.FeatureTwo" | Get-ExperimentalFeature -ListAvailable
        $features | Should -Not -BeNullOrEmpty
        $features[0].Name | Should -BeExactly "ExpTest.FeatureOne"
        $features[0].Enabled | Should -Be $false
        $features[0].Source | Should -BeExactly $testModuleManifestPath

        $features[1].Name | Should -BeExactly "ExpTest.FeatureTwo"
        $features[1].Enabled | Should -Be $false
        $features[1].Source | Should -BeExactly $testModuleManifestPath
    }
}

Describe "Get-ExperimentalFeature basic tests - Feature-Enabled" -tags "CI" {

    BeforeAll {
        $skipTest = -not $EnabledExperimentalFeatures.Contains('ExpTest.FeatureOne')

        if ($skipTest) {
            Write-Verbose "Test Suite Skipped. The test suite requires the experimental feature 'ExpTest.FeatureOne' to be enabled." -Verbose
            $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
            $PSDefaultParameterValues["it:skip"] = $true
        } else {
            Remove-Module -Name ExpTest -Force -ErrorAction SilentlyContinue
            $testModulePath = Join-Path -Path $PSScriptRoot -ChildPath "assets"
            $testModuleManifestPath = Join-Path -Path $testModulePath "ExpTest" "ExpTest.psd1"
            $originalModulePath = $env:PSModulePath
            $env:PSModulePath = $testModulePath
        }
    }

    AfterAll {
        if ($skipTest) {
            $global:PSDefaultParameterValues = $originalDefaultParameterValues
        } else {
            $env:PSModulePath = $originalModulePath
        }
    }

    It "'Get-ExperimentalFeature' should return enabled features 'ExpTest.FeatureOne'" {
        $EnabledExperimentalFeatures.Count | Should -Be 1
        $feature = Get-ExperimentalFeature "ExpTest.FeatureOne"
        $feature | Should -Not -BeNullOrEmpty
        $feature.Enabled | Should -Be $true
        $feature.Source | Should -BeExactly $testModuleManifestPath
    }

    It "'Get-ExperimentalFeature -ListAvailable' should return all available features from module path" {
        $features = Get-ExperimentalFeature "ExpTest*" -ListAvailable
        $features | Should -Not -BeNullOrEmpty
        $features[0].Name | Should -BeExactly "ExpTest.FeatureOne"
        $features[0].Enabled | Should -Be $true
        $features[0].Source | Should -BeExactly $testModuleManifestPath

        $features[1].Name | Should -BeExactly "ExpTest.FeatureTwo"
        $features[1].Enabled | Should -Be $false
        $features[1].Source | Should -BeExactly $testModuleManifestPath
    }

    It "'Get-ExperimentalFeature -ListAvailable' pipeline input" {
        $features = "ExpTest.FeatureOne", "ExpTest.FeatureTwo" | Get-ExperimentalFeature -ListAvailable
        $features | Should -Not -BeNullOrEmpty
        $features[0].Name | Should -BeExactly "ExpTest.FeatureOne"
        $features[0].Enabled | Should -Be $true
        $features[0].Source | Should -BeExactly $testModuleManifestPath

        $features[1].Name | Should -BeExactly "ExpTest.FeatureTwo"
        $features[1].Enabled | Should -Be $false
        $features[1].Source | Should -BeExactly $testModuleManifestPath
    }
}
