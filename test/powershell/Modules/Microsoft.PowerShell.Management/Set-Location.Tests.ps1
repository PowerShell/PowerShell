# Copyright (c) Microsoft Corporation.
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

    It "Should be able to use the Path parameter" {
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

    It "Should use actual casing of folder on case-insensitive filesystem" -Skip:($IsLinux) {
        $testPath = New-Item -ItemType Directory -Path testdrive:/teST
        Set-Location $testPath.FullName.ToUpper()
        $(Get-Location).Path | Should -BeExactly $testPath.FullName
    }

    It "Should use actual casing of folder on case-sensitive filesystem: <dir>" -Skip:(!$IsLinux) {
        $dir = "teST"
        $testPathLower = New-Item -ItemType Directory -Path (Join-Path $TestDrive $dir.ToLower())
        $testPathUpper = New-Item -ItemType Directory -Path (Join-Path $TestDrive $dir.ToUpper())
        Set-Location $testPathLower.FullName
        $(Get-Location).Path | Should -BeExactly $testPathLower.FullName
        Set-Location $testPathUpper.FullName
        $(Get-Location).Path | Should -BeExactly $testPathUpper.FullName
        { Set-Location (Join-Path $TestDrive $dir) -ErrorAction Stop } | Should -Throw -ErrorId "PathNotFound,Microsoft.PowerShell.Commands.SetLocationCommand"
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


    # Issue #25282: Set-Location fails for paths with special characters when switching drives
    Context "Set-Location with special characters in path" {

        BeforeAll {
            # Find an available drive letter for subst
            $script:availableDrive = $null
            foreach ($letter in 'T','U','V','W','X','Y','Z') {
                if (-not (Test-Path "${letter}:\")) {
                    $script:availableDrive = $letter
                    break
                }
            }

            if (-not $script:availableDrive) {
                throw "No available drive letter found for subst"
            }

            $script:oldLocation = Get-Location
            # Create a temporary directory for subst (physical drive)
            $script:tempRoot = New-Item -Path "$env:TEMP\TestDrive_$([guid]::NewGuid())" -ItemType Directory -Force
            # Create a subst drive (physical Windows drive)
            & subst "${script:availableDrive}:" $script:tempRoot.FullName
            Start-Sleep -Milliseconds 200

            # Create directories with special characters on both drives
            # Physical drive with brackets
            $script:testDirPhysical = New-Item -Path "${script:availableDrive}:\Test [Folder] physical" -ItemType Directory -Force
            # C: drive with brackets
            $script:testDirC = New-Item -Path "$env:TEMP\Test [Folder] c" -ItemType Directory -Force

            # Create a PSDrive that points to a path with special characters
            $script:psDrivePath = New-Item -Path "$env:TEMP\PSDrive [Root]" -ItemType Directory -Force
            $script:testDirPSDrive = New-Item -Path "$($script:psDrivePath.FullName)\Test [Folder] psdrive" -ItemType Directory -Force
            New-PSDrive -Name 'TestPS' -PSProvider FileSystem -Root $script:psDrivePath.FullName -Scope Global | Out-Null
        }

        AfterAll {
            Set-Location C:\
            if ($script:availableDrive) {
                & subst "${script:availableDrive}:" /D 2>$null
                Start-Sleep -Milliseconds 200
            }
            if (Get-PSDrive -Name 'TestPS' -ErrorAction SilentlyContinue) {
                Remove-PSDrive -Name 'TestPS' -Force -ErrorAction SilentlyContinue
            }
            if ($script:tempRoot -and (Test-Path $script:tempRoot)) {
                Remove-Item $script:tempRoot -Recurse -Force -ErrorAction SilentlyContinue
            }
            if ($script:testDirC -and (Test-Path $script:testDirC)) {
                Remove-Item $script:testDirC -Recurse -Force -ErrorAction SilentlyContinue
            }
            if ($script:psDrivePath -and (Test-Path $script:psDrivePath)) {
                Remove-Item $script:psDrivePath -Recurse -Force -ErrorAction SilentlyContinue
            }
            if ($script:oldLocation) {
                Set-Location $script:oldLocation
            }
        }

        It "Should navigate to directory with brackets using escaped path" {
            # Reproduce the exact issue: navigate using escaped brackets
            $escapedPath = "${script:availableDrive}:\Test ``[Folder``] physical"
            Set-Location $escapedPath
            $expectedPath = $script:testDirPhysical.FullName
            (Get-Location).Path | Should -BeExactly $expectedPath
        }

        It "Should restore path with brackets when switching from physical drive to C: and back" {
            # Step 1: Navigate to physical drive directory with brackets (using escaped path)
            $escapedPath = "${script:availableDrive}:\Test ``[Folder``] physical"
            Set-Location $escapedPath
            $expectedPath = (Get-Location).Path

            # Step 2: Switch to C: drive
            Set-Location 'C:\'

            # Step 3: Switch back using drive letter only - this is where the bug occurred
            Set-Location "${script:availableDrive}:"

            # Verify we're back in the directory with brackets
            (Get-Location).Path | Should -BeExactly $expectedPath
        }

        It "Should restore path with brackets when switching from C: to physical drive and back" {
            # Step 1: Navigate to C: directory with brackets (using escaped path)
            $escapedPath = "$env:TEMP\Test ``[Folder``] c"
            Set-Location $escapedPath
            $expectedPathC = (Get-Location).Path

            # Step 2: Switch to physical drive
            Set-Location "${script:availableDrive}:\"

            # Step 3: Switch back to C: using drive letter only
            Set-Location 'C:'

            # Verify we're back in the C: directory with brackets
            (Get-Location).Path | Should -BeExactly $expectedPathC
        }

        It "Should handle multiple round-trips between drives with special characters" {
            # Navigate to physical drive directory
            $escapedPathPhysical = "${script:availableDrive}:\Test ``[Folder``] physical"
            Set-Location $escapedPathPhysical
            $expectedPathPhysical = (Get-Location).Path

            # Navigate to C: directory
            $escapedPathC = "$env:TEMP\Test ``[Folder``] c"
            Set-Location $escapedPathC
            $expectedPathC = (Get-Location).Path

            # Round-trip 1: C: -> Physical -> C:
            Set-Location "${script:availableDrive}:"
            (Get-Location).Path | Should -BeExactly $expectedPathPhysical

            Set-Location 'C:'
            (Get-Location).Path | Should -BeExactly $expectedPathC

            # Round-trip 2: C: -> Physical -> C:
            Set-Location "${script:availableDrive}:"
            (Get-Location).Path | Should -BeExactly $expectedPathPhysical

            Set-Location 'C:'
            (Get-Location).Path | Should -BeExactly $expectedPathC
        }

        It "Should restore path with brackets when switching from PSDrive to C: and back" {
            # Step 1: Navigate to PSDrive directory with brackets
            $escapedPath = "TestPS:\Test ``[Folder``] psdrive"
            Set-Location $escapedPath
            $expectedPath = (Get-Location).Path

            # Step 2: Switch to C: drive
            Set-Location 'C:\'

            # Step 3: Switch back to PSDrive using drive letter only
            Set-Location 'TestPS:'

            # Verify we're back in the PSDrive directory with brackets
            (Get-Location).Path | Should -BeExactly $expectedPath
        }

        It "Should restore path with brackets when switching from C: to PSDrive and back" {
            # Step 1: Navigate to C: directory with brackets
            $escapedPathC = "$env:TEMP\Test ``[Folder``] c"
            Set-Location $escapedPathC
            $expectedPathC = (Get-Location).Path

            # Step 2: Switch to PSDrive
            Set-Location 'TestPS:\'

            # Step 3: Switch back to C: using drive letter only
            Set-Location 'C:'

            # Verify we're back in the C: directory with brackets
            (Get-Location).Path | Should -BeExactly $expectedPathC
        }

        It "Should handle multiple round-trips between PSDrive and C: with special characters" {
            # Navigate to PSDrive directory
            $escapedPathPS = "TestPS:\Test ``[Folder``] psdrive"
            Set-Location $escapedPathPS
            $expectedPathPS = (Get-Location).Path

            # Navigate to C: directory
            $escapedPathC = "$env:TEMP\Test ``[Folder``] c"
            Set-Location $escapedPathC
            $expectedPathC = (Get-Location).Path

            # Round-trip 1: C: -> PSDrive -> C:
            Set-Location 'TestPS:'
            (Get-Location).Path | Should -BeExactly $expectedPathPS

            Set-Location 'C:'
            (Get-Location).Path | Should -BeExactly $expectedPathC

            # Round-trip 2: C: -> PSDrive -> C:
            Set-Location 'TestPS:'
            (Get-Location).Path | Should -BeExactly $expectedPathPS

            Set-Location 'C:'
            (Get-Location).Path | Should -BeExactly $expectedPathC
        }

        It "Should not change location when using same-drive syntax on current drive" {
            # Navigate to a path with brackets
            $escapedPath = "${script:availableDrive}:\Test ``[Folder``] physical"
            Set-Location $escapedPath
            $currentPath = (Get-Location).Path

            # Use same-drive syntax - should stay in same location
            Set-Location "${script:availableDrive}:"
            (Get-Location).Path | Should -BeExactly $currentPath
        }

        It "Should not change location when using same-drive syntax on C: drive" {
            # Navigate to C: directory with brackets
            $escapedPath = "$env:TEMP\Test ``[Folder``] c"
            Set-Location $escapedPath
            $currentPath = (Get-Location).Path

            # Use same-drive syntax - should stay in same location
            Set-Location 'C:'
            (Get-Location).Path | Should -BeExactly $currentPath
        }

        It "Should not change location when using same-drive syntax on PSDrive" {
            # Navigate to PSDrive directory with brackets
            $escapedPath = "TestPS:\Test ``[Folder``] psdrive"
            Set-Location $escapedPath
            $currentPath = (Get-Location).Path

            # Use same-drive syntax - should stay in same location
            Set-Location 'TestPS:'
            (Get-Location).Path | Should -BeExactly $currentPath
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
            (Get-Location).Path | Should -Be $initialLocation
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
            $initialPath = $PWD
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
            $newPath.Path | Should -Be $PWD.Path
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

    Context 'Parsing of Set-Location to cd' {
        It 'Should go to Filesystem home on cd~ run' {
            Set-Location 'TestDrive:\'
            cd~
            (Get-Location).Path | Should -BeExactly (Get-PSProvider FileSystem).Home
        }
        It 'Should go to the parent folder on cd.. run' {
            Set-Location 'TestDrive:\'
            $ParentDir = (Get-Location).path
            New-Item -Path 'TestDrive:\' -Name 'Directory1' -ItemType Directory -ErrorAction Ignore
            Set-Location 'TestDrive:\Directory1'
            cd..
            (Get-Location).Path | Should -BeExactly $ParentDir
        }
        It 'Should go to root of current drive on cd\ run (Windows)' -Skip:(!$IsWindows){
            #root is / on linux and Mac, so it's not happy with this check.
            Set-Location 'TestDrive:\'
            $DriveRoot = (Get-Location).path
            New-Item -Path 'TestDrive:\Directory1' -Name 'Directory2' -ItemType Directory
            Set-Location 'TestDrive:\Directory1\Directory2'
            cd\
            (Get-Location).Path | Should -BeExactly $DriveRoot
        }
        It 'Should go to root of current drive on cd\ run (Linux/Mac)' -Skip:($IsWindows){
            Set-Location 'TestDrive:\'
            New-Item -Path 'TestDrive:\Directory1' -Name 'Directory2' -ItemType Directory -ErrorAction Ignore
            Set-Location 'TestDrive:\Directory1\Directory2'
            cd\
            (Get-Location).Path | Should -BeExactly "/"
        }
    }
}
