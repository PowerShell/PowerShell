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

function Test-IsElevated
{
    $IsElevated = $False
    if ( $IsWindows ) {
        # on Windows we can determine whether we're executing in an
        # elevated context
        $identity = [System.Security.Principal.WindowsIdentity]::GetCurrent()
        $windowsPrincipal = new-object 'Security.Principal.WindowsPrincipal' $identity
        if ($windowsPrincipal.IsInRole("Administrators") -eq 1) 
        { 
            $IsElevated = $true 
        } 
    }
    else {
        # on Linux, tests run via sudo will generally report "root" for whoami
        if ( (whoami) -match "root" ) {
            $IsElevated = $true
        }
    }
    return $IsElevated
}

export-modulemember -function Wait-CompleteExecution,Test-IsElevated

