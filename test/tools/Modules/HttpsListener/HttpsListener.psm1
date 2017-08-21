Class HttpsListener 
{
    [int]$Port
    [System.Management.Automation.Job]$Job
    
    HttpsListener () { }

    [string] ToString() {
        return ('Port: {0}; Status: {1}' -f $This.Port, $This.JobStateInfo.State)
    }
}

$HttpsListeners = @{}

function Get-HttpsListener 
{
    [CmdletBinding(ConfirmImpact = 'Low')]
    [OutputType([HttpsListener])]
    param 
    (
        [ValidateRange(1,65535)]
        [int[]]$Port
    )

    process 
    {
        if ($Port)
        {
            return $Script:HttpsListeners[$port]
        }
        return [HttpsListener[]]$Script:HttpsListeners.Values
    }
}

function Start-HttpsListener 
{
    [CmdletBinding(ConfirmImpact = 'Low')]
    [OutputType([HttpsListener])]
    param 
    (
        [ValidateRange(1,65535)]
        [int]$Port = 8083
    )
    
    process 
    {
        $runningListener = Get-HttpsListener -Port $Port
        if ($runningListener)
        {
            return $runningListener
        }

        $initTimeoutSeconds  = 15
        $appDll              = 'HttpsListener.dll'
        $serverPfx           = 'ServerCert.pfx'
        $serverPfxPassword   = 'password'
        $initCompleteMessage = 'Now listening on'
        
        
        $timeOut = (get-date).AddSeconds($initTimeoutSeconds)
        $Job = Start-Job {
            $path = Split-Path -parent (get-command HttpsListener).Path
            Push-Location $path
            dotnet $using:appDll $using:serverPfx $using:serverPfxPassword $using:Port
        }
        $httpsListener =  [HttpsListener]@{
            Port = $Port 
            Job = $Job
        }
        # Wait until the app is running or until the initTimeoutSeconds have been reached
        do
        {
            Start-Sleep -Milliseconds 100
            $initStatus = $Job.ChildJobs[0].Output | Out-String
            $isRunning = $initStatus -match $initCompleteMessage
        }
        while (-not $isRunning -and (get-date) -lt $timeOut)
    
        if (-not $isRunning) 
        {
            $Job | Stop-Job -PassThru | Receive-Job
            $Job | Remove-Job
            throw 'ClientCertificateCheck did not start before the timeout was reached.'
        }
        $Script:HttpsListeners.Add($Port,$httpsListener)
        return $httpsListener
    }
}

function Stop-HttpsListener 
{
    [CmdletBinding(ConfirmImpact = 'Low')]
    [OutputType([Void])]
    param 
    (
        [parameter(
            ParameterSetName = 'HttpsListener',
            ValueFromPipeline = $true,
            Mandatory = $true
        )]
        [HttpsListener[]]
        $InputObject,

        [parameter(
            ParameterSetName = 'Port',
            Mandatory = $true
        )]
        [ValidateRange(1,65535)]
        [int[]]
        $Port
    )
    
    process 
    {
        if ($PSCmdlet.ParameterSetName -eq 'Port') 
        {
            $InputObject = Get-HttpsListener -Port $Port
        }
        foreach ($listener in $InputObject)
        {
            $listener.job | Stop-Job -PassThru | Remove-Job
            $null = $Script:HttpsListeners.Remove($listener.Port)
            
        }
    }
}
