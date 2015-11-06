Describe "Get-UICulture" {
    It "Should return a type of VistaCultureInfo as the name of the type" {

	(Get-UICulture).GetType().Name | Should Be VistaCultureInfo

    }

    It "Should have $ PsUICulture variable be equivalent to Get-UICulture object" {

	(Get-UICulture).Name | Should Be $PsUICulture

    }
}
