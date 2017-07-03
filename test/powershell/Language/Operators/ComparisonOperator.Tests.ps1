Describe "ComparisonOperator" -tag "CI" {
    It "Should be $true for 1 -lt 2" {
	 1 -lt 2       | Should Be $true
    }
	It "Should be $false for 1 -gt 2" {
	 1 -gt 2       | Should Be $false
    }
	It "Should be $true for 1 -le 2" {
	 1 -le 2       | Should Be $true
    }
	It "Should be $true for 1 -le 1" {
	 1 -le 1       | Should Be $true
    }
	It "Should be $false for 1 -ge 2" {
	 1 -ge 2       | Should Be $false
    }
	It "Should be $true for 1 -ge 1" {
	 1 -ge 1       | Should Be $true
    }
	It "Should be $true for 1 -eq 1" {
	 1 -eq 1       | Should Be $true
    }
	It "Should be $true for 'abc' -ceq 'abc' and $false for 'abc' -ceq 'Abc'" {
	 'abc' -ceq 'abc'       | Should Be $true
	 'abc' -ceq 'Abc'       | Should Be $false
    }
	It "Should be $true for 1 -ne 2" {
	 1 -ne 2       | Should Be $true
    }

	It "Should be $true for 1 -and 1, $false for 1 -and 0, $false for 0 -and 0" {
	 1 -and 1       | Should Be $true
	 1 -and 0       | Should Be $false
	 0 -and 0       | Should Be $false
    }
	It "Should be $true for 1 -or 1, $true for 1 -or 0, $false for 0 -or 0" {
	 1 -or 1       | Should Be $true
	 1 -or 0       | Should Be $true
	 0 -or 0       | Should Be $false
    }
	It "Should be $false for -not 1, $true for -not 0" {
	 -not 1       | Should Be $false
	 -not 0       | Should Be $true
    }
	It "Should be $false for !1, $true for !0" {
	 !1       | Should Be $false
	 !0       | Should Be $true
    }

	It "Should be <result> for 'Hello','world' <operator> <rhs>" -TestCases @(
        @{result = $true; operator = "-contains"; rhs = "'Hello'"},
        @{result = $false; operator = "-notcontains"; rhs = "'Hello'"}
    ) {
        param($result, $operator, $rhs)
        $arr= "'Hello','world'"
        Invoke-Expression "$arr $operator $rhs" | Should Be $result
    }
	It "Should be $false for 'Hello','world' -ccontains 'hello' and $true for 'Hello','world' -ccontains 'Hello'" {
	 $arr= 'Hello','world'
	 $arr -ccontains 'hello'       | Should Be $false
	 $arr -ccontains 'Hello'       | Should Be $true
    }
	It "Should be $true for 'Hello world' -match 'Hello*'" {
	 "Hello world" -match "Hello*"       | Should Be $true
    }
	It "Should be $true for 'Hello world' -like 'Hello*'" {
	 "Hello world" -like "Hello*"       | Should Be $true
    }
	It "Should be $false for 'Hello world' -notmatch 'Hello*'" {
	 "Hello world" -notmatch "Hello*"       | Should Be $false
    }
	It "Should be $false for 'Hello world' -notlike 'Hello*'" {
	 "Hello world" -notlike "Hello*"       | Should Be $false
    }

    # -Is/-IsNot operator
    It "Should return error if right hand is not a valid type: 'hello' <operator> <type>" -TestCases @(
        @{operator = "-is"; type = "'foo'"},
        @{operator = "-isnot"; type = "'foo'"},
        @{operator = "-is"; type = "[foo]"},
        @{operator = "-isnot"; type = "[foo]"}
    ) {
        param($operator, $type)
        { Invoke-Expression "'Hello' $operator $type" | Should Be ErrorId "RuntimeException" }
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
        Invoke-Expression "$lhs $operator $rhs" | Should Be $true
    }

    It "Should fail in comparing type: <lhs> <operator> <rhs>" -TestCases @(
        @{lhs = '[pscustomobject]@{foo=1}'; operator = '-is'; rhs = '[string]'},
        @{lhs = '"hello"'; operator = '-is'; rhs = "[psobject]"},
        @{lhs = '"hello"'; operator = '-isnot'; rhs = "[string]"}
    ) {
        param($lhs, $operator, $rhs)
        Invoke-Expression "$lhs $operator $rhs" | Should Be $false
    }
}


Describe "Bytewise Operator" -tag "CI" {

    It "Test -bor on enum with [byte] as underlying type" {
        $result = [System.Security.AccessControl.AceFlags]::ObjectInherit -bxor `
                  [System.Security.AccessControl.AceFlags]::ContainerInherit
        $result.ToString() | Should Be "ObjectInherit, ContainerInherit"
    }

    It "Test -bor on enum with [int] as underlying type" {
        $result = [System.Management.Automation.CommandTypes]::Alias -bor `
                  [System.Management.Automation.CommandTypes]::Application
        $result.ToString() | Should Be "Alias, Application"
    }

    It "Test -band on enum with [byte] as underlying type" {
        $result = [System.Security.AccessControl.AceFlags]::ObjectInherit -band `
                  [System.Security.AccessControl.AceFlags]::ContainerInherit
        $result.ToString() | Should Be "None"
    }

    It "Test -band on enum with [int] as underlying type" {
        $result = [System.Management.Automation.CommandTypes]::Alias -band `
                  [System.Management.Automation.CommandTypes]::All
        $result.ToString() | Should Be "Alias"
    }

    It "Test -bxor on enum with [byte] as underlying type" {
        $result = [System.Security.AccessControl.AceFlags]::ObjectInherit -bxor `
                  [System.Security.AccessControl.AceFlags]::ContainerInherit
        $result.ToString() | Should Be "ObjectInherit, ContainerInherit"
    }

    It "Test -bxor on enum with [int] as underlying type" {
        $result = [System.Management.Automation.CommandTypes]::Alias -bxor `
                  [System.Management.Automation.CommandTypes]::Application
        $result.ToString() | Should Be "Alias, Application"
    }
}
