Describe "New-Object" {
    It "should create an object with 4 fields" {
	$o = New-Object psobject
	$val = $o.GetType()

	$val.IsPublic       | Should Not BeNullOrEmpty
	$val.Name           | Should Not BeNullOrEmpty
	$val.IsSerializable | Should Not BeNullOrEmpty
	$val.BaseType       | Should Not BeNullOrEmpty

	$val.IsPublic       | Should Be $true
	$val.IsSerializable | Should Be $false
	$val.Name           | Should Be 'PSCustomObject'
	$val.BaseType       | Should Be 'System.Object'
    }
}
