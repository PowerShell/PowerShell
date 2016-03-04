Describe "Get-FormatData" {

    Context "Check return type of Get-FormatData" {

	It "Should return an object[] as the return type" {
	    (Get-FormatData).GetType() | Should be System.Object[]
	}

	It "Should return a System.Object[] with a count greater than 0" {
	    (Get-formatData).Count | Should BeGreaterThan 0
	}
    }
}
