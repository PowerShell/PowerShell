$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$sut = (Split-Path -Leaf $MyInvocation.MyCommand.Path).Replace(".Tests.", ".")
. "$here\$sut"

Describe "Test-Get-ChildItem" {
    Context ": non-aliased testing" {
        It "Should list the contents of the current folder" {
            (Get-ChildItem .).Name.Length | Should BeGreaterThan 0
        }
    
        It "Should list the contents of the home directory" {
            pushd $HOME
            (Get-ChildItem .).Name.Length | Should BeGreaterThan 0
            popd
        }

    }
    Context ": alias tests" {
        It "Should list the contents of the current folder" {
            (ls).Name.Length | Should BeGreaterThan 0
        }
    
        It "Should list the contents of the home directory" {
            pushd $HOME
            (ls).Name.Length | Should BeGreaterThan 0
            popd
        }

        It "Should list the contents of environment variables" {
            (ls ENV:).Count | Should BeGreaterThan 10
            (ls ENV:os).Value | Should Be (Get-ChildItem ENV:os).Value
            (ls ENV:PROCESSOR_ARCHITECTURE).Value | Should Be (Get-ChildItem ENV:PROCESSOR_ARCHITECTURE).Value
            (ls ENV:OS).Value | Should Be $env:OS
        }
    }
}
