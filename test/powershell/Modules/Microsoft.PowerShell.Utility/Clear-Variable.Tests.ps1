# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Clear-Variable DRT Unit Tests" -Tags "CI" {
	It "Clear-Variable normal variable Name should works"{
		Set-Variable foo bar
		Clear-Variable -Name foo
		$var1=Get-Variable -Name foo
		$var1.Name | Should -BeExactly "foo"
		$var1.Value | Should -BeNullOrEmpty
		$var1.Options | Should -BeExactly "None"
		$var1.Description | Should -BeNullOrEmpty
	}

	It "Clear-Variable ReadOnly variable Name should throw exception and force Clear-Variable should works"{
		Set-Variable foo bar -Option ReadOnly

		$e = { Clear-Variable -Name foo -Scope 1 -ErrorAction Stop } | Should -Throw -ErrorId "VariableNotWritable,Microsoft.PowerShell.Commands.ClearVariableCommand" -PassThru
		$e.CategoryInfo | Should -Match "SessionStateUnauthorizedAccessException"

		Clear-Variable -Name foo -Force
		$var1=Get-Variable -Name foo
		$var1.Name | Should -BeExactly "foo"
		$var1.Value | Should -BeNullOrEmpty
		$var1.Options | Should -BeExactly "ReadOnly"
		$var1.Description | Should -BeNullOrEmpty
	}

	It "Clear-Variable normal variable Name with local scope should works"{
		Set-Variable foo bar
		&{
			Set-Variable foo baz
			$foo | Should -Be baz
			Clear-Variable -Name foo -Scope "local"

			$var1=Get-Variable -Name foo -Scope "local"
			$var1.Name | Should -BeExactly "foo"
			$var1.Value | Should -BeNullOrEmpty
			$var1.Options | Should -BeExactly "None"
			$var1.Description | Should -BeNullOrEmpty
		}

		$var1=Get-Variable -Name foo
		$var1.Name | Should -BeExactly "foo"
		$var1.Value | Should -BeExactly "bar"
		$var1.Options | Should -BeExactly "None"
		$var1.Description | Should -BeNullOrEmpty
	}

	It "Clear-Variable Private variable Name should works and Get-Variable with local scope should throw exception"{
		Set-Variable foo bar -Option Private
		&{
			$e = { Get-Variable -Name foo -Scope local -ErrorAction Stop } |
				Should -Throw -ErrorId "VariableNotFound,Microsoft.PowerShell.Commands.GetVariableCommand" -PassThru
			$e.CategoryInfo | Should -Match "ItemNotFoundException"
		}

		$var1=Get-Variable -Name foo
		$var1.Name | Should -BeExactly "foo"
		$var1.Value | Should -BeExactly "bar"
		$var1.Options | Should -BeExactly "Private"
		$var1.Description | Should -BeNullOrEmpty
	}

	It "Clear-Variable normal variable Name with local scope should works in different scope"{
		Set-Variable foo bar
		&{
			Set-Variable foo baz
			Clear-Variable -Name foo -Scope "local"

			$var1=Get-Variable -Name foo -Scope "local"
			$var1.Name | Should -BeExactly "foo"
			$var1.Value | Should -BeNullOrEmpty
			$var1.Options | Should -BeExactly "None"
			$var1.Description | Should -BeNullOrEmpty
		}

		$var1=Get-Variable -Name foo
		$var1.Name | Should -BeExactly "foo"
		$var1.Value | Should -BeExactly "bar"
		$var1.Options | Should -BeExactly "None"
		$var1.Description | Should -BeNullOrEmpty

		$var1=Get-Variable -Name foo -Scope "local"
		$var1.Name | Should -BeExactly "foo"
		$var1.Value | Should -BeExactly "bar"
		$var1.Options | Should -BeExactly "None"
		$var1.Description | Should -BeNullOrEmpty
	}
}

Describe "Clear-Variable" -Tags "CI" {
	BeforeEach {
		$var1 = 3
	}

	It "Should be able to clear a variable using the Name switch" {
		Clear-Variable -Name var1
		$var1 | Should -BeNullOrEmpty
		{ Get-Variable var1 } | Should -Not -Throw
	}

	It "Should be able to clear a variable without using the Name switch" {
		Clear-Variable var1
		$var1 | Should -BeNullOrEmpty
		{ Get-Variable var1 } | Should -Not -Throw
	}

	It "Should be able to include a set of variables to clear" {
		$var1      = 2
		$var2      = 3
		$var3      = 4
		$variable1 = 23
		$variable2 = 4
		$variable3 = 2
		$w         = 3

		Clear-Variable -Name w, vari* -Include w, vari*

		$variable1 | Should -BeNullOrEmpty
		$variable2 | Should -BeNullOrEmpty
		$variable3 | Should -BeNullOrEmpty
		$w         | Should -BeNullOrEmpty

		$var1 | Should -Not -BeNullOrEmpty
		$var2 | Should -Not -BeNullOrEmpty
		$var3 | Should -Not -BeNullOrEmpty
	}

	It "Should be able to exclude a set of variables to clear" {
		$var1      = 2
		$var2      = 3
		$var3      = 4
		$variable1 = 23
		$variable2 = 4
		$variable3 = 2
		$w         = 3

		Clear-Variable -Name w, vari* -Exclude var*

		$variable1 | Should -Not -BeNullOrEmpty
		$variable2 | Should -Not -BeNullOrEmpty
		$variable3 | Should -Not -BeNullOrEmpty
		$var1      | Should -Not -BeNullOrEmpty
		$var2      | Should -Not -BeNullOrEmpty
		$var3      | Should -Not -BeNullOrEmpty

		$w         | Should -BeNullOrEmpty
	}

	It "Should be able to pass the cleared object through the pipeline using the passthru switch" {
		{ Clear-Variable -Name var1 -PassThru | Format-Wide -Property Value } | Should -Not -Throw
	}

	It "Should not clear environment variables" {
		$env:TEMPVARIABLE = "test data"

		{ Clear-Variable -Name env:TEMPVARIABLE -ErrorAction Stop } | Should -Throw
	}

	It "Should clear variable even if it is read-only using the Force parameter" {
		try
		{
			New-Variable -Name var2 -Option ReadOnly -Value 100

			Clear-Variable -Name var1
			Clear-Variable -Name var2 -Force

			$var1 | Should -BeNullOrEmpty
			$var2 | Should -BeNullOrEmpty
		}
		finally
		{
			Remove-Variable -Name var2 -Force
		}
	}

	It "Should throw error when trying to clear variable that is read-only without using the Force parameter" {
		New-Variable -Name var2 -Option ReadOnly -Value 100
		{ Clear-Variable -Name var2 -Scope 1 -ErrorAction stop } | Should -Throw -ErrorId "VariableNotWritable,Microsoft.PowerShell.Commands.ClearVariableCommand"

		$var2 | Should -Not -BeNullOrEmpty

		Remove-Variable -Name var2 -Force
	}

	Context "Scope Tests" {
	# This will violate the DRY principle.  Tread softly.

		It "Should be able to clear a global scope variable using the global switch" {
			New-Variable globalVar -Value 1 -Scope global -Force

			Clear-Variable -Name globalVar -Scope global

			$globalVar | Should -BeNullOrEmpty
		}

		It "Should not be able to clear a global scope variable using the local switch" {
			New-Variable globalVar -Value 1 -Scope global -Force

			{ Clear-Variable -Name globalVar -Scope local -ErrorAction Stop } |
				Should -Throw -ErrorId "VariableNotFound,Microsoft.PowerShell.Commands.ClearVariableCommand"
		}

		It "Should not be able to clear a global variable using the script scope switch" {
			New-Variable globalVar -Value 1 -Scope global -Force

			{ Clear-Variable -Name localVar -Scope script -ErrorAction Stop } | Should -Throw -ErrorId "VariableNotFound,Microsoft.PowerShell.Commands.ClearVariableCommand"
		}

		It "Should be able to clear an item locally using the local switch" {
			New-Variable localVar -Value 2 -Scope local -Force

			Clear-Variable -Name localVar -Scope local

			$localVar | Should -BeNullOrEmpty

			{ Clear-Variable -Name localVar -Scope script -ErrorAction Stop } | Should -Throw -ErrorId "VariableNotFound,Microsoft.PowerShell.Commands.ClearVariableCommand"
		}

		It "Should not be able to clear an item locally using the global switch" {
			New-Variable localVar -Value 2 -Scope local -Force

			{ Clear-Variable -Name localVar -Scope global -ErrorAction Stop } | Should -Throw -ErrorId "VariableNotFound,Microsoft.PowerShell.Commands.ClearVariableCommand"
		}

		It "Should not be able to clear a local variable using the script scope switch" {
			New-Variable localVar -Value 2 -Scope local -Force

			{ Clear-Variable -Name localVar -Scope script -ErrorAction Stop } | Should -Throw -ErrorId "VariableNotFound,Microsoft.PowerShell.Commands.ClearVariableCommand"
		}

		It "Should be able to clear a script variable created using the script switch" {
			{
				New-Variable -Name derp2 -Value 3 -Scope script -Force
				Clear-Variable -Name derp2 -Scope script
			} | Should -Not -Throw
		}

		It "Should be able to clear a global script variable that was created using the script scope switch" {
			{
				New-Variable -Name derpx -Value 4 -Scope script -Force
				Clear-Variable -Name derpx -Scope script
			} | Should -Not -Throw
		}
	}
}
