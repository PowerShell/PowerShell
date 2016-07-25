Describe "Get-Culture DRT Unit Tests" -Tags "CI" {
    It "Should works proper with get-culture" {
        $results = get-Culture
        $results -is "System.Globalization.CultureInfo" | Should be $true
        $results[0].Name | Should Be $PSCulture
    }
}

Describe "Get-Culture" -Tags "CI" {

    It "Should return a type of CultureInfo for Get-Culture cmdlet" {

	(Get-Culture).GetType() | Should Be CultureInfo

    }

    It "Should have $ culture variable be equivalent to (Get-Culture).Name" {

	(Get-Culture).Name | Should Be $PsCulture

    }


}
