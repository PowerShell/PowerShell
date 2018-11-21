# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Set-Location" -Tags "CI" {

    BeforeAll {
        $startDirectory = Get-Location

        if ($IsWindows)
        {
            $target = "C:\"
        }
        else
        {
            $target = "/"
        }
    }

    AfterAll {
        Set-Location $startDirectory
    }

    It "Should be able to be called without error" {
        { Set-Location $target }    | Should -Not -Throw
    }

    It "Should be able to be called on different providers" {
        { Set-Location alias: } | Should -Not -Throw
        { Set-Location env: }   | Should -Not -Throw
    }

    It "Should have the correct current location when using the set-location cmdlet" {
        Set-Location $startDirectory

        $(Get-Location).Path | Should -BeExactly $startDirectory.Path
    }

    It "Should be able to use the Path switch" {
        { Set-Location -Path $target } | Should -Not -Throw
    }

    It "Should generate a pathinfo object when using the Passthru switch" {
        $result = Set-Location $target -PassThru
        $result | Should -BeOfType System.Management.Automation.PathInfo
    }

    # https://github.com/PowerShell/PowerShell/issues/5752
    It "Should accept path containing wildcard characters" -Pending {
        $null = New-Item -ItemType Directory -Path "$TestDrive\aa"
        $null = New-Item -ItemType Directory -Path "$TestDrive\ba"
        $testPath = New-Item -ItemType Directory -Path "$TestDrive\[ab]a"

        Set-Location $TestDrive
        Set-Location -Path "[ab]a"
        $(Get-Location).Path | Should -BeExactly $testPath.FullName
    }

    It "Should not use filesystem root folder if not in filesystem provider" -Skip:(!$IsWindows) {
        # find filesystem root folder that doesn't exist in HKCU:
        $foundFolder = $false
        foreach ($folder in Get-ChildItem "${env:SystemDrive}\" -Directory) {
            if (-Not (Test-Path "HKCU:\$($folder.Name)")) {
                $testFolder = $folder.Name
                $foundFolder = $true
                break
            }
        }
        $foundFolder | Should -BeTrue
        Set-Location HKCU:\
        { Set-Location ([System.IO.Path]::DirectorySeparatorChar + $testFolder) -ErrorAction Stop } |
            Should -Throw -ErrorId "PathNotFound,Microsoft.PowerShell.Commands.SetLocationCommand"
    }

    Context 'Set-Location with no arguments' {

        It 'Should go to $env:HOME when Set-Location run with no arguments from FileSystem provider' {
            Set-Location 'TestDrive:\'
            Set-Location
            (Get-Location).Path | Should -BeExactly (Get-PSProvider FileSystem).Home
        }

        It 'Should go to $env:HOME when Set-Location run with no arguments from Env: provider' {
            Set-Location 'Env:'
            Set-Location
            (Get-Location).Path | Should -BeExactly (Get-PSProvider FileSystem).Home
        }
    }

    It "Should set location to new drive's current working directory when path is the colon-terminated name of a different drive" {
        try
        {
            $oldLocation = Get-Location
            Set-Location 'TestDrive:\'
            New-Item -Path 'TestDrive:\' -Name 'Directory1' -ItemType Directory
            New-PSDrive -Name 'Z' -PSProvider FileSystem -Root 'TestDrive:\Directory1'
            New-Item -Path 'Z:\' -Name 'Directory2' -ItemType Directory

            Set-Location 'TestDrive:\Directory1'
            $pathToTest1 = (Get-Location).Path
            Set-Location 'Z:\Directory2'
            $pathToTest2 = (Get-Location).Path

            Set-Location 'TestDrive:'
            (Get-Location).Path | Should -BeExactly $pathToTest1
            Set-Location 'Z:'
            (Get-Location).Path | Should -BeExactly $pathToTest2
        }
        finally
        {
            Set-Location $oldLocation
            Remove-PSDrive -Name 'Z'
        }
    }

    Context 'Set-Location with last location history' {

        It 'Should go to last location when specifying minus as a path' {
            $initialLocation = Get-Location
            Set-Location ([System.IO.Path]::GetTempPath())
            Set-Location -
            (Get-Location).Path | Should -Be ($initialLocation).Path
        }

        It 'Should go to last location back, forth and back again when specifying minus, plus and minus as a path' {
            $initialLocation = (Get-Location).Path
            Set-Location ([System.IO.Path]::GetTempPath())
            $tempPath = (Get-Location).Path
            Set-Location -
            (Get-Location).Path | Should -Be $initialLocation
            Set-Location +
            (Get-Location).Path | Should -Be $tempPath
            Set-Location -
            (Get-Location).Path | Should -Be $initialLocation
        }

        It 'Should go back to previous locations when specifying minus twice' {
            $initialLocation = (Get-Location).Path
            Set-Location ([System.IO.Path]::GetTempPath())
            $firstLocationChange = (Get-Location).Path
            Set-Location ([System.Environment]::GetFolderPath("user"))
            Set-Location -
            (Get-Location).Path | Should -Be $firstLocationChange
            Set-Location -
            (Get-Location).Path | Should -Be $initialLocation
        }

        It 'Location History is limited' {
            $initialLocation = (Get-Location).Path
            $maximumLocationHistory = 20
            foreach ($i in 1..$maximumLocationHistory) {
                Set-Location ([System.IO.Path]::GetTempPath())
            }
            $tempPath = (Get-Location).Path
            # Go back up to the maximum
            foreach ($i in 1..$maximumLocationHistory) {
                Set-Location -
            }
            (Get-Location).Path | Should Be $initialLocation
            { Set-Location - } | Should -Throw -ErrorId 'System.InvalidOperationException,Microsoft.PowerShell.Commands.SetLocationCommand'
            # Go forwards up to the maximum
            foreach ($i in 1..($maximumLocationHistory)) {
                Set-Location +
            }
            (Get-Location).Path | Should -Be $tempPath
            { Set-Location + } | Should -Throw -ErrorId 'System.InvalidOperationException,Microsoft.PowerShell.Commands.SetLocationCommand'
        }
    }

    It 'Should nativate to literal path "<path>"' -TestCases @(
        @{ path = "-" },
        @{ path = "+" }
    ) {
        param($path)

        Set-Location $TestDrive
        $literalPath = Join-Path $TestDrive $path
        New-Item -ItemType Directory -Path $literalPath
        Set-Location -LiteralPath $path
        (Get-Location).Path | Should -BeExactly $literalPath
    }

    Context 'Test the LocationChangedAction event handler' {

        AfterEach {
            $ExecutionContext.InvokeCommand.LocationChangedAction = $null
        }

        It 'The LocationChangedAction should fire when changing location' {
            $initialPath = $pwd
            $oldPath = $null
            $newPath = $null
            $eventSessionState = $null
            $eventRunspace = $null
            $ExecutionContext.InvokeCommand.LocationChangedAction = {
                (Get-Variable eventRunspace).Value = $this
                (Get-Variable eventSessionState).Value = $_.SessionState
                (Get-Variable oldPath).Value = $_.oldPath
                (Get-Variable newPath).Value = $_.newPath
            }
            Set-Location ..
            $newPath.Path | Should -Be $pwd.Path
            $oldPath.Path | Should -Be $initialPath.Path
            $eventSessionState | Should -Be $ExecutionContext.SessionState
            $eventRunspace | Should -Be ([runspace]::DefaultRunspace)
        }

        It 'Errors in the LocationChangedAction should be catchable but not fail the cd' {
            $location = $PWD
            Set-Location ..
            $ExecutionContext.InvokeCommand.LocationChangedAction = { throw "Boom" }
            # Verify that the exception occurred
            { Set-Location $location } | Should -Throw "Boom"
            # But the location should still have changed
            $PWD.Path | Should -Be $location.Path
        }
    }
}
