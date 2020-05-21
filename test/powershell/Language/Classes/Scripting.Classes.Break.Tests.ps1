# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe 'Break statements with classes' -Tags "CI" {

    function Get-Errors([string]$sourceCode) {
        $tokens = $null
        $errors = $null
        $ast = [System.Management.Automation.Language.Parser]::ParseInput($sourceCode, [ref] $tokens, [ref] $errors)
        return $errors
    }

    Context 'break is inside a class method' {
        It 'reports parse error for break on non-existing label' {
            $errors = Get-Errors @'
class A
{
    static [int] foo()
    {
        while (1) { break some_label }
        return 1
    }
}
'@
            $errors.Count | Should -Be 1
            $errors[0].ErrorId | Should -BeExactly 'LabelNotFound'
        }

        It 'reports parse error for break outside of loop' {
            $errors = Get-Errors @'
class A
{
    static [int] foo()
    {
        break some_label
        return 1
    }
}
'@
            $errors.Count | Should -Be 1
            $errors[0].ErrorId | Should -BeExactly 'LabelNotFound'
        }

        It 'work fine, when break is legit' {
            class C
            {
                static [int] foo()
                {
                    foreach ($i in 101..102) {
                        break
                    }
                    return $i
                }
            }
            [C]::foo() | Should -Be 101
        }
    }

    Context 'continue inside a class method' {
        It 'reports parse error for continue on non-existing label' {
            $errors = Get-Errors @'
class A
{
    static [int] foo()
    {
        while (1) { continue some_label }
        return 1
    }
}
'@
            $errors.Count | Should -Be 1
            $errors[0].ErrorId | Should -BeExactly 'LabelNotFound'
        }
    }

    Context 'break is in called function'  {
        It 'doesn''t terminate caller method' -Skip {

            function ImBreak() {
                break
            }

            class C
            {
                static [int] getInt()
                {
                    ImBreak
                    return 123
                }
            }

            $canary = $false
            try {
                [C]::getInt() | Should -Be 123
                $canary = $true
            } finally {
                $canary | Should -BeTrue
            }
        }

        It 'doesn''t allow goto outside of function with break' -Skip {

            function ImBreak() {
                break label1
            }

            class C
            {
                static [int] getInt()
                {
                    $count = 123
                    :label1
                    foreach ($i in 0..3) {
                        foreach ($i in 0..3) {
                            ImBreak
                            $count++
                        }
                    }
                    return $count
                }
            }

            $canary = $false
            try {
                [C]::getInt() | Should -Be (123 + 4*4)
                $canary = $true
            } finally {
                $canary | Should -BeTrue
            }
        }
    }

    Context 'no classes involved' {

         It 'doesn''t report parse error for non-existing label' {
            $errors = Get-Errors @'
function foo()
{
    while (1) { break some_label }
    while (1) { continue another_label }
    return 1
}
'@
            $errors.Count | Should -Be 0
        }

    }
}
