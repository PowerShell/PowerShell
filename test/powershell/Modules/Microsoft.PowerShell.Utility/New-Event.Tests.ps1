# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "New-Event" -Tags "CI" {

    Context "Check return type of New-Event" {

        It "Should return PSEventArgs as return type of New-Event" {
            New-Event -SourceIdentifier a | Should -BeOfType System.Management.Automation.PSEventArgs
        }
    }

    Context "Check New-Event can register an event"{
	It "Should return PesterTestMessage as the MessageData" {
	    (New-Event -SourceIdentifier PesterTimer -Sender Windows.timer -MessageData "PesterTestMessage")
	    (Get-Event -SourceIdentifier PesterTimer).MessageData  | Should -BeExactly "PesterTestMessage"
	    Remove-Event -SourceIdentifier PesterTimer
	}

	It "Should return Sender as Windows.timer" {
	    (New-Event -SourceIdentifier PesterTimer -Sender Windows.timer -MessageData "PesterTestMessage")
	    (Get-Event -SourceIdentifier PesterTimer).Sender  | Should -Be Windows.timer
	    Remove-Event -SourceIdentifier PesterTimer
	}
    }
}

Describe "PSEventManager.AddForwardedEvent" -Tags "CI" {
    BeforeAll {
        $sourceId = "PesterForwardedEvent"
    }

    AfterEach {
        Remove-Event -SourceIdentifier $sourceId -ErrorAction SilentlyContinue
    }

    It "Forwards an event from another runspace and preserves the original event" {
        $rs = [runspacefactory]::CreateRunspace()
        try {
            $rs.Open()
            $eventSender = [pscustomobject]@{ Name = 'sender' }
            $original = $rs.Events.GenerateEvent($sourceId, $eventSender, @('arg1'), [psobject]'message')
            $originalId = $original.EventIdentifier

            $actual = $ExecutionContext.Events.AddForwardedEvent($original)

            $actual | Should -BeOfType System.Management.Automation.PSEventArgs
            [object]::ReferenceEquals($actual, $original) | Should -BeFalse
            $actual.SourceIdentifier | Should -BeExactly $sourceId
            $actual.Sender | Should -Be $eventSender
            $actual.SourceArgs | Should -Be @('arg1')
            $actual.MessageData | Should -BeExactly 'message'
            $actual.RunspaceId | Should -Be $rs.InstanceId
            $actual.TimeGenerated | Should -Be $original.TimeGenerated

            $original.EventIdentifier | Should -Be $originalId

            $received = Wait-Event -SourceIdentifier $sourceId -Timeout 5
            $received | Should -Not -BeNullOrEmpty
            $received.EventIdentifier | Should -Be $actual.EventIdentifier
            $received.RunspaceId | Should -Be $rs.InstanceId
        }
        finally {
            $rs.Dispose()
        }
    }

    It "Triggers a subscriber action for a forwarded event" {
        $rs = [runspacefactory]::CreateRunspace()
        try {
            $rs.Open()
            $job = Register-EngineEvent -SourceIdentifier $sourceId -Action { $Event.RunspaceId }
            $original = $rs.Events.GenerateEvent($sourceId, $null, $null, $null)

            $null = $ExecutionContext.Events.AddForwardedEvent($original)

            $timeout = [DateTime]::Now.AddSeconds(5)
            while (-not $job.HasMoreData -and [DateTime]::Now -lt $timeout) {
                Start-Sleep -Milliseconds 100
            }
            $job | Receive-Job | Should -Be $rs.InstanceId
        }
        finally {
            Unregister-Event -SourceIdentifier $sourceId -ErrorAction SilentlyContinue
            $rs.Dispose()
        }
    }

    It "Forwards an event received from an out of process runspace" {
        $rs = [runspacefactory]::CreateOutOfProcessRunspace($null)
        try {
            $rs.Open()
            $ps = [powershell]::Create()
            $ps.Runspace = $rs
            $null = $ps.AddScript(@"
`$null = Register-EngineEvent -SourceIdentifier $sourceId -Forward
`$null = New-Event -SourceIdentifier $sourceId -MessageData 'remote message'
"@).Invoke()
            $ps.Dispose()

            $timeout = [DateTime]::Now.AddSeconds(10)
            while ($rs.Events.ReceivedEvents.Count -eq 0 -and [DateTime]::Now -lt $timeout) {
                Start-Sleep -Milliseconds 100
            }
            $rs.Events.ReceivedEvents.Count | Should -Be 1
            $remoteEvent = $rs.Events.ReceivedEvents[0]

            # The first hop stamps the event with the client side runspace and computer name.
            $remoteEvent.RunspaceId | Should -Be $rs.InstanceId
            $remoteEvent.ComputerName | Should -BeExactly 'localhost'

            $null = $ExecutionContext.Events.AddForwardedEvent($remoteEvent)

            $received = Wait-Event -SourceIdentifier $sourceId -Timeout 5
            $received.MessageData | Should -BeExactly 'remote message'
            $received.ComputerName | Should -BeExactly 'localhost'
            $received.RunspaceId | Should -Be $rs.InstanceId
            $received.TimeGenerated | Should -Be $remoteEvent.TimeGenerated
        }
        finally {
            $rs.Dispose()
        }
    }

    It "Start-Job forwards a copy and leaves the job runspace event unchanged" {
        # Bump the local event id so it differs from the id assigned in the job runspace.
        $null = New-Event -SourceIdentifier "$sourceId-Local"
        Remove-Event -SourceIdentifier "$sourceId-Local"

        $job = Start-Job -ScriptBlock {
            $null = Register-EngineEvent -SourceIdentifier $using:sourceId -Forward
            $null = New-Event -SourceIdentifier $using:sourceId -MessageData 'job message'
            Start-Sleep -Seconds 2
        }
        try {
            $received = Wait-Event -SourceIdentifier $sourceId -Timeout 30
            $received.MessageData | Should -BeExactly 'job message'

            $jobEvents = $job.ChildJobs[0].Runspace.Events.ReceivedEvents
            $jobEvents.Count | Should -Be 1
            $jobEvent = $jobEvents[0]
            [object]::ReferenceEquals($received, $jobEvent) | Should -BeFalse
            $jobEvent.EventIdentifier | Should -Be 1
            $received.EventIdentifier | Should -Not -Be $jobEvent.EventIdentifier
            $received.ComputerName | Should -BeExactly $jobEvent.ComputerName
            $received.RunspaceId | Should -Be $jobEvent.RunspaceId
            $received.TimeGenerated | Should -Be $jobEvent.TimeGenerated
        }
        finally {
            $job | Remove-Job -Force
        }
    }

    It "Throws when the event is null" {
        { $ExecutionContext.Events.AddForwardedEvent($null) } | Should -Throw -ErrorId 'ArgumentNullException'
    }
}
