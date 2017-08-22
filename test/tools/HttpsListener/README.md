# HTTPS Listener

ASP.NET Core 2.0 app for testing HTTPS Requests.
The included SelF-Signed Certificate `ServerCert.pfx` has the password set to `password` and is issued for the Client and Server Authentication key usages. This certificate is used by the ASP.NET Kestrel server for SSL/TLS. The included SelF-Signed Certificate `ClientCert.pfx` has the password set to `password` and has not been issued for any specific key usage. The app can be run directly with `dotnet` or as a Docker container.

The default page will return information about the certificate if one was provided.

Response when certificate is provided in request:
```
Status: OK
Thumbprint: 2DECF1348FF21B780F45D316A039B5EB4C6312F7
Subject: E=randd@adatum.com, CN=adatum.com, OU=R&amp;D, O=A. Datum Corporation, L=Redmond, S=Washington, C=US
Subject Name: E=randd@adatum.com, CN=adatum.com, OU=R&amp;D, O=A. Datum Corporation, L=Redmond, S=Washington, C=US
Issuer: E=randd@adatum.com, CN=adatum.com, OU=R&amp;D, O=A. Datum Corporation, L=Redmond, S=Washington, C=US
Issuer Name: E=randd@adatum.com, CN=adatum.com, OU=R&amp;D, O=A. Datum Corporation, L=Redmond, S=Washington, C=US
Not After: 12/26/2044 18:16:46
Not Before: 08/10/2017 18:16:46
```

Response when certificate is not provided in request:
```
Status: FAILED
Thumbprint:
Subject:
Subject Name:
Issuer:
Issuer Name:
Not After:
Not Before:
```

# Run with `dotnet`

```
dotnet restore
dotnet publish --output bin --configuration Release
cd bin
dotnet HttpsListener.dll ServerCert.pfx password 8443
```

The test site can then be accessed via `https://localhost:8443/`.  

The `HttpsListener.dll` takes 3 arguments: the path to the Server Certificate, the Server Certificate Password, and the TCP Port to bind on.