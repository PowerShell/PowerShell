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

        [scriptblock]$script = {
            param ($Port)

            # HttpListener.QueryString is not being populated, need to follow-up with CoreFx, workaround is to parse it ourselves
            Function ParseQueryString([string]$url)
            {
                Write-Verbose "Parsing: $url"
                $uri = [uri]$url
                $queryItems = @{}
                if ($uri.Query -ne $null)
                {
                    foreach ($segment in $uri.Query.Split("&"))
                    {
                        if ($segment -match "\??(?<name>\w*)(=(?<value>.*?))?$")
                        {
                            $name = $matches["name"]
                            $value = $matches["value"]
                            if ($value -ne $null)
                            {
                                $value = [System.Web.HttpUtility]::UrlDecode($value)
                            }
                            $queryItems.Add($name, $value)
                            Write-Verbose "Found: $name = $value"
                        }
                    }
                }
                return $queryItems
            }

            $listener = [System.Net.HttpListener]::New()
            $urlPrefix = "/PowerShell"
            $url = "http://localhost:$Port$urlPrefix"
            $listener.Prefixes.Add($url + "/")  # trailing slash required for registration
            $listener.AuthenticationSchemes = [System.Net.AuthenticationSchemes]::Anonymous 
            try {
                Write-Warning "Note that thread is blocked waiting for a request.  After using Ctrl-C to stop listening, you need to send a valid HTTP request to stop the listener cleanly."
                Write-Warning "Use Stop-HttpListener or Invoke-WebRequest -Uri '${Url}?test=exit' to stop the listener."
                Write-Verbose "Listening on $Url..."
                $listener.Start()
                $exit = $false
                while ($exit -eq $false) {
                    $context = $listener.GetContext()
                    $request = $context.Request
                    
                    $queryItems = ParseQueryString($request.Url)

                    $test = $queryItems["test"]
                    Write-Verbose "Testing: $test"

                    # the status code to return for the response
                    $statusCode = [System.Net.HttpStatusCode]::OK
                    # this is the body of the response, return json/xml as appropriate
                    $output = ""
                    # this is hashtable of headers in the response
                    $outputHeader = @{}
                    # this is the contenttype, example 'application/json'
                    $contentType = $null

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
                        "response"
                        {
                            $statusCode = $queryItems["statuscode"]
                            $contentType = $queryItems["contenttype"]
                            $output = $queryItems["output"]
                        }
                        "redirect"
                        {
                            $redirect = $queryItems["redirect"]
                            if ($redirect -eq $null)
                            {
                                $statusCode = [System.Net.HttpStatusCode]::Found
                                $redirectedUrl = "${Url}?test=redirect&redirect=false"
                                Write-Verbose "$redirectedUrl"
                                $outputHeader.Add("Location",$redirectedUrl)
                                Write-Verbose "Redirecting to $($outputHeader.Location)"
                            }
                            else
                            {
                                $output = $request | ConvertTo-Json
                            }
                        }
                        "linkheader"
                        {
                            $maxLinks = $queryItems["maxlinks"]
                            if ($maxlinks -eq $null)
                            {
                                $maxLinks = 3
                            }
                            $linkNumber = [int]$queryItems["linknumber"]
                            $prev = ""
                            if ($linkNumber -eq 0)
                            {
                                $linkNumber = 1
                            }
                            else
                            {
                                # use $urlPrefix to ensure output is resolved to absolute uri
                                $prev = ", <$($urlPrefix)?test=linkheader&maxlinks=$maxlinks&linknumber=$($linkNumber-1); rel=`"prev`""
                            }
                            $links = ""
                            if ($linkNumber -lt $maxLinks)
                            {
                                switch ($queryItems["type"])
                                {
                                    "noUrl"
                                    {
                                        $links = "<>; rel=`"next`","
                                    }
                                    "malformed"
                                    {
                                        $links = "{url}; foo,"
                                    }
                                    "noRel"
                                    {
                                        $links = "<url>; foo=`"bar`","
                                    }
                                    default
                                    {
                                        $links = "<$($urlPrefix)?test=linkheader&maxlinks=$maxlinks&linknumber=$($linkNumber+1)>; rel=`"next`", "
                                    }
                                }
                            }
                            $links = "$links<$($urlPrefix)?test=linkheader&maxlinks=$maxlinks&linknumber=$maxlinks>; rel=`"last`"$prev"
                            $outputHeader.Add("Link", $links)
                            $output = "{ `"output`": `"$linkNumber`"}"
                        }                        
                        default
                        {
                            $statusCode = [System.Net.HttpStatusCode]::NotFound
                            $output = "Unknown Test: $Test"
                        }
                    }

                    $response = $context.Response
                    if ($contentType -ne $null)
                    {
                        Write-Verbose "Setting ContentType to $contentType"
                        $response.ContentType = $contentType
                    }
                    if ($statusCode -ne $null)
                    {
                        $response.StatusCode = $statusCode
                    }
                    $response.Headers.Clear()
                    foreach ($header in $outputHeader.Keys)
                    {
                        $response.Headers.Add($header, $outputHeader[$header])
                    }
                    if ($output -ne $null)
                    {
                        $buffer = [System.Text.Encoding]::UTF8.GetBytes($output)
                        $response.ContentLength64 = $buffer.Length
                        $output = $response.OutputStream
                        $output.Write($buffer,0,$buffer.Length)
                        $output.Close()
                    }
                    $response.Close()
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
