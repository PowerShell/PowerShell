# Known Issues

## `Computer.cs`

The file `monad/src/commands/management/Computer.cs` has been removed
temporarily from `Microsoft.PowerShell.Commands.Management` because we
cannot resolve `Microsoft.WSMan.Management` for FullCLR builds. This must be
fixed ASAP.

## `ImplicitRemotingCommands.cs`

The file `monad\src\commands\utility\ImplicitRemotingCommands.cs` has been
removed temporarily from `Microsoft.PowerShell.Commands.Utility` because it
does not build on Core PowerShell, and cannot be guarded with preprocessor
directives due to the use of here-strings that contain lines starting with `#`,
which breaks the preprocessor.

## `WebCmdlet`

All files in `monad\src\commands\utility\WebCmdlet` have been removed from
`Microsoft.PowerShell.Commands.Utility` because we cannot resolve `mshtml.dll`
as a framework assembly.

## `Microsoft.Management.Infrastructure.Native`

Windows builds currently use the native stub; this should be replaced with
actual compilation of the managed C++ library on Windows (with the stub used on
Linux).

## CorePS Eventing Library

The Eventing library reimplementation for Core PowerShell does not exist on
Linux, and so the ETW stub is used via a `#if LINUX` guard. On Windows, this
library now exists, but its build needs to be ported to .NET CLI. Until then,
the stub is also used with a `#if ETW` guard.

## xUnit

The xUnit tests cannot currently be run; we are working to integrate the
prototype .NET Core runner to re-enable them.

## Console Output

The console output on Windows and under certain `TERM` environments on Linux
(`xterm` is known to work fine), the console scrolls badly. We believe this is
due to incomplete System.Console APIs, which have been fixed upstream and will
be updated when new packages drop.

Performance issues have been seen in some scenarios, such as nested SSH
sessions. We believe this is likely an issue with `Console.ReadKey()` and are
investigating.

## Remoting

Only basic authentication is implemented

Multiple sessions are not yet supported

Server shut-down is not complete (must restart `omiserver` after a session is
completed.

## Unavailable cmdlets

This project includes the CoreCLR versions of the `Commands.Management`,
`Commands.Utility`, `Security`, and `PSDiagnostics` modules.

The `Archive`, `Diagnostics`, `PSGet`, and `Host` modules are not yet included.

The `WSMan.Management` module cannot be included unless the
`Management.Infrastructure.Native` library is ported.

The CoreCLR version of the `Commands.Utility` module does not contain the
following cmdlets that exist in the FullCLR version:

- ConvertFrom-String
- ConvertTo-Html
- Export-PSSession
- Get-TraceSource
- Import-PSSession
- Invoke-RestMethod
- Invoke-WebRequest
- Out-GridView
- Out-Printer
- Send-MailMessage
- Set-TraceSource
- Show-Command
- Trace-Command
- Update-List
