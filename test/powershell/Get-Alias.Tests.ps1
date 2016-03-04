Describe "Get-Alias" {
    It "Should have a return type of System.Array when gal returns more than one object" {
	$val1=(Get-Alias a*)
	$val2=(Get-Alias c*)
	$i=0

	$val1 | ForEach-Object{ $i++};
	if($i -lt 2) {
	    $val1.GetType().BaseType.FullName | Should Be "System.Management.Automation.CommandInfo"
	}
	else
	{
	    $val1.GetType().BaseType.FullName | Should Be "System.Array"
	}

	$val2 | ForEach-Object{ $i++};
	if($i -lt 2) {
	    $val2.GetType().BaseType.FullName | Should Be "System.Management.Automation.CommandInfo"
	}
	else
	{
	    $val2.GetType().BaseType.FullName | Should Be "System.Array"
	}

    }

    It "should return an array of 3 objects" {
	$val = Get-Alias a*
	$alias = gal a*

	$val.CommandType | Should Not BeNullOrEmpty
	$val.Name	 | Should Not BeNullOrEmpty
	$val.ModuleName  | Should BeNullOrEmpty

	$alias.CommandType | Should Not BeNullOrEmpty
	$alias.Name        | Should Not BeNullOrEmpty
	$alias.ModuleName  | Should BeNullOrEmpty
    }
}
