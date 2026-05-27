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

    It "AllUsers path points to system config directory or legacy PSHOME location" {
        $config = Get-PowerShellConfiguration -Scope AllUsers
        $config.Path | Should -BeLike "*powershell.config.json"
        if ($IsWindows) {
            $isNewPath = $config.Path -like "*ProgramData*Microsoft*PowerShell*"
            $isLegacyPath = $config.Path -like "$PSHOME*"
            ($isNewPath -or $isLegacyPath) | Should -BeTrue
        }
        else {
            $isNewPath = $config.Path -eq "/etc/powershell/powershell.config.json"
            $isLegacyPath = $config.Path -like "$PSHOME*"
            ($isNewPath -or $isLegacyPath) | Should -BeTrue
        }
    }

    It "AllUsers path prefers new location over PSHOME when config exists there" {
        $config = Get-PowerShellConfiguration -Scope AllUsers
        if ($IsWindows) {
            $newPath = Join-Path $env:ProgramData "Microsoft\PowerShell\powershell.config.json"
        }
        else {
            $newPath = "/etc/powershell/powershell.config.json"
        }
        if (Test-Path $newPath) {
            $config.Path | Should -Be $newPath
        }
    }

    It "CurrentUser path points to user config directory" {
        $config = Get-PowerShellConfiguration -Scope CurrentUser
        $config.Path | Should -BeLike "*powershell.config.json"
        $config.Path | Should -Not -BeLike "*ProgramData*"
        if (-not $IsWindows) {
            $expectedBase = $env:XDG_CONFIG_HOME
            if (-not $expectedBase) {
                $expectedBase = Join-Path $HOME ".config"
            }
            $config.Path | Should -BeLike "$expectedBase/powershell/*"
        }
    }

    It "Output type is PowerShellConfigurationInfo" {
        $config = Get-PowerShellConfiguration -Scope AllUsers
        $config.GetType().Name | Should -Be "PowerShellConfigurationInfo"
    }
}
