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
                  $JSONResult = $Result|ConvertFrom-JSON
                  $JSONResult
        }
        Catch
        {
                $Result
        }
}