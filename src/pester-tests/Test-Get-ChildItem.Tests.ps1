$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$sut = (Split-Path -Leaf $MyInvocation.MyCommand.Path).Replace(".Tests.", ".")
. "$here/$sut"

Describe "Test-Get-ChildItem" {
    It "Should list the contents of the current folder" {
        (Get-ChildItem .).Name.Length | Should BeGreaterThan 0

        (ls .).Name.Length | Should BeGreaterThan 0
    }

    It "Should list the contents of the home directory" {
        pushd /usr/
        (Get-ChildItem .).Name.Length | Should BeGreaterThan 0
        popd

        pushd /usr/
        (ls .).Name.Length | Should BeGreaterThan 0
        popd

    }
}
