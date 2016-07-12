# Known Issues

## Files excluded from the build

#### Microsoft.PowerShell.Commands.Management

- The file `ControlPanelItemCommand.cs` is excluded from all frameworks in `Microsoft.PowerShell.Commands.Management` 
because it has dependency on `[Shell32.ShellFolderItem]` for FullCLR builds.

#### Microsoft.PowerShell.GraphicalHost

```
"ManagementList/CommonControls/ExpanderButtonAutomationPeer.cs",
"ManagementList/CommonControls/ExpanderButton.cs",
"ManagementList/CommonControls/ExpanderButton.Generated.cs",
"ManagementList/Common/PopupControlButton.cs",
"ManagementList/Common/PopupControlButton.Generated.cs"
```

Excluded because they requires `UIAutomationTypes.dll`

#### Microsoft.PowerShell.ConsoleHost

These are excluded from all builds with `#if !PORTABLE`.
They require .NET types that are currently missing.

```
singleshell/installer/EngineInstaller.cs
singleshell/installer/MshHostMshSnapin.cs
```

## Jobs

The PowerShell jobs fail, see [#1010][].

[#1010]: https://github.com/PowerShell/PowerShell/issues/1010

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

On Linux, PowerShell sessions do not work because of remoting requirements, so
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
- Import-PSSession
- Invoke-RestMethod
- Invoke-WebRequest
- Out-GridView
- Out-Printer
- Send-MailMessage
- Show-Command
- Update-List

### ExecutionPolicy unavailable on non-Windows platform

ExecutionPolicy is not implemented on non-Windows platforms and the following related CmdLets will return the error below.
 
- Get-ExecutionPolicy
- Set-ExecutionPolicy

```
Set-ExecutionPolicy : Operation is not supported on this platform.
At line:1 char:1
+ Set-ExecutionPolicy AllSigned
+ ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    + CategoryInfo          : NotSpecified: (:) [Set-ExecutionPolicy], Platfor
   mNotSupportedException
    + FullyQualifiedErrorId : System.PlatformNotSupportedException,Microsoft.P
   owerShell.Commands.SetExecutionPolicyCommand

```

## File paths with literal backward slashes

On some filesystems (Linux, OS X), file paths are allowed to contain literal
backward slashes, '\', as valid filename characters. These slashes, when
escaped, are not directory separators. In Bash, the backward slash is the escape
character, so a `path/with/a\\slash` is two directories, `path` and `with`, and
one file, `a\slash`. In PowerShell, we *will* support this using the normal
backtick escape character, so a `path\with\a``\slash` or a
`path/with/a``\slash`, but this edge case is *currently unsupported*.

That being said, native commands will work as expected. Thus this is the current
scenario:

```powershell
PS > Get-Content a`\slash
Get-Content : Cannot find path '/home/andrew/src/PowerShell/a/slash' because it does not exist.
At line:1 char:1
+ Get-Content a`\slash
+ ~~~~~~~~~~~~~~~~~~~~
    + CategoryInfo          : ObjectNotFound: (/home/andrew/src/PowerShell/a/slash:String) [Get-Co
   ntent], ItemNotFoundException
    + FullyQualifiedErrorId : PathNotFound,Microsoft.PowerShell.Commands.GetContentCommand

PS > /bin/cat a\slash
hi

```

The PowerShell cmdlet `Get-Content` cannot yet understand the escaped backward
slash, but the path is passed literally to the native command `/bin/cat`. Most
file operations are thus implicitly supported by the native commands. The
notable exception is `cd` since it is not a command, but a shell built-in,
`Set-Location`. So until this issue is resolved, PowerShell cannot change to a
directory whose name contains a literal backward slash.
