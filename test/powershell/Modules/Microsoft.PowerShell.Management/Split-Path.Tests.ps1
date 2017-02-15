Describe "Split-Path" -Tags "CI" {

    It "Should return a string object when invoked" {
        $result = Split-Path .
        $result | Should BeOfType String

        $result = Split-Path . -Leaf
        $result | Should BeOfType String

        $result = Split-Path . -Resolve
        $result | Should BeOfType String
    }

    It "Should return the name of the drive when the qualifier switch is used" {
	Split-Path -Qualifier env:     | Should Be "env:"
	Split-Path -Qualifier env:PATH | Should Be "env:"
    }

    It "Should error when using the qualifier switch and no qualifier in the path" {
        { Split-Path -Qualifier -ErrorAction Stop /Users } | Should Throw
	{ Split-Path -Qualifier -ErrorAction Stop abcdef } | Should Throw
    }

    It "Should return the path when the noqualifier switch is used" {
	Split-Path env:PATH -NoQualifier | Should Be "PATH"
    }

    It "Should return the base name when the leaf switch is used" {
	Split-Path -Leaf /usr/bin                  | Should be "bin"
	Split-Path -Leaf fs:/usr/local/bin         | Should be "bin"
	Split-Path -Leaf usr/bin                   | Should be "bin"
	Split-Path -Leaf ./bin                     | Should be "bin"
	Split-Path -Leaf bin                       | Should be "bin"
	Split-Path -Leaf "C:\Temp\Folder1"         | Should be "Folder1"
	Split-Path -Leaf "C:\Temp"                 | Should be "Temp"
	Split-Path -Leaf "\\server1\share1\folder" | Should be "folder"
	Split-Path -Leaf "\\server1\share1"        | Should be "share1"
    }

    It "Should be able to accept regular expression input and output an array for multiple objects" {
        $testDir = $TestDrive
        $testFile1     = "testfile1.ps1"
        $testFile2     = "testfile2.ps1"
        $testFilePath1 = Join-Path -Path $testDir -ChildPath $testFile1
        $testFilePath2 = Join-Path -Path $testDir -ChildPath $testFile2

        New-Item -ItemType file -Path $testFilePath1, $testFilePath2 -Force

        Test-Path $testFilePath1 | Should Be $true
        Test-Path $testFilePath2 | Should Be $true

        $actual = ( Split-Path (Join-Path -Path $testDir -ChildPath "testfile*.ps1") -Leaf -Resolve ) | Sort-Object
        $actual.Count                   | Should Be 2
        $actual[0]                      | Should Be $testFile1
        $actual[1]                      | Should Be $testFile2
        ,$actual                        | Should BeOfType "System.Array"
    }

    It "Should be able to tell if a given path is an absolute path" {
	Split-Path -IsAbsolute fs:/usr/bin | Should Be $true
	Split-Path -IsAbsolute ..          | Should Be $false
	Split-Path -IsAbsolute /usr/..     | Should Be (!$IsWindows)
	Split-Path -IsAbsolute fs:/usr/../ | Should Be $true
	Split-Path -IsAbsolute ../         | Should Be $false
	Split-Path -IsAbsolute .           | Should Be $false
	Split-Path -IsAbsolute ~/          | Should Be $false
	Split-Path -IsAbsolute ~/..        | Should Be $false
	Split-Path -IsAbsolute ~/../..     | Should Be $false
    }

    It "Should support piping" {
        "usr/bin" | Split-Path | Should Be "usr"
    }

    It "Should return the path up to the parent of the directory when Parent switch is used" {
        $dirSep = [string]([System.IO.Path]::DirectorySeparatorChar)
	Split-Path -Parent "fs:/usr/bin"             | Should Be "fs:${dirSep}usr"
	Split-Path -Parent "/usr/bin"                | Should Be "${dirSep}usr"
	Split-Path -Parent "/usr/local/bin"          | Should Be "${dirSep}usr${dirSep}local"
	Split-Path -Parent "usr/local/bin"           | Should Be "usr${dirSep}local"
	Split-Path -Parent "C:\Temp\Folder1"         | Should be "C:${dirSep}Temp"
	Split-Path -Parent "C:\Temp"                 | Should be "C:${dirSep}"
	Split-Path -Parent "\\server1\share1\folder" | Should be "${dirSep}${dirSep}server1${dirSep}share1"
	Split-Path -Parent "\\server1\share1"        | Should be "${dirSep}${dirSep}server1"
    }

    It 'Does not split a drive leter'{
    Split-Path -Path 'C:\' | Should be ''
    }
}
