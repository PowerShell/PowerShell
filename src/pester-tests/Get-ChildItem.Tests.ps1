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

    It "Should have a the proper fields and be populated" {
        $var = Get-Childitem .

        $var.Name.Length   | Should BeGreaterThan 0
        $var.Mode.Length   | Should BeGreaterThan 0
        $var.LastWriteTime | Should BeGreaterThan 0
        $var.Length.Length | Should BeGreaterThan 0

    }
}
