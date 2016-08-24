function Wait-CompleteExecution
{
    [CmdletBinding()]
    param ( 
        [ScriptBlock]$sb,
        [int]$TimeoutInMilliseconds = 10000,
        [int]$IntervalInMilliseconds = 1000
        )
    # Get the current time
    $startTime = [DateTime]::Now

    # Loop until the script block evaluates to true
    while (-not ($sb.Invoke())) {
        # Sleep for the specified interval
        start-sleep -mil $intervalInMilliseconds

        # If the timeout period has passed, throw an exception
        if (([DateTime]::Now - $startTime).TotalMilliseconds -gt $timeoutInMilliseconds)
        {
            return $false
        }
    }
    return $true
}
export-modulemember -function Wait-CompleteExecution
