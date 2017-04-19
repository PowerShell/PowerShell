function Wait-UntilTrue
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
#This function follows the pester naming convention
function ShouldBeErrorId
{
    param([Parameter(ValueFromPipeline, Mandatory)]
        [ScriptBlock]
        $sb,

        [Parameter(Mandatory, Position=0)]
        [string]
        $FullyQualifiedErrorId)

        try
        {
            & $sb | Out-Null
            Throw "No Exception!"
        }
        catch
        {
            $_.FullyQualifiedErrorId | Should Be $FullyQualifiedErrorId | Out-Null
            # Write the exception to output that allow us to check later other properies of the exception
            Write-Output $_
        }
}

