Describe "Get-TypeData" {
    It "Should be able to call Get-TypeData with no arguments without throwing" {
        { Get-TypeData } | Should Not Throw
    }

    It "Should return an array of several elements when no arguments are used" {
        $output = Get-TypeData

        $output.Length | Should BeGreaterThan 1
    }

    It "Should be able to take wildcard input" {
        $output = Get-TypeData *Sys*

        $output.Length | Should BeGreaterThan 1
    }
}