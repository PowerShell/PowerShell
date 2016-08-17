Changelog
=========

Unreleased
----------

v6.0.0-alpha.9 - 2016-08-15
---------------------------

- Better man page
- Added third-party and proprietary licenses
- Added license to MSI

v6.0.0-alpha.8 - 2016-08-11
---------------------------

- PowerSell packages pre-compiled with CrossGen
- `Get-Help` content added
- `Get-Help` null reference exception fixed
- Ubuntu 16.04 support added
- Unsupported cmdlets removed from Unix modules
- PSReadLine long prompt bug fixed
- PSReadLine custom key binding bug on Linux fixed
- Default terminal colors now respected
- Semantic Version support added
- `$env:` fixed for case-sensitive variables
- Added JSON config files to hold some settings
- `cd` with no arguments now behaves as `cd ~`
- `ConvertFrom-Json` fixed for multiple lines
- Windows branding removed
- .NET CoreCLR Runtime patched to version 1.0.4
- `Write-Host` with unknown hostname bug fixed
- `powershell` man-page added to package
- `Get-PSDrive` ported to report free space
- Desired State Configuration MOF compilation ported to Linux
- Windows 2012 R2 / Windows 8.1 remoting enabled

v6.0.0-alpha.7 - 2016-07-26
---------------------------

- Invoke-WebRequest and Invoke-RestMethod ported to PowerShell Core
- Set PSReadLine default edit mode to Emacs on Linux
- IsCore variable renamed to IsCoreCLR
- LocalAccounts and other Windows-only assemblies excluded on Linux
- PowerShellGet fully ported to Linux
- PackageManagement NuGet provider ported
- Write-Progress ported to Linux
- Get-Process -IncludeUserName ported
- Enumerating symlinks to folders fixed
- Bugs around administrator permissions fixed on Linux
- ConvertFrom-Json multi-line bug fixed
- Execution policies fixed on Windows
- TimeZone cmdlets added back; excluded from Linux
- FileCatalog cmdlets add back for Windows
- Get-ComputerInfo cmdlet added back for Windows

v0.6.0 - 2016-07-08
-------------------

- Targets .NET Core 1.0 release
- PowerShellGet enabled
- [system.manage<tab>] completion issues fixed
- AssemblyLoadContext intercepts dependencies correctly
- Type catalog issues fixed
- Invoke-Item enabled for Linux and OS X
- Windows ConsoleHost reverted to native interfaces
- Portable ConsoleHost redirection issues fixed
- Bugs with pseudo (and no) TTYs fixed
- Source Depot synced to baseline changeset 717473
- SecureString stub replaced with .NET Core package

v0.5.0 - 2016-06-16
-------------------

- Paths given to cmdlets are now slash-agnostic (both / and \ work as directory separator)
- Lack of cmdlet support for paths with literal \ is a known issue
- .NET Core packages downgraded to build rc2-24027 (Nano's build)
- XDG Base Directory Specification is now respected and used by default
- Linux and OS X profile path is now `~/.config/powershell/profile.ps1`
- Linux and OS X history save path is now `~/.local/share/powershell/PSReadLine/ConsoleHost_history.txt`
- Linux and OS X user module path is now `~/.local/share/powershell/Modules`
- The `~/.powershell` folder is deprecated and should be deleted
- Scripts can be called within PowerShell without the `.ps1` extension
- `Trace-Command` and associated source cmdlets are now available
- `Ctrl-C` now breaks running cmdlets correctly
- Source Depot changesets up to 715912 have been merged
- `Set-PSBreakPoint` debugging works on Linux, but not on Windows
- MSI and APPX packages for Windows are now available
- Microsoft.PowerShell.LocalAccounts is available on Windows
- Microsoft.PowerShell.Archive is available on Windows
- Linux xUnit tests are running again
- Many more Pester tests are running

v0.4.0 - 2016-05-17
-------------------

- PSReadLine is ported and included by default
- Original Windows ConsoleHost is ported and replaced CoreConsoleHost
- .NET Core packages set to the RC2 release at build 24103
- OS X 10.11 added to Continuous Integration matrix
- Third-party C# cmdlets can be built with .NET CLI
- Improved symlink support on Linux
- Microsoft.Management.Infrastructure.Native replaced with package
- Many more Pester tests

v0.3.0 - 2016-04-11
-------------------

- Supports Windows, Nano, OS X, Ubuntu 14.04, and CentOS 7.1
- .NET Core packages are build rc3-24011
- Native Linux commands are not shadowed by aliases
- `Get-Help -Online` works
- `more` function respects the Linux `$PAGER`; defaults to `less`
- `IsWindows`, `IsLinux`, `IsOSX`, `IsCore` built-in PowerShell variables added
- `Microsoft.PowerShell.Platform` removed for the above
- Cross-platform core host is now `CoreConsoleHost`
- Host now catches exceptions in `--command` scripts
- Host's shell ID changed to `Microsoft.PowerShellCore`
- Modules that use C# assemblies can be loaded
- `New-Item -ItemType SymbolicLink` supports arbitrary targets
- Readline implementation supports multi-line input
- `Ctrl-R` provides incremental reverse history search
- `$Host.UI.RawUI` now supported
- `Ctrl-K` and `Ctrl-Y` for kill and yank implemented
- `Ctrl-L` to clear screen now works
- Documentation was completely overhauled
- Many more Pester and xUnit tests added

v0.2.0 - 2016-03-08
-------------------

- Supports Windows, OS X, Ubuntu 14.04, and CentOS 7.1
- .NET Core packages are build 23907
- `System.Console` readline is fully functional
- Tests pass on OS X
- `Microsoft.PowerShell.Platform` module is available
- `New-Item` supports symbolic and hard links
- `Add-Type` now works
- PowerShell code merged with upstream `rs1_srv_ps`

v0.1.0 - 2016-02-23
-------------------

- Supports Windows, OS X, and Ubuntu 14.04
