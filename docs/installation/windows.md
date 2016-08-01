Package installation instructions
=================================

###MSI:
To install PowerShell on Windows Full SKU (works on Win7 SP1 and above - x64 based systems), download either the MSI from [AppVeyor][] for a nightly build, 
or a released package from our GitHub [releases][] page. The MSI file looks like this - `PowerShell_6.0.0.buildversion.msi`

Once downloaded, double-click the installer and follow the prompts.

There is a shortcut placed in the Start Menu upon installation.

> By default the package is installed to `$env:ProgramFiles\PowerShell\`

###APPX/WSA:
Additionally, we provide APPX/WSA packages (that are compatible with Nano Server and works on Win10 x64 and above systems), 
but these must be self-signed prior to install. 
See the [`Sign-Package.ps1`][signing] script for details.

When using APPX/WSA, the OS needs to be configured to allow [sideloading apps](https://technet.microsoft.com/en-us/windows/jj874388.aspx)

Artifact installation instructions
==================================

We publish an archive with CoreCLR and FullCLR bits on every CI build with [AppVeyor][].

[releases]: https://github.com/PowerShell/PowerShell/releases
[signing]: ../../tools/Sign-Package.ps1
[AppVeyor]: https://ci.appveyor.com/project/PowerShell/powershell

CoreCLR artifacts
=================

* Download zip package from **artifacts** tab of the particular build.
* Unblock zip file: right-click in File Explorer -> Properties ->
  check 'Unblock' box -> apply
* Extract zip file to `bin` directory
* `./bin/powershell.exe`
