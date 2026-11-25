# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Runspace.RunspaceOpened event" -Tags "CI" {
    BeforeAll {
        Add-Type -TypeDefinition @'
using System;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

public static class RunspaceOpenedTestHandler
{
    public static void OnRunspaceOpened(object sender, RunspaceOpenedEventArgs e)
    {
        // Runs synchronously before any user code, so the new runspace sees this variable.
        using PowerShell ps = PowerShell.Create(e.Runspace);
        ps.AddCommand("Set-Variable")
            .AddParameter("Name", "RunspaceOpenedTest")
            .AddParameter("Value", $"set by handler in {e.Runspace.InstanceId}")
            .AddParameter("Scope", "Global")
            .Invoke();
    }
}
'@
    }

    It "Can be subscribed to with Register-ObjectEvent" {
        $sourceId = "RunspaceOpenedTest"
        $null = Register-ObjectEvent -InputObject ([runspace]) -EventName RunspaceOpened -SourceIdentifier $sourceId
        try {
            $parallelId = 1 | ForEach-Object -Parallel { [runspace]::DefaultRunspace.InstanceId }

            # Script subscribers are queued so wait for the event to be processed.
            $null = Wait-Event -SourceIdentifier $sourceId -Timeout 10
            $openedIds = Get-Event -SourceIdentifier $sourceId | ForEach-Object { $_.SourceEventArgs.Runspace.InstanceId }

            $openedIds | Should -Contain $parallelId
        }
        finally {
            Unregister-Event -SourceIdentifier $sourceId
            Remove-Event -SourceIdentifier $sourceId -ErrorAction SilentlyContinue
        }
    }

    It "Can run commands in the runspace from a handler before user code runs" {
        # A C# handler, not a scriptblock, as it runs on the thread opening the runspace which has no runspace to run script in.
        [runspace]::add_RunspaceOpened([RunspaceOpenedTestHandler]::OnRunspaceOpened)
        try {
            $result = 1 | ForEach-Object -Parallel {
                [PSCustomObject]@{
                    Id = [runspace]::DefaultRunspace.InstanceId
                    Value = $global:RunspaceOpenedTest
                }
            }
        }
        finally {
            [runspace]::remove_RunspaceOpened([RunspaceOpenedTestHandler]::OnRunspaceOpened)
        }

        $result.Value | Should -BeExactly "set by handler in $($result.Id)"
    }
}
