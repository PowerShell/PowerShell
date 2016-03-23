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

    It "should create an object with using Property switch" {
    $hash = @{
        FirstVal = 'test1'
        SecondVal = 'test2'
    }
	$o = New-Object psobject -Property $hash

	$o.FirstVal     | Should Be 'test1'
	$o.SecondVal    | Should Be 'test2'
    }

    It "should create a .Net object with using ArgumentList switch" {
	$o = New-Object -TypeName System.Version -ArgumentList "1.2.3.4"
    $val = $o.GetType()

	$o.Major      | Should Be 1
	$o.Minor      | Should Be 2
    $o.Build      | Should Be 3
    $o.Revision   | Should Be 4
    
    $val.IsPublic       | Should Be $true
	$val.IsSerializable | Should Be $false
	$val.Name           | Should Be 'Version'
	$val.BaseType       | Should Be 'System.Object'
    }
}
