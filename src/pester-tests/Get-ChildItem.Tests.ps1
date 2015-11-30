Describe "Get-ChildItem" {
    It "Should list the contents of the current folder" {
        (Get-ChildItem .).Name.Length | Should BeGreaterThan 0

        (ls .).Name.Length | Should BeGreaterThan 0
    }

    It "Should list the contents of the home directory" {
        pushd $HOME
        (Get-ChildItem .).Name.Length | Should BeGreaterThan 0
        popd

        pushd $HOME
        (ls .).Name.Length | Should BeGreaterThan 0
        popd

    }
}
