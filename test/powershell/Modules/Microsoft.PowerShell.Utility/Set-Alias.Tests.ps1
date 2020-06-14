# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Set-Alias DRT Unit Tests" -Tags "CI" {
	It "Set-Alias Invalid Scope Name should throw PSArgumentException"{
			{ Set-Alias -Name "ABCD" -Value "foo" -Scope "bogus" } | Should -Throw -ErrorId "Argument,Microsoft.PowerShell.Commands.SetAliasCommand"
	}

	It "Set-Alias ReadOnly Force"{
			Set-Alias -Name ABCD -Value "foo" -Option ReadOnly -Force:$true
			$result=Get-Alias -Name ABCD
			$result.Name | Should -BeExactly "ABCD"
			$result.Definition | Should -BeExactly "foo"
			$result.Description | Should -BeNullOrEmpty
			$result.Options | Should -BeExactly "ReadOnly"

			Set-Alias -Name ABCD -Value "foo" -Force:$true
			$result=Get-Alias -Name ABCD
			$result.Name | Should -BeExactly "ABCD"
			$result.Definition | Should -BeExactly "foo"
			$result.Description | Should -BeNullOrEmpty
			$result.Options | Should -BeExactly "None"
	}

	It "Set-Alias Name And Value Valid"{
			Set-Alias -Name ABCD -Value "MyCommand"
			$result=Get-Alias -Name ABCD
			$result.Name | Should -BeExactly "ABCD"
			$result.Definition | Should -BeExactly "MyCommand"
			$result.Description | Should -BeNullOrEmpty
			$result.Options | Should -BeExactly "None"
	}
	It "Set-Alias Name And Value Positional Valid"{
			Set-Alias -Name ABCD "foo"
			$result=Get-Alias ABCD
			$result.Name | Should -BeExactly "ABCD"
			$result.Definition | Should -BeExactly "foo"
			$result.Description | Should -BeNullOrEmpty
			$result.Options | Should -BeExactly "None"
	}
	It "Set-Alias Description Valid"{
			Set-Alias -Name ABCD -Value "MyCommand" -Description "test description"
			$result=Get-Alias -Name ABCD
			$result.Name | Should -BeExactly "ABCD"
			$result.Definition | Should -BeExactly "MyCommand"
			$result.Description | Should -BeExactly "test description"
			$result.Options | Should -BeExactly "None"
	}
	It "Set-Alias Scope Valid"{
			Set-Alias -Name ABCD -Value "localfoo" -Scope local -Force:$true
			Set-Alias -Name ABCD -Value "foo1" -Scope "1" -Force:$true

			$result=Get-Alias -Name ABCD
			$result.Name | Should -BeExactly "ABCD"
			$result.Definition | Should -BeExactly "localfoo"
			$result.Description | Should -BeNullOrEmpty
			$result.Options | Should -BeExactly "None"

			$result=Get-Alias -Name ABCD -Scope local
			$result.Name | Should -BeExactly "ABCD"
			$result.Definition | Should -BeExactly "localfoo"
			$result.Description | Should -BeNullOrEmpty
			$result.Options | Should -BeExactly "None"

			$result=Get-Alias -Name ABCD -Scope "1"
			$result.Name | Should -BeExactly "ABCD"
			$result.Definition | Should -BeExactly "foo1"
			$result.Description | Should -BeNullOrEmpty
			$result.Options | Should -BeExactly "None"
	}
	It "Set-Alias Expose Bug 1062958, BugId:905449"{
			{ Set-Alias -Name "ABCD" -Value "foo" -Scope "-1" } | Should -Throw -ErrorId "ArgumentOutOfRange,Microsoft.PowerShell.Commands.SetAliasCommand"
	}
}

Describe "Set-Alias" -Tags "CI" {
    Mock Get-Date { return "Friday, October 30, 2015 3:38:08 PM" }
    It "Should be able to set alias without error" {

	{ Set-Alias -Name gd -Value Get-Date } | Should -Not -Throw
    }

    It "Should be able to have the same output between set-alias and the output of the function being aliased" {
	Set-Alias -Name gd -Value Get-Date
	gd | Should -Be $(Get-Date)
    }

    It "Should be able to use the sal alias" {
	{ sal gd Get-Date } | Should -Not -Throw
    }

    It "Should have the same output between the sal alias and the original set-alias cmdlet" {
	sal -Name gd -Value Get-Date

	Set-Alias -Name gd2 -Value Get-Date

	gd2 | Should -Be $(gd)
    }
}
