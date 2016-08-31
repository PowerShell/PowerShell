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
}
