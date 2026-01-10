# install-powershell.ps1

## Features of install-powershell.ps1

- Can be called directly from Git
- Optionally allows install of the latest Preview build
- Optionally installs using the latest MSI
- Automatically looks up latest version via Git tags
- Optionally installs silently
- Optionally adds the install location to Path environment variable

## Examples

### Install PowerShell Preview Build

```PowerShell
Invoke-Expression "& { $(Invoke-RestMethod 'https://aka.ms/install-powershell.ps1') } -Preview"
```

### Install PowerShell using the MSI installer

```PowerShell
Invoke-Expression "& { $(Invoke-RestMethod 'https://aka.ms/install-powershell.ps1') } -UseMSI"
```
