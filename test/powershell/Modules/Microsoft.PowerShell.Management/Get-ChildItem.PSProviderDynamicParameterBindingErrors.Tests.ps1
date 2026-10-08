# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'Get-ChildItem with PSProviderDynamicParameterBindingErrors' -Tags 'CI' {
    BeforeAll {
        $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
        $PSDefaultParameterValues['It:Skip'] = -not [ExperimentalFeature]::IsEnabled('PSProviderDynamicParameterBindingErrors')
    }

    AfterAll {
        $global:PSDefaultParameterValues = $originalDefaultParameterValues
    }

    It 'reports a missing drive before binding file system dynamic parameters' {
        {
            Get-ChildItem -LiteralPath 'PSDynamicParameterBindingMissingDrive:\path' -File -ErrorAction Stop
        } | Should -Throw -ErrorId 'DriveNotFound'
    }

    It 'reports a missing drive before binding directory dynamic parameters' {
        {
            Get-ChildItem -LiteralPath 'PSDynamicParameterBindingMissingDrive:\path' -Directory -ErrorAction Stop
        } | Should -Throw -ErrorId 'DriveNotFound'
    }

    It 'reports a missing provider before binding file system dynamic parameters' {
        {
            Get-ChildItem -LiteralPath 'PSDynamicParameterBindingMissingProvider::path' -File -ErrorAction Stop
        } | Should -Throw -ErrorId 'ProviderNotFound'
    }

    It 'continues to report file system dynamic parameters as unavailable for registry paths' {
        {
            Get-ChildItem -LiteralPath 'HKCU:\Software' -File -ErrorAction Stop
        } | Should -Throw -ErrorId 'NamedParameterNotFound,Microsoft.PowerShell.Commands.GetChildItemCommand'
    }
}