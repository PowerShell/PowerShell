Describe "Split-Path" {
    It "Should return a string object when invoked" {
        ( Split-Path . ).GetType().Name          | Should Be "String"
        ( Split-Path . -Leaf ).GetType().Name    | Should Be "String"
        ( Split-Path . -Resolve ).GetType().Name | Should Be "String"
    }

    It "Should return the name of the drive when the qualifier switch is used" {
        Split-Path / -Qualifier        | Should Be "/"
        Split-Path /usr/bin -Qualifier | Should Be "/"
    }

    It "Should error when using the qualifier switch for a windows path while on a nonwindows machine" {
        # ErrorAction SilentlyContinue merely suppresses the error from the console.
        # Throwing exceptions still seen by Pester.

        Split-Path "C:\Users" -Qualifier -ErrorAction SilentlyContinue  | Should Throw
    }

    It "Should error when no directory separator characters are used with a qualifier" {
        Split-Path "abadTest" -Qualifier -ErrorAction SilentlyContinue  | Should Throw
    }

    It "Should return the path when the noqualifier switch is used on a Linux system" {
        { Split-Path /usr/bin -NoQualifier } | Should Not Throw
        Split-Path /usr/bin -NoQualifier     | Should Be "/usr/bin"
    }

    It "Should return the base name when the leaf switch is used" {
        Split-Path /usr/bin -Leaf       | Should be "bin"
        Split-Path /usr/local/bin -Leaf | Should be "bin"
        Split-Path usr/bin -Leaf        | Should be "bin"
        Split-Path ./bin -Leaf          | Should be "bin"
        Split-Path bin -Leaf            | Should be "bin"
    }

    It "Should be able to accept regular expression input and output an array for multiple objects" {
        $testDir = "./tmp"
        Remove-Item $testDir -Recurse -Force -ErrorAction Ignore

	$testFile1     = "testfile1.ps1"
	$testFile2     = "testfile2.ps1"
        $testFilePath1 = $testDir + "/" + $testFile1
        $testFilePath2 = $testDir + "/" + $testFile2

        New-Item -ItemType file -Path $testFilePath1, $testFilePath2 -Force

        Test-Path $testFilePath1 | Should Be $true
        Test-Path $testFilePath2 | Should Be $true

        $actual = ( Split-Path $testDir/*file*.ps1 -Leaf -Resolve ) | Sort-Object
        $actual.GetType().BaseType.Name | Should Be "Array"
        $actual[0]                      | Should Be $testFile1
        $actual[1]                      | Should Be $testFile2

        Remove-Item $testDir -Recurse -Force -ErrorAction Ignore
        Test-Path $testDir | Should Be $false
    }

    It "Should be able to tell if a given path is an absolute path" {
        ( Split-Path /usr/bin -IsAbsolute ) | Should be $true
        ( Split-Path .. -IsAbsolute )       | Should be $false
        ( Split-Path /usr/.. -IsAbsolute )  | Should be $true
        ( Split-Path /usr/../ -IsAbsolute ) | Should be $true
        ( Split-Path ../ -IsAbsolute )      | Should be $false
        ( Split-Path . -IsAbsolute )        | Should be $false
        ( Split-Path ~/ -IsAbsolute )       | Should be $false
        ( Split-Path ~/.. -IsAbsolute )     | Should be $false
        ( Split-Path ~/../.. -IsAbsolute )  | Should be $false

    }

    It "Should support piping" {
        ( "/usr/bin" | Split-Path ) | Should Be "/usr"
    }

    It "Should return the path up to the parent of the directory when Parent switch is used" {
        Split-Path "/usr/bin" -Parent       | Should Be "/usr"
        Split-Path "/usr/local/bin" -Parent | Should Be "/usr/local"
        Split-Path "usr/local/bin" -Parent  | Should Be "usr/local"
    }

    It "Should throw if a parameterSetName is incorrect" {
        { Split-Path "/usr/bin/" -Parentaoeu } | Should Throw "A parameter cannot be found that matches parameter name"
    }
}
