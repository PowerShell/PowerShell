# Known Issues

## `ControlPanelItemCommand.cs`

The file `monad/src/commands/management/ControlPanelItemCommand.cs` has been removed
temporarily from `Microsoft.PowerShell.Commands.Management` because we
cannot resolve `[Shell32.ShellFolderItem]` for FullCLR builds. This must be
fixed ASAP.

## `GetComputerInfoCommand.cs`

The file
`src\Microsoft.PowerShell.Commands.Management\commands\management\GetComputerInfoCommand.cs`
is not currently compiled because it needs resources.

## xUnit

The xUnit tests can only be run on Linux.

## Console Output

Performance issues have been seen in some scenarios, such as nested SSH
sessions. We believe this is likely an issue with `Console.ReadKey()` and are
investigating.

## Non-interactive console bugs

The `ConsoleHost` is buggy when running under an environment without a proper
TTY. This is due to exceptions thrown in the `RawUI` class from `System.Console`
that are silenced in the formatting subsystem. See issue [#984][].

[#984]: https://github.com/PowerShell/PowerShell/issues/984

## Sessions

PowerShell sessions do not work because of remoting requirements, so
`New-PSSession` etc. crash.

## Aliases

The aliases that conflict with native Linux / OS X commands are removed. This is
an open discussion in issue [#929][]. See commit 7d9f43966 for their removal,
and 3582bb421 for the merge.

[#929]: https://github.com/PowerShell/PowerShell/issues/929

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
