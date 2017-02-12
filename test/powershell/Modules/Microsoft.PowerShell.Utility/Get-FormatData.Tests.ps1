Describe "Get-FormatData" -Tags "CI" {

    Context "Check return type of Get-FormatData" {

        It "Should return an object[] as the return type" {
            $result = Get-FormatData
            $result.Count  | Should BeGreaterThan 0
            $result.GetType() | Should be System.Object[]
        }
    }
}
