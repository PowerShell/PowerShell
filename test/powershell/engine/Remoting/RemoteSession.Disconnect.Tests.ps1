# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Import-Module HelpersCommon

Describe "WinRM based remoting session abrupt disconnect" -Tags 'Feature','RequireAdminOnWindows' {

    BeforeAll {
        $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
        $pendingTest = (Test-IsWinWow64)
        $skipTest = !$IsWindows

        if ($pendingTest)
        {
            $PSDefaultParameterValues["it:pending"] = $true
            return
        }
        elseif ($skipTest)
        {
            $PSDefaultParameterValues["it:skip"] = $true
            return
        }

        $disconnectScript = @'
            param (
                [int] $RunspaceId
            )

            $rs = Get-Runspace -Id $RunspaceId

            if (! $rs.RunspaceIsRemote)
            {
                throw "Runspace $RunspaceId is not a remote runspace."
            }

            # Wait up to one minute for Runspace to begin running script
            $count = 0
            while (($rs.RunspaceAvailability -ne "busy") -and (++$count -le 60)) {
                Start-Sleep 1
            }

            if ($rs.RunspaceAvailability -ne "busy")
            {
                throw "Runspace $RunspaceId is not running any script after one minute."
            }

            # Disconnect running runspace
            $rs.Disconnect()
'@

        $endPointName = "PowerShell.$(${PSVersionTable}.GitCommitId)"
        $endPoint = (Get-PSSessionConfiguration -Name $endPointName -ErrorAction SilentlyContinue).Name
        if ($endPoint -eq $null)
        {
            Enable-PSRemoting -SkipNetworkProfileCheck
            $endPoint = (Get-PSSessionConfiguration -Name $endPointName).Name
        }
        $session = New-RemoteSession -ConfigurationName $endPoint

        $ps = [powershell]::Create("NewRunspace")
        $ps.AddScript($disconnectScript).AddParameter("RunspaceId", $session.Runspace.Id)
    }

    AfterAll {
        $global:PSDefaultParameterValues = $originalDefaultParameterValues

        if ($pendingTest -or $skipTest)
        {
            return
        }

        if ($ps -ne $null) { $ps.Dispose() }
        if ($session -ne $null) { Remove-PSSession -Session $session }
        if ($script:job -ne $null) { Remove-Job -Job $script:job -Force }
    }

    It "Verifies that an abruptly disconnected Invoke-Command session produces a valid disconnected job needed for reconnect" {

        # Start disconnect script running.
        $ps.BeginInvoke()

        # Run script synchronously on remote session, and let disconnect script disconnect the remote session.
        $null = Invoke-Command -Session $session -ScriptBlock {
            1..60 | ForEach-Object { Start-Sleep 1; "Output $_" }
        } -ErrorAction SilentlyContinue

        # Session should be disconnected.
        $session.State | Should -BeLikeExactly 'Disconnect*'

        # A disconnected job should have been created for reconnect.
        $script:job = Get-Job | Where-Object { $_.ChildJobs[0].Runspace.Id -eq $session.Runspace.Id }
        $script:job | Should -Not -BeNullOrEmpty
        $script:job.State | Should -BeExactly 'Disconnected'
    }
}
