# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Set-Variable DRT Unit Tests" -Tags "CI" {
	It "Set-Variable normal variable Name should works"{
		Set-Variable foo bar
		$var1=Get-Variable -Name foo
		$var1.Name | Should -BeExactly "foo"
		$var1.Value | Should -BeExactly "bar"
		$var1.Options | Should -BeExactly "None"
		$var1.Description | Should -BeNullOrEmpty
	}

	It "Set-Variable normal variable Name with position should works"{
		Set-Variable -Name foo bar
		$var1=Get-Variable -Name foo
		$var1.Name | Should -BeExactly "foo"
		$var1.Value | Should -BeExactly "bar"
		$var1.Options | Should -BeExactly "None"
		$var1.Description | Should -BeNullOrEmpty
	}

	It "Set-Variable normal variable Name with scope should works"{
		Set-Variable -Name foo -Value bar0

		Set-Variable -Name foo -Value bar -Scope "1"
		$var1=Get-Variable -Name foo -Scope "1"
		$var1.Name | Should -BeExactly "foo"
		$var1.Value | Should -BeExactly "bar"
		$var1.Options | Should -BeExactly "None"
		$var1.Description | Should -BeNullOrEmpty

		Set-Variable -Name foo -Value newValue -Scope "local"
		$var1=Get-Variable -Name foo -Scope "local"
		$var1.Name | Should -BeExactly "foo"
		$var1.Value | Should -BeExactly "newValue"
		$var1.Options | Should -BeExactly "None"
		$var1.Description | Should -BeNullOrEmpty

		Set-Variable -Name foo -Value newValue2 -Scope "script"
		$var1=Get-Variable -Name foo -Scope "script"
		$var1.Name | Should -BeExactly "foo"
		$var1.Value | Should -BeExactly "newValue2"
		$var1.Options | Should -BeExactly "None"
		$var1.Description | Should -BeNullOrEmpty
	}

	It "Set-Variable normal variable Name with position should works"{
		Set-Variable abcaVar bar
		Set-Variable bcdaVar anotherVal
		Set-Variable aVarfoo bogusval

		Set-Variable -Name "*aV*" -Value "overwrite" -Include "*Var*" -Exclude "bcd*"

		$var1=Get-Variable -Name "*aVar*" -Scope "local"
		$var1[0].Name | Should -BeExactly "abcaVar"
		$var1[0].Value | Should -BeExactly "overwrite"
		$var1[0].Options | Should -BeExactly "None"
		$var1[0].Description | Should -BeNullOrEmpty

		$var1[1].Name | Should -BeExactly "aVarfoo"
		$var1[1].Value | Should -BeExactly "overwrite"
		$var1[1].Options | Should -BeExactly "None"
		$var1[1].Description | Should -BeNullOrEmpty

		$var1[2].Name | Should -BeExactly "bcdaVar"
		$var1[2].Value | Should -BeExactly "anotherVal"
		$var1[2].Options | Should -BeExactly "None"
		$var1[2].Description | Should -BeNullOrEmpty
	}

	It "Set-Variable normal variable Name with Description and Value should works"{
		Set-Variable foo bar
		Set-Variable -Name foo $null -Description "new description" -PassThru:$true -Scope "local"
		$var1=Get-Variable -Name foo -Scope "local"
		$var1.Name | Should -BeExactly "foo"
		$var1.Value | Should -BeNullOrEmpty
		$var1.Options | Should -BeExactly "None"
		$var1.Description | Should -BeExactly "new description"
	}

	It "Set-Variable normal variable Name with just Description should works"{
		Set-Variable foo bar
		Set-Variable -Name foo -Description "new description" -PassThru:$true -Scope "local"
		$var1=Get-Variable -Name foo -Scope "local"
		$var1.Name | Should -BeExactly "foo"
		$var1.Value | Should -BeExactly "bar"
		$var1.Options | Should -BeExactly "None"
		$var1.Description | Should -BeExactly "new description"
	}

	It "Set-Variable overwrite Constant Option should throw SessionStateUnauthorizedAccessException"{
		Set-Variable -Name abcaVar bar -Option Constant -Scope "local"
		{ Set-Variable -Name abcaVar new -Scope 1 -ErrorAction Stop } |  Should -Throw -ErrorId "VariableNotWritable,Microsoft.PowerShell.Commands.SetVariableCommand"
	}

	It "Set-Variable of existing Private variable without force should throw Exception"{
		Set-Variable abcaVar bar -Description "new description" -Option Private
		$var1=Get-Variable -Name abcaVar
		$var1.Name | Should -BeExactly "abcaVar"
		$var1.Value | Should -BeExactly "bar"
		$var1.Options | Should -BeExactly "Private"
		$var1.Description | Should -BeExactly "new description"

		Set-Variable abcaVar other -Description "new description"
		$var1=Get-Variable -Name abcaVar
		$var1.Name | Should -BeExactly "abcaVar"
		$var1.Value | Should -BeExactly "other"
		$var1.Options | Should -BeExactly "Private"
		$var1.Description | Should -BeExactly "new description"
	}

	It "Set-Variable with Exclude, then Get-Variable it should throw ItemNotFoundException"{
		Set-Variable -Name foo1,foo2 hello -Exclude foo2 -ErrorAction Stop
		{ Get-Variable -Name foo2 -ErrorAction Stop } | Should -Throw -ErrorId "VariableNotFound,Microsoft.PowerShell.Commands.GetVariableCommand"
	}

	It "Set-Variable of existing ReadOnly variable without force should throw Exception"{
		Set-Variable abcaVar bar -Description "new description" -Option ReadOnly
		$var1=Get-Variable -Name abcaVar
		$var1.Name | Should -BeExactly "abcaVar"
		$var1.Value | Should -BeExactly "bar"
		$var1.Options | Should -BeExactly "ReadOnly"
		$var1.Description | Should -BeExactly "new description"
		{ Set-Variable abcaVar -Option None -Scope 1 -ErrorAction Stop } | Should -Throw -ErrorId "VariableNotWritable,Microsoft.PowerShell.Commands.SetVariableCommand"
	}

	It "Set-Variable of ReadOnly variable with private scope should work"{
		Set-Variable foo bar -Description "new description" -Option ReadOnly -Scope "private"
		$var1=Get-Variable -Name foo
		$var1.Name | Should -BeExactly "foo"
		$var1.Value | Should -BeExactly "bar"
		$var1.Options | Should -BeExactly "ReadOnly, Private"
		$var1.Description | Should -BeExactly "new description"
	}

	It "Set-Variable pipeline with Get-Variable should work"{
		$footest1="bar"
		${Get-Variable footest1 -valueonly|Set-Variable bootest1 -passthru}
		$var1=Get-Variable -Name footest1
		$var1.Name | Should -BeExactly "footest1"
		$var1.Value | Should -BeExactly "bar"
		$var1.Options | Should -BeExactly "None"
		$var1.Description | Should -BeNullOrEmpty
	}
}

Describe "Set-Variable" -Tags "CI" {
    BeforeAll {
        if ($null -ne $PSStyle) {
            $outputRendering = $PSStyle.OutputRendering
            $PSStyle.OutputRendering = 'plaintext'
        }

        $expectedContent = "some test text"
        $inObject = New-Object psobject -Property @{text=$expectedContent}
        $testfile = Join-Path -Path $TestDrive -ChildPath outfileTest.txt
    }

    AfterAll {
        if ($null -ne $PSStyle) {
            $PSStyle.OutputRendering = $outputRendering
        }
    }

    It "Should create a new variable with no parameters" {
	{ Set-Variable testVar } | Should -Not -Throw
    }

    It "Should assign a value to a variable it has to create" {
	Set-Variable -Name testVar -Value 4

	Get-Variable testVar -ValueOnly | Should -Be 4
    }

    It "Should change the value of an already existing variable" {
	$testVar=1

	$testVar | Should -Not -Be 2

	Set-Variable testVar -Value 2

	$testVar | Should -Be 2
    }

    It "Should be able to be called with the set alias" {
	set testVar -Value 1

	$testVar | Should -Be 1
    }

    It "Should be able to be called with the sv alias" {
	sv testVar -Value 2

	$testVar | Should -Be 2
    }

    It "Should be able to set variable name using the Name parameter" {
	Set-Variable -Name testVar -Value 1

	$testVar | Should -Be 1
    }

    It "Should be able to set the value of a variable by piped input" {
	$testValue = "piped input"

	$testValue | Set-Variable -Name testVar

	$testVar | Should -Be $testValue
    }

    It "Should be able to pipe object properties to output using the PassThru switch" {
	$in = Set-Variable -Name testVar -Value "test" -Description "test description" -PassThru

	$output = $in | Format-List -Property Description | Out-String

	# This will cause errors running these tests in Windows
	$output.Trim() | Should -BeExactly "Description : test description"
    }

    It "Should be able to set the value using the value switch" {
	Set-Variable -Name testVar -Value 4

	$testVar | Should -Be 4

	Set-Variable -Name testVar -Value "test"

	$testVar | Should -BeExactly "test"
    }

    Context "Scope Tests" {
	It "Should be able to set a global scope variable using the global switch" {
	    { Set-Variable globalVar -Value 1 -Scope global -Force } | Should -Not -Throw
	}

	It "Should be able to set a global variable using the script scope switch" {
	    { Set-Variable globalVar -Value 1 -Scope script -Force } | Should -Not -Throw
	}

	It "Should be able to set an item locally using the local switch" {
	    { Set-Variable globalVar -Value 1 -Scope local -Force } | Should -Not -Throw
	}
    }

    Context "Set-Variable -Append tests" {
        BeforeAll {
            $testCases = @{ value = 2; Count = 2 },
                @{ value = @(2,3,4); Count = 2},
                @{ value = "abc",(Get-Process -Id $PID) ; count = 2}
        }

        It "Can append values <value> to a variable" -testCases $testCases {
            param ($value, $count)

            $variableName = "testVar"
            Set-Variable -Name $variableName -Value 1
            Set-Variable -Name $variableName -Value $value -Append

            $observedValues = Get-Variable $variableName -Value

            $observedValues.Count | Should -Be $count
            $observedValues[0] | Should -Be 1

            $observedValues[1] | Should -Be $value
        }

        It "Can use set-variable via streaming and append values" {
            $testVar = 1
            4..6 | Set-Variable -Name testVar -Append
            $testVar | Should -Be @(1,4,5,6)
        }
    }
}
