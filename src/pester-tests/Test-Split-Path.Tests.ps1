$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$sut = (Split-Path -Leaf $MyInvocation.MyCommand.Path).Replace(".Tests.", ".")
. "$here\$sut"

Describe "Test-Split-Path" {
<#
    Dependencies:
    1. Split-Path - FUT
    2. ForEach
    3. Object piping
    4. ls/Get-ChildItem - filter output of ls

#>
    It "Should return a string object when invoked" {
        (Split-Path .).GetType().Name |Should Be "String"
        (Split-Path . -Leaf).GetType().Name | Should Be "String"
        (Split-Path . -Resolve).GetType().Name | Should Be "String"
    }

    It "Should return the name of the drive when the qualifier switch is used" {
        Split-Path / -Qualifier | Should Be "/"
    }

    It "Should return the parent folder name when the leaf switch is used" {
        Split-Path . -Leaf | Should be "pester-tests"
    }

    It "Should be able to accept regular expression input and output an array for multiple objects" {
        (Split-Path *Get*.ps1 -Leaf -Resolve).GetType().BaseType.Name | Should Be "Array"
    }

    It "Should be able to tell if a given path is an absolute path" {
        (Split-Path /usr/bin -IsAbsolute) |Should be $true
        (Split-Path . -IsAbsolute) | Should be $false
    }

    It "Should support piping" {
        ("." | Split-Path -leaf) | Should Be "pester-tests"
    }
}
