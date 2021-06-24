# install-powershell.ps1

## Features of install-powershell.ps1

* Can be called directly from git
* Optionally allows install of the latest Preview build
* Optionally allows install of the Daily build
* Optionally installs using the latest MSI
* Automatically looks up latest version via git tags
* Optionally installs silently
* Optionally adds the install location to Path environment variable

## Examples

### Install PowerShell Core Daily Build

```PowerShell
Invoke-Expression "& { $(Invoke-RestMethod 'https://aka.ms/install-powershell.ps1') } -daily"
```

### Install PowerShell Core using the MSI installer

```PowerShell
Invoke-Expression "& { $(Invoke-RestMethod 'https://aka.ms/install-powershell.ps1') } -UseMSI"
```
