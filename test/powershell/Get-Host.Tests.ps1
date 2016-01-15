Describe "Get-Host" {
    It "Should return a type of InternalHost for Get-Host cmdlet" {

	(Get-Host).GetType().Name | Should Be InternalHost

    }

    It "Should have $ host variable be equivalent to Get-Host object" {

	Get-Host | Should Be $host

    }
}
