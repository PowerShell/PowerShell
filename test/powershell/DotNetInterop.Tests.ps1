Describe ".NET class interoperability" {
    It "Should access types in System.Console" {
        [System.Console]::TreatControlCAsInput | Should Be $false
    }
}
