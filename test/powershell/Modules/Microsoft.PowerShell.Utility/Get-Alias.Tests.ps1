
Describe "Get-Alias DRT Unit Tests" -Tags "CI" {
    It "Get-Alias Bogus Scope Name should throw PSArgumentException"{    
        try { 
            Get-Alias -Name "ABCD" -Scope "bogus"
            Throw "Execution OK"
        } 
        catch {
            $_.FullyQualifiedErrorId | Should be "Argument,Microsoft.PowerShell.Commands.GetAliasCommand"
        }
    }
    It "Get-Alias OutOfRange Scope"{
        try { 
            Get-Alias -Name "ABCD" -Scope "99999"
            Throw "Execution OK"
        } 
        catch {
            $_.FullyQualifiedErrorId | Should be "ArgumentOutOfRange,Microsoft.PowerShell.Commands.GetAliasCommand"
        }
    }
    It "Get-Alias Named Single Valid"{
            Set-Alias -Name ABCD -Value "foo" 
            $result=Get-Alias -Name ABCD
            $result.Name| Should Be "ABCD"
            $result.Definition| Should Be "foo"
            $result.Description| Should Be ""
            $result.Options| Should Be "None"
    }
    It "Get-Alias Positional Single Valid"{
            Set-Alias -Name ABCD -Value "foo" 
            $result=Get-Alias ABCD
            $result.Name| Should Be "ABCD"
            $result.Definition| Should Be "foo"
            $result.Description| Should Be ""
            $result.Options| Should Be "None"
    }
    It "Get-Alias Named Multiple Valid"{
            Set-Alias -Name ABCD -Value "foo" 
            Set-Alias -Name AEFG -Value "bar" 
            $result=Get-Alias -Name ABCD,AEFG
            $result[0].Name| Should Be "ABCD"
            $result[0].Definition| Should Be "foo"
            $result[0].Description| Should Be ""
            $result[0].Options| Should Be "None"
            $result[1].Name| Should Be "AEFG"
            $result[1].Definition| Should Be "bar"
            $result[1].Description| Should Be ""
            $result[1].Options| Should Be "None"
    }
    It "Get-Alias Named Wildcard Valid"{
            Set-Alias -Name ABCD -Value "foo" 
            Set-Alias -Name ABCG -Value "bar" 
            $result=Get-Alias -Name ABC*
            $result[0].Name| Should Be "ABCD"
            $result[0].Definition| Should Be "foo"
            $result[0].Description| Should Be ""
            $result[0].Options| Should Be "None"
            $result[1].Name| Should Be "ABCG"
            $result[1].Definition| Should Be "bar"
            $result[1].Description| Should Be ""
            $result[1].Options| Should Be "None"
    }
    It "Get-Alias Positional Wildcard Valid"{
            Set-Alias -Name ABCD -Value "foo" 
            Set-Alias -Name ABCG -Value "bar" 
            $result=Get-Alias ABC*
            $result[0].Name| Should Be "ABCD"
            $result[0].Definition| Should Be "foo"
            $result[0].Description| Should Be ""
            $result[0].Options| Should Be "None"
            $result[1].Name| Should Be "ABCG"
            $result[1].Definition| Should Be "bar"
            $result[1].Description| Should Be ""
            $result[1].Options| Should Be "None"
    }
    It "Get-Alias Named Wildcard And Exclude Valid"{
            Set-Alias -Name ABCD -Value "foo" 
            Set-Alias -Name ABCG -Value "bar" 
            $result=Get-Alias -Name ABC* -Exclude "*BCG"
            $result[0].Name| Should Be "ABCD"
            $result[0].Definition| Should Be "foo"
            $result[0].Description| Should Be ""
            $result[0].Options| Should Be "None"
    }
    It "Get-Alias Scope Valid"{
            Set-Alias -Name ABCD -Value "foo"
            $result=Get-Alias -Name ABCD
            $result.Name| Should Be "ABCD"
            $result.Definition| Should Be "foo"
            $result.Description| Should Be ""
            $result.Options| Should Be "None"
            
            Set-Alias -Name ABCD -Value "localfoo" -scope local
            $result=Get-Alias -Name ABCD -scope local
            $result.Name| Should Be "ABCD"
            $result.Definition| Should Be "localfoo"
            $result.Description| Should Be ""
            $result.Options| Should Be "None"
            
            Set-Alias -Name ABCD -Value "globalfoo" -scope global
            Set-Alias -Name ABCD -Value "scriptfoo" -scope "script"
            Set-Alias -Name ABCD -Value "foo0" -scope "0"
            Set-Alias -Name ABCD -Value "foo1" -scope "1"
            
            $result=Get-Alias -Name ABCD
            $result.Name| Should Be "ABCD"
            $result.Definition| Should Be "foo0"
            $result.Description| Should Be ""
            $result.Options| Should Be "None"
            
            $result=Get-Alias -Name ABCD -scope local
            $result.Name| Should Be "ABCD"
            $result.Definition| Should Be "foo0"
            $result.Description| Should Be ""
            $result.Options| Should Be "None"
            
            $result=Get-Alias -Name ABCD -scope global
            $result.Name| Should Be "ABCD"
            $result.Definition| Should Be "globalfoo"
            $result.Description| Should Be ""
            $result.Options| Should Be "None"
            
            $result=Get-Alias -Name ABCD -scope "script"
            $result.Name| Should Be "ABCD"
            $result.Definition| Should Be "scriptfoo"
            $result.Description| Should Be ""
            $result.Options| Should Be "None"
            
            $result=Get-Alias -Name ABCD -scope "0"
            $result.Name| Should Be "ABCD"
            $result.Definition| Should Be "foo0"
            $result.Description| Should Be ""
            $result.Options| Should Be "None"
            
            $result=Get-Alias -Name ABCD -scope "1"
            $result.Name| Should Be "ABCD"
            $result.Definition| Should Be "foo1"
            $result.Description| Should Be ""
            $result.Options| Should Be "None"
    }
    It "Get-Alias Expose Bug 1065828, BugId:905235"{
        try { 
            Get-Alias -Name "ABCD" -Scope "100"
            Throw "Execution OK"
        } 
        catch {
            $_.FullyQualifiedErrorId | Should be "ArgumentOutOfRange,Microsoft.PowerShell.Commands.GetAliasCommand"
        }
    }
    It "Get-Alias Zero Scope Valid"{
            Set-Alias -Name ABCD -Value "foo"
            $result=Get-Alias -Name ABCD
            $result.Name| Should Be "ABCD"
            $result.Definition| Should Be "foo"
            $result.Description| Should Be ""
            $result.Options| Should Be "None"
            
            $result=Get-Alias -Name ABCD -scope "0"
            $result.Name| Should Be "ABCD"
            $result.Definition| Should Be "foo"
            $result.Description| Should Be ""
            $result.Options| Should Be "None"
    }

    It "Test get-alias with Definition parameter" {
        $returnObject = Get-Alias -Definition Get-Command
        For($i = 0; $i -lt $returnObject.Length;$i++)
        {
            $returnObject[$i] | Should Not BeNullOrEmpty 
            $returnObject[$i].CommandType | Should Be 'Alias'
            $returnObject[$i].Definition | Should Be 'Get-Command'
        }
    }
}

Describe "Get-Alias" -Tags "CI" {
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

    It "should return an array of objects" {
        $val = Get-Alias a*
        $alias = gal a*

        $val.Count | Should Be $alias.Count
        for ($i=0; $i -lt $val.Count;$i++)
        {
            $val[$i].CommandType | Should Be $alias[$i].CommandType
            $val[$i].Name | Should Be $alias[$i].Name
            $val[$i].ModuleName | Should Be $alias[$i].ModuleName
        }
    }
}
