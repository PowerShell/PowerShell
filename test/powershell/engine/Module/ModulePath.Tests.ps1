# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe "SxS Module Path Basic Tests" -tags "CI" {

    BeforeAll {

        if ($IsWindows)
        {
            $powershell = "$PSHOME\pwsh.exe"
            $ProductName =  "PowerShell"
            $expectedUserPath = Join-Path -Path $HOME -ChildPath "Documents\$ProductName\Modules"
            $expectedSharedPath = Join-Path -Path $env:ProgramFiles -ChildPath "$ProductName\Modules"
        }
        else
        {
            $powershell = "$PSHOME/pwsh"
            $expectedUserPath = [System.Management.Automation.Platform]::SelectProductNameForDirectory("USER_MODULES")
            $expectedSharedPath = [System.Management.Automation.Platform]::SelectProductNameForDirectory("SHARED_MODULES")
        }

        $expectedSystemPath = Join-Path -Path $PSHOME -ChildPath 'Modules'

        # Skip these tests in cases when there is no 'pwsh' executable (e.g. when framework dependent PS package is used)
        $skipNoPwsh = -not (Test-Path $powershell)

        if ($IsWindows)
        {
            $expectedWindowsPowerShellPSHomePath = Join-Path $env:windir "System32" "WindowsPowerShell" "v1.0" "Modules"
        }

        ## Setup a fake PSHome
        $fakePSHome = Join-Path -Path $TestDrive -ChildPath 'FakePSHome'
        $fakePSHomeModuleDir = Join-Path -Path $fakePSHome -ChildPath 'Modules'
    }

    BeforeEach {
        $originalModulePath = $env:PSModulePath
    }

    AfterEach {
        $env:PSModulePath = $originalModulePath
    }

    It "All standard module paths present" -Skip:$skipNoPwsh {

        $env:PSModulePath = ""

        $expectedPaths = @($expectedUserPath, $expectedSharedPath, $expectedSystemPath)
        if ($IsWindows)
        {
            $expectedPaths += $expectedWindowsPowerShellPSHomePath
        }

        [System.Management.Automation.Internal.InternalTestHooks]::SetTestHook("TestPowerShellPSModulePaths", $expectedPaths)
        try {
            { Get-Module -Listavailable -All }  | Should -Not -Throw
        } finally {
            [System.Management.Automation.Internal.InternalTestHooks]::SetTestHook("TestPowerShellPSModulePaths", $null)
        }

    }

    It "keep non-pshome module path derived from PowerShell instance parent" -Skip:$skipNoPwsh {

        ## non-pshome module path derived from another PowerShell instance should be preserved
        $customeModules = Join-Path -Path $TestDrive -ChildPath 'CustomModules'
        $env:PSModulePath = $fakePSHomeModuleDir, $customeModules -join ([System.IO.Path]::PathSeparator)
        $newModulePath = & $powershell -nopro -c '$env:PSModulePath'
        $paths = $newModulePath -split [System.IO.Path]::PathSeparator

        $paths -contains $fakePSHomeModuleDir | Should -BeTrue
        $paths -contains $customeModules | Should -BeTrue
    }
}
