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
