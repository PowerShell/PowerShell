Describe "Get-Host DRT Unit Tests" -Tags DRT{
    It "Should works proper with get-host" {
        $results = Get-Host 
        $results | Should Be $Host
        $results.GetType().Name | Should Be InternalHost
    }
}

Describe "Get-Host" {
    It "Should return a type of InternalHost for Get-Host cmdlet" {

	(Get-Host).GetType().Name | Should Be InternalHost

    }

    It "Should have $ host variable be equivalent to Get-Host object" {

	Get-Host | Should Be $host

    }
}
