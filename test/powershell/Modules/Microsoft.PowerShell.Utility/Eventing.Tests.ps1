# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe "Event Subscriber Tests" -Tags "Feature" {
    BeforeEach {
        Get-EventSubscriber | Unregister-Event
    }
    AfterEach {
        Get-EventSubscriber | Unregister-Event
    }

    # can't let this case to work
    It "Register an event with no action, trigger it and wait for it to be raised." -Pending:$true{
        Get-EventSubscriber | Should -BeNullOrEmpty
        $messageData = new-object psobject
        $job = Start-Job { Start-Sleep -Seconds 5; 1..5 }
        $null = Register-ObjectEvent $job -EventName StateChanged -SourceIdentifier EventSIDTest -Action {} -MessageData $messageData
        new-event EventSIDTest

        wait-event EventSIDTest
        $eventdata = get-event EventSIDTest
        $eventdata.MessageData | Should -Be $messageData
        remove-event EventSIDTest
        Unregister-Event EventSIDTest
        Get-EventSubscriber | Should -BeNullOrEmpty
    }

    It "Access a global variable from an event action." {
        Get-EventSubscriber | Should -BeNullOrEmpty
        set-variable incomingGlobal -scope global -value globVarValue
        $null = register-engineevent -SourceIdentifier foo -Action {set-variable -scope global -name aglobalvariable -value $incomingGlobal}
        new-event foo
        $getvar = get-variable aglobalvariable -scope global
        $getvar.Name | Should -Be aglobalvariable
        $getvar.Value | Should -Be globVarValue
        Unregister-Event foo
        Get-EventSubscriber | Should -BeNullOrEmpty
    }

    It 'Should not throw when having finally block in Powershell.Exiting Action scriptblock' {
        $pwsh = "$PSHOME/pwsh"
        $output = & $pwsh {
        Register-EngineEvent -SourceIdentifier Powershell.Exiting -Action {
            try{
                try{} finally{}
            }
            catch{ Write-Host "Exception" -Nonewline }
        }
        } | Out-String

        $output | Should -Not -BeLike "*Exception*"
    }
}
