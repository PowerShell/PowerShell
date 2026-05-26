# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Get-PowerShellConfiguration Tests" -Tags "CI" {

    It "Returns both scopes when no -Scope is specified" {
        $configs = Get-PowerShellConfiguration
        $configs | Should -HaveCount 2
        $configs[0].Scope | Should -Be "AllUsers"
        $configs[1].Scope | Should -Be "CurrentUser"
    }

    It "Returns AllUsers scope when -Scope AllUsers is specified" {
        $config = Get-PowerShellConfiguration -Scope AllUsers
        $config | Should -Not -BeNullOrEmpty
        $config.Scope | Should -Be "AllUsers"
    }

    It "Returns CurrentUser scope when -Scope CurrentUser is specified" {
        $config = Get-PowerShellConfiguration -Scope CurrentUser
        $config | Should -Not -BeNullOrEmpty
        $config.Scope | Should -Be "CurrentUser"
    }

    It "AllUsers path points to platform-specific system config directory" {
        $config = Get-PowerShellConfiguration -Scope AllUsers
        if ($IsWindows) {
            $config.Path | Should -BeLike "*ProgramData*Microsoft*PowerShell*powershell.config.json"
        }
        else {
            $config.Path | Should -Be "/etc/powershell/powershell.config.json"
        }
    }

    It "CurrentUser path points to user config directory" {
        $config = Get-PowerShellConfiguration -Scope CurrentUser
        $config.Path | Should -BeLike "*powershell.config.json"
        $config.Path | Should -Not -BeLike "*ProgramData*"
        if (-not $IsWindows) {
            $config.Path | Should -BeLike "*/.config/powershell/*"
        }
    }

    It "AllUsers path does not point to PSHOME" {
        $config = Get-PowerShellConfiguration -Scope AllUsers
        $config.Path | Should -Not -BeLike "$PSHOME*"
    }

    It "Output type is PowerShellConfigurationInfo" {
        $config = Get-PowerShellConfiguration -Scope AllUsers
        $config.GetType().Name | Should -Be "PowerShellConfigurationInfo"
    }
}
