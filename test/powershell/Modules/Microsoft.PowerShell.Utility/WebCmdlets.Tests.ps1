# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# This is a Pester test suite which validate the Web cmdlets.
#
# Note: These tests use data from WebListener
#

# Invokes the given command via script block invocation.
#
function ExecuteWebCommand {
    param (
        [ValidateNotNullOrEmpty()]
        [string]
        $command
    )

    $result = [PSObject]@{Output = $null; Error = $null}

    try {
        $scriptBlock = [scriptblock]::Create($command)
        $result.Output = & $scriptBlock
    } catch {
        $result.Error = $_
    }

    return $result
}

# This function calls either Invoke-WebRequest or Invoke-RestMethod using the OutFile parameter
# Then, the file content is read and return in a $result object.
#
function ExecuteRequestWithOutFile {
    param (
        [ValidateSet("Invoke-RestMethod", "Invoke-WebRequest" )]
        [string]
        $cmdletName,

        [string]
        $uri = (Get-WebListenerUrl -Test 'Get')
    )

    $result = [PSObject]@{Output = $null; Error = $null}
    # We use '[outfile1]' in the file name to check that OutFile parameter is literal path
    $filePath = Join-Path $TestDrive ((Get-Random).ToString() + "[outfile1].txt")
    try {
        if ($cmdletName -eq "Invoke-WebRequest") {
            Invoke-WebRequest -Uri $uri -OutFile $filePath
        } else {
            Invoke-RestMethod -Uri $uri -OutFile $filePath
        }
        $result.Output = Get-Content -LiteralPath $filePath -Raw -ErrorAction SilentlyContinue
    } catch {
        $result.Error = $_
    } finally {
        if (Test-Path $filePath) {
            Remove-Item $filePath -Force -ErrorAction SilentlyContinue
        }
    }
    return $result
}

# This function calls either Invoke-WebRequest or Invoke-RestMethod with the given uri
# using the Headers parameter to disable keep-alive.
#
function ExecuteRequestWithHeaders {
    param (
        [ValidateSet("Invoke-RestMethod", "Invoke-WebRequest" )]
        [string]
        $cmdletName,

        [string]
        $uri = (Get-WebListenerUrl -Test 'Get')
    )

    $result = [PSObject]@{Output = $null; Error = $null}
    try {
        $headers = @{ Connection = 'close'}
        if ($cmdletName -eq "Invoke-WebRequest") {
            $result.Output = Invoke-WebRequest -Uri $uri -Headers $headers
        } else {
            $result.Output = Invoke-RestMethod -Uri $uri -Headers $headers
        }
    } catch {
        $result.Error = $_
    }
    return $result
}

# Returns test data for the given content type.
#
function GetTestData {
    param (
        [ValidateSet("text/plain", "application/xml", "application/json")]
        [String]
        $contentType
    )

    $testData = @{ItemID = 987123; Name = 'TestData'}

    if ($contentType -eq "text/plain") {
        $body = $testData | Out-String
    } elseif ($contentType -eq "application/xml") {
        $body = '
<?xml version="1.0" encoding="utf-8"?>
<Objects>
<Object>
<ItemID>987123</ItemID>
<Name>TestData</Name>
</Object>
</Objects>
'
    } else {
        # "application/json"
        $body = $testData | ConvertTo-Json -Compress
    }

    return $body
}

function ExecuteRedirectRequest {
    param (
        [Parameter(Mandatory)]
        [string]
        $uri,

        [ValidateSet('Invoke-WebRequest', 'Invoke-RestMethod')]
        [string]
        $Cmdlet = 'Invoke-WebRequest',

        [ValidateSet('POST', 'GET')]
        [string]
        $Method = 'GET',

        [switch]
        $PreserveAuthorizationOnRedirect,

        [ValidateRange(0, [int]::MaxValue)]
        [int]
        $MaximumRedirection
    )
    $result = [PSObject]@{Output = $null; Error = $null; Content = $null}

    try {
        $headers = @{"Authorization" = "test"}
        if ($Cmdlet -eq 'Invoke-WebRequest') {
            if ($MaximumRedirection -gt 0) {
                $result.Output = Invoke-WebRequest -Uri $uri -Headers $headers -PreserveAuthorizationOnRedirect:$PreserveAuthorizationOnRedirect.IsPresent -Method $Method -MaximumRedirection:$MaximumRedirection
            } else {
                $result.Output = Invoke-WebRequest -Uri $uri -Headers $headers -PreserveAuthorizationOnRedirect:$PreserveAuthorizationOnRedirect.IsPresent -Method $Method
            }
            $result.Content = $result.Output.Content | ConvertFrom-Json
        } else {
            $result.Output = Invoke-RestMethod -Uri $uri -Headers $headers -PreserveAuthorizationOnRedirect:$PreserveAuthorizationOnRedirect.IsPresent -Method $Method
            # NOTE: $result.Output should already be a PSObject (Invoke-RestMethod converts the returned json automatically)
            # so simply reference $result.Output
            $result.Content = $result.Output
        }
    } catch {
        $result.Error = $_
    }

    return $result
}

# This function calls either Invoke-WebRequest or Invoke-RestMethod with the given uri
# using the custum headers and the optional SkipHeaderValidation switch.
function ExecuteRequestWithCustomHeaders {
    param (
        [Parameter(Mandatory)]
        [string]
        $Uri,

        [ValidateSet('Invoke-WebRequest', 'Invoke-RestMethod')]
        [string]
        $Cmdlet = 'Invoke-WebRequest',

        [Parameter(Mandatory)]
        [ValidateNotNull()]
        [Hashtable]
        $Headers,

        [switch]
        $SkipHeaderValidation
    )
    $result = [PSObject]@{Output = $null; Error = $null; Content = $null}

    try {
        if ($Cmdlet -eq 'Invoke-WebRequest') {
            $result.Output = Invoke-WebRequest -Uri $Uri -Headers $Headers -SkipHeaderValidation:$SkipHeaderValidation.IsPresent
            $result.Content = $result.Output.Content | ConvertFrom-Json
        } else {
            $result.Output = Invoke-RestMethod -Uri $Uri -Headers $Headers -SkipHeaderValidation:$SkipHeaderValidation.IsPresent
            # NOTE: $result.Output should already be a PSObject (Invoke-RestMethod converts the returned json automatically)
            # so simply reference $result.Output
            $result.Content = $result.Output
        }
    } catch {
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
        [string]
        $Cmdlet = 'Invoke-WebRequest',

        [Parameter(Mandatory)]
        [ValidateNotNull()]
        [string]
        $UserAgent,

        [switch]
        $SkipHeaderValidation
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
        } else {
            $result.Output = Invoke-RestMethod @Params
            # NOTE: $result.Output should already be a PSObject (Invoke-RestMethod converts the returned json automatically)
            # so simply reference $result.Output
            $result.Content = $result.Output
        }
    } catch {
        $result.Error = $_
    }

    return $result
}

# This function calls Invoke-WebRequest with the given uri
function ExecuteWebRequest {
    param (
        [Parameter(Mandatory)]
        [string]
        $Uri,

        [switch]
        $UseBasicParsing
    )
    $result = [PSObject]@{Output = $null; Error = $null; Content = $null}

    try {
        $result.Output = Invoke-WebRequest -Uri $Uri -UseBasicParsing:$UseBasicParsing.IsPresent
        $result.Content = $result.Output.Content
    } catch {
        $result.Error = $_
    }

    return $result
}

[string] $verboseEncodingPrefix = 'Content encoding: '
# This function calls Invoke-WebRequest with the given uri and
# parses the verbose output to determine the encoding used for the content.
function ExecuteRestMethod {
    param (
        [Parameter(Mandatory)]
        [string]
        $Uri,

        [switch]
        $UseBasicParsing
    )
    $result = @{Output = $null; Error = $null; Encoding = $null; Content = $null}
    $verbosePreferenceSave = $VerbosePreference
    $VerbosePreference = 'Continue'
    try {
        $verboseFile = Join-Path $TestDrive -ChildPath ExecuteRestMethod.verbose.txt
        $result.Output = Invoke-RestMethod -Uri $Uri -UseBasicParsing:$UseBasicParsing.IsPresent -Verbose 4>$verboseFile
        $result.Content = $result.Output

        if (Test-Path -Path $verboseFile) {
            $result.Verbose = Get-Content -Path $verboseFile
            foreach ($item in $result.Verbose) {
                $line = $item.Trim()
                if ($line.StartsWith($verboseEncodingPrefix)) {
                    $encodingName = $item.SubString($verboseEncodingPrefix.Length).Trim()
                    $result.Encoding = [System.Text.Encoding]::GetEncoding($encodingName)
                    break
                }
            }
            if ($result.Encoding -eq $null) {
                throw "Encoding not found in verbose output. Lines: $($result.Verbose.Count) Content:$($result.Verbose)"
            }
        }

        if ($result.Verbose -eq $null) {
            throw "No verbose output was found"
        }
    } catch {
        $result.Error = $_ | Select-Object * | Out-String
    } finally {
        $VerbosePreference = $verbosePreferenceSave
        if (Test-Path -Path $verboseFile) {
            Remove-Item -Path $verboseFile -ErrorAction SilentlyContinue
        }
    }

    return $result
}

function GetMultipartBody {
    param (
        [Switch]
        $String,

        [Switch]
        $File
    )
    $multipartContent = [System.Net.Http.MultipartFormDataContent]::new()
    if ($String.IsPresent) {
        $stringHeader = [System.Net.Http.Headers.ContentDispositionHeaderValue]::new("form-data")
        $stringHeader.Name = "TestString"
        $StringContent = [System.Net.Http.StringContent]::new("TestValue")
        $StringContent.Headers.ContentDisposition = $stringHeader
        $multipartContent.Add($stringContent)
    }
    if ($File.IsPresent) {
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
    return , $multipartContent
}

<#
    Defines the list of redirect codes to test as well as the
    expected Method when the redirection is handled.
    See https://docs.microsoft.com/previous-versions/windows/apps/f92ssyy1(v=vs.105)
    for additonal details.
#>
$redirectTests = @(
    @{redirectType = 'MultipleChoices'; redirectedMethod = 'GET'}
    @{redirectType = 'Ambiguous'; redirectedMethod = 'GET'} # Synonym for MultipleChoices

    @{redirectType = 'Moved'; redirectedMethod = 'GET'}
    @{redirectType = 'MovedPermanently'; redirectedMethod = 'GET'} # Synonym for Moved

    @{redirectType = 'Found'; redirectedMethod = 'GET'}
    @{redirectType = 'Redirect'; redirectedMethod = 'GET'} # Synonym for Found

    @{redirectType = 'redirectMethod'; redirectedMethod = 'GET'}
    @{redirectType = 'SeeOther'; redirectedMethod = 'GET'} # Synonym for RedirectMethod

    @{redirectType = 'TemporaryRedirect'; redirectedMethod = 'POST'}
    @{redirectType = 'RedirectKeepVerb'; redirectedMethod = 'POST'} # Synonym for TemporaryRedirect

    @{redirectType = 'relative'; redirectedMethod = 'GET'}
)

Describe "Invoke-WebRequest tests" -Tags "Feature", "RequireAdminOnWindows" {
    BeforeAll {
        $oldProgress = $ProgressPreference
        $ProgressPreference = 'SilentlyContinue'
        $WebListener = Start-WebListener
        $NotFoundQuery = @{
            statuscode = 404
            responsephrase = 'Not Found'
            contenttype = 'text/plain'
            body = 'oops'
            headers = "{}"
        }
    }

    AfterAll {
        $ProgressPreference = $oldProgress
    }

    # Validate the output of Invoke-WebRequest
    #
    function ValidateResponse {
        param ($response)

        $response.Error | Should -Be $null

        # A successful call returns: Status = 200, and StatusDescription = "OK"
        $response.Output.StatusDescription | Should -Match "OK"
        $response.Output.StatusCode | Should -Be 200

        # Make sure the response contains the following properties:
        $response.Output.RawContent | Should -Not -Be $null
        $response.Output.Headers | Should -Not -Be $null
        $response.Output.RawContent | Should -Not -Be $null
        $response.Output.RawContentLength | Should -Not -Be $null
        $response.Output.Content | Should -Not -Be $null
    }

    #User-Agent changes on different platforms, so tests should only be run if on the correct platform
    It "Invoke-WebRequest returns Correct User-Agent on MacOSX" -Skip:(!$IsMacOS) {
        $uri = Get-WebListenerUrl -Test 'Get'
        $command = "Invoke-WebRequest -Uri '$uri'"

        $result = ExecuteWebCommand -command $command
        ValidateResponse -response $result

        # Validate response content
        $jsonContent = $result.Output.Content | ConvertFrom-Json
        $jsonContent.headers.'User-Agent' | Should -MatchExactly '.*\(Macintosh;.*\) PowerShell\/\d+\.\d+\.\d+.*'
    }

    It "Invoke-WebRequest returns Correct User-Agent on Linux" -Skip:(!$IsLinux) {
        $uri = Get-WebListenerUrl -Test 'Get'
        $command = "Invoke-WebRequest -Uri '$uri'"

        $result = ExecuteWebCommand -command $command
        ValidateResponse -response $result

        # Validate response content
        $jsonContent = $result.Output.Content | ConvertFrom-Json
        $jsonContent.headers.'User-Agent' | Should -MatchExactly '.*\(Linux;.*\) PowerShell\/\d+\.\d+\.\d+.*'
    }

    It "Invoke-WebRequest returns Correct User-Agent on Windows" -Skip:(!$IsWindows) {
        $uri = Get-WebListenerUrl -Test 'Get'
        $command = "Invoke-WebRequest -Uri '$uri'"

        $result = ExecuteWebCommand -command $command
        ValidateResponse -response $result

        # Validate response content
        $jsonContent = $result.Output.Content | ConvertFrom-Json
        $jsonContent.headers.'User-Agent' | Should -MatchExactly '.*\(Windows NT \d+\.\d*;.*\) PowerShell\/\d+\.\d+\.\d+.*'
    }

    It "Invoke-WebRequest returns headers dictionary" {
        $uri = Get-WebListenerUrl -Test 'Get'
        $command = "Invoke-WebRequest -Uri '$uri'"

        $result = ExecuteWebCommand -command $command
        ValidateResponse -response $result

        # Validate response content
        $jsonContent = $result.Output.Content | ConvertFrom-Json
        $jsonContent.headers.Host | Should -Be $Uri.Authority
    }

    It "Invoke-WebRequest with blank ContentType succeeds" {
        $uri = Get-WebListenerUrl -Test 'Get'
        $command = "Invoke-WebRequest -Uri '$uri' -ContentType ''"

        $result = ExecuteWebCommand -command $command

        # Validate response
        ValidateResponse -response $result

        $result.Output.Headers.'Content-Length' | Should -BeNullOrEmpty
    }

    It "Validate Invoke-WebRequest -DisableKeepAlive" {
        # Operation options
        $uri = Get-WebListenerUrl -Test 'Get'
        $command = "Invoke-WebRequest -Uri $uri -DisableKeepAlive"

        $result = ExecuteWebCommand -command $command
        ValidateResponse -response $result

        $result.Output.Headers.Connection | Should -Be "Close"
    }

    It "Validate Invoke-WebRequest -HttpVersion '<httpVersion>'" -Skip:(!$IsWindows) -TestCases @(
        @{ httpVersion = '1.1'},
        @{ httpVersion = '2'}
    ) {
        param($httpVersion)
        # Operation options
        $uri = Get-WebListenerUrl -Test 'Get' -Https
        $command = "Invoke-WebRequest -Uri $uri -HttpVersion $httpVersion -SkipCertificateCheck"

        $result = ExecuteWebCommand -command $command
        ValidateResponse -response $result

        # Validate response content
        $jsonContent = $result.Output.Content | ConvertFrom-Json
        $jsonContent.protocol | Should -Be "HTTP/$httpVersion"
    }

    It "Validate Invoke-WebRequest -MaximumRedirection" {
        $uri = Get-WebListenerUrl -Test 'Redirect' -TestValue '3'
        $command = "Invoke-WebRequest -Uri '$uri' -MaximumRedirection 4"

        $result = ExecuteWebCommand -command $command
        ValidateResponse -response $result

        # Validate response content
        $jsonContent = $result.Output.Content | ConvertFrom-Json
        $jsonContent.headers.Host | Should -Match $uri.Authority
    }

    It "Validate Invoke-WebRequest error for -MaximumRedirection" {
        $uri = Get-WebListenerUrl -Test 'Redirect' -TestValue '3'
        $command = "Invoke-WebRequest -Uri '$uri' -MaximumRedirection 2"

        $result = ExecuteWebCommand -command $command
        $result.Error.FullyQualifiedErrorId | Should -Be "WebCmdletWebResponseException,Microsoft.PowerShell.Commands.InvokeWebRequestCommand"
    }

    It "Validate Invoke-WebRequest redirect with -Query destination Http" {
        $httpUri = Get-WebListenerUrl -Test 'Get'
        $uri = Get-WebListenerUrl -Test 'Redirect' -Query @{destination = $httpUri}
        $command = "Invoke-WebRequest -Uri '$uri'"

        $result = ExecuteWebCommand -command $command
        $jsonContent = $result.Output.Content | ConvertFrom-Json
        $jsonContent.headers.Host | Should -Match $httpUri.Authority
    }

    It "Validate Invoke-WebRequest redirect with -Query destination Https" {
        $httpsUri = Get-WebListenerUrl -Test 'Get' -Https
        $uri = Get-WebListenerUrl -Test 'Redirect' -Https -Query @{destination = $httpsUri}
        $command = "Invoke-WebRequest -Uri '$uri' -SkipCertificateCheck"

        $result = ExecuteWebCommand -command $command
        $jsonContent = $result.Output.Content | ConvertFrom-Json
        $jsonContent.headers.Host | Should -Match $httpsUri.Authority
    }

    It "Invoke-WebRequest supports request that returns page containing UTF-8 data." {
        $uri = Get-WebListenerUrl -Test 'Encoding' -TestValue 'Utf8'
        $command = "Invoke-WebRequest -Uri '$uri'"

        $result = ExecuteWebCommand -command $command
        ValidateResponse -response $result

        $Result.Output.Encoding.BodyName | Should -Be 'utf-8'
        $Result.Output.Content | Should -Match '⡌⠁⠧⠑ ⠼⠁⠒  ⡍⠜⠇⠑⠹⠰⠎ ⡣⠕⠌'
    }

    It "Invoke-WebRequest supports sending request as UTF-8." {
        $uri = Get-WebListenerUrl -Test 'POST'
        # Body must contain non-ASCII characters
        $command = "Invoke-WebRequest -Uri '$uri' -Body 'проверка' -ContentType 'application/json; charset=utf-8' -Method 'POST'"

        $result = ExecuteWebCommand -command $command
        ValidateResponse -response $result

        $Result.Output.Encoding.BodyName | Should -BeExactly 'utf-8'
        $object = $Result.Output.Content | ConvertFrom-Json
        $object.Data | Should -BeExactly 'проверка'
    }

    It "Invoke-WebRequest supports request that returns page containing CodPage 936 data." {
        $uri = Get-WebListenerUrl -Test 'Encoding' -TestValue 'CP936'
        $command = "Invoke-WebRequest -Uri '$uri'"

        $result = ExecuteWebCommand -command $command
        ValidateResponse -response $result

        $Result.Output.Encoding.CodePage | Should -Be 936
        $Result.Output.Content | Should -Match '测试123'
    }

    It "Invoke-WebRequest validate timeout option" {
        $uri = Get-WebListenerUrl -Test 'Delay' -TestValue '5'
        $command = "Invoke-WebRequest -Uri '$uri' -TimeoutSec 2"

        $result = ExecuteWebCommand -command $command
        $result.Error.FullyQualifiedErrorId | Should -Be "System.Threading.Tasks.TaskCanceledException,Microsoft.PowerShell.Commands.InvokeWebRequestCommand"
    }

    It "Validate Invoke-WebRequest error with -Proxy and -NoProxy option" {
        $uri = Get-WebListenerUrl -Test 'Delay' -TestValue '10'
        $command = "Invoke-WebRequest -Uri '$uri' -Proxy 'http://127.0.0.1:8080' -NoProxy -TimeoutSec 2"

        $result = ExecuteWebCommand -command $command
        $result.Error.FullyQualifiedErrorId | Should -Be "AmbiguousParameterSet,Microsoft.PowerShell.Commands.InvokeWebRequestCommand"
    }

    $testCase = @(
        @{ proxy_address = (Get-WebListenerUrl).Authority; name = 'HTTP proxy'; protocol = 'http' }
        @{ proxy_address = (Get-WebListenerUrl -https).Authority; name = 'HTTPS proxy'; protocol = 'https' }
    )

    It "Validate Invoke-WebRequest with -Proxy option set - '<name>'" -TestCases $testCase {
        param($proxy_address, $name, $protocol)

        # use external url, but with proxy the external url should not actually be called
        $command = "Invoke-WebRequest -Uri ${protocol}://httpbin.org -Proxy '${protocol}://${proxy_address}' -SkipCertificateCheck"
        $result = ExecuteWebCommand -command $command
        $command = "Invoke-WebRequest -Uri '${protocol}://${proxy_address}' -NoProxy"
        $expectedResult = ExecuteWebCommand -command $command
        $result.Output.Content | Should -BeExactly $expectedResult.Output.Content
    }

    # Perform the following operation for Invoke-WebRequest
    # gzip Returns gzip-encoded data.
    # deflate Returns deflate-encoded data.
    # brotli Returns brotli-encoded data.
    # $dataEncodings = @("Chunked", "Compress", "Deflate", "GZip", "Identity")
    # Note: These are the supported options, but we do not have a web service to test them all.
    It "Invoke-WebRequest supports request that returns <DataEncoding>-encoded data." -TestCases @(
        @{ DataEncoding = "gzip" }
        @{ DataEncoding = "deflate" }
        @{ DataEncoding = "brotli" }
    ) {
        param($dataEncoding)
        $uri = Get-WebListenerUrl -Test 'Compression' -TestValue $dataEncoding
        $command = "Invoke-WebRequest -Uri '$uri'"

        $result = ExecuteWebCommand -command $command
        ValidateResponse -response $result

        # Validate response content
        # The content should be de-compressed, and otherwise converting from JSON will fail.
        $jsonContent = $result.Output.Content | ConvertFrom-Json
        $jsonContent.Headers.Host | Should -BeExactly $uri.Authority
    }

    # Perform the following operation for Invoke-WebRequest using the following content types: "text/plain", "application/xml", "application/xml"
    # post Returns POST data.
    # patch Returns PATCH data.
    # put Returns PUT data.
    # delete Returns DELETE data
    $testMethods = @("POST", "PATCH", "PUT", "DELETE")
    $contentTypes = @("text/plain", "application/xml", "application/json")

    foreach ($contentType in $contentTypes) {
        foreach ($method in $testMethods) {
            # Operation options
            $uri = Get-WebListenerUrl -Test $method
            $body = GetTestData -contentType $contentType
            $command = "Invoke-WebRequest -Uri $uri -Body '$body' -Method $method -ContentType $contentType"

            It "Invoke-WebRequest -Uri $uri  -Method $method -ContentType $contentType -Body [body data]" {

                $result = ExecuteWebCommand -command $command
                ValidateResponse -response $result

                # Validate response content
                $jsonContent = $result.Output.Content | ConvertFrom-Json
                $jsonContent.url | Should -Match $uri
                $jsonContent.headers.'Content-Type' | Should -Match $contentType
                # Validate that the response Content.data field is the same as what we sent.
                $jsonContent.data | Should -Be $body
            }
        }
    }

    It "Validate Invoke-WebRequest -Headers --> Set KeepAlive to false via headers" {
        $uri = Get-WebListenerUrl -Test 'Get'
        $result = ExecuteRequestWithHeaders -cmdletName Invoke-WebRequest -uri $uri
        ValidateResponse -response $result
        $result.Output.Headers.Connection | Should -Be "Close"
    }

    # Validate all available user agents for Invoke-WebRequest
    $agents = @{
        InternetExplorer = "MSIE 9.0"
        Chrome           = "Chrome"
        Opera            = "Opera"
        Safari           = "Safari"
        FireFox          = "Firefox"
    }

    foreach ($agentName in $agents.Keys) {
        $expectedAgent = $agents[$agentName]
        $uri = Get-WebListenerUrl -Test 'Get'
        $userAgent = "[Microsoft.PowerShell.Commands.PSUserAgent]::$agentName"
        $command = "Invoke-WebRequest -Uri $uri -UserAgent ($userAgent) "

        It "Validate Invoke-WebRequest UserAgent. Execute--> $command" {

            $result = ExecuteWebCommand -command $command
            ValidateResponse -response $result

            # Validate response content
            $jsonContent = $result.Output.Content | ConvertFrom-Json
            $jsonContent.headers.Host | Should -Be $uri.Authority
            $jsonContent.headers.'User-Agent' | Should -Match $expectedAgent
        }
    }

    It "Validate Invoke-WebRequest -OutFile" {
        $uri = Get-WebListenerUrl -Test 'Get'
        $result = ExecuteRequestWithOutFile -cmdletName "Invoke-WebRequest" -uri $uri
        $jsonContent = $result.Output | ConvertFrom-Json
        $jsonContent.headers.Host | Should -Be $uri.Authority
    }

    It "Invoke-WebRequest -OutFile folder Downloads the file and names it" {
        $uri = Get-WebListenerUrl -Test 'Get'
        $content = Invoke-WebRequest -Uri $uri
        $outFile = Join-Path $TestDrive $content.BaseResponse.RequestMessage.RequestUri.Segments[-1]

        # ensure the file does not exist
        Remove-Item -Force -ErrorAction 'SilentlyContinue' -Path $outFile
        Invoke-WebRequest -Uri $uri -OutFile $TestDrive

        Test-Path $outFile | Should -Be $true
        Get-Item $outFile | Select-Object -ExpandProperty Length | Should -Be $content.Content.Length
    }

    It "Invoke-WebRequest should fail if -OutFile is <Name>." -TestCases @(
        @{ Name = "empty"; Value = [string]::Empty }
        @{ Name = "null"; Value = $null }
    ) {
        param ($value)
        $uri = Get-WebListenerUrl -Test 'Get'
        $errorId = "ParameterArgumentValidationError,Microsoft.PowerShell.Commands.InvokeWebRequestCommand"
        { Invoke-WebRequest -Uri $uri -OutFile $value} | Should -Throw -ErrorId $errorId
    }

    It "Validate Invoke-WebRequest handles missing Content-Type in response header" {
        #Validate that exception is not thrown when response headers are missing Content-Type.
        $uri = Get-WebListenerUrl -Test 'ResponseHeaders' -Query @{'Content-Type' = ''}
        $command = "Invoke-WebRequest -Uri '$uri'"
        $result = ExecuteWebCommand -command $command
        $result.Error | Should -BeNullOrEmpty
    }

    It "Validate Invoke-WebRequest StandardMethod and CustomMethod parameter sets" {
        $uri = Get-WebListenerUrl -Test 'Get'
        #Validate that parameter sets are functioning correctly
        $errorId = "AmbiguousParameterSet,Microsoft.PowerShell.Commands.InvokeWebRequestCommand"
        { Invoke-WebRequest -Uri $uri -Method GET -CustomMethod TEST } | Should -Throw -ErrorId $errorId
    }

    It "Validate Invoke-WebRequest CustomMethod method is used" {
        $uri = Get-WebListenerUrl -Test 'Get'
        $command = "Invoke-WebRequest -Uri '$uri' -CustomMethod TEST"
        $result = ExecuteWebCommand -command $command
        $result.Error | Should -BeNullOrEmpty
        ($result.Output.Content | ConvertFrom-Json).method | Should -Be "TEST"
    }

    It "Validate Invoke-WebRequest default ContentType for CustomMethod POST" {
        $uri = Get-WebListenerUrl -Test 'Post'
        $command = "Invoke-WebRequest -Uri '$uri' -CustomMethod POST -Body 'testparam=testvalue'"
        $result = ExecuteWebCommand -command $command
        $jsonResult = $result.Output.Content | ConvertFrom-Json
        $jsonResult.form.testparam | Should -Be "testvalue"
        $jsonResult.Headers.'Content-Type' | Should -Be "application/x-www-form-urlencoded"
    }

    It "Validate Invoke-WebRequest body is converted to query params for CustomMethod GET" {
        $uri = Get-WebListenerUrl -Test 'Get'
        $command = "Invoke-WebRequest -Uri '$uri' -CustomMethod GET -Body @{'testparam'='testvalue'}"
        $result = ExecuteWebCommand -command $command
        ($result.Output.Content | ConvertFrom-Json).args.testparam | Should -Be "testvalue"
    }

    It 'Validate Invoke-WebRequest empty body CustomMethod GET' {
        $uri = Get-WebListenerUrl -Test 'Get'
        $command = "Invoke-WebRequest -Uri '$uri' -CustomMethod GET"
        $result = ExecuteWebCommand -command $command
        $result.Output.Headers.'Content-Length' | Should -BeNullOrEmpty
    }

    It "Validate Invoke-WebRequest body is converted to query params for CustomMethod GET and -NoProxy" {
        $uri = Get-WebListenerUrl -Test 'Get'
        $command = "Invoke-WebRequest -Uri '$uri' -CustomMethod GET -Body @{'testparam'='testvalue'} -NoProxy"
        $result = ExecuteWebCommand -command $command
        ($result.Output.Content | ConvertFrom-Json).query | Should -Be "?testparam=testvalue"
    }

    It "Validate Invoke-WebRequest returns HTTP errors in exception" {
        $query = @{
            body           = "I am a teapot!!!"
            statuscode     = 418
            responsephrase = "I am a teapot"
        }
        $uri = Get-WebListenerUrl -Test 'Response' -Query $query
        $command = "Invoke-WebRequest -Uri '$uri'"
        $result = ExecuteWebCommand -command $command

        $result.Error.ErrorDetails.Message | Should -Be $query.body
        $result.Error.Exception | Should -BeOfType Microsoft.PowerShell.Commands.HttpResponseException
        $result.Error.Exception.Response.StatusCode | Should -Be 418
        $result.Error.Exception.Response.ReasonPhrase | Should -Be $query.responsephrase
        $result.Error.Exception.Message | Should -Match ": 418 \($($query.responsephrase)\)\."
        $result.Error.FullyQualifiedErrorId | Should -Be "WebCmdletWebResponseException,Microsoft.PowerShell.Commands.InvokeWebRequestCommand"
    }

    It "Validate Invoke-WebRequest returns empty RelationLink property if there is no Link Header" {
        $uri = $uri = Get-WebListenerUrl -Test 'Get'
        $command = "Invoke-WebRequest -Uri '$uri'"
        $result = ExecuteWebCommand -command $command

        $result.Output.RelationLink.Count | Should -Be 0
    }

    It "Validate Invoke-WebRequest returns valid RelationLink property with absolute uris if Link Header is present" {
        $uri = Get-WebListenerUrl -Test 'Link' -Query @{maxlinks = 5; linknumber = 2}
        $command = "Invoke-WebRequest -Uri '$uri'"
        $result = ExecuteWebCommand -command $command

        $result.Output.RelationLink.Count | Should -BeExactly 5
        $baseUri = Get-WebListenerUrl -Test 'Link'
        $result.Output.RelationLink["next"] | Should -BeExactly "${baseUri}?maxlinks=5&linknumber=3&type=default"
        $result.Output.RelationLink["last"] | Should -BeExactly "${baseUri}?maxlinks=5&linknumber=5&type=default"
        $result.Output.RelationLink["prev"] | Should -BeExactly "${baseUri}?maxlinks=5&linknumber=1&type=default"
        $result.Output.RelationLink["first"] | Should -BeExactly "${baseUri}?maxlinks=5&linknumber=1&type=default"
        $result.Output.RelationLink["self"] | Should -BeExactly "${baseUri}?maxlinks=5&linknumber=2&type=default"
    }

    It "Validate Invoke-WebRequest returns valid RelationLink property with absolute uris if Multiple Link Headers are present" {
        $uri = Get-WebListenerUrl -Test 'Link' -Query @{maxlinks = 5; linknumber = 2; type = 'multiple'}
        $command = "Invoke-WebRequest -Uri '$uri'"
        $result = ExecuteWebCommand -command $command

        $result.Output.RelationLink.Count | Should -BeExactly 5
        $baseUri = Get-WebListenerUrl -Test 'Link'
        $result.Output.RelationLink["next"] | Should -BeExactly "${baseUri}?maxlinks=5&linknumber=3&type=multiple"
        $result.Output.RelationLink["last"] | Should -BeExactly "${baseUri}?maxlinks=5&linknumber=5&type=multiple"
        $result.Output.RelationLink["prev"] | Should -BeExactly "${baseUri}?maxlinks=5&linknumber=1&type=multiple"
        $result.Output.RelationLink["first"] | Should -BeExactly "${baseUri}?maxlinks=5&linknumber=1&type=multiple"
        $result.Output.RelationLink["self"] | Should -BeExactly "${baseUri}?maxlinks=5&linknumber=2&type=multiple"
    }


    It "Validate Invoke-WebRequest RelationLink keys are treated case-insensitively" {
        $uri = Get-WebListenerUrl -Test 'Link' -Query @{maxlinks = 5; linknumber = 2; type = 'multiple'}
        $command = "Invoke-WebRequest -Uri '$uri'"
        $result = ExecuteWebCommand -command $command

        $result.Output.RelationLink["next"] | Should -Not -BeNullOrEmpty
        $result.Output.RelationLink["next"] | Should -BeExactly $result.Output.RelationLink["Next"]

        $result.Output.RelationLink["last"] | Should -Not -BeNullOrEmpty
        $result.Output.RelationLink["last"] | Should -BeExactly $result.Output.RelationLink["LAST"]

        $result.Output.RelationLink["prev"] | Should -Not -BeNullOrEmpty
        $result.Output.RelationLink["prev"] | Should -BeExactly $result.Output.RelationLink["preV"]

        $result.Output.RelationLink["first"] | Should -Not -BeNullOrEmpty
        $result.Output.RelationLink["first"] | Should -BeExactly $result.Output.RelationLink["FiRsT"]

        $result.Output.RelationLink["self"] | Should -Not -BeNullOrEmpty
        $result.Output.RelationLink["self"] | Should -BeExactly $result.Output.RelationLink["self"]
    }

    It "Validate Invoke-WebRequest quietly ignores invalid Link Headers in RelationLink property: <type>" -TestCases @(
        @{ type = "noUrl" }
        @{ type = "malformed" }
        @{ type = "noRel" }
    ) {
        param($type)
        $uri = Get-WebListenerUrl -Test 'Link' -Query @{type = $type}
        $command = "Invoke-WebRequest -Uri '$uri'"
        $result = ExecuteWebCommand -command $command

        $result.Output.RelationLink.Count | Should -BeExactly 3
        $baseUri = Get-WebListenerUrl -Test 'Link'
        $result.Output.RelationLink["last"] | Should -BeExactly "${baseUri}?maxlinks=3&linknumber=3&type=${type}"
        $result.Output.RelationLink["first"] | Should -BeExactly "${baseUri}?maxlinks=3&linknumber=1&type=${type}"
        $result.Output.RelationLink["self"] | Should -BeExactly "${baseUri}?maxlinks=3&linknumber=1&type=${type}"
    }

    It "Validate Invoke-WebRequest handles different whitespace for Link Headers: <type>" -TestCases @(
        @{ type = "noWhitespace" }
        @{ type = "extraWhitespace" }
    ) {
        param($type)
        $uri = Get-WebListenerUrl -Test 'Link' -Query @{type = $type}
        $command = "Invoke-WebRequest -Uri '$uri'"
        $result = ExecuteWebCommand -command $command

        $result.Output.RelationLink.Count | Should -BeExactly 4
        $baseUri = Get-WebListenerUrl -Test 'Link'
        $result.Output.RelationLink["last"] | Should -BeExactly "${baseUri}?maxlinks=3&linknumber=3&type=${type}"
        $result.Output.RelationLink["first"] | Should -BeExactly "${baseUri}?maxlinks=3&linknumber=1&type=${type}"
        $result.Output.RelationLink["self"] | Should -BeExactly "${baseUri}?maxlinks=3&linknumber=1&type=${type}"
        $result.Output.RelationLink["next"] | Should -BeExactly "${baseUri}?maxlinks=3&linknumber=2&type=${type}"
    }

    It "Verify Invoke-WebRequest supresses terminating errors with -SkipHttpErrorCheck" {
        $uri =  Get-WebListenerUrl -Test 'Response' -Query $NotFoundQuery
        $command = "Invoke-WebRequest -SkipHttpErrorCheck -Uri '$uri'"
        $result = ExecuteWebCommand -Command $command
        $result.output.StatusCode | Should -Be 404
        $result.output.Content | Should -BeExactly "oops"
        $result.error | Should -BeNullOrEmpty
    }

    It "Verify Invoke-WebRequest terminates without -SkipHttpErrorCheck" {
        $uri =  Get-WebListenerUrl -Test 'Response' -Query $NotFoundQuery
        $command = "Invoke-WebRequest -Uri '$uri'"
        $result = ExecuteWebCommand -Command $command
        $result.output | Should -BeNullOrEmpty
        $result.error | Should -Not -BeNullOrEmpty
    }

    Context "Redirect" {
        It "Validates Invoke-WebRequest with -PreserveAuthorizationOnRedirect preserves the authorization header on redirect: <redirectType> <redirectedMethod>" -TestCases $redirectTests {
            param($redirectType, $redirectedMethod)
            $uri = Get-WebListenerUrl -Test 'Redirect' -Query @{type = $redirectType}
            $response = ExecuteRedirectRequest -Uri $uri -PreserveAuthorizationOnRedirect

            $response.Error | Should -BeNullOrEmpty
            $response.Content.Headers."Authorization" | Should -BeExactly "test"
        }

        It "Validates Invoke-WebRequest with -PreserveAuthorizationOnRedirect respects -MaximumRedirection on redirect: <redirectType> <redirectedMethod>" -TestCases $redirectTests {
            param($redirectType, $redirectedMethod)
            $uri = Get-WebListenerUrl -Test 'Redirect' -TestValue '3' -Query @{type = $redirectType}
            $response = ExecuteRedirectRequest -Uri $uri -PreserveAuthorizationOnRedirect -MaximumRedirection 2

            $response.Error.FullyQualifiedErrorId | Should -Be "WebCmdletWebResponseException,Microsoft.PowerShell.Commands.InvokeWebRequestCommand"
        }

        It "Validates Invoke-WebRequest preserves the authorization header on multiple redirects: <redirectType>" -TestCases $redirectTests {
            param($redirectType)
            $uri = Get-WebListenerUrl -Test 'Redirect' -TestValue 3 -Query @{type = $redirectType}
            $response = ExecuteRedirectRequest -Uri $uri -PreserveAuthorizationOnRedirect

            $response.Error | Should -BeNullOrEmpty
            $response.Content.Headers."Authorization" | Should -BeExactly "test"
        }

        It "Validates Invoke-WebRequest strips the authorization header on various redirects: <redirectType>" -TestCases $redirectTests {
            param($redirectType)
            $uri = Get-WebListenerUrl -Test 'Redirect' -Query @{type = $redirectType}
            $response = ExecuteRedirectRequest -Uri $uri

            $response.Error | Should -BeNullOrEmpty
            # ensure user-agent is present (i.e., no false positives )
            $response.Content.Headers."User-Agent" | Should -Not -BeNullOrEmpty
            # ensure Authorization header has been removed.
            $response.Content.Headers."Authorization" | Should -BeNullOrEmpty
        }

        # NOTE: Only testing redirection of POST -> GET for unique underlying values of HttpStatusCode.
        # Some names overlap in underlying value.
        It "Validates Invoke-WebRequest strips the authorization header redirects and switches from POST to GET when it handles the redirect: <redirectType> <redirectedMethod>" -TestCases $redirectTests {
            param($redirectType, $redirectedMethod)
            $uri = Get-WebListenerUrl -Test 'Redirect' -Query @{type = $redirectType}
            $response = ExecuteRedirectRequest -Uri $uri -Method 'POST'

            $response.Error | Should -BeNullOrEmpty
            # ensure user-agent is present (i.e., no false positives )
            $response.Content.Headers."User-Agent" | Should -Not -BeNullOrEmpty
            # ensure Authorization header has been removed.
            $response.Content.Headers."Authorization" | Should -BeNullOrEmpty
            # ensure POST was changed to GET for selected redirections and remains as POST for others.
            $response.Content.Method | Should -Be $redirectedMethod
        }

        It "Validates Invoke-WebRequest -PreserveAuthorizationOnRedirect keeps the authorization header redirects and switches from POST to GET when it handles the redirect: <redirectType> <redirectedMethod>" -TestCases $redirectTests {
            param($redirectType, $redirectedMethod)
            $uri = Get-WebListenerUrl -Test 'Redirect' -Query @{type = $redirectType}
            $response = ExecuteRedirectRequest -PreserveAuthorizationOnRedirect -Uri $uri -Method 'POST'

            $response.Error | Should -BeNullOrEmpty
            # ensure user-agent is present (i.e., no false positives )
            $response.Content.Headers."User-Agent" | Should -Not -BeNullOrEmpty
            # ensure Authorization header has been removed.
            $response.Content.Headers."Authorization" | Should -BeExactly 'test'
            # ensure POST was changed to GET for selected redirections and remains as POST for others.
            $response.Content.Method | Should -Be $redirectedMethod
        }

        It "Validates Invoke-WebRequest handles responses without Location header for requests with Authorization header and redirect: <redirectType>" -TestCases $redirectTests {
            param($redirectType, $redirectedMethod)
            # Skip relative test as it is not a valid response type.
            if ($redirectType -eq 'relative') { return }

            # When an Authorization request header is present,
            # and -PreserveAuthorizationOnRedirect is not present,
            # PowerShell should throw an HTTP Response Exception
            # for a redirect response which does not contain a Location response header.
            # The correct redirect status code should be included in the exception.

            $StatusCode = [int][System.Net.HttpStatusCode]$redirectType
            $uri = Get-WebListenerUrl -Test Response -Query @{statuscode = $StatusCode}
            $command = "Invoke-WebRequest -Uri '$uri' -Headers @{Authorization = 'foo'}"
            $response = ExecuteWebCommand -command $command

            $response.Error.Exception | Should -BeOfType Microsoft.PowerShell.Commands.HttpResponseException
            $response.Error.Exception.Response.StatusCode | Should -Be $StatusCode
            $response.Error.Exception.Response.Headers.Location | Should -BeNullOrEmpty
        }

        It "Validate Invoke-WebRequest Https to Http redirect with -AllowInsecureRedirect" {
            $httpUri = Get-WebListenerUrl -Test 'Get'
            $uri = Get-WebListenerUrl -Test 'Redirect' -Https -Query @{destination = $httpUri}
            $command = "Invoke-WebRequest -Uri '$uri' -SkipCertificateCheck -AllowInsecureRedirect"

            $result = ExecuteWebCommand -command $command
            $jsonContent = $result.Output.Content | ConvertFrom-Json
            $jsonContent.headers.Host | Should -Match $httpUri.Authority
        }

        It "Validate Invoke-WebRequest Https to Http redirect without -AllowInsecureRedirect" {
            $httpUri = Get-WebListenerUrl -Test 'Get'
            $uri = Get-WebListenerUrl -Test 'Redirect' -Https -Query @{destination = $httpUri}
            $command = "Invoke-WebRequest -Uri '$uri' -SkipCertificateCheck"

            $result = ExecuteWebCommand -command $command
            $result.Error.FullyQualifiedErrorId | Should -Be "WebCmdletWebResponseException,Microsoft.PowerShell.Commands.InvokeWebRequestCommand"
        }
    }


    Context "Invoke-WebRequest SkipHeaderVerification Tests" {
        BeforeAll {
            $Testfile = Join-Path $testdrive 'SkipHeaderVerification.txt'
            'bar' | Set-Content $Testfile
        }

        It "Verifies Invoke-WebRequest default header handling with no errors" {
            $uri = Get-WebListenerUrl -Test 'Get'
            $headers = @{"If-Match" = "*"}
            $response = ExecuteRequestWithCustomHeaders -Uri $uri -headers $headers

            $response.Error | Should -BeNullOrEmpty
            $response.Content.Headers."If-Match" | Should -BeExactly "*"
        }

        It "Verifies Invoke-WebRequest default header handling reports an error is returned for an invalid If-Match header value" {
            $uri = Get-WebListenerUrl -Test 'Get'
            $headers = @{"If-Match" = "12345"}
            $response = ExecuteRequestWithCustomHeaders -Uri $uri -headers $headers

            $response.Error | Should -Not -BeNullOrEmpty
            $response.Error.FullyQualifiedErrorId | Should -Be "System.FormatException,Microsoft.PowerShell.Commands.InvokeWebRequestCommand"
            $response.Error.Exception.Message | Should -Be "The format of value '12345' is invalid."
        }

        It "Verifies Invoke-WebRequest header handling does not report an error when using -SkipHeaderValidation" {
            $uri = Get-WebListenerUrl -Test 'Get'
            $headers = @{"If-Match" = "12345"}
            $response = ExecuteRequestWithCustomHeaders -Uri $uri -headers $headers -SkipHeaderValidation

            $response.Error | Should -BeNullOrEmpty
            $response.Content.Headers."If-Match" | Should -BeExactly "12345"
        }

        It "Verifies Invoke-WebRequest default UserAgent handling with no errors" {
            $uri = Get-WebListenerUrl -Test 'Get'
            $UserAgent = [Microsoft.PowerShell.Commands.PSUserAgent]::InternetExplorer
            $response = ExecuteRequestWithCustomUserAgent -Uri $uri -UserAgent $UserAgent -Cmdlet "Invoke-WebRequest"

            $response.Error | Should -BeNullOrEmpty
            $Pattern = [regex]::Escape($UserAgent)
            $response.Content.Headers."User-Agent" | Should -Match $Pattern
        }

        It "Verifies Invoke-WebRequest default UserAgent handling reports an error is returned for an invalid UserAgent value" {
            $uri = Get-WebListenerUrl -Test 'Get'
            $UserAgent = 'Invalid:Agent'
            $response = ExecuteRequestWithCustomUserAgent -Uri $uri -UserAgent $UserAgent  -Cmdlet "Invoke-WebRequest"

            $response.Error | Should -Not -BeNullOrEmpty
            $response.Error.FullyQualifiedErrorId | Should -Be "System.FormatException,Microsoft.PowerShell.Commands.InvokeWebRequestCommand"
            $response.Error.Exception.Message | Should -Be "The format of value 'Invalid:Agent' is invalid."
        }

        It "Verifies Invoke-WebRequest UserAgent handling does not report an error when using -SkipHeaderValidation" {
            $uri = Get-WebListenerUrl -Test 'Get'
            $UserAgent = 'Invalid:Agent'
            $response = ExecuteRequestWithCustomUserAgent -Uri $uri -UserAgent $UserAgent  -SkipHeaderValidation -Cmdlet "Invoke-WebRequest"

            $response.Error | Should -BeNullOrEmpty
            $Pattern = [regex]::Escape($UserAgent)
            $response.Content.Headers."User-Agent" | Should -Match $Pattern
        }

        It "Verifies Invoke-WebRequest default ContentType handling reports no error is returned for a valid Content-Type header value and -Body" {
            $contentType = 'text/plain'
            $body = "bar"
            $uri = Get-WebListenerUrl -Test 'Post'

            $response = Invoke-WebRequest -Uri $uri -Method 'Post' -ContentType $contentType -Body $body
            $result = $response.Content | ConvertFrom-Json

            $result.data | Should -BeExactly $body
            $result.headers.'Content-Type' | Should -BeExactly $contentType
        }

        It "Verifies Invoke-WebRequest default ContentType handling reports an error is returned for an invalid Content-Type header value and -Body" {
            $contentType = 'foo'
            $body = "bar"
            $uri = Get-WebListenerUrl -Test 'Post'

            { Invoke-WebRequest -Uri $uri -Method 'Post' -ContentType $contentType -Body $body -ErrorAction 'Stop' } |
                Should -Throw -ErrorId "WebCmdletContentTypeException,Microsoft.PowerShell.Commands.InvokeWebRequestCommand"
        }

        It "Verifies Invoke-WebRequest ContentType handling reports no error is returned for an invalid Content-Type header value, -Body, and -SkipHeaderValidation" {
            $contentType = 'foo'
            $body = "bar"
            $uri = Get-WebListenerUrl -Test 'Post'

            $response = Invoke-WebRequest -Uri $uri -Method 'Post' -ContentType $contentType -Body $body -SkipHeaderValidation
            $result = $response.Content | ConvertFrom-Json

            $result.data | Should -BeExactly 'bar'
            $result.headers.'Content-Type' | Should -BeExactly $contentType
        }

        It "Verifies Invoke-WebRequest default ContentType handling reports no error is returned for a valid Content-Type header value and -InFile" {
            $contentType = 'text/plain'
            $uri = Get-WebListenerUrl -Test 'Post'

            $response = Invoke-WebRequest -Uri $uri -Method 'Post' -ContentType $contentType -InFile $Testfile
            $result = $response.Content | ConvertFrom-Json

            # Match used due to inconsistent newline rendering
            $result.data | Should -Match 'bar'
            $result.headers.'Content-Type' | Should -BeExactly $contentType
        }

        It "Verifies Invoke-WebRequest default ContentType handling reports an error is returned for an invalid Content-Type header value and -InFile" {
            $contentType = 'foo'
            $uri = Get-WebListenerUrl -Test 'Post'

            { Invoke-WebRequest -Uri $uri -Method 'Post' -ContentType $contentType -InFile $Testfile -ErrorAction 'Stop' } |
                Should -Throw -ErrorId "WebCmdletContentTypeException,Microsoft.PowerShell.Commands.InvokeWebRequestCommand"
        }

        It "Verifies Invoke-WebRequest default ContentType handling reports no error is returned for an invalid Content-Type header value, -Infile, and -SkipHeaderValidation" {
            $contentType = 'foo'
            $uri = Get-WebListenerUrl -Test 'Post'

            $response = Invoke-WebRequest -Uri $uri -Method 'Post' -ContentType $contentType -InFile $Testfile -SkipHeaderValidation
            $result = $response.Content | ConvertFrom-Json

            # Match used due to inconsistent newline rendering
            $result.data | Should -Match 'bar'
            $result.headers.'Content-Type' | Should -BeExactly $contentType
        }

        It "Verifies Invoke-WebRequest applies -ContentType when no -Body is present" {
            $contentType = 'application/json'
            $uri = Get-WebListenerUrl -Test 'Get'

            $response = Invoke-WebRequest -Uri $uri -Method 'GET' -ContentType $contentType
            $result = $response.Content | ConvertFrom-Json

            $result.data | Should -BeNullOrEmpty
            $result.headers.'Content-Type' | Should -BeExactly $contentType
        }

        It "Verifies Invoke-WebRequest applies an invalid -ContentType when no -Body is present and -SkipHeaderValidation is present" {
            $contentType = 'foo'
            $uri = Get-WebListenerUrl -Test 'Get'

            $response = Invoke-WebRequest -Uri $uri -Method 'GET' -ContentType $contentType -SkipHeaderValidation
            $result = $response.Content | ConvertFrom-Json

            $result.data | Should -BeNullOrEmpty
            $result.headers.'Content-Type' | Should -BeExactly $contentType
        }
    }

    #region charset encoding tests

    Context  "BasicHtmlWebResponseObject Encoding tests" {
        It "Verifies Invoke-WebRequest detects charset meta value when the ContentType header does not define it." {
            $query = @{
                contenttype = 'text/html'
                body        = '<html><head><meta charset="Unicode"></head></html>'
            }
            $uri = Get-WebListenerUrl -Test 'Response' -Query $query
            $expectedEncoding = [System.Text.Encoding]::GetEncoding('Unicode')
            $response = ExecuteWebRequest -Uri $uri -UseBasicParsing

            $response.Error | Should -BeNullOrEmpty
            $response.Output.Encoding.EncodingName | Should -Be $expectedEncoding.EncodingName
            $response.Output | Should -BeOfType Microsoft.PowerShell.Commands.BasicHtmlWebResponseObject
        }

        It "Verifies Invoke-WebRequest detects charset meta value when newlines are encountered in the element." {
            $query = @{
                contenttype = 'text/html'
                body        = "<html>`n    <head>`n        <meta`n            charset=`"Unicode`"`n            >`n    </head>`n</html>"
            }
            $uri = Get-WebListenerUrl -Test 'Response' -Query $query
            $expectedEncoding = [System.Text.Encoding]::GetEncoding('Unicode')
            $response = ExecuteWebRequest -Uri $uri -UseBasicParsing

            $response.Error | Should -BeNullOrEmpty
            $response.Output.Encoding.EncodingName | Should -Be $expectedEncoding.EncodingName
            $response.Output | Should -BeOfType Microsoft.PowerShell.Commands.BasicHtmlWebResponseObject
        }

        It "Verifies Invoke-WebRequest detects charset meta value when the attribute value is unquoted." {
            $query = @{
                contenttype = 'text/html'
                body        = '<html><head><meta charset = Unicode></head></html>'
            }
            $uri = Get-WebListenerUrl -Test 'Response' -Query $query
            $expectedEncoding = [System.Text.Encoding]::GetEncoding('Unicode')
            $response = ExecuteWebRequest -Uri $uri -UseBasicParsing

            $response.Error | Should -BeNullOrEmpty
            $response.Output.Encoding.EncodingName | Should -Be $expectedEncoding.EncodingName
            $response.Output | Should -BeOfType Microsoft.PowerShell.Commands.BasicHtmlWebResponseObject
        }

        It "Verifies Invoke-WebRequest detects http-equiv charset meta value when the ContentType header does not define it." {
            $query = @{
                contenttype = 'text/html'
                body        = "<html><head>`n<meta http-equiv=`"content-type`" content=`"text/html; charset=Unicode`">`n</head>`n</html>"
            }
            $uri = Get-WebListenerUrl -Test 'Response' -Query $query
            $expectedEncoding = [System.Text.Encoding]::GetEncoding('Unicode')
            $response = ExecuteWebRequest -Uri $uri -UseBasicParsing

            $response.Error | Should -BeNullOrEmpty
            $response.Output.Encoding.EncodingName | Should -Be $expectedEncoding.EncodingName
            $response.Output | Should -BeOfType Microsoft.PowerShell.Commands.BasicHtmlWebResponseObject
        }

        It "Verifies Invoke-WebRequest detects http-equiv charset meta value newlines are encountered in the element." {
            $query = @{
                contenttype = 'text/html'
                body        = "<html><head>`n<meta`n    http-equiv=`"content-type`"`n    content=`"text/html; charset=Unicode`">`n</head>`n</html>"
            }
            $uri = Get-WebListenerUrl -Test 'Response' -Query $query
            $expectedEncoding = [System.Text.Encoding]::GetEncoding('Unicode')
            $response = ExecuteWebRequest -Uri $uri -UseBasicParsing

            $response.Error | Should -BeNullOrEmpty
            $response.Output.Encoding.EncodingName | Should -Be $expectedEncoding.EncodingName
            $response.Output | Should -BeOfType Microsoft.PowerShell.Commands.BasicHtmlWebResponseObject
        }

        It "Verifies Invoke-WebRequest ignores meta charset value when Content-Type header defines it." {
            $query = @{
                contenttype = 'text/html; charset=utf-8'
                body        = '<html><head><meta charset="utf-32"></head></html>'
            }
            $uri = Get-WebListenerUrl -Test 'Response' -Query $query
            # NOTE: meta charset should be ignored
            $expectedEncoding = [System.Text.Encoding]::UTF8
            $response = ExecuteWebRequest -Uri $uri -UseBasicParsing

            $response.Error | Should -BeNullOrEmpty
            $response.Output.Encoding.EncodingName | Should -Be $expectedEncoding.EncodingName
            $response.Output | Should -BeOfType Microsoft.PowerShell.Commands.BasicHtmlWebResponseObject
        }

        It "Verifies Invoke-WebRequest honors non-utf8 charsets in the Content-Type header" {
            $query = @{
                contenttype = 'text/html; charset=utf-16'
                body        = '<html><head><meta charset="utf-32"></head></html>'
            }
            $uri = Get-WebListenerUrl -Test 'Response' -Query $query
            # NOTE: meta charset should be ignored
            $expectedEncoding = [System.Text.Encoding]::GetEncoding('utf-16')
            $response = ExecuteWebRequest -Uri $uri -UseBasicParsing

            $response.Error | Should -BeNullOrEmpty
            $response.Output.Encoding.EncodingName | Should -Be $expectedEncoding.EncodingName
            $response.Output | Should -BeOfType Microsoft.PowerShell.Commands.BasicHtmlWebResponseObject
        }

        It "Verifies Invoke-WebRequest defaults to iso-UTF-8 when an unsupported/invalid charset is declared" {
            $query = @{
                contenttype = 'text/html'
                body        = '<html><head><meta charset="invalid"></head></html>'
            }
            $uri = Get-WebListenerUrl -Test 'Response' -Query $query
            $expectedEncoding = [System.Text.Encoding]::UTF8
            $response = ExecuteWebRequest -Uri $uri -UseBasicParsing

            $response.Error | Should -BeNullOrEmpty
            $response.Output.Encoding.EncodingName | Should -Be $expectedEncoding.EncodingName
            $response.Output | Should -BeOfType Microsoft.PowerShell.Commands.BasicHtmlWebResponseObject
        }

        It "Verifies Invoke-WebRequest defaults to UTF-8 when an unsupported/invalid charset is declared using http-equiv" {
            $query = @{
                contenttype = 'text/html'
                body        = "<html><head>`n<meta http-equiv=`"content-type`" content=`"text/html; charset=Invalid`">`n</head>`n</html>"
            }
            $uri = Get-WebListenerUrl -Test 'Response' -Query $query
            $expectedEncoding = [System.Text.Encoding]::UTF8
            $response = ExecuteWebRequest -Uri $uri -UseBasicParsing

            $response.Error | Should -BeNullOrEmpty
            $response.Output.Encoding.EncodingName | Should -Be $expectedEncoding.EncodingName
            $response.Output | Should -BeOfType Microsoft.PowerShell.Commands.BasicHtmlWebResponseObject
        }
    }

    #endregion charset encoding tests

    #region Content Header Inclusion
    Context "Content Header" {
        It "Verifies Invoke-WebRequest includes Content headers in Headers property" {
            $query = @{
                contenttype = 'text/plain'
                body        = 'OK'
            }
            $uri = Get-WebListenerUrl -Test 'Response' -Query $query
            $command = "Invoke-WebRequest -Uri '$uri'"
            $result = ExecuteWebCommand -command $command
            ValidateResponse $result

            $result.Output.Headers.'Content-Type' | Should -Be 'text/plain'
            $result.Output.Headers.'Content-Length' | Should -Be 2
        }

        It "Verifies Invoke-WebRequest includes Content headers in RawContent property" {
            $query = @{
                contenttype = 'text/plain'
                body        = 'OK'
            }
            $uri = Get-WebListenerUrl -Test 'Response' -Query $query
            $command = "Invoke-WebRequest -Uri '$uri'"
            $result = ExecuteWebCommand -command $command
            ValidateResponse $result

            $result.Output.RawContent | Should -Match ([regex]::Escape('Content-Type: text/plain'))
            $result.Output.RawContent | Should -Match ([regex]::Escape('Content-Length: 2'))
        }

        It "Verifies Invoke-WebRequest Supports Multiple response headers with same name" {
            $query = @{
                contenttype = 'text/plain'
                body        = 'OK'
                headers     = @{
                    'X-Fake-Header' = @('testvalue01', 'testvalue02')
                } | ConvertTo-Json -Compress
            }
            $uri = Get-WebListenerUrl -Test 'Response' -Query $query
            $command = "Invoke-WebRequest -Uri '$uri'"
            $result = ExecuteWebCommand -command $command
            ValidateResponse $result

            $result.Output.Headers.'X-Fake-Header'.Count | Should -Be 2
            $result.Output.Headers.'X-Fake-Header'.Contains('testvalue01') | Should -BeTrue
            $result.Output.Headers.'X-Fake-Header'.Contains('testvalue02') | Should -BeTrue
            $result.Output.RawContent | Should -Match ([regex]::Escape('X-Fake-Header: testvalue01'))
            $result.Output.RawContent | Should -Match ([regex]::Escape('X-Fake-Header: testvalue02'))
        }

        It "Verifies Invoke-WebRequest does not sent expect 100-continue headers by default" {
            $uri = Get-WebListenerUrl -Test 'Get'

            $response = Invoke-WebRequest -Uri $uri
            $result = $response.Content | ConvertFrom-Json

            $result.headers.Expect | Should -BeNullOrEmpty
            $result.method | Should -BeExactly "GET"
            $result.url | Should -BeExactly $uri.ToString()
        }

        It "Verifies Invoke-WebRequest sends expect 100-continue header when defined in -Headers" {
            $uri = Get-WebListenerUrl -Test 'Get'

            $response = Invoke-WebRequest -Uri $uri -Headers @{Expect = '100-continue'}
            $result = $response.Content | ConvertFrom-Json

            $result.headers.Expect | Should -BeExactly '100-continue'
            $result.method | Should -BeExactly "GET"
            $result.url | Should -BeExactly $uri.ToString()
        }
    }

    #endregion Content Header Inclusion

    Context "HTTPS Tests" {
        It "Validate Invoke-WebRequest -SkipCertificateCheck" {
            # validate that exception is thrown for URI with expired certificate
            $Uri = Get-WebListenerUrl -Https
            $command = "Invoke-WebRequest -Uri '$Uri'"
            $result = ExecuteWebCommand -command $command
            $result.Error.FullyQualifiedErrorId | Should -Be "WebCmdletWebResponseException,Microsoft.PowerShell.Commands.InvokeWebRequestCommand"

            # validate that no exception is thrown for URI with expired certificate when using -SkipCertificateCheck option
            $Uri = Get-WebListenerUrl -Https
            $command = "Invoke-WebRequest -Uri '$Uri' -SkipCertificateCheck"
            $result = ExecuteWebCommand -command $command
            $result.Error | Should -BeNullOrEmpty
        }

        It "Validate Invoke-WebRequest returns native HTTPS error message in exception" {
            $uri = Get-WebListenerUrl -Https
            $command = "Invoke-WebRequest -Uri '$uri'"
            $result = ExecuteWebCommand -command $command

            # need to check against inner exception since Linux and Windows uses different HTTP client libraries so errors aren't the same
            $result.Error.ErrorDetails.Message | Should -Match $result.Error.Exception.InnerException.Message
            $result.Error.FullyQualifiedErrorId | Should -Be "WebCmdletWebResponseException,Microsoft.PowerShell.Commands.InvokeWebRequestCommand"
        }

        It "Verifies Invoke-WebRequest Certificate Authentication Fails without -Certificate" {
            $uri = Get-WebListenerUrl -Https -Test 'Cert'
            $result = Invoke-WebRequest -Uri $uri -SkipCertificateCheck |
                Select-Object -ExpandProperty Content |
                ConvertFrom-Json

            $result.Status | Should -Be 'FAILED'
        }

        It "Verifies Invoke-WebRequest Certificate Authentication Successful with -Certificate" {
            $uri = Get-WebListenerUrl -Https -Test 'Cert'
            $certificate = Get-WebListenerClientCertificate
            $result = Invoke-WebRequest -Uri $uri -Certificate $certificate -SkipCertificateCheck |
                Select-Object -ExpandProperty Content |
                ConvertFrom-Json

            $result.Status | Should -Be 'OK'
            $result.Thumbprint | Should -Be $certificate.Thumbprint
        }
    }

    Context "Multipart/form-data Tests" {
        <#
            Content-Type request headers for multipart/form-data appear as:
                multipart/form-data; boundary="0ab0cb90-f01b-4c15-bd4d-53d073efcc1d"
            MultipartFormDataContent sets a random GUID for the boundary before submitting the request
            to the remote endpoint. Tests in this context for Content-Type match 'multipart/form-data'
            as we do not have access to the random GUID.
        #>
        <#
            Kestrel/ASP.NET inconsistently renders the new line for uploaded text files.
            File content tests in this context use match as a workaround.
        #>
        BeforeAll {
            $file1Name = "testfile1.txt"
            $file1Path = Join-Path $testdrive $file1Name
            $file1Contents = "Test123"
            $file1Contents | Set-Content $file1Path -Force

            $file2Name = "testfile2.txt"
            $file2Path = Join-Path $testdrive $file2Name
            $file2Contents = "Test456"
            $file2Contents | Set-Content $file2Path -Force
        }

        It "Verifies Invoke-WebRequest Supports Multipart String Values" {
            $body = GetMultipartBody -String
            $uri = Get-WebListenerUrl -Test 'Multipart'
            $response = Invoke-WebRequest -Uri $uri -Body $body -Method 'POST'
            $result = $response.Content | ConvertFrom-Json

            $result.Headers.'Content-Type' | Should -Match 'multipart/form-data'
            $result.Items.TestString[0] | Should -Be 'TestValue'
        }

        It "Verifies Invoke-WebRequest Supports Multipart File Values" {
            $body = GetMultipartBody -File
            $uri = Get-WebListenerUrl -Test 'Multipart'
            $response = Invoke-WebRequest -Uri $uri -Body $body -Method 'POST'
            $result = $response.Content | ConvertFrom-Json

            $result.Headers.'Content-Type' | Should -Match 'multipart/form-data'
            $result.Files[0].FileName | Should -Be 'multipart.txt'
            $result.Files[0].ContentType | Should -Be 'text/plain'
            $result.Files[0].Content | Should -Match 'TestContent'
        }

        It "Verifies Invoke-WebRequest Supports Mixed Multipart String and File Values" {
            $body = GetMultipartBody -String -File
            $uri = Get-WebListenerUrl -Test 'Multipart'
            $response = Invoke-WebRequest -Uri $uri -Body $body -Method 'POST'
            $result = $response.Content | ConvertFrom-Json

            $result.Headers.'Content-Type' | Should -Match 'multipart/form-data'
            $result.Items.TestString[0] | Should -Be 'TestValue'
            $result.Files[0].FileName | Should -Be 'multipart.txt'
            $result.Files[0].ContentType | Should -Be 'text/plain'
            $result.Files[0].Content | Should -Match 'TestContent'
        }

        It "Verifies Invoke-WebRequest -Form supports string values" {
            $form = @{TestString = "TestValue"}
            $uri = Get-WebListenerUrl -Test 'Multipart'
            $response = Invoke-WebRequest -Uri $uri -Form $form -Method 'POST'
            $result = $response.Content | ConvertFrom-Json

            $result.Headers.'Content-Type' | Should -Match 'multipart/form-data'
            $result.Items.TestString.Count | Should -Be 1
            $result.Items.TestString[0] | Should -BeExactly 'TestValue'
        }

        It "Verifies Invoke-WebRequest -Form supports a collection of string values" {
            $form = @{TestStrings = "TestValue", "TestValue2"}
            $uri = Get-WebListenerUrl -Test 'Multipart'
            $response = Invoke-WebRequest -Uri $uri -Form $form -Method 'POST'
            $result = $response.Content | ConvertFrom-Json

            $result.Headers.'Content-Type' | Should -Match 'multipart/form-data'
            $result.Items.TestStrings.Count | Should -Be 2
            $result.Items.TestStrings[0] | Should -BeExactly 'TestValue'
            $result.Items.TestStrings[1] | Should -BeExactly 'TestValue2'
        }

        It "Verifies Invoke-WebRequest -Form supports file values" {
            $form = @{TestFile = [System.IO.FileInfo]$file1Path}
            $uri = Get-WebListenerUrl -Test 'Multipart'
            $response = Invoke-WebRequest -Uri $uri -Form $form -Method 'POST'
            $result = $response.Content | ConvertFrom-Json

            $result.Headers.'Content-Type' | Should -Match 'multipart/form-data'
            $result.Files.Count | Should -Be 1

            $result.Files[0].Name | Should -BeExactly "TestFile"
            $result.Files[0].FileName | Should -BeExactly $file1Name
            $result.Files[0].ContentType | Should -BeExactly 'application/octet-stream'
            $result.Files[0].Content | Should -Match $file1Contents
        }

        It "Verifies Invoke-WebRequest -Form supports a collection of file values" {
            $form = @{TestFiles = [System.IO.FileInfo]$file1Path, [System.IO.FileInfo]$file2Path}
            $uri = Get-WebListenerUrl -Test 'Multipart'
            $response = Invoke-WebRequest -Uri $uri -Form $form -Method 'POST'
            $result = $response.Content | ConvertFrom-Json

            $result.Headers.'Content-Type' | Should -Match 'multipart/form-data'
            $result.Files.Count | Should -Be 2

            $result.Files[0].Name | Should -BeExactly "TestFiles"
            $result.Files[0].FileName | Should -BeExactly $file1Name
            $result.Files[0].ContentType | Should -BeExactly 'application/octet-stream'
            $result.Files[0].Content | Should -Match $file1Contents

            $result.Files[1].Name | Should -BeExactly "TestFiles"
            $result.Files[1].FileName | Should -BeExactly $file2Name
            $result.Files[1].ContentType | Should -BeExactly 'application/octet-stream'
            $result.Files[1].Content | Should -Match $file2Contents
        }

        It "Verifies Invoke-WebRequest -Form supports combinations of strings and files" {
            $form = @{
                TestStrings = "TestValue", "TestValue2"
                TestFiles   = [System.IO.FileInfo]$file1Path, [System.IO.FileInfo]$file2Path
            }
            $uri = Get-WebListenerUrl -Test 'Multipart'
            $response = Invoke-WebRequest -Uri $uri -Form $form -Method 'POST'
            $result = $response.Content | ConvertFrom-Json

            $result.Headers.'Content-Type' | Should -Match 'multipart/form-data'
            $result.Items.TestStrings.Count | Should -Be 2
            $result.Files.Count | Should -Be 2

            $result.Items.TestStrings[0] | Should -BeExactly 'TestValue'
            $result.Items.TestStrings[1] | Should -BeExactly 'TestValue2'

            $result.Files[0].Name | Should -Be "TestFiles"
            $result.Files[0].FileName | Should -BeExactly $file1Name
            $result.Files[0].ContentType | Should -BeExactly 'application/octet-stream'
            $result.Files[0].Content | Should -Match $file1Contents

            $result.Files[1].Name | Should -BeExactly "TestFiles"
            $result.Files[1].FileName | Should -BeExactly $file2Name
            $result.Files[1].ContentType | Should -BeExactly 'application/octet-stream'
            $result.Files[1].Content | Should -Match $file2Contents
        }

        It "Verifies Invoke-WebRequest -Form is mutually exclusive with -Body" {
            $form = @{TestString = "TestValue"}
            $body = "test"
            $uri = Get-WebListenerUrl -Test 'Multipart'

            {Invoke-WebRequest -Uri $uri -Form $form -Body $Body -ErrorAction 'Stop'} |
                Should -Throw -ErrorId 'WebCmdletBodyFormConflictException,Microsoft.PowerShell.Commands.InvokeWebRequestCommand'
        }

        It "Verifies Invoke-WebRequest -Form is mutually exclusive with -InFile" {
            $form = @{TestString = "TestValue"}
            $uri = Get-WebListenerUrl -Test 'Multipart'

            {Invoke-WebRequest -Uri $uri -Form $form -InFile $file1Path -ErrorAction 'Stop'} |
                Should -Throw -ErrorId 'WebCmdletFormInFileConflictException,Microsoft.PowerShell.Commands.InvokeWebRequestCommand'
        }
    }

    Context "Invoke-WebRequest -Authentication tests" {
        BeforeAll {
            #[SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="Demo/doc/test secret.")]
            $token = "testpassword" | ConvertTo-SecureString -AsPlainText -Force
            $credential = [pscredential]::new("testuser", $token)
            $httpUri = Get-WebListenerUrl -Test 'Get'
            $httpsUri = Get-WebListenerUrl -Test 'Get' -Https
            $httpBasicUri = Get-WebListenerUrl -Test 'Auth' -TestValue 'Basic'
            $httpsBasicUri = Get-WebListenerUrl -Test 'Auth' -TestValue 'Basic' -Https
            $testCases = @(
                @{Authentication = "bearer"}
                @{Authentication = "OAuth"}
            )
        }

        It "Verifies Invoke-WebRequest -Authentication Basic" {
            $params = @{
                Uri                  = $httpsUri
                Authentication       = "Basic"
                Credential           = $credential
                SkipCertificateCheck = $true
            }
            $Response = Invoke-WebRequest @params
            $result = $response.Content | ConvertFrom-Json

            $result.Headers.Authorization | Should -BeExactly "Basic dGVzdHVzZXI6dGVzdHBhc3N3b3Jk"
        }

        It "Verifies Invoke-WebRequest -Authentication Basic with null username" {
            $credential = [pscredential]::new([PSCustomObject]@{UserName = $null;Password=$token.psobject.BaseObject})
            $params = @{
                Uri                  = $httpsUri
                Authentication       = "Basic"
                Credential           = $credential
                SkipCertificateCheck = $true
            }
            $Response = Invoke-WebRequest @params
            $result = $response.Content | ConvertFrom-Json

            $result.Headers.Authorization | Should -BeExactly "Basic OnRlc3RwYXNzd29yZA=="
        }

        It "Verifies Invoke-WebRequest -Authentication <Authentication>" -TestCases $testCases {
            param($Authentication)
            $params = @{
                Uri                  = $httpsUri
                Authentication       = $Authentication
                Token                = $token
                SkipCertificateCheck = $true
            }
            $Response = Invoke-WebRequest @params
            $result = $response.Content | ConvertFrom-Json

            $result.Headers.Authorization | Should -BeExactly "Bearer testpassword"
        }

        It "Verifies Invoke-WebRequest -Authentication does not support -UseDefaultCredentials" {
            $params = @{
                Uri                   = $httpsUri
                Token                 = $token
                Authentication        = "OAuth"
                UseDefaultCredentials = $true
                ErrorAction           = 'Stop'
                SkipCertificateCheck  = $true
            }
            { Invoke-WebRequest @params } | Should -Throw -ErrorId "WebCmdletAuthenticationConflictException,Microsoft.PowerShell.Commands.InvokeWebRequestCommand"
        }

        It "Verifies Invoke-WebRequest -Authentication does not support Both -Credential and -Token" {
            $params = @{
                Uri                  = $httpsUri
                Token                = $token
                Credential           = $credential
                Authentication       = "OAuth"
                ErrorAction          = 'Stop'
                SkipCertificateCheck = $true
            }
            { Invoke-WebRequest @params } | Should -Throw -ErrorId "WebCmdletAuthenticationTokenConflictException,Microsoft.PowerShell.Commands.InvokeWebRequestCommand"
        }

        It "Verifies Invoke-WebRequest -Authentication <Authentication> requires -Token" -TestCases $testCases {
            param($Authentication)
            $params = @{
                Uri                  = $httpsUri
                Authentication       = $Authentication
                ErrorAction          = 'Stop'
                SkipCertificateCheck = $true
            }
            { Invoke-WebRequest @params } | Should -Throw -ErrorId "WebCmdletAuthenticationTokenNotSuppliedException,Microsoft.PowerShell.Commands.InvokeWebRequestCommand"
        }

        It "Verifies Invoke-WebRequest -Authentication Basic requires -Credential" {
            $params = @{
                Uri                  = $httpsUri
                Authentication       = "Basic"
                ErrorAction          = 'Stop'
                SkipCertificateCheck = $true
            }
            { Invoke-WebRequest @params } | Should -Throw -ErrorId "WebCmdletAuthenticationCredentialNotSuppliedException,Microsoft.PowerShell.Commands.InvokeWebRequestCommand"
        }

        It "Verifies Invoke-WebRequest -Authentication Requires HTTPS" {
            $params = @{
                Uri            = $httpUri
                Token          = $token
                Authentication = "OAuth"
                ErrorAction    = 'Stop'
            }
            { Invoke-WebRequest @params } | Should -Throw -ErrorId "WebCmdletAllowUnencryptedAuthenticationRequiredException,Microsoft.PowerShell.Commands.InvokeWebRequestCommand"
        }

        It "Verifies Invoke-WebRequest -Authentication Can use HTTP with -AllowUnencryptedAuthentication" {
            $params = @{
                Uri                            = $httpUri
                Token                          = $token
                Authentication                 = "OAuth"
                AllowUnencryptedAuthentication = $true
            }
            $Response = Invoke-WebRequest @params
            $result = $response.Content | ConvertFrom-Json

            $result.Headers.Authorization | Should -BeExactly "Bearer testpassword"
        }

        It "Verifies Invoke-WebRequest Negotiated -Credential over HTTPS" {
            $params = @{
                Uri                  = $httpsBasicUri
                Credential           = $credential
                SkipCertificateCheck = $true
            }
            $Response = Invoke-WebRequest @params
            $result = $response.Content | ConvertFrom-Json

            $result.Headers.Authorization | Should -BeExactly "Basic dGVzdHVzZXI6dGVzdHBhc3N3b3Jk"
        }

        It "Verifies Invoke-WebRequest Negotiated -Credential Requires HTTPS" {
            $params = @{
                Uri         = $httpBasicUri
                Credential  = $credential
                ErrorAction = 'Stop'
            }
            { Invoke-WebRequest @params } | Should -Throw -ErrorId "WebCmdletAllowUnencryptedAuthenticationRequiredException,Microsoft.PowerShell.Commands.InvokeWebRequestCommand"
        }

        It "Verifies Invoke-WebRequest Negotiated -Credential Can use HTTP with -AllowUnencryptedAuthentication" {
            $params = @{
                Uri                            = $httpBasicUri
                Credential                     = $credential
                AllowUnencryptedAuthentication = $true
            }
            $Response = Invoke-WebRequest @params
            $result = $response.Content | ConvertFrom-Json

            $result.Headers.Authorization | Should -BeExactly "Basic dGVzdHVzZXI6dGVzdHBhc3N3b3Jk"
        }

        # UseDefaultCredentials is only reliably testable on Windows
        It "Verifies Invoke-WebRequest Negotiated -UseDefaultCredentials with '<AuthType>' over HTTPS" -Skip:$(!$IsWindows) -TestCases @(
            @{AuthType = 'NTLM'}
            @{AuthType = 'Negotiate'}
        ) {
            param($AuthType)
            $params = @{
                Uri                   = Get-WebListenerUrl -Test 'Auth' -TestValue $AuthType -Https
                UseDefaultCredentials = $true
                SkipCertificateCheck  = $true
            }
            $Response = Invoke-WebRequest @params
            $result = $response.Content | ConvertFrom-Json

            $result.Headers.Authorization | Should -Match "^$AuthType "
        }

        # The error condition can at least be tested on all platforms.
        It "Verifies Invoke-WebRequest Negotiated -UseDefaultCredentials Requires HTTPS" {
            $params = @{
                Uri                   = $httpUri
                UseDefaultCredentials = $true
                ErrorAction           = 'Stop'
            }
            { Invoke-WebRequest @params } | Should -Throw -ErrorId "WebCmdletAllowUnencryptedAuthenticationRequiredException,Microsoft.PowerShell.Commands.InvokeWebRequestCommand"
        }

        # UseDefaultCredentials is only reliably testable on Windows
        It "Verifies Invoke-WebRequest Negotiated -UseDefaultCredentials with '<AuthType>' Can use HTTP with -AllowUnencryptedAuthentication" -Skip:$(!$IsWindows) -TestCases @(
            @{AuthType = 'NTLM'}
            @{AuthType = 'Negotiate'}
        ) {
            param($AuthType)
            $params = @{
                Uri                            = Get-WebListenerUrl -Test 'Auth' -TestValue $AuthType
                UseDefaultCredentials          = $true
                AllowUnencryptedAuthentication = $true
            }
            $Response = Invoke-WebRequest @params
            $result = $response.Content | ConvertFrom-Json

            $result.Headers.Authorization | Should -Match "^$AuthType "
        }
    }

    Context "Invoke-WebRequest -SslProtocol Test" {
        BeforeAll {
            # We put Tls13 tests at pending due to modern OS limitations.
            # Tracking issue https://github.com/PowerShell/PowerShell/issues/13439

            $skipForTls1 = $true

            ## Test cases for the 1st 'It'
            $testCases1 = @(
                @{ Test = @{SslProtocol = 'Default'; ActualProtocol = 'Default'}; Pending = $false }
                @{ Test = @{SslProtocol = 'Tls'; ActualProtocol = 'Tls'}; Pending = $skipForTls1 }
                @{ Test = @{SslProtocol = 'Tls11'; ActualProtocol = 'Tls11'}; Pending = $skipForTls1 }
                @{ Test = @{SslProtocol = 'Tls12'; ActualProtocol = 'Tls12'}; Pending = $false }
                @{ Test = @{SslProtocol = 'Tls13'; ActualProtocol = 'Tls13'}; Pending = $true }
                @{ Test = @{SslProtocol = 'Tls, Tls11, Tls12'; ActualProtocol = 'Tls12'}; Pending = $false }
                @{ Test = @{SslProtocol = 'Tls, Tls11, Tls12, Tls13'; ActualProtocol = 'Tls13'}; Pending = $true }
                @{ Test = @{SslProtocol = 'Tls11, Tls12'; ActualProtocol = 'Tls12'}; Pending = $false }
                @{ Test = @{SslProtocol = 'Tls, Tls11, Tls12'; ActualProtocol = 'Tls11'}; Pending = $skipForTls1 }
                @{ Test = @{SslProtocol = 'Tls, Tls11, Tls12, Tls13'; ActualProtocol = 'Tls11'}; Pending = $skipForTls1 }
                @{ Test = @{SslProtocol = 'Tls11, Tls12'; ActualProtocol = 'Tls11'}; Pending = $skipForTls1 }
                @{ Test = @{SslProtocol = 'Tls, Tls11'; ActualProtocol = 'Tls11'}; Pending = $skipForTls1 }
                @{ Test = @{SslProtocol = 'Tls, Tls11, Tls12'; ActualProtocol = 'Tls'}; Pending = $skipForTls1 }
                @{ Test = @{SslProtocol = 'Tls, Tls11, Tls13'; ActualProtocol = 'Tls'}; Pending = $true }
                @{ Test = @{SslProtocol = 'Tls, Tls11'; ActualProtocol = 'Tls'}; Pending = $skipForTls1 }
                # Skipping intermediary protocols is not supported on all platforms
                # Removed this as Tls now default to Tls12
                # @{ Test = @{SslProtocol = 'Tls, Tls12'; ActualProtocol = 'Tls'}; Pending = -not $IsWindows }
                @{ Test = @{SslProtocol = 'Tls, Tls12'; ActualProtocol = 'Tls12'}; Pending = -not $IsWindows }
            )

            $testCases2 = @(
                @{ Test = @{IntendedProtocol = 'Tls'; ActualProtocol = 'Tls13'}; Pending = $true }
                @{ Test = @{IntendedProtocol = 'Tls11'; ActualProtocol = 'Tls13'}; Pending = $true }
                @{ Test = @{IntendedProtocol = 'Tls13'; ActualProtocol = 'Tls'}; Pending = $true }
                @{ Test = @{IntendedProtocol = 'Tls11, Tls12, Tls13'; ActualProtocol = 'Tls'}; Pending = $true }
                @{ Test = @{IntendedProtocol = 'Tls, Tls12'; ActualProtocol = 'Tls13'}; Pending = $true }
                @{ Test = @{IntendedProtocol = 'Tls, Tls11'; ActualProtocol = 'Tls13'}; Pending = $true }
            )
        }

        foreach ($entry in $testCases1) {
            It "Verifies Invoke-WebRequest -SslProtocol <SslProtocol> works on <ActualProtocol>" -TestCases ($entry.Test) -Pending:($entry.Pending) {
                param($SslProtocol, $ActualProtocol)
                $params = @{
                    Uri                  = Get-WebListenerUrl -Test 'Get' -Https -SslProtocol $ActualProtocol
                    SslProtocol          = $SslProtocol
                    SkipCertificateCheck = $true
                }
                $response = Invoke-WebRequest @params
                $result = $Response.Content | ConvertFrom-Json

                $result.headers.Host | Should -Be $params.Uri.Authority
            }
        }

        foreach ($entry in $testCases2) {
            It "Verifies Invoke-WebRequest -SslProtocol -SslProtocol <IntendedProtocol> fails on a <ActualProtocol> only connection" -TestCases ($entry.Test) -Pending:($entry.Pending) {
                param( $IntendedProtocol, $ActualProtocol)
                $params = @{
                    Uri                  = Get-WebListenerUrl -Test 'Get' -Https -SslProtocol $ActualProtocol
                    SslProtocol          = $IntendedProtocol
                    SkipCertificateCheck = $true
                    ErrorAction          = 'Stop'
                }
                { Invoke-WebRequest @params } | Should -Throw -ErrorId 'WebCmdletWebResponseException,Microsoft.PowerShell.Commands.InvokeWebRequestCommand'
            }
        }
    }

    Context "Invoke-WebRequest File Resume Feature" {
        BeforeAll {
            $outFile = Join-Path $TestDrive "resume.txt"

            # Download the entire file to reference in tests
            $referenceFile = Join-Path $TestDrive "reference.txt"
            $resumeUri = Get-WebListenerUrl -Test 'Resume'
            Invoke-WebRequest -Uri $resumeUri -OutFile $referenceFile -ErrorAction Stop
            $referenceFileHash = Get-FileHash -Algorithm SHA256 -Path $referenceFile
            $referenceFileSize = Get-Item $referenceFile | Select-Object -ExpandProperty Length
        }

        AfterEach {
            Remove-Item -Force -ErrorAction 'SilentlyContinue' -Path $outFile
        }

        It "Invoke-WebRequest -Resume requires -OutFile" {
            { Invoke-WebRequest -Resume -Uri $resumeUri -ErrorAction Stop } |
                Should -Throw -ErrorId 'WebCmdletOutFileMissingException,Microsoft.PowerShell.Commands.InvokeWebRequestCommand'
        }

        It "Invoke-WebRequest -Resume should fail if -OutFile folder" {
            { Invoke-WebRequest -Resume -Uri $resumeUri -OutFile $TestDrive -ErrorAction Stop } |
                Should -Throw -ErrorId 'WebCmdletResumeNotFilePathException,Microsoft.PowerShell.Commands.InvokeWebRequestCommand'
        }

        It "Invoke-WebRequest -Resume Downloads the whole file when the file does not exist" {
            $response = Invoke-WebRequest -Uri $resumeUri -OutFile $outFile -Resume -PassThru

            $outFileHash = Get-FileHash -Algorithm SHA256 -Path $outFile
            $outFileHash.Hash | Should -BeExactly $referenceFileHash.Hash
            Get-Item $outFile | Select-Object -ExpandProperty Length | Should -Be $referenceFileSize
            $response.Headers.'X-WebListener-Has-Range'[0] | Should -BeExactly 'true'
            $response.Headers.'X-WebListener-Request-Range'[0] | Should -BeExactly 'bytes=0-'
            $response.StatusCode | Should -Be 206
            $response.Headers.'Content-Range'[0] | Should -BeExactly "bytes 0-$($referenceFileSize-1)/$referenceFileSize"
        }

        It "Invoke-WebRequest -Resume overwrites an existing file that is larger than the remote file" {
            # Create a file larger than the download file
            $largerFileSize = $referenceFileSize + 20
            1..$largerFileSize | ForEach-Object { [Byte]$_ } | Set-Content -AsByteStream $outFile
            $largerFileSize = Get-Item $outFile | Select-Object -ExpandProperty Length

            $response = Invoke-WebRequest -Uri $resumeUri -OutFile $outFile -Resume -PassThru

            $outFileHash = Get-FileHash -Algorithm SHA256 -Path $outFile
            $outFileHash.Hash | Should -BeExactly $referenceFileHash.Hash
            Get-Item $outFile | Select-Object -ExpandProperty Length | Should -Be $referenceFileSize
            Get-Item $outFile | Select-Object -ExpandProperty Length | Should -BeLessThan $largerFileSize
            $response.Headers.'X-WebListener-Has-Range'[0] | Should -BeExactly 'false'
            $response.StatusCode | Should -Be 200
            $response.Headers.ContainsKey('Content-Range') | Should -BeFalse
        }

        It "Invoke-WebRequest -Resume overwrites existing file when remote server does not support resume" {
            # Create a file larger than the download file
            $largerFileSize = $referenceFileSize + 20
            1..$largerFileSize | ForEach-Object { [Byte]$_ } | Set-Content -AsByteStream $outFile
            $largerFileSize = Get-Item $outFile | Select-Object -ExpandProperty Length

            $uri = Get-WebListenerUrl -Test 'Resume' -TestValue 'NoResume'
            $response = Invoke-WebRequest -Uri $uri -OutFile $outFile -Resume -PassThru

            $outFileHash = Get-FileHash -Algorithm SHA256 -Path $outFile
            $outFileHash.Hash | Should -BeExactly $referenceFileHash.Hash
            Get-Item $outFile | Select-Object -ExpandProperty Length | Should -Be $referenceFileSize
            $response.Headers.'X-WebListener-Has-Range'[0] | Should -BeExactly 'true'
            $response.Headers.'X-WebListener-Request-Range'[0] | Should -BeExactly "bytes=$largerFileSize-"
            $response.StatusCode | Should -Be 200
            $response.Headers.ContainsKey('Content-Range') | Should -BeFalse
        }

        It "Invoke-WebRequest -Resume resumes downloading from <bytes> bytes" -TestCases @(
            @{bytes = 4}
            @{bytes = 8}
            @{bytes = 12}
            @{bytes = 16}
        ) {
            param($bytes, $statuscode)
            # Simulate partial download
            $uri = Get-WebListenerUrl -Test 'Resume' -TestValue "Bytes/$bytes"
            $null = Invoke-WebRequest -Uri $uri -OutFile $outFile
            Get-Item $outFile | Select-Object -ExpandProperty Length | Should -Be $bytes

            $response = Invoke-WebRequest -Uri $resumeUri -OutFile $outFile -Resume -PassThru

            $outFileHash = Get-FileHash -Algorithm SHA256 -Path $outFile
            $outFileHash.Hash | Should -BeExactly $referenceFileHash.Hash
            Get-Item $outFile | Select-Object -ExpandProperty Length | Should -Be $referenceFileSize
            $response.Headers.'X-WebListener-Has-Range'[0] | Should -BeExactly 'true'
            $response.Headers.'X-WebListener-Request-Range'[0] | Should -BeExactly "bytes=$bytes-"
            $response.StatusCode | Should -Be 206
            $response.Headers.'Content-Range'[0] | Should -BeExactly "bytes $bytes-$($referenceFileSize-1)/$referenceFileSize"
        }

        It "Invoke-WebRequest -Resume assumes the file was successfully completed when the local and remote file are the same size." {
            # Download the entire file
            $uri = Get-WebListenerUrl -Test 'Resume' -TestValue 'NoResume'
            $null = Invoke-WebRequest -Uri $uri -OutFile $outFile
            $fileSize = Get-Item $outFile | Select-Object -ExpandProperty Length

            $response = Invoke-WebRequest -Uri $resumeUri -OutFile $outFile -Resume -PassThru

            $outFileHash = Get-FileHash -Algorithm SHA256 -Path $outFile
            $outFileHash.Hash | Should -BeExactly $referenceFileHash.Hash
            Get-Item $outFile | Select-Object -ExpandProperty Length | Should -Be $referenceFileSize
            $response.Headers.'X-WebListener-Has-Range'[0] | Should -BeExactly 'true'
            $response.Headers.'X-WebListener-Request-Range'[0] | Should -BeExactly "bytes=$fileSize-"
            # The web cmdlets special case 416 as a success code when the local file and remote file are the same size
            $response.StatusCode | Should -Be 416
            $response.Headers.'Content-Range'[0] | Should -BeExactly "bytes */$referenceFileSize"
        }
    }

    Context "Invoke-WebRequest retry tests" {

        It "Invoke-WebRequest can retry - <Name>" -TestCases @(
            @{Name = "specified number of times - error 304"; failureCount = 2; failureCode = 304; retryCount = 2}
            @{Name = "specified number of times - error 400"; failureCount = 3; failureCode = 400; retryCount = 3}
            @{Name = "specified number of times - error 599"; failureCount = 1; failureCode = 599; retryCount = 2}
            @{Name = "specified number of times - error 404"; failureCount = 2; failureCode = 404; retryCount = 2}
            @{Name = "when retry count is higher than failure count"; failureCount = 2; failureCode = 404; retryCount = 4}
        ) {
            param($failureCount, $retryCount, $failureCode)

            $uri = Get-WebListenerUrl -Test 'Retry' -Query @{ sessionid = (New-Guid).Guid; failureCode = $failureCode; failureCount = $failureCount }
            $commandStr = "Invoke-WebRequest -Uri '$uri' -MaximumRetryCount $retryCount -RetryIntervalSec 1"
            $result = ExecuteWebCommand -command $commandStr

            $result.output.StatusCode | Should -Be "200"
            $jsonResult = $result.output.Content | ConvertFrom-Json
            $jsonResult.failureResponsesSent | Should -Be $failureCount
        }

        It "Invoke-WebRequest should fail when failureCount is greater than MaximumRetryCount" {

            $uri = Get-WebListenerUrl -Test 'Retry' -Query @{ sessionid = (New-Guid).Guid; failureCode = 400; failureCount = 4 }
            $command = "Invoke-WebRequest -Uri '$uri' -MaximumRetryCount 1 -RetryIntervalSec 1"
            $result = ExecuteWebCommand -command $command
            $jsonError = $result.error | ConvertFrom-Json
            $jsonError.error | Should -BeExactly 'Error: HTTP - 400 occurred.'
        }

        It "Invoke-WebRequest can retry with POST" {

            $uri = Get-WebListenerUrl -Test 'Retry'
            $sessionId = (New-Guid).Guid
            $body = @{ sessionid = $sessionId; failureCode = 404; failureCount = 1 }
            $commandStr = "Invoke-WebRequest -Uri '$uri' -MaximumRetryCount 2 -RetryIntervalSec 1 -Method POST -Body `$body"
            $result = ExecuteWebCommand -command $commandStr

            $result.output.StatusCode | Should -Be "200"
            $jsonResult = $result.output.Content | ConvertFrom-Json
            $jsonResult.SessionId | Should -BeExactly $sessionId
        }
    }

    Context "Regex Parsing" {

        It 'correctly parses an image with id, class, and src attributes' {
            $dosUri = Get-WebListenerUrl -Test 'Dos' -query @{
                dosType = 'img-attribute'
            }

            $response = Invoke-WebRequest -Uri $dosUri
            $response.Images | Should -Not -BeNullOrEmpty
        }

        $singleInputExpected = @(
            @{ Name = 'foo'; Value = 'bar' }
        )

        It 'correctly parses input tag(s) for `<markup>`' -TestCases @(
            @{
                Markup = "<input name='foo' value='bar'>";
                ExpectedFields = $singleInputExpected
            },
            @{
                Markup = "<input name='foo' value='bar'/>";
                ExpectedFields = $singleInputExpected
            },
            @{
                Markup = "<input name='foo' value='bar'>baz</input>";
                ExpectedFields = $singleInputExpected
            }
            @{
                Markup = "<input name='item1' value='bar'><input name='item2' value='foo'><input name='item3'></input><input name='item4' value='fu'><input name='item5' value='bahr'/>";
                ExpectedFields = @(
                    @{ Name = 'item1'; Value = 'bar'},
                    @{ Name = 'item2'; Value = 'foo' },
                    @{ Name = 'item3'; Value = $null },
                    @{ Name = 'item4'; Value = 'fu' },
                    @{ Name = 'item5'; Value = 'bahr' }
                )
            }
        ) {
            param($markup, $expectedFields)
            $query = @{
                contenttype = 'text/html'
                body        = "<html><body>${markup}</body></html>"
            }
            $uri = Get-WebListenerUrl -Test 'Response' -Query $query
            $response = Invoke-WebRequest -Uri $uri -UseBasicParsing
            $response.Error | Should -BeNullOrEmpty
            ForEach ($expectedField in $expectedFields) {
                $actualField = $response.InputFields.FindByName($expectedField.Name)
                $actualField.Value | Should -Be $expectedField.Value
            }
        }
    }

    Context "Denial of service" -Tag 'DOS' {
        It "Image Parsing" {
            Set-ItResult -Pending -Because "The pathological regex runs fast due to https://github.com/dotnet/runtime/issues/33399.  Fixed in .NET 5 preview.2"
            $dosUri = Get-WebListenerUrl -Test 'Dos' -query @{
                dosType='img'
                dosLength='5000'
            }
            $script:content = ''
            [TimeSpan] $timeSpan = Measure-Command {
                $response = Invoke-WebRequest -Uri $dosUri
                $script:content = $response.content
                $response.Images | Out-Null
            }

            $script:content | Should -Not -BeNullOrEmpty

            # pathological regex
            $regex = [RegEx]::new('<img\s+[^>]*>')

            [TimeSpan] $pathologicalTimeSpan = Measure-Command {
                $regex.Match($content)
            }

            $pathologicalRatio = $pathologicalTimeSpan.TotalMilliseconds/$timeSpan.TotalMilliseconds
            Write-Verbose "Pathological ratio: $pathologicalRatio" -Verbose

            # dosLength 4,000 on my 3.5 GHz 6-Core Intel Xeon E5 macpro produced a ratio of 12
            # dosLength 5,000 on my 3.5 GHz 6-Core Intel Xeon E5 macpro produced a ratio of 21
            # dosLength 10,000 on my 3.5 GHz 6-Core Intel Xeon E5 macpro produced a ratio of 75
            # in some cases we will be running in a Docker container with modest resources
            $pathologicalRatio | Should -BeGreaterThan 5
        }

        It "Charset Parsing" {
            Set-ItResult -Pending -Because "We need a better way to test this and .NET has made bad regex's run faster"
            $dosUri = Get-WebListenerUrl -Test 'Dos' -query @{
                dosType='charset'
                dosLength='2850'
            }
            $script:content = ''
            [TimeSpan] $timeSpan = Measure-Command {
                $response = Invoke-WebRequest -Uri $dosUri
                $script:content = $response.content
            }

            # Pathological regex
            $regex = [RegEx]::new('<meta\s[.\n]*[^><]*charset\s*=\s*["''\n]?(?<charset>[A-Za-z].[^\s"''\n<>]*)[\s"''\n>]')

            $script:content | Should -Not -BeNullOrEmpty

            [TimeSpan] $pathologicalTimeSpan = Measure-Command {
                $regex.Match($content)
            }

            $pathologicalRatio = $pathologicalTimeSpan.TotalMilliseconds/$timeSpan.TotalMilliseconds
            Write-Verbose "Pathological ratio: $pathologicalRatio" -Verbose

            # dosLength 2,750 on my 3.5 GHz 6-Core Intel Xeon E5 macpro produced a ratio of 13
            # dosLength 2,850 on my 3.5 GHz 6-Core Intel Xeon E5 macpro produced a ratio of 22
            # dosLength 3,000 on my 3.5 GHz 6-Core Intel Xeon E5 macpro produced a ratio of 31
            # in some cases we will be running in a Docker container with modest resources
            $pathologicalRatio | Should -BeGreaterThan 5
        }
    }
}

Describe "Invoke-RestMethod tests" -Tags "Feature", "RequireAdminOnWindows" {
    BeforeAll {
        $oldProgress = $ProgressPreference
        $ProgressPreference = 'SilentlyContinue'

        $WebListener = Start-WebListener

        $NotFoundQuery = @{
            statuscode = 404
            responsephrase = 'Not Found'
            contenttype = 'application/json'
            body = '{"message": "oops"}'
            headers = "{}"
        }
    }

    AfterAll {
        $ProgressPreference = $oldProgress
    }

    #User-Agent changes on different platforms, so tests should only be run if on the correct platform
    It "Invoke-RestMethod returns Correct User-Agent on MacOSX" -Skip:(!$IsMacOS) {
        $uri = Get-WebListenerUrl -Test 'Get'
        $command = "Invoke-RestMethod -Uri '$uri'"

        $result = ExecuteWebCommand -command $command

        # Validate response
        $result.Output.headers.'User-Agent' | Should -MatchExactly '.*\(Macintosh;.*\) PowerShell\/\d+\.\d+\.\d+.*'
    }

    It "Invoke-RestMethod returns Correct User-Agent on Linux" -Skip:(!$IsLinux) {
        $uri = Get-WebListenerUrl -Test 'Get'
        $command = "Invoke-RestMethod -Uri '$uri'"

        $result = ExecuteWebCommand -command $command

        # Validate response
        $result.Output.headers.'User-Agent' | Should -MatchExactly '.*\(Linux;.*\) PowerShell\/\d+\.\d+\.\d+.*'
    }

    It "Invoke-RestMethod returns Correct User-Agent on Windows" -Skip:(!$IsWindows) {
        $uri = Get-WebListenerUrl -Test 'Get'
        $command = "Invoke-RestMethod -Uri '$uri'"

        $result = ExecuteWebCommand -command $command

        # Validate response
        $result.Output.headers.'User-Agent' | Should -MatchExactly '.*\(Windows NT \d+\.\d*;.*\) PowerShell\/\d+\.\d+\.\d+.*'
    }

    It "Invoke-RestMethod with blank ContentType succeeds" {
        $uri = Get-WebListenerUrl -Test 'Get'
        $command = "Invoke-RestMethod -Uri '$uri' -ContentType ''"

        $result = ExecuteWebCommand -command $command

        # Validate response
        $result.Error | Should -BeNullOrEmpty
        $result.Output.headers.'Content-Length' | Should -Be 0
    }

    It "Invoke-RestMethod returns headers dictionary" {
        $uri = Get-WebListenerUrl -Test 'Get'
        $command = "Invoke-RestMethod -Uri '$uri'"

        $result = ExecuteWebCommand -command $command

        # Validate response
        $result.Output.headers.Host | Should -Be $Uri.Authority
    }

    It "Validate Invoke-RestMethod -DisableKeepAlive" {
        # Operation options
        $uri = Get-WebListenerUrl -Test 'Get'
        $command = "Invoke-RestMethod -Uri '$uri' -DisableKeepAlive"

        $result = ExecuteWebCommand -command $command

        # Validate response
        $result.Output.headers.Host | Should -Be $uri.Authority
        $result.Output.Headers.Connection | Should -Be "Close"
    }

    It "Validate Invoke-RestMethod -HttpVersion '<httpVersion>'" -Skip:(!$IsWindows) -TestCases @(
        @{ httpVersion = '1.1'},
        @{ httpVersion = '2'}
    ) {
        param($httpVersion)
        # Operation options
        $uri = Get-WebListenerUrl -Test 'Get' -Https
        $command = "Invoke-RestMethod -Uri $uri -HttpVersion $httpVersion -SkipCertificateCheck"

        $result = ExecuteWebCommand -command $command

        # Validate response
        $result.Output.protocol | Should -Be "HTTP/$httpVersion"
    }

    It "Validate Invoke-RestMethod -MaximumRedirection" {
        $uri = Get-WebListenerUrl -Test 'Redirect' -TestValue '3'
        $command = "Invoke-RestMethod -Uri '$uri' -MaximumRedirection 4"

        $result = ExecuteWebCommand -command $command

        # Validate response
        $result.Output.headers.Host | Should -Match $uri.Authority
    }

    It "Validate Invoke-RestMethod error for -MaximumRedirection" {
        $uri = Get-WebListenerUrl -Test 'Redirect' -TestValue '3'
        $command = "Invoke-RestMethod -Uri '$uri' -MaximumRedirection 2"

        $result = ExecuteWebCommand -command $command
        $result.Error.FullyQualifiedErrorId | Should -Be "WebCmdletWebResponseException,Microsoft.PowerShell.Commands.InvokeRestMethodCommand"
    }

    It "Validate Invoke-RestMethod redirect with -Query destination Http" {
        $httpUri = Get-WebListenerUrl -Test 'Get'
        $uri = Get-WebListenerUrl -Test 'Redirect' -Query @{destination = $httpUri}
        $command = "Invoke-RestMethod -Uri '$uri'"

        $result = ExecuteWebCommand -command $command
        $result.Output.Headers.Host | Should -Be $httpUri.Authority
    }

    It "Validate Invoke-RestMethod redirect with -Query destination Https" {
        $httpsUri = Get-WebListenerUrl -Test 'Get' -Https
        $uri = Get-WebListenerUrl -Test 'Redirect' -Https -Query @{destination = $httpsUri}
        $command = "Invoke-RestMethod -Uri '$uri' -SkipCertificateCheck"

        $result = ExecuteWebCommand -command $command
        $result.Output.Headers.Host | Should -Be $httpsUri.Authority
    }

    It "Invoke-RestMethod supports request that returns page containing UTF-8 data." {
        $uri = Get-WebListenerUrl -Test 'Encoding' -TestValue 'Utf8'
        $command = "Invoke-RestMethod -Uri '$uri'"

        $result = ExecuteWebCommand -command $command
        $Result.Output | Should -Match '⡌⠁⠧⠑ ⠼⠁⠒  ⡍⠜⠇⠑⠹⠰⠎ ⡣⠕⠌'
    }

    It "Invoke-RestMethod supports sending requests as UTF8" {
        $uri = Get-WebListenerUrl -Test POST
        # Body must contain non-ASCII characters
        $command = "Invoke-RestMethod -Uri '$uri' -body 'проверка' -ContentType 'application/json; charset=utf-8' -method 'POST'"

        $result = ExecuteWebCommand -command $command
        $Result.Output.Data | Should -BeExactly 'проверка'
    }

    It "Invoke-RestMethod supports request that returns page containing Code Page 936 data." {
        $uri = Get-WebListenerUrl -Test 'Encoding' -TestValue 'CP936'
        $command = "Invoke-RestMethod -Uri '$uri'"

        $result = ExecuteWebCommand -command $command
        $Result.Output | Should -Match '测试123'
    }

    It "Invoke-RestMethod validate timeout option" {
        $uri = Get-WebListenerUrl -Test 'Delay' -TestValue '5'
        $command = "Invoke-RestMethod -Uri '$uri' -TimeoutSec 2"

        $result = ExecuteWebCommand -command $command
        $result.Error.FullyQualifiedErrorId | Should -Be "System.Threading.Tasks.TaskCanceledException,Microsoft.PowerShell.Commands.InvokeRestMethodCommand"
    }

    It "Validate Invoke-RestMethod error with -Proxy and -NoProxy option" {
        $uri = Get-WebListenerUrl -Test 'Delay' -TestValue '10'
        $command = "Invoke-RestMethod -Uri '$uri' -Proxy 'http://127.0.0.1:8080' -NoProxy -TimeoutSec 2"

        $result = ExecuteWebCommand -command $command
        $result.Error.FullyQualifiedErrorId | Should -Be "AmbiguousParameterSet,Microsoft.PowerShell.Commands.InvokeRestMethodCommand"
    }

    $testCase = @(
        @{ proxy_address = (Get-WebListenerUrl).Authority; name = 'HTTP proxy'; protocol = 'http' }
        @{ proxy_address = (Get-WebListenerUrl -https).Authority; name = 'HTTPS proxy'; protocol = 'https' }
    )

    It "Validate Invoke-RestMethod with -Proxy option - '<name>'" -TestCases $testCase {
        param($proxy_address, $name, $protocol)

        # use external url, but with proxy the external url should not actually be called
        $command = "Invoke-RestMethod -Uri ${protocol}://httpbin.org -Proxy '${protocol}://${proxy_address}'"
        $result = ExecuteWebCommand -command $command
        $command = "Invoke-RestMethod -Uri '${protocol}://${proxy_address}' -NoProxy"
        $expectedResult = ExecuteWebCommand -command $command
        $result.Output | Should -BeExactly $expectedResult.Output
    }

    # Perform the following operation for Invoke-RestMethod
    # gzip Returns gzip-encoded data.
    # deflate Returns deflate-encoded data.
    # brotli Returns brotli-encoded data.
    # $dataEncodings = @("Chunked", "Compress", "Deflate", "GZip", "Identity")
    # Note: These are the supported options, but we do not have a web service to test them all.
    It "Invoke-RestMethod supports request that returns <DataEncoding>-encoded data." -TestCases @(
        @{ DataEncoding = "gzip" }
        @{ DataEncoding = "deflate" }
        @{ DataEncoding = "brotli" }
    ) {
        param($dataEncoding)
        $uri = Get-WebListenerUrl -Test 'Compression' -TestValue $dataEncoding
        $result = Invoke-RestMethod -Uri $uri

        # Validate response content
        # The content should be de-compressed. Otherwise, the above 'Invoke-RestMethod' would have thrown because converting to JSON internally would fail.
        $result.Headers.Host | Should -BeExactly $uri.Authority
    }

    # Perform the following operation for Invoke-RestMethod using the following content types: "text/plain", "application/xml", "application/xml"
    # post Returns POST data.
    # patch Returns PATCH data.
    # put Returns PUT data.
    # delete Returns DELETE data
    $testMethods = @("POST", "PATCH", "PUT", "DELETE")
    $contentTypes = @("text/plain", "application/xml", "application/json")

    foreach ($contentType in $contentTypes) {
        foreach ($method in $testMethods) {
            # Operation options
            $uri = Get-WebListenerUrl -Test $method
            $body = GetTestData -contentType $contentType
            $command = "Invoke-RestMethod -Uri $uri -Body '$body' -Method $method -ContentType $contentType"

            It "Invoke-RestMethod -Uri $uri -Method $method -ContentType $contentType -Body [body data]" {

                $result = ExecuteWebCommand -command $command

                # Validate response
                $result.Output.url | Should -Match $uri
                $result.Output.headers.'Content-Type' | Should -Match $contentType

                # Validate that the response Content.data field is the same as what we sent.
                $result.Output.data | Should -Be $body
            }
        }
    }

    It "Validate Invoke-RestMethod -Headers --> Set KeepAlive to false via headers" {
        $uri = Get-WebListenerUrl -Test 'Get'
        $result = ExecuteRequestWithHeaders -cmdletName Invoke-RestMethod -uri $uri

        # Validate response
        $result.Output.url | Should -Match $uri
        $result.Output.Headers.Connection | Should -Be "Close"
    }

    # Validate all available user agents for Invoke-RestMethod
    $agents = @{
        InternetExplorer = "MSIE 9.0"
        Chrome           = "Chrome"
        Opera            = "Opera"
        Safari           = "Safari"
        FireFox          = "Firefox"
    }

    foreach ($agentName in $agents.Keys) {
        $expectedAgent = $agents[$agentName]
        $uri = Get-WebListenerUrl -Test 'Get'
        $userAgent = "[Microsoft.PowerShell.Commands.PSUserAgent]::$agentName"
        $command = "Invoke-RestMethod -Uri $uri -UserAgent ($userAgent) "

        It "Validate Invoke-RestMethod UserAgent. Execute--> $command" {

            $result = ExecuteWebCommand -command $command

            # Validate response
            $result.Output.headers.Host | Should -Be $uri.Authority
            $result.Output.headers.'User-Agent' | Should -Match $expectedAgent
        }
    }

    It "Validate Invoke-RestMethod -OutFile" {
        $uri = Get-WebListenerUrl -Test 'Get'
        $result = ExecuteRequestWithOutFile -cmdletName "Invoke-RestMethod" -uri $uri
        $jsonContent = $result.Output | ConvertFrom-Json
        $jsonContent.headers.Host | Should -Be $uri.Authority
    }

    It "Invoke-RestMethod -OutFile folder Downloads the file and names it" {
        $uri = Get-WebListenerUrl -Test 'Get'
        $content = Invoke-WebRequest -Uri $uri
        $outFile = Join-Path $TestDrive $content.BaseResponse.RequestMessage.RequestUri.Segments[-1]

        # ensure the file does not exist
        Remove-Item -Force -ErrorAction 'SilentlyContinue' -Path $outFile
        Invoke-RestMethod -Uri $uri -OutFile $TestDrive

        Test-Path $outFile | Should -Be $true
        Get-Item $outFile | Select-Object -ExpandProperty Length | Should -Be $content.Content.Length
    }

    It "Invoke-RestMethod should fail if -OutFile is <Name>." -TestCases @(
        @{ Name = "empty"; Value = [string]::Empty }
        @{ Name = "null"; Value = $null }
    ) {
        param ($value)
        $uri = Get-WebListenerUrl -Test 'Get'
        $errorId = "ParameterArgumentValidationError,Microsoft.PowerShell.Commands.InvokeRestMethodCommand"
        { Invoke-RestMethod -Uri $uri -OutFile $value} | Should -Throw -ErrorId $errorId
    }

    It "Validate Invoke-RestMethod handles missing Content-Type in response header" {
        #Validate that exception is not thrown when response headers are missing Content-Type.
        $uri = Get-WebListenerUrl -Test 'ResponseHeaders' -Query @{'Content-Type' = ''}
        $command = "Invoke-RestMethod -Uri '$uri'"
        $result = ExecuteWebCommand -command $command
        $result.Error | Should -BeNullOrEmpty
    }

    It "Validate Invoke-RestMethod StandardMethod and CustomMethod parameter sets" {
        $uri = Get-WebListenerUrl -Test 'Get'
        $errorId = "AmbiguousParameterSet,Microsoft.PowerShell.Commands.InvokeRestMethodCommand"
        { Invoke-RestMethod -Uri $uri -Method GET -CustomMethod TEST } | Should -Throw -ErrorId $errorId
    }

    It "Validate CustomMethod method is used" {
        $uri = Get-WebListenerUrl -Test 'Get'
        $command = "Invoke-RestMethod -Uri '$uri' -CustomMethod TEST"
        $result = ExecuteWebCommand -command $command
        $result.Error | Should -BeNullOrEmpty
        $result.Output.method | Should -Be "TEST"
    }

    It "Validate Invoke-RestMethod default ContentType for CustomMethod POST" {
        $uri = Get-WebListenerUrl -Test 'Post'
        $command = "Invoke-RestMethod -Uri '$uri' -CustomMethod POST -Body 'testparam=testvalue'"
        $result = ExecuteWebCommand -command $command
        $result.Output.form.testparam | Should -Be "testvalue"
        $result.Output.Headers.'Content-Type' | Should -Be "application/x-www-form-urlencoded"
    }

    It "Validate Invoke-RestMethod body is converted to query params for CustomMethod GET" {
        $uri = Get-WebListenerUrl -Test 'Get'
        $command = "Invoke-RestMethod -Uri '$uri' -CustomMethod GET -Body @{'testparam'='testvalue'}"
        $result = ExecuteWebCommand -command $command
        $result.Output.args.testparam | Should -Be "testvalue"
    }

    It 'Validate Invoke-RestMethod empty body CustomMethod GET' {
        $uri = Get-WebListenerUrl -Test 'Get'
        $command = "Invoke-RestMethod -Uri '$uri' -CustomMethod GET"
        $result = ExecuteWebCommand -command $command
        $result.Output.Headers.'Content-Length' | Should -BeNullOrEmpty
    }

    It "Validate Invoke-RestMethod body is converted to query params for CustomMethod GET and -NoProxy" {
        $uri = Get-WebListenerUrl -Test 'Get'
        $command = "Invoke-RestMethod -Uri '$uri' -CustomMethod GET -Body @{'testparam'='testvalue'} -NoProxy"
        $result = ExecuteWebCommand -command $command
        $result.Output.Query | Should -Be "?testparam=testvalue"
    }

    It "Validate Invoke-RestMethod returns HTTP errors in exception" {
        $query = @{
            body           = "I am a teapot!!!"
            statuscode     = 418
            responsephrase = "I am a teapot"
        }
        $uri = Get-WebListenerUrl -Test 'Response' -Query $query
        $command = "Invoke-RestMethod -Uri '$uri'"
        $result = ExecuteWebCommand -command $command

        $result.Error.ErrorDetails.Message | Should -Be $query.body
        $result.Error.Exception | Should -BeOfType Microsoft.PowerShell.Commands.HttpResponseException
        $result.Error.Exception.Response.StatusCode | Should -Be 418
        $result.Error.Exception.Response.ReasonPhrase | Should -Be $query.responsephrase
        $result.Error.Exception.Message | Should -Match ": 418 \($($query.responsephrase)\)\."
        $result.Error.FullyQualifiedErrorId | Should -Be "WebCmdletWebResponseException,Microsoft.PowerShell.Commands.InvokeRestMethodCommand"
    }

    It "Validate Invoke-RestMethod -FollowRelLink doesn't fail if no Link Header is present" {
        $query = @{
            contenttype = 'application/json'
            body        = '"foo"'
        }
        $uri = Get-WebListenerUrl -Test 'Response' -Query $query
        $command = "Invoke-RestMethod -Uri '$uri' -FollowRelLink"
        $result = ExecuteWebCommand -command $command

        $result.Output | Should -BeExactly "foo"
    }

    It "Validate Invoke-RestMethod -FollowRelLink correctly follows all the available relation links" {
        $maxLinks = 5
        $uri = Get-WebListenerUrl -Test 'Link' -Query @{maxlinks = $maxLinks}
        $command = "Invoke-RestMethod -Uri '$uri' -FollowRelLink"
        $result = ExecuteWebCommand -command $command

        $result.Output.Count | Should -BeExactly $maxLinks
        1..$maxLinks | ForEach-Object { $result.Output[$_ - 1].linknumber | Should -BeExactly $_ }
    }

    It "Validate Invoke-RestMethod -FollowRelLink correctly limits to -MaximumRelLink" {
        $maxLinks = 10
        $maxLinksToFollow = 6
        $uri = Get-WebListenerUrl -Test 'Link' -Query @{maxlinks = $maxLinks}
        $command = "Invoke-RestMethod -Uri '$uri' -FollowRelLink -MaximumFollowRelLink $maxLinksToFollow"
        $result = ExecuteWebCommand -command $command

        $result.Output.Count | Should -BeExactly $maxLinksToFollow
        1..$maxLinksToFollow | ForEach-Object { $result.Output[$_ - 1].linknumber | Should -BeExactly $_ }
    }

    It "Validate Invoke-RestMethod quietly ignores invalid Link Headers if -FollowRelLink is specified: <type>" -TestCases @(
        @{ type = "noUrl" }
        @{ type = "malformed" }
        @{ type = "noRel" }
    ) {
        param($type)
        $uri = Get-WebListenerUrl -Test 'Link' -Query @{type = $type}
        $command = "Invoke-RestMethod -Uri '$uri' -FollowRelLink"
        $result = ExecuteWebCommand -command $command
        $result.Output.linknumber | Should -BeExactly 1
    }

    It "Validate Invoke-RestMethod handles whitespace for Link Headers if -FollowRelLink is specified: <type>" -TestCases @(
        @{ type = "noWhitespace" }
        @{ type = "extraWhitespace" }
    ) {
        param($type)
        $uri = Get-WebListenerUrl -Test 'Link' -Query @{type = $type}
        $command = "Invoke-RestMethod -Uri '$uri' -FollowRelLink"
        $result = ExecuteWebCommand -command $command
        1..3 | ForEach-Object { $result.Output[$_ - 1].linknumber | Should -BeExactly $_ }
    }

    It "Validate Invoke-RestMethod -FollowRelLink correctly manages commas" {
        $maxLinks = 5
        $uri = Get-WebListenerUrl -Test 'Link' -Query @{maxlinks = $maxLinks; type = "with,comma"}
        $command = "Invoke-RestMethod -Uri '$uri' -FollowRelLink"
        $result = ExecuteWebCommand -command $command

        $result.Output.Count | Should -BeExactly $maxLinks
        1..$maxLinks | ForEach-Object { $result.Output[$_ - 1].linknumber | Should -BeExactly $_ }
    }

    It "Verify Invoke-RestMethod supresses terminating errors with -SkipHttpErrorCheck" {
        $uri =  Get-WebListenerUrl -Test 'Response' -Query $NotFoundQuery
        $command = "Invoke-RestMethod -SkipHttpErrorCheck -Uri '$uri'"
        $result = ExecuteWebCommand -Command $command
        $result.output.message | Should -BeExactly "oops"
        $result.output.error | Should -BeNullOrEmpty
    }

    It "Verify Invoke-RestMethod terminates without -SkipHttpErrorCheck" {
        $uri =  Get-WebListenerUrl -Test 'Response' -Query $NotFoundQuery
        $command = "Invoke-RestMethod -Uri '$uri'"
        $result = ExecuteWebCommand -Command $command
        $result.output | Should -BeNullOrEmpty
        $result.error | Should -Not -BeNullOrEmpty
    }

    It "Verify Invoke-RestMethod assigns 200 status code with -StatusCodeVariable" {
        $query = @{
            statuscode = 200
            responsephrase = 'OK'
            contenttype = 'application/json'
            body = '{"message": "works"}'
            headers = "{}"
        }

        $uri =  Get-WebListenerUrl -Test 'Response' -Query $query
        Invoke-RestMethod -StatusCodeVariable code -Uri "$uri"
        $code | Should -Be 200
    }

    It "Verify Invoke-RestMethod assigns 404 status code with -StatusCodeVariable" {
        $query = @{
            statuscode = 404
            responsephrase = 'Not Found'
            contenttype = 'application/json'
            body = '{"message": "oops"}'
            headers = "{}"
        }

        $uri =  Get-WebListenerUrl -Test 'Response' -Query $query
        Invoke-RestMethod -SkipHttpErrorCheck -StatusCodeVariable code -Uri "$uri"
        $code | Should -Be 404
    }

    It "Verify Invoke-RestMethod assigns 500 status code with -StatusCodeVariable" {
        $query = @{
            statuscode = 500
            responsephrase = 'Internal Server Error'
            contenttype = 'application/json'
            body = '{"message": "oops"}'
            headers = "{}"
        }

        $uri =  Get-WebListenerUrl -Test 'Response' -Query $query
        Invoke-RestMethod -SkipHttpErrorCheck -StatusCodeVariable code -Uri "$uri"
        $code | Should -Be 500
    }

    #region Redirect tests

    It "Validates Invoke-RestMethod with -PreserveAuthorizationOnRedirect preserves the authorization header on redirect: <redirectType> <redirectedMethod>" -TestCases $redirectTests {
        param($redirectType, $redirectedMethod)
        $uri = Get-WebListenerUrl -Test 'Redirect' -Query @{type = $redirectType}
        $response = ExecuteRedirectRequest  -Cmdlet 'Invoke-RestMethod' -Uri $uri -PreserveAuthorizationOnRedirect

        $response.Error | Should -BeNullOrEmpty
        # ensure Authorization header has been preserved.
        $response.Content.Headers."Authorization" | Should -BeExactly "test"
    }

    It "Validates Invoke-RestMethod preserves the authorization header on multiple redirects: <redirectType>" -TestCases $redirectTests {
        param($redirectType)
        $uri = Get-WebListenerUrl -Test 'Redirect' -TestValue 3 -Query @{type = $redirectType}
        $response = ExecuteRedirectRequest  -Cmdlet 'Invoke-RestMethod' -Uri $uri -PreserveAuthorizationOnRedirect

        $response.Error | Should -BeNullOrEmpty
        # ensure Authorization header was stripped
        $response.Content.Headers."Authorization" | Should -BeExactly "test"
    }

    It "Validates Invoke-RestMethod strips the authorization header on various redirects: <redirectType>" -TestCases $redirectTests {
        param($redirectType)
        $uri = Get-WebListenerUrl -Test 'Redirect' -Query @{type = $redirectType}
        $response = ExecuteRedirectRequest  -Cmdlet 'Invoke-RestMethod' -Uri $uri

        $response.Error | Should -BeNullOrEmpty
        # ensure user-agent is present (i.e., no false positives )
        $response.Content.Headers."User-Agent" | Should -Not -BeNullOrEmpty
        # ensure Authorization header has been removed.
        $response.Content.Headers."Authorization" | Should -BeNullOrEmpty
    }

    # NOTE: Only testing redirection of POST -> GET for unique underlying values of HttpStatusCode.
    # Some names overlap in underlying value.
    It "Validates Invoke-RestMethod strips the authorization header redirects and switches from POST to GET when it handles the redirect: <redirectType> <redirectedMethod>" -TestCases $redirectTests {
        param($redirectType, $redirectedMethod)
        $uri = Get-WebListenerUrl -Test 'Redirect' -Query @{type = $redirectType}
        $response = ExecuteRedirectRequest  -Cmdlet 'Invoke-RestMethod' -Uri $uri -Method 'POST'

        $response.Error | Should -BeNullOrEmpty
        # ensure user-agent is present (i.e., no false positives )
        $response.Content.Headers."User-Agent" | Should -Not -BeNullOrEmpty
        # ensure Authorization header has been removed.
        $response.Content.Headers."Authorization" | Should -BeNullOrEmpty
        # ensure POST was changed to GET for selected redirections and remains as POST for others.
        $response.Content.Method | Should -Be $redirectedMethod
    }

    It "Validates Invoke-RestMethod -PreserveAuthorizationOnRedirect keeps the authorization header redirects and switches from POST to GET when it handles the redirect: <redirectType> <redirectedMethod>" -TestCases $redirectTests {
        param($redirectType, $redirectedMethod)
        $uri = Get-WebListenerUrl -Test 'Redirect' -Query @{type = $redirectType}
        $response = ExecuteRedirectRequest -PreserveAuthorizationOnRedirect -Cmdlet 'Invoke-RestMethod' -Uri $uri -Method 'POST'

        $response.Error | Should -BeNullOrEmpty
        # ensure user-agent is present (i.e., no false positives )
        $response.Content.Headers."User-Agent" | Should -Not -BeNullOrEmpty
        # ensure Authorization header has been removed.
        $response.Content.Headers."Authorization" | Should -BeExactly 'test'
        # ensure POST was changed to GET for selected redirections and remains as POST for others.
        $response.Content.Method | Should -Be $redirectedMethod
    }

    It "Validates Invoke-RestMethod handles responses without Location header for requests with Authorization header and redirect: <redirectType>" -TestCases $redirectTests {
        param($redirectType, $redirectedMethod)
        # Skip relative test as it is not a valid response type.
        if ($redirectType -eq 'relative') { return }

        # When an Authorization request header is present,
        # and -PreserveAuthorizationOnRedirect is not present,
        # PowerShell should throw an HTTP Response Exception
        # for a redirect response which does not contain a Location response header.
        # The correct redirect status code should be included in the exception.

        $StatusCode = [int][System.Net.HttpStatusCode]$redirectType
        $uri = Get-WebListenerUrl -Test Response -Query @{statuscode = $StatusCode}
        $command = "Invoke-RestMethod -Uri '$uri' -Headers @{Authorization = 'foo'}"
        $response = ExecuteWebCommand -command $command

        $response.Error.Exception | Should -BeOfType Microsoft.PowerShell.Commands.HttpResponseException
        $response.Error.Exception.Response.StatusCode | Should -Be $StatusCode
        $response.Error.Exception.Response.Headers.Location | Should -BeNullOrEmpty
    }

    It "Validate Invoke-RestMethod Https to Http redirect with -AllowInsecureRedirect" {
        $httpUri = Get-WebListenerUrl -Test 'Get'
        $uri = Get-WebListenerUrl -Test 'Redirect' -Https -Query @{destination = $httpUri}
        $command = "Invoke-RestMethod -Uri '$uri' -SkipCertificateCheck -AllowInsecureRedirect"

        $result = ExecuteWebCommand -command $command
        $result.Output.Headers.Host | Should -Be $httpUri.Authority
    }

    It "Validate Invoke-RestMethod Https to Http redirect without -AllowInsecureRedirect" {
        $httpUri = Get-WebListenerUrl -Test 'Get'
        $uri = Get-WebListenerUrl -Test 'Redirect' -Https -Query @{destination = $httpUri}
        $command = "Invoke-RestMethod -Uri '$uri' -SkipCertificateCheck"

        $result = ExecuteWebCommand -command $command
        $result.Error.FullyQualifiedErrorId | Should -Be "WebCmdletWebResponseException,Microsoft.PowerShell.Commands.InvokeRestMethodCommand"
    }

    #endregion Redirect tests

    Context "Invoke-RestMethod SkipHeaderVerification Tests" {
        BeforeAll {
            $Testfile = Join-Path $testdrive 'SkipHeaderVerification.txt'
            'bar' | Set-Content $Testfile
        }

        It "Verifies Invoke-RestMethod default header handling with no errors" {
            $uri = Get-WebListenerUrl -Test 'Get'
            $headers = @{"If-Match" = "*"}
            $response = ExecuteRequestWithCustomHeaders -Uri $uri -headers $headers -Cmdlet "Invoke-RestMethod"

            $response.Error | Should -BeNullOrEmpty
            $response.Content.Headers."If-Match" | Should -BeExactly "*"
        }

        It "Verifies Invoke-RestMethod default header handling reports an error is returned for an invalid If-Match header value" {
            $uri = Get-WebListenerUrl -Test 'Get'
            $headers = @{"If-Match" = "12345"}
            $response = ExecuteRequestWithCustomHeaders -Uri $uri -headers $headers -Cmdlet "Invoke-RestMethod"

            $response.Error | Should -Not -BeNullOrEmpty
            $response.Error.FullyQualifiedErrorId | Should -Be "System.FormatException,Microsoft.PowerShell.Commands.InvokeRestMethodCommand"
            $response.Error.Exception.Message | Should -Be "The format of value '12345' is invalid."
        }

        It "Verifies Invoke-RestMethod header handling does not report an error when using -SkipHeaderValidation" {
            $uri = Get-WebListenerUrl -Test 'Get'
            $headers = @{"If-Match" = "12345"}
            $response = ExecuteRequestWithCustomHeaders -Uri $uri -headers $headers -SkipHeaderValidation -Cmdlet "Invoke-RestMethod"

            $response.Error | Should -BeNullOrEmpty
            $response.Content.Headers."If-Match" | Should -BeExactly "12345"
        }

        It "Verifies Invoke-RestMethod default UserAgent handling with no errors" {
            $uri = Get-WebListenerUrl -Test 'Get'
            $UserAgent = [Microsoft.PowerShell.Commands.PSUserAgent]::InternetExplorer
            $response = ExecuteRequestWithCustomUserAgent -Uri $uri -UserAgent $UserAgent -Cmdlet "Invoke-RestMethod"

            $response.Error | Should -BeNullOrEmpty
            $Pattern = [regex]::Escape($UserAgent)
            $response.Content.Headers."User-Agent" | Should -Match $Pattern
        }

        It "Verifies Invoke-RestMethod default UserAgent handling reports an error is returned for an invalid UserAgent value" {
            $uri = Get-WebListenerUrl -Test 'Get'
            $UserAgent = 'Invalid:Agent'
            $response = ExecuteRequestWithCustomUserAgent -Uri $uri -UserAgent $UserAgent  -Cmdlet "Invoke-RestMethod"

            $response.Error | Should -Not -BeNullOrEmpty
            $response.Error.FullyQualifiedErrorId | Should -Be "System.FormatException,Microsoft.PowerShell.Commands.InvokeRestMethodCommand"
            $response.Error.Exception.Message | Should -Be "The format of value 'Invalid:Agent' is invalid."
        }

        It "Verifies Invoke-RestMethod UserAgent handling does not report an error when using -SkipHeaderValidation" {
            $uri = Get-WebListenerUrl -Test 'Get'
            $UserAgent = 'Invalid:Agent'
            $response = ExecuteRequestWithCustomUserAgent -Uri $uri -UserAgent $UserAgent  -SkipHeaderValidation -Cmdlet "Invoke-RestMethod"

            $response.Error | Should -BeNullOrEmpty
            $Pattern = [regex]::Escape($UserAgent)
            $response.Content.Headers."User-Agent" | Should -Match $Pattern
        }

        It "Verifies Invoke-RestMethod default ContentType handling reports no error is returned for a valid Content-Type header value and -Body" {
            $contentType = 'text/plain'
            $body = "bar"
            $uri = Get-WebListenerUrl -Test 'Post'

            $result = Invoke-RestMethod -Uri $uri -Method 'Post' -ContentType $contentType -Body $body

            $result.data | Should -BeExactly $body
            $result.headers.'Content-Type' | Should -BeExactly $contentType
        }

        It "Verifies Invoke-RestMethod default ContentType handling reports an error is returned for an invalid Content-Type header value and -Body" {
            $contentType = 'foo'
            $body = "bar"
            $uri = Get-WebListenerUrl -Test 'Post'

            { Invoke-RestMethod -Uri $uri -Method 'Post' -ContentType $contentType -Body $body -ErrorAction 'Stop' } |
                Should -Throw -ErrorId "WebCmdletContentTypeException,Microsoft.PowerShell.Commands.InvokeRestMethodCommand"
        }

        It "Verifies Invoke-RestMethod ContentType handling reports no error is returned for an invalid Content-Type header value, -Body, and -SkipHeaderValidation" {
            $contentType = 'foo'
            $body = "bar"
            $uri = Get-WebListenerUrl -Test 'Post'

            $result = Invoke-RestMethod -Uri $uri -Method 'Post' -ContentType $contentType -Body $body -SkipHeaderValidation

            $result.data | Should -BeExactly $body
            $result.headers.'Content-Type' | Should -BeExactly $contentType
        }

        It "Verifies Invoke-RestMethod default ContentType handling reports no error is returned for a valid Content-Type header value and -InFile" {
            $contentType = 'text/plain'
            $uri = Get-WebListenerUrl -Test 'Post'

            $result = Invoke-RestMethod -Uri $uri -Method 'Post' -ContentType $contentType -InFile $Testfile

            # Match used due to inconsistent newline rendering
            $result.data | Should -Match 'bar'
            $result.headers.'Content-Type' | Should -BeExactly $contentType
        }

        It "Verifies Invoke-RestMethod default ContentType handling reports an error is returned for an invalid Content-Type header value and -InFile" {
            $contentType = 'foo'
            $uri = Get-WebListenerUrl -Test 'Post'

            { Invoke-RestMethod -Uri $uri -Method 'Post' -ContentType $contentType -InFile $Testfile -ErrorAction 'Stop' } |
                Should -Throw -ErrorId "WebCmdletContentTypeException,Microsoft.PowerShell.Commands.InvokeRestMethodCommand"
        }

        It "Verifies Invoke-RestMethod default ContentType handling reports no error is returned for an invalid Content-Type header value, -Infile, and -SkipHeaderValidation" {
            $contentType = 'foo'
            $uri = Get-WebListenerUrl -Test 'Post'

            $result = Invoke-RestMethod -Uri $uri -Method 'Post' -ContentType $contentType -InFile $Testfile -SkipHeaderValidation

            # Match used due to inconsistent newline rendering
            $result.data | Should -Match 'bar'
            $result.headers.'Content-Type' | Should -BeExactly $contentType
        }

        It "Verifies Invoke-RestMethod applies -ContentType when no -Body is present" {
            $contentType = 'application/json'
            $uri = Get-WebListenerUrl -Test 'Get'

            $result = Invoke-RestMethod -Uri $uri -Method 'GET' -ContentType $contentType

            $result.data | Should -BeNullOrEmpty
            $result.headers.'Content-Type' | Should -BeExactly $contentType
        }

        It "Verifies Invoke-RestMethod applies an invalid -ContentType when no -Body is present and -SkipHeaderValidation is present" {
            $contentType = 'foo'
            $uri = Get-WebListenerUrl -Test 'Get'

            $result = Invoke-RestMethod -Uri $uri -Method 'GET' -ContentType $contentType -SkipHeaderValidation

            $result.data | Should -BeNullOrEmpty
            $result.headers.'Content-Type' | Should -BeExactly $contentType
        }
    }

    Context "HTTPS Tests" {
        It "Validate Invoke-RestMethod -SkipCertificateCheck" {
            # HTTP method HEAD must be used to not retrieve an unparsable HTTP body
            # validate that exception is thrown for URI with expired certificate
            $uri = Get-WebListenerUrl -Https
            $command = "Invoke-RestMethod -Uri '$uri' -Method HEAD"
            $result = ExecuteWebCommand -command $command
            $result.Error.FullyQualifiedErrorId | Should -Be "WebCmdletWebResponseException,Microsoft.PowerShell.Commands.InvokeRestMethodCommand"

            # validate that no exception is thrown for URI with expired certificate when using -SkipCertificateCheck option
            $command = "Invoke-RestMethod -Uri '$uri' -SkipCertificateCheck -Method HEAD"
            $result = ExecuteWebCommand -command $command
            $result.Error | Should -BeNullOrEmpty
        }

        It "Validate Invoke-RestMethod returns native HTTPS error message in exception" {
            $uri = Get-WebListenerUrl -Https
            $command = "Invoke-RestMethod -Uri '$uri'"
            $result = ExecuteWebCommand -command $command

            # need to check against inner exception since Linux and Windows uses different HTTP client libraries so errors aren't the same
            $result.Error.ErrorDetails.Message | Should -Match $result.Error.Exception.InnerException.Message
            $result.Error.FullyQualifiedErrorId | Should -Be "WebCmdletWebResponseException,Microsoft.PowerShell.Commands.InvokeRestMethodCommand"
        }

        It "Verifies Invoke-RestMethod Certificate Authentication Fails without -Certificate" {
            $uri = Get-WebListenerUrl -Https -Test 'Cert'
            $result = Invoke-RestMethod -Uri $uri -SkipCertificateCheck

            $result.Status | Should -Be 'FAILED'
        }

        It "Verifies Invoke-RestMethod Certificate Authentication Successful with -Certificate" {
            $uri = Get-WebListenerUrl -Https -Test 'Cert'
            $certificate = Get-WebListenerClientCertificate
            $result = Invoke-RestMethod -Uri $uri -Certificate $certificate -SkipCertificateCheck

            $result.Status | Should -Be 'OK'
            $result.Thumbprint | Should -Be $certificate.Thumbprint
        }
    }

    Context "Multipart/form-data Tests" {
        <#
            Content-Type request headers for multipart/form-data appear as:
                multipart/form-data; boundary="0ab0cb90-f01b-4c15-bd4d-53d073efcc1d"
            MultipartFormDataContent sets a random GUID for the boundary before submitting the request
            to the remote endpoint. Tests in this context for Content-Type match 'multipart/form-data'
            as we do not have access to the random GUID.
        #>
        <#
            Kestrel/ASP.NET inconsistently renders the new line for uploaded text files.
            File content tests in this context use match as a workaround.
        #>
        BeforeAll {
            $file1Name = "testfile1.txt"
            $file1Path = Join-Path $testdrive $file1Name
            $file1Contents = "Test123"
            $file1Contents | Set-Content $file1Path -Force

            $file2Name = "testfile2.txt"
            $file2Path = Join-Path $testdrive $file2Name
            $file2Contents = "Test456"
            $file2Contents | Set-Content $file2Path -Force
        }

        It "Verifies Invoke-RestMethod Supports Multipart String Values" {
            $body = GetMultipartBody -String
            $uri = Get-WebListenerUrl -Test 'Multipart'
            $result = Invoke-RestMethod -Uri $uri -Body $body -Method 'POST'

            $result.Headers.'Content-Type' | Should -Match 'multipart/form-data'
            $result.Items.TestString[0] | Should -Be 'TestValue'
        }

        It "Verifies Invoke-RestMethod Supports Multipart File Values" {
            $body = GetMultipartBody -File
            $uri = Get-WebListenerUrl -Test 'Multipart'
            $result = Invoke-RestMethod -Uri $uri -Body $body -Method 'POST'

            $result.Headers.'Content-Type' | Should -Match 'multipart/form-data'
            $result.Files[0].FileName | Should -Be 'multipart.txt'
            $result.Files[0].ContentType | Should -Be 'text/plain'
            $result.Files[0].Content | Should -Match 'TestContent'
        }

        It "Verifies Invoke-RestMethod Supports Mixed Multipart String and File Values" {
            $body = GetMultipartBody -String -File
            $uri = Get-WebListenerUrl -Test 'Multipart'
            $result = Invoke-RestMethod -Uri $uri -Body $body -Method 'POST'

            $result.Headers.'Content-Type' | Should -Match 'multipart/form-data'
            $result.Items.TestString[0] | Should -Be 'TestValue'
            $result.Files[0].FileName | Should -Be 'multipart.txt'
            $result.Files[0].ContentType | Should -Be 'text/plain'
            $result.Files[0].Content | Should -Match 'TestContent'
        }

        It "Verifies Invoke-RestMethod -Form supports string values" {
            $form = @{TestString = "TestValue"}
            $uri = Get-WebListenerUrl -Test 'Multipart'
            $result = Invoke-RestMethod -Uri $uri -Form $form -Method 'POST'

            $result.Headers.'Content-Type' | Should -Match 'multipart/form-data'
            $result.Items.TestString.Count | Should -Be 1
            $result.Items.TestString[0] | Should -Be 'TestValue'
        }

        It "Verifies Invoke-RestMethod -Form supports a collection of string values" {
            $form = @{TestStrings = "TestValue", "TestValue2"}
            $uri = Get-WebListenerUrl -Test 'Multipart'
            $result = Invoke-RestMethod -Uri $uri -Form $form -Method 'POST'

            $result.Headers.'Content-Type' | Should -Match 'multipart/form-data'
            $result.Items.TestStrings.Count | Should -Be 2
            $result.Items.TestStrings[0] | Should -Be 'TestValue'
            $result.Items.TestStrings[1] | Should -Be 'TestValue2'
        }

        It "Verifies Invoke-RestMethod -Form supports file values" {
            $form = @{TestFile = [System.IO.FileInfo]$file1Path}
            $uri = Get-WebListenerUrl -Test 'Multipart'
            $result = Invoke-RestMethod -Uri $uri -Form $form -Method 'POST'

            $result.Headers.'Content-Type' | Should -Match 'multipart/form-data'
            $result.Files.Count | Should -Be 1

            $result.Files[0].Name | Should -Be "TestFile"
            $result.Files[0].FileName | Should -Be $file1Name
            $result.Files[0].ContentType | Should -Be 'application/octet-stream'
            $result.Files[0].Content | Should -Match $file1Contents
        }

        It "Verifies Invoke-RestMethod -Form supports a collection of file values" {
            $form = @{TestFiles = [System.IO.FileInfo]$file1Path, [System.IO.FileInfo]$file2Path}
            $uri = Get-WebListenerUrl -Test 'Multipart'
            $result = Invoke-RestMethod -Uri $uri -Form $form -Method 'POST'

            $result.Headers.'Content-Type' | Should -Match 'multipart/form-data'
            $result.Files.Count | Should -Be 2

            $result.Files[0].Name | Should -Be "TestFiles"
            $result.Files[0].FileName | Should -Be $file1Name
            $result.Files[0].ContentType | Should -Be 'application/octet-stream'
            $result.Files[0].Content | Should -Match $file1Contents

            $result.Files[1].Name | Should -Be "TestFiles"
            $result.Files[1].FileName | Should -Be $file2Name
            $result.Files[1].ContentType | Should -Be 'application/octet-stream'
            $result.Files[1].Content | Should -Match $file2Contents
        }

        It "Verifies Invoke-RestMethod -Form supports combinations of strings and files" {
            $form = @{
                TestStrings = "TestValue", "TestValue2"
                TestFiles   = [System.IO.FileInfo]$file1Path, [System.IO.FileInfo]$file2Path
            }
            $uri = Get-WebListenerUrl -Test 'Multipart'
            $result = Invoke-RestMethod -Uri $uri -Form $form -Method 'POST'

            $result.Headers.'Content-Type' | Should -Match 'multipart/form-data'
            $result.Items.TestStrings.Count | Should -Be 2
            $result.Files.Count | Should -Be 2

            $result.Items.TestStrings[0] | Should -Be 'TestValue'
            $result.Items.TestStrings[1] | Should -Be 'TestValue2'

            $result.Files[0].Name | Should -Be "TestFiles"
            $result.Files[0].FileName | Should -Be $file1Name
            $result.Files[0].ContentType | Should -Be 'application/octet-stream'
            $result.Files[0].Content | Should -Match $file1Contents

            $result.Files[1].Name | Should -Be "TestFiles"
            $result.Files[1].FileName | Should -Be $file2Name
            $result.Files[1].ContentType | Should -Be 'application/octet-stream'
            $result.Files[1].Content | Should -Match $file2Contents
        }

        It "Verifies Invoke-RestMethod -Form is mutually exclusive with -Body" {
            $form = @{TestString = "TestValue"}
            $body = "test"
            $uri = Get-WebListenerUrl -Test 'Multipart'

            {Invoke-RestMethod -Uri $uri -Form $form -Body $Body -ErrorAction 'Stop'} |
                Should -Throw -ErrorId 'WebCmdletBodyFormConflictException,Microsoft.PowerShell.Commands.InvokeRestMethodCommand'
        }

        It "Verifies Invoke-RestMethod -Form is mutually exclusive with -InFile" {
            $form = @{TestString = "TestValue"}
            $uri = Get-WebListenerUrl -Test 'Multipart'

            {Invoke-RestMethod -Uri $uri -Form $form -InFile $file1Path -ErrorAction 'Stop'} |
                Should -Throw -ErrorId 'WebCmdletFormInFileConflictException,Microsoft.PowerShell.Commands.InvokeRestMethodCommand'
        }
    }

    It "Verifies Invoke-RestMethod does not sent expect 100-continue headers by default" {
        $uri = Get-WebListenerUrl -Test 'Get'

        $result = Invoke-RestMethod -Uri $uri

        $result.headers.Expect | Should -BeNullOrEmpty
        $result.method | Should -BeExactly "GET"
        $result.url | Should -BeExactly $uri.ToString()
    }

    It "Verifies Invoke-RestMethod sends expect 100-continue header when defined in -Headers" {
        $uri = Get-WebListenerUrl -Test 'Get'

        $result = Invoke-RestMethod -Uri $uri -Headers @{Expect = '100-continue'}

        $result.headers.Expect | Should -BeExactly '100-continue'
        $result.method | Should -BeExactly "GET"
        $result.url | Should -BeExactly $uri.ToString()
    }

    #region charset encoding tests

    Context  "Invoke-RestMethod Encoding tests with BasicHtmlWebResponseObject response" {
        It "Verifies Invoke-RestMethod detects charset meta value when the ContentType header does not define it." {
            $query = @{
                contenttype = 'text/html'
                body        = '<html><head><meta charset="Unicode"></head></html>'
            }
            $uri = Get-WebListenerUrl -Test 'Response' -Query $query
            $expectedEncoding = [System.Text.Encoding]::GetEncoding('Unicode')
            $response = ExecuteRestMethod -Uri $uri -UseBasicParsing

            $response.Error | Should -BeNullOrEmpty
            $response.Encoding.EncodingName | Should -Be $expectedEncoding.EncodingName
        }

        It "Verifies Invoke-WebRequest detects charset meta value when newlines are encountered in the element." {
            $query = @{
                contenttype = 'text/html'
                body        = "<html>`n    <head>`n        <meta`n            charset=`"Unicode`"`n            >`n    </head>`n</html>"
            }
            $uri = Get-WebListenerUrl -Test 'Response' -Query $query
            $expectedEncoding = [System.Text.Encoding]::GetEncoding('Unicode')
            $response = ExecuteRestMethod -Uri $uri -UseBasicParsing

            $response.Error | Should -BeNullOrEmpty
            $response.Encoding.EncodingName | Should -Be $expectedEncoding.EncodingName
        }

        It "Verifies Invoke-RestMethod detects charset meta value when the attribute value is unquoted." {
            $query = @{
                contenttype = 'text/html'
                body        = '<html><head><meta charset = Unicode></head></html>'
            }
            $uri = Get-WebListenerUrl -Test 'Response' -Query $query
            $expectedEncoding = [System.Text.Encoding]::GetEncoding('Unicode')
            $response = ExecuteRestMethod -Uri $uri -UseBasicParsing

            $response.Error | Should -BeNullOrEmpty
            $response.Encoding.EncodingName | Should -Be $expectedEncoding.EncodingName
        }

        It "Verifies Invoke-RestMethod detects http-equiv charset meta value when the ContentType header does not define it." {
            $query = @{
                contenttype = 'text/html'
                body        = "<html><head>`n<meta http-equiv=`"content-type`" content=`"text/html; charset=Unicode`">`n</head>`n</html>"
            }
            $uri = Get-WebListenerUrl -Test 'Response' -Query $query
            $expectedEncoding = [System.Text.Encoding]::GetEncoding('Unicode')
            $response = ExecuteRestMethod -Uri $uri -UseBasicParsing

            $response.Error | Should -BeNullOrEmpty
            $response.Encoding.EncodingName | Should -Be $expectedEncoding.EncodingName
        }

        It "Verifies Invoke-RestMethod detects http-equiv charset meta value newlines are encountered in the element." {
            $query = @{
                contenttype = 'text/html'
                body        = "<html><head>`n<meta`n    http-equiv=`"content-type`"`n    content=`"text/html; charset=Unicode`">`n</head>`n</html>`n"
            }
            $uri = Get-WebListenerUrl -Test 'Response' -Query $query
            $expectedEncoding = [System.Text.Encoding]::GetEncoding('Unicode')
            $response = ExecuteRestMethod -Uri $uri -UseBasicParsing

            $response.Error | Should -BeNullOrEmpty
            $response.Encoding.EncodingName | Should -Be $expectedEncoding.EncodingName
        }

        It "Verifies Invoke-RestMethod ignores meta charset value when Content-Type header defines it." {
            $query = @{
                contenttype = 'text/html; charset=utf-8'
                body        = '<html><head><meta charset="utf-32"></head></html>'
            }
            $uri = Get-WebListenerUrl -Test 'Response' -Query $query
            # NOTE: meta charset should be ignored
            $expectedEncoding = [System.Text.Encoding]::UTF8
            $response = ExecuteRestMethod -Uri $uri -UseBasicParsing

            $response.Error | Should -BeNullOrEmpty
            $response.Encoding.EncodingName | Should -Be $expectedEncoding.EncodingName
        }

        It "Verifies Invoke-RestMethod honors non-utf8 charsets in the Content-Type header" {
            $query = @{
                contenttype = 'text/html; charset=utf-16'
                body        = '<html><head><meta charset="utf-32"></head></html>'
            }
            $uri = Get-WebListenerUrl -Test 'Response' -Query $query
            # NOTE: meta charset should be ignored
            $expectedEncoding = [System.Text.Encoding]::GetEncoding('utf-16')
            $response = ExecuteRestMethod -Uri $uri -UseBasicParsing

            $response.Error | Should -BeNullOrEmpty
            $response.Encoding.EncodingName | Should -Be $expectedEncoding.EncodingName
        }

        It "Verifies Invoke-RestMethod defaults to UTF-8 when an unsupported/invalid charset is declared" {
            $query = @{
                contenttype = 'text/html'
                body        = '<html><head><meta charset="invalid"></head></html>'
            }
            $uri = Get-WebListenerUrl -Test 'Response' -Query $query
            $expectedEncoding = [System.Text.Encoding]::UTF8
            $response = ExecuteRestMethod -Uri $uri -UseBasicParsing

            $response.Error | Should -BeNullOrEmpty
            $response.Encoding.EncodingName | Should -Be $expectedEncoding.EncodingName
        }

        It "Verifies Invoke-RestMethod defaults to UTF-8 when an unsupported/invalid charset is declared using http-equiv" {
            $query = @{
                contenttype = 'text/html'
                body        = "<html><head>`n<meta http-equiv=`"content-type`" content=`"text/html; charset=Invalid`">`n</head>`n</html>"
            }
            $uri = Get-WebListenerUrl -Test 'Response' -Query $query
            $expectedEncoding = [System.Text.Encoding]::UTF8
            $response = ExecuteRestMethod -Uri $uri -UseBasicParsing

            $response.Error | Should -BeNullOrEmpty
            $response.Encoding.EncodingName | Should -Be $expectedEncoding.EncodingName
        }
    }

    #endregion charset encoding tests

    Context 'Invoke-RestMethod ResponseHeadersVariable Tests' {
        It "Verifies Invoke-RestMethod supports -ResponseHeadersVariable" {
            $uri = Get-WebListenerUrl -Test '/'
            $response = Invoke-RestMethod -Uri $uri -ResponseHeadersVariable 'headers'

            $headers.'Content-Type' | Should -Be 'text/html; charset=utf-8'
            $headers.Server | Should -Be 'Kestrel'
        }

        It "Verifies Invoke-RestMethod supports -ResponseHeadersVariable overwriting existing variable" {
            $uri = Get-WebListenerUrl -Test '/'
            $headers = 'prexisting'
            $response = Invoke-RestMethod -Uri $uri -ResponseHeadersVariable 'headers'

            $headers | Should -Not -Be 'prexisting'
            $headers.'Content-Type' | Should -Be 'text/html; charset=utf-8'
            $headers.Server | Should -Be 'Kestrel'
        }
    }

    Context "Invoke-RestMethod -Authentication tests" {
        BeforeAll {
            #[SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="Demo/doc/test secret.")]
            $token = "testpassword" | ConvertTo-SecureString -AsPlainText -Force
            $credential = [pscredential]::new("testuser", $token)
            $httpUri = Get-WebListenerUrl -Test 'Get'
            $httpsUri = Get-WebListenerUrl -Test 'Get' -Https
            $httpBasicUri = Get-WebListenerUrl -Test 'Auth' -TestValue 'Basic'
            $httpsBasicUri = Get-WebListenerUrl -Test 'Auth' -TestValue 'Basic' -Https
            $testCases = @(
                @{Authentication = "bearer"}
                @{Authentication = "OAuth"}
            )
        }

        It "Verifies Invoke-RestMethod -Authentication Basic" {
            $params = @{
                Uri                  = $httpsUri
                Authentication       = "Basic"
                Credential           = $credential
                SkipCertificateCheck = $true
            }
            $result = Invoke-RestMethod @params

            $result.Headers.Authorization | Should -BeExactly "Basic dGVzdHVzZXI6dGVzdHBhc3N3b3Jk"
        }

        It "Verifies Invoke-RestMethod -Authentication Basic with null username" {
            $credential = [pscredential]::new([PSCustomObject]@{UserName = $null;Password=$token.psobject.BaseObject})
            $params = @{
                Uri                  = $httpsUri
                Authentication       = "Basic"
                Credential           = $credential
                SkipCertificateCheck = $true
            }
            $Response = Invoke-RestMethod @params

            $Response.Headers.Authorization | Should -BeExactly "Basic OnRlc3RwYXNzd29yZA=="
        }

        It "Verifies Invoke-RestMethod -Authentication <Authentication>" -TestCases $testCases {
            param($Authentication)
            $params = @{
                Uri                  = $httpsUri
                Authentication       = $Authentication
                Token                = $token
                SkipCertificateCheck = $true
            }
            $result = Invoke-RestMethod @params

            $result.Headers.Authorization | Should -BeExactly "Bearer testpassword"
        }

        It "Verifies Invoke-RestMethod -Authentication does not support -UseDefaultCredentials" {
            $params = @{
                Uri                   = $httpsUri
                Token                 = $token
                Authentication        = "OAuth"
                UseDefaultCredentials = $true
                ErrorAction           = 'Stop'
                SkipCertificateCheck  = $true
            }
            { Invoke-RestMethod @params } | Should -Throw -ErrorId "WebCmdletAuthenticationConflictException,Microsoft.PowerShell.Commands.InvokeRestMethodCommand"
        }

        It "Verifies Invoke-RestMethod -Authentication does not support Both -Credential and -Token" {
            $params = @{
                Uri                  = $httpsUri
                Token                = $token
                Credential           = $credential
                Authentication       = "OAuth"
                ErrorAction          = 'Stop'
                SkipCertificateCheck = $true
            }
            { Invoke-RestMethod @params } | Should -Throw -ErrorId "WebCmdletAuthenticationTokenConflictException,Microsoft.PowerShell.Commands.InvokeRestMethodCommand"
        }

        It "Verifies Invoke-RestMethod -Authentication <Authentication> requires -Token" -TestCases $testCases {
            param($Authentication)
            $params = @{
                Uri                  = $httpsUri
                Authentication       = $Authentication
                ErrorAction          = 'Stop'
                SkipCertificateCheck = $true
            }
            { Invoke-RestMethod @params } | Should -Throw -ErrorId "WebCmdletAuthenticationTokenNotSuppliedException,Microsoft.PowerShell.Commands.InvokeRestMethodCommand"
        }

        It "Verifies Invoke-RestMethod -Authentication Basic requires -Credential" {
            $params = @{
                Uri                  = $httpsUri
                Authentication       = "Basic"
                ErrorAction          = 'Stop'
                SkipCertificateCheck = $true
            }
            { Invoke-RestMethod @params } | Should -Throw -ErrorId "WebCmdletAuthenticationCredentialNotSuppliedException,Microsoft.PowerShell.Commands.InvokeRestMethodCommand"
        }

        It "Verifies Invoke-RestMethod -Authentication Requires HTTPS" {
            $params = @{
                Uri            = $httpUri
                Token          = $token
                Authentication = "OAuth"
                ErrorAction    = 'Stop'
            }
            { Invoke-RestMethod @params } | Should -Throw -ErrorId "WebCmdletAllowUnencryptedAuthenticationRequiredException,Microsoft.PowerShell.Commands.InvokeRestMethodCommand"
        }

        It "Verifies Invoke-RestMethod -Authentication Can use HTTP with -AllowUnencryptedAuthentication" {
            $params = @{
                Uri                            = $httpUri
                Token                          = $token
                Authentication                 = "OAuth"
                AllowUnencryptedAuthentication = $true
            }
            $result = Invoke-RestMethod @params

            $result.Headers.Authorization | Should -BeExactly "Bearer testpassword"
        }

        It "Verifies Invoke-RestMethod Negotiated -Credential over HTTPS" {
            $params = @{
                Uri                  = $httpsBasicUri
                Credential           = $credential
                SkipCertificateCheck = $true
            }
            $result = Invoke-RestMethod @params

            $result.Headers.Authorization | Should -BeExactly "Basic dGVzdHVzZXI6dGVzdHBhc3N3b3Jk"
        }

        It "Verifies Invoke-RestMethod Negotiated -Credential Requires HTTPS" {
            $params = @{
                Uri         = $httpBasicUri
                Credential  = $credential
                ErrorAction = 'Stop'
            }
            { Invoke-RestMethod @params } | Should -Throw -ErrorId "WebCmdletAllowUnencryptedAuthenticationRequiredException,Microsoft.PowerShell.Commands.InvokeRestMethodCommand"
        }

        It "Verifies Invoke-RestMethod Negotiated -Credential Can use HTTP with -AllowUnencryptedAuthentication" {
            $params = @{
                Uri                            = $httpBasicUri
                Credential                     = $credential
                AllowUnencryptedAuthentication = $true
            }
            $result = Invoke-RestMethod @params

            $result.Headers.Authorization | Should -BeExactly "Basic dGVzdHVzZXI6dGVzdHBhc3N3b3Jk"
        }

        # UseDefaultCredentials is only reliably testable on Windows
        It "Verifies Invoke-RestMethod Negotiated -UseDefaultCredentials with '<AuthType>' over HTTPS" -Skip:$(!$IsWindows) -TestCases @(
            @{AuthType = 'NTLM'}
            @{AuthType = 'Negotiate'}
        ) {
            param($AuthType)
            $params = @{
                Uri                   = Get-WebListenerUrl -Test 'Auth' -TestValue $AuthType -Https
                UseDefaultCredentials = $true
                SkipCertificateCheck  = $true
            }
            $result = Invoke-RestMethod @params

            $result.Headers.Authorization | Should -Match "^$AuthType "
        }

        # The error condition can at least be tested on all platforms.
        It "Verifies Invoke-RestMethod Negotiated -UseDefaultCredentials Requires HTTPS" {
            $params = @{
                Uri                   = $httpUri
                UseDefaultCredentials = $true
                ErrorAction           = 'Stop'
            }
            { Invoke-RestMethod @params } | Should -Throw -ErrorId "WebCmdletAllowUnencryptedAuthenticationRequiredException,Microsoft.PowerShell.Commands.InvokeRestMethodCommand"
        }

        # UseDefaultCredentials is only reliably testable on Windows
        It "Verifies Invoke-RestMethod Negotiated -UseDefaultCredentials with '<AuthType>' Can use HTTP with -AllowUnencryptedAuthentication" -Skip:$(!$IsWindows) -TestCases @(
            @{AuthType = 'NTLM'}
            @{AuthType = 'Negotiate'}
        ) {
            param($AuthType)
            $params = @{
                Uri                            = Get-WebListenerUrl -Test 'Auth' -TestValue $AuthType
                UseDefaultCredentials          = $true
                AllowUnencryptedAuthentication = $true
            }
            $result = Invoke-RestMethod @params

            $result.Headers.Authorization | Should -Match "^$AuthType "
        }
    }

    Context "Invoke-RestMethod -SslProtocol Test" {
        BeforeAll {
            # We put Tls13 tests at pending due to modern OS limitations.
            # Tracking issue https://github.com/PowerShell/PowerShell/issues/13439

            $skipForTls1 = $true

            $testCases1 = @(
                @{ Test = @{SslProtocol = 'Default'; ActualProtocol = 'Default'}; Pending = $false }
                @{ Test = @{SslProtocol = 'Tls'; ActualProtocol = 'Tls'}; Pending = $skipForTls1 }
                @{ Test = @{SslProtocol = 'Tls11'; ActualProtocol = 'Tls11'}; Pending = $skipForTls1 }
                @{ Test = @{SslProtocol = 'Tls12'; ActualProtocol = 'Tls12'}; Pending = $false }
                @{ Test = @{SslProtocol = 'Tls13'; ActualProtocol = 'Tls13'}; Pending = $true }
                @{ Test = @{SslProtocol = 'Tls, Tls11, Tls12'; ActualProtocol = 'Tls12'}; Pending = $false }
                @{ Test = @{SslProtocol = 'Tls, Tls11, Tls12, Tls13'; ActualProtocol = 'Tls13'}; Pending = $true }
                @{ Test = @{SslProtocol = 'Tls11, Tls12'; ActualProtocol = 'Tls12'}; Pending = $false }
                @{ Test = @{SslProtocol = 'Tls, Tls11, Tls12'; ActualProtocol = 'Tls11'}; Pending = $skipForTls1 }
                @{ Test = @{SslProtocol = 'Tls, Tls11, Tls12, Tls13'; ActualProtocol = 'Tls11'}; Pending = $skipForTls1 }
                @{ Test = @{SslProtocol = 'Tls11, Tls12'; ActualProtocol = 'Tls11'}; Pending = $skipForTls1 }
                @{ Test = @{SslProtocol = 'Tls, Tls11'; ActualProtocol = 'Tls11'}; Pending = $skipForTls1 }
                @{ Test = @{SslProtocol = 'Tls, Tls11, Tls12'; ActualProtocol = 'Tls'}; Pending = $skipForTls1 }
                @{ Test = @{SslProtocol = 'Tls, Tls11, Tls13'; ActualProtocol = 'Tls'}; Pending = $true }
                @{ Test = @{SslProtocol = 'Tls, Tls11'; ActualProtocol = 'Tls'}; Pending = $skipForTls1 }
                # Skipping intermediary protocols is not supported on all platforms
                # Removed this as Tls now default to Tls12
                # @{ Test = @{SslProtocol = 'Tls, Tls12'; ActualProtocol = 'Tls'}; Pending = -not $IsWindows }
                @{ Test = @{SslProtocol = 'Tls, Tls12'; ActualProtocol = 'Tls12'}; Pending = -not $IsWindows }
            )

            $testCases2 = @(
                @{ Test = @{IntendedProtocol = 'Tls'; ActualProtocol = 'Tls13'}; Pending = $true }
                @{ Test = @{IntendedProtocol = 'Tls11'; ActualProtocol = 'Tls13'}; Pending = $true }
                @{ Test = @{IntendedProtocol = 'Tls13'; ActualProtocol = 'Tls'}; Pending = $true }
                @{ Test = @{IntendedProtocol = 'Tls, Tls12'; ActualProtocol = 'Tls13'}; Pending = $true }
            )
        }

        foreach ($entry in $testCases1) {
            It "Verifies Invoke-RestMethod -SslProtocol <SslProtocol> works on <ActualProtocol>" -TestCases ($entry.Test) -Pending:($entry.Pending) {
                param($SslProtocol, $ActualProtocol)
                $params = @{
                    Uri                  = Get-WebListenerUrl -Test 'Get' -Https -SslProtocol $ActualProtocol
                    SslProtocol          = $SslProtocol
                    SkipCertificateCheck = $true
                }
                $result = Invoke-RestMethod @params

                $result.headers.Host | Should -Be $params.Uri.Authority
            }
        }

        foreach ($entry in $testCases2) {
            It "Verifies Invoke-RestMethod -SslProtocol <IntendedProtocol> fails on a <ActualProtocol> only connection" -TestCases ($entry.Test) -Pending:($entry.Pending) {
                param( $IntendedProtocol, $ActualProtocol)
                $params = @{
                    Uri                  = Get-WebListenerUrl -Test 'Get' -Https -SslProtocol $ActualProtocol
                    SslProtocol          = $IntendedProtocol
                    SkipCertificateCheck = $true
                    ErrorAction          = 'Stop'
                }
                { Invoke-RestMethod @params } | Should -Throw -ErrorId 'WebCmdletWebResponseException,Microsoft.PowerShell.Commands.InvokeRestMethodCommand'
            }
        }
    }

    Context "Invoke-RestMethod Single Value JSON null support" {
        It "Invoke-RestMethod Supports a Single Value JSON null" {
            $query = @{
                contenttype = 'application/json'
                body        = 'null'
            }
            $uri = Get-WebListenerUrl -Test 'Response' -Query $query
            Invoke-RestMethod -Uri $uri | Should -Be $null
        }

        It "Invoke-RestMethod Supports a Single Value JSON null and ignores whitespace" {
            $query = @{
                contenttype = 'application/json'
                body        = "            null         "
            }
            $uri = Get-WebListenerUrl -Test 'Response' -Query $query
            Invoke-RestMethod -Uri $uri | Should -Be $null
            $query['body'] = "           null         `n"
            $uri = Get-WebListenerUrl -Test 'Response' -Query $query
            Invoke-RestMethod -Uri $uri | Should -Be $null
        }
    }

    Context "Invoke-RestMethod File Resume Feature" {
        BeforeAll {
            $outFile = Join-Path $TestDrive "resume.txt"

            # Download the entire file to reference in tests
            $referenceFile = Join-Path $TestDrive "reference.txt"
            $resumeUri = Get-WebListenerUrl -Test 'Resume'
            Invoke-RestMethod -Uri $resumeUri -OutFile $referenceFile -ErrorAction Stop
            $referenceFileHash = Get-FileHash -Algorithm SHA256 -Path $referenceFile
            $referenceFileSize = Get-Item $referenceFile | Select-Object -ExpandProperty Length
        }

        AfterEach {
            Remove-Item -Force -ErrorAction 'SilentlyContinue' -Path $outFile
        }

        It "Invoke-RestMethod -Resume requires -OutFile" {
            { Invoke-RestMethod -Resume -Uri $resumeUri -ErrorAction Stop } |
                Should -Throw -ErrorId 'WebCmdletOutFileMissingException,Microsoft.PowerShell.Commands.InvokeRestMethodCommand'
        }

        It "Invoke-RestMethod -Resume should fail if -OutFile folder" {
            { Invoke-RestMethod -Resume -Uri $resumeUri -OutFile $TestDrive -ErrorAction Stop } |
                Should -Throw -ErrorId 'WebCmdletResumeNotFilePathException,Microsoft.PowerShell.Commands.InvokeRestMethodCommand'
        }

        It "Invoke-RestMethod -Resume Downloads the whole file when the file does not exist" {
            # ensure the file does not exist
            Remove-Item -Force -ErrorAction 'SilentlyContinue' -Path $outFile

            Invoke-RestMethod -Uri $resumeUri -OutFile $outFile -ResponseHeadersVariable 'Headers' -Resume

            $outFileHash = Get-FileHash -Algorithm SHA256 -Path $outFile
            $outFileHash.Hash | Should -BeExactly $referenceFileHash.Hash
            Get-Item $outFile | Select-Object -ExpandProperty Length | Should -Be $referenceFileSize
            $Headers.'X-WebListener-Has-Range'[0] | Should -BeExactly 'true'
            $Headers.'X-WebListener-Request-Range'[0] | Should -BeExactly 'bytes=0-'
            $Headers.'Content-Range'[0] | Should -BeExactly "bytes 0-$($referenceFileSize-1)/$referenceFileSize"
        }

        It "Invoke-RestMethod -Resume overwrites an existing file that is larger than the remote file" {
            # Create a file larger than the download file
            $largerFileSize = $referenceFileSize + 20
            1..$largerFileSize | ForEach-Object { [Byte]$_ } | Set-Content -AsByteStream $outFile
            $largerFileSize = Get-Item $outFile | Select-Object -ExpandProperty Length

            $response = Invoke-RestMethod -Uri $resumeUri -OutFile $outFile -ResponseHeadersVariable 'Headers' -Resume

            $outFileHash = Get-FileHash -Algorithm SHA256 -Path $outFile
            $outFileHash.Hash | Should -BeExactly $referenceFileHash.Hash
            Get-Item $outFile | Select-Object -ExpandProperty Length | Should -Be $referenceFileSize
            Get-Item $outFile | Select-Object -ExpandProperty Length | Should -BeLessThan $largerFileSize
            $Headers.'X-WebListener-Has-Range'[0] | Should -BeExactly 'false'
            $Headers.ContainsKey('Content-Range') | Should -BeFalse
        }

        It "Invoke-RestMethod -Resume overwrites existing file when remote server does not support resume" {
            # Create a file larger than the download file
            $largerFileSize = $referenceFileSize + 20
            1..$largerFileSize | ForEach-Object { [Byte]$_ } | Set-Content -AsByteStream $outFile
            $largerFileSize = Get-Item $outFile | Select-Object -ExpandProperty Length

            $uri = Get-WebListenerUrl -Test 'Resume' -TestValue 'NoResume'
            $response = Invoke-RestMethod -Uri $uri -OutFile $outFile -ResponseHeadersVariable 'Headers' -Resume

            $outFileHash = Get-FileHash -Algorithm SHA256 -Path $outFile
            $outFileHash.Hash | Should -BeExactly $referenceFileHash.Hash
            Get-Item $outFile | Select-Object -ExpandProperty Length | Should -Be $referenceFileSize
            Get-Item $outFile | Select-Object -ExpandProperty Length | Should -BeLessThan $largerFileSize
            $Headers.'X-WebListener-Has-Range'[0] | Should -BeExactly 'true'
            $Headers.'X-WebListener-Request-Range'[0] | Should -BeExactly "bytes=$largerFileSize-"
            $Headers.ContainsKey('Content-Range') | Should -BeFalse
        }

        It "Invoke-RestMethod -Resume resumes downloading from <bytes> bytes" -TestCases @(
            @{bytes = 4}
            @{bytes = 8}
            @{bytes = 12}
            @{bytes = 16}
        ) {
            param($bytes)
            # Simulate partial download
            $uri = Get-WebListenerUrl -Test 'Resume' -TestValue "Bytes/$bytes"
            $null = Invoke-RestMethod -Uri $uri -OutFile $outFile
            Get-Item $outFile | Select-Object -ExpandProperty Length | Should -Be $bytes

            $response = Invoke-RestMethod -Uri $resumeUri -OutFile $outFile -ResponseHeadersVariable 'Headers' -Resume

            $outFileHash = Get-FileHash -Algorithm SHA256 -Path $outFile
            $outFileHash.Hash | Should -BeExactly $referenceFileHash.Hash
            Get-Item $outFile | Select-Object -ExpandProperty Length | Should -Be $referenceFileSize
            $Headers.'X-WebListener-Has-Range'[0] | Should -BeExactly 'true'
            $Headers.'X-WebListener-Request-Range'[0] | Should -BeExactly "bytes=$bytes-"
            $Headers.'Content-Range'[0] | Should -BeExactly "bytes $bytes-$($referenceFileSize-1)/$referenceFileSize"
        }

        It "Invoke-RestMethod -Resume assumes the file was successfully completed when the local and remote file are the same size." {
            # Download the entire file
            $uri = Get-WebListenerUrl -Test 'Resume' -TestValue 'NoResume'
            $null = Invoke-RestMethod -Uri $uri -OutFile $outFile
            $fileSize = Get-Item $outFile | Select-Object -ExpandProperty Length

            $response = Invoke-RestMethod -Uri $resumeUri -OutFile $outFile -ResponseHeadersVariable 'Headers' -Resume

            $outFileHash = Get-FileHash -Algorithm SHA256 -Path $outFile
            $outFileHash.Hash | Should -BeExactly $referenceFileHash.Hash
            Get-Item $outFile | Select-Object -ExpandProperty Length | Should -Be $referenceFileSize
            $Headers.'X-WebListener-Has-Range'[0] | Should -BeExactly 'true'
            $Headers.'X-WebListener-Request-Range'[0] | Should -BeExactly "bytes=$fileSize-"
            $Headers.'Content-Range'[0] | Should -BeExactly "bytes */$referenceFileSize"
        }
    }

    Context "Invoke-RestMethod retry tests" {

        It "Invoke-RestMethod can retry - specified number of times - error 304" {

            $uri = Get-WebListenerUrl -Test 'Retry'
            $sessionId = (New-Guid).Guid
            $body = @{ sessionid = $sessionId; failureCode = 304; failureCount = 2 }
            $commandStr = "Invoke-RestMethod -Uri '$uri' -MaximumRetryCount 2 -RetryIntervalSec 1 -Method POST -Body `$body"
            $result = ExecuteWebCommand -command $commandStr

            $result.output.failureResponsesSent | Should -Be 2
            $result.output.sessionId | Should -BeExactly $sessionId
        }

        It "Invoke-RestMethod can retry with POST" {

            $uri = Get-WebListenerUrl -Test 'Retry'
            $sessionId = (New-Guid).Guid
            $body = @{ sessionid = $sessionId; failureCode = 404; failureCount = 1 }
            $commandStr = "Invoke-RestMethod -Uri '$uri' -MaximumRetryCount 2 -RetryIntervalSec 1 -Method POST -Body `$body"
            $result = ExecuteWebCommand -command $commandStr

            $result.output.failureResponsesSent | Should -Be 1
            $result.output.sessionId | Should -BeExactly $sessionId
        }
    }
}

Describe "Validate Invoke-WebRequest and Invoke-RestMethod -InFile" -Tags "Feature", "RequireAdminOnWindows" {
    BeforeAll {
        $oldProgress = $ProgressPreference
        $ProgressPreference = 'SilentlyContinue'
        $WebListener = Start-WebListener
    }

    AfterAll {
        $ProgressPreference = $oldProgress
    }

    Context "InFile parameter negative tests" {
        BeforeAll {
            $uri = Get-WebListenerUrl -Test 'Post'
            $testCases = @(
                #region INVOKE-WEBREQUEST
                @{
                    Name                          = 'Validate error for Invoke-WebRequest -InFile null'
                    ScriptBlock                   = {Invoke-WebRequest -Uri $uri -Method Post -InFile $null}
                    ExpectedFullyQualifiedErrorId = 'ParameterArgumentValidationError,Microsoft.PowerShell.Commands.InvokeWebRequestCommand'
                }

                @{
                    Name                          = 'Validate error for Invoke-WebRequest -InFile ""'
                    ScriptBlock                   = {Invoke-WebRequest -Uri $uri -Method Post -InFile ""}
                    ExpectedFullyQualifiedErrorId = 'ParameterArgumentValidationError,Microsoft.PowerShell.Commands.InvokeWebRequestCommand'
                }

                @{
                    Name                          = 'Validate error for Invoke-WebRequest -InFile'
                    ScriptBlock                   = {Invoke-WebRequest -Uri $uri -Method Post -InFile}
                    ExpectedFullyQualifiedErrorId = 'MissingArgument,Microsoft.PowerShell.Commands.InvokeWebRequestCommand'
                }

                @{
                    Name                          = "Validate error for Invoke-WebRequest -InFile $TestDrive\content.txt"
                    ScriptBlock                   = {Invoke-WebRequest -Uri $uri -Method Post -InFile $TestDrive\content.txt}
                    ExpectedFullyQualifiedErrorId = 'PathNotFound,Microsoft.PowerShell.Commands.InvokeWebRequestCommand'
                }

                @{
                    Name                          = "Validate error for Invoke-WebRequest -InFile $TestDrive"
                    ScriptBlock                   = {Invoke-WebRequest -Uri $uri -Method Post -InFile $TestDrive}
                    ExpectedFullyQualifiedErrorId = 'WebCmdletInFileNotFilePathException,Microsoft.PowerShell.Commands.InvokeWebRequestCommand'
                }
                #endregion

                #region INVOKE-RESTMETHOD
                @{
                    Name                          = "Validate error for Invoke-RestMethod -InFile null"
                    ScriptBlock                   = {Invoke-RestMethod -Uri $uri -Method Post -InFile $null}
                    ExpectedFullyQualifiedErrorId = 'ParameterArgumentValidationError,Microsoft.PowerShell.Commands.InvokeRestMethodCommand'
                }

                @{
                    Name                          = "Validate error for Invoke-RestMethod -InFile ''"
                    ScriptBlock                   = {Invoke-RestMethod -Uri $uri -Method Post -InFile ''}
                    ExpectedFullyQualifiedErrorId = 'ParameterArgumentValidationError,Microsoft.PowerShell.Commands.InvokeRestMethodCommand'
                }

                @{
                    Name                          = "Validate error for Invoke-RestMethod -InFile"
                    ScriptBlock                   = {Invoke-RestMethod -Uri $uri -Method Post -InFile}
                    ExpectedFullyQualifiedErrorId = 'MissingArgument,Microsoft.PowerShell.Commands.InvokeRestMethodCommand'
                }

                @{
                    Name                          = "Validate error for Invoke-RestMethod -InFile $TestDrive\content.txt"
                    ScriptBlock                   = {Invoke-RestMethod -Uri $uri -Method Post -InFile $TestDrive\content.txt}
                    ExpectedFullyQualifiedErrorId = 'PathNotFound,Microsoft.PowerShell.Commands.InvokeRestMethodCommand'
                }

                @{
                    Name                          = "Validate error for Invoke-RestMethod -InFile $TestDrive"
                    ScriptBlock                   = {Invoke-RestMethod -Uri $uri -Method Post -InFile $TestDrive}
                    ExpectedFullyQualifiedErrorId = 'WebCmdletInFileNotFilePathException,Microsoft.PowerShell.Commands.InvokeRestMethodCommand'
                }
                #endregion
            )
        }

        It "<Name>" -TestCases $testCases {
            param ($scriptblock, $expectedFullyQualifiedErrorId)

            { & $scriptblock } | Should -Throw -ErrorId $ExpectedFullyQualifiedErrorId
        }
    }

    Context "InFile parameter positive tests" {
        BeforeAll {
            $filePath = Join-Path $TestDrive test.txt
            New-Item -Path $filePath -Value "hello=world" -ItemType File -Force
            $uri = Get-WebListenerUrl -Test 'Post'
        }

        It "Invoke-WebRequest -InFile" {
            $result = Invoke-WebRequest -InFile $filePath  -Uri $uri -Method Post
            $content = $result.Content | ConvertFrom-Json
            $content.form.hello.Count | Should -Be 1
            $content.form.hello[0] | Should -Match "world"
        }

        It "Invoke-RestMethod -InFile" {
            $result = Invoke-RestMethod -InFile $filePath  -Uri $uri -Method Post
            $result.form.hello.Count | Should -Be 1
            $result.form.hello[0] | Should -Match "world"
        }
    }
}

Describe "Web cmdlets tests using the cmdlet's aliases" -Tags "CI", "RequireAdminOnWindows" {
    BeforeAll {
        $WebListener = Start-WebListener
    }

    It "Execute Invoke-WebRequest" {
        $query = @{
            body        = "hello"
            contenttype = 'text/plain'
        }
        $uri = Get-WebListenerUrl -Test 'Response' -Query $query
        $result = Invoke-WebRequest $uri
        $result.StatusCode | Should -Be "200"
        $result.Content | Should -Be "hello"
    }

    It "Execute Invoke-RestMethod" {
        $query = @{
            contenttype = 'application/json'
            body        = @{Hello = "world"} | ConvertTo-Json -Compress
        }
        $uri = Get-WebListenerUrl -Test 'Response' -Query $query
        $result = Invoke-RestMethod $uri
        $result.Hello | Should -Be "world"
    }

    It "Web cmdlets ignore headers with null value" {
        $query = @{
            body        = "hello"
            contenttype = 'text/plain'
        }
        $uri = Get-WebListenerUrl -Test 'Response' -Query $query

        # Core throws if a header has null value.
        # We ignore such headers so no exception is expected.
        { Invoke-WebRequest -Uri $uri -Headers @{ "Location" = $null } } | Should -Not -Throw
        { Invoke-WebRequest -Uri $uri -ContentType $null } | Should -Not -Throw
        { Invoke-RestMethod -Uri $uri -Headers @{ "Location" = $null } } | Should -Not -Throw
        { Invoke-RestMethod -Uri $uri -ContentType $null } | Should -Not -Throw
    }
}
