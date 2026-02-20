# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Get-Alias DRT Unit Tests" -Tags "CI" {
    It "Get-Alias Bogus Scope Name should throw PSArgumentException"{
        { Get-Alias -Name "ABCD" -Scope "bogus" } | Should -Throw -ErrorId "Argument,Microsoft.PowerShell.Commands.GetAliasCommand"
    }
    It "Get-Alias OutOfRange Scope"{
        { Get-Alias -Name "ABCD" -Scope "99999" } | Should -Throw -ErrorId "ArgumentOutOfRange,Microsoft.PowerShell.Commands.GetAliasCommand"
    }
    It "Get-Alias Named Single Valid"{
            Set-Alias -Name ABCD -Value "foo"
            $result=Get-Alias -Name ABCD
            $result.Name | Should -BeExactly "ABCD"
            $result.Definition | Should -BeExactly "foo"
            $result.Description | Should -BeNullOrEmpty
            $result.Options | Should -BeExactly "None"
    }
    It "Get-Alias Positional Single Valid"{
            Set-Alias -Name ABCD -Value "foo"
            $result=Get-Alias ABCD
            $result.Name | Should -BeExactly "ABCD"
            $result.Definition | Should -BeExactly "foo"
            $result.Description | Should -BeNullOrEmpty
            $result.Options | Should -BeExactly "None"
    }
    It "Get-Alias Named Multiple Valid"{
            Set-Alias -Name ABCD -Value "foo"
            Set-Alias -Name AEFG -Value "bar"
            $result=Get-Alias -Name ABCD,AEFG
            $result[0].Name | Should -BeExactly "ABCD"
            $result[0].Definition | Should -BeExactly "foo"
            $result[0].Description | Should -BeNullOrEmpty
            $result[0].Options | Should -BeExactly "None"
            $result[1].Name | Should -BeExactly "AEFG"
            $result[1].Definition | Should -BeExactly "bar"
            $result[1].Description | Should -BeNullOrEmpty
            $result[1].Options | Should -BeExactly "None"
    }
    It "Get-Alias Named Wildcard Valid"{
            Set-Alias -Name ABCD -Value "foo"
            Set-Alias -Name ABCG -Value "bar"
            $result=Get-Alias -Name ABC*
            $result[0].Name | Should -BeExactly "ABCD"
            $result[0].Definition | Should -BeExactly "foo"
            $result[0].Description | Should -BeNullOrEmpty
            $result[0].Options | Should -BeExactly "None"
            $result[1].Name | Should -BeExactly "ABCG"
            $result[1].Definition | Should -BeExactly "bar"
            $result[1].Description | Should -BeNullOrEmpty
            $result[1].Options | Should -BeExactly "None"
    }
    It "Get-Alias Positional Wildcard Valid"{
            Set-Alias -Name ABCD -Value "foo"
            Set-Alias -Name ABCG -Value "bar"
            $result=Get-Alias ABC*
            $result[0].Name | Should -BeExactly "ABCD"
            $result[0].Definition | Should -BeExactly "foo"
            $result[0].Description | Should -BeNullOrEmpty
            $result[0].Options | Should -BeExactly "None"
            $result[1].Name | Should -BeExactly "ABCG"
            $result[1].Definition | Should -BeExactly "bar"
            $result[1].Description | Should -BeNullOrEmpty
            $result[1].Options | Should -BeExactly "None"
    }
    It "Get-Alias Named Wildcard And Exclude Valid"{
            Set-Alias -Name ABCD -Value "foo"
            Set-Alias -Name ABCG -Value "bar"
            $result=Get-Alias -Name ABC* -Exclude "*BCG"
            $result[0].Name | Should -BeExactly "ABCD"
            $result[0].Definition | Should -BeExactly "foo"
            $result[0].Description | Should -BeNullOrEmpty
            $result[0].Options | Should -BeExactly "None"
    }
    It "Get-Alias Scope Valid"{
            Set-Alias -Name ABCD -Value "foo"
            $result=Get-Alias -Name ABCD
            $result.Name | Should -BeExactly "ABCD"
            $result.Definition | Should -BeExactly "foo"
            $result.Description | Should -BeNullOrEmpty
            $result.Options | Should -BeExactly "None"

            Set-Alias -Name ABCD -Value "localfoo" -Scope local
            $result=Get-Alias -Name ABCD -Scope local
            $result.Name | Should -BeExactly "ABCD"
            $result.Definition | Should -BeExactly "localfoo"
            $result.Description | Should -BeNullOrEmpty
            $result.Options | Should -BeExactly "None"

            Set-Alias -Name ABCD -Value "globalfoo" -Scope global
            Set-Alias -Name ABCD -Value "scriptfoo" -Scope "script"
            Set-Alias -Name ABCD -Value "foo0" -Scope "0"
            Set-Alias -Name ABCD -Value "foo1" -Scope "1"

            $result=Get-Alias -Name ABCD
            $result.Name | Should -BeExactly "ABCD"
            $result.Definition | Should -BeExactly "foo0"
            $result.Description | Should -BeNullOrEmpty
            $result.Options | Should -BeExactly "None"

            $result=Get-Alias -Name ABCD -Scope local
            $result.Name | Should -BeExactly "ABCD"
            $result.Definition | Should -BeExactly "foo0"
            $result.Description | Should -BeNullOrEmpty
            $result.Options | Should -BeExactly "None"

            $result=Get-Alias -Name ABCD -Scope global
            $result.Name | Should -BeExactly "ABCD"
            $result.Definition | Should -BeExactly "globalfoo"
            $result.Description | Should -BeNullOrEmpty
            $result.Options | Should -BeExactly "None"

            $result=Get-Alias -Name ABCD -Scope "script"
            $result.Name | Should -BeExactly "ABCD"
            $result.Definition | Should -BeExactly "scriptfoo"
            $result.Description | Should -BeNullOrEmpty
            $result.Options | Should -BeExactly "None"

            $result=Get-Alias -Name ABCD -Scope "0"
            $result.Name | Should -BeExactly "ABCD"
            $result.Definition | Should -BeExactly "foo0"
            $result.Description | Should -BeNullOrEmpty
            $result.Options | Should -BeExactly "None"

            $result=Get-Alias -Name ABCD -Scope "1"
            $result.Name | Should -BeExactly "ABCD"
            $result.Definition | Should -BeExactly "foo1"
            $result.Description | Should -BeNullOrEmpty
            $result.Options | Should -BeExactly "None"
    }
    It "Get-Alias Expose Bug 1065828, BugId:905235"{
            { Get-Alias -Name "ABCD" -Scope "100" } | Should -Throw -ErrorId "ArgumentOutOfRange,Microsoft.PowerShell.Commands.GetAliasCommand"
    }
    It "Get-Alias Zero Scope Valid"{
            Set-Alias -Name ABCD -Value "foo"
            $result=Get-Alias -Name ABCD
            $result.Name | Should -BeExactly "ABCD"
            $result.Definition | Should -BeExactly "foo"
            $result.Description | Should -BeNullOrEmpty
            $result.Options | Should -BeExactly "None"

            $result=Get-Alias -Name ABCD -Scope "0"
            $result.Name | Should -BeExactly "ABCD"
            $result.Definition | Should -BeExactly "foo"
            $result.Description | Should -BeNullOrEmpty
            $result.Options | Should -BeExactly "None"
    }

    It "Test get-alias with Definition parameter" {
        $returnObject = Get-Alias -Definition Get-Command
        For($i = 0; $i -lt $returnObject.Length;$i++)
        {
            $returnObject[$i] | Should -Not -BeNullOrEmpty
            $returnObject[$i].CommandType | Should -Be 'Alias'
            $returnObject[$i].Definition | Should -Be 'Get-Command'
        }
    }

    It "Get-Alias DisplayName should always show AliasName -> ResolvedCommand for all aliases" {
        Set-Alias -Name Test-MyAlias -Value Get-Command -Force
        Set-Alias -Name tma -Value Test-MyAlias -force
        $aliases = Get-Alias Test-MyAlias, tma
        $aliases | ForEach-Object {
            $_.DisplayName | Should -Be "$($_.Name) -> Get-Command"
        }
        $aliases.Name.foreach{Remove-Item Alias:$_ -ErrorAction SilentlyContinue}
    }
}

Describe "Get-Alias" -Tags "CI" {
    It "Should have a return type of System.Array when gal returns more than one object" {
        $val1=(Get-Alias a*)
        $val2=(Get-Alias c*)
        $i=0

        $val1 | Should -Not -BeNullOrEmpty
        $val2 | Should -Not -BeNullOrEmpty

        $val1 | ForEach-Object{ $i++};
        if($i -lt 2) {
            $val1 | Should -BeOfType System.Management.Automation.CommandInfo
        }
        else
        {
            ,$val1 | Should -BeOfType System.Array
        }

        $val2 | ForEach-Object{ $i++};
        if($i -lt 2) {
            $val2 | Should -BeOfType System.Management.Automation.CommandInfo
        }
        else
        {
            ,$val2 | Should -BeOfType System.Array
        }
    }

    It "should return an array of objects" {
        $val = Get-Alias a*
        $alias = gal a*

        $val.Count | Should -Be $alias.Count
        for ($i=0; $i -lt $val.Count;$i++)
        {
            $val[$i].CommandType | Should -Be $alias[$i].CommandType
            $val[$i].Name | Should -Be $alias[$i].Name
            $val[$i].ModuleName | Should -Be $alias[$i].ModuleName
        }
    }
}

Describe "Get-Alias null tests" -Tags "CI" {

  $testCases =
    @{ data = $null; value = 'null' },
    @{ data = [string]::Empty; value = 'empty string' }

  Context 'Check null or empty value to the -Name parameter' {
    It 'Should throw if <value> is passed to -Name parameter' -TestCases $testCases {
      param($data)
      { Get-Alias -Name $data } | Should -Throw -ErrorId 'ParameterArgumentValidationError,Microsoft.PowerShell.Commands.GetAliasCommand'
    }
  }
  Context 'Check null or empty value to the -Name parameter via pipeline' {
    It 'Should throw if <value> is passed through pipeline to -Name parameter' -TestCases $testCases {
      param($data)
      { $data | Get-Alias -ErrorAction Stop } | Should -Throw -ErrorId 'ParameterArgumentValidationError,Microsoft.PowerShell.Commands.GetAliasCommand'
    }
  }
}
