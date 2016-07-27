Describe ".NET class interoperability" -Tags "CI" {
    It "Should access types in System.Console" {
        [System.Console]::TreatControlCAsInput | Should Be $false
    }
}
