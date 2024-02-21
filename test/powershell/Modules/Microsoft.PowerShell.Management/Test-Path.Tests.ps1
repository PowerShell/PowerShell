# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Test-Path" -Tags "CI" {
    BeforeAll {
        $testdirectory = $TestDrive
        $testfilename = New-Item -Path $testdirectory -Name testfile.txt -ItemType file -Value 1 -Force

        # populate with additional files
        New-Item -Path $testdirectory -Name datestfile -Value 1 -ItemType file | Out-Null
        New-Item -Path $testdirectory -Name gatestfile -Value 1 -ItemType file | Out-Null
        New-Item -Path $testdirectory -Name usr -Value 1 -ItemType directory | Out-Null

        $nonExistentDir = Join-Path -Path (Join-Path -Path $testdirectory -ChildPath usr) -ChildPath bin
        $nonExistentPath = Join-Path -Path (Join-Path -Path (Join-Path -Path $testdirectory -ChildPath usr) -ChildPath bin) -ChildPath error

        $today = Get-Date
        $oneDayOld = (Get-Date).AddDays(-1)
        $twoDaysOld = (Get-Date).AddDays(-2)

        $oldFilePath = Join-Path -Path $testdirectory -ChildPath oldfile
        $oldFile = New-Item -Path $oldFilePath -ItemType File
        $oldFile.LastWriteTime = $oneDayOld

        $oldDirPath = Join-Path -Path $testdirectory -ChildPath olddir
        $oldDir = New-Item -Path $oldDirPath -ItemType Directory
        $oldDir.LastWriteTime = $oneDayOld

        $newFilePath = Join-Path -Path $testdirectory -ChildPath newfile
        New-Item -Path $newFilePath -ItemType File | Out-Null

        $newDirPath = Join-Path -Path $testdirectory -ChildPath newdir
        New-Item -Path $newDirPath -ItemType Directory | Out-Null
    }

    It "Should be called on an existing path without error" {
        { Test-Path $testdirectory }              | Should -Not -Throw
        { Test-Path -Path $testdirectory }        | Should -Not -Throw
        { Test-Path -LiteralPath $testdirectory } | Should -Not -Throw
    }

    It "Should allow piping objects to it" {
        { $testdirectory | Test-Path  } | Should -Not -Throw

        $testdirectory                  | Test-Path | Should -BeTrue
        $nonExistentDir                 | Test-Path | Should -BeFalse
    }

    It "Should be called on a nonexistent path without error" {
        { Test-Path -Path $nonExistentPath } | Should -Not -Throw
    }

    It "Should return false for a nonexistent path" {
        Test-Path -Path $nonExistentPath | Should -BeFalse
    }

    It "Should return true for an existing path" {
        { Test-Path -Path $testdirectory } | Should -BeTrue
    }

    It 'Should return false for an empty string' {
        Test-Path -Path '' | Should -BeFalse
    }

    It 'Should return false for a whitespace string' {
        Test-Path -Path '  ' | Should -BeFalse
    }

    It 'Should write a non-terminating error when given a null path' {
        # This ensures the error is non-terminating; a terminating error would fail the first test
        { Test-Path -Path $null -ErrorAction SilentlyContinue } | Should -Not -Throw
        { Test-Path -Path $null -ErrorAction Stop }             | Should -Throw -ErrorId 'NullPathNotPermitted,Microsoft.PowerShell.Commands.TestPathCommand'
    }

    It 'Should write a non-terminating error when given an array of null paths' {
        # This ensures the error is non-terminating; a terminating error would fail the first test
        { Test-Path -Path $null, $null -ErrorAction SilentlyContinue } | Should -Not -Throw
        { Test-Path -Path $null, $null -ErrorAction Stop }             | Should -Throw -ErrorId 'NullPathNotPermitted,Microsoft.PowerShell.Commands.TestPathCommand'
    }

    It "Should be able to accept a regular expression" {
        { Test-Path -Path (Join-Path -Path $testdirectory -ChildPath "u*") }           | Should -Not -Throw
        { Test-Path -Path (Join-Path -Path $testdirectory -ChildPath "u[a-z]r") }      | Should -Not -Throw
    }

    It "Should be able to return the correct result when a regular expression is used" {
        Test-Path -Path (Join-Path -Path $testdirectory -ChildPath "u*")               | Should -BeTrue
        Test-Path -Path (Join-Path -Path $testdirectory -ChildPath "u[a-z]*")          | Should -BeTrue

        Test-Path -Path (Join-Path -Path $testdirectory -ChildPath "aoeu*")            | Should -BeFalse
        Test-Path -Path (Join-Path -Path $testdirectory -ChildPath "u[A-Z]")           | Should -BeFalse
    }

    It "Should return false when the Leaf pathtype is used on a directory" {
        Test-Path -Path $testdirectory -PathType Leaf | Should -BeFalse
    }

    It "Should return true when the Leaf pathtype is used on an existing endpoint" {
        Test-Path -Path $testfilename -PathType Leaf | Should -BeTrue
    }

    It "Should return false when the Leaf pathtype is used on a nonexistent file" {
        Test-Path -Path "aoeu" -PathType Leaf | Should -BeFalse
    }

    It "Should return true when the Leaf pathtype is used on a file using the Type alias instead of PathType" {
        Test-Path -Path $testfilename -Type Leaf | Should -BeTrue
    }

    It "Should be able to search multiple regular expressions using the include switch" {
        { Test-Path -Path (Join-Path -Path $testdirectory -ChildPath "*") -Include t* } | Should -BeTrue
    }

    It "Should be able to exclude a regular expression using the exclude switch" {
        { Test-Path -Path (Join-Path -Path $testdirectory -ChildPath "*") -Exclude v* } | Should -BeTrue
    }

    It "Should be able to exclude multiple regular expressions using the exclude switch" {
        # tests whether there's any files in the usr directory that don't start with 'd' or 'g'
        { Test-Path -Path $testfilename -Exclude d*, g* } | Should -BeTrue
    }

    It "Should return true if the syntax of the path is correct when using the IsValid switch" {
        { Test-Path -Path $nonExistentPath -IsValid } | Should -BeTrue
    }

    It "Should return false if the syntax of the path is incorrect when using the IsValid switch" {
        $badPath = " :;!@#$%^&*(){}?+|_-"
        Test-Path -Path $badPath -IsValid | Should -BeFalse
    }

    It "Should return true on paths containing spaces when the path is surrounded in quotes" {
        Test-Path -Path "/totally a valid/path" -IsValid | Should -BeTrue
    }

    It "Should throw on paths containing spaces when the path is not surrounded in quotes" {
        { Test-Path -Path /a path/without quotes/around/it -IsValid } | Should -Throw -ErrorId "PositionalParameterNotFound,Microsoft.PowerShell.Commands.TestPathCommand"
    }

    It "Should return true if a directory leads or trails with a space when surrounded by quotes" {
        Test-Path -Path "/a path / with/funkyspaces" -IsValid | Should -BeTrue
    }

    It "Should return true on a valid path when the LiteralPath switch is used" {
        Test-Path -LiteralPath $testfilename | Should -BeTrue
    }

    It "Should return false if regular expressions are used with the LiteralPath switch" {
        Test-Path -LiteralPath (Join-Path -Path $testdirectory -ChildPath "u*")            | Should -BeFalse
        Test-Path -LiteralPath (Join-Path -Path $testdirectory -ChildPath "u[a-z]r")       | Should -BeFalse
    }

    It "Should return false if regular expressions are used with the LiteralPath alias PSPath switch" {
        Test-Path -PSPath (Join-Path -Path $testdirectory -ChildPath "u*")            | Should -BeFalse
        Test-Path -PSPath (Join-Path -Path $testdirectory -ChildPath "u[a-z]r")       | Should -BeFalse
    }

    It "Should return true if used on components other than filesystem objects" {
        Test-Path Alias:\gci | Should -BeTrue
        Test-Path Env:\PATH  | Should -BeTrue
    }

    It "Should return true if NewerThan is used and path is newer than one day" {
        Test-Path -Path $newFilePath -PathType Leaf -NewerThan $oneDayOld | Should -BeTrue
        Test-Path -Path $newDirPath -PathType Container -NewerThan $oneDayOld | Should -BeTrue
        Test-Path -Path $newFilePath -PathType Any -NewerThan $oneDayOld | Should -BeTrue
        Test-Path -Path $newDirPath -PathType Any -NewerThan $oneDayOld | Should -BeTrue
        Test-Path -Path $newFilePath -NewerThan $oneDayOld | Should -BeTrue
        Test-Path -Path $newDirPath -NewerThan $oneDayOld | Should -BeTrue
    }

    It "Should return false if NewerThan is used and path is not newer than today" {
        Test-Path -Path $oldFilePath -PathType Leaf -NewerThan $today | Should -BeFalse
        Test-Path -Path $oldDirPath -PathType Container -NewerThan $today | Should -BeFalse
        Test-Path -Path $oldFilePath -PathType Any -NewerThan $today | Should -BeFalse
        Test-Path -Path $oldDirPath -PathType Any -NewerThan $today | Should -BeFalse
        Test-Path -Path $oldFilePath -NewerThan $today | Should -BeFalse
        Test-Path -Path $oldDirPath -NewerThan $today | Should -BeFalse
    }

    It "Should return true if OlderThan is used and path is older than today" {
        Test-Path -Path $oldFilePath -PathType Leaf -OlderThan $today | Should -BeTrue
        Test-Path -Path $oldDirPath -PathType Container -OlderThan $today | Should -BeTrue
        Test-Path -Path $oldFilePath -PathType Any -OlderThan $today | Should -BeTrue
        Test-Path -Path $oldDirPath -PathType Any -OlderThan $today | Should -BeTrue
        Test-Path -Path $oldFilePath -OlderThan $today | Should -BeTrue
        Test-Path -Path $oldDirPath -OlderThan $today | Should -BeTrue
    }

    It "Should return false if OlderThan is used and path is not older than one day" {
        Test-Path -Path $newFilePath -PathType Leaf -OlderThan $oneDayOld | Should -BeFalse
        Test-Path -Path $newDirPath -PathType Container -OlderThan $oneDayOld | Should -BeFalse
        Test-Path -Path $newFilePath -PathType Any -OlderThan $oneDayOld | Should -BeFalse
        Test-Path -Path $newDirPath -PathType Any -OlderThan $oneDayOld | Should -BeFalse
        Test-Path -Path $newFilePath -OlderThan $oneDayOld | Should -BeFalse
        Test-Path -Path $newDirPath -OlderThan $oneDayOld | Should -BeFalse
    }

    It "Should return true if OlderThan and NewerThan is used together and path exists in date range" {
        Test-Path -Path $oldFilePath -PathType Leaf -NewerThan $twoDaysOld -OlderThan $today | Should -BeTrue
        Test-Path -Path $oldDirPath -PathType Container -NewerThan $twoDaysOld -OlderThan $today | Should -BeTrue
        Test-Path -Path $oldFilePath -PathType Any -NewerThan $twoDaysOld -OlderThan $today | Should -BeTrue
        Test-Path -Path $oldDirPath -PathType Any -NewerThan $twoDaysOld -OlderThan $today | Should -BeTrue
        Test-Path -Path $oldFilePath -NewerThan $twoDaysOld -OlderThan $today | Should -BeTrue
        Test-Path -Path $oldDirPath -NewerThan $twoDaysOld -OlderThan $today | Should -BeTrue
    }

    It "Should return false if OlderThan and NewerThan is used together and path does not exist in date range" {
        Test-Path -Path $newFilePath -PathType Leaf -NewerThan $twoDaysOld -OlderThan $oneDayOld | Should -BeFalse
        Test-Path -Path $newDirPath -PathType Container -NewerThan $twoDaysOld -OlderThan $oneDayOld | Should -BeFalse
        Test-Path -Path $newFilePath -PathType Any -NewerThan $twoDaysOld -OlderThan $oneDayOld | Should -BeFalse
        Test-Path -Path $newDirPath -PathType Any -NewerThan $twoDaysOld -OlderThan $oneDayOld | Should -BeFalse
        Test-Path -Path $newFilePath -NewerThan $twoDaysOld -OlderThan $oneDayOld | Should -BeFalse
        Test-Path -Path $newDirPath -NewerThan $twoDaysOld -OlderThan $oneDayOld | Should -BeFalse
    }
}
