Describe "Test-Split-Path" {
    $testDir                = "/tmp"
    $testfile               = "testfile.ps1"
    $FullyQualifiedTestFile = $testDir + "/" + $testFile

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

    It "Should return the path when the noqualifier switch is used on a linux system" { 
        { Split-Path /usr/bin -NoQualifier } | Should Not Throw
        Split-Path /usr/bin -NoQualifier     | Should Be "/usr/bin"
    }

    It "Should return the parent folder name when the leaf switch is used" {
        Split-Path /usr/bin -Leaf       | Should be "bin"
        Split-Path /usr/local/bin -Leaf | Should be "bin"
        Split-Path usr/bin -Leaf        | Should be "bin"
    }

    It "Should be able to accept regular expression input and output an array for multiple objects" {
        $testfile2               = "testfilenumber2.ps1"
        $FullyQualifiedTestFile2 = $testDir + "/" + $testfile2

        New-Item -ItemType file -Path $FullyQualifiedTestFile, $FullyQualifiedTestFile2

        Test-Path $FullyQualifiedTestFile  | Should Be $true
        Test-Path $FullyQualifiedTestFile2 | Should Be $true

        ( Split-Path /tmp/*estf*.ps1 -Leaf -Resolve ).GetType().BaseType.Name | Should Be "Array"
        ( Split-path /tmp/*estf*.ps1 -Leaf -Resolve )[0]                      | Should Be $testfile
        ( Split-path /tmp/*estf*.ps1 -Leaf -Resolve )[1]                      | Should Be $testfile2

        Remove-Item $FullyQualifiedTestFile, $FullyQualifiedTestFile2
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
