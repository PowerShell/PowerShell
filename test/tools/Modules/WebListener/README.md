# WebListener Module

A PowerShell module for managing the WebListener App.

When the WebListener is started from this module,
it will automatically generate two fresh certificates,
`ClientCert.pfx` and `ServerCert.pfx` using the `SelfSignedCertificate` module.

The generated Self-Signed Certificate `ServerCert.pfx` has a randomly generated password
and is issued for the Client and Server Authentication key usages.
This certificate is used by the WebListener App for SSL/TLS.

The generated Self-Signed Certificate `ClientCert.pfx` has a randomly generated password
and is not issued for any specific key usage.
This Certificate is used for Client Certificate Authentication with the WebListener App.
The port used for `-HttpsPort` will use TLS 1.2.

## Running WebListener

```powershell
Import-Module .\build.psm1
Publish-PSTestTools
$Listener = Start-WebListener -HttpPort 8083 -HttpsPort 8084 -Tls11Port 8085 -TlsPort 8086
```

## Stopping WebListener

```powershell
Stop-WebListener
```
