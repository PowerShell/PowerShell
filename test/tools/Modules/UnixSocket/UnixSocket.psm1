# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Class UnixSocket
{
    [System.Management.Automation.Job]$Job

    UnixSocket () { }

    [String] GetStatus()
    {
        return $this.Job.JobStateInfo.State
    }
}

[UnixSocket]$UnixSocket

function Get-UnixSocket
{
    [CmdletBinding(ConfirmImpact = 'Low')]
    [OutputType([UnixSocket])]
    param()

    process
    {
        return [UnixSocket]$Script:UnixSocket
    }
}

function Start-UnixSocket
{
    [CmdletBinding(ConfirmImpact = 'Low')]
    [OutputType([UnixSocket])]
    param()

    process
    {
        $runningListener = Get-UnixSocket
        if ($null -ne $runningListener -and $runningListener.GetStatus() -eq 'Running')
        {
            return $runningListener
        }

        $initTimeoutSeconds  = 25
        $appExe              = (Get-Command UnixSocket).Path
        $initCompleteMessage = 'Now listening on'
        $sleepMilliseconds   = 100

        $Job = Start-Job {
            $path = Split-Path -Parent (Get-Command UnixSocket).Path -Verbose
            Push-Location $path -Verbose
            'appEXE: {0}' -f $using:appExe
            $env:ASPNETCORE_ENVIRONMENT = 'Development'
            & $using:appExe
        }

        $Script:UnixSocket = [UnixSocket]@{
            Job   = $Job
        }

        # Count iterations of $sleepMilliseconds instead of using system time to work around possible CI VM sleep/delays
        $sleepCountRemaining = $initTimeoutSeconds * 1000 / $sleepMilliseconds
        do
        {
            Start-Sleep -Milliseconds $sleepMilliseconds
            $initStatus = $Job.ChildJobs[0].Output | Out-String
            $isRunning = $initStatus -match $initCompleteMessage
            $sleepCountRemaining--
        }
        while (-not $isRunning -and $sleepCountRemaining -gt 0)

        if (-not $isRunning)
        {
            $jobErrors = $Job.ChildJobs[0].Error | Out-String
            $jobOutput =  $Job.ChildJobs[0].Output | Out-String
            $jobVerbose =  $Job.ChildJobs[0].Verbose | Out-String
            $Job | Stop-Job
            $Job | Remove-Job -Force
            $message = 'UnixSocket did not start before the timeout was reached.{0}Errors:{0}{1}{0}Output:{0}{2}{0}Verbose:{0}{3}' -f ([System.Environment]::NewLine), $jobErrors, $jobOutput, $jobVerbose
            throw $message
        }
        return $Script:UnixSocket
    }
}

function Stop-UnixSocket
{
    [CmdletBinding(ConfirmImpact = 'Low')]
    [OutputType([Void])]
    param()

    process
    {
        $Script:UnixSocket.Job | Stop-Job -PassThru | Remove-Job
        $Script:UnixSocket = $null
    }
}

function Get-UnixSocketName {
    [CmdletBinding()]
    [OutputType([string])]
    param ()

    process {
        $runningListener = Get-UnixSocket
        if ($null -eq $runningListener -or $runningListener.GetStatus() -ne 'Running')
        {
            return $null
        }
        $unixSocketName = "/tmp/UnixSocket.sock"

        return $unixSocketName
    }
}

function Get-UnixSocketUri {
    [CmdletBinding()]
    [OutputType([Uri])]
    param ()

    process {
        $runningListener = Get-UnixSocket
        if ($null -eq $runningListener -or $runningListener.GetStatus() -ne 'Running')
        {
            return $null
        }
        $Uri = [System.UriBuilder]::new()
        $Uri.Host = '127.0.0.0'

        return [Uri]$Uri.ToString()
    }
}
