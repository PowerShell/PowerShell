# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
using namespace system.collections.generic
using namespace System.Management.Automation
Describe "Generics support" -Tags "CI" {
    # list and stack are in different assemblies, and dictionary
    # takes more than one type parameter.

    It 'Type list[Int] works properly' {
        $x = [list[int]]::New()
        $x.Add(42)
        $x.Add(40)
        $x.count | Should -Be 2
    }

    It 'Type stack[Int] works properly' {
        $x = [stack[int]]::New()
        $x.Push(42)
        $x.Push(40)
        $x.count | Should -Be 2
    }

    It 'Type dictionary[string, Int] works properly' {
        $x = [dictionary[string, int]]::New()
        $x.foo = 42
        $x.foo | Should -Be 42
    }

    It 'Type list[[Int]] works properly' {
        $x = [list[[int]]]::New()
        $x.Add(42)
        $x.Add(40)
        $x.count | Should -Be 2
    }

    It 'Type stack[[Int]] works properly' {
        $x = [stack[[int]]]::New()
        $x.Push(42)
        $x.Push(40)
        $x.count | Should -Be 2
    }

    It 'Type dictionary[[string], [Int]] works properly' {
        $x = [dictionary[[string], [int]]]::New()
        $x.foo = 42
        $x.foo | Should -Be 42
    }

    It 'Type dictionary[dictionary[list[int],string], stack[double]] works properly' {
        $x = [dictionary[dictionary[list[int],string], stack[double]]]::new()
        $x.gettype().fullname | Should -Match "double"

        $y = New-Object "dictionary[dictionary[list[int],string], stack[double]]"
        $y.gettype().fullname | Should -Match "double"
    }

    It 'non-generic EventHandler works properly' {
        # EventHandler has a generic and a non-generic.  This code caused an exception trying to
        # use the non-generic.
        $x = [System.EventHandler[PSInvocationStateChangedEventArgs]]

        # The error message for a generic that doesn't meet the constraints should mention which
        # argument failed.
        $e = { [nullable[object]] } | Should -Throw -ErrorId 'TypeNotFoundWithMessage' -PassThru
        $e | Should -Match "\[T\]"
    }

    It 'Array type works properly' -Skip:$IsCoreCLR{
        $x = [array]::ConvertAll.OverloadDefinitions
        $x | Should -Match "static\s+TOutput\[\]\s+ConvertAll\[TInput,\s+TOutput\]\("
    }

   It 'Class type works properly' {
       class TestClass {
            [string] $name = "default"
            [int] $port = 80
            [string] $scriptText = "1..6"

            TestClass([string] $name1, [int] $port1, [string] $scriptText1)
            {
                $this.name = $name1
                $this.port = $port1
                $this.scriptText = $scriptText1
            }
        }

        $x = [TestClass]::New("default1", 90, "1...5")
        $x.scriptText = "1...4"
        $x.name | Should -BeExactly 'default1'
        $x.port | Should -Be 90
        $x.scriptText | Should -BeExactly "1...4"
   }
}

