# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'Get-ChildItem with PSProviderDynamicParameterBindingErrors' -Tags 'CI' {
    BeforeAll {
        $isFeatureEnabled = [ExperimentalFeature]::IsEnabled('PSProviderDynamicParameterBindingErrors')
    }

    It 'reports a missing drive before binding file system dynamic parameters' -Skip:$(-not $isFeatureEnabled) {
        {
            Get-ChildItem -LiteralPath 'PSDynamicParameterBindingMissingDrive:\path' -File -ErrorAction Stop
        } | Should -Throw -ErrorId 'DriveNotFound'
    }

    It 'reports a missing drive before binding directory dynamic parameters' -Skip:$(-not $isFeatureEnabled) {
        {
            Get-ChildItem -LiteralPath 'PSDynamicParameterBindingMissingDrive:\path' -Directory -ErrorAction Stop
        } | Should -Throw -ErrorId 'DriveNotFound'
    }

    It 'reports a missing provider before binding file system dynamic parameters' -Skip:$(-not $isFeatureEnabled) {
        {
            Get-ChildItem -LiteralPath 'PSDynamicParameterBindingMissingProvider::path' -File -ErrorAction Stop
        } | Should -Throw -ErrorId 'ProviderNotFound'
    }

    It 'continues to report file system dynamic parameters as unavailable for registry paths' -Skip:$(-not ($IsWindows -and $isFeatureEnabled)) {
        {
            Get-ChildItem -LiteralPath 'HKCU:\Software' -File -ErrorAction Stop
        } | Should -Throw -ErrorId 'NamedParameterNotFound,Microsoft.PowerShell.Commands.GetChildItemCommand'
    }
}