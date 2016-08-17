Package Installation Instructions
=================================

# MSI:
To install PowerShell on Windows Full SKU (works on Win8 and above - x64 based systems), download either the MSI from [AppVeyor][] for a nightly build, 
or a released package from our GitHub [releases][] page. The MSI file looks like this - `PowerShell_6.0.0.buildversion.msi`

Once downloaded, double-click the installer and follow the prompts.

There is a shortcut placed in the Start Menu upon installation.

> By default the package is installed to `$env:ProgramFiles\PowerShell\`
>
> You can launch PowerShell via the Start Menu or `$env:ProgramFiles\PowerShell\powershell.exe`

# Instructions to Create a Remoting Endpoint

Beginning with 6.0.0-alpha.9, the PowerShell package for Windows includes a WinRM plugin (pwrshplugin.dll) and an installation script (Install-PowerShellRemoting.ps1). 
These files enable PowerShell to accept incoming PowerShell remote connections when its endpoint is specified. 

## Motivation

An installation of PowerShell can establish PowerShell sessions to remote computers using ```New-PSSession``` and ```Enter-PSSession```. 
To enable it to accept incoming PowerShell remote connections, the user must create a WinRM remoting endpoint. 
This is an explicit opt-in scenario where the user runs Install-PowerShellRemoting.ps1 to create the WinRM endpoint. 
The installation script is a short-term solution until we add additional functionality to ```Enable-PSRemoting``` to perform the same action. 
For more details, please see issue [#1193](https://github.com/PowerShell/PowerShell/issues/1193).

## Script Actions

The script

1. Creates a directory for the plugin within %windir%\System32\PowerShell
2. Copies pwrshplugin.dll to that location
3. Generates a configuration file
4. Registers that plugin with WinRM

## Registration
The script must be executed within an Administrator-level PowerShell session and runs in two modes.

* Executed by the instance of PowerShell that it will register
``` powershell
Install-PowerShellRemoting.ps1
``` 
* Executed by another instance of PowerShell on behalf of the instance that it will register.
``` powershell
<path to powershell>\Install-PowerShellRemoting.ps1 -PowerShellHome "<absolute path to the instance's $PSHOME>" -PowerShellVersion "<the powershell version tag>"
```
For Example:
``` powershell
C:\Program Files\PowerShell\6.0.0.9\Install-PowerShellRemoting.ps1 -PowerShellHome "C:\Program Files\PowerShell\6.0.0.9\" -PowerShellVersion "6.0.0-alpha.9" 
```

## How to Connect to the New Endpoint

Create a PowerShell session to the new PowerShell endpoint by specifying `-ConfigurationName "some endpoint name"`. To connect to the PowerShell instance from the example above, use either:
``` powershell
New-PSSession ... -ConfigurationName "powershell.6.0.0-alpha.9"
Enter-PSSession ... -ConfigurationName "powershell.6.0.0-alpha.9"
``` 
Note that `New-PSSession` and `Enter-PSSession` invocations that do not specify `-ConfigurationName` will target the default PowerShell endpoint, `microsoft.powershell`.

Artifact Installation Instructions
==================================

We publish an archive with CoreCLR and FullCLR bits on every CI build with [AppVeyor][].

[releases]: https://github.com/PowerShell/PowerShell/releases
[signing]: ../../tools/Sign-Package.ps1
[AppVeyor]: https://ci.appveyor.com/project/PowerShell/powershell

CoreCLR Artifacts
=================

* Download zip package from **artifacts** tab of the particular build.
* Unblock zip file: right-click in File Explorer -> Properties ->
  check 'Unblock' box -> apply
* Extract zip file to `bin` directory
* `./bin/powershell.exe`
