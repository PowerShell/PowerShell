# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'NullCoalesceOperations' -Tags 'CI' {
    BeforeAll {
        $someGuid = New-Guid
        $typesTests = @(
            @{ name = 'string'; valueToSet = 'hello' }
            @{ name = 'dotnetType'; valueToSet = $someGuid }
            @{ name = 'byte'; valueToSet = [byte]0x94 }
            @{ name = 'intArray'; valueToSet = 1..2 }
            @{ name = 'stringArray'; valueToSet = 'a'..'c' }
            @{ name = 'emptyArray'; valueToSet = @(1, 2, 3) }
        )
    }

    Context "Null conditional assignment operator ??=" {
        It 'Variable doesnot exist' {

            Remove-Variable variableDoesNotExist -ErrorAction SilentlyContinue -Force

            $variableDoesNotExist ??= 1
            $variableDoesNotExist | Should -Be 1

            $variableDoesNotExist ??= 2
            $variableDoesNotExist | Should -Be 1
        }

        It 'Variable exists and is null' {
            $variableDoesNotExist = $null

            $variableDoesNotExist ??= 2
            $variableDoesNotExist | Should -Be 2
        }

        It 'Validate types - <name> can be set' -TestCases $typesTests {
            param ($name, $valueToSet)

            $x = $null
            $x ??= $valueToSet
            $x | Should -Be $valueToSet
        }

        It 'Validate hashtable can be set' {
            $x = $null
            $x ??= @{ 1 = '1' }
            $x.Keys | Should -Be @(1)
        }

        It 'Validate lhs is returned' {
            $x = 100
            $x ??= 200
            $x | Should -Be 100
        }

        It 'Rhs is a cmdlet' {
            $x = $null
            $x ??= (Get-Alias -Name 'where')
            $x.Definition | Should -BeExactly 'Where-Object'
        }

        It 'Lhs is DBNull' {
            $x = [System.DBNull]::Value
            $x ??= 200
            $x | Should -Be ([System.DBNull]::Value)
        }

        It 'Lhs is AutomationNull' {
            $x = [System.Management.Automation.Internal.AutomationNull]::Value
            $x ??= 200
            $x | Should -Be 200
        }

        It 'Lhs is NullString' {
            $x = [NullString]::Value
            $x ??= 200
            $x | Should -Be ([NullString]::Value)
        }

        It 'Lhs is empty string' {
            $x = ''
            $x ??= 20
            $x | Should -BeExactly ''
        }

        It 'Error case' {
            $e = $null
            $null = [System.Management.Automation.Language.Parser]::ParseInput('1 ??= 100', [ref] $null, [ref] $e)
            $e[0].ErrorId | Should -BeExactly 'InvalidLeftHandSide'
        }

        It 'Variable is non-null' {
            $num = 10
            $num ??= 20

            $num | Should -Be 10
        }

        It 'Lhs is $?' {
            function Get-TestCase
            {
                [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSAvoidAssignmentToAutomaticVariable', '')]
                param()
                { $???=$false}
            }
            Get-TestCase
            $? | Should -BeTrue
        }
    }

    Context 'Null coalesce operator ??' {
        BeforeEach {
            $x = $null
        }

        It 'Variable does not exist' {
            Remove-Variable variableDoesNotExist -ErrorAction SilentlyContinue -Force
            $variableDoesNotExist ?? 100 | Should -Be 100
        }

        It 'Variable exists but is null' {
            $x ?? 100 | Should -Be 100
        }

        It 'Lhs is not null' {
            $x = 100
            $x ?? 200 | Should -Be 100
        }

        It 'Lhs is a non-null constant' {
            1 ?? 2 | Should -Be 1
        }

        It 'Lhs is `$null' {
            $null ?? 'string value' | Should -BeExactly 'string value'
        }

        It 'Check precedence of ?? expression resolution' {
            $x ?? $null ?? 100 | Should -Be 100
            $null ?? $null ?? 100 | Should -Be 100
            $null ?? $null ?? $null | Should -Be $null
            $x ?? 200 ?? $null | Should -Be 200
            $x ?? 200 ?? 300 | Should -Be 200
            100 ?? $x ?? 200 | Should -Be 100
            $null ?? 100 ?? $null ?? 200 | Should -Be 100
        }

        It 'Rhs is a cmdlet' {
            $result = $x ?? (Get-Alias -Name 'where')
            $result.Definition | Should -BeExactly 'Where-Object'
        }

        It 'Lhs is DBNull' {
            $x = [System.DBNull]::Value
            $x ?? 200 | Should -Be ([System.DBNull]::Value)
        }

        It 'Lhs is AutomationNull' {
            $x = [System.Management.Automation.Internal.AutomationNull]::Value
            $x ??  200 | Should -Be 200
        }

        It 'Lhs is NullString' {
            $x = [NullString]::Value
            $x ?? 200 | Should -Be ([NullString]::Value)
        }

        It 'Rhs is a get variable expression' {
            $x = $null
            $y = 2
            $x ?? $y | Should -Be 2
        }

        It 'Lhs is a constant' {
            [System.DBNull]::Value ?? 2 | Should -Be ([System.DBNull]::Value)
        }

        It 'Lhs is $?' {
            {$???$false} | Should -BeTrue
        }

        It 'Should only evaluate LHS once when it IS null' {
            $testState = [pscustomobject]@{ Value = 0 }
            (& { [void]$testState.Value++ }) ?? 'Nothing' | Should -BeExactly 'Nothing'
            $testState.Value | Should -Be 1
        }

        It 'Should only evaluate LHS once when it is NOT null' {
            $testState = [pscustomobject]@{ Value = 0 }
            (& { 'Test'; [void]$testState.Value++ }) ?? 'Nothing' | Should -BeExactly 'Test'
            $testState.Value | Should -Be 1
        }
    }

    Context 'Null Coalesce ?? operator precedence' {
        It '?? precedence over -and' {
            $true -and $null ?? $true | Should -BeTrue
        }

        It '?? precedence over -band' {
            1 -band $null ?? 1 | Should -Be 1
        }

        It '?? precedence over -eq' {
            'x' -eq $null ?? 'x' | Should -BeTrue
            $null -eq $null ?? 'x' | Should -BeFalse
        }

        It '?? precedence over -as' {
            'abc' -as [datetime] ?? 1 | Should -BeNullOrEmpty
        }

        It '?? precedence over -replace' {
            'x' -replace 'x',$null ?? 1 | Should -Be ([string]::empty)
        }

        It '+ precedence over ??' {
            2 + $null ?? 3 | Should -Be 2
        }

        It '* precedence over ??' {
            2 * $null ?? 3 | Should -Be 0
        }

        It '-f precedence over ??' {
            "{0}" -f $null ?? 'b' | Should -Be ([string]::empty)
        }

        It '.. precedence ove ??' {
            1..$null ?? 2 | Should -BeIn 1,0
        }
    }

    Context 'Combined usage of null conditional operators' {

        BeforeAll {
            function GetNull {
                return $null
            }

            function GetHello {
                return "Hello"
            }
        }

        BeforeEach {
            $x = $null
        }

        It '?? and ??= used together' {
            $x ??= 100 ?? 200
            $x | Should -Be 100
        }

        It '?? and ??= chaining' {
            $x ??= $x ?? (GetNull) ?? (GetHello)
            $x | Should -BeExactly 'Hello'
        }

        It 'First two are null' {
            $z ??= $null ?? 100
            $z | Should -Be 100
        }
    }
}

Describe 'NullConditionalMemberAccess' -Tag 'CI' {

    Context '?. operator tests' {
        BeforeAll {
            $psObj = [psobject]::new()
            $psObj | Add-Member -Name 'name' -Value 'value' -MemberType NoteProperty
            $psObj | Add-Member -Name 'nested' -Value @{name = 'valuenested'} -MemberType NoteProperty

            $psobj2 = [psobject]::new()
            $psobj2 | Add-Member -Name 'GetHello' -Value { "hello" } -MemberType ScriptMethod
            $psObj | Add-Member -Name 'nestedMethod' -Value $psobj2 -MemberType NoteProperty

            $array = 1..3
            $hash = @{ a = 1; b = 2}

            $null = New-Item -ItemType File -Path "$TestDrive/testfile.txt" -Force
        }

        It 'Can get member value of a non-null variable' {
            ${psObj}?.name | Should -BeExactly 'value'
            ${array}?.length | Should -Be 3
            ${hash}?.a | Should -Be 1

            (Get-Item $TestDrive)?.EnumerateFiles()?.Name | Should -BeExactly 'testfile.txt'

            [int32]::MaxValue?.ToString() | Should -BeExactly '2147483647'
        }

        It 'Can get null when variable is null' {
            ${nonExistent}?.name | Should -BeNullOrEmpty
            ${nonExistent}?.MyMethod() | Should -BeNullOrEmpty

            (get-process -Name doesnotexist -ErrorAction SilentlyContinue)?.Id | Should -BeNullOrEmpty
        }

        It 'Use ?. operator multiple times in statement' {
            ${psObj}?.name?.nonExistent | Should -BeNullOrEmpty
            ${psObj}?.nonExistent?.nonExistent | Should -BeNullOrEmpty
            ${nonExistent}?.nonExistent?.nonExistent | Should -BeNullOrEmpty

            ${psObj}?.nested?.name | Should -BeExactly 'valuenested'
            ${psObj}?.nestedMethod?.GetHello() | Should -BeExactly 'hello'
        }

        It 'Use ?. on a dynamic method name' {
            $methodName = 'ToLongDateString'
            (Get-Date '11/11/2019')?.$methodName() | Should -BeExactly 'Monday, November 11, 2019'

            ${doesNotExist}?.$methodName() | Should -BeNullOrEmpty
        }

        It 'Use ?. on a dynamic method name that does not exist' {
            $methodName = 'DoesNotExist'
            { (Get-Date '11/11/2019')?.$methodName() } | Should -Throw -ErrorId 'MethodNotFound'
        }

        It 'Use ?. on a dynamic method name that does not exist' {
            $methodName = $null
            { (Get-Date '11/11/2019')?.$methodName() } | Should -Throw -ErrorId 'MethodNotFound'
        }

        It 'Use ?. on a dynamic property name' {
            $propName = 'SI'
            (Get-Process -Id $PID)?.$propName | Should -Be (Get-Process -id $PID).SessionId

            ${doesNotExist}?.$propName() | Should -BeNullOrEmpty
        }

        It 'Should throw error when method does not exist' {
            { ${psObj}?.nestedMethod?.NonExistent() } | Should -Throw -ErrorId 'MethodNotFound'
        }
    }

    Context '?[] operator tests' {
        BeforeAll {
            $array = 1..3
            $hash = @{ a = 1; b = 2}

            $dateArray = @(
                (Get-Date '11/1/2019'),
                (Get-Date '11/2/2019'),
                (Get-Date '11/3/2019'))
        }

        It 'Can index can call properties' {
            ${array}?[0] | Should -Be 1
            ${array}?[0,1] | Should -Be @(1,2)
            ${array}?[0..2] | Should -Be @(1,2,3)
            ${array}?[-2] | Should -Be 2

            ${hash}?['a'] | Should -Be 1
        }

        It 'Indexing in null items should be null' {
            ${doesnotExist}?[0] | Should -BeNullOrEmpty
            ${doesnotExist}?[0,1] | Should -BeNullOrEmpty
            ${doesnotExist}?[0..2] | Should -BeNullOrEmpty
            ${doesnotExist}?[-2] | Should -BeNullOrEmpty

            ${doesnotExist}?['a'] | Should -BeNullOrEmpty
        }

        It 'Can call methods on indexed items' {
            ${dateArray}?[0]?.ToLongDateString() | Should -BeExactly 'Friday, November 1, 2019'
        }

        It 'Calling a method on nonexistent item give null' {
            ${dateArray}?[1234]?.ToLongDateString() | Should -BeNullOrEmpty
            ${doesNotExist}?[0]?.MyGetMethod() | Should -BeNullOrEmpty
        }
    }
}
