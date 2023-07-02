# install-powershell.ps1

## Features of install-powershell.ps1

* Can be called directly from git
* Optionally allows install of the latest Preview build
* Optionally allows install of the Daily build
* Optionally installs using the latest MSI
* Automatically looks up latest version via git tags
* Optionally installs silently
* Optionally adds the install location to Path environment variable
* Optionally allows for installation of specified version
* Optionally installs the LTS version

## Examples

### Install PowerShell Daily Build

```PowerShell
Invoke-Expression "& { $(Invoke-RestMethod 'https://aka.ms/install-powershell.ps1') } -daily"
```

### Install PowerShell using the MSI installer

```PowerShell
Invoke-Expression "& { $(Invoke-RestMethod 'https://aka.ms/install-powershell.ps1') } -UseMSI"
```

### Install PowerShell version 7.3.1

```PowerShell
Invoke-Expression "& { $(Invoke-RestMethod 'https://aka.ms/install-powershell.ps1') } -Destination 'C:\ps' -Version '7.3.1'"
```
### Install Current LTS version of PowerShell (7.2.12)

```PowerShell
Invoke-Expression "& { $(Invoke-RestMethod 'https://aka.ms/install-powershell.ps1') } -Destination 'C:\ps' -LTS"
```
