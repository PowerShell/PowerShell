Describe "Failing test used to test CI Scripts" -Tags 'CI' {
    It "Should fail" {
        1 | should be 2
    }
}
