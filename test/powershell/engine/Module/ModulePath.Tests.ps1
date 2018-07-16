# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "SxS Module Path Basic Tests" -tags "CI" {

    BeforeAll {

        if ($IsWindows)
        {
            $powershell = "$PSHOME\pwsh.exe"
            $ProductName = "WindowsPowerShell"
            if ($IsCoreCLR -and ($PSHOME -notlike "*Windows\System32\WindowsPowerShell\v1.0"))
            {
                $ProductName =  "PowerShell"
            }
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

        if ($IsWindows)
        {
            $expectedWindowsPowerShellPSHomePath = Join-Path $env:windir "System32" "WindowsPowerShell" "v1.0" "Modules"
        }

        ## Setup a fake PSHome
        $fakePSHome = Join-Path -Path $TestDrive -ChildPath 'FakePSHome'
        $fakePSHomeModuleDir = Join-Path -Path $fakePSHome -ChildPath 'Modules'
        $fakePowerShell = Join-Path -Path $fakePSHome -ChildPath (Split-Path -Path $powershell -Leaf)
        $fakePSDepsFile = Join-Path -Path $fakePSHome -ChildPath "pwsh.deps.json"

        New-Item -Path $fakePSHome -ItemType Directory > $null
        New-Item -Path $fakePSHomeModuleDir -ItemType Directory > $null
    }

    BeforeEach {
        $originalModulePath = $env:PSModulePath
    }

    AfterEach {
        $env:PSModulePath = $originalModulePath
    }

    It "validate sxs module path" {

        $env:PSModulePath = ""
        $defaultModulePath = & $powershell -nopro -c '$env:PSModulePath'

        $paths = $defaultModulePath -split [System.IO.Path]::PathSeparator

        if ($IsWindows)
        {
            $paths.Count | Should -Be 4
        }
        else
        {
            $paths.Count | Should -Be 3
        }

        $paths[0].TrimEnd([System.IO.Path]::DirectorySeparatorChar) | Should -Be $expectedUserPath
        $paths[1].TrimEnd([System.IO.Path]::DirectorySeparatorChar) | Should -Be $expectedSharedPath
        $paths[2].TrimEnd([System.IO.Path]::DirectorySeparatorChar) | Should -Be $expectedSystemPath
        if ($IsWindows)
        {
            $paths[3].TrimEnd([System.IO.Path]::DirectorySeparatorChar) | Should -Be $expectedWindowsPowerShellPSHomePath
        }
    }

    It "ignore pshome module path derived from a different powershell core instance" -Skip:(!$IsCoreCLR) {

        ## Create 'powershell' and 'pwsh.deps.json' in the fake PSHome folder,
        ## so that the module path calculation logic would believe it's real.
        New-Item -Path $fakePowerShell -ItemType File -Force > $null
        New-Item -Path $fakePSDepsFile -ItemType File -Force > $null

        try {

            ## PSHome module path derived from another powershell core instance should be ignored
            $env:PSModulePath = $fakePSHomeModuleDir
            $newModulePath = & $powershell -nopro -c '$env:PSModulePath'
            $paths = $newModulePath -split [System.IO.Path]::PathSeparator

            if ($IsWindows)
            {
                $paths.Count | Should -Be 4
            }
            else
            {
                $paths.Count | Should -Be 3
            }

            $paths[0] | Should -Be $expectedUserPath
            $paths[1] | Should -Be $expectedSharedPath
            $paths[2] | Should -Be $expectedSystemPath
            if ($IsWindows)
            {
                $paths[3].TrimEnd([System.IO.Path]::DirectorySeparatorChar) | Should -Be $expectedWindowsPowerShellPSHomePath
            }

        } finally {

            ## Remove 'powershell' and 'pwsh.deps.json' from the fake PSHome folder
            Remove-Item -Path $fakePowerShell -Force -ErrorAction SilentlyContinue
            Remove-Item -Path $fakePSDepsFile -Force -ErrorAction SilentlyContinue
        }
    }

    It "keep non-pshome module path derived from powershell core instance parent" -Skip:(!$IsCoreCLR) {

        ## non-pshome module path derived from another powershell core instance should be preserved
        $customeModules = Join-Path -Path $TestDrive -ChildPath 'CustomModules'
        $env:PSModulePath = $fakePSHomeModuleDir, $customeModules -join ([System.IO.Path]::PathSeparator)
        $newModulePath = & $powershell -nopro -c '$env:PSModulePath'
        $paths = $newModulePath -split [System.IO.Path]::PathSeparator

        if ($IsWindows)
        {
            $paths.Count | Should -Be 6
        }
        else
        {
            $paths.Count | Should -Be 5
        }
        $paths -contains $fakePSHomeModuleDir | Should -BeTrue
        $paths -contains $customeModules | Should -BeTrue
    }

}
