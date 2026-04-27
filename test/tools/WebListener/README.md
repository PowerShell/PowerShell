# WebListener App

ASP.NET Core app for testing HTTP and HTTPS Requests.

## Run with `dotnet`

```bash
dotnet restore
dotnet publish --output bin --configuration Release
cd bin
dotnet WebListener.dll ServerCert.pfx password 8083 8084 8085 8086
```

**NOTE**: `ServerCert.pfx` is no longer a static asset
and you will need to create your own certificate for this purpose.
The `SelfSignedCertificate` module in the PowerShell Gallery provides this functionality.

The test site can then be accessed via `http://localhost:8083/`, `https://localhost:8084/`, `https://localhost:8085/`, or `https://localhost:8086/`.

The `WebListener.dll` takes 6 arguments:

* The path to the Server Certificate
* The Server Certificate Password
* The TCP Port to bind on for HTTP
* The TCP Port to bind on for HTTPS using TLS 1.2
* The TCP Port to bind on for HTTPS using TLS 1.1
* The TCP Port to bind on for HTTPS using TLS 1.0

## Run With WebListener Module

```powershell
Import-Module .\build.psm1
Publish-PSTestTools
$Listener = Start-WebListener -HttpPort 8083 -HttpsPort 8084 -Tls11Port 8085 -TlsPort 8086

```

## Tests

### / or /Home/

Returns a static HTML page containing links and descriptions of the available tests in WebListener. This can be used as a default or general test where no specific test functionality or return data is required.

### /Auth/Basic/

Provides a mock Basic authentication challenge. If a basic authorization header is sent, then the same results as /Get/ are returned.

```powershell
$credential = Get-Credential
$uri = Get-WebListenerUrl -Test 'Auth' -TestValue 'Basic' -Https
Invoke-RestMethod -Uri $uri -Credential $credential -SkipCertificateCheck
```

```json
{
    "headers":{
        "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Microsoft Windows 10.0.15063; en-US) PowerShell/6.0.0",
        "Connection": "Keep-Alive",
        "Authorization": "Basic dGVzdHVzZXI6dGVzdHBhc3N3b3Jk",
        "Host": "localhost:8084"
    },
    "origin": "127.0.0.1",
    "args": {},
    "url": "https://localhost:8084/Auth/Basic"
}
```

### /Auth/Negotiate/

Provides a mock Negotiate authentication challenge. If a basic authorization header is sent, then the same results as /Get/ are returned.

```powershell
$uri = Get-WebListenerUrl -Test 'Auth' -TestValue 'Negotiate' -Https
Invoke-RestMethod -Uri $uri -UseDefaultCredential -SkipCertificateCheck
```

```json
{
    "headers":{
        "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Microsoft Windows 10.0.15063; en-US) PowerShell/6.0.0",
        "Connection": "Keep-Alive",
        "Authorization": "Negotiate jjaguasgtisi7tiqkagasjjajvs",
        "Host": "localhost:8084"
    },
    "origin": "127.0.0.1",
    "args": {},
    "url": "https://localhost:8084/Auth/Negotiate"
}
```

### /Auth/NTLM/

Provides a mock NTLM authentication challenge. If a basic authorization header is sent, then the same results as /Get/ are returned.

```powershell
$uri = Get-WebListenerUrl -Test 'Auth' -TestValue 'NTLM' -Https
Invoke-RestMethod -Uri $uri -UseDefaultCredential -SkipCertificateCheck
```

```json
{
    "headers":{
        "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Microsoft Windows 10.0.15063; en-US) PowerShell/6.0.0",
        "Connection": "Keep-Alive",
        "Authorization": "NTLM jjaguasgtisi7tiqkagasjjajvs",
        "Host": "localhost:8084"
    },
    "origin": "127.0.0.1",
    "args": {},
    "url": "https://localhost:8084/Auth/NTLM"
}
```

### /Cert/

Returns a JSON object containing the details of the Client Certificate if one is provided in the request.

```powershell
$certificate = Get-WebListenerClientCertificate
$uri = Get-WebListenerUrl -Test 'Cert' -Https
Invoke-RestMethod -Uri $uri -Certificate $certificate
```

Response when certificate is provided in request:

```json
{
  "Status": "OK",
  "IssuerName": "E=randd@adatum.com, CN=adatum.com, OU=R&D, O=A. Datum Corporation, L=Redmond, S=Washington, C=US",
  "SubjectName": "E=randd@adatum.com, CN=adatum.com, OU=R&D, O=A. Datum Corporation, L=Redmond, S=Washington, C=US",
  "NotAfter": "2044-12-26T12:16:46-06:00",
  "Issuer": "E=randd@adatum.com, CN=adatum.com, OU=R&D, O=A. Datum Corporation, L=Redmond, S=Washington, C=US",
  "Subject": "E=randd@adatum.com, CN=adatum.com, OU=R&D, O=A. Datum Corporation, L=Redmond, S=Washington, C=US",
  "NotBefore": "2017-08-10T13:16:46-05:00",
  "Thumbprint": "2DECF1348FF21B780F45D316A039B5EB4C6312F7"
}
```

Response when certificate is not provided in request:

```json
{
  "Status": "FAILED"
}
```

### /Compression/Brotli/

Returns the same results as the Get test with brotli compression.

```powershell
$uri = Get-WebListenerUrl -Test 'Compression' -TestValue 'Brotli'
Invoke-RestMethod -Uri $uri
```

```json
{
  "args": {},
  "origin": "127.0.0.1",
  "headers": {
    "User-Agent": "Mozilla/5.0 (Windows NT; Microsoft Windows 10.0.15063 ; en-US) PowerShell/6.0.0",
    "Host": "localhost:8083"
  },
  "url": "http://localhost:8083/Compression/Brotli"
}
```

### /Compression/Deflate/

Returns the same results as the Get test with deflate compression.

```powershell
$uri = Get-WebListenerUrl -Test 'Compression' -TestValue 'Deflate'
Invoke-RestMethod -Uri $uri
```

```json
{
  "args": {},
  "origin": "127.0.0.1",
  "headers": {
    "User-Agent": "Mozilla/5.0 (Windows NT; Microsoft Windows 10.0.15063 ; en-US) PowerShell/6.0.0",
    "Host": "localhost:8083"
  },
  "url": "http://localhost:8083/Compression/Deflate"
}
```

### /Compression/Gzip/

Returns the same results as the Get test with gzip compression.

```powershell
$uri = Get-WebListenerUrl -Test 'Compression' -TestValue 'Gzip'
Invoke-RestMethod -Uri $uri
```

```json
{
  "args": {},
  "origin": "127.0.0.1",
  "headers": {
    "User-Agent": "Mozilla/5.0 (Windows NT; Microsoft Windows 10.0.15063 ; en-US) PowerShell/6.0.0",
    "Host": "localhost:8083"
  },
  "url": "http://localhost:8083/Compression/Gzip"
}
```

### /Delay/

Returns the same results as the Get test. If a number is supplied, the server will wait that many seconds before returning a response. This can be used to test timeouts.

```powershell
$uri = Get-WebListenerUrl -Test 'Delay' -TestValue '5'
Invoke-RestMethod -Uri $uri
```

After 5 Seconds:

```json
{
  "args": {
  },
  "origin": "127.0.0.1",
  "headers": {
    "User-Agent": "Mozilla/5.0 (Windows NT; Windows NT 10.0; en-US) WindowsPowerShell/5.1.15063.608",
    "Host": "localhost:8083"
  },
  "url": "http://localhost:8083/Delay/5"
}
```

### /Delete/

Returns the same results as the Get test. Will only accept the `DELETE` request method.

```powershell
$uri = Get-WebListenerUrl -Test 'Delete'
$Body = @{id = 12345} | ConvertTo-Json -Compress
Invoke-RestMethod -Uri $uri -Body $body -Method 'Delete'
```

```json
{
  "method": "DELETE",
  "headers": {
    "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Microsoft Windows 10.0.15063; en-US) PowerShell/6.0.0",
    "Connection": "Keep-Alive",
    "Host": "localhost:8083",
    "Content-Length": "12"
  },
  "origin": "127.0.0.1",
  "url": "http://localhost:8083/Delete",
  "args": {},
  "data": "{\"id\":12345}"
}
```

### /Dos/

Returns HTML designed to create denial of service against specific RegEx Expressions

#### Image Parsing RegEx

```powershell
$uri = Get-WebListenerUrl -Test 'Dos' -query @{
                dosType='img'
                dosLength='5000'
            }
Invoke-RestMethod -Uri $uri -Body $body -Method 'Delete'
```

Return the following followed by 5,000 spaces.

```html
<img
```

#### Charset Parsing RegEx

```powershell
$uri = Get-WebListenerUrl -Test 'Dos' -query @{
                dosType='charset'
                dosLength='5000'
            }
Invoke-RestMethod -Uri $uri -Body $body -Method 'Delete'
```

Return the following followed by 5,000 spaces.

```html
<meta
```

### /Encoding/Utf8/

Returns page containing UTF-8 data.

```powershell
$uri = Get-WebListenerUrl -Test 'Encoding' -TestValue 'Utf8'
Invoke-RestMethod -Uri $uri
```

### /Get/

Returns a JSON object containing the Request URL, Request Headers, GET Query Fields and Values, and Origin IP. This emulates the functionality of [HttpBin's get test](https://httpbin.org/get).

```powershell
$uri = Get-WebListenerUrl -Test 'Get'
Invoke-RestMethod -Uri $uri -Body @{TestField = 'TestValue'}
```

```json
{
  "origin": "127.0.0.1",
  "url": "http://localhost:8083/Get?TestField=TestValue",
  "method": "GET",
  "args": {
    "TestField": "TestValue"
  },
  "headers": {
    "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Microsoft Windows 10.0.15063; en-US) PowerShell/6.0.0",
    "Connection": "Keep-Alive",
    "Host": "localhost:8083"
  }
}
```

### /Link/

Returns Link response headers to test paginated results. The endpoint accepts 3 query items:

* `linknumber` - The current link number. This determines the current page. If not supplied or less than 1, this will be set to 1.
* `maxlinks` - The maximum number of links. This determines the last page. If not supplied or less than 1, this will be set to 3.
* `type` - The type of link to return. When not supplied or not in the list below, `default` will be used.
  * `default` - Does not return any special test links and returns `next` link if one is available.
  * `norel` - Returns a Link header that does not include the `rel=` portion. Suppresses `next` link.
  * `nourl` - Returns a Link header that does not include the URI portion. Suppresses `next` link.
  * `malformed` - Returns a malformed Link header. Suppresses `next` link.
  * `multiple` - Returns multiple Link headers instead of a single Link header and returns `next` link if one is available.
  * `nowhitespace` - Returns `default` links without any whitespace between the semicolon and `rel`
  * `extrawhitespace` - Returns `default` links with double whitespace between the semicolon and `rel`

The body will contain the same results as `/Get/` with the addition of the `type`, `linknumber`, and `maxlinks` for the current page.

```powershell
$Query = @{
    linknumber = 1
    maxlinks = 3
    type = 'default'
}
$Uri =  Get-WebListenerUrl -Test 'Link' -Query $Query
Invoke-RestMethod -Uri $uri -FollowRelLink -MaximumFollowRelLink 1
```

Headers:

```none
HTTP/1.1 200 OK
Date: Sat, 06 Jan 2018 14:27:36 GMT
Content-Type: application/json; charset=utf-8
Server: Kestrel
Transfer-Encoding: chunked
Link: <http://localhost:8083/Link/?maxlinks=3&linknumber=3>; rel="last",<http://localhost:8083/Link/?maxlinks=3&linknumber=1>; rel="first",<http://localhost:8083/Link/?maxlinks=3&linknumber=1>; rel="self",<http://localhost:8083/Link/?maxlinks=3&linknumber=2>; rel="next"
```

Body:

```json
{
    "type": "default",
    "url": "http://localhost:8083/Link/?maxlinks=3&linknumber=1&type=default",
    "maxlinks": 3,
    "linknumber": 1,
    "headers": {
        "User-Agent": "insomnia/5.12.4",
        "Accept": "*/*",
        "Content-Length": "0",
        "Host": "localhost:8083",
        "Content-Type": "application/json"
    },
    "args": {
        "linknumber": "1",
        "maxlinks": "3",
        "type": "default"
    },
    "origin": "127.0.0.1",
    "method": "GET"
}
```

### /Multipart/

#### GET

Provides an HTML form for `multipart/form-data` submission.

#### POST

Accepts a `multipart/form-data` submission and returns a JSON object containing information about the submission including the items and files submitted.

```powershell
$uri = Get-WebListenerUrl -Test 'Multipart'
Invoke-RestMethod -Uri $uri -Body $multipartData -Method 'POST'
```

```json
{
  "Files": [
    {
      "ContentDisposition": "form-data; name=fileData; filename=test.txt",
      "Headers": {
        "Content-Disposition": [
          "form-data; name=fileData; filename=test.txt"
        ],
        "Content-Type": [
          "text/plain"
        ]
      },
      "FileName": "test.txt",
      "Length": 15,
      "ContentType": "text/plain",
      "Name": "fileData",
      "Content": "Test Contents\r\n"
    }
  ],
  "Items": {
    "stringData": [
      "TestValue"
    ]
  },
  "Boundary": "83027bde-fd9b-4ea0-b1ca-a1f661d01ada",
  "Headers": {
    "Content-Type": "multipart/form-data; boundary=\"83027bde-fd9b-4ea0-b1ca-a1f661d01ada\"",
    "Connection": "Keep-Alive",
    "Content-Length": "336",
    "Host": "localhost:8083",
    "User-Agent": "Mozilla/5.0 (Windows NT; Microsoft Windows 10.0.15063 ; en-US) WindowsPowerShell/6.0.0"
  }
}
```

### /Patch/

Returns the same results as the Get test. Will only accept the `PATCH` request method.

```powershell
$uri = Get-WebListenerUrl -Test 'Patch'
$Body = @{id = 12345} | ConvertTo-Json -Compress
Invoke-RestMethod -Uri $uri -Body $body -Method 'Patch'
```

```json
{
  "method": "PATCH",
  "headers": {
    "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Microsoft Windows 10.0.15063; en-US) PowerShell/6.0.0",
    "Connection": "Keep-Alive",
    "Host": "localhost:8083",
    "Content-Length": "12"
  },
  "origin": "127.0.0.1",
  "url": "http://localhost:8083/Patch",
  "args": {},
  "data": "{\"id\":12345}"
}
```

### /Post/

Returns the same results as the Get test. Will only accept the `POST` request method. If the POST request is sent with a forms based content type the body will be interpreted as a form instead of raw data.

```powershell
$uri = Get-WebListenerUrl -Test 'Post'
$Body = @{id = 12345}
Invoke-RestMethod -Uri $uri -Body $body -Method 'Post'
```

```json
{
  "method": "POST",
  "headers": {
    "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Microsoft Windows 10.0.15063; en-US) PowerShell/6.0.0",
    "Connection": "Keep-Alive",
    "Host": "localhost:8083",
    "Content-Type": "application/x-www-form-urlencoded",
    "Content-Length": "8"
  },
  "form": {
    "id": [
      "12345"
    ]
  },
  "origin": "127.0.0.1",
  "url": "http://localhost:8083/Post",
  "args": {}
}
```

Otherwise, the body will be interpreted as raw data.

```powershell
$uri = Get-WebListenerUrl -Test 'Post'
$Body = @{id = 12345} | ConvertTo-Json -Compress
Invoke-RestMethod -Uri $uri -Body $body -Method 'Post' -ContentType 'application/json'
```

```json
{
  "method": "POST",
  "headers": {
    "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Microsoft Windows 10.0.15063; en-US) PowerShell/6.0.0",
    "Connection": "Keep-Alive",
    "Host": "localhost:8083",
    "Content-Type": "application/json",
    "Content-Length": "12"
  },
  "origin": "127.0.0.1",
  "url": "http://localhost:8083/Post",
  "args": {},
  "data": "{\"id\":12345}"
}
```

### /Put/

Returns the same results as the Get test. Will only accept the `PUT` request method.

```powershell
$uri = Get-WebListenerUrl -Test 'Put'
$Body = @{id = 12345} | ConvertTo-Json -Compress
Invoke-RestMethod -Uri $uri -Body $body -Method 'Put'
```

```json
{
  "method": "PUT",
  "headers": {
    "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Microsoft Windows 10.0.15063; en-US) PowerShell/6.0.0",
    "Connection": "Keep-Alive",
    "Host": "localhost:8083",
    "Content-Length": "12"
  },
  "origin": "127.0.0.1",
  "url": "http://localhost:8083/Put",
  "args": {},
  "data": "{\"id\":12345}"
}
```

### /Redirect/

Will `302` redirect to `/Get/`. If a number is supplied, redirect will occur that many times. Can be used to test maximum redirects.
If the `type` query field is supplied the corresponding `System.Net.HttpStatusCode` will be returned instead of `302`.
If `type` is `relative`, the redirect URI will be relative instead of absolute.

```powershell
$uri = Get-WebListenerUrl -Test 'Redirect' -TestValue '2'
Invoke-RestMethod -Uri $uri
```

Request 1:

```none
GET http://localhost:8083/Redirect/2 HTTP/1.1
Connection: Keep-Alive
User-Agent: Mozilla/5.0 (Windows NT; Microsoft Windows 10.0.15063 ; en-US) WindowsPowerShell/6.0.0
Host: localhost:8083
```

Response 1:

```none
HTTP/1.1 302 Found
Date: Fri, 15 Sep 2017 10:46:41 GMT
Content-Type: text/html; charset=utf-8
Server: Kestrel
Transfer-Encoding: chunked
Location: /Redirect/1

<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 3.2 Final//EN">
<title>Redirecting...</title>
<h1>Redirecting...</h1>
<p>You should be redirected automatically to target URL: <a href="/Redirect/1">/Redirect/1</a>.  If not click the link.
```

Request 2:

```none
GET http://localhost:8083/Redirect/1 HTTP/1.1
Connection: Keep-Alive
User-Agent: Mozilla/5.0 (Windows NT; Microsoft Windows 10.0.15063 ; en-US) WindowsPowerShell/6.0.0
Host: localhost:8083
```

Response 2:

```none
HTTP/1.1 302 Found
Date: Fri, 15 Sep 2017 10:46:41 GMT
Content-Type: text/html; charset=utf-8
Server: Kestrel
Transfer-Encoding: chunked
Location: /Get/

<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 3.2 Final//EN">
<title>Redirecting...</title>
<h1>Redirecting...</h1>
<p>You should be redirected automatically to target URL: <a href="/Get/">/Get/</a>.  If not click the link.
```

### /Response/

Will return a response crafted from the query string. The following four fields are supported:

* `body` - a string containing the response body
* `statuscode` - the HTTP Status Code to return
* `contenttype` - The `Content-Type` response header
* `headers` - a JSON string containing response headers. `Content-Type` will be ignored in `headers`. Use `contenttype` instead.
* `responsephrase` - the HTTP response phrase to return

```powershell
$Query = @{
    statsucode = 200
    responsephrase = 'OK'
    contenttype = 'application/json'
    body = '{"key1": "value1"}'
    headers = @{
        "X-Header" = "Response header value"
    } | ConvertTo-Json
}
$Uri =  Get-WebListenerUrl -Test 'Response' -Query $Query
Invoke-RestMethod -Uri $uri
```

Response headers:

```none
Content-Type: application/json
X-Header: Response header value
```

Response Body:

```json
{"key1": "value1"}
```

### /ResponseHeaders/

Will return the response headers passed in query string. The response body will be the supplied headers as a JSON object.

```powershell
$uri = Get-WebListenerUrl -Test 'ResponseHeaders' -Query @{'Content-Type' = 'custom'; 'x-header-01' = 'value01'; 'x-header-02' = 'value02'}
Invoke-RestMethod -Uri $uri
```

Response Headers:

```none
HTTP/1.1 200 OK
Date: Sun, 08 Oct 2017 18:20:38 GMT
Transfer-Encoding: chunked
Server: Kestrel
x-header-02: value02
x-header-01: value01
Content-Type: custom
```

Body:

```json
{
    "Content-Type": "custom",
    "x-header-02": "value02",
    "x-header-01": "value01"
}
```

### /Resume/

This endpoint simulates the download of a 20 byte file with support for resuming with the use of the `Range` HTTP request header.
The bytes returned are numbered 1 to 20 inclusive.
If the `Range` header is greater than 20, the endpoint will return a `416 Requested Range Not Satisfiable` response.
The endpoint also returns an `X-WebListener-Has-Range` response header containing `true` or `false` if the HTTP Request contains a `Range` request header.
The endpoint will also return an `X-WebListener-Request-Range` response header which contains the `Range` header value if one was present.

```powershell
$uri = Get-WebListenerUrl -Test 'Resume'
$response = Invoke-WebRequest -Uri $uri -Headers @{"Range" = "bytes=0-"}
```

Response Headers:

```none
HTTP/1.1 206 PartialContent
Date: Tue, 20 Mar 2018 08:45:42 GMT
Server: Kestrel
X-WebListener-Has-Range: true
X-WebListener-Request-Range: bytes=0-
Content-Length: 20
Content-Type: application/octet-stream
Content-Range: bytes 0-19/20
```

### /Resume/Bytes/{NumberBytes}

This endpoint emulates a partial download of the same 20 bytes provided by the `/Resume/` endpoint.
The endpoint will return `{NumberBytes}` bytes of the 20 bytes.
For example `/Resume/Bytes/5` will return bytes 1 through 5 inclusive of the 20 byte file.

```powershell
$uri = Get-WebListenerUrl -Test 'Resume' -TestValue 'Bytes/5'
$response = Invoke-WebRequest -Uri $uri
```

Response Headers:

```none
HTTP/1.1 200 OK
Date: Tue, 20 Mar 2018 08:50:57 GMT
Server: Kestrel
Content-Length: 5
Content-Type: application/octet-stream
```

### /Resume/NoResume

This endpoint is the same as `/Resume/` with the exception that it ignores the `Range` HTTP request header.
This endpoint always returns the full 20 bytes and a `200` status.
The `X-WebListener-Has-Range` and `X-WebListener-Request-Range` headers are also returned the same as the `/Resume/` endpoint.

```powershell
$uri = Get-WebListenerUrl -Test 'Resume' -TestValue 'NoResume'
$response = Invoke-WebRequest -Uri $uri
```

Response Headers:

```none
HTTP/1.1 200 OK
Date: Tue, 20 Mar 2018 08:48:21 GMT
Server: Kestrel
X-WebListener-Has-Range: false
Content-Length: 20
Content-Type: application/octet-stream
```

### /Retry/{sessionId}/{failureCode}/{failureCount}

This endpoint causes the failure specified by `failureCode` for `failureCount` number of times.
After that a status 200 is returned with body containing the number of times the failure was caused.

```powershell
$response = Invoke-WebRequest -Uri 'http://127.0.0.1:8083/Retry?failureCode=599&failureCount=2&sessionid=100&' -MaximumRetryCount 2 -RetryIntervalSec 1
```

Response Body:

```json
{
  "failureResponsesSent":2,
  "sessionId":100
}
```
