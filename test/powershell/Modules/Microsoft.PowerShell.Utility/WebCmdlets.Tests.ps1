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

function ExecuteRedirectRequest
{
    param (
        [Parameter(Mandatory)]
        [string]
        $uri,

        [ValidateSet('Invoke-WebRequest', 'Invoke-RestMethod')]
        [string] $Cmdlet = 'Invoke-WebRequest',

        [ValidateSet('POST', 'GET')]
        [string] $Method = 'GET',

        [switch] $PreserveAuthorizationOnRedirect
    )
    $result = [PSObject]@{Output = $null; Error = $null; Content = $null}

    try
    {
        $headers = @{"Authorization" = "test"}
        if ($Cmdlet -eq 'Invoke-WebRequest')
        {
            $result.Output = Invoke-WebRequest -Uri $uri -TimeoutSec 5 -Headers $headers -PreserveAuthorizationOnRedirect:$PreserveAuthorizationOnRedirect.IsPresent -Method $Method
            $result.Content = $result.Output.Content | ConvertFrom-Json
        }
        else
        {
            $result.Output = Invoke-RestMethod -Uri $uri -TimeoutSec 5 -Headers $headers -PreserveAuthorizationOnRedirect:$PreserveAuthorizationOnRedirect.IsPresent -Method $Method
            # NOTE: $result.Output should already be a PSObject (Invoke-RestMethod converts the returned json automatically)
            # so simply reference $result.Output
            $result.Content = $result.Output
        }
    }
    catch
    {
        $result.Error = $_
    }

    return $result
}

# This function calls either Invoke-WebRequest or Invoke-RestMethod with the given uri
# using the custum headers and the  optional SkipHeaderValidation switch.
function ExecuteRequestWithCustomHeaders
{
    param (
        [Parameter(Mandatory)]
        [string]
        $Uri,

        [ValidateSet('Invoke-WebRequest', 'Invoke-RestMethod')]
        [string] $Cmdlet = 'Invoke-WebRequest',

        [Parameter(Mandatory)]
        [ValidateNotNull()]
        [Hashtable] $Headers,

        [switch] $SkipHeaderValidation
    )
    $result = [PSObject]@{Output = $null; Error = $null; Content = $null}

    try
    {
        if ($Cmdlet -eq 'Invoke-WebRequest')
        {
            $result.Output = Invoke-WebRequest -Uri $Uri -TimeoutSec 5 -Headers $Headers -SkipHeaderValidation:$SkipHeaderValidation.IsPresent
            $result.Content = $result.Output.Content | ConvertFrom-Json
        }
        else
        {
            $result.Output = Invoke-RestMethod -Uri $Uri -TimeoutSec 5 -Headers $Headers -SkipHeaderValidation:$SkipHeaderValidation.IsPresent
            # NOTE: $result.Output should already be a PSObject (Invoke-RestMethod converts the returned json automatically)
            # so simply reference $result.Output
            $result.Content = $result.Output
        }
    }
    catch
    {
        $result.Error = $_
    }

    return $result
}

# This function calls either Invoke-WebRequest or Invoke-RestMethod with the given uri
# using the custom UserAgent and the  optional SkipHeaderValidation switch.
function ExecuteRequestWithCustomUserAgent {
    param (
        [Parameter(Mandatory)]
        [string]
        $Uri,

        [ValidateSet('Invoke-WebRequest', 'Invoke-RestMethod')]
        [string] $Cmdlet = 'Invoke-WebRequest',

        [Parameter(Mandatory)]
        [ValidateNotNull()]
        [string] $UserAgent,

        [switch] $SkipHeaderValidation
    )
    $result = [PSObject]@{Output = $null; Error = $null; Content = $null}

    try {
        $Params = @{
            Uri                  = $Uri
            TimeoutSec           = 5
            UserAgent            = $UserAgent
            SkipHeaderValidation = $SkipHeaderValidation.IsPresent
        }
        if ($Cmdlet -eq 'Invoke-WebRequest') {
            $result.Output = Invoke-WebRequest @Params
            $result.Content = $result.Output.Content | ConvertFrom-Json
        }
        else {
            $result.Output = Invoke-RestMethod @Params
            # NOTE: $result.Output should already be a PSObject (Invoke-RestMethod converts the returned json automatically)
            # so simply reference $result.Output
            $result.Content = $result.Output
        }
    }
    catch {
        $result.Error = $_
    }

    return $result
}

# This function calls Invoke-WebRequest with the given uri
function ExecuteWebRequest
{
    param (
        [Parameter(Mandatory)]
        [string]
        $Uri,

        [switch] $UseBasicParsing
    )
   $result = [PSObject]@{Output = $null; Error = $null; Content = $null}

    try
    {
        $result.Output = Invoke-WebRequest -Uri $Uri -TimeoutSec 5 -UseBasicParsing:$UseBasicParsing.IsPresent
        $result.Content = $result.Output.Content
    }
    catch
    {
        $result.Error = $_
    }

    return $result
}

function GetSelfSignedCert {
    <#    
        .NOTES
            This certificate is not issued for any specific Key Usage
            It cannot be used for any service that requires a specific key usage
            It can be used for SSL/TLS Client Authentication
    #>
    $PfxBase64 = @'
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
hqPP3QQIlSXpfTECuB4CAggA
'@
    $Bytes = [System.Convert]::FromBase64String($PfxBase64)
    [System.Security.Cryptography.X509Certificates.X509Certificate2]::new($Bytes)
}

<#
    Defines the list of redirect codes to test as well as the
    expected Method when the redirection is handled.
    See https://msdn.microsoft.com/en-us/library/windows/apps/system.net.httpstatuscode(v=vs.105).aspx
    for additonal details.
#>
$redirectTests = @(
    @{redirectType = 'MultipleChoices'; redirectedMethod='POST'}
    @{redirectType = 'Ambiguous'; redirectedMethod='POST'} # Synonym for MultipleChoices

    @{redirectType = 'Moved'; redirectedMethod='GET'}
    @{redirectType = 'MovedPermanently'; redirectedMethod='GET'} # Synonym for Moved

    @{redirectType = 'Found'; redirectedMethod='GET'}
    @{redirectType = 'Redirect'; redirectedMethod='GET'} # Synonym for Found

    @{redirectType = 'redirectMethod'; redirectedMethod='GET'}
    @{redirectType = 'SeeOther'; redirectedMethod='GET'} # Synonym for RedirectMethod

    @{redirectType = 'TemporaryRedirect'; redirectedMethod='GET'}
    @{redirectType = 'RedirectKeepVerb'; redirectedMethod='GET'} # Synonym for TemporaryRedirect
)

Describe "Invoke-WebRequest tests" -Tags "Feature" {

    BeforeAll {
        $response = Start-HttpListener -Port 8080
    }

    AfterAll {
        $null = Stop-HttpListener -Port 8080
        $response.PowerShell.Dispose()
    }

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
        @{ proxy_address = "http://localhost:9"; name = 'http_proxy'; protocol = 'http' }
        @{ proxy_address = "http://localhost:9"; name = 'https_proxy'; protocol = 'https' }
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

    It "Validate Invoke-WebRequest returns empty RelationLink property if there is no Link Header" {

        $command = "Invoke-WebRequest -Uri http://localhost:8080/PowerShell?test=response"
        $result = ExecuteWebCommand -command $command

        $result.Output.RelationLink.Count | Should Be 0
    }

    It "Validate Invoke-WebRequest returns valid RelationLink property with absolute uris if Link Header is present" {

        $command = "Invoke-WebRequest -Uri 'http://localhost:8080/PowerShell?test=linkheader&maxlinks=5'"
        $result = ExecuteWebCommand -command $command
        $result.Output.RelationLink.Count | Should BeExactly 2
        $result.Output.RelationLink["next"] | Should BeExactly "http://localhost:8080/PowerShell?test=linkheader&maxlinks=5&linknumber=2"
        $result.Output.RelationLink["last"] | Should BeExactly "http://localhost:8080/PowerShell?test=linkheader&maxlinks=5&linknumber=5"
    }

    It "Validate Invoke-WebRequest quietly ignores invalid Link Headers in RelationLink property: <type>" -TestCases @(
        @{ type = "noUrl" }
        @{ type = "malformed" }
        @{ type = "noRel" }
    ) {
        param($type)
        $command = "Invoke-WebRequest -Uri 'http://localhost:8080/PowerShell?test=linkheader&type=$type'"
        $result = ExecuteWebCommand -command $command
        $result.Output.RelationLink.Count | Should BeExactly 1
        $result.Output.RelationLink["last"] | Should BeExactly "http://localhost:8080/PowerShell?test=linkheader&maxlinks=3&linknumber=3"
    }

    #region Redirect tests

    It "Validates Invoke-WebRequest with -PreserveAuthorizationOnRedirect preserves the authorization header on redirect: <redirectType> <redirectedMethod>" -TestCases $redirectTests {
        param($redirectType, $redirectedMethod)

        $response = ExecuteRedirectRequest -Uri "http://localhost:8080/PowerShell?test=redirect&type=$redirectType" -PreserveAuthorizationOnRedirect

        $response.Error | Should BeNullOrEmpty
        # ensure Authorization header has been preserved.
        $response.Content.Headers -contains "Authorization" | Should Be $true
    }


    It "Validates Invoke-WebRequest preserves the authorization header on multiple redirects: <redirectType>" -TestCases $redirectTests {
        param($redirectType)

        $response = ExecuteRedirectRequest -Uri "http://localhost:8080/PowerShell?test=redirect&type=$redirectType&multiredirect=true" -PreserveAuthorizationOnRedirect

        $response.Error | Should BeNullOrEmpty
        # ensure Authorization header was stripped
        $response.Content.Headers -contains "Authorization" | Should Be $true
    }

    It "Validates Invoke-WebRequest strips the authorization header on various redirects: <redirectType>" -TestCases $redirectTests {
        param($redirectType)

        $response = ExecuteRedirectRequest -Uri "http://localhost:8080/PowerShell?test=redirect&type=$redirectType"

        $response.Error | Should BeNullOrEmpty
        # ensure user-agent is present (i.e., no false positives )
        $response.Content.Headers -contains "User-Agent" | Should Be $true
        # ensure Authorization header has been removed.
        $response.Content.Headers -contains "Authorization" | Should Be $false
    }

    # NOTE: Only testing redirection of POST -> GET for unique underlying values of HttpStatusCode.
    # Some names overlap in underlying value.
    It "Validates Invoke-WebRequest strips the authorization header redirects and switches from POST to GET when it handles the redirect: <redirectType> <redirectedMethod>" -TestCases $redirectTests {
        param($redirectType, $redirectedMethod)

        $response = ExecuteRedirectRequest -Uri "http://localhost:8080/PowerShell?test=redirect&type=$redirectType" -Method 'POST'

        $response.Error | Should BeNullOrEmpty
        # ensure user-agent is present (i.e., no false positives )
        $response.Content.Headers -contains "User-Agent" | Should Be $true
        # ensure Authorization header has been removed.
        $response.Content.Headers -contains "Authorization" | Should Be $false
        # ensure POST was changed to GET for selected redirections and remains as POST for others.
        $response.Content.HttpMethod | Should Be $redirectedMethod
    }

    #endregion Redirect tests

    #region SkipHeaderVerification Tests

    It "Verifies Invoke-WebRequest default header handling with no errors" {
        $headers = @{"If-Match" = "*"}
        $response = ExecuteRequestWithCustomHeaders -Uri "http://localhost:8080/PowerShell?test=echo" -headers $headers

        $response.Error | Should BeNullOrEmpty
        $response.Content.Headers -contains "If-Match" | Should Be $true
    }

    It "Verifies Invoke-WebRequest default header handling reports an error is returned for an invalid If-Match header value" {
        $headers = @{"If-Match" = "12345"}
        $response = ExecuteRequestWithCustomHeaders -Uri "http://localhost:8080/PowerShell?test=echo" -headers $headers

        $response.Error | Should Not BeNullOrEmpty
        $response.Error.FullyQualifiedErrorId | Should Be "System.FormatException,Microsoft.PowerShell.Commands.InvokeWebRequestCommand"
        $response.Error.Exception.Message | Should Be "The format of value '12345' is invalid."
    }

    It "Verifies Invoke-WebRequest header handling does not report an error when using -SkipHeaderValidation" {
        $headers = @{"If-Match" = "12345"}
        $response = ExecuteRequestWithCustomHeaders -Uri "http://localhost:8080/PowerShell?test=echo" -headers $headers -SkipHeaderValidation

        $response.Error | Should BeNullOrEmpty
        $response.Content.Headers -contains "If-Match" | Should Be $true
    }

    It "Verifies Invoke-WebRequest default UserAgent handling with no errors" {
        $UserAgent = [Microsoft.PowerShell.Commands.PSUserAgent]::InternetExplorer
        $response = ExecuteRequestWithCustomUserAgent -Uri "http://localhost:8080/PowerShell?test=echo" -UserAgent $UserAgent -Cmdlet "Invoke-WebRequest"

        $response.Error | Should BeNullOrEmpty
        $Pattern = [regex]::Escape($UserAgent)
        $response.Content.UserAgent | Should Match $Pattern
    }

    It "Verifies Invoke-WebRequest default UserAgent handling reports an error is returned for an invalid UserAgent value" {
        $UserAgent = 'Invalid:Agent'
        $response = ExecuteRequestWithCustomUserAgent -Uri "http://localhost:8080/PowerShell?test=echo" -UserAgent $UserAgent  -Cmdlet "Invoke-WebRequest"

        $response.Error | Should Not BeNullOrEmpty
        $response.Error.FullyQualifiedErrorId | Should Be "System.FormatException,Microsoft.PowerShell.Commands.InvokeWebRequestCommand"
        $response.Error.Exception.Message | Should Be "The format of value 'Invalid:Agent' is invalid."
    }

    It "Verifies Invoke-WebRequest UserAgent handling does not report an error when using -SkipHeaderValidation" {
        $UserAgent = 'Invalid:Agent'
        $response = ExecuteRequestWithCustomUserAgent -Uri "http://localhost:8080/PowerShell?test=echo" -UserAgent $UserAgent  -SkipHeaderValidation -Cmdlet "Invoke-WebRequest"

        $response.Error | Should BeNullOrEmpty
        $Pattern = [regex]::Escape($UserAgent)
        $response.Content.UserAgent | Should Match $Pattern
    }

    #endregion SkipHeaderVerification Tests

    #region Certificate Authentication Tests

    # Test pending creation of native test solution 
    # https://github.com/PowerShell/PowerShell/issues/4609
    It "Verifies Invoke-WebRequest Certificate Authentication Fails without -Certificate" -Pending {
        $command = 'Invoke-WebRequest https://prod.idrix.eu/secure/'
        $result = ExecuteWebCommand -command $command
        ValidateResponse -response $result

        $result.Output | Should Match ([regex]::Escape('Error: No SSL client certificate presented'))
    }

    # Test pending creation of native test solution 
    # https://github.com/PowerShell/PowerShell/issues/4609
    It "Verifies Invoke-WebRequest  Certificate Authentication Successful with -Certificate" -Pending {
        $Certificate = GetSelfSignedCert
        $command = 'Invoke-WebRequest https://prod.idrix.eu/secure/ -Certificate $Certificate'
        $result = ExecuteWebCommand -command $command
        ValidateResponse -response $result

        $result.Output.Content | Should Match ([regex]::Escape('SSL Authentication OK!'))
    }

    #endregion Certificate Authentication Tests

    #region charset encoding tests

    Context  "BasicHtmlWebResponseObject Encoding tests" {
        It "Verifies Invoke-WebRequest detects charset meta value when the ContentType header does not define it." {
            $output = '<html><head><meta charset="Unicode"></head></html>'
            $expectedEncoding = [System.Text.Encoding]::GetEncoding('Unicode')
            $response = ExecuteWebRequest -Uri "http://localhost:8080/PowerShell?test=response&output=$output" -UseBasicParsing

            $response.Error | Should BeNullOrEmpty
            $response.Output.Encoding.EncodingName | Should Be $expectedEncoding.EncodingName
            $response.Output | Should BeOfType 'Microsoft.PowerShell.Commands.BasicHtmlWebResponseObject'
        }

        It "Verifies Invoke-WebRequest detects charset meta value when newlines are encountered in the element." {
            $output = @'
<html>
    <head>
        <meta
            charset="Unicode"
            >
    </head>
</html>
'@
            $expectedEncoding = [System.Text.Encoding]::GetEncoding('Unicode')
            $response = ExecuteWebRequest -Uri "http://localhost:8080/PowerShell?test=response&output=$output" -UseBasicParsing

            $response.Error | Should BeNullOrEmpty
            $response.Output.Encoding.EncodingName | Should Be $expectedEncoding.EncodingName
            $response.Output | Should BeOfType 'Microsoft.PowerShell.Commands.BasicHtmlWebResponseObject'
        }

        It "Verifies Invoke-WebRequest detects charset meta value when the attribute value is unquoted." {
            $output = '<html><head><meta charset = Unicode></head></html>'
            $expectedEncoding = [System.Text.Encoding]::GetEncoding('Unicode')
            $response = ExecuteWebRequest -Uri "http://localhost:8080/PowerShell?test=response&output=$output" -UseBasicParsing

            $response.Error | Should BeNullOrEmpty
            $response.Output.Encoding.EncodingName | Should Be $expectedEncoding.EncodingName
            $response.Output | Should BeOfType 'Microsoft.PowerShell.Commands.BasicHtmlWebResponseObject'
        }

        It "Verifies Invoke-WebRequest detects http-equiv charset meta value when the ContentType header does not define it." {
            $output = @'
<html><head>
<meta http-equiv="content-type" content="text/html; charset=Unicode">
</head>
</html>
'@
            $expectedEncoding = [System.Text.Encoding]::GetEncoding('Unicode')
            $response = ExecuteWebRequest -Uri "http://localhost:8080/PowerShell?test=response&output=$output" -UseBasicParsing

            $response.Error | Should BeNullOrEmpty
            $response.Output.Encoding.EncodingName | Should Be $expectedEncoding.EncodingName
            $response.Output | Should BeOfType 'Microsoft.PowerShell.Commands.BasicHtmlWebResponseObject'
        }

        It "Verifies Invoke-WebRequest detects http-equiv charset meta value newlines are encountered in the element." {
            $output = @'
<html><head>
<meta
    http-equiv="content-type"
    content="text/html; charset=Unicode">
</head>
</html>
'@
            $expectedEncoding = [System.Text.Encoding]::GetEncoding('Unicode')
            $response = ExecuteWebRequest -Uri "http://localhost:8080/PowerShell?test=response&output=$output" -UseBasicParsing

            $response.Error | Should BeNullOrEmpty
            $response.Output.Encoding.EncodingName | Should Be $expectedEncoding.EncodingName
            $response.Output | Should BeOfType 'Microsoft.PowerShell.Commands.BasicHtmlWebResponseObject'
        }

        It "Verifies Invoke-WebRequest ignores meta charset value when Content-Type header defines it." {
            $output = '<html><head><meta charset="utf-32"></head></html>'
            # NOTE: meta charset should be ignored
            $expectedEncoding = [System.Text.Encoding]::UTF8
            $response = ExecuteWebRequest -Uri "http://localhost:8080/PowerShell?test=response&contenttype=text/html; charset=utf-8&output=$output" -UseBasicParsing

            $response.Error | Should BeNullOrEmpty
            $response.Output.Encoding.EncodingName | Should Be $expectedEncoding.EncodingName
            $response.Output | Should BeOfType 'Microsoft.PowerShell.Commands.BasicHtmlWebResponseObject'
        }

        It "Verifies Invoke-WebRequest honors non-utf8 charsets in the Content-Type header" {
            $output = '<html><head><meta charset="utf-32"></head></html>'
            # NOTE: meta charset should be ignored
            $expectedEncoding = [System.Text.Encoding]::GetEncoding('utf-16')
            $response = ExecuteWebRequest -Uri "http://localhost:8080/PowerShell?test=response&contenttype=text/html; charset=utf-16&output=$output" -UseBasicParsing

            $response.Error | Should BeNullOrEmpty
            $response.Output.Encoding.EncodingName | Should Be $expectedEncoding.EncodingName
            $response.Output | Should BeOfType 'Microsoft.PowerShell.Commands.BasicHtmlWebResponseObject'
        }

        It "Verifies Invoke-WebRequest defaults to iso-8859-1 when an unsupported/invalid charset is declared" {
            $output = '<html><head><meta charset="invalid"></head></html>'
            $expectedEncoding = [System.Text.Encoding]::GetEncoding('iso-8859-1')
            $response = ExecuteWebRequest -Uri "http://localhost:8080/PowerShell?test=response&contenttype=text/html&output=$output" -UseBasicParsing

            $response.Error | Should BeNullOrEmpty
            $response.Output.Encoding.EncodingName | Should Be $expectedEncoding.EncodingName
            $response.Output | Should BeOfType 'Microsoft.PowerShell.Commands.BasicHtmlWebResponseObject'
        }

        It "Verifies Invoke-WebRequest defaults to iso-8859-1 when an unsupported/invalid charset is declared using http-equiv" {
            $output = @'
<html><head>
<meta http-equiv="content-type" content="text/html; charset=Invalid">
</head>
</html>
'@
            $expectedEncoding = [System.Text.Encoding]::GetEncoding('iso-8859-1')
            $response = ExecuteWebRequest -Uri "http://localhost:8080/PowerShell?test=response&contenttype=text/html&output=$output" -UseBasicParsing

            $response.Error | Should BeNullOrEmpty
            $response.Output.Encoding.EncodingName | Should Be $expectedEncoding.EncodingName
            $response.Output | Should BeOfType 'Microsoft.PowerShell.Commands.BasicHtmlWebResponseObject'
        }
    }

    Context  "HtmlWebResponseObject Encoding" {
        # these tests are dependent on https://github.com/PowerShell/PowerShell/issues/2867
        # Currently, all paths return BasicHtmlWebResponseObject
        It "Verifies Invoke-WebRequest detects charset meta value when the ContentType header does not define it." -Pending {
            $output = '<html><head><meta charset="Unicode"></head></html>'
            $expectedEncoding = [System.Text.Encoding]::GetEncoding('Unicode')
            $response = ExecuteWebRequest -Uri "http://localhost:8080/PowerShell?test=response&output=$output"

            $response.Error | Should BeNullOrEmpty
            $response.Output.Encoding.EncodingName | Should Be $expectedEncoding.EncodingName
            # Update to test for HtmlWebResponseObject when mshtl dependency has been resolved.
            $response.Output | Should BeOfType 'Microsoft.PowerShell.Commands.HtmlWebResponseObject'
        }

        It "Verifies Invoke-WebRequest detects charset meta value when newlines are encountered in the element." -Pending {
            $output = @'
<html>
    <head>
        <meta
            charset="Unicode"
            >
    </head>
</html>
'@
            $expectedEncoding = [System.Text.Encoding]::GetEncoding('Unicode')
            $response = ExecuteWebRequest -Uri "http://localhost:8080/PowerShell?test=response&output=$output"

            $response.Error | Should BeNullOrEmpty
            $response.Output.Encoding.EncodingName | Should Be $expectedEncoding.EncodingName
            $response.Output | Should BeOfType 'Microsoft.PowerShell.Commands.HtmlWebResponseObject'
        }

        It "Verifies Invoke-WebRequest ignores meta charset value when Content-Type header defines it." -Pending {
            $output = '<html><head><meta charset="utf-16"></head></html>'
            # NOTE: meta charset should be ignored
            $expectedEncoding = [System.Text.Encoding]::UTF8
            # Update to test for HtmlWebResponseObject when mshtl dependency has been resolved.
            $response = ExecuteWebRequest -Uri "http://localhost:8080/PowerShell?test=response&contenttype=text/html; charset=utf-8&output=$output"

            $response.Error | Should BeNullOrEmpty
            $response.Output.Encoding.EncodingName | Should Be $expectedEncoding.EncodingName
            # Update to test for HtmlWebResponseObject when mshtl dependency has been resolved.
            $response.Output | Should BeOfType 'Microsoft.PowerShell.Commands.HtmlWebResponseObject'
        }

        It "Verifies Invoke-WebRequest detects http-equiv charset meta value when the ContentType header does not define it." -Pending {
            $output = @'
<html><head>
<meta http-equiv="content-type" content="text/html; charset=Unicode">
</head>
</html>
'@
            $expectedEncoding = [System.Text.Encoding]::GetEncoding('Unicode')
            $response = ExecuteWebRequest -Uri "http://localhost:8080/PowerShell?test=response&output=$output"

            $response.Error | Should BeNullOrEmpty
            $response.Output.Encoding.EncodingName | Should Be $expectedEncoding.EncodingName
            $response.Output | Should BeOfType 'Microsoft.PowerShell.Commands.HtmlWebResponseObject'
        }

        It "Verifies Invoke-WebRequest detects http-equiv charset meta value newlines are encountered in the element." -Pending {
            $output = @'
<html><head>
<meta
    http-equiv="content-type"
    content="text/html; charset=Unicode">
</head>
</html>
'@
            $expectedEncoding = [System.Text.Encoding]::GetEncoding('Unicode')
            $response = ExecuteWebRequest -Uri "http://localhost:8080/PowerShell?test=response&output=$output"

            $response.Error | Should BeNullOrEmpty
            $response.Output.Encoding.EncodingName | Should Be $expectedEncoding.EncodingName
            $response.Output | Should BeOfType 'Microsoft.PowerShell.Commands.HtmlWebResponseObject'
        }

        It "Verifies Invoke-WebRequest honors non-utf8 charsets in the Content-Type header" -Pending {
            $output = '<html><head><meta charset="utf-32"></head></html>'
            # NOTE: meta charset should be ignored
            $expectedEncoding = [System.Text.Encoding]::GetEncoding('utf-16')
            $response = ExecuteWebRequest -Uri "http://localhost:8080/PowerShell?test=response&contenttype=text/html; charset=utf-16&output=$output"

            $response.Error | Should BeNullOrEmpty
            $response.Output.Encoding.EncodingName | Should Be $expectedEncoding.EncodingName
            # Update to test for HtmlWebResponseObject when mshtl dependency has been resolved.
            $response.Output | Should BeOfType 'Microsoft.PowerShell.Commands.HtmlWebResponseObject'
        }

        It "Verifies Invoke-WebRequest defaults to iso-8859-1 when an unsupported/invalid charset is declared" -Pending {
            $output = '<html><head><meta charset="invalid"></head></html>'
            $expectedEncoding = [System.Text.Encoding]::GetEncoding('iso-8859-1')
            $response = ExecuteWebRequest -Uri "http://localhost:8080/PowerShell?test=response&contenttype=text/html&output=$output"

            $response.Error | Should BeNullOrEmpty
            $response.Output.Encoding.EncodingName | Should Be $expectedEncoding.EncodingName
            # Update to test for HtmlWebResponseObject when mshtl dependency has been resolved.
            $response.Output | Should BeOfType 'Microsoft.PowerShell.Commands.HtmlWebResponseObject'
        }

        It "Verifies Invoke-WebRequest defaults to iso-8859-1 when an unsupported/invalid charset is declared using http-equiv" -Pending {
            $output = @'
<html><head>
<meta http-equiv="content-type" content="text/html; charset=Invalid">
</head>
</html>
'@
            $expectedEncoding = [System.Text.Encoding]::GetEncoding('iso-8859-1')
            $response = ExecuteWebRequest -Uri "http://localhost:8080/PowerShell?test=response&contenttype=text/html&output=$output"

            $response.Error | Should BeNullOrEmpty
            $response.Output.Encoding.EncodingName | Should Be $expectedEncoding.EncodingName
            $response.Output | Should BeOfType 'Microsoft.PowerShell.Commands.HtmlWebResponseObject'
        }
    }

    #endregion charset encoding tests

    #region Content Header Inclusion
    It "Verifies Invoke-WebRequest includes Content headers in Headers property" {
        $uri = "http://localhost:8080/PowerShell?test=response&contenttype=text/plain&output=OK"
        $command = "Invoke-WebRequest -Uri '$uri'"
        $result = ExecuteWebCommand -command $command
        ValidateResponse $result

        $result.Output.Headers.'Content-Type' | Should Be 'text/plain'
        $result.Output.Headers.'Content-Length' | Should Be 2
    }

    It "Verifies Invoke-WebRequest includes Content headers in RawContent property" {
        $uri = "http://localhost:8080/PowerShell?test=response&contenttype=text/plain&output=OK"
        $command = "Invoke-WebRequest -Uri '$uri'"
        $result = ExecuteWebCommand -command $command
        ValidateResponse $result

        $result.Output.RawContent | Should Match ([regex]::Escape('Content-Type: text/plain'))
        $result.Output.RawContent | Should Match ([regex]::Escape('Content-Length: 2'))
    }

    # Test pending due to HttpListener limitation on Linux/macOS
    # https://github.com/PowerShell/PowerShell/pull/4640
    It "Verifies Invoke-WebRequest Supports Multiple response headers with same name" -Pending {
        $headers = @{
            'X-Fake-Header' = 'testvalue01','testvalue02'
        } | ConvertTo-Json -Compress
        $uri = "http://localhost:8080/PowerShell?test=response&contenttype=text/plain&output=OK&headers=$headers"
        $command = "Invoke-WebRequest -Uri '$uri'"
        $result = ExecuteWebCommand -command $command
        ValidateResponse $result

        $result.Output.Headers.'X-Fake-Header'.Count | Should Be 2
        $result.Output.Headers.'X-Fake-Header'.Contains('testvalue01') | Should Be $True
        $result.Output.Headers.'X-Fake-Header'.Contains('testvalue02') | Should Be $True
        $result.Output.RawContent | Should Match ([regex]::Escape('X-Fake-Header: testvalue01'))
        $result.Output.RawContent | Should Match ([regex]::Escape('X-Fake-Header: testvalue02'))
    }

    #endregion Content Header Inclusion

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

    BeforeAll {
        $response = Start-HttpListener -Port 8081
    }

    AfterAll {
        $null = Stop-HttpListener -Port 8081
        $response.PowerShell.Dispose()
    }

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
        $result.Output.headers.Host | Should Match "httpbin.org"
        $result.Output.headers.'User-Agent' | Should Match "WindowsPowerShell"
    }

    It "Validate Invoke-RestMethod -DisableKeepAlive" {

        # Operation options
        $command = "Invoke-RestMethod -Uri 'http://httpbin.org/get' -TimeoutSec 5 -DisableKeepAlive"

        $result = ExecuteWebCommand -command $command

        # Validate response
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
        @{ proxy_address = "http://localhost:9"; name = 'http_proxy'; protocol = 'http' }
        @{ proxy_address = "http://localhost:9"; name = 'https_proxy'; protocol = 'https' }
    )

    It "Validate Invoke-RestMethod error with -Proxy option - '<name>'" -TestCases $testCase {
        param($proxy_address, $name, $protocol)

        $command = "Invoke-RestMethod -Uri '${protocol}://httpbin.org/' -Proxy '${proxy_address}'"

        $result = ExecuteWebCommand -command $command
        $result.Error.FullyQualifiedErrorId | Should Be "WebCmdletWebResponseException,Microsoft.PowerShell.Commands.InvokeRestMethodCommand"
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

    It "Validate Invoke-RestMethod -FollowRelLink doesn't fail if no Link Header is present" {

        $command = "Invoke-RestMethod -Uri 'http://localhost:8081/PowerShell?test=response&output=foo' -FollowRelLink"
        $result = ExecuteWebCommand -command $command

        $result.Output | Should BeExactly "foo"
    }

    It "Validate Invoke-RestMethod -FollowRelLink correctly follows all the available relation links" {
        $maxLinks = 5

        $command = "Invoke-RestMethod -Uri 'http://localhost:8081/PowerShell?test=linkheader&maxlinks=$maxlinks' -FollowRelLink"
        $result = ExecuteWebCommand -command $command

        $result.Output.output.Count | Should BeExactly $maxLinks
        1..$maxLinks | ForEach-Object { $result.Output.output[$_ - 1] | Should BeExactly $_ }
    }

    It "Validate Invoke-RestMethod -FollowRelLink correctly limits to -MaximumRelLink" {
        $maxLinks = 10
        $maxLinksToFollow = 6

        $command = "Invoke-RestMethod -Uri 'http://localhost:8081/PowerShell?test=linkheader&maxlinks=$maxlinks' -FollowRelLink -MaximumFollowRelLink $maxLinksToFollow"
        $result = ExecuteWebCommand -command $command

        $result.Output.output.Count | Should BeExactly $maxLinksToFollow
        1..$maxLinksToFollow | ForEach-Object { $result.Output.output[$_ - 1] | Should BeExactly $_ }
    }

    It "Validate Invoke-RestMethod quietly ignores invalid Link Headers if -FollowRelLink is specified: <type>" -TestCases @(
        @{ type = "noUrl" }
        @{ type = "malformed" }
        @{ type = "noRel" }
    ) {
        param($type)
        $command = "Invoke-RestMethod -Uri 'http://localhost:8081/PowerShell?test=linkheader&type=$type' -FollowRelLink"
        $result = ExecuteWebCommand -command $command
        $result.Output.output | Should BeExactly 1
    }

    #region Redirect tests

    It "Validates Invoke-RestMethod with -PreserveAuthorizationOnRedirect preserves the authorization header on redirect: <redirectType> <redirectedMethod>" -TestCases $redirectTests {
        param($redirectType, $redirectedMethod)

        $response = ExecuteRedirectRequest  -Cmdlet 'Invoke-RestMethod' -Uri "http://localhost:8081/PowerShell?test=redirect&type=$redirectType" -PreserveAuthorizationOnRedirect

        $response.Error | Should BeNullOrEmpty
        # ensure Authorization header has been preserved.
        $response.Content.Headers -contains "Authorization" | Should Be $true
    }

    It "Validates Invoke-RestMethod preserves the authorization header on multiple redirects: <redirectType>" -TestCases $redirectTests {
        param($redirectType)

        $response = ExecuteRedirectRequest  -Cmdlet 'Invoke-RestMethod' -Uri "http://localhost:8081/PowerShell?test=redirect&type=$redirectType&multiredirect=true" -PreserveAuthorizationOnRedirect

        $response.Error | Should BeNullOrEmpty
        # ensure Authorization header was stripped
        $response.Content.Headers -contains "Authorization" | Should Be $true
    }

    It "Validates Invoke-RestMethod strips the authorization header on various redirects: <redirectType>" -TestCases $redirectTests {
        param($redirectType)

        $response = ExecuteRedirectRequest  -Cmdlet 'Invoke-RestMethod' -Uri "http://localhost:8081/PowerShell?test=redirect&type=$redirectType"

        $response.Error | Should BeNullOrEmpty
        # ensure user-agent is present (i.e., no false positives )
        $response.Output.Headers -contains "User-Agent" | Should Be $true
        # ensure Authorization header has been removed.
        $response.Content.Headers -contains "Authorization" | Should Be $false
    }

    # NOTE: Only testing redirection of POST -> GET for unique underlying values of HttpStatusCode.
    # Some names overlap in underlying value.
    It "Validates Invoke-RestMethod strips the authorization header redirects and switches from POST to GET when it handles the redirect: <redirectType> <redirectedMethod>" -TestCases $redirectTests {
        param($redirectType, $redirectedMethod)

        $response = ExecuteRedirectRequest  -Cmdlet 'Invoke-RestMethod' -Uri "http://localhost:8081/PowerShell?test=redirect&type=$redirectType" -Method 'POST'

        $response.Error | Should BeNullOrEmpty
        # ensure user-agent is present (i.e., no false positives )
        $response.Content.Headers -contains "User-Agent" | Should Be $true
        # ensure Authorization header has been removed.
        $response.Content.Headers -contains "Authorization" | Should Be $false
        # ensure POST was changed to GET for selected redirections and remains as POST for others.
        $response.Content.HttpMethod | Should Be $redirectedMethod
    }

    #endregion Redirect tests

    #region SkipHeaderVerification tests

    It "Verifies Invoke-RestMethod default header handling with no errors" {
        $headers = @{"If-Match" = "*"}
        $response = ExecuteRequestWithCustomHeaders -Uri "http://localhost:8081/PowerShell?test=echo" -headers $headers -Cmdlet "Invoke-RestMethod"

        $response.Error | Should BeNullOrEmpty
        $response.Content.Headers -contains "If-Match" | Should Be $true
    }

    It "Verifies Invoke-RestMethod default header handling reports an error is returned for an invalid If-Match header value" {
        $headers = @{"If-Match" = "12345"}
        $response = ExecuteRequestWithCustomHeaders -Uri "http://localhost:8081/PowerShell?test=echo" -headers $headers -Cmdlet "Invoke-RestMethod"

        $response.Error | Should Not BeNullOrEmpty
        $response.Error.FullyQualifiedErrorId | Should Be "System.FormatException,Microsoft.PowerShell.Commands.InvokeRestMethodCommand"
        $response.Error.Exception.Message | Should Be "The format of value '12345' is invalid."
    }

    It "Verifies Invoke-RestMethod header handling does not report an error when using -SkipHeaderValidation" {
        $headers = @{"If-Match" = "12345"}
        $response = ExecuteRequestWithCustomHeaders -Uri "http://localhost:8081/PowerShell?test=echo" -headers $headers -SkipHeaderValidation -Cmdlet "Invoke-RestMethod"

        $response.Error | Should BeNullOrEmpty
        $response.Content.Headers -contains "If-Match" | Should Be $true
    }

    It "Verifies Invoke-RestMethod default UserAgent handling with no errors" {
        $UserAgent = [Microsoft.PowerShell.Commands.PSUserAgent]::InternetExplorer
        $response = ExecuteRequestWithCustomUserAgent -Uri "http://localhost:8081/PowerShell?test=echo" -UserAgent $UserAgent -Cmdlet "Invoke-RestMethod"

        $response.Error | Should BeNullOrEmpty
        $Pattern = [regex]::Escape($UserAgent)
        $response.Content.UserAgent | Should Match $Pattern
    }

    It "Verifies Invoke-RestMethod default UserAgent handling reports an error is returned for an invalid UserAgent value" {
        $UserAgent = 'Invalid:Agent'
        $response = ExecuteRequestWithCustomUserAgent -Uri "http://localhost:8081/PowerShell?test=echo" -UserAgent $UserAgent  -Cmdlet "Invoke-RestMethod"

        $response.Error | Should Not BeNullOrEmpty
        $response.Error.FullyQualifiedErrorId | Should Be "System.FormatException,Microsoft.PowerShell.Commands.InvokeRestMethodCommand"
        $response.Error.Exception.Message | Should Be "The format of value 'Invalid:Agent' is invalid."
    }

    It "Verifies Invoke-RestMethod UserAgent handling does not report an error when using -SkipHeaderValidation" {
        $UserAgent = 'Invalid:Agent'
        $response = ExecuteRequestWithCustomUserAgent -Uri "http://localhost:8081/PowerShell?test=echo" -UserAgent $UserAgent  -SkipHeaderValidation -Cmdlet "Invoke-RestMethod"

        $response.Error | Should BeNullOrEmpty
        $Pattern = [regex]::Escape($UserAgent)
        $response.Content.UserAgent | Should Match $Pattern
    }

    #endregion SkipHeaderVerification tests

    #region Certificate Authentication Tests

    # Test pending creation of native test solution 
    # https://github.com/PowerShell/PowerShell/issues/4609
    It "Verifies Invoke-RestMethod Certificate Authentication Fails without -Certificate" -Pending {
        $command = 'Invoke-RestMethod https://prod.idrix.eu/secure/'
        $result = ExecuteWebCommand -command $command

        $result.Output | Should Match ([regex]::Escape('Error: No SSL client certificate presented'))
    }

    # Test pending creation of native test solution 
    # https://github.com/PowerShell/PowerShell/issues/4609
    It "Verifies Invoke-RestMethod Certificate Authentication Successful with -Certificate" -Pending {
        $Certificate = GetSelfSignedCert
        $command = 'Invoke-RestMethod https://prod.idrix.eu/secure/ -Certificate $Certificate'
        $result = ExecuteWebCommand -command $command

        $result.Output | Should Match ([regex]::Escape('SSL Authentication OK!'))
    }

    #endregion Certificate Authentication Tests

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

    BeforeAll {
        $response = Start-HttpListener -Port 8082
    }

    AfterAll {
        $null = Stop-HttpListener -Port 8082
        $response.PowerShell.Dispose()
    }

    It "Execute Invoke-WebRequest" {
        $result = iwr "http://localhost:8082/PowerShell?test=response&output=hello" -TimeoutSec 5
        $result.StatusCode | Should Be "200"
        $result.Content | Should Be "hello"
    }

    It "Execute Invoke-RestMethod" {
        $result = irm "http://localhost:8082/PowerShell?test=response&output={%22hello%22:%22world%22}&contenttype=application/json" -TimeoutSec 5
        $result.Hello | Should Be "world"
    }
}
