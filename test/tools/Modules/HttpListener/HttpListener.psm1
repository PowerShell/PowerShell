# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
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
        [switch] $Foreground
        )

    Process {
        $ErrorActionPreference = "Stop"

        [scriptblock]$script = {
            param ($Port)

            $script:supportedRedirects = @{
                [System.Net.HttpStatusCode]::Ok = 1; # No redirect
                [System.Net.HttpStatusCode]::Found = 1;
                [System.Net.HttpStatusCode]::MultipleChoices = 1;
                [System.Net.HttpStatusCode]::Moved = 1;
                [System.Net.HttpStatusCode]::SeeOther = 1;
                [System.Net.HttpStatusCode]::TemporaryRedirect = 1;
            }

            # HttpListener.QueryString is not being populated, need to follow-up with CoreFx, workaround is to parse it ourselves
            Function ParseQueryString([string]$url)
            {
                Write-Verbose "Parsing: $url"
                $uri = [uri]$url
                $queryItems = @{}
                if ($null -ne $uri.Query)
                {
                    foreach ($segment in $uri.Query.Split("&"))
                    {
                        if ($segment -match "\??(?<name>\w*)(=(?<value>.*?))?$")
                        {
                            $name = $Matches["name"]
                            $value = $Matches["value"]
                            if ($null -ne $value)
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

                    foreach ($key in $request.Headers.Keys)
                    {
                        Write-Verbose -Message "Found Header: $key, $($request.Headers[$key])"
                    }

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

                            # Pass a JSON collection to the 'headers' field
                            # /PowerShell?test=response&headers={"Pragma":"no-cache","X-Fake-Header":["testvalue01","testvalue02"]}
                            # In PowerShell:
                            # $headers = @{Pragma='no-cache';'X-Fake-Header'='testvalue01','testvalue02'} | ConvertTo-Json -Compress
                            # $uri = "http://localhost:8080/PowerShell?test=response&headers=$headers"
                            if ($queryItems['headers'])
                            {
                                $headerCollection = $queryItems['headers'] | ConvertFrom-Json
                                foreach ($header in $headerCollection.psobject.Properties.name)
                                {
                                    $outputHeader.add($header,$headerCollection.$header)
                                }
                            }
                        }

                        # Echo the request as the output.
                        "echo"
                        {
                            Write-Verbose -Message "Echo request"
                            $output = $request | ConvertTo-Json -Depth 6
                        }

                        <#
                            This test provides support for multiple redirection types as well as a custom
                            multi-hop redirection to handle Authorization stripping logic.
                            The following  redirection types are supported:
                                 MultipleChoices (300), Moved (301), Found (302), SeeOther (303), TemporaryRedirect (307)

                            The original URL should  indicate the type of redirection.
                            For example: The following indicates that a 302 redirection (found) should be used.
                             ?test=redirectex&type=Found

                            WebRequest cmdlet tests also use a special option called multiredirect. This produces two redirects
                            where the second

                            Example: test=redirectex&type=Moved&multiredirect=true

                            See also https://learn.microsoft.com/dotnet/api/system.net.httpstatuscode
                        #>
                        "redirect"
                        {
                            $redirectedUrl = [string]::Empty
                            $redirectType = $queryItems["type"]
                            $multiredirect = $queryItems["multiredirect"]

                            if ($null -eq $redirectType)
                            {
                                # End of redirection
                                $redirectType = 'Ok'
                            }

                            [System.Net.HttpStatusCode] $type = [System.Net.HttpStatusCode]::Found
                            [bool] $isValid = [System.Enum]::TryParse($redirectType, $true, [ref] $type)
                            if ($isValid -eq $false -or $script:supportedRedirects.ContainsKey($type) -eq $false)
                            {
                                Write-Verbose -Message "Invalid request type: $type"
                                $statusCode = [System.Net.HttpStatusCode]::BadRequest
                                $output = "Invalid Redirect Type: $type"
                            }
                            elseif ($type -eq [System.Net.HttpStatusCode]::Ok)
                            {
                                # no redirection
                                Write-Verbose -Message "No redirection"
                                $output = $request | ConvertTo-Json -Depth 6
                            }
                            elseif ($null -eq $multiredirect)
                            {
                                Write-Verbose -Message "Standard redirection"
                                $redirectedUrl = "${Url}?test=redirect&type=Ok"
                            }
                            elseif ($multiredirect -eq $true)
                            {
                               Write-Verbose -Message "Redirect 1 of 2"
                               $redirectedUrl = "${Url}?test=redirect&type=$type&multiredirect=false"
                            }
                            elseif ($multiredirect -eq $false)
                            {
                                Write-Verbose -Message "Redirect 2 of 2"
                                $redirectedUrl = "${Url}?test=redirect&type=$type"
                            }

                            if ($isValid)
                            {
                                $statusCode = $type
                                if (-not [string]::IsNullOrEmpty($redirectedUrl))
                                {
                                    $outputHeader.Add("Location",$redirectedUrl)
                                    Write-Verbose -Message "Redirecting to $($outputHeader.Location)"
                                }
                            }
                        }
                        "linkheader"
                        {
                            $maxLinks = $queryItems["maxlinks"]
                            if ($null -eq $maxlinks)
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

                    if ($outputHeader.ContainsKey('Content-Type') -eq $false)
                    {
                        if ([string]::IsNullOrEmpty($contentType))
                        {
                            $contentType = 'application/json'
                        }

                        $outputHeader.Add('Content-Type', $contentType)
                        $response.ContentType = $contentType
                        Write-Verbose -Message "Setting ContentType to $contentType"
                    }

                    if ($null -ne $statusCode)
                    {
                        $response.StatusCode = $statusCode
                    }
                    $response.Headers.Clear()
                    foreach ($header in $outputHeader.Keys)
                    {
                        foreach ($headerValue in $outputHeader.$header)
                        {
                            $response.Headers.Add($header, $headerValue)
                        }
                    }

                    if ($null -ne $output)
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
            catch
            {
                $errormsg = $_ | ConvertTo-Json
                Write-Error $errormsg
            }
            finally
            {
                $listener.Stop()
                Write-Information "Listener is stopped" -InformationAction Continue
            }
        }

        if ($Foreground)
        {
            & $script -Port $Port
        }
        else
        {
            $ps = [powershell]::Create()
            $null = $ps.AddScript($script)
            $null = $ps.AddParameter("port",$port)
            $AsyncResponse = $ps.BeginInvoke()
            # check that it's up and running
            $out = $null
            $startTime = Get-Date
            $succeeded = $false
            while (!$succeeded -and (((Get-Date) - $startTime)).Seconds -lt 10)
            {
                try
                {
                    $out = Invoke-WebRequest "http://localhost:${Port}/PowerShell?test=response"
                    if ($out.StatusCode -eq 200)
                    {
                        $succeeded = $true
                    }
                }
                catch
                {
                    # ignore if listener is not ready
                }
                Start-Sleep -Milliseconds 100
            }
            if (!$succeeded)
            {
                throw "HttpListener failed to respond"
            }

            # include the AsyncResponse in the return object
            # it can be used to determine whether execution
            # is still underway, and may be useful in debugging
            # if something goes amiss
            [pscustomobject]@{
                PowerShell = $ps
                AsyncResponse  = $AsyncResponse
                }
        }
    }
}
