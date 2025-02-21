# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Event Subscriber Tests" -Tags "Feature" {

    BeforeEach {
        Get-EventSubscriber | Unregister-Event
    }

    AfterEach {
        Get-EventSubscriber | Unregister-Event
    }

    # can't let this case to work
    It "Register an event with no action, trigger it and wait for it to be raised." -Pending:$true {
        Get-EventSubscriber | Should -BeNullOrEmpty
        $messageData = New-Object psobject
        $job = Start-Job { Start-Sleep -Seconds 5; 1..5 }
        $null = Register-ObjectEvent $job -EventName StateChanged -SourceIdentifier EventSIDTest -Action {} -MessageData $messageData
        New-Event EventSIDTest

        Wait-Event EventSIDTest
        $eventdata = Get-Event EventSIDTest
        $eventdata.MessageData | Should -Be $messageData
        Remove-Event EventSIDTest
        Unregister-Event EventSIDTest
        Get-EventSubscriber | Should -BeNullOrEmpty
    }

    It "Access a global variable from an event action." {
        Get-EventSubscriber | Should -BeNullOrEmpty
        Set-Variable incomingGlobal -Scope global -Value globVarValue
        $null = Register-EngineEvent -SourceIdentifier foo -Action { Set-Variable -Scope global -Name aglobalvariable -Value $incomingGlobal }
        New-Event foo
        $getvar = Get-Variable aglobalvariable -Scope global
        $getvar.Name | Should -Be aglobalvariable
        $getvar.Value | Should -Be globVarValue
        Unregister-Event foo
        Get-EventSubscriber | Should -BeNullOrEmpty
    }

    It 'Should not throw when having finally block in Powershell.Exiting Action scriptblock' {
        $pwsh = "$PSHOME/pwsh"
        $output = & $pwsh {
            Register-EngineEvent -SourceIdentifier Powershell.Exiting -Action {
                try {
                    try {} finally {}
                } catch { Write-Host "Exception" -NoNewline }
            }
        } | Out-String

        $output | Should -Not -BeLike "*Exception*"
    }

    Context 'Event (De)Registration with += / -=' {

        BeforeAll {
            $EventScript = { $global:EventTestResult = 120 }

            Add-Type -TypeDefinition @'
using System;
public static class _A99_EventTestHelper_91C_ {
    public static event EventHandler TestEvent;
    public static void RaiseEvent() => TestEvent?.Invoke(null, null);
}
'@
        }

        BeforeEach {
            $global:EventTestResult = 0
        }

        AfterAll {
            Get-Variable -Name EventTestResult -Scope Global -ErrorAction Ignore | Remove-Variable
        }

        It 'Registers events with += for an event member' {
            $ps = [powershell]::Create()
            $ps.InvocationStateChanged += $EventScript

            $ps.AddScript('2 + 2').Invoke() > $null
            $global:EventTestResult | Should -Be 120
        }

        It 'Deregisters events with -= for an event member' {
            $ps = [powershell]::Create()
            $ps.InvocationStateChanged += $EventScript
            $ps.InvocationStateChanged -= $EventScript

            $ps.AddScript('2 + 2').Invoke() > $null
            $global:EventTestResult | Should -Be 0
        }

        It 'Correctly registers a static event with +=' {
            [_A99_EventTestHelper_91C_]::TestEvent += $EventScript
            [_A99_EventTestHelper_91C_]::RaiseEvent()
            $global:EventTestResult | Should -Be 120
        }

        It 'Correctly unregisters a static event with -=' {
            # Original event should still be registered from above
            [_A99_EventTestHelper_91C_]::TestEvent -= $EventScript
            [_A99_EventTestHelper_91C_]::RaiseEvent()
            $global:EventTestResult | Should -Be 0
        }
    }
}
