# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Function Get-SystemDJournal {
    [CmdletBinding()]
    param (
        [Alias("args")][string]$journalctlParameters
       )
        $sudocmd = "sudo"
        $cmd = "journalctl"
        $Result = & $sudocmd $cmd $journalctlParameters -o json --no-pager
        Try
        {
                  $JSONResult = $Result|ConvertFrom-Json
                  $JSONResult
        }
        Catch
        {
                $Result
        }
}
