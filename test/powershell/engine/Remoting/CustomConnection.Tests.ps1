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

    BeforeAll {
        try {
            Import-Module -Name Microsoft.PowerShell.NamedPipeConnection -ErrorAction Stop
        }
        catch {
            Get-Error $_
        }

        $script:PwshProcId = Start-PwshProcess
        $script:session = $null
    }

    AfterAll {
        if ($null -ne $script:session)
        {
            Remove-PSSession -Session $script:session
        }

        Remove-Job -Id $script:JobId -Force -ErrorAction SilentlyContinue
    }

    It 'Verifies that New-NamedPipeSession succeeds in connectiong to Pwsh process' {
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
        $brokenSession = New-NamedPipeSession -ProcessId $script:PwshProcId -ConnectingTimeout 2 -Name CustomNPConnection -ErrorAction Stop

        # Verify expected broken session
        $brokenSession.State | Should -BeExactly 'Broken'
        $brokenSession.Availability | Should -BeExactly 'None'
        $brokenSession.Runspace.RunspaceStateInfo.Reason.InnerException.GetType().Name | Should -BeExactly 'TimeoutException'

        $brokenSession | Remove-PSSession
    }
}
