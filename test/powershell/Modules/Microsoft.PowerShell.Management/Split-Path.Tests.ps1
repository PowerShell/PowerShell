# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Split-Path" -Tags "CI" {

    It "Should return a string object when invoked" {
        try {
            Push-Location TestDrive:
            $result = Split-Path .
            $result | Should -BeOfType String

            $result = Split-Path . -Leaf
            $result | Should -BeOfType String

            $result = Split-Path . -Resolve
            $result | Should -BeOfType String
        }
        finally {
            Pop-Location
        }
    }

    It "Should return the name of the drive when the qualifier switch is used" {
	Split-Path -Qualifier env:     | Should -Be "env:"
	Split-Path -Qualifier env:PATH | Should -Be "env:"
    }

    It "Should error when using the qualifier switch and no qualifier in the path" {
        { Split-Path -Qualifier -ErrorAction Stop /Users } | Should -Throw
	{ Split-Path -Qualifier -ErrorAction Stop abcdef } | Should -Throw
    }

    It "Should error given positional parameter #2" {
	    { Split-Path env: $NULL } | Should -Throw  -ErrorId 'PositionalParameterNotFound,Microsoft.PowerShell.Commands.SplitPathCommand'
    }

    It "Should return the path when the noqualifier switch is used" {
	Split-Path env:PATH -NoQualifier | Should -BeExactly "PATH"
    }

    It "Should return the base name when the leaf switch is used" {
	Split-Path -Leaf /usr/bin                  | Should -BeExactly "bin"
	Split-Path -Leaf fs:/usr/local/bin         | Should -BeExactly "bin"
	Split-Path -Leaf usr/bin                   | Should -BeExactly "bin"
	Split-Path -Leaf ./bin                     | Should -BeExactly "bin"
	Split-Path -Leaf bin                       | Should -BeExactly "bin"
	Split-Path -Leaf "C:\Temp\Folder1"         | Should -BeExactly "Folder1"
	Split-Path -Leaf "C:\Temp"                 | Should -BeExactly "Temp"
	Split-Path -Leaf "\\server1\share1\folder" | Should -BeExactly "folder"
	Split-Path -Leaf "\\server1\share1"        | Should -BeExactly "share1"
    }

    It "Should be able to accept regular expression input and output an array for multiple objects" {
        $testDir = $TestDrive
        $testFile1     = "testfile1.ps1"
        $testFile2     = "testfile2.ps1"
        $testFilePath1 = Join-Path -Path $testDir -ChildPath $testFile1
        $testFilePath2 = Join-Path -Path $testDir -ChildPath $testFile2

        New-Item -ItemType file -Path $testFilePath1, $testFilePath2 -Force

        Test-Path $testFilePath1 | Should -BeTrue
        Test-Path $testFilePath2 | Should -BeTrue

        $actual = ( Split-Path (Join-Path -Path $testDir -ChildPath "testfile*.ps1") -Leaf -Resolve ) | Sort-Object
        $actual.Count                   | Should -Be 2
        $actual[0]                      | Should -BeExactly $testFile1
        $actual[1]                      | Should -BeExactly $testFile2
        ,$actual                        | Should -BeOfType System.Array
    }

    It "Should be able to tell if a given path is an absolute path" {
	Split-Path -IsAbsolute fs:/usr/bin | Should -BeTrue
	Split-Path -IsAbsolute ..          | Should -BeFalse
	Split-Path -IsAbsolute /usr/..     | Should -Be (!$IsWindows)
	Split-Path -IsAbsolute fs:/usr/../ | Should -BeTrue
	Split-Path -IsAbsolute ../         | Should -BeFalse
	Split-Path -IsAbsolute .           | Should -BeFalse
	Split-Path -IsAbsolute ~/          | Should -BeFalse
	Split-Path -IsAbsolute ~/..        | Should -BeFalse
	Split-Path -IsAbsolute ~/../..     | Should -BeFalse
    }

    It "Should support piping" {
        "usr/bin" | Split-Path | Should -Be "usr"
    }

    It "Should return the path up to the parent of the directory when Parent switch is used" {
        $dirSep = [string]([System.IO.Path]::DirectorySeparatorChar)
	Split-Path -Parent "fs:/usr/bin"             | Should -BeExactly "fs:${dirSep}usr"
	Split-Path -Parent "/usr/bin"                | Should -BeExactly "${dirSep}usr"
	Split-Path -Parent "/usr/local/bin"          | Should -BeExactly "${dirSep}usr${dirSep}local"
	Split-Path -Parent "usr/local/bin"           | Should -BeExactly "usr${dirSep}local"
	Split-Path -Parent "C:\Temp\Folder1"         | Should -BeExactly "C:${dirSep}Temp"
	Split-Path -Parent "C:\Temp"                 | Should -BeExactly "C:${dirSep}"
	Split-Path -Parent "\\server1\share1\folder" | Should -BeExactly "${dirSep}${dirSep}server1${dirSep}share1"
	Split-Path -Parent "\\server1\share1"        | Should -BeExactly "${dirSep}${dirSep}server1"
    }

    It 'Does not split a drive letter'{
    Split-Path -Path 'C:\' | Should -BeNullOrEmpty
    }
}
