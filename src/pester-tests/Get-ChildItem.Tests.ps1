Describe "Get-ChildItem" {
    It "Should list the contents of the current folder" {
        (Get-ChildItem .).Name.Length | Should BeGreaterThan 0
    }

    It "Should list the contents of the home directory" {
        pushd $HOME
        (Get-ChildItem .).Name.Length | Should BeGreaterThan 0
        popd
    }

    It "Should be able to use the ls alias" {
        $(ls .).Name.Length | Should Be $(Get-ChildItem .).Name.Length
    }
}
