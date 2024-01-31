# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# There is an automatic 'using namespace system' which is
# tested by this test, so don't uncomment the following:
#using namespace System
using namespace System.Threading
using namespace System.Timers
using namespace System.Diagnostics
# Test parsing more than one using statement on one line
using namespace System.Diagnostics; using namespace System.Runtime.CompilerServices
using namespace System.Collections.Generic

# Flags is System.FlagsAttribute
# This tests our implicit 'using namespace System'
# despite having other explicit using namespace statements.
[Flags()]
enum E1
{
    E1 = 0x01
    E2 = 0x02
    E4 = 0x04
}

# Test attributes that won't be found w/o using, but w/o implicit Attribute suffix
[CompilerGenerated()]
class C1
{
    [Thread][CompilerGenerated()]$Thread
    [Int32][CompilerGenerated()]$Int
    #[ElapsedEventHandler][CompilerGenerated()]$EventHandler
}

# Test attributes that won't be found w/o using, but w/ implicit Attribute suffix
[CompilerGeneratedAttribute()]
class C2
{
    [Thread][CompilerGeneratedAttribute()]$Thread
    [Int32][CompilerGeneratedAttribute()]$Int
    #[ElapsedEventHandler][CompilerGeneratedAttribute()]$EventHandler
}

Describe "Using Namespace" -Tags "CI" {
    It "Type literals w/ using namespace" {
        [Thread].FullName | Should -Be System.Threading.Thread
        [Int32].FullName | Should -Be System.Int32
        #[ElapsedEventHandler].FullName | Should -Be System.Timers.ElapsedEventHandler

        [C1].GetProperty("Thread").PropertyType.FullName | Should -Be System.Threading.Thread
        [C1].GetProperty("Int").PropertyType.FullName | Should -Be System.Int32
        # [C1].GetProperty("EventHandler").PropertyType.FullName | Should -Be System.Timers.ElapsedEventHandler
    }

    It "Covert string to Type w/ using namespace" {
        ("Thread" -as [Type]).FullName | Should -Be System.Threading.Thread
        ("Int32" -as [Type]).FullName | Should -Be System.Int32
        # ("ElapsedEventHandler" -as [Type]).FullName | Should -Be System.Timers.ElapsedEventHandler

        New-Object Int32 | Should -Be 0
        New-Object CompilerGeneratedAttribute | Should -Be System.Runtime.CompilerServices.CompilerGeneratedAttribute
    }

    It "Attributes w/ using namespace" -Pending {
        function foo
        {
            [DebuggerStepThrough()]
            param(
                [CompilerGeneratedAttribute()]
                $a,

                [CompilerGenerated()]
                $b
            )

            "OK"
        }

        foo | Should -Be OK
        $cmdInfo = Get-Commmand foo
        $cmdInfo.ScriptBlock.Attributes[0] | Should -Be System.Diagnostics.DebuggerStepThroughAttribute
        $cmdInfo.Parameters['a'].Attributes[1] | Should -Be System.Runtime.CompilerServices.CompilerGeneratedAttribute
        $cmdInfo.Parameters['b'].Attributes[1] | Should -Be System.Runtime.CompilerServices.CompilerGeneratedAttribute

        [C1].GetProperty("Thread").GetCustomAttributesData()[0].AttributeType.FullName | Should -Be System.Runtime.CompilerServices.CompilerGeneratedAttribute
        [C1].GetProperty("Int").GetCustomAttributesData()[0].AttributeType.FullName | Should -Be System.Runtime.CompilerServices.CompilerGeneratedAttribute
        # [C1].GetProperty("EventHandler").GetCustomAttributesData()[0].AttributeType.FullName | Should -Be System.Runtime.CompilerServices.CompilerGeneratedAttribute

        [C2].GetProperty("Thread").GetCustomAttributesData()[0].AttributeType.FullName | Should -Be System.Runtime.CompilerServices.CompilerGeneratedAttribute
        [C2].GetProperty("Int").GetCustomAttributesData()[0].AttributeType.FullName | Should -Be System.Runtime.CompilerServices.CompilerGeneratedAttribute
        # [C2].GetProperty("EventHandler").GetCustomAttributesData()[0].AttributeType.FullName | Should -Be System.Runtime.CompilerServices.CompilerGeneratedAttribute

        [C1].GetCustomAttributesData()[0].AttributeType.FullName | Should -Be System.Runtime.CompilerServices.CompilerGeneratedAttribute
        [C2].GetCustomAttributesData()[0].AttributeType.FullName | Should -Be System.Runtime.CompilerServices.CompilerGeneratedAttribute

        [E1].GetCustomAttributesData()[0].AttributeType.FullName | Should -Be System.FlagsAttribute
    }

    It "Ambiguous type reference" {
        { [ThreadState] } | Should -Throw -ErrorId AmbiguousTypeReference
    }

    It "Parameters" {
        function foo([Thread]$t = $null) { 42 }

        foo | Should -Be 42

        $mod = New-Module -Name UsingNamespaceModule -ScriptBlock {
            function Set-Thread([Thread]$t = $null)
            {
                44
            }
        }
        Import-Module $mod
        Set-Thread | Should -Be 44
        Remove-Module $mod
    }

    It "Generics" {
        function foo([List[string]]$l)
        {
            $l | Should -Be "a string"
        }

        $l = [List[string]]::new()
        $l.Add("a string")
        foo $l
    }

    ShouldBeParseError "1; using namespace System" UsingMustBeAtStartOfScript 3
    ShouldBeParseError "using namespace Foo = System" UsingStatementNotSupported 0
    ShouldBeParseError "using namespace ''" InvalidNamespaceValue 16
    ShouldBeParseError "using namespace [System]" InvalidNamespaceValue 16
    ShouldBeParseError "using namespace ',System'" InvalidNamespaceValue 16
    ShouldBeParseError "using namespace ]System" InvalidNamespaceValue 16
    # TODO: add diagnostic (low pri)
    # ShouldBeParseError "using namespace System; using namespace System" UsingNamespaceAlreadySpecified 24
}
