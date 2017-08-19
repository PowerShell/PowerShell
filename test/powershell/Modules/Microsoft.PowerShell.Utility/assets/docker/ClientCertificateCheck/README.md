# ClientCertificateCheck

ASP.NET Core 2.0 Docker Container for testing Client Certificate Authentication.
The included SelF-Signed Certificate `ServerCert.pfx` has the password set to `passWord` and is issued for the Client and Server Authentication key usages. This certificate is used by the Asp.Net Kestrel server for SSL/TLS. The included SelF-Signed Certificate `ClientCert.pfx` has no password and has not been issued for any specific key usage.

The Kestrel server will bind on the port passed to the `docker run` command.

```
docker build -t clientcertificatecheck .
docker run -d -p 8443:8443 --name clientcertificatecheck clientcertificatecheck 8443
```

The test site can then be accessed via `https://localhost:8443/`. The default page will return information about the certificate is one was provided. 

Certificate Provided in request:
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

No certificate provided in request:
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