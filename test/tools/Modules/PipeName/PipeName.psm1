# Copyright (c) kestrun.
# Licensed under the MIT License.

Class PipeNameServer
{
    [System.Management.Automation.Job]$Job

    PipeNameServer () { }

    [String] GetStatus()
    {
        return $this.Job.JobStateInfo.State
    }
}

[PipeNameServer]$PipeServer

function Get-PipeServer
{
    [CmdletBinding()] [OutputType([PipeNameServer])]
    param()
    process { return [PipeNameServer]$Script:PipeServer }
}

function Get-PipeName
{
    [CmdletBinding()] [OutputType([string])]
    param()
    process { return [System.IO.Path]::GetRandomFileName() }
}

function Get-PipeServerUri
{
    [CmdletBinding()] [OutputType([Uri])]
    param()
    process {
        $running = Get-PipeServer
        if ($null -eq $running -or $running.GetStatus() -ne 'Running') { return $null }
        return [Uri]"http://localhost/"
    }
}

function Start-PipeServer
{
    [CmdletBinding()] [OutputType([PipeNameServer])]
    param([Parameter(Mandatory)] [string] $PipeName)

    process {
        $running = Get-PipeServer
        if ($null -ne $running -and $running.GetStatus() -eq 'Running') { return $running }

        $Job = Start-Job -ArgumentList $PipeName {
            param($pn)
            $responseBody = "Hello World PipeName."
            $responseBodyBytes = [System.Text.Encoding]::UTF8.GetBytes($responseBody)
            $headerTemplate = "HTTP/1.1 200 OK`r`nContent-Type: text/plain`r`nContent-Length: {0}`r`nConnection: close`r`n`r`n"
            $headers = [System.Text.Encoding]::ASCII.GetBytes([string]::Format($headerTemplate, $responseBodyBytes.Length))

            for($i = 0; $i -lt 2; $i++) {
                $server = [System.IO.Pipes.NamedPipeServerStream]::new($pn, [System.IO.Pipes.PipeDirection]::InOut, 1, [System.IO.Pipes.PipeTransmissionMode]::Byte, [System.IO.Pipes.PipeOptions]::Asynchronous)
                try {
                    $server.WaitForConnection()
                    $reader = [System.IO.StreamReader]::new($server, [System.Text.Encoding]::ASCII, $false, 1024, $true)
                    while(($line = $reader.ReadLine()) -ne $null) { if ($line -eq '') { break } }
                    $server.Write($headers,0,$headers.Length)
                    $server.Write($responseBodyBytes,0,$responseBodyBytes.Length)
                    $server.Flush()
                } finally {
                    $server.Dispose()
                }
            }
        }

        $Script:PipeServer = [PipeNameServer]@{ Job = $Job }
        Start-Sleep -Milliseconds 200
        return $Script:PipeServer
    }
}

function Stop-PipeServer
{
    [CmdletBinding()] [OutputType([void])]
    param()
    process {
        if ($Script:PipeServer) {
            $Script:PipeServer.Job | Stop-Job -ErrorAction SilentlyContinue | Remove-Job -Force -ErrorAction SilentlyContinue
            $Script:PipeServer = $null
        }
    }
}

Export-ModuleMember -Function Get-PipeServer,Get-PipeName,Get-PipeServerUri,Start-PipeServer,Stop-PipeServer
