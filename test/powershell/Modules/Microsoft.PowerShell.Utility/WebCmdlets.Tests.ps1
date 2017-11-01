#
# Copyright (c) Microsoft Corporation. All rights reserved.
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
        $uri = (Get-WebListenerUrl -Test 'Get')
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
        $uri = (Get-WebListenerUrl -Test 'Get')
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
# using the custum headers and the optional SkipHeaderValidation switch.
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
# using the custom UserAgent and the optional SkipHeaderValidation switch.
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

[string] $verboseEncodingPrefix = 'Content encoding: '
# This function calls Invoke-WebRequest with the given uri and
# parses the verbose output to determine the encoding used for the content.
function ExecuteRestMethod
{
    param
    (
        [Parameter(Mandatory)]
        [string]
        $Uri,

        [switch] $UseBasicParsing
    )
    $result = @{Output = $null; Error = $null; Encoding = $null; Content = $null}
    $verbosePreferenceSave = $VerbosePreference
    $VerbosePreference = 'Continue'
    try
    {

        $verboseFile = Join-Path $TestDrive -ChildPath ExecuteRestMethod.verbose.txt
        $result.Output = Invoke-RestMethod -Uri $Uri -TimeoutSec 5 -UseBasicParsing:$UseBasicParsing.IsPresent -Verbose 4>$verboseFile
        $result.Content = $result.Output

        if (Test-Path -Path $verboseFile)
        {
            $result.Verbose = Get-Content -Path $verboseFile
            foreach ($item in $result.Verbose)
            {
                $line = $item.Trim()
                if ($line.StartsWith($verboseEncodingPrefix))
                {
                    $encodingName = $item.SubString($verboseEncodingPrefix.Length).Trim()
                    $result.Encoding = [System.Text.Encoding]::GetEncoding($encodingName)
                    break
                }
            }
            if ($result.Encoding -eq $null)
            {
                throw "Encoding not found in verbose output. Lines: $($result.Verbose.Count) Content:$($result.Verbose)"
            }
        }

        if ($result.Verbose -eq $null)
        {
            throw "No verbose output was found"
        }
    }
    catch
    {
        $result.Error = $_ | select-object * | Out-String
    }
    finally
    {
        $VerbosePreference = $verbosePreferenceSave
        if (Test-Path -Path $verboseFile)
        {
            Remove-Item -Path $verboseFile -ErrorAction SilentlyContinue
        }
    }

    return $result
}

function GetMultipartBody
{
    param
    (
        [Switch]$String,
        [Switch]$File
    )
    $multipartContent = [System.Net.Http.MultipartFormDataContent]::new()
    if ($String.IsPresent)
    {
        $stringHeader = [System.Net.Http.Headers.ContentDispositionHeaderValue]::new("form-data")
        $stringHeader.Name = "TestString"
        $StringContent = [System.Net.Http.StringContent]::new("TestValue")
        $StringContent.Headers.ContentDisposition = $stringHeader
        $multipartContent.Add($stringContent)
    }
    if ($File.IsPresent)
    {
        $multipartFile = Join-Path $TestDrive 'multipart.txt'
        "TestContent" | Set-Content $multipartFile
        $FileStream = [System.IO.FileStream]::new($multipartFile, [System.IO.FileMode]::Open)
        $fileHeader = [System.Net.Http.Headers.ContentDispositionHeaderValue]::new("form-data")
        $fileHeader.Name = "TestFile"
        $fileHeader.FileName = 'multipart.txt'
        $fileContent = [System.Net.Http.StreamContent]::new($FileStream)
        $fileContent.Headers.ContentDisposition = $fileHeader
        $fileContent.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::Parse("text/plain")
        $multipartContent.Add($fileContent)
    }
    # unary comma required to prevent $multipartContent from being unwrapped/enumerated
    return ,$multipartContent
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

$PendingCertificateTest = $false
# we can't check for Certificate on MacOS and Centos libcurl (currently 7.29.0) returns the following error:
# The handler does not support client authentication certificates with this combination of libcurl (7.29.0) and its SSL backend ("NSS/3.21 Basic ECC")
if ( $IsMacOS ) { $PendingCertificateTest = $true }
if ( test-path /etc/centos-release ) { $PendingCertificateTest = $true }

Describe "Invoke-WebRequest tests" -Tags "Feature" {

    BeforeAll {
        $response = Start-HttpListener -Port 8080
        $WebListener = Start-WebListener
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

    #User-Agent changes on different platforms, so tests should only be run if on the correct platform
    It "Invoke-WebRequest returns Correct User-Agent on MacOSX" -Skip:(!$IsMacOS) {

        $uri = Get-WebListenerUrl -Test 'Get'
        $command = "Invoke-WebRequest -Uri '$uri' -TimeoutSec 5"

        $result = ExecuteWebCommand -command $command
        ValidateResponse -response $result

        # Validate response content
        $jsonContent = $result.Output.Content | ConvertFrom-Json
        $jsonContent.headers.'User-Agent' | Should MatchExactly '.*\(Macintosh;.*\) PowerShell\/\d+\.\d+\.\d+.*'
    }

    It "Invoke-WebRequest returns Correct User-Agent on Linux" -Skip:(!$IsLinux) {

        $uri = Get-WebListenerUrl -Test 'Get'
        $command = "Invoke-WebRequest -Uri '$uri' -TimeoutSec 5"

        $result = ExecuteWebCommand -command $command
        ValidateResponse -response $result

        # Validate response content
        $jsonContent = $result.Output.Content | ConvertFrom-Json
        $jsonContent.headers.'User-Agent' | Should MatchExactly '.*\(Linux;.*\) PowerShell\/\d+\.\d+\.\d+.*'
    }

    It "Invoke-WebRequest returns Correct User-Agent on Windows" -Skip:(!$IsWindows) {

        $uri = Get-WebListenerUrl -Test 'Get'
        $command = "Invoke-WebRequest -Uri '$uri' -TimeoutSec 5"

        $result = ExecuteWebCommand -command $command
        ValidateResponse -response $result

        # Validate response content
        $jsonContent = $result.Output.Content | ConvertFrom-Json
        $jsonContent.headers.'User-Agent' | Should MatchExactly '.*\(Windows NT \d+\.\d*;.*\) PowerShell\/\d+\.\d+\.\d+.*'
    }

    It "Invoke-WebRequest returns headers dictionary" {

        $uri = Get-WebListenerUrl -Test 'Get'
        $command = "Invoke-WebRequest -Uri '$uri' -TimeoutSec 5"

        $result = ExecuteWebCommand -command $command
        ValidateResponse -response $result

        # Validate response content
        $jsonContent = $result.Output.Content | ConvertFrom-Json
        $jsonContent.headers.Host | Should Be $Uri.Authority
    }

    It "Validate Invoke-WebRequest -DisableKeepAlive" {

        # Operation options
        $uri = Get-WebListenerUrl -Test 'Get'
        $command = "Invoke-WebRequest -Uri $uri -TimeoutSec 5 -DisableKeepAlive"

        $result = ExecuteWebCommand -command $command
        ValidateResponse -response $result

        $result.Output.Headers.Connection | Should Be "Close"
    }

    It "Validate Invoke-WebRequest -MaximumRedirection" {

        $uri = Get-WebListenerUrl -Test 'Redirect' -TestValue '3'
        $command = "Invoke-WebRequest -Uri '$uri' -MaximumRedirection 4"

        $result = ExecuteWebCommand -command $command
        ValidateResponse -response $result

        # Validate response content
        $jsonContent = $result.Output.Content | ConvertFrom-Json
        $jsonContent.headers.Host | Should Match $uri.Authority
    }

    It "Validate Invoke-WebRequest error for -MaximumRedirection" {

        $uri = Get-WebListenerUrl -Test 'Redirect' -TestValue '3'
        $command = "Invoke-WebRequest -Uri '$uri' -MaximumRedirection 2"

        $result = ExecuteWebCommand -command $command
        $result.Error.FullyQualifiedErrorId | Should Be "WebCmdletWebResponseException,Microsoft.PowerShell.Commands.InvokeWebRequestCommand"
    }

    It "Invoke-WebRequest supports request that returns page containing UTF-8 data." {

        $uri = Get-WebListenerUrl -Test 'Encoding' -TestValue 'Utf8'
        $command = "Invoke-WebRequest -Uri '$uri'"

        $result = ExecuteWebCommand -command $command
        ValidateResponse -response $result

        $Result.Output.Encoding.BodyName | Should Be 'utf-8'
        $Result.Output.Content | Should Match '⡌⠁⠧⠑ ⠼⠁⠒  ⡍⠜⠇⠑⠹⠰⠎ ⡣⠕⠌'
    }

    It "Invoke-WebRequest validate timeout option" {

        $uri = Get-WebListenerUrl -Test 'Delay' -TestValue '5'
        $command = "Invoke-WebRequest -Uri '$uri' -TimeoutSec 2"

        $result = ExecuteWebCommand -command $command
        $result.Error.FullyQualifiedErrorId | Should Be "System.Threading.Tasks.TaskCanceledException,Microsoft.PowerShell.Commands.InvokeWebRequestCommand"
    }

    It "Validate Invoke-WebRequest error with -Proxy and -NoProxy option" {

        $uri = Get-WebListenerUrl -Test 'Delay' -TestValue '10'
        $command = "Invoke-WebRequest -Uri '$uri' -Proxy 'http://localhost:8080' -NoProxy -TimeoutSec 2"

        $result = ExecuteWebCommand -command $command
        $result.Error.FullyQualifiedErrorId | Should Be "AmbiguousParameterSet,Microsoft.PowerShell.Commands.InvokeWebRequestCommand"
    }

    $testCase = @(
        @{ proxy_address = "http://localhost:9"; name = 'http_proxy'; protocol = 'http' }
        @{ proxy_address = "http://localhost:9"; name = 'https_proxy'; protocol = 'https' }
    )

    It "Validate Invoke-WebRequest error with -Proxy option set - '<name>'" -TestCases $testCase {
        param($proxy_address, $name, $protocol)

        $uri = Get-WebListenerUrl -Test 'Delay' -TestValue '5' -Https:$($protocol -eq 'https')
        $command = "Invoke-WebRequest -Uri '$uri' -TimeoutSec 2 -Proxy '${proxy_address}' -SkipCertificateCheck"

        $result = ExecuteWebCommand -command $command
        $result.Error.FullyQualifiedErrorId | Should Be "System.Threading.Tasks.TaskCanceledException,Microsoft.PowerShell.Commands.InvokeWebRequestCommand"
    }

    It "Validate Invoke-WebRequest error with environment proxy set - '<name>'" -TestCases $testCase {
        param($proxy_address, $name, $protocol)

        # Configure the environment variable.
        New-Item -Name ${name} -Value ${proxy_address} -ItemType Variable -Path Env: -Force

        $uri = Get-WebListenerUrl -Test 'Delay' -TestValue '5' -Https:$($protocol -eq 'https')
        $command = "Invoke-WebRequest -Uri '$uri' -TimeoutSec 2 -SkipCertificateCheck"

        $result = ExecuteWebCommand -command $command
        $result.Error.FullyQualifiedErrorId | Should Be "System.Threading.Tasks.TaskCanceledException,Microsoft.PowerShell.Commands.InvokeWebRequestCommand"
    }

    It "Validate Invoke-WebRequest returns User-Agent where -NoProxy with envirionment proxy set - '<name>'" -TestCases $testCase {
        param($proxy_address, $name, $protocol)

        # Configure the environment variable.
        New-Item -Name ${name} -Value ${proxy_address} -ItemType Variable -Path Env: -Force

        $uri = Get-WebListenerUrl -Test 'Get' -Https:$($protocol -eq 'https')
        $command = "Invoke-WebRequest -Uri '$uri' -TimeoutSec 5 -NoProxy -SkipCertificateCheck"

        $result = ExecuteWebCommand -command $command
        ValidateResponse -response $result

        # Validate response content
        $jsonContent = $result.Output.Content | ConvertFrom-Json
        $jsonContent.headers.Host | Should Be $uri.Authority
    }

    # Perform the following operation for Invoke-WebRequest
    # gzip Returns gzip-encoded data.
    # deflate Returns deflate-encoded data.
    # $dataEncodings = @("Chunked", "Compress", "Deflate", "GZip", "Identity")
    # Note: These are the supported options, but we do not have a web service to test them all.
    It "Invoke-WebRequest supports request that returns <DataEncoding>-encoded data." -TestCases @(
        @{ DataEncoding = "gzip"}
        @{ DataEncoding = "deflate"}
    ) {
        param($dataEncoding)
        $uri = Get-WebListenerUrl -Test 'Compression' -TestValue $dataEncoding
        $command = "Invoke-WebRequest -Uri '$uri'"

        $result = ExecuteWebCommand -command $command
        ValidateResponse -response $result

        # Validate response content
        $result.Output.Headers.'Content-Encoding'[0] | Should BeExactly $dataEncoding
        $jsonContent = $result.Output.Content | ConvertFrom-Json
        $jsonContent.Headers.Host | Should BeExactly $uri.Authority
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

        $uri = Get-WebListenerUrl -Test 'Get'
        $result = ExecuteRequestWithHeaders -cmdletName Invoke-WebRequest -uri $uri
        ValidateResponse -response $result
        $result.Output.Headers.Connection | Should Be "Close"
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
        $uri = Get-WebListenerUrl -Test 'Get'
        $userAgent = "[Microsoft.PowerShell.Commands.PSUserAgent]::$agentName"
        $command = "Invoke-WebRequest -Uri $uri -UserAgent ($userAgent)  -TimeoutSec 5"

        It "Validate Invoke-WebRequest UserAgent. Execute--> $command" {

            $result = ExecuteWebCommand -command $command
            ValidateResponse -response $result

            # Validate response content
            $jsonContent = $result.Output.Content | ConvertFrom-Json
            $jsonContent.headers.Host | Should Be $uri.Authority
            $jsonContent.headers.'User-Agent' | Should Match $expectedAgent
        }
    }

    It "Validate Invoke-WebRequest -OutFile" {

        $uri = Get-WebListenerUrl -Test 'Get'
        $result = ExecuteRequestWithOutFile -cmdletName "Invoke-WebRequest" -uri $uri
        $jsonContent = $result.Output | ConvertFrom-Json
        $jsonContent.headers.Host | Should Be $uri.Authority
    }

    It "Validate Invoke-WebRequest handles missing Content-Type in response header" {

        #Validate that exception is not thrown when response headers are missing Content-Type.
        $uri = Get-WebListenerUrl -Test 'ResponseHeaders' -Query @{'Content-Type' = ''}
        $command = "Invoke-WebRequest -Uri '$uri'"
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

        $uri = Get-WebListenerUrl -Test 'Get'
        $command = "Invoke-WebRequest -Uri '$uri' -CustomMethod GET -Body @{'testparam'='testvalue'}"
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

    # Test pending support for multiple header capable server on Linux/macOS see issue #4639
    It "Validate Invoke-WebRequest returns valid RelationLink property with absolute uris if Multiple Link Headers are present" -Pending:$(!$IsWindows){
        $headers = @{
            Link =
                '<http://localhost:8080/PowerShell?test=linkheader&maxlinks=5&linknumber=1>; rel="self"',
                '<http://localhost:8080/PowerShell?test=linkheader&maxlinks=5&linknumber=2>; rel="next"',
                '<http://localhost:8080/PowerShell?test=linkheader&maxlinks=5&linknumber=5>; rel="last"'
        } | ConvertTo-Json -Compress
        $headers = [uri]::EscapeDataString($headers)
        $uri = "http://localhost:8080/PowerShell?test=response&contenttype=text/plain&output=OK&headers=$headers"
        $command = "Invoke-WebRequest -Uri '$uri'"
        $result = ExecuteWebCommand -command $command
        $result.Output.RelationLink.Count | Should BeExactly 3
        $result.Output.RelationLink["self"] | Should BeExactly "http://localhost:8080/PowerShell?test=linkheader&maxlinks=5&linknumber=1"
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

    Context "HTTPS Tests" {
        It "Validate Invoke-WebRequest -SkipCertificateCheck" {
            # validate that exception is thrown for URI with expired certificate
            $Uri = Get-WebListenerUrl -Https
            $command = "Invoke-WebRequest -Uri '$Uri'"
            $result = ExecuteWebCommand -command $command
            $result.Error.FullyQualifiedErrorId | Should Be "WebCmdletWebResponseException,Microsoft.PowerShell.Commands.InvokeWebRequestCommand"

            # validate that no exception is thrown for URI with expired certificate when using -SkipCertificateCheck option
            $Uri = Get-WebListenerUrl -Https
            $command = "Invoke-WebRequest -Uri '$Uri' -SkipCertificateCheck"
            $result = ExecuteWebCommand -command $command
            $result.Error | Should BeNullOrEmpty
        }

        It "Validate Invoke-WebRequest returns native HTTPS error message in exception" {
            $uri = Get-WebListenerUrl -Https
            $command = "Invoke-WebRequest -Uri '$uri'"
            $result = ExecuteWebCommand -command $command

            # need to check against inner exception since Linux and Windows uses different HTTP client libraries so errors aren't the same
            $result.Error.ErrorDetails.Message | Should Match $result.Error.Exception.InnerException.Message
            $result.Error.FullyQualifiedErrorId | Should Be "WebCmdletWebResponseException,Microsoft.PowerShell.Commands.InvokeWebRequestCommand"
        }

        It "Verifies Invoke-WebRequest Certificate Authentication Fails without -Certificate"  {
            $uri = Get-WebListenerUrl -Https -Test 'Cert'
            $result = Invoke-WebRequest -Uri $uri -SkipCertificateCheck |
                Select-Object -ExpandProperty Content |
                ConvertFrom-Json

            $result.Status | Should Be 'FAILED'
        }

        # Test skipped on macOS and CentOS pending support for Client Certificate Authentication
        # https://github.com/PowerShell/PowerShell/issues/4650
        It "Verifies Invoke-WebRequest Certificate Authentication Successful with -Certificate" -Pending:$PendingCertificateTest {
            $uri = Get-WebListenerUrl -Https -Test 'Cert'
            $certificate = Get-WebListenerClientCertificate
            $result = Invoke-WebRequest -Uri $uri -Certificate $certificate -SkipCertificateCheck |
                Select-Object -ExpandProperty Content |
                ConvertFrom-Json

            $result.Status | Should Be 'OK'
            $result.Thumbprint | Should Be $certificate.Thumbprint
        }
    }

    Context "Multipart/form-data Tests" {
        It "Verifies Invoke-WebRequest Supports Multipart String Values" {
            $body = GetMultipartBody -String
            $uri = Get-WebListenerUrl -Test 'Multipart'
            $response = Invoke-WebRequest -Uri $uri -Body $body -Method 'POST'
            $result = $response.Content | ConvertFrom-Json

            $result.Headers.'Content-Type' | Should Match 'multipart/form-data'
            $result.Items.TestString[0] | Should Be 'TestValue'
        }
        It "Verifies Invoke-WebRequest Supports Multipart File Values" {
            $body = GetMultipartBody -File
            $uri = Get-WebListenerUrl -Test 'Multipart'
            $response = Invoke-WebRequest -Uri $uri -Body $body -Method 'POST'
            $result = $response.Content | ConvertFrom-Json

            $result.Headers.'Content-Type' | Should Match 'multipart/form-data'
            $result.Files[0].FileName | Should Be 'multipart.txt'
            $result.Files[0].ContentType | Should Be 'text/plain'
            $result.Files[0].Content | Should Match 'TestContent'
        }
        It "Verifies Invoke-WebRequest Supports Mixed Multipart String and File Values" {
            $body = GetMultipartBody -String -File
            $uri = Get-WebListenerUrl -Test 'Multipart'
            $response = Invoke-WebRequest -Uri $uri -Body $body -Method 'POST'
            $result = $response.Content | ConvertFrom-Json

            $result.Headers.'Content-Type' | Should Match 'multipart/form-data'
            $result.Items.TestString[0] | Should Be 'TestValue'
            $result.Files[0].FileName | Should Be 'multipart.txt'
            $result.Files[0].ContentType | Should Be 'text/plain'
            $result.Files[0].Content | Should Match 'TestContent'
        }
    }

    Context "Invoke-WebRequest -Authentication tests" {
        BeforeAll {
            #[SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="Demo/doc/test secret.")]
            $token = "testpassword" | ConvertTo-SecureString -AsPlainText -Force
            $credential = [pscredential]::new("testuser",$token)
            $httpUri = Get-WebListenerUrl -Test 'Get'
            $httpsUri = Get-WebListenerUrl -Test 'Get' -Https
            $testCases = @(
                @{Authentication = "bearer"}
                @{Authentication = "OAuth"}
            )
        }

        It "Verifies Invoke-WebRequest -Authentication Basic" {
            $params = @{
                Uri = $httpsUri
                Authentication = "Basic"
                Credential = $credential
                SkipCertificateCheck = $true
            }
            $Response = Invoke-WebRequest @params
            $result = $response.Content | ConvertFrom-Json

            $result.Headers.Authorization | Should BeExactly "Basic dGVzdHVzZXI6dGVzdHBhc3N3b3Jk"
        }

        It "Verifies Invoke-WebRequest -Authentication <Authentication>" -TestCases $testCases {
            param($Authentication)
            $params = @{
                Uri = $httpsUri
                Authentication = $Authentication
                Token = $token
                SkipCertificateCheck = $true
            }
            $Response = Invoke-WebRequest @params
            $result = $response.Content | ConvertFrom-Json

            $result.Headers.Authorization | Should BeExactly "Bearer testpassword"
        }

        It "Verifies Invoke-WebRequest -Authentication does not support -UseDefaultCredentials" {
            $params = @{
                Uri = $httpsUri
                Token = $token
                Authentication = "OAuth"
                UseDefaultCredentials = $true
                ErrorAction = 'Stop'
                SkipCertificateCheck = $true
            }
            { Invoke-WebRequest @params } | ShouldBeErrorId "WebCmdletAuthenticationConflictException,Microsoft.PowerShell.Commands.InvokeWebRequestCommand"
        }

        It "Verifies Invoke-WebRequest -Authentication does not support Both -Credential and -Token" {
            $params = @{
                Uri = $httpsUri
                Token = $token
                Credential = $credential
                Authentication = "OAuth"
                ErrorAction = 'Stop'
                SkipCertificateCheck = $true
            }
            { Invoke-WebRequest @params } | ShouldBeErrorId "WebCmdletAuthenticationTokenConflictException,Microsoft.PowerShell.Commands.InvokeWebRequestCommand"
        }

        It "Verifies Invoke-WebRequest -Authentication <Authentication> requires -Token" -TestCases $testCases {
            param($Authentication)
            $params = @{
                Uri = $httpsUri
                Authentication = $Authentication
                ErrorAction = 'Stop'
                SkipCertificateCheck = $true
            }
            { Invoke-WebRequest @params } | ShouldBeErrorId "WebCmdletAuthenticationTokenNotSuppliedException,Microsoft.PowerShell.Commands.InvokeWebRequestCommand"
        }

        It "Verifies Invoke-WebRequest -Authentication Basic requires -Credential" {
            $params = @{
                Uri = $httpsUri
                Authentication = "Basic"
                ErrorAction = 'Stop'
                SkipCertificateCheck = $true
            }
            { Invoke-WebRequest @params } | ShouldBeErrorId "WebCmdletAuthenticationCredentialNotSuppliedException,Microsoft.PowerShell.Commands.InvokeWebRequestCommand"
        }

        It "Verifies Invoke-WebRequest -Authentication Requires HTTPS" {
            $params = @{
                Uri = $httpUri
                Token = $token
                Authentication = "OAuth"
                ErrorAction = 'Stop'
            }
            { Invoke-WebRequest @params } | ShouldBeErrorId "WebCmdletAllowUnencryptedAuthenticationRequiredException,Microsoft.PowerShell.Commands.InvokeWebRequestCommand"
        }

        It "Verifies Invoke-WebRequest -Authentication Can use HTTP with -AllowUnencryptedAuthentication" {
            $params = @{
                Uri = $httpUri
                Token = $token
                Authentication = "OAuth"
                AllowUnencryptedAuthentication = $true
            }
            $Response = Invoke-WebRequest @params
            $result = $response.Content | ConvertFrom-Json

            $result.Headers.Authorization | Should BeExactly "Bearer testpassword"
        }
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

    BeforeAll {
        $response = Start-HttpListener -Port 8081
        $WebListener = Start-WebListener
    }

    AfterAll {
        $null = Stop-HttpListener -Port 8081
        $response.PowerShell.Dispose()
    }

    #User-Agent changes on different platforms, so tests should only be run if on the correct platform
    It "Invoke-RestMethod returns Correct User-Agent on MacOSX" -Skip:(!$IsMacOS) {

        $uri = Get-WebListenerUrl -Test 'Get'
        $command = "Invoke-RestMethod -Uri '$uri' -TimeoutSec 5"

        $result = ExecuteWebCommand -command $command

        # Validate response
        $result.Output.headers.'User-Agent' | Should MatchExactly '.*\(Macintosh;.*\) PowerShell\/\d+\.\d+\.\d+.*'
    }

    It "Invoke-RestMethod returns Correct User-Agent on Linux" -Skip:(!$IsLinux) {

        $uri = Get-WebListenerUrl -Test 'Get'
        $command = "Invoke-RestMethod -Uri '$uri' -TimeoutSec 5"

        $result = ExecuteWebCommand -command $command

        # Validate response
        $result.Output.headers.'User-Agent' | Should MatchExactly '.*\(Linux;.*\) PowerShell\/\d+\.\d+\.\d+.*'
    }

    It "Invoke-RestMethod returns Correct User-Agent on Windows" -Skip:(!$IsWindows) {

        $uri = Get-WebListenerUrl -Test 'Get'
        $command = "Invoke-RestMethod -Uri '$uri' -TimeoutSec 5"

        $result = ExecuteWebCommand -command $command

        # Validate response
        $result.Output.headers.'User-Agent' | Should MatchExactly '.*\(Windows NT \d+\.\d*;.*\) PowerShell\/\d+\.\d+\.\d+.*'
    }

    It "Invoke-RestMethod returns headers dictionary" {

        $uri = Get-WebListenerUrl -Test 'Get'
        $command = "Invoke-RestMethod -Uri '$uri' -TimeoutSec 5"

        $result = ExecuteWebCommand -command $command

        # Validate response
        $result.Output.headers.Host | Should Be $Uri.Authority
    }

    It "Validate Invoke-RestMethod -DisableKeepAlive" {

        # Operation options
        $uri = Get-WebListenerUrl -Test 'Get'
        $command = "Invoke-RestMethod -Uri '$uri' -TimeoutSec 5 -DisableKeepAlive"

        $result = ExecuteWebCommand -command $command

        # Validate response
        $result.Output.headers.Host | Should Be $uri.Authority
        $result.Output.Headers.Connection | Should Be "Close"
    }

    It "Validate Invoke-RestMethod -MaximumRedirection" {

        $uri = Get-WebListenerUrl -Test 'Redirect' -TestValue '3'
        $command = "Invoke-RestMethod -Uri '$uri' -MaximumRedirection 4"

        $result = ExecuteWebCommand -command $command

        # Validate response
        $result.Output.headers.Host | Should Match $uri.Authority
    }

    It "Validate Invoke-RestMethod error for -MaximumRedirection" {

        $uri = Get-WebListenerUrl -Test 'Redirect' -TestValue '3'
        $command = "Invoke-RestMethod -Uri '$uri' -MaximumRedirection 2"

        $result = ExecuteWebCommand -command $command
        $result.Error.FullyQualifiedErrorId | Should Be "WebCmdletWebResponseException,Microsoft.PowerShell.Commands.InvokeRestMethodCommand"
    }

    It "Invoke-RestMethod supports request that returns page containing UTF-8 data." {

        $uri = Get-WebListenerUrl -Test 'Encoding' -TestValue 'Utf8'
        $command = "Invoke-RestMethod -Uri '$uri'"

        $result = ExecuteWebCommand -command $command
        $Result.Output | Should Match '⡌⠁⠧⠑ ⠼⠁⠒  ⡍⠜⠇⠑⠹⠰⠎ ⡣⠕⠌'
    }

    It "Invoke-RestMethod validate timeout option" {

        $uri = Get-WebListenerUrl -Test 'Delay' -TestValue '5'
        $command = "Invoke-RestMethod -Uri '$uri' -TimeoutSec 2"

        $result = ExecuteWebCommand -command $command
        $result.Error.FullyQualifiedErrorId | Should Be "System.Threading.Tasks.TaskCanceledException,Microsoft.PowerShell.Commands.InvokeRestMethodCommand"
    }

    It "Validate Invoke-RestMethod error with -Proxy and -NoProxy option" {

        $uri = Get-WebListenerUrl -Test 'Delay' -TestValue '10'
        $command = "Invoke-RestMethod -Uri '$uri' -Proxy 'http://localhost:8080' -NoProxy -TimeoutSec 2"

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

        $uri = Get-WebListenerUrl -Test 'Delay' -TestValue '5' -Https:$($protocol -eq 'https')
        $command = "Invoke-RestMethod -Uri '$uri' -TimeoutSec 2 -SkipCertificateCheck"

        $result = ExecuteWebCommand -command $command
        $result.Error.FullyQualifiedErrorId | Should Be "System.Threading.Tasks.TaskCanceledException,Microsoft.PowerShell.Commands.InvokeRestMethodCommand"
    }

    It "Validate Invoke-RestMethod returns User-Agent with option -NoProxy when environment proxy set - '<name>'" -TestCases $testCase {
        param($proxy_address, $name, $protocol)

        # Configure the environment variable.
        New-Item -Name ${name} -Value ${proxy_address} -ItemType Variable -Path Env: -Force

        $uri = Get-WebListenerUrl -Test 'Get' -Https:$($protocol -eq 'https')
        $command = "Invoke-RestMethod -Uri '$uri' -TimeoutSec 5 -NoProxy -SkipCertificateCheck"

        $result = ExecuteWebCommand -command $command

        # Validate response
        $result.Output.headers.'User-Agent' | Should MatchExactly '(?<!Windows)PowerShell\/\d+\.\d+\.\d+.*'
    }

    # Perform the following operation for Invoke-RestMethod
    # gzip Returns gzip-encoded data.
    # deflate Returns deflate-encoded data.
    # $dataEncodings = @("Chunked", "Compress", "Deflate", "GZip", "Identity")
    # Note: These are the supported options, but we do not have a web service to test them all.
    It "Invoke-RestMethod supports request that returns <DataEncoding>-encoded data." -TestCases @(
        @{ DataEncoding = "gzip"}
        @{ DataEncoding = "deflate"}
    ) {
        param($dataEncoding)
        $uri = Get-WebListenerUrl -Test 'Compression' -TestValue $dataEncoding
        $result = Invoke-RestMethod -Uri $uri -ResponseHeadersVariable 'headers'

        # Validate response content
        $headers.'Content-Encoding'[0] | Should BeExactly $dataEncoding
        $result.Headers.Host | Should BeExactly $uri.Authority
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

        $uri = Get-WebListenerUrl -Test 'Get'
        $result = ExecuteRequestWithHeaders -cmdletName Invoke-RestMethod -uri $uri

        # Validate response
        $result.Output.url | Should Match $uri
        $result.Output.Headers.Connection | Should Be "Close"
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
        $uri = Get-WebListenerUrl -Test 'Get'
        $userAgent = "[Microsoft.PowerShell.Commands.PSUserAgent]::$agentName"
        $command = "Invoke-RestMethod -Uri $uri -UserAgent ($userAgent)  -TimeoutSec 5"

        It "Validate Invoke-RestMethod UserAgent. Execute--> $command" {

            $result = ExecuteWebCommand -command $command

            # Validate response
            $result.Output.headers.Host | Should Be $uri.Authority
            $result.Output.headers.'User-Agent' | Should Match $expectedAgent
        }
    }

    It "Validate Invoke-RestMethod -OutFile" {

        $uri = Get-WebListenerUrl -Test 'Get'
        $result = ExecuteRequestWithOutFile -cmdletName "Invoke-RestMethod" -uri $uri
        $jsonContent = $result.Output | ConvertFrom-Json
        $jsonContent.headers.Host | Should Be $uri.Authority
    }

    It "Validate Invoke-RestMethod handles missing Content-Type in response header" {

        #Validate that exception is not thrown when response headers are missing Content-Type.
        $uri = Get-WebListenerUrl -Test 'ResponseHeaders' -Query @{'Content-Type' = ''}
        $command = "Invoke-RestMethod -Uri '$uri'"
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

        $uri = Get-WebListenerUrl -Test 'Get'
        $command = "Invoke-RestMethod -Uri '$uri' -CustomMethod GET -Body @{'testparam'='testvalue'}"
        $result = ExecuteWebCommand -command $command
        $result.Output.args.testparam | Should Be "testvalue"
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

    Context "HTTPS Tests" {
        It "Validate Invoke-RestMethod -SkipCertificateCheck" {
            # HTTP method HEAD must be used to not retrieve an unparsable HTTP body
            # validate that exception is thrown for URI with expired certificate
            $uri= Get-WebListenerUrl -Https
            $command = "Invoke-RestMethod -Uri '$uri' -Method HEAD"
            $result = ExecuteWebCommand -command $command
            $result.Error.FullyQualifiedErrorId | Should Be "WebCmdletWebResponseException,Microsoft.PowerShell.Commands.InvokeRestMethodCommand"

            # validate that no exception is thrown for URI with expired certificate when using -SkipCertificateCheck option
            $command = "Invoke-RestMethod -Uri '$uri' -SkipCertificateCheck -Method HEAD"
            $result = ExecuteWebCommand -command $command
            $result.Error | Should BeNullOrEmpty
        }

        It "Validate Invoke-RestMethod returns native HTTPS error message in exception" {
            $uri = Get-WebListenerUrl -Https
            $command = "Invoke-RestMethod -Uri '$uri'"
            $result = ExecuteWebCommand -command $command

            # need to check against inner exception since Linux and Windows uses different HTTP client libraries so errors aren't the same
            $result.Error.ErrorDetails.Message | Should Match $result.Error.Exception.InnerException.Message
            $result.Error.FullyQualifiedErrorId | Should Be "WebCmdletWebResponseException,Microsoft.PowerShell.Commands.InvokeRestMethodCommand"
        }

        It "Verifies Invoke-RestMethod Certificate Authentication Fails without -Certificate" {
            $uri = Get-WebListenerUrl -Https -Test 'Cert'
            $result = Invoke-RestMethod -Uri $uri -SkipCertificateCheck

            $result.Status | Should Be 'FAILED'
        }

        # Test skipped on macOS and CentOS pending support for Client Certificate Authentication
        # https://github.com/PowerShell/PowerShell/issues/4650
        It "Verifies Invoke-RestMethod Certificate Authentication Successful with -Certificate" -Pending:$PendingCertificateTest {
            $uri = Get-WebListenerUrl -Https -Test 'Cert'
            $certificate = Get-WebListenerClientCertificate
            $result = Invoke-RestMethod -uri $uri -Certificate $certificate -SkipCertificateCheck

            $result.Status | Should Be 'OK'
            $result.Thumbprint | Should Be $certificate.Thumbprint
        }
    }

    Context "Multipart/form-data Tests" {
        It "Verifies Invoke-RestMethod Supports Multipart String Values" {
            $body = GetMultipartBody -String
            $uri = Get-WebListenerUrl -Test 'Multipart'
            $result = Invoke-RestMethod -Uri $uri -Body $body -Method 'POST'

            $result.Headers.'Content-Type' | Should Match 'multipart/form-data'
            $result.Items.TestString[0] | Should Be 'TestValue'
        }
        It "Verifies Invoke-RestMethod Supports Multipart File Values" {
            $body = GetMultipartBody -File
            $uri = Get-WebListenerUrl -Test 'Multipart'
            $result = Invoke-RestMethod -Uri $uri -Body $body -Method 'POST'

            $result.Headers.'Content-Type' | Should Match 'multipart/form-data'
            $result.Files[0].FileName | Should Be 'multipart.txt'
            $result.Files[0].ContentType | Should Be 'text/plain'
            $result.Files[0].Content | Should Match 'TestContent'
        }
        It "Verifies Invoke-RestMethod Supports Mixed Multipart String and File Values" {
            $body = GetMultipartBody -String -File
            $uri = Get-WebListenerUrl -Test 'Multipart'
            $result = Invoke-RestMethod -Uri $uri -Body $body -Method 'POST'

            $result.Headers.'Content-Type' | Should Match 'multipart/form-data'
            $result.Items.TestString[0] | Should Be 'TestValue'
            $result.Files[0].FileName | Should Be 'multipart.txt'
            $result.Files[0].ContentType | Should Be 'text/plain'
            $result.Files[0].Content | Should Match 'TestContent'
        }
    }

    #region charset encoding tests

    Context  "Invoke-RestMethod Encoding tests with BasicHtmlWebResponseObject response" {
        It "Verifies Invoke-RestMethod detects charset meta value when the ContentType header does not define it." {
            $output = '<html><head><meta charset="Unicode"></head></html>'
            $expectedEncoding = [System.Text.Encoding]::GetEncoding('Unicode')
            $response = ExecuteRestMethod -Uri "http://localhost:8081/PowerShell?test=response&output=$output" -UseBasicParsing

            $response.Error | Should BeNullOrEmpty
            $response.Encoding.EncodingName | Should Be $expectedEncoding.EncodingName
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
            $response = ExecuteRestMethod -Uri "http://localhost:8081/PowerShell?test=response&output=$output" -UseBasicParsing

            $response.Error | Should BeNullOrEmpty
            $response.Encoding.EncodingName | Should Be $expectedEncoding.EncodingName
        }

        It "Verifies Invoke-RestMethod detects charset meta value when the attribute value is unquoted." {
            $output = '<html><head><meta charset = Unicode></head></html>'
            $expectedEncoding = [System.Text.Encoding]::GetEncoding('Unicode')
            $response = ExecuteRestMethod -Uri "http://localhost:8081/PowerShell?test=response&output=$output" -UseBasicParsing

            $response.Error | Should BeNullOrEmpty
            $response.Encoding.EncodingName | Should Be $expectedEncoding.EncodingName
        }

        It "Verifies Invoke-RestMethod detects http-equiv charset meta value when the ContentType header does not define it." {
            $output = @'
<html><head>
<meta http-equiv="content-type" content="text/html; charset=Unicode">
</head>
</html>
'@
            $expectedEncoding = [System.Text.Encoding]::GetEncoding('Unicode')
            $response = ExecuteRestMethod -Uri "http://localhost:8081/PowerShell?test=response&output=$output" -UseBasicParsing

            $response.Error | Should BeNullOrEmpty
            $response.Encoding.EncodingName | Should Be $expectedEncoding.EncodingName
        }

        It "Verifies Invoke-RestMethod detects http-equiv charset meta value newlines are encountered in the element." {
            $output = @'
<html><head>
<meta
    http-equiv="content-type"
    content="text/html; charset=Unicode">
</head>
</html>
'@
            $expectedEncoding = [System.Text.Encoding]::GetEncoding('Unicode')
            $response = ExecuteRestMethod -Uri "http://localhost:8081/PowerShell?test=response&output=$output" -UseBasicParsing

            $response.Error | Should BeNullOrEmpty
            $response.Encoding.EncodingName | Should Be $expectedEncoding.EncodingName
        }

        It "Verifies Invoke-RestMethod ignores meta charset value when Content-Type header defines it." {
            $output = '<html><head><meta charset="utf-32"></head></html>'
            # NOTE: meta charset should be ignored
            $expectedEncoding = [System.Text.Encoding]::UTF8
            $response = ExecuteRestMethod -Uri "http://localhost:8081/PowerShell?test=response&contenttype=text/html; charset=utf-8&output=$output" -UseBasicParsing

            $response.Error | Should BeNullOrEmpty
            $response.Encoding.EncodingName | Should Be $expectedEncoding.EncodingName
        }

        It "Verifies Invoke-RestMethod honors non-utf8 charsets in the Content-Type header" {
            $output = '<html><head><meta charset="utf-32"></head></html>'
            # NOTE: meta charset should be ignored
            $expectedEncoding = [System.Text.Encoding]::GetEncoding('utf-16')
            $response = ExecuteRestMethod -Uri "http://localhost:8081/PowerShell?test=response&contenttype=text/html; charset=utf-16&output=$output" -UseBasicParsing

            $response.Error | Should BeNullOrEmpty
            $response.Encoding.EncodingName | Should Be $expectedEncoding.EncodingName
        }

        It "Verifies Invoke-RestMethod defaults to iso-8859-1 when an unsupported/invalid charset is declared" {
            $output = '<html><head><meta charset="invalid"></head></html>'
            $expectedEncoding = [System.Text.Encoding]::GetEncoding('iso-8859-1')
            $response = ExecuteRestMethod -Uri "http://localhost:8081/PowerShell?test=response&contenttype=text/html&output=$output" -UseBasicParsing

            $response.Error | Should BeNullOrEmpty
            $response.Encoding.EncodingName | Should Be $expectedEncoding.EncodingName
        }

        It "Verifies Invoke-RestMethod defaults to iso-8859-1 when an unsupported/invalid charset is declared using http-equiv" {
            $output = @'
<html><head>
<meta http-equiv="content-type" content="text/html; charset=Invalid">
</head>
</html>
'@
            $expectedEncoding = [System.Text.Encoding]::GetEncoding('iso-8859-1')
            $response = ExecuteRestMethod -Uri "http://localhost:8081/PowerShell?test=response&contenttype=text/html&output=$output" -UseBasicParsing

            $response.Error | Should BeNullOrEmpty
            $response.Encoding.EncodingName | Should Be $expectedEncoding.EncodingName
        }
    }

    Context  "Invoke-RestMethod Encoding tests with HtmlWebResponseObject response" {
        # these tests are dependent on https://github.com/PowerShell/PowerShell/issues/2867
        # Currently, all paths return BasicHtmlWebResponseObject
        It "Verifies Invoke-RestMethod detects charset meta value when the ContentType header does not define it." -Pending {
            $output = '<html><head><meta charset="Unicode"></head></html>'
            $expectedEncoding = [System.Text.Encoding]::GetEncoding('Unicode')
            $response = ExecuteRestMethod -Uri "http://localhost:8081/PowerShell?test=response&output=$output"

            $response.Error | Should BeNullOrEmpty
            $response.Encoding.EncodingName | Should Be $expectedEncoding.EncodingName
        }

        It "Verifies Invoke-RestMethod detects charset meta value when newlines are encountered in the element." -Pending {
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
            $response = ExecuteRestMethod -Uri "http://localhost:8081/PowerShell?test=response&output=$output"

            $response.Error | Should BeNullOrEmpty
            $response.Encoding.EncodingName | Should Be $expectedEncoding.EncodingName
        }

        It "Verifies Invoke-RestMethod ignores meta charset value when Content-Type header defines it." -Pending {
            $output = '<html><head><meta charset="utf-16"></head></html>'
            # NOTE: meta charset should be ignored
            $expectedEncoding = [System.Text.Encoding]::UTF8
            # Update to test for HtmlWebResponseObject when mshtl dependency has been resolved.
            $response = ExecuteRestMethod -Uri "http://localhost:8081/PowerShell?test=response&contenttype=text/html; charset=utf-8&output=$output"

            $response.Error | Should BeNullOrEmpty
            $response.Encoding.EncodingName | Should Be $expectedEncoding.EncodingName
        }

        It "Verifies Invoke-RestMethod detects http-equiv charset meta value when the ContentType header does not define it." -Pending {
            $output = @'
<html><head>
<meta http-equiv="content-type" content="text/html; charset=Unicode">
</head>
</html>
'@
            $expectedEncoding = [System.Text.Encoding]::GetEncoding('Unicode')
            $response = ExecuteRestMethod -Uri "http://localhost:8081/PowerShell?test=response&output=$output"

            $response.Error | Should BeNullOrEmpty
            $response.Encoding.EncodingName | Should Be $expectedEncoding.EncodingName
        }

        It "Verifies Invoke-RestMethod detects http-equiv charset meta value newlines are encountered in the element." -Pending {
            $output = @'
<html><head>
<meta
    http-equiv="content-type"
    content="text/html; charset=Unicode">
</head>
</html>
'@
            $expectedEncoding = [System.Text.Encoding]::GetEncoding('Unicode')
            $response = ExecuteRestMethod -Uri "http://localhost:8081/PowerShell?test=response&output=$output"

            $response.Error | Should BeNullOrEmpty
            $response.Encoding.EncodingName | Should Be $expectedEncoding.EncodingName
        }

        It "Verifies Invoke-RestMethod honors non-utf8 charsets in the Content-Type header" -Pending {
            $output = '<html><head><meta charset="utf-32"></head></html>'
            # NOTE: meta charset should be ignored
            $expectedEncoding = [System.Text.Encoding]::GetEncoding('utf-16')
            $response = ExecuteRestMethod -Uri "http://localhost:8081/PowerShell?test=response&contenttype=text/html; charset=utf-16&output=$output"

            $response.Error | Should BeNullOrEmpty
            $response.Encoding.EncodingName | Should Be $expectedEncoding.EncodingName
        }

        It "Verifies Invoke-RestMethod defaults to iso-8859-1 when an unsupported/invalid charset is declared" -Pending {
            $output = '<html><head><meta charset="invalid"></head></html>'
            $expectedEncoding = [System.Text.Encoding]::GetEncoding('iso-8859-1')
            $response = ExecuteRestMethod -Uri "http://localhost:8081/PowerShell?test=response&contenttype=text/html&output=$output"

            $response.Error | Should BeNullOrEmpty
            $response.Encoding.EncodingName | Should Be $expectedEncoding.EncodingName
        }

        It "Verifies Invoke-RestMethod defaults to iso-8859-1 when an unsupported/invalid charset is declared using http-equiv" -Pending {
            $output = @'
<html><head>
<meta http-equiv="content-type" content="text/html; charset=Invalid">
</head>
</html>
'@
            $expectedEncoding = [System.Text.Encoding]::GetEncoding('iso-8859-1')
            $response = ExecuteRestMethod -Uri "http://localhost:8081/PowerShell?test=response&contenttype=text/html&output=$output"

            $response.Error | Should BeNullOrEmpty
            $response.Encoding.EncodingName | Should Be $expectedEncoding.EncodingName
        }
    }

    #endregion charset encoding tests

    Context 'Invoke-RestMethod ResponseHeadersVariable Tests' {
        It "Verifies Invoke-RestMethod supports -ResponseHeadersVariable" {
            $uri = Get-WebListenerUrl -Test '/'
            $response = Invoke-RestMethod -Uri $uri -ResponseHeadersVariable 'headers'

            $headers.'Content-Type' | Should Be 'text/html; charset=utf-8'
            $headers.Server | Should Be 'Kestrel'
        }

        It "Verifies Invoke-RestMethod supports -ResponseHeadersVariable overwriting existing variable" {
            $uri = Get-WebListenerUrl -Test '/'
            $headers = 'prexisting'
            $response = Invoke-RestMethod -Uri $uri -ResponseHeadersVariable 'headers'

            $headers | Should Not Be 'prexisting'
            $headers.'Content-Type' | Should Be 'text/html; charset=utf-8'
            $headers.Server | Should Be 'Kestrel'
        }
    }

    Context "Invoke-RestMethod -Authentication tests" {
        BeforeAll {
            #[SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="Demo/doc/test secret.")]
            $token = "testpassword" | ConvertTo-SecureString -AsPlainText -Force
            $credential = [pscredential]::new("testuser",$token)
            $httpUri = Get-WebListenerUrl -Test 'Get'
            $httpsUri = Get-WebListenerUrl -Test 'Get' -Https
            $testCases = @(
                @{Authentication = "bearer"}
                @{Authentication = "OAuth"}
            )
        }

        It "Verifies Invoke-RestMethod -Authentication Basic" {
            $params = @{
                Uri = $httpsUri
                Authentication = "Basic"
                Credential = $credential
                SkipCertificateCheck = $true
            }
            $result = Invoke-RestMethod @params

            $result.Headers.Authorization | Should BeExactly "Basic dGVzdHVzZXI6dGVzdHBhc3N3b3Jk"
        }

        It "Verifies Invoke-RestMethod -Authentication <Authentication>" -TestCases $testCases {
            param($Authentication)
            $params = @{
                Uri = $httpsUri
                Authentication = $Authentication
                Token = $token
                SkipCertificateCheck = $true
            }
            $result = Invoke-RestMethod @params

            $result.Headers.Authorization | Should BeExactly "Bearer testpassword"
        }

        It "Verifies Invoke-RestMethod -Authentication does not support -UseDefaultCredentials" {
            $params = @{
                Uri = $httpsUri
                Token = $token
                Authentication = "OAuth"
                UseDefaultCredentials = $true
                ErrorAction = 'Stop'
                SkipCertificateCheck = $true
            }
            { Invoke-RestMethod @params } | ShouldBeErrorId "WebCmdletAuthenticationConflictException,Microsoft.PowerShell.Commands.InvokeRestMethodCommand"
        }

        It "Verifies Invoke-RestMethod -Authentication does not support Both -Credential and -Token" {
            $params = @{
                Uri = $httpsUri
                Token = $token
                Credential = $credential
                Authentication = "OAuth"
                ErrorAction = 'Stop'
                SkipCertificateCheck = $true
            }
            { Invoke-RestMethod @params } | ShouldBeErrorId "WebCmdletAuthenticationTokenConflictException,Microsoft.PowerShell.Commands.InvokeRestMethodCommand"
        }

        It "Verifies Invoke-RestMethod -Authentication <Authentication> requires -Token" -TestCases $testCases {
            param($Authentication)
            $params = @{
                Uri = $httpsUri
                Authentication = $Authentication
                ErrorAction = 'Stop'
                SkipCertificateCheck = $true
            }
            { Invoke-RestMethod @params } | ShouldBeErrorId "WebCmdletAuthenticationTokenNotSuppliedException,Microsoft.PowerShell.Commands.InvokeRestMethodCommand"
        }

        It "Verifies Invoke-RestMethod -Authentication Basic requires -Credential" {
            $params = @{
                Uri = $httpsUri
                Authentication = "Basic"
                ErrorAction = 'Stop'
                SkipCertificateCheck = $true
            }
            { Invoke-RestMethod @params } | ShouldBeErrorId "WebCmdletAuthenticationCredentialNotSuppliedException,Microsoft.PowerShell.Commands.InvokeRestMethodCommand"
        }

        It "Verifies Invoke-RestMethod -Authentication Requires HTTPS" {
            $params = @{
                Uri = $httpUri
                Token = $token
                Authentication = "OAuth"
                ErrorAction = 'Stop'
            }
            { Invoke-RestMethod @params } | ShouldBeErrorId "WebCmdletAllowUnencryptedAuthenticationRequiredException,Microsoft.PowerShell.Commands.InvokeRestMethodCommand"
        }

        It "Verifies Invoke-RestMethod -Authentication Can use HTTP with -AllowUnencryptedAuthentication" {
            $params = @{
                Uri = $httpUri
                Token = $token
                Authentication = "OAuth"
                AllowUnencryptedAuthentication = $true
            }
            $result = Invoke-RestMethod @params

            $result.Headers.Authorization | Should BeExactly "Bearer testpassword"
        }
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
