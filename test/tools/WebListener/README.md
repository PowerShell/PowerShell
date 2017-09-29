# WebListener App

ASP.NET Core 2.0 app for testing HTTP and HTTPS Requests.

# Run with `dotnet`

```
dotnet restore
dotnet publish --output bin --configuration Release
cd bin
dotnet WebListener.dll ServerCert.pfx password 8083 8084
```

The test site can then be accessed via `http://localhost:8083/` or `https://localhost:8084/`.  

The `WebListener.dll` takes 4 arguments: 

* The path to the Server Certificate
* The Server Certificate Password
* The TCP Port to bind on for HTTP
* The TCP Port to bind on for HTTPS

# Run With WebListener Module

```powershell
Import-Module .\build.psm1
Publish-PSTestTools
$Listener = Start-WebListener -HttpPort 8083 -HttpsPort 8084
```

# Tests

## / or /Home/

Returns a static HTML page containing links and descriptions of the available tests in WebListener. This can be used as a default or general test where no specific test functionality or return data is required.

## /Cert/

Returns a JSON object containing the details of the Client Certificate if one is provided in the request.

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

## /Compression/Deflate/
Returns the same results as the Get test with deflate compression.

```powershell
$uri = Get-WebListenerUrl -Test 'Compression' -TestValue 'Deflate'
Invoke-RestMethod -Uri $uri -Headers $headers
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

## /Compression/Gzip/
Returns the same results as the Get test with gzip compression.

```powershell
$uri = Get-WebListenerUrl -Test 'Compression' -TestValue 'Gzip'
Invoke-RestMethod -Uri $uri -Headers $headers
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

## /Delay/

Returns the same results as the Get test. If a number is supplied, the server will wait that many seconds before returning a response. This can be used to test timeouts.

```powershell
Invoke-WebRequest -Uri 'http://localhost:8083/Delay/5'
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

## /Encoding/Utf8/

Returns page containing UTF-8 data.


## /Get/

Returns a JSON object containing the Request URL, Request Headers, GET Query Fields and Values, and Origin IP. This emulates the functionality of [HttpBin's get test](https://httpbin.org/get).

```powershell
Invoke-WebRequest -Uri 'http://localhost:8083/Get/' -Body @{TestField = 'TestValue'}
```

```json
{
  "url": "http://localhost:8083/Get/?TestField=TestValue",
  "args": {
    "TestField": "TestValue"
  },
  "headers": {
    "Connection": "Keep-Alive",
    "User-Agent": "Mozilla/5.0 (Windows NT; Microsoft Windows 10.0.15063 ; en-US) WindowsPowerShell/6.0.0",
    "Host": "localhost:8083"
  },
  "origin": "127.0.0.1"
}
```

## /Multipart/

### GET 
Provides an HTML form for `multipart/form-data` submission.

### POST
Accepts a `multipart/form-data` submission and returns a JSON object containing information about the submission including the items and files submitted.

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

## /Redirect/

Will 302 redirect to `/Get/`. If a number is supplied, redirect will occur that many times. Can be used to test maximum redirects.

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
