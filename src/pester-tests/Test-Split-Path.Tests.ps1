Describe "Test-Split-Path" {
<#
    Dependencies:

    1. Split-Path - FUT
    2. ForEach
    3. Object piping
    4. ls/Get-ChildItem - filter output of ls

    parent and Literal 

#>
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
         Split-Path "C:\Users" -Qualifier -ErrorAction SilentlyContinue  | Should Throw
    }

    It "Should error when no directory separator characters are used with a qualifier" {
         Split-Path "abadTest" -Qualifier -ErrorAction SilentlyContinue  | Should Throw
    }

    It "Should return the path when the noqualifier switch is used on a linux system" { 
        { Split-Path /usr/bin -NoQualifier } | Should Not Throw
        Split-Path /usr/bin -NoQualifier | Should Be "/usr/bin"
    }

    It "Should return the parent folder name when the leaf switch is used" {
        Split-Path /usr/bin -Leaf | Should be "bin"
    }

    It "Should be able to accept regular expression input and output an array for multiple objects" {
        ( Split-Path *Get*.ps1 -Leaf -Resolve ).GetType().BaseType.Name | Should Be "Array"
    }

    It "Should be able to tell if a given path is an absolute path" {
        ( Split-Path /usr/bin -IsAbsolute ) | Should be $true
        ( Split-Path . -IsAbsolute )        | Should be $false
    }

    It "Should support piping" {
        ( "/usr/bin" | Split-Path ) | Should Be "/usr"
    }

    It "Should return the path up to the parent of the directory when Parent switch is used" {
        Split-Path "/usr/bin" | Should Be ( Split-Path "/usr/bin" -Parent )
    }

    It "Should not throw if a parameterSetName is correct" {
        { Split-Path "/usr/bin/" -Parent } | Should Not Throw
    }

    It "Should throw if a parameterSetName is incorrect" {
        { Split-Path "/usr/bin/" -Parentaoeu } | Should Throw
    }

}
