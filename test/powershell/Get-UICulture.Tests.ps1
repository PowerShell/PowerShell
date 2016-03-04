Describe "Get-UICulture" {
    It "Should return a type of CultureInfo as the name of the type" {
	(Get-UICulture).GetType().Name | Should Match CultureInfo
    }

    It "Should have $ PsUICulture variable be equivalent to Get-UICulture object" {
	(Get-UICulture).Name | Should Be $PsUICulture
    }
}
