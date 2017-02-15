Describe "Get-FormatData" -Tags "CI" {

    Context "Check return type of Get-FormatData" {

        It "Should return an object[] as the return type" {
            $result = Get-FormatData
            ,$result | Should BeOfType "System.Object[]"
        }
    }
}
