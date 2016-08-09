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

The xUnit tests are disabled pending implementation of a new runner.

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

### ExecutionPolicy unavailable on non-Windows platforms

ExecutionPolicy is not implemented on non-Windows platforms.
`Get-ExecutionPolicy` will always return `Unrestricted` which is the correct operating mode.
`Set-ExecutionPolicy` will throw `PlatformNotSupported`.

```
Set-ExecutionPolicy : Operation is not supported on this platform.
At line:1 char:1
+ Set-ExecutionPolicy AllSigned
+ ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    + CategoryInfo          : NotSpecified: (:) [Set-ExecutionPolicy], PlatformNotSupportedException
    + FullyQualifiedErrorId : System.PlatformNotSupportedException,Microsoft.PowerShell.Commands.SetExecutionPolicyCommand

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
