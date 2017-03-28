#
# Copyright (c) Microsoft Corporation, 2016
#
# This is a Pester test suite which validate the Web cmdlets.
#
# Note: These tests use data from http://httpbin.org/
#

# Invokes the given command via script block invocation.
#
function ExecuteWebCommand
{
    param (
        [ValidateNotNullOrEmpty()]
        [string]
        $command
    )

    $result = [PSObject]@{Output = $null; Error = $null}

    try
    {
        $scriptBlock = [scriptblock]::Create($command)
        $result.Output =  & $scriptBlock
    }
    catch
    {
        $result.Error = $_
    }

    return $result
}

# This function calls either Invoke-WebRequest or Invoke-RestMethod using the OutFile parameter
# Then, the file content is read and return in a $result object.
#
function ExecuteRequestWithOutFile
{
    param (
        [ValidateSet("Invoke-RestMethod", "Invoke-WebRequest" )]
        [string]
        $cmdletName,
        [string]
        $uri = "http://httpbin.org/get"
    )

    $result = [PSObject]@{Output = $null; Error = $null}
    $filePath = Join-Path $TestDrive ((Get-Random).ToString() + ".txt")
    try
    {
        if ($cmdletName -eq "Invoke-WebRequest")
        {
            Invoke-WebRequest -Uri $uri -OutFile $filePath
        }
        else
        {
            Invoke-RestMethod -Uri $uri -OutFile $filePath
        }
        $result.Output =  Get-Content $filePath -Raw -ea SilentlyContinue
    }
    catch
    {
        $result.Error = $_
    }
    finally
    {
        if (Test-Path $filePath)
        {
            Remove-Item $filePath -Force -ea SilentlyContinue
        }
    }
    return $result
}

# This function calls either Invoke-WebRequest or Invoke-RestMethod with the given uri
# using the Headers parameter to disable keep-alive.
#
function ExecuteRequestWithHeaders
{
    param (
        [ValidateSet("Invoke-RestMethod", "Invoke-WebRequest" )]
        [string]
        $cmdletName,
        [string]
        $uri = "http://httpbin.org/get"
    )

    $result = [PSObject]@{Output = $null; Error = $null}
    try
    {
        $headers = @{ Connection = 'close'}
        if ($cmdletName -eq "Invoke-WebRequest")
        {
            $result.Output =  Invoke-WebRequest -Uri $uri -TimeoutSec 5 -Headers $headers
        }
        else
        {
            $result.Output =  Invoke-RestMethod -Uri $uri -TimeoutSec 5 -Headers $headers
        }
    }
    catch
    {
        $result.Error = $_
    }
    return $result
}

# Returns test data for the given content type.
#
function GetTestData
{
    param(
        [ValidateSet("text/plain", "application/xml", "application/json")]
        [String]
        $contentType
    )

    $testData = @{ItemID = 987123; Name = 'TestData'}

    if ($contentType -eq "text/plain")
    {
        $body = $testData | Out-String
    }

    elseif ($contentType -eq "application/xml")
    {
        $body = '
<?xml version="1.0" encoding="utf-8"?>
<Objects>
<Object>
<ItemID>987123</ItemID>
<Name>TestData</Name>
</Object>
</Objects>
'
    }

    else # "application/json"
    {
        $body = $testData | ConvertTo-Json -Compress
    }

    return $body
}

Describe "Invoke-WebRequest tests" -Tags "Feature" {

    # Validate the output of Invoke-WebRequest
    #
    function ValidateResponse
    {
        param ($response)

        $response.Error | Should Be $null

        # A successful call returns: Status = 200, and StatusDescription = "OK"
        $response.Output.StatusDescription | Should Match "OK"
        $response.Output.StatusCode | Should Be 200

        # Make sure the response contains the following properties:
        $response.Output.RawContent | Should Not Be $null
        $response.Output.Headers | Should Not Be $null
        $response.Output.RawContent | Should Not Be $null
        $response.Output.RawContentLength | Should Not Be $null
        $response.Output.Content | Should Not Be $null
    }

    It "Invoke-WebRequest returns User-Agent" {

        $command = "Invoke-WebRequest -Uri http://httpbin.org/user-agent -TimeoutSec 5"

        $result = ExecuteWebCommand -command $command
        ValidateResponse -response $result

        # Validate response content
        $jsonContent = $result.Output.Content | ConvertFrom-Json
        $jsonContent.'User-Agent' | Should Match "WindowsPowerShell"
    }

    It "Invoke-WebRequest returns headers dictionary" {

        $command = "Invoke-WebRequest -Uri http://httpbin.org/headers -TimeoutSec 5"

        $result = ExecuteWebCommand -command $command
        ValidateResponse -response $result

        # Validate response content
        $jsonContent = $result.Output.Content | ConvertFrom-Json
        $jsonContent.headers.'Accept-Encoding' | Should Match "gzip, ?deflate"
        $jsonContent.headers.Host | Should Match "httpbin.org"
        $jsonContent.headers.'User-Agent' | Should Match "WindowsPowerShell"
    }

    It "Validate Invoke-WebRequest -DisableKeepAlive" {

        # Operation options
        $uri = "http://httpbin.org/get"
        $command = "Invoke-WebRequest -Uri $uri -TimeoutSec 5 -DisableKeepAlive"

        $result = ExecuteWebCommand -command $command
        ValidateResponse -response $result

        $result.Output.Headers["Connection"] | Should Be "Close"
    }

    It "Validate Invoke-WebRequest -MaximumRedirection" {

        $command = "Invoke-WebRequest -Uri 'http://httpbin.org/redirect/3' -MaximumRedirection 4 -TimeoutSec 5"

        $result = ExecuteWebCommand -command $command
        ValidateResponse -response $result

        # Validate response content
        $jsonContent = $result.Output.Content | ConvertFrom-Json
        $jsonContent.headers.Host | Should Match "httpbin.org"
        $jsonContent.headers.'User-Agent' | Should Match "WindowsPowerShell"
    }

    It "Validate Invoke-WebRequest error for -MaximumRedirection" {

        $command = "Invoke-WebRequest -Uri 'http://httpbin.org/redirect/3' -MaximumRedirection 2 -TimeoutSec 5"

        $result = ExecuteWebCommand -command $command
        $result.Error.FullyQualifiedErrorId | Should Be "WebCmdletWebResponseException,Microsoft.PowerShell.Commands.InvokeWebRequestCommand"
    }

    It "Invoke-WebRequest supports request that returns page containing UTF-8 data." {

        $command = "Invoke-WebRequest -Uri http://httpbin.org/encoding/utf8 -TimeoutSec 5"

        $result = ExecuteWebCommand -command $command
        ValidateResponse -response $result

        # Validate response content
        # TODO: There is a bug on ConvertFrom-Json that fails for utf8.
        <#
        $jsonContent = $result.Output.Content | ConvertFrom-Json
        $jsonContent.headers.'Accept-Encoding' | Should Match "gzip, deflate"
        $jsonContent.headers.Host | Should Match "httpbin.org"
        $jsonContent.headers.'User-Agent' | Should Match "WindowsPowerShell"
        #>
    }

    It "Invoke-WebRequest validate timeout option" {

        $command = "Invoke-WebRequest -Uri http://httpbin.org/delay/:5 -TimeoutSec 5"

        $result = ExecuteWebCommand -command $command
        $result.Error.FullyQualifiedErrorId | Should Be "WebCmdletWebResponseException,Microsoft.PowerShell.Commands.InvokeWebRequestCommand"
    }

    It "Validate Invoke-WebRequest error with -Proxy and -NoProxy option" {

        $command = "Invoke-WebRequest -Uri http://httpbin.org/delay/:10 -Proxy 'http://localhost:8080' -NoProxy -TimeoutSec 2"

        $result = ExecuteWebCommand -command $command
        $result.Error.FullyQualifiedErrorId | Should Be "AmbiguousParameterSet,Microsoft.PowerShell.Commands.InvokeWebRequestCommand"
    }

    $testCase = @(
        @{ proxy_address = "http://localhost:8080"; name = 'http_proxy'; protocol = 'http' }
        @{ proxy_address = "http://localhost:8080"; name = 'https_proxy'; protocol = 'https' }
    )

    It "Validate Invoke-WebRequest error with -Proxy option set - '<name>'" -TestCases $testCase {
        param($proxy_address, $name, $protocol)

        $command = "Invoke-WebRequest -Uri '${protocol}://httpbin.org/delay/:5' -TimeoutSec 5 -Proxy '${proxy_address}'"

        $result = ExecuteWebCommand -command $command
        $result.Error.FullyQualifiedErrorId | Should Be "WebCmdletWebResponseException,Microsoft.PowerShell.Commands.InvokeWebRequestCommand"
    }

    It "Validate Invoke-WebRequest error with environment proxy set - '<name>'" -TestCases $testCase {
        param($proxy_address, $name, $protocol)

        # Configure the environment variable.
        New-Item -Name ${name} -Value ${proxy_address} -ItemType Variable -Path Env: -Force

        $command = "Invoke-WebRequest -Uri '${protocol}://httpbin.org/delay/:5' -TimeoutSec 5"

        $result = ExecuteWebCommand -command $command
        $result.Error.FullyQualifiedErrorId | Should Be "WebCmdletWebResponseException,Microsoft.PowerShell.Commands.InvokeWebRequestCommand"
    }

    It "Validate Invoke-WebRequest returns User-Agent where -NoProxy with envirionment proxy set - '<name>'" -TestCases $testCase {
        param($proxy_address, $name, $protocol)

        # Configure the environment variable.
        New-Item -Name ${name} -Value ${proxy_address} -ItemType Variable -Path Env: -Force

        $command = "Invoke-WebRequest -Uri '${protocol}://httpbin.org/headers' -TimeoutSec 5 -NoProxy"

        $result = ExecuteWebCommand -command $command
        ValidateResponse -response $result

        # Validate response content
        $jsonContent = $result.Output.Content | ConvertFrom-Json
        $jsonContent.headers.'Accept-Encoding' | Should Match "gzip, ?deflate"
        $jsonContent.headers.Host | Should Match "httpbin.org"
        $jsonContent.headers.'User-Agent' | Should Match "WindowsPowerShell"
    }

    It "Invoke-WebRequest validate timeout option" {

        $command = "Invoke-WebRequest -Uri http://httpbin.org/delay/:5 -TimeoutSec 10"

        $result = ExecuteWebCommand -command $command
        $result.Error.FullyQualifiedErrorId | Should Be "WebCmdletWebResponseException,Microsoft.PowerShell.Commands.InvokeWebRequestCommand"
    }

    # Perform the following operation for Invoke-WebRequest
    # gzip Returns gzip-encoded data.
    # deflate Returns deflate-encoded data.
    # $dataEncodings = @("Chunked", "Compress", "Deflate", "GZip", "Identity")
    #                 Note: These are the supported options, but we do not have a web service to test them all.
    # $dataEncodings = @("gzip", "deflate") --> Currently there is a bug for deflate encoding. Please see '7976639:Invoke-WebRequest does not support -TransferEncoding deflate' for more info.
    $dataEncodings = @("gzip")
    foreach ($data in $dataEncodings)
    {
        It "Invoke-WebRequest supports request that returns $data-encoded data." {

            $command = "Invoke-WebRequest -Uri http://httpbin.org/$data -TimeoutSec 5"

            $result = ExecuteWebCommand -command $command
            ValidateResponse -response $result

            # Validate response content
            $jsonContent = $result.Output.Content | ConvertFrom-Json
            if ($data -eq "gzip")
            {
                $jsonContent.gzipped | Should Match $true
            }
            else
            {
                $jsonContent.deflated | Should Match $true
            }
        }
    }

    # Perform the following operation for Invoke-WebRequest using the following content types: "text/plain", "application/xml", "application/xml"
    # post Returns POST data.
    # patch Returns PATCH data.
    # put Returns PUT data.
    # delete Returns DELETE data
    $testMethods = @("GET", "POST", "PATCH", "PUT", "DELETE")
    $contentTypes = @("text/plain", "application/xml", "application/json")

    foreach ($contentType in $contentTypes)
    {
        foreach ($method in $testMethods)
        {
            # Operation options
            $operation = $method.ToLower()
            $uri = "http://httpbin.org/$operation"
            $body = GetTestData -contentType $contentType

            if ($method -eq "GET")
            {
                $command = "Invoke-WebRequest -Uri $uri"
            }
            else
            {
                $command = "Invoke-WebRequest -Uri $uri -Body '$body' -Method $method -ContentType $contentType -TimeoutSec 5"
            }

            It "$command" {

                $result = ExecuteWebCommand -command $command
                ValidateResponse -response $result

                # Validate response content
                $jsonContent = $result.Output.Content | ConvertFrom-Json
                $jsonContent.url | Should Match $uri
                $jsonContent.headers.'Accept-Encoding' | Should Match "gzip, ?deflate"
                $jsonContent.headers.'User-Agent' | Should Match "WindowsPowerShell"

                # For a GET request, there is no data property to validate.
                if ($method -ne "GET")
                {
                    $jsonContent.headers.'Content-Type' | Should Match $contentType

                    # Validate that the response Content.data field is the same as what we sent.
                    if ($contentType -eq "application/xml")
                    {
                        $jsonContent.data | Should Be $body
                    }
                    else
                    {
                        $jsonContent.data | Should Match $body
                    }
                }
            }
        }
    }

    It "Validate Invoke-WebRequest -Headers --> Set KeepAlive to false via headers" {

        $uri = "http://httpbin.org/get"
        $result = ExecuteRequestWithHeaders -cmdletName Invoke-WebRequest -uri $uri
        ValidateResponse -response $result
        $result.Output.Headers["Connection"] | Should Be "Close"
    }

    # Validate all available user agents for Invoke-WebRequest
    $agents = @{InternetExplorer = "MSIE 9.0"
                Chrome           = "Chrome"
                Opera            = "Opera"
                Safari           = "Safari"
                FireFox          = "Firefox"
                }

    foreach ($agentName in $agents.Keys)
    {
        $expectedAgent = $agents[$agentName]
        $uri = "http://httpbin.org/get"
        $userAgent = "[Microsoft.PowerShell.Commands.PSUserAgent]::$agentName"
        $command = "Invoke-WebRequest -Uri $uri -UserAgent ($userAgent)  -TimeoutSec 5"

        It "Validate Invoke-WebRequest UserAgent. Execute--> $command" {

            $result = ExecuteWebCommand -command $command
            ValidateResponse -response $result

            # Validate response content
            $jsonContent = $result.Output.Content | ConvertFrom-Json
            $jsonContent.headers.Host | Should Match "httpbin.org"
            $jsonContent.headers.'User-Agent' | Should Match $expectedAgent
        }
    }

    It "Validate Invoke-WebRequest -OutFile" {

        $uri = "http://httpbin.org/get"
        $result = ExecuteRequestWithOutFile -cmdletName "Invoke-WebRequest" -uri $uri
        $jsonContent = $result.Output | ConvertFrom-Json
        $jsonContent.headers.Host | Should Match "httpbin.org"
        $jsonContent.headers.'User-Agent' | Should Match "WindowsPowerShell"
    }

    It "Validate Invoke-WebRequest -SkipCertificateCheck" {

        # validate that exception is thrown for URI with expired certificate
        $command = "Invoke-WebRequest -Uri 'https://expired.badssl.com'"
        $result = ExecuteWebCommand -command $command
        $result.Error.FullyQualifiedErrorId | Should Be "WebCmdletWebResponseException,Microsoft.PowerShell.Commands.InvokeWebRequestCommand"

        # validate that no exception is thrown for URI with expired certificate when using -SkipCertificateCheck option
        $command = "Invoke-WebRequest -Uri 'https://expired.badssl.com' -SkipCertificateCheck"
        $result = ExecuteWebCommand -command $command
        $result.Error | Should BeNullOrEmpty
    }

    It "Validate Invoke-WebRequest handles missing Content-Type in response header" {

        #Validate that exception is not thrown when response headers are missing Content-Type.
        $command = "Invoke-WebRequest -Uri 'http://httpbin.org/response-headers?Content-Type='"
        $result = ExecuteWebCommand -command $command
        $result.Error | Should BeNullOrEmpty
    }

    It "Validate Invoke-WebRequest StandardMethod and CustomMethod parameter sets" {
        
        #Validate that parameter sets are functioning correctly
        $errorId = "AmbiguousParameterSet,Microsoft.PowerShell.Commands.InvokeWebRequestCommand"
        { Invoke-WebRequest -Uri 'http://http.lee.io/method' -Method GET -CustomMethod TEST } | ShouldBeErrorId $errorId
    }

    It "Validate Invoke-WebRequest CustomMethod method is used" {
        
        $command = "Invoke-WebRequest -Uri 'http://http.lee.io/method' -CustomMethod TEST"
        $result = ExecuteWebCommand -command $command
        $result.Error | Should BeNullOrEmpty
        ($result.Output.Content | ConvertFrom-Json).output.method | Should Be "TEST"
    }

    It "Validate Invoke-WebRequest default ContentType for CustomMethod POST" {
        
        $command = "Invoke-WebRequest -Uri 'http://httpbin.org/post' -CustomMethod POST -Body 'testparam=testvalue'"
        $result = ExecuteWebCommand -command $command
        ($result.Output.Content | ConvertFrom-Json).form.testparam | Should Be "testvalue"
    }

    It "Validate Invoke-WebRequest body is converted to query params for CustomMethod GET" {
        
        $command = "Invoke-WebRequest -Uri 'http://httpbin.org/get' -CustomMethod GET -Body @{'testparam'='testvalue'}"
        $result = ExecuteWebCommand -command $command
        ($result.Output.Content | ConvertFrom-Json).args.testparam | Should Be "testvalue"
    }
    
    It "Validate Invoke-WebRequest returns HTTP errors in exception" {

        $command = "Invoke-WebRequest -Uri http://httpbin.org/status/418"
        $result = ExecuteWebCommand -command $command

        $result.Error.ErrorDetails.Message | Should Match "\-=\[ teapot \]"
        $result.Error.Exception | Should BeOfType Microsoft.PowerShell.Commands.HttpResponseException
        $result.Error.Exception.Response.StatusCode | Should Be 418
        $result.Error.Exception.Response.ReasonPhrase | Should Be "I'm a teapot"
        $result.Error.Exception.Message | Should Match ": 418 \(I'm a teapot\)\."
        $result.Error.FullyQualifiedErrorId | Should Be "WebCmdletWebResponseException,Microsoft.PowerShell.Commands.InvokeWebRequestCommand"
    }

    It "Validate Invoke-WebRequest returns native HTTPS error message in exception" {

        $command = "Invoke-WebRequest -Uri https://incomplete.chain.badssl.com"
        $result = ExecuteWebCommand -command $command

        # need to check against inner exception since Linux and Windows uses different HTTP client libraries so errors aren't the same
        $result.Error.ErrorDetails.Message | Should Match $result.Error.Exception.InnerException.Message
        $result.Error.FullyQualifiedErrorId | Should Be "WebCmdletWebResponseException,Microsoft.PowerShell.Commands.InvokeWebRequestCommand"
    }

    BeforeEach {
        if ($env:http_proxy) {
            $savedHttpProxy = $env:http_proxy
            $copiedHttpProxy = $true
        }

        if ($env:https_proxy) {
            $savedHttpsProxy = $env:https_proxy
            $copiedHttpsProxy = $true
        }
    }

    AfterEach {
        if ($copiedHttpProxy) {
            $env:http_proxy = $savedHttpProxy
        } else {
            $env:http_proxy = $null
        }

        if ($copiedHttpsProxy) {
            $env:https_proxy = $savedHttpsProxy
        } else {
            $env:https_proxy = $null
        }
    }
}

Describe "Invoke-RestMethod tests" -Tags "Feature" {

    It "Invoke-RestMethod returns User-Agent" {

        $command = "Invoke-RestMethod -Uri http://httpbin.org/user-agent -TimeoutSec 5"

        $result = ExecuteWebCommand -command $command

        # Validate response
        $result.Output.'User-Agent' | Should Match "WindowsPowerShell"
    }

    It "Invoke-RestMethod returns headers dictionary" {

        $command = "Invoke-RestMethod -Uri http://httpbin.org/headers -TimeoutSec 5"

        $result = ExecuteWebCommand -command $command

        # Validate response
        $result.Output.headers.'Accept-Encoding' | Should Match "gzip, ?deflate"
        $result.Output.headers.Host | Should Match "httpbin.org"
        $result.Output.headers.'User-Agent' | Should Match "WindowsPowerShell"
    }

    It "Validate Invoke-RestMethod -DisableKeepAlive" {

        # Operation options
        $command = "Invoke-RestMethod -Uri 'http://httpbin.org/get' -TimeoutSec 5 -DisableKeepAlive"

        $result = ExecuteWebCommand -command $command

        # Validate response
        $result.Output.headers.'Accept-Encoding' | Should Match "gzip, ?deflate"
        $result.Output.headers.Host | Should Match "httpbin.org"
        $result.Output.headers.'User-Agent' | Should Match "WindowsPowerShell"

        # Unfortunately, the connection information is not display in the output of Invoke-RestMethod
        #$result.Output.Headers["Connection"] | Should Be "Close"
    }

    It "Validate Invoke-RestMethod -MaximumRedirection" {

        $command = "Invoke-RestMethod -Uri 'http://httpbin.org/redirect/3' -MaximumRedirection 4 -TimeoutSec 5"

        $result = ExecuteWebCommand -command $command

        # Validate response
        $result.Output.headers.Host | Should Match "httpbin.org"
        $result.Output.headers.'User-Agent' | Should Match "WindowsPowerShell"
    }

    It "Validate Invoke-RestMethod error for -MaximumRedirection" {

        $command = "Invoke-RestMethod -Uri 'http://httpbin.org/redirect/3' -MaximumRedirection 2 -TimeoutSec 5"

        $result = ExecuteWebCommand -command $command
        $result.Error.FullyQualifiedErrorId | Should Be "WebCmdletWebResponseException,Microsoft.PowerShell.Commands.InvokeRestMethodCommand"
    }

    <#
    It "Invoke-RestMethod supports request that returns page containing UTF-8 data." {

        $command = "Invoke-RestMethod -Uri http://httpbin.org/encoding/utf8 -TimeoutSec 5"

        $result = ExecuteWebCommand -command $command

        # Validate response content
        # TODO: There is a bug on ConvertFrom-Json that fails for utf8.
        $result.headers.'Accept-Encoding' | Should Match "gzip, deflate"
        $result.headers.Host | Should Match "httpbin.org"
        $result.headers.'User-Agent' | Should Match "WindowsPowerShell"
    }
    #>

    It "Invoke-RestMethod validate timeout option" {

        $command = "Invoke-RestMethod -Uri http://httpbin.org/delay/:5 -TimeoutSec 2"

        $result = ExecuteWebCommand -command $command
        $result.Error.FullyQualifiedErrorId | Should Be "WebCmdletWebResponseException,Microsoft.PowerShell.Commands.InvokeRestMethodCommand"
    }

    It "Validate Invoke-RestMethod error with -Proxy and -NoProxy option" {

        $command = "Invoke-RestMethod -Uri http://httpbin.org/delay/:10 -Proxy 'http://localhost:8080' -NoProxy -TimeoutSec 2"

        $result = ExecuteWebCommand -command $command
        $result.Error.FullyQualifiedErrorId | Should Be "AmbiguousParameterSet,Microsoft.PowerShell.Commands.InvokeRestMethodCommand"
    }

    $testCase = @(
        @{ proxy_address = "http://localhost:8080"; name = 'http_proxy'; protocol = 'http' }
        @{ proxy_address = "http://localhost:8080"; name = 'https_proxy'; protocol = 'https' }
    )

    It "Validate Invoke-RestMethod error with -Proxy option - '<name>'" -TestCases $testCase {
        param($proxy_address, $name, $protocol)

        $command = "Invoke-RestMethod -Uri '${protocol}://httpbin.org/' -Proxy '${proxy_address}' -TimeoutSec 2"

        $result = ExecuteWebCommand -command $command
        $result.Error.FullyQualifiedErrorId | Should Be "System.Threading.Tasks.TaskCanceledException,Microsoft.PowerShell.Commands.InvokeRestMethodCommand"
    }

    It "Validate Invoke-RestMethod error with environment proxy set - '<name>'" -TestCases $testCase {
        param($proxy_address, $name, $protocol)

        # Configure the environment variable.
        New-Item -Name ${name} -Value ${proxy_address} -ItemType Variable -Path Env: -Force

        $command = "Invoke-RestMethod -Uri '${protocol}://httpbin.org/delay/:5' -TimeoutSec 5"

        $result = ExecuteWebCommand -command $command
        $result.Error.FullyQualifiedErrorId | Should Be "WebCmdletWebResponseException,Microsoft.PowerShell.Commands.InvokeRestMethodCommand"
    }

    It "Validate Invoke-RestMethod returns User-Agent with option -NoProxy when environment proxy set - '<name>'" -TestCases $testCase {
        param($proxy_address, $name, $protocol)

        # Configure the environment variable.
        New-Item -Name ${name} -Value ${proxy_address} -ItemType Variable -Path Env: -Force

        $command = "Invoke-RestMethod -Uri '${protocol}://httpbin.org/user-agent' -TimeoutSec 5 -NoProxy"

        $result = ExecuteWebCommand -command $command

        # Validate response
        $result.Output.'User-Agent' | Should Match "WindowsPowerShell"
    }

    # Perform the following operation for Invoke-RestMethod
    # gzip Returns gzip-encoded data.
    # deflate Returns deflate-encoded data.
    # $dataEncodings = @("Chunked", "Compress", "Deflate", "GZip", "Identity")
    #                 Note: These are the supported options, but we do not have a web service to test them all.
    # $dataEncodings = @("gzip", "deflate") --> Currently there is a bug for deflate encoding. Please see '7976639:Invoke-RestMethod does not support -TransferEncoding deflate' for more info.
    $dataEncodings = @("gzip")
    foreach ($data in $dataEncodings)
    {
        It "Invoke-RestMethod supports request that returns $data-encoded data." {

            $command = "Invoke-RestMethod -Uri http://httpbin.org/$data -TimeoutSec 5"

            $result = ExecuteWebCommand -command $command

            # Validate response
            if ($data -eq "gzip")
            {
                $result.Output.gzipped | Should Match $true
            }
            else
            {
                $result.Output.deflated | Should Match $true
            }
        }
    }

    # Perform the following operation for Invoke-RestMethod using the following content types: "text/plain", "application/xml", "application/xml"
    # post Returns POST data.
    # patch Returns PATCH data.
    # put Returns PUT data.
    # delete Returns DELETE data
    $testMethods = @("GET", "POST", "PATCH", "PUT", "DELETE")
    $contentTypes = @("text/plain", "application/xml", "application/json")

    foreach ($contentType in $contentTypes)
    {
        foreach ($method in $testMethods)
        {
            # Operation options
            $operation = $method.ToLower()
            $uri = "http://httpbin.org/$operation"
            $body = GetTestData -contentType $contentType

            if ($method -eq "GET")
            {
                $command = "Invoke-RestMethod -Uri $uri"
            }
            else
            {
                $command = "Invoke-RestMethod -Uri $uri -Body '$body' -Method $method -ContentType $contentType -TimeoutSec 5"
            }

            It "$command" {

                $result = ExecuteWebCommand -command $command

                # Validate response
                $result.Output.url | Should Match $uri
                $result.Output.headers.'Accept-Encoding' | Should Match "gzip, ?deflate"
                $result.Output.headers.'User-Agent' | Should Match "WindowsPowerShell"

                # For a GET request, there is no data property to validate.
                if ($method -ne "GET")
                {
                    $result.Output.headers.'Content-Type' | Should Match $contentType

                    # Validate that the response Content.data field is the same as what we sent.
                    if ($contentType -eq "application/xml")
                    {
                        $result.Output.data | Should Be $body
                    }
                    else
                    {
                        $result.Output.data | Should Match $body
                    }
                }
            }
        }
    }

    It "Validate Invoke-RestMethod -Headers --> Set KeepAlive to false via headers" {

        $uri = "http://httpbin.org/get"
        $result = ExecuteRequestWithHeaders -cmdletName Invoke-RestMethod -uri $uri

        # Validate response
        $result.Output.url | Should Match $uri
        $result.Output.headers.'Accept-Encoding' | Should Match "gzip, ?deflate"
        $result.Output.headers.'User-Agent' | Should Match "WindowsPowerShell"

        # Unfortunately, the connection information is not display in the output of Invoke-RestMethod
        #$result.Output.Headers["Connection"] | Should Be "Close"
    }

    # Validate all available user agents for Invoke-RestMethod
    $agents = @{InternetExplorer = "MSIE 9.0"
                Chrome           = "Chrome"
                Opera            = "Opera"
                Safari           = "Safari"
                FireFox          = "Firefox"
                }

    foreach ($agentName in $agents.Keys)
    {
        $expectedAgent = $agents[$agentName]
        $uri = "http://httpbin.org/get"
        $userAgent = "[Microsoft.PowerShell.Commands.PSUserAgent]::$agentName"
        $command = "Invoke-RestMethod -Uri $uri -UserAgent ($userAgent)  -TimeoutSec 5"

        It "Validate Invoke-RestMethod UserAgent. Execute--> $command" {

            $result = ExecuteWebCommand -command $command

            # Validate response
            $result.Output.headers.Host | Should Match "httpbin.org"
            $result.Output.headers.'User-Agent' | Should Match $expectedAgent
        }
    }

    It "Validate Invoke-RestMethod -OutFile" {

        $uri = "http://httpbin.org/get"
        $result = ExecuteRequestWithOutFile -cmdletName "Invoke-RestMethod" -uri $uri
        $jsonContent = $result.Output | ConvertFrom-Json
        $jsonContent.headers.Host | Should Match "httpbin.org"
        $jsonContent.headers.'User-Agent' | Should Match "WindowsPowerShell"
    }

    It "Validate Invoke-RestMethod -SkipCertificateCheck" {

        # HTTP method HEAD must be used to not retrieve an unparsable HTTP body
        # validate that exception is thrown for URI with expired certificate
        $command = "Invoke-RestMethod -Uri 'https://expired.badssl.com' -Method HEAD"
        $result = ExecuteWebCommand -command $command
        $result.Error.FullyQualifiedErrorId | Should Be "WebCmdletWebResponseException,Microsoft.PowerShell.Commands.InvokeRestMethodCommand"

        # validate that no exception is thrown for URI with expired certificate when using -SkipCertificateCheck option
        $command = "Invoke-RestMethod -Uri 'https://expired.badssl.com' -SkipCertificateCheck -Method HEAD"
        $result = ExecuteWebCommand -command $command
        $result.Error | Should BeNullOrEmpty
    }

    It "Validate Invoke-RestMethod handles missing Content-Type in response header" {

        #Validate that exception is not thrown when response headers are missing Content-Type.
        $command = "Invoke-RestMethod -Uri 'http://httpbin.org/response-headers?Content-Type='"
        $result = ExecuteWebCommand -command $command
        $result.Error | Should BeNullOrEmpty
    }

    It "Validate Invoke-RestMethod StandardMethod and CustomMethod parameter sets" {

        $errorId = "AmbiguousParameterSet,Microsoft.PowerShell.Commands.InvokeRestMethodCommand"
        { Invoke-RestMethod -Uri 'http://http.lee.io/method' -Method GET -CustomMethod TEST } | ShouldBeErrorId $errorId
    }

    It "Validate CustomMethod method is used" {
        
        $command = "Invoke-RestMethod -Uri 'http://http.lee.io/method' -CustomMethod TEST"
        $result = ExecuteWebCommand -command $command
        $result.Error | Should BeNullOrEmpty
        $result.Output.output.method | Should Be "TEST"
    }

    It "Validate Invoke-RestMethod default ContentType for CustomMethod POST" {
        
        $command = "Invoke-RestMethod -Uri 'http://httpbin.org/post' -CustomMethod POST -Body 'testparam=testvalue'"
        $result = ExecuteWebCommand -command $command
        $result.Output.form.testparam | Should Be "testvalue"
    }

    It "Validate Invoke-RestMethod body is converted to query params for CustomMethod GET" {
        
        $command = "Invoke-RestMethod -Uri 'http://httpbin.org/get' -CustomMethod GET -Body @{'testparam'='testvalue'}"
        $result = ExecuteWebCommand -command $command
        $result.Output.args.testparam | Should Be "testvalue"
    }

    It "Invoke-RestMethod supports request that returns plain text response." {

        $command = "Invoke-RestMethod -Uri 'http://httpbin.org/encoding/utf8'"
        $result = ExecuteWebCommand -command $command
        $result.Error | Should BeNullOrEmpty
    }

    It "Validate Invoke-RestMethod returns HTTP errors in exception" {

        $command = "Invoke-RestMethod -Uri http://httpbin.org/status/418"
        $result = ExecuteWebCommand -command $command

        $result.Error.ErrorDetails.Message | Should Match "\-=\[ teapot \]"
        $result.Error.Exception | Should BeOfType Microsoft.PowerShell.Commands.HttpResponseException
        $result.Error.Exception.Response.StatusCode | Should Be 418
        $result.Error.Exception.Response.ReasonPhrase | Should Be "I'm a teapot"
        $result.Error.Exception.Message | Should Match ": 418 \(I'm a teapot\)\."
        $result.Error.FullyQualifiedErrorId | Should Be "WebCmdletWebResponseException,Microsoft.PowerShell.Commands.InvokeRestMethodCommand"
    }

    It "Validate Invoke-RestMethod returns native HTTPS error message in exception" {

        $command = "Invoke-RestMethod -Uri https://incomplete.chain.badssl.com"
        $result = ExecuteWebCommand -command $command

        # need to check against inner exception since Linux and Windows uses different HTTP client libraries so errors aren't the same
        $result.Error.ErrorDetails.Message | Should Match $result.Error.Exception.InnerException.Message
        $result.Error.FullyQualifiedErrorId | Should Be "WebCmdletWebResponseException,Microsoft.PowerShell.Commands.InvokeRestMethodCommand"
    }

    BeforeEach {
        if ($env:http_proxy) {
            $savedHttpProxy = $env:http_proxy
            $copiedHttpProxy = $true
        }

        if ($env:https_proxy) {
            $savedHttpsProxy = $env:https_proxy
            $copiedHttpsProxy = $true
        }
    }

    AfterEach {
        if ($copiedHttpProxy) {
            $env:http_proxy = $savedHttpProxy
        } else {
            $env:http_proxy = $null
        }

        if ($copiedHttpsProxy) {
            $env:https_proxy = $savedHttpsProxy
        } else {
            $env:https_proxy = $null
        }
    }
}

Describe "Validate Invoke-WebRequest and Invoke-RestMethod -InFile" -Tags "Feature" {

    Context "InFile parameter negative tests" {

        $testCases = @(
#region INVOKE-WEBREQUEST
            @{
                Name = 'Validate error for Invoke-WebRequest -InFile ""'
                ScriptBlock = {Invoke-WebRequest -Uri http://httpbin.org/post -Method Post -InFile ""}
                ExpectedFullyQualifiedErrorId = 'WebCmdletInFileNotFilePathException,Microsoft.PowerShell.Commands.InvokeWebRequestCommand'
            }

            @{
                Name = 'Validate error for Invoke-WebRequest -InFile'
                ScriptBlock = {Invoke-WebRequest -Uri http://httpbin.org/post -Method Post -InFile}
                ExpectedFullyQualifiedErrorId = 'MissingArgument,Microsoft.PowerShell.Commands.InvokeWebRequestCommand'
            }

            @{
                Name = "Validate error for Invoke-WebRequest -InFile  $TestDrive\content.txt"
                ScriptBlock = {Invoke-WebRequest -Uri http://httpbin.org/post -Method Post -InFile  $TestDrive\content.txt}
                ExpectedFullyQualifiedErrorId = 'PathNotFound,Microsoft.PowerShell.Commands.InvokeWebRequestCommand'
            }
#endregion

#region INVOKE-RESTMETHOD
            @{
                Name = "Validate error for Invoke-RestMethod -InFile ''"
                ScriptBlock = {Invoke-RestMethod -Uri http://httpbin.org/post -Method Post -InFile ''}
                ExpectedFullyQualifiedErrorId = 'WebCmdletInFileNotFilePathException,Microsoft.PowerShell.Commands.InvokeRestMethodCommand'
            }

            @{
                Name = "Validate error for Invoke-RestMethod -InFile <null>"
                ScriptBlock = {Invoke-RestMethod -Uri http://httpbin.org/post -Method Post -InFile}
                ExpectedFullyQualifiedErrorId = 'MissingArgument,Microsoft.PowerShell.Commands.InvokeRestMethodCommand'
            }

            @{
                Name = "Validate error for Invoke-RestMethod -InFile  $TestDrive\content.txt"
                ScriptBlock = {Invoke-RestMethod -Uri http://httpbin.org/post -Method Post -InFile $TestDrive\content.txt}
                ExpectedFullyQualifiedErrorId = 'PathNotFound,Microsoft.PowerShell.Commands.InvokeRestMethodCommand'
            }
#endregion
        )

        It "<Name>" -TestCases $testCases {
            param ($scriptblock, $expectedFullyQualifiedErrorId)

            try
            {
                & $scriptblock
                throw "No Exception!"
            }
            catch
            {
                $_.FullyQualifiedErrorId | should be $ExpectedFullyQualifiedErrorId
            }
        }
    }

    Context "InFile parameter positive tests" {

        BeforeAll {
            $filePath = Join-Path $TestDrive test.txt
            New-Item -Path $filePath -Value "hello" -ItemType File -Force
        }

        It "Invoke-WebRequest -InFile" {
            $result = Invoke-WebRequest -InFile $filePath  -Uri http://httpbin.org/post -Method Post
            $content = $result.Content | ConvertFrom-Json
            $content.form | Should Match "hello"
        }

        It "Invoke-RestMethod -InFile" {
            $result = Invoke-RestMethod -InFile $filePath  -Uri http://httpbin.org/post -Method Post
            $result.form | Should Match "hello"
        }
    }
}

Describe "Web cmdlets tests using the cmdlet's aliases" -Tags "CI" {

    function SearchEngineIsOnline
    {
        param (
            [ValidateNotNullOrEmpty()]
            $webAddress
        )
        $ping = new-object System.Net.NetworkInformation.Ping
        $sendPing = $ping.SendPingAsync($webAddress)
        return ($sendPing.Result.Status -eq "Success")
    }

    # Make sure either www.bing.com or www.google.com are online to send a request.
    $endPointToUse = $null
    foreach ($uri in @("www.bing.com", "www.google.com"))
    {
        if (SearchEngineIsOnline $uri)
        {
            $endPointToUse = $uri
            break
        }
    }

    # If neither www.bing.com nor www.google.com are online, then skip the tests.
    $skipTests = ($endPointToUse -eq $null)
    $finalUri = $endPointToUse + "?q=how+many+feet+in+a+mile"

    It "Execute Invoke-WebRequest --> 'iwr -URI $finalUri'" -Skip:$skipTests {
        $result = iwr -URI $finalUri -TimeoutSec 5
        $result.StatusCode | Should Be "200"
        $result.Links | Should Not Be $null
    }

    It "Execute Invoke-RestMethod --> 'irm -URI $finalUri'" -Skip:$skipTests {
        $result = irm -URI $finalUri -TimeoutSec 5
        foreach ($word in @("200", "how", "many", "feet", "in", "mile"))
        {
            $result | Should Match $word
        }
    }
}
