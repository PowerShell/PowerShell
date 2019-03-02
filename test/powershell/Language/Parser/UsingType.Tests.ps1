# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

using namespace System.Threading
using type TimerTimer = System.Timers.Timer
using type vexillum = System.FlagsAttribute
# Test parsing more than one using statement on one line
using type proc = System.Diagnostics.Process; using type psi = System.Diagnostics.ProcessStartInfo
using namespace System.Collections.Generic

Describe "Using Type" -Tags "CI" {
    It "Type literals w/ using namespace and using type with no ambiguity" {
        [Timer].FullName | Should -Be System.Threading.Timer
        [TimerTimer].FullName | Should -Be System.Timers.Timer
    }

    It "Type aliases are available in Type Definitions" {
        class C1
        {
            [Timer]$t1
            [TimerTimer]$t2
        }

        [C1].GetProperty("t1").PropertyType.FullName | Should -Be System.Threading.Timer
        [C1].GetProperty("t2").PropertyType.FullName | Should -Be System.Timers.Timer
    }

    It "Covert string to Type w/ using namespace and using type with no ambiguity" {
        ("Timer" -as [Type]).FullName | Should -Be System.Threading.Thread
        ("TimerTimer" -as [Type]).FullName | Should -Be System.Timers.Timer

        (New-Object "Timer").GetType().FullName | Should -Be System.Threading.Timer
        (New-Object "TimerTimer").GetType().FullName | Should -Be System.Timers.Timer
    }

    It "Attributes w/ using type" {
        # vexillum is System.FlagsAttribute
        # This tests our 'using type vexillum = ...' statement applies to attribute literals
        [vexillum()]
        enum E1
        {
            E1 = 0x01
            E2 = 0x02
            E4 = 0x04
        }

        [E1].GetCustomAttributesData()[0].AttributeType.FullName | Should -Be System.FlagsAttribute

        $e1 = [E1]'V1,V2'
        $e1.value__ |Should -Be 0x3
    }

    It "Parameters" {
        function foo([psi]$psi = $null) { return $psi }

        foo | Should -BeNullOrEmpty

        $params = @{
            psi = [psi]::new('fakeprogram')
        }
        (foo @params).FileName | Should -Be fakeprogram
        $mod = New-Module -Name UsingTypeModule -ScriptBlock {
            function Test-ProcIsProcess()
            {
                @((Get-Command Get-Process).Output.Type) -contains [proc]
            }
        }
        Import-Module $mod
        Test-ProcIsProcess | Should -BeTrue
        Remove-Module $mod
    }

    #TODO: test aliases for generic types
}
