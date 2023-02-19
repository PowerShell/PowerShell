# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Basic Alias Provider Tests" -Tags "CI" {
    BeforeAll {
        $testAliasName = "TestAlias"
        $testAliasValue = "Get-Date"
    }

    BeforeEach {
        New-Item -Path "Alias:\" -Name $testAliasName -Value $testAliasValue > $null
    }

    AfterEach {
        Remove-Item -Path "Alias:\${testAliasName}" -Force -ErrorAction SilentlyContinue
    }

    It "Test number of alias not Zero" {
        $aliases = @(Get-ChildItem "Alias:\")
        $aliases.Count | Should -Not -Be 0
    }

    It "Test alias dir" {
        $dirAlias = Get-Item "Alias:\dir"
        $dirAlias.CommandType | Should -BeExactly "Alias"
        $dirAlias.Name | Should -BeExactly "dir"
        $dirAlias.Definition | Should -BeExactly "Get-ChildItem"
    }

    It "Test creating new alias" {
        try {
            $newAlias = New-Item -Path "Alias:\" -Name "NewTestAlias" -Value $testAliasValue
            $newAlias.CommandType | Should -BeExactly "Alias"
            $newAlias.Name | Should -BeExactly "NewTestAlias"
            $newAlias.Definition | Should -BeExactly $testAliasValue
        }
        finally {
            Remove-Item -Path "Alias:\NewTestAlias" -Force -ErrorAction SilentlyContinue
        }
    }

    It "Test Get-Item on alias provider" {
        $alias = Get-Item -Path "Alias:\${testAliasName}"
        $alias.CommandType | Should -BeExactly "Alias"
        $alias.Name | Should -BeExactly $testAliasName
        $alias.Definition | Should -BeExactly $testAliasValue
    }

    It "Test Test-Path on alias provider" {
        $aliasExists = Test-Path "Alias:\testAlias"
        $aliasExists | Should -BeTrue
    }

    It "Test executing the new alias" {
        $result = Invoke-Expression $testAliasName
        $result | Should -BeOfType DateTime
    }
}

Describe "Extended Alias Provider Tests" -Tags "Feature" {
    Context "Validation of Set-Item parameters for the Alias Provider" {
        BeforeAll {
            $testAliasName = "TestAlias"
            $testAliasName2 = "TestAlias2"
            $testAliasValue = "Get-Date"
        }

        BeforeEach {
            New-Item -Path Alias:\ -Name $testAliasName -Value $testAliasValue > $null
            New-Item -Path Alias:\ -Name $testAliasName2 -Value $testAliasValue > $null
        }

        AfterEach {
            Remove-Item -Path "Alias:\${testAliasName}" -Force -ErrorAction SilentlyContinue
            Remove-Item -Path "Alias:\${testAliasName2}" -Force -ErrorAction SilentlyContinue
        }

        It "Verifying Whatif" {
            $before = (Get-Item -Path "Alias:\${testAliasName}").Definition
            Set-Item -Path "Alias:\${testAliasName}" -Value "Get-Location" -WhatIf
            $after = (Get-Item -Path "Alias:\${testAliasName}").Definition
            $after | Should -BeExactly $before # Definition should not have changed
        }

        It "Verifying Confirm can be bypassed" {
            Set-Item -Path "Alias:\${testAliasName}" -Value "Get-Location" -Confirm:$false
            $result = Get-Item -Path "Alias:\${testAliasName}"
            $result.Definition | Should -BeExactly "Get-Location"
        }

        It "Verifying Force" {
            Set-Item -Path "Alias:\${testAliasName}" -Value "Get-Location" -Force
            $result =  Get-Item -Path "Alias:\${testAliasName}"
            $result.Definition | Should -BeExactly "Get-Location"
        }

        It "Verifying Include" {
            Set-Item -Path "Alias:\*" -Value "Get-Location" -Include "TestAlias*"
            $alias1 = Get-Item -Path "Alias:\${testAliasName}"
            $alias2 = Get-Item -Path "Alias:\${testAliasName2}"
            $alias1.Definition | Should -BeExactly "Get-Location"
            $alias2.Definition | Should -BeExactly "Get-Location"
        }

        It "Verifying Exclude" {
            Set-Item -Path "Alias:\TestAlias*" -Value "Get-Location" -Exclude "*2"
            $alias1 = Get-Item -Path "Alias:\${testAliasName}"
            $alias2 = Get-Item -Path "Alias:\${testAliasName2}"
            $alias1.Definition | Should -BeExactly "Get-Location"
            $alias2.Definition | Should -BeExactly "Get-Date"
        }
    }
}
