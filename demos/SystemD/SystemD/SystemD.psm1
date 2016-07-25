Function Get-SystemDJournal {
    [CmdletBinding()]
    param (
        [Alias("args")][string]$journalctlParameters
       )
        $cmd = "journalctl"
        $Result = & $cmd $journalctlParameters -o json --no-pager
        Try
        {
                  $JSONResult = $Result|ConvertFrom-JSON
                  $JSONResult
        }
        Catch
        {
                $Result
        }
}