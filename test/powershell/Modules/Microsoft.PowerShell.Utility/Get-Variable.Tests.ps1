
Describe "Get-Variable DRT Unit Tests" -Tags "CI" {
	It "Get-Variable of not existing variable Name should throw ItemNotFoundException"{
		try {
			Get-Variable -EA Stop -Name nonexistingVariableName
			Throw "Execution OK"
		}
		catch {
			$_.FullyQualifiedErrorId | Should be "VariableNotFound,Microsoft.PowerShell.Commands.GetVariableCommand"
		}
	}

	It "Get-Variable of existing variable Name with include and bogus exclude should work"{
		Set-Variable newVar testing
		$var1=get-variable -Name newVar -Include newVar -Exclude bogus
		$var1.Name|Should Be "newVar"
		$var1.Value|Should Be "testing"
	}

	It "Get-Variable of existing variable Name with Description and Option should work"{
		Set-Variable newVar testing -Option ReadOnly -Description "testing description"
		$var1=get-variable -Name newVar
		$var1.Name|Should Be "newVar"
		$var1.Value|Should Be "testing"
		$var1.Options|Should Be "ReadOnly"
		$var1.Description|Should Be "testing description"
	}

	It "Get-Variable of existing variable Globbing Name should work"{
		Set-Variable abcaVar testing
		Set-Variable bcdaVar "another test"
		Set-Variable aVarfoo wow
		$var1=get-variable -Name *aVar* -Scope local
		$var1.Count | Should be 3
		$var1[0].Name|Should Be "abcaVar"
		$var1[0].Value|Should Be "testing"
		$var1[1].Name|Should Be "aVarfoo"
		$var1[1].Value|Should Be "wow"
		$var1[2].Name|Should Be "bcdaVar"
		$var1[2].Value|Should Be "another test"
	}

	It "Get-Variable of existing private variable Name should throw ItemNotFoundException"{
		try {
			Set-Variable newVar testing -Option Private
			&{Get-Variable -Name newVar -EA Stop}
			Throw "Execution OK"
		}
		catch {
			$_.FullyQualifiedErrorId | Should be "VariableNotFound,Microsoft.PowerShell.Commands.GetVariableCommand"
		}
	}
}

Describe "Get-Variable" -Tags "CI" {
    It "Should be able to call with no parameters without error" {
		{ Get-Variable } | Should Not Throw
    }

    It "Should return environment variables when called with no parameters" {
		(Get-Variable).Name -contains "$" | Should Be $true
		(Get-Variable).Name -contains "?" | Should Be $true
		(Get-Variable).Name -contains "HOST" | Should Be $true
		(Get-Variable).Name -contains "PWD" | Should Be $true
		(Get-Variable).Name -contains "PID" | Should Be $true
		(Get-Variable).Name -contains "^" | Should Be $true
    }

    It "Should return the value of an object" {
		New-Variable -Name tempVar -Value 1
		(Get-Variable tempVar).Value | Should Be (1)
    }

    It "Should be able to call using the Name switch" {
		New-Variable -Name var1 -Value 4

		{ Get-Variable -Name var1 } | Should Not Throw

		(Get-Variable -Name var1).Value | Should Be 4

		Remove-Variable var1
    }

    It "Should be able to use wildcard characters in the Name field" {
		New-Variable -Name var1 -Value 4
		New-Variable -Name var2 -Value "test"

		(Get-Variable -Name var*).Value[0] | Should be 4
		(Get-Variable -Name var*).Value[1] | Should be "test"

		Remove-Variable var1
		Remove-Variable var2
    }

    It "Should return only the value if the value switch is used" {
		New-Variable -Name var1 -Value 4

		Get-Variable -Name var1 -ValueOnly | Should be 4

		Remove-Variable var1
    }

    It "Should pipe string to the name field without the Name field being specified"{
		New-Variable -Name var1 -Value 3

		("var1" | Get-Variable ).Value | Should Be 3

		Remove-Variable var1
    }

    It "Should be able to include a set of variables to get" {
		New-Variable -Name var1 -Value 4
		New-Variable -Name var2 -Value 2

		$actual = Get-Variable -Include var1, var2

		$actual[0].Name | Should Be var1
		$actual[1].Name | Should Be var2

		$actual[0].Value | Should Be 4
		$actual[1].Value | Should Be 2

		Remove-Variable var1
		Remove-Variable var2
    }

    It "Should be able to exclude a set of variables to get" {
		New-Variable -Name var1 -Value 4
		New-Variable -Name var2 -Value 2
		New-Variable -Name var3 -Value "test"

		$actual = Get-Variable -Exclude var1, var2

		$actual.Name -contains "var3" | Should Be $true
    }

    Context "Scope Tests" {
	# This will violate the DRY principle.  Tread softly.
	It "Should be able to get a global scope variable using the global switch" {
	    New-Variable globalVar -Value 1 -Scope global -Force

	    (Get-Variable -Name globalVar -Scope global).Value | Should Be 1
	}

	It "Should not be able to clear a global scope variable using the local switch" {
	    New-Variable globalVar -Value 1 -Scope global -Force

	    Get-Variable -Name globalVar -Scope local -ErrorAction SilentlyContinue | Should Throw
	}

	It "Should be able to get a global variable when there's one in the script scope" {
	    New-Variable globalVar -Value 1 -Scope global -Force
	    { New-Variable globalVar -Value 2 -Scope script -Force}

	    (Get-Variable -Name globalVar).Value | Should Be 1
	}

	It "Should be able to get an item locally using the local switch" {
	    {
		New-Variable localVar -Value 1 -Scope local -Force

		Get-Variable -Name localVar -Scope local
	    } | Should Not Throw
	}

	It "Should be able to get a variable created in the global scope when there's one in local scope" {
	    New-Variable localVar -Value 1 -Scope local -Force

	    New-Variable localVar -Value 2 -Scope global -Force

	    (Get-Variable -Name localVar -Scope global).Value | Should Be 2
	}

	It "Should be able to get a script variable created using the script switch" {
	    {
		New-Variable scriptVar -Value 1 -Scope script -Force

		Get-Variable -Name scriptVar -Scope script
	    } | Should Not Throw
	}

	It "Should be able to clear a global script variable that was created using the script scope switch" {
	    {
		New-Variable scriptVar -Value 1 -Scope script -Force

		Get-Variable -Name scriptVar -Scope script
	    } | Should Not Throw
	}
    }
}
