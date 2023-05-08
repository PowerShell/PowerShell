# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Get-Variable DRT Unit Tests" -Tags "CI" {
	It "Get-Variable of not existing variable Name should throw ItemNotFoundException"{
		{ Get-Variable -ErrorAction Stop -Name nonexistingVariableName } |
			Should -Throw -ErrorId "VariableNotFound,Microsoft.PowerShell.Commands.GetVariableCommand"
	}

	It "Get-Variable of existing variable Name with include and bogus exclude should work"{
		Set-Variable newVar testing
		$var1=Get-Variable -Name newVar -Include newVar -Exclude bogus
		$var1.Name | Should -BeExactly "newVar"
		$var1.Value | Should -BeExactly "testing"
	}

	It "Get-Variable of existing variable Name with Description and Option should work"{
		Set-Variable newVar testing -Option ReadOnly -Description "testing description"
		$var1=Get-Variable -Name newVar
		$var1.Name | Should -BeExactly "newVar"
		$var1.Value | Should -BeExactly "testing"
		$var1.Options | Should -BeExactly "ReadOnly"
		$var1.Description | Should -BeExactly "testing description"
	}

	It "Get-Variable of existing variable Globbing Name should work"{
		Set-Variable abcaVar testing
		Set-Variable bcdaVar "another test"
		Set-Variable aVarfoo wow
		$var1=Get-Variable -Name *aVar* -Scope local
		$var1.Count | Should -Be 3
		$var1[0].Name | Should -BeExactly "abcaVar"
		$var1[0].Value | Should -BeExactly "testing"
		$var1[1].Name | Should -BeExactly "aVarfoo"
		$var1[1].Value | Should -BeExactly "wow"
		$var1[2].Name | Should -BeExactly "bcdaVar"
		$var1[2].Value | Should -BeExactly "another test"
	}

	It "Get-Variable of existing private variable Name should throw ItemNotFoundException"{
		Set-Variable newVar testing -Option Private
		{Get-Variable -Name newVar -ErrorAction Stop} |
			Should -Throw -ErrorId "VariableNotFound,Microsoft.PowerShell.Commands.GetVariableCommand"
	}
}

Describe "Get-Variable" -Tags "CI" {
    It "Should be able to call with no parameters without error" {
		{ Get-Variable } | Should -Not -Throw
    }

    It "Should return environment variables when called with no parameters" {
		(Get-Variable).Name -contains "$" | Should -BeTrue
		(Get-Variable).Name -contains "?" | Should -BeTrue
		(Get-Variable).Name -contains "HOST" | Should -BeTrue
		(Get-Variable).Name -contains "PWD" | Should -BeTrue
		(Get-Variable).Name -contains "PID" | Should -BeTrue
		(Get-Variable).Name -contains "^" | Should -BeTrue
    }

    It "Should return the value of an object" {
		New-Variable -Name tempVar -Value 1
		(Get-Variable tempVar).Value | Should -Be (1)
    }

    It "Should be able to call using the Name switch" {
		New-Variable -Name var1 -Value 4

		{ Get-Variable -Name var1 } | Should -Not -Throw

		(Get-Variable -Name var1).Value | Should -Be 4

		Remove-Variable var1
    }

    It "Should be able to use wildcard characters in the Name field" {
		New-Variable -Name var1 -Value 4
		New-Variable -Name var2 -Value "test"

		(Get-Variable -Name var*).Value[0] | Should -Be 4
		(Get-Variable -Name var*).Value[1] | Should -BeExactly "test"

		Remove-Variable var1
		Remove-Variable var2
    }

    It "Should return only the value if the value switch is used" {
		New-Variable -Name var1 -Value 4

		Get-Variable -Name var1 -ValueOnly | Should -Be 4

		Remove-Variable var1
    }

    It "Should pipe string to the name field without the Name field being specified"{
		New-Variable -Name var1 -Value 3

		("var1" | Get-Variable ).Value | Should -Be 3

		Remove-Variable var1
    }

    It "Should be able to include a set of variables to get" {
		New-Variable -Name var1 -Value 4
		New-Variable -Name var2 -Value 2

		$actual = Get-Variable -Include var1, var2

		$actual[0].Name | Should -Be var1
		$actual[1].Name | Should -Be var2

		$actual[0].Value | Should -Be 4
		$actual[1].Value | Should -Be 2

		Remove-Variable var1
		Remove-Variable var2
    }

    It "Should be able to exclude a set of variables to get" {
		New-Variable -Name var1 -Value 4
		New-Variable -Name var2 -Value 2
		New-Variable -Name var3 -Value "test"

		$actual = Get-Variable -Exclude var1, var2

		$actual.Name -contains "var3" | Should -BeTrue
    }

    Context "Scope Tests" {
	# This will violate the DRY principle.  Tread softly.
	It "Should be able to get a global scope variable using the global switch" {
	    New-Variable globalVar -Value 1 -Scope global -Force

	    (Get-Variable -Name globalVar -Scope global).Value | Should -Be 1
	}

	It "Should not be able to clear a global scope variable using the local switch" {
	    New-Variable globalVar -Value 1 -Scope global -Force

	    { Get-Variable -Name globalVar -Scope local -ErrorAction Stop } | Should -Throw -ErrorId "VariableNotFound,Microsoft.PowerShell.Commands.GetVariableCommand"
	}

	It "Should be able to get a global variable when there's one in the script scope" {
	    New-Variable globalVar -Value 1 -Scope global -Force
	    { New-Variable globalVar -Value 2 -Scope script -Force}

	    (Get-Variable -Name globalVar).Value | Should -Be 1
	}

	It "Should be able to get an item locally using the local switch" {
	    {
		New-Variable localVar -Value 1 -Scope local -Force

		Get-Variable -Name localVar -Scope local
	    } | Should -Not -Throw
	}

	It "Should be able to get a variable created in the global scope when there's one in local scope" {
	    New-Variable localVar -Value 1 -Scope local -Force

	    New-Variable localVar -Value 2 -Scope global -Force

	    (Get-Variable -Name localVar -Scope global).Value | Should -Be 2
	}

	It "Should be able to get a script variable created using the script switch" {
	    {
		New-Variable scriptVar -Value 1 -Scope script -Force

		Get-Variable -Name scriptVar -Scope script
	    } | Should -Not -Throw
	}

	It "Should be able to clear a global script variable that was created using the script scope switch" {
	    {
		New-Variable scriptVar -Value 1 -Scope script -Force

		Get-Variable -Name scriptVar -Scope script
	    } | Should -Not -Throw
	}
    }
}
