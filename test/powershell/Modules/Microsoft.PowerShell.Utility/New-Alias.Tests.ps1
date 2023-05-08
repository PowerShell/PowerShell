# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "New-Alias DRT Unit Tests" -Tags "CI" {
	It "New-Alias Constant should throw SessionStateUnauthorizedAccessException"{
		New-Alias -Name "ABCD" -Value "foo" -Option "Constant" -Force:$true
		$e = { New-Alias -Name "ABCD" -Value "foo" -Force:$true -Scope 1 -ErrorAction Stop } |
			Should -Throw -ErrorId "AliasNotWritable,Microsoft.PowerShell.Commands.NewAliasCommand" -PassThru
		$e.CategoryInfo | Should -Match "SessionStateUnauthorizedAccessException"
	}

	It "New-Alias NamePositional And Value Valid" {
			New-Alias ABCD -Value "MyCommand" -Scope "0"
			$result=Get-Alias -Name ABCD -Scope "0"
			$result.Name | Should -BeExactly "ABCD"
			$result.Definition | Should -BeExactly "MyCommand"
			$result.Description | Should -BeNullOrEmpty
			$result.Options | Should -BeExactly "None"
	}

	It "New-Alias NamePositional And ValuePositional Valid" {
			New-Alias ABCD "MyCommand" -Scope "0"
			$result=Get-Alias -Name ABCD -Scope "0"
			$result.Name | Should -BeExactly "ABCD"
			$result.Definition | Should -BeExactly "MyCommand"
			$result.Description | Should -BeNullOrEmpty
			$result.Options | Should -BeExactly "None"
	}

	It "New-Alias Description Valid" {
			New-Alias -Name ABCD -Value "MyCommand" -Description "test description" -Scope "0"
			$result=Get-Alias -Name ABCD -Scope "0"
			$result.Name | Should -BeExactly "ABCD"
			$result.Definition | Should -BeExactly "MyCommand"
			$result.Description | Should -BeExactly "test description"
			$result.Options | Should -BeExactly "None"
	}
}

Describe "New-Alias" -Tags "CI" {
    It "Should be able to be called using the name and value parameters without error" {
	{ New-Alias -Name testAlias -Value 100 } | Should -Not -Throw
    }

    It "Should have the same output between the alias and the original cmdlet" {
	New-Alias -Name testAlias -Value Get-Command

	$aliasId  = $(testAlias).Id
	$cmdletId = $(Get-Command).Id
	foreach ($IdNumber in $aliasId)
	{
	    $aliasId[$IdNumber] | Should -Be $cmdletId[$IdNumber]
	}
    }

    It "Should be able to call the New-Alias cmdlet using the nal alias without error" {
	{ nal -Name testAlias -Value 100 } | Should -Not -Throw
    }

    It "Should have the same output between the nal alias and the New-Alias cmdlet" {
	nal -Name testAlias -Value Get-Command

	New-Alias -Name testalias2 -Value Get-Command

	$aliasData = $(testAlias).Id
	$cmdletData = $(testAlias2).Id

	foreach ($IdNumber in $aliasData)
	{
	    $aliasData[$IdNumber] | Should -Be $cmdletData[$IdNumber]
	}
    }
}
