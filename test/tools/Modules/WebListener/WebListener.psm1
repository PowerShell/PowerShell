Class WebListener 
{
    [int]$HttpPort
    [int]$HttpsPort
    [int]$Tls11Port
    [int]$TlsPort
    [System.Management.Automation.Job]$Job

    WebListener () { }

    [String] GetStatus() 
    {
        return $This.Job.JobStateInfo.State
    }
}

[WebListener]$WebListener

function Get-WebListener 
{
    [CmdletBinding(ConfirmImpact = 'Low')]
    [OutputType([WebListener])]
    param()

    process 
    {
        return [WebListener]$Script:WebListener
    }
}

function Start-WebListener 
{
    [CmdletBinding(ConfirmImpact = 'Low')]
    [OutputType([WebListener])]
    param 
    (
        [ValidateRange(1,65535)]
        [int]$HttpPort = 8083,

        [ValidateRange(1,65535)]
        [int]$HttpsPort = 8084,

        [ValidateRange(1,65535)]
        [int]$Tls11Port = 8085,

        [ValidateRange(1,65535)]
        [int]$TlsPort = 8086
    )
    
    process 
    {
        $runningListener = Get-WebListener
        if ($null -ne $runningListener -and $runningListener.GetStatus() -eq 'Running')
        {
            return $runningListener
        }

        $initTimeoutSeconds  = 15
        $appDll              = 'WebListener.dll'
        $serverPfx           = 'ServerCert.pfx'
        $serverPfxPassword   = 'password'
        $initCompleteMessage = 'Now listening on'
        
        $serverPfxPath = Join-Path $MyInvocation.MyCommand.Module.ModuleBase $serverPfx
        $timeOut = (get-date).AddSeconds($initTimeoutSeconds)
        $Job = Start-Job {
            $path = Split-Path -parent (get-command WebListener).Path
            Push-Location $path
            dotnet $using:appDll $using:serverPfxPath $using:serverPfxPassword $using:HttpPort $using:HttpsPort $using:Tls11Port $using:TlsPort
        }
        $Script:WebListener = [WebListener]@{
            HttpPort  = $HttpPort 
            HttpsPort = $HttpsPort
            Tls11Port = $Tls11Port
            TlsPort   = $TlsPort
            Job       = $Job
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
            throw 'WebListener did not start before the timeout was reached.'
        }
        return $Script:WebListener
    }
}

function Stop-WebListener 
{
    [CmdletBinding(ConfirmImpact = 'Low')]
    [OutputType([Void])]
    param()
    
    process 
    {
        $Script:WebListener.job | Stop-Job -PassThru | Remove-Job
        $Script:WebListener = $null
    }
}

function Get-WebListenerClientCertificate {
    [CmdletBinding(ConfirmImpact = 'Low')]
    [OutputType([System.Security.Cryptography.X509Certificates.X509Certificate2])]
    param()
    process {
        $pfxPath = Join-Path $MyInvocation.MyCommand.Module.ModuleBase 'ClientCert.pfx'
        [System.Security.Cryptography.X509Certificates.X509Certificate2]::new($pfxPath,'password')
    }
}

function Get-WebListenerUrl {
    [CmdletBinding()]
    [OutputType([Uri])]
    param (
        [switch]$Https,

        [ValidateSet('Default', 'Tls12', 'Tls11', 'Tls')]
        [string]$SslProtocol = 'Default',

        [ValidateSet(
            'Auth',
            'Cert',
            'Compression',
            'Delay',
            'Encoding',
            'Get',
            'Home',
            'Multipart',
            'Redirect',
            'ResponseHeaders',
            '/'
        )]
        [String]$Test,

        [String]$TestValue,

        [System.Collections.IDictionary]$Query
    )
    process {
        $runningListener = Get-WebListener
        if ($null -eq $runningListener -or $runningListener.GetStatus() -ne 'Running')
        {
            return $null
        }
        $Uri = [System.UriBuilder]::new()
        $Uri.Host = 'localhost'
        $Uri.Port = $runningListener.HttpPort
        $Uri.Scheme = 'Http'

        if ($Https.IsPresent)
        {
            switch ($SslProtocol)
            {
                'Tls11' { $Uri.Port = $runningListener.Tls11Port }
                'Tls'   { $Uri.Port = $runningListener.TlsPort }
                # The base HTTPs port is configured for Tls12 only
                default { $Uri.Port = $runningListener.HttpsPort }
            }
            $Uri.Scheme = 'Https'
        }

        if ($TestValue)
        {
            $Uri.Path = '{0}/{1}' -f $Test, $TestValue
        }
        else 
        {
            $Uri.Path = $Test
        }
        $StringBuilder = [System.Text.StringBuilder]::new()
        foreach ($key in $Query.Keys)
        {
            $null = $StringBuilder.Append([System.Net.WebUtility]::UrlEncode($key))
            $null = $StringBuilder.Append('=')
            $null = $StringBuilder.Append([System.Net.WebUtility]::UrlEncode($Query[$key].ToString()))
            $null = $StringBuilder.Append('&')
        }
        $Uri.Query = $StringBuilder.ToString()

        return [Uri]$Uri.ToString()
    }
}
