Describe "Basic Validation of Alias Provider" -Tags "CI" {
    BeforeAll {
        $testAliasName = 'TestAlias'
        $testAliasValue = 'Get-Date'
    }

    BeforeEach {
        New-Item -Path Alias:\ -Name $testAliasName -Value $testAliasValue > $null
    }

    AfterEach {
        Remove-Item -Path "Alias:\${testAliasName}" -ErrorAction SilentlyContinue
    }
    
    It "Test number of alias not Zero" {
        $aliases = Get-ChildItem Alias:\
        $aliases.Count | Should Not Be 0
    }

    It "Test alias 'dir'" {
        $dirAlias = Get-Item Alias:\dir
        $dirAlias.CommandType | Should Be 'Alias'
        $dirAlias.Name | Should Be 'dir'
        $dirAlias.Definition | Should Be 'Get-ChildItem'
    }

    It "Test creating new alias" {
        $newAlias = New-Item -Path Alias:\ -Name 'NewTestAlias' -Value $testAliasValue
        try {
            $newAlias.CommandType | Should Be 'Alias'
            $newAlias.Name | Should Be 'NewTestAlias'
            $newAlias.Definition | Should Be $testAliasValue
        }
        finally {
            Remove-Item -Path 'Alias:\NewTestAlias' -ErrorAction SilentlyContinue
        }
    }

    It "Test get-item on alias provider" {
        $alias = Get-Item -Path "Alias:\${testAliasName}"
        $alias.CommandType | Should Be 'Alias'
        $alias.Name | Should Be $testAliasName
        $alias.Definition | Should Be $testAliasValue
    }

    It "Test test-path on alias provider" {
        $aliasExists = Test-Path Alias:\testAlias
        $aliasExists | Should Be $true
    }

    It "Test executing the new alias" {
        $result = testAlias
        $result.GetType().Name | Should Be 'DateTime'
    }
}
