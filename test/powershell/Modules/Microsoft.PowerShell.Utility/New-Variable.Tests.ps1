# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "New-Variable DRT Unit Tests" -Tags "CI" {
	It "New-Variable variable with description should works"{
		New-Variable foo bar -Description "my description"
		$var1=Get-Variable -Name foo
		$var1.Name | Should -BeExactly "foo"
		$var1.Value | Should -BeExactly "bar"
		$var1.Options | Should -BeExactly "None"
		$var1.Description | Should -BeExactly "my description"
	}

	It "New-Variable variable with option should works"{
		New-Variable foo bar -Option Constant
		$var1=Get-Variable -Name foo
		$var1.Name | Should -BeExactly "foo"
		$var1.Value | Should -BeExactly "bar"
		$var1.Options | Should -BeExactly "Constant"
		$var1.Description | Should -BeNullOrEmpty
	}

	It "New-Variable variable twice should throw Exception"{
		New-Variable foo bogus

		$e = { New-Variable foo bar -Scope 1 -ErrorAction Stop } |
		    Should -Throw -ErrorId "VariableAlreadyExists,Microsoft.PowerShell.Commands.NewVariableCommand" -PassThru
		$e.CategoryInfo | Should -Match "SessionStateException"

		New-Variable foo bar -Force -PassThru
		$var1=Get-Variable -Name foo
		$var1.Name | Should -BeExactly "foo"
		$var1.Value | Should -BeExactly "bar"
		$var1.Options | Should -BeExactly "None"
		$var1.Description | Should -BeNullOrEmpty
	}

	It "New-Variable ReadOnly variable twice should throw Exception"{
		New-Variable foo bogus -Option ReadOnly

		$e = { New-Variable foo bar -Scope 1 -ErrorAction Stop } |
		    Should -Throw -ErrorId "VariableAlreadyExists,Microsoft.PowerShell.Commands.NewVariableCommand" -PassThru
		$e.CategoryInfo | Should -Match "SessionStateException"

		New-Variable foo bar -Force -PassThru
		$var1=Get-Variable -Name foo
		$var1.Name | Should -BeExactly "foo"
		$var1.Value | Should -BeExactly "bar"
		$var1.Options | Should -BeExactly "None"
		$var1.Description | Should -BeNullOrEmpty
	}
}

Describe "New-Variable" -Tags "CI" {
    It "Should create a new variable with no parameters" {
	{ New-Variable var1 } | Should -Not -Throw
    }

    It "Should be able to set variable name using the Name parameter" {
	{ New-Variable -Name var1 } | Should -Not -Throw
    }

    It "Should be able to assign a value to a variable using the value switch" {
	New-Variable var1 -Value 4

	$var1 | Should -Be 4
    }

    It "Should be able to assign a value to a new variable without using the value switch" {
	New-Variable var1 "test"

	$var1 | Should -BeExactly "test"
    }

    It "Should assign a description to a new variable using the description switch" {
	New-Variable var1 100 -Description "Test Description"

	(Get-Variable var1).Description | Should -BeExactly "Test Description"
    }

    It "Should not be able to set the name of a new variable to that of an old variable within same scope when the Force switch is missing" {
        New-Variable var1
        { New-Variable var1 -Scope 1 -ErrorAction Stop } | Should -Throw -ErrorId "VariableAlreadyExists,Microsoft.PowerShell.Commands.NewVariableCommand"
    }

    It "Should change the value of an already existing variable using the Force switch" {
	New-Variable var1 -Value 1

	$var1 | Should -Be 1

	New-Variable var1 -Value 2 -Force

	$var1 | Should -Be 2
	$var1 | Should -Not -Be 1

    }

    It "Should be able to set the value of a variable by piped input" {
	$in = "value"

	$in | New-Variable -Name var1

	$var1 | Should -Be $in

    }

    It "Should be able to pipe object properties to output using the PassThru switch" {
	$in = Set-Variable -Name testVar -Value "test" -Description "test description" -PassThru

	$in.Description | Should -BeExactly "test description"
    }

    It "Should be able to set the value using the value switch" {
	New-Variable -Name var1 -Value 2

	$var1 | Should -Be 2
    }

    Context "Option tests" {
	It "Should be able to use the options switch without error" {
		{ New-Variable -Name var1 -Value 2 -Option Unspecified } | Should -Not -Throw
	}

	It "Should default to none as the value for options" {
		 (New-Variable -Name var2 -Value 4 -PassThru).Options | Should -BeExactly "None"
	}

	It "Should be able to set ReadOnly option" {
		{ New-Variable -Name var1 -Value 2 -Option ReadOnly } | Should -Not -Throw
	}

	It "Should not be able to change variable created using the ReadOnly option when the Force switch is not used" {
		New-Variable -Name var1 -Value 1 -Option ReadOnly

		Set-Variable -Name var1 -Value 2 -ErrorAction SilentlyContinue

		$var1 | Should -Not -Be 2
	}

	It "Should be able to set a new variable to constant" {
		{ New-Variable -Name var1 -Option Constant } | Should -Not -Throw
	}

	It "Should not be able to change an existing variable to constant" {
		New-Variable -Name var1 -Value 1 -PassThru

		Set-Variable -Name var1 -Option Constant  -ErrorAction SilentlyContinue

		(Get-Variable var1).Options | Should -BeExactly "None"
	}

	It "Should not be able to delete a constant variable" {
		New-Variable -Name var1 -Value 2 -Option Constant

		Remove-Variable -Name var1 -ErrorAction SilentlyContinue

		$var1 | Should -Be 2
	}

	It "Should not be able to change a constant variable" {
		New-Variable -Name var1 -Value 1 -Option Constant

		Set-Variable -Name var1 -Value 2  -ErrorAction SilentlyContinue

		$var1 | Should -Not -Be 2
	}

	It "Should be able to create a variable as private without error" {
		{ New-Variable -Name var1 -Option Private } | Should -Not -Throw
	}

	It "Should be able to see the value of a private variable when within scope" {

		New-Variable -Name var1 -Value 100 -Option Private

		$var1 | Should -Be 100

	}

	It "Should not be able to see the value of a private variable when out of scope" {
		{New-Variable -Name var1 -Value 1 -Option Private} | Should -Not -Throw

		$var1 | Should -BeNullOrEmpty
	}

	It "Should be able to use the AllScope switch without error" {
	    { New-Variable -Name var1 -Option AllScope } | Should -Not -Throw
	}

	It "Should be able to see variable created using the AllScope switch in a child scope" {
	    New-Variable -Name var1 -Value 1 -Option AllScope
	    &{ $var1 = 2 }
		$var1 | Should -Be 2
	}

    }

    Context "Scope Tests" {
    BeforeAll {
        if ( Get-Variable -Scope global -Name globalVar1 -ErrorAction SilentlyContinue )
        {
            Remove-Variable -Scope global -Name globalVar1
        }
        if ( Get-Variable -Scope script -Name scriptvar -ErrorAction SilentlyContinue )
        {
            Remove-Variable -Scope script -Name scriptvar
        }
        # no check for local scope variable as that scope is created with test invocation
    }
    AfterAll {
        if ( Get-Variable -Scope global -Name globalVar1 )
        {
            Remove-Variable -Scope global -Name globalVar1
        }
        if ( Get-Variable -Scope script -Name scriptvar )
        {
            Remove-Variable -Scope script -Name scriptvar
        }
    }
    It "Should be able to create a global scope variable using the global switch" {
        New-Variable -Scope global -Name globalvar1 -Value 1
        Get-Variable -Scope global -Name globalVar1 -ValueOnly | Should -Be 1
    }
    It "Should be able to create a local scope variable using the local switch" {
        Get-Variable -Scope local -Name localvar -ValueOnly -ErrorAction silentlycontinue | Should -BeNullOrEmpty
        New-Variable -Scope local -Name localVar -Value 10
        Get-Variable -Scope local -Name localvar -ValueOnly | Should -Be 10
    }
    It "Should be able to create a script scope variable using the script switch" {
        New-Variable -Scope script -Name scriptvar -Value 100
        Get-Variable -Scope script -Name scriptvar -ValueOnly | Should -Be 100
    }
	}
}
