# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "ComparisonOperator" -Tag "CI" {

    It "Should be <result> for <lhs> <operator> <rhs>" -TestCases @(
        @{lhs = 1; operator = "-lt"; rhs = 2; result = $true},
        @{lhs = 1; operator = "-gt"; rhs = 2; result = $false},
        @{lhs = 1; operator = "-le"; rhs = 2; result = $true},
        @{lhs = 1; operator = "-le"; rhs = 1; result = $true},
        @{lhs = 1; operator = "-ge"; rhs = 2; result = $false},
        @{lhs = 1; operator = "-ge"; rhs = 1; result = $true},
        @{lhs = 1; operator = "-eq"; rhs = 1; result = $true},
        @{lhs = 1; operator = "-ne"; rhs = 2; result = $true},
        @{lhs = "'abc'"; operator = "-ceq"; rhs = "'abc'"; result = $true}
        @{lhs = "'abc'"; operator = "-ceq"; rhs = "'Abc'"; result = $false}
        @{lhs = 1; operator = "-and"; rhs = 1; result = $true},
        @{lhs = 1; operator = "-and"; rhs = 0; result = $false},
        @{lhs = 0; operator = "-and"; rhs = 0; result = $false},
        @{lhs = 1; operator = "-or"; rhs = 1; result = $true},
        @{lhs = 1; operator = "-or"; rhs = 0; result = $true},
        @{lhs = 0; operator = "-or"; rhs = 0; result = $false}
    ) {
        param($lhs, $operator, $rhs, $result)
	    Invoke-Expression "$lhs $operator $rhs" | Should -Be $result
    }

	It "Should be <result> for <operator> <rhs>" -TestCases @(
        @{operator = "-not "; rhs = "1"; result = $false},
        @{operator = "-not "; rhs = "0"; result = $true},
        @{operator = "! "; rhs = "1"; result = $false},
        @{operator = "! "; rhs = "0"; result = $true},
        @{operator = "!"; rhs = "1"; result = $false},
        @{operator = "!"; rhs = "0"; result = $true}
    ) {
        param($operator, $rhs, $result)
        Invoke-Expression "$operator$rhs" | Should -Be $result
    }

	It "Should be <result> for <lhs> <operator> <rhs>" -TestCases @(
        @{lhs = "'Hello'"; operator = "-contains"; rhs = "'Hello'"; result = $true},
        @{lhs = "'Hello'"; operator = "-notcontains"; rhs = "'Hello'"; result = $false},
        @{lhs = "'Hello','world'"; operator = "-ccontains"; rhs = "'hello'"; result = $false},
        @{lhs = "'Hello','world'"; operator = "-ccontains"; rhs = "'Hello'"; result = $true}
        @{lhs = "'Hello','world'"; operator = "-cnotcontains"; rhs = "'Hello'"; result = $false}
        @{lhs = "'Hello world'"; operator = "-match"; rhs = "'Hello*'"; result = $true},
        @{lhs = "'Hello world'"; operator = "-like"; rhs = "'Hello*'"; result = $true},
        @{lhs = "'Hello world'"; operator = "-notmatch"; rhs = "'Hello*'"; result = $false},
        @{lhs = "'Hello world'"; operator = "-notlike"; rhs = "'Hello*'"; result = $false}
    ) {
        param($lhs, $operator, $rhs, $result)
        Invoke-Expression "$lhs $operator $rhs" | Should -Be $result
    }

    It "Should return error if right hand is not a valid type: 'hello' <operator> <type>" -TestCases @(
        @{operator = "-is"; type = "'foo'";    expectedError='RuntimeException,Microsoft.PowerShell.Commands.InvokeExpressionCommand'},
        @{operator = "-isnot"; type = "'foo'"; expectedError='RuntimeException,Microsoft.PowerShell.Commands.InvokeExpressionCommand'},
        @{operator = "-is"; type = "[foo]";    expectedError='TypeNotFound,Microsoft.PowerShell.Commands.InvokeExpressionCommand'},
        @{operator = "-isnot"; type = "[foo]"; expectedError='TypeNotFound,Microsoft.PowerShell.Commands.InvokeExpressionCommand'}
    ) {
        param($operator, $type, $expectedError)
        { Invoke-Expression "'Hello' $operator $type" } | Should -Throw -ErrorId $expectedError
    }

    It "Should succeed in comparing type: <lhs> <operator> <rhs>" -TestCases @(
        @{lhs = '[pscustomobject]@{foo=1}'; operator = '-is'; rhs = '[pscustomobject]'},
        @{lhs = '[pscustomobject]@{foo=1}'; operator = '-is'; rhs = '[psobject]'},
        @{lhs = '"hello"'; operator = '-is'; rhs = "[string]"},
        @{lhs = '"hello"'; operator = '-is'; rhs = "[system.string]"},
        @{lhs = '100'; operator = '-is'; rhs = "[int]"},
        @{lhs = '100'; operator = '-is'; rhs = "[system.int32]"},
        @{lhs = '"hello"'; operator = '-isnot'; rhs = "[int]"}
    ) {
        param($lhs, $operator, $rhs)
        Invoke-Expression "$lhs $operator $rhs" | Should -BeTrue
    }

    It "Should fail in comparing type: <lhs> <operator> <rhs>" -TestCases @(
        @{lhs = '[pscustomobject]@{foo=1}'; operator = '-is'; rhs = '[string]'},
        @{lhs = '"hello"'; operator = '-is'; rhs = "[psobject]"},
        @{lhs = '"hello"'; operator = '-isnot'; rhs = "[string]"}
    ) {
        param($lhs, $operator, $rhs)
        Invoke-Expression "$lhs $operator $rhs" | Should -BeFalse
    }

    It "Should be <result> for backtick comparison <lhs> <operator> <rhs>" -TestCases @(
        @{ lhs = 'abc`def'; operator = '-like'; rhs = 'abc`def'; result = $false }
        @{ lhs = 'abc`def'; operator = '-like'; rhs = 'abc``def'; result = $true }
        @{ lhs = 'abc`def'; operator = '-like'; rhs = 'abc````def'; result = $false }
        @{ lhs = 'abc``def'; operator = '-like'; rhs = 'abc````def'; result = $true }
        @{ lhs = 'abc`def'; operator = '-like'; rhs = [WildcardPattern]::Escape('abc`def'); result = $true }
        @{ lhs = 'abc`def'; operator = '-like'; rhs = [WildcardPattern]::Escape('abc``def'); result = $false }
        @{ lhs = 'abc``def'; operator = '-like'; rhs = [WildcardPattern]::Escape('abc``def'); result = $true }
        @{ lhs = 'abc``def'; operator = '-like'; rhs = [WildcardPattern]::Escape('abc````def'); result = $false }
    ) {
        param($lhs, $operator, $rhs, $result)
        $expression = "'$lhs' $operator '$rhs'"
        Invoke-Expression $expression | Should -Be $result
    }
}

Describe "Bytewise Operator" -Tag "CI" {

    It "Test -bor on enum with [byte] as underlying type" {
        $result = [System.Security.AccessControl.AceFlags]::ObjectInherit -bxor `
                  [System.Security.AccessControl.AceFlags]::ContainerInherit
        $result.ToString() | Should -BeExactly "ObjectInherit, ContainerInherit"
    }

    It "Test -bor on enum with [int] as underlying type" {
        $result = [System.Management.Automation.CommandTypes]::Alias -bor `
                  [System.Management.Automation.CommandTypes]::Application
        $result.ToString() | Should -BeExactly "Alias, Application"
    }

    It "Test -band on enum with [byte] as underlying type" {
        $result = [System.Security.AccessControl.AceFlags]::ObjectInherit -band `
                  [System.Security.AccessControl.AceFlags]::ContainerInherit
        $result.ToString() | Should -BeExactly "None"
    }

    It "Test -band on enum with [int] as underlying type" {
        $result = [System.Management.Automation.CommandTypes]::Alias -band `
                  [System.Management.Automation.CommandTypes]::All
        $result.ToString() | Should -BeExactly "Alias"
    }

    It "Test -bxor on enum with [byte] as underlying type" {
        $result = [System.Security.AccessControl.AceFlags]::ObjectInherit -bxor `
                  [System.Security.AccessControl.AceFlags]::ContainerInherit
        $result.ToString() | Should -BeExactly "ObjectInherit, ContainerInherit"
    }

    It "Test -bxor on enum with [int] as underlying type" {
        $result = [System.Management.Automation.CommandTypes]::Alias -bxor `
                  [System.Management.Automation.CommandTypes]::Application
        $result.ToString() | Should -BeExactly "Alias, Application"
    }
}
