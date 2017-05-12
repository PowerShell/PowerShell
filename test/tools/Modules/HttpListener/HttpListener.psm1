Function Stop-HTTPListener {
    <#
    .Synopsis
        Stop HTTP Listener used for PowerShell tests rather than rely on 3rd party websites
    .Description
        Sends `exit` command to HTTP Listener causing it to exit.
    .Parameter Port
        Port to use, default is 8080
    .Example
        Stop-HTTPListener -Port 8080
    #>
    Param (
        [Parameter()]
        [Int] $Port = 8080
    )

    Invoke-WebRequest -Uri "http://localhost:$port/PowerShell?test=exit"
}

Function Start-HTTPListener {
    <#
    .Synopsis
        Creates a new HTTP Listener to be used for PowerShell tests rather than rely on 3rd party websites
    .Description
        Creates a new HTTP Listener supporting several test cases intended to be used only by PowerShell tests.
        This cmdlet requires running from an elevated administrator prompt to open a port.

        Use Ctrl-C to stop the listener.  You'll need to send another web request to allow the listener to stop since
        it will be blocked waiting for a request.
    .Parameter Port
        Port to listen, default is 8080
    .Example
        Start-HTTPListener -Port 8080
        Invoke-WebRequest -Uri "http://localhost:8080/PowerShell/?test=linkheader&maxlinks=5"
    #>
    Param (
        [Parameter()]
        [Int] $Port = 8080,

        [Parameter()]
        [switch] $AsJob
        )

    Process {
        $ErrorActionPreference = "Stop"

        if ($IsWindows)
        {
            $CurrentPrincipal = New-Object Security.Principal.WindowsPrincipal( [Security.Principal.WindowsIdentity]::GetCurrent())
            if ( -not ($currentPrincipal.IsInRole( [Security.Principal.WindowsBuiltInRole]::Administrator ))) {
                Write-Error "This script must be executed from an elevated PowerShell session" -ErrorAction Stop
            }
        }

        [scriptblock]$script = {
            param ($Port)

            # HttpListener.QueryString is not being populated, need to follow-up with CoreFx, workaround is to parse it ourselves
            Function ParseQueryString([string]$url)
            {
                Write-Verbose "Parsing: $url"
                $queryItems = @{}
                foreach ($segment in $url.Split("&"))
                {
                    if ($segment -match "\??(?<name>\w*)(=(?<value>\w*))?$")
                    {
                        $name = $matches["name"]
                        $value = $matches["value"]
                        $queryItems.Add($name, $value)
                        Write-Verbose "Found: $name = $value"
                    }
                }
                return $queryItems
            }

            $listener = [System.Net.HttpListener]::New()
            $urlPrefix = "/PowerShell"
            $url = "http://localhost:$Port$urlPrefix/"
            $listener.Prefixes.Add($url)
            $listener.AuthenticationSchemes = [System.Net.AuthenticationSchemes]::Anonymous 
            try {
                Write-Warning "Note that thread is blocked waiting for a request.  After using Ctrl-C to stop listening, you need to send a valid HTTP request to stop the listener cleanly."
                Write-Warning "Sending 'exit' command will cause listener to stop"
                Write-Verbose "Listening on $url..."
                $listener.Start()
                $exit = $false
                while ($exit -eq $false) {
                    $context = $listener.GetContext()
                    $request = $context.Request
                    
                    $queryItems = ParseQueryString($request.RawUrl)

                    $test = $queryItems["test"]
                    Write-Verbose "Testing: $test"

                    # the status code to return for the response
                    $statusCode = [System.Net.HttpStatusCode]::OK
                    # this is the body of the response, return json/xml as appropriate
                    $output = ""
                    # this is hashtable of headers in the response
                    $outputHeader = @{}

                    switch ($test)
                    {
                        $null
                        {
                            $statusCode = [System.Net.HttpStatusCode]::BadRequest
                            $output = "Test not specified"
                        }
                        "exit"
                        {
                            Write-Verbose "Received command to exit listener"
                            $output = "Exit command received"
                            $exit = $true
                        }
                        default
                        {
                            $statusCode = [System.Net.HttpStatusCode]::NotFound
                            $output = "Unknown Test: $Test"
                        }
                    }

                    $response = $context.Response
                    $response.StatusCode = $statusCode
                    $response.Headers.Clear()
                    foreach ($header in $outputHeader.Keys)
                    {
                        $response.Headers.Add($header, $outputHeader[$header])
                    }
                    $buffer = [System.Text.Encoding]::UTF8.GetBytes($output)

                    $response.ContentLength64 = $buffer.Length
                    $output = $response.OutputStream
                    $output.Write($buffer,0,$buffer.Length)
                    $output.Close()
                }
            }
            finally
            {
                $listener.Stop()
            }
        }

        if ($AsJob)
        {
            Start-Job -ScriptBlock $script -ArgumentList $Port
        }
        else
        {
            & $script -Port $Port
        }
    }
}
