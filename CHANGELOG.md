Changelog
=========

v6.0.0-alpha.15 - 2016-01-18
----------------------------
- Use parens around file length for offline files
- Fix issues with the Windows console mode (terminal emulation) and native executables
- Fix error recovery with `using module`
- Report `PlatformNotSupported` on IoT for Get/Import/Export-Counter
- Add `-Group` parameter to `Get-Verb`
- Use MB instead of KB for memory columns of `Get-Process`
- Add new escape character for ESC: `` `e``
- Fix a small parsing issue with a here string
- Improve tab completion of types that use type accelerators
- `Invoke-RestMethod` improvements for non-XML non-JSON input
- PSRP remoting now works on CentOS without addition setup

v6.0.0-alpha.14 - 2016-12-14
----------------------------
- Moved to .NET Core 1.1
- Add Windows performance counter cmdlets to PowerShell Core
- Fix try/catch to choose the more specific exception handler
- Fix issue reloading modules that define PowerShell classes
- `Add ValidateNotNullOrEmpty` to approximately 15 parameters
- `New-TemporaryFile` and `New-Guid` rewritten in C#
- Enable client side PSRP on non-Windows platforms
- `Split-Path` now works with UNC roots
- Implicitly convert value assigned to XML property to string
- Updates to `Invoke-Command` parameters when using SSH remoting transport
- Fix `Invoke-WebRequest` with non-text responses on non-Windows platforms
- `Write-Progress` performance improvement from `alpha13` reverted because it introduced crash with a race condition

v6.0.0-alpha.13 - 2016-11-22
----------------------------
- Fix `NullReferenceException` in binder after turning on constrained language mode
- Enable `Invoke-WebRequest` and `Invoke-RestMethod` to not validate the HTTPS certificate of the server if required.
- Enable binder debug logging in PowerShell Core
- Add parameters `-Top` and `-Bottom` to `Sort-Object` for Top/Bottom N sort
- Enable `Update-Help` and `Save-Help` on Unix platforms
- Update the formatter for `System.Diagnostics.Process` to not show the `Handles` column
- Improve `Write-Progress` performance by adding timer to update a progress pane every 100 ms
- Enable correct table width calculations with ANSI escape sequences on Unix
- Fix background jobs for Unix and Windows
- Add `Get-Uptime` to `Microsoft.PowerShell.Utility`
- Make `Out-Null` as fast as `> $null`
- Add Dockerfile for windowsservercore and NanoServer
- Fix WebRequest failure to handle missing ContentType in response header
- Make `Write-Host` fast by delay initializing some properties in InformationRecord
- Ensure PowerShell Core adds an initial `/` rooted drive on Unix platforms
- Enable streaming behavior for native command execution in pipeline, so that `ping | grep` doesn't block
- Make `Write-Information` accept objects from pipeline
- Fixes deprecated syscall issue on macOS 10.12
- Fix code errors found by the static analysis using PVS-Studio
- Add support to W3C Extended Log File Format in `Import-Csv`
- Guard against `ReflectionTypeLoadException` in type name auto-completion
- Update build scripts to support win7-x86 runtime
- Move PackageManagement code/test to oneget.org

v6.0.0-alpha.12 - 2016-11-03
----------------------------
- Fix `Get-ChildItem -Recurse -ErrorAction Ignore` to ignore additional errors
- Don't block pipeline when running Windows exes
- Fix for PowerShell SSH remoting with recent Win32-OpenSSH change.
- `Select-Object` with `-ExcludeProperty` now implies `-Property *` if -Property is not specified.
- Adding ValidateNotNullOrEmpty to `-Name` parameter of `Get-Alias`
- Enable Implicit remoting commands in PowerShell Core
- Fix GetParentProcess() to replace an expensive WMI query with Win32 API calls
- Fix `Set-Content` failure to create a file in PSDrive under certain conditions.
- Adding ValidateNotNullOrEmpty to `-Name` parameter of `Get-Service`
- Adding support <Suppress> in `Get-WinEvent -FilterHashtable`
- Adding WindowsVersion to `Get-ComputerInfo`
- Remove the unnecessary use of lock in PseudoParameterBinder to avoid deadlock
- Refactor `Get-WinEvent` to use StringBuilder for XPath query construction
- Clean up and fix error handling of libpsl-native
- Exclude Registry and Certificate providers from UNIX PS
- Update PowerShell Core to consume .Net Core preview1-24530-04

v6.0.0-alpha.11 - 2016-10-17
----------------------------
- Add '-Title' to 'Get-Credential' and unify the prompt experience
- Update dependency list for PowerShell Core on Linux and OS X
- Fix 'powershell -Command -' to not hang and to not ignore the last command
- Fix binary operator tab completion
- Enable 'ConvertTo-Html' in PowerShell Core
- Remove most Maximum* capacity variables
- Fix 'Get-ChildItem -Hidden' to work on system hidden files on Windows
- Fix 'JsonConfigFileAccessor' to handle corrupted 'PowerShellProperties.json'
and defer creating the user setting directory until a write request comes
- Fix variable assignment to not overwrite readonly variables
- Fix 'Get-WinEvent -FilterHashtable' to work with named fields in UserData of event logs
- Fix 'Get-Help -Online' in PowerShell Core on Windows
- Spelling/grammar fixes

v6.0.0-alpha.10 - 2016-09-15
----------------------------
- Fix passing escaped double quoted spaces to native executables
- Add Dockerfiles to build each Linux distribution
- `~/.config/PowerShell` capitalization bug fixed
- Fix crash on Windows 7
- Fix remote debugging on Windows client
- Fix multi-line input with redirected stdin
- Add PowerShell to `/etc/shells` on installation
- Fix `Install-Module` version comparison bug
- Spelling fixes

v6.0.0-alpha.9 - 2016-08-15
---------------------------

- Better man page
- Added third-party and proprietary licenses
- Added license to MSI

v6.0.0-alpha.8 - 2016-08-11
---------------------------

- PowerShell packages pre-compiled with CrossGen
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
- FileCatalog cmdlets added back for Windows
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
