# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

$script:JobId = -1

function Start-PwshProcess
{
    $job = Start-Job -ScriptBlock { Write-Output $PID; Start-Sleep -Seconds 300 }
    $script:JobId = $job.Id
    $procId = -1
    $count = 0
    while (($procId -eq -1) -and ($count++ -lt 10))
    {
        Start-Sleep -Seconds 1
        $procId = Receive-Job -Job $job
        if ($null -eq $procId) {
            $procId = -1
        }
    }

    if ($procId -eq -1) {
        throw "Start-PwshProcess: Unable to start job process and obtain process Id."
    }

    return $procId
}

Describe 'NamedPipe Custom Remote Connection Tests' -Tags 'Feature','RequireAdminOnWindows' {

    BeforeEach {
        Import-Module -Name Microsoft.PowerShell.NamedPipeConnection -ErrorAction Stop

        $script:PwshProcId = Start-PwshProcess
        $script:session = $null
    }

    AfterEach {
        if ($null -ne $script:session)
        {
            Remove-PSSession -Session $script:session
        }

        Remove-Job -Id $script:JobId -Force -ErrorAction SilentlyContinue
    }

    It 'Verifies that New-NamedPipeSession succeeds in connecting to Pwsh process' {
        $script:session = New-NamedPipeSession -ProcessId $script:PwshProcId -ConnectingTimeout 10 -Name CustomNPConnection -ErrorAction Stop

        # Verify created PSSession
        $script:session.State | Should -BeExactly 'Opened'
        $script:session.Availability | Should -BeExactly 'Available'
        $script:session.Name | Should -BeExactly 'CustomNPConnection'
        $script:session.Transport | Should -BeExactly 'PSNPTest'
        $script:session.ComputerName | Should -BeExactly "LocalMachine:$($script:PwshProcId)"
    }

    # Skip this timeout test for non-Windows platforms, because dotNet named pipes do not honor the 'NumberOfServerInstances'
    # property and allows connection to a currently connected server.
    It 'Verifies timeout error when trying to connect to pwsh process with current connection' -Skip:(!$IsWindows) {
        # We start an active connection to have it block the second connection attempt.
        $script:session = New-NamedPipeSession -ProcessId $script:PwshProcId -ConnectingTimeout 10 -Name CustomNPConnection -ErrorAction Stop
        
        # The above connection means the named pipe server is busy and won't allow this second connection.
        $brokenSession = New-NamedPipeSession -ProcessId $script:PwshProcId -ConnectingTimeout 2 -Name CustomNPConnection -ErrorAction Stop

        # Verify expected broken session
        $brokenSession.State | Should -BeExactly 'Broken'
        $brokenSession.Availability | Should -BeExactly 'None'
        $brokenSession.Runspace.RunspaceStateInfo.Reason.InnerException.GetType().Name | Should -BeExactly 'TimeoutException'

        $brokenSession | Remove-PSSession
    }

    It 'Passes $using: with PSv5 compatibility in Invoke-Command' {
        $script:session = New-NamedPipeSession -ProcessId $script:PwshProcId -ConnectingTimeout 10 -Name CustomNPConnection -ErrorAction Stop

        Function Test-Function {
            'foo'
        }

        # The v2 engine will choke on a var with '-' in the name and the v3/v4
        # using logic will revert to the v2 branch if $using is in a new scope.
        # By using a function and a new scope we can verify the v5 logic is
        # used and not the v2-4 one.
        $result = Invoke-Command -Session $script:session -ScriptBlock {
            ${function:Test-Function} = ${using:function:Test-Function}

            Test-Function

            # Running in a new scope triggers the v2 logic if the v3/v4 branch
            # was used.
            & { (${using:function:Test-Function}).Trim() }
        }
        $result.Count | Should -Be 2
        $result[0] | Should -BeExactly foo
        $result[1] | Should -BeExactly "'foo'"
    }
}
