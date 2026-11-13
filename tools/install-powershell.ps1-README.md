# install-powershell.ps1

## Features of install-powershell.ps1

- Can be called directly from Git
- Optionally allows install of the latest Preview build
- Installs the latest stable release by default
- Optionally installs the latest LTS 7.6.x release using MSI on Windows, which still supports MSI
- Automatically looks up release versions via the upstream `tools/metadata.json`
- Optionally installs silently
- Optionally adds the install location to Path environment variable

## Examples

### Install PowerShell Preview Build

```PowerShell
Invoke-Expression "& { $(Invoke-RestMethod 'https://aka.ms/install-powershell.ps1') } -Preview"
```

### Install PowerShell using the MSI installer

Without `-Preview`, selects the latest LTS 7.6.x release (`ReleaseTag`), which still supports MSI.
MSI packages are not available for PowerShell 7.7.0 or later, including preview builds.
Using `-Preview -UseMSI` fails if the selected preview does not support MSI; omit `-UseMSI`
to install that preview from an archive instead.

```PowerShell
Invoke-Expression "& { $(Invoke-RestMethod 'https://aka.ms/install-powershell.ps1') } -UseMSI"
```
