Describe "Event Subscriber Tests" -tags "CI" {
    BeforeEach {
        Get-EventSubscriber | Unregister-Event
    }
    AfterEach {
        Get-EventSubscriber | Unregister-Event
    }

    # can't let this case to work
    It "Register an event with no action, trigger it and wait for it to be raised." -Pending:$true{
        Get-EventSubscriber | should BeNullOrEmpty
        $messageData = new-object psobject
        $job = Start-Job { Start-Sleep -Seconds 5; 1..5 }
        $eventtest = Register-ObjectEvent $job -EventName StateChanged -SourceIdentifier EventSIDTest -Action {} -MessageData $messageData
        new-event EventSIDTest
        
        wait-event EventSIDTest
        $eventdata = get-event EventSIDTest
        $eventdata.MessageData | should Be $messageData
        remove-event EventSIDTest
        Unregister-Event EventSIDTest
        Get-EventSubscriber | should BeNullOrEmpty
    }
    
    It "Access a global variable from an event action." {
        Get-EventSubscriber | should BeNullOrEmpty
        set-variable incomingGlobal -scope global -value globVarValue
        $eventtest = register-engineevent -SourceIdentifier foo -Action {set-variable -scope global -name aglobalvariable -value $incomingGlobal}
        new-event foo
        $getvar = get-variable aglobalvariable -scope global
        $getvar.Name | should Be aglobalvariable
        $getvar.Value | should Be globVarValue
        Unregister-Event foo
        Get-EventSubscriber | should BeNullOrEmpty
    }
}
