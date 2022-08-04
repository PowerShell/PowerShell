# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

function Start-PwshProcess
{
    param (
        [string] $FilePath = $null
    )

    if ($FilePath -eq $null -or $FilePath -eq "") {
        $FilePath = Join-Path -Path $PSHOME -ChildPath 'pwsh'
    }

    $proc = Start-Process -FilePath $FilePath -WindowStyle Hidden -PassThru -ErrorAction Stop

    return $proc.Id
}

Describe 'NamedPipe Custom Remote Connection Tests' -Tags 'Feature','RequireAdminOnWindows' {

    BeforeAll {
        if (!$IsWindows) {
            return
        }
        Import-Module -Name Microsoft.PowerShell.NamedPipeConnection -ErrorAction Stop

        $script:PwshProcId = Start-PwshProcess
        $script:session = $null
    }

    AfterAll {
        if (!$IsWindows) {
            return
        }
        if ($null -ne $script:session)
        {
            Remove-PSSession -Session $script:session
        }

        if ($script:PwshProcId -gt 0)
        {
            Stop-Process -Id $script:PwshProcId
        }
    }

    It 'Verifies that New-NamedPipeSession succeeds in connectiong to Pwsh process' -Skip:(!$IsWindows) {
        $script:session = New-NamedPipeSession -ProcessId $script:PwshProcId -ConnectingTimeout 10 -Name CustomNPConnection -ErrorAction Stop

        # Verify created PSSession
        $script:session.State | Should -BeExactly 'Opened'
        $script:session.Availability | Should -BeExactly 'Available'
        $script:session.Name | Should -BeExactly 'CustomNPConnection'
        $script:session.Transport | Should -BeExactly 'PSNPTest'
        $script:session.ComputerName | Should -BeExactly "LocalMachine:$($script:PwshProcId)"
    }

    It 'Verifies timeout error when trying to connect to pwsh process with current connection' -Skip:(!$IsWindows) {
        $brokenSession = New-NamedPipeSession -ProcessId $script:PwshProcId -ConnectingTimeout 2 -Name CustomNPConnection -ErrorAction Stop

        # Verify expected broken session
        $brokenSession.State | Should -BeExactly 'Broken'
        $brokenSession.Availability | Should -BeExactly 'None'
        $brokenSession.Runspace.RunspaceStateInfo.Reason.InnerException.GetType().Name | Should -BeExactly 'TimeoutException'

        $brokenSession | Remove-PSSession
    }
}
