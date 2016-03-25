Describe "Get-Culture" {

    It "Should return a type of CultureInfo for Get-Culture cmdlet" {

	(Get-Culture).GetType() | Should Be CultureInfo

    }

    It "Should have $ culture variable be equivalent to (Get-Culture).Name" {

	(Get-Culture).Name | Should Be $PsCulture

    }


}
