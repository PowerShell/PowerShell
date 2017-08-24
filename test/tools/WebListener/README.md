# HTTPS Listener

ASP.NET Core 2.0 app for testing HTTP and HTTPS Requests.
The included SelF-Signed Certificate `ServerCert.pfx` has the password set to `password` and is issued for the Client and Server Authentication key usages. This certificate is used by the ASP.NET Kestrel server for SSL/TLS. The included SelF-Signed Certificate `ClientCert.pfx` has the password set to `password` and has not been issued for any specific key usage.

The default page will return a list of available tests.

# Run with `dotnet`

```
dotnet restore
dotnet publish --output bin --configuration Release
cd bin
dotnet WebListener.dll ServerCert.pfx password 8083 8084
```

The test site can then be accessed via `https://localhost:8084/`.  

The `WebListener.dll` takes 4 arguments: 

* The path to the Server Certificate
* The Server Certificate Password
* The TCP Port to bind on for HTTP
* The TCP Port to bind on for HTTPS


# Tests

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
