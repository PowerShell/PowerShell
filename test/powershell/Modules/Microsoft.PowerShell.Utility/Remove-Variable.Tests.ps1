# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# ensure the machine is in a clean state from the outset.
Remove-Variable -Name var1 -ErrorAction SilentlyContinue -Force

Describe "Remove-Variable" -Tags "CI" {
    It "Should throw an error when a dollar sign is used in the variable name place" {
	New-Variable -Name var1 -Value 4

	{ Remove-Variable $var1 -ErrorAction Stop } | Should -Throw -ErrorId "VariableNotFound,Microsoft.PowerShell.Commands.RemoveVariableCommand"
    }

    It "Should not throw error when used without the Name field, and named variable is properly specified and exists" {
	New-Variable -Name var1 -Value 4

	Remove-Variable var1

	$var1 | Should -BeNullOrEmpty
	{ Get-Variable var1 -ErrorAction stop } |
		Should -Throw -ErrorId 'VariableNotFound,Microsoft.PowerShell.Commands.GetVariableCommand'
    }

    It "Should not throw error when used with the Name field, and named variable is specified and exists" {
	New-Variable -Name var1 -Value 2

	Remove-Variable -Name var1

	$var1 | Should -BeNullOrEmpty
	{ Get-Variable var1 -ErrorAction stop } |
		Should -Throw -ErrorId 'VariableNotFound,Microsoft.PowerShell.Commands.GetVariableCommand'
    }

    It "Should throw error when used with Name field, and named variable does not exist" {
	{ Remove-Variable -Name nonexistentVariable -ErrorAction Stop } | Should -Throw
    }

    It "Should be able to remove a set of variables using wildcard characters" {
	New-Variable tmpvar1 -Value "tempvalue"
	New-Variable tmpvar2 -Value 2
	New-Variable tmpmyvar1 -Value 234

	$tmpvar1   | Should -BeExactly "tempvalue"
	$tmpvar2   | Should -Be 2
	$tmpmyvar1 | Should -Be 234

	Remove-Variable -Name tmp*

	$tmpvar1   | Should -BeNullOrEmpty
	$tmpvar2   | Should -BeNullOrEmpty
	$tmpmyvar1 | Should -BeNullOrEmpty

	{ Get-Variable tmpvar1 -ErrorAction stop } |
		Should -Throw -ErrorId 'VariableNotFound,Microsoft.PowerShell.Commands.GetVariableCommand'
	{ Get-Variable tmpvar2 -ErrorAction stop } |
		Should -Throw -ErrorId 'VariableNotFound,Microsoft.PowerShell.Commands.GetVariableCommand'
	{ Get-Variable tmpmyvar1 -ErrorAction stop } |
		Should -Throw -ErrorId 'VariableNotFound,Microsoft.PowerShell.Commands.GetVariableCommand'
    }

    It "Should be able to exclude a set of variables to remove using the Exclude switch" {
	New-Variable tmpvar1 -Value "tempvalue"
	New-Variable tmpvar2 -Value 2
	New-Variable tmpmyvar1 -Value 234

	$tmpvar1   | Should -BeExactly "tempvalue"
	$tmpvar2   | Should -Be 2
	$tmpmyvar1 | Should -Be 234

	Remove-Variable -Name tmp* -Exclude *my*

	$tmpvar1   | Should -BeNullOrEmpty
	$tmpvar2   | Should -BeNullOrEmpty
	$tmpmyvar1 | Should -Be 234

	{ Get-Variable tmpvar1 -ErrorAction stop } |
		Should -Throw -ErrorId 'VariableNotFound,Microsoft.PowerShell.Commands.GetVariableCommand'
	{ Get-Variable tmpvar2 -ErrorAction stop } |
		Should -Throw -ErrorId 'VariableNotFound,Microsoft.PowerShell.Commands.GetVariableCommand'
    }

    It "Should be able to include a set of variables to remove using the Include switch" {
	New-Variable tmpvar1 -Value "tempvalue"
	New-Variable tmpvar2 -Value 2
	New-Variable tmpmyvar1 -Value 234
	New-Variable thevar -Value 1

	$tmpvar1   | Should -BeExactly "tempvalue"
	$tmpvar2   | Should -Be 2
	$tmpmyvar1 | Should -Be 234
	$thevar    | Should -Be 1

	Remove-Variable -Name tmp* -Include *my*

	$tmpvar1   | Should -BeExactly "tempvalue"
	$tmpvar2   | Should -Be 2
	$tmpmyvar1 | Should -BeNullOrEmpty
	$thevar    | Should -Be 1

	{ Get-Variable tmpmyvar1 -ErrorAction stop } |
		Should -Throw -ErrorId 'VariableNotFound,Microsoft.PowerShell.Commands.GetVariableCommand'

	Remove-Variable tmpvar1
	Remove-Variable tmpvar2
	Remove-Variable thevar

    }

    It "Should throw an error when attempting to remove a read-only variable and the Force switch is not used" {
	New-Variable -Name var1 -Value 2 -Option ReadOnly

	{ Remove-Variable -Name var1 -ErrorAction Stop } | Should -Throw

	$var1 | Should -Be 2

	Remove-Variable -Name var1 -Force
    }

    It "Should not throw an error when attempting to remove a read-only variable and the Force switch is used" {
	New-Variable -Name var1 -Value 2 -Option ReadOnly

	Remove-Variable -Name var1 -Force

	$var1 | Should -BeNullOrEmpty
    }

    Context "Scope Tests" {
	It "Should be able to remove a global variable using the global switch" {
	    New-Variable -Name var1 -Value "context" -Scope global

	    Remove-Variable -Name var1 -Scope global

	    $var1 | Should -BeNullOrEmpty
	}

	It "Should not be able to clear a global variable using the local switch" {
	    New-Variable -Name var1 -Value "context" -Scope global

	    { Remove-Variable -Name var1 -Scope local -ErrorAction Stop } | Should -Throw

	    $var1 | Should -BeExactly "context"

	    Remove-Variable -Name var1 -Scope global
	    $var1 | Should -BeNullOrEmpty
	}

	It "Should not be able to clear a global variable using the script switch" {
	    New-Variable -Name var1 -Value "context" -Scope global

	    { Remove-Variable -Name var1 -Scope local -ErrorAction Stop } | Should -Throw

	    $var1 | Should -BeExactly "context"

	    Remove-Variable -Name var1 -Scope global
	    $var1 | Should -BeNullOrEmpty
	}

	It "Should be able to remove an item locally using the local switch" {
	    New-Variable -Name var1 -Value "context" -Scope local

	    { Remove-Variable -Name var1 -Scope local -ErrorAction Stop } | Should -Throw

	    $var1 | Should -Be context
	}

	It "Should be able to remove an item locally using the global switch" {
	    New-Variable -Name var1 -Value "context" -Scope local

	    { Remove-Variable -Name var1 -Scope global -ErrorAction Stop } | Should -Throw

	    $var1 | Should -BeExactly "context"

	    Remove-Variable -Name var1 -Scope local
	    $var1 | Should -BeNullOrEmpty
	}

	It "Should be able to remove a local variable using the script scope switch" {
	    New-Variable -Name var1 -Value "context" -Scope local

	    { Remove-Variable -Name var1 -Scope script -ErrorAction Stop } | Should -Throw

	    $var1 | Should -BeExactly "context"

	    Remove-Variable -Name var1 -Scope local
	    $var1 | Should -BeNullOrEmpty
	}

	It "Should be able to remove a script variable created using the script switch" {
	    New-Variable -Name var1 -Value "context" -Scope script

	    { Remove-Variable -Name var1 -Scope script } | Should -Not -Throw

	    $var1 | Should -BeNullOrEmpty
	}

	It "Should not be able to remove a global script variable that was created using the script scope switch" {
	    New-Variable -Name var1 -Value "context" -Scope script

	    { Remove-Variable -Name var1 -Scope global -ErrorAction Stop } | Should -Throw

	    $var1 | Should -BeExactly "context"
	}
    }
}

Describe "Remove-Variable basic functionality" -Tags "CI" {
	It "Remove-Variable variable should works"{
		New-Variable foo bar
		Remove-Variable foo
		$var1 = Get-Variable -Name foo -ErrorAction SilentlyContinue
		$var1 | Should -BeNullOrEmpty
	}

	It "Remove-Variable Constant variable should throw SessionStateUnauthorizedAccessException"{
		New-Variable foo bar -Option Constant
		$e = { Remove-Variable foo -Scope 1 -ErrorAction Stop } |
		    Should -Throw -ErrorId "VariableNotRemovable,Microsoft.PowerShell.Commands.RemoveVariableCommand" -PassThru
		$e.CategoryInfo | Should -Match "SessionStateUnauthorizedAccessException"
	}

	It "Remove-Variable ReadOnly variable should throw SessionStateUnauthorizedAccessException and force remove should work"{
		New-Variable foo bar -Option ReadOnly
		$e = { Remove-Variable foo -Scope 1 -ErrorAction Stop } |
		    Should -Throw -ErrorId "VariableNotRemovable,Microsoft.PowerShell.Commands.RemoveVariableCommand" -PassThru
		$e.CategoryInfo | Should -Match "SessionStateUnauthorizedAccessException"
		Remove-Variable foo -Force
		$var1 = Get-Variable -Name foo -ErrorAction SilentlyContinue
		$var1 | Should -BeNullOrEmpty
	}

	It "Remove-Variable Constant variable should throw SessionStateUnauthorizedAccessException and force remove should also throw exception"{
		New-Variable foo bar -Option Constant
		$e = { Remove-Variable foo -Scope 1 -ErrorAction Stop } |
		    Should -Throw -ErrorId "VariableNotRemovable,Microsoft.PowerShell.Commands.RemoveVariableCommand" -PassThru
		$e.CategoryInfo | Should -Match "SessionStateUnauthorizedAccessException"

		{ Remove-Variable foo -Force -Scope 1 -ErrorAction Stop } |
		    Should -Throw -ErrorId "VariableNotRemovable,Microsoft.PowerShell.Commands.RemoveVariableCommand" -PassThru
		$e.CategoryInfo | Should -Match "SessionStateUnauthorizedAccessException"
	}

	It "Remove-Variable variable in new scope should works and Get-Variable with different scope should have different result"{
		New-Variable foo bar
		&{
			Clear-Variable foo
			Remove-Variable foo
			$e = { Get-Variable -Name foo -Scope local -ErrorAction Stop } |
				Should -Throw -ErrorId "VariableNotFound,Microsoft.PowerShell.Commands.GetVariableCommand" -PassThru
			$e.CategoryInfo | Should -Match "ItemNotFoundException"
		}

		$var1 = Get-Variable -Name foo
		$var1.Name | Should -BeExactly "foo"
		$var1.Value | Should -BeExactly "bar"
		$var1.Options | Should -BeExactly "None"
		$var1.Description | Should -BeExactly ""

	}
}
