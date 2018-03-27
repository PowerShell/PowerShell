# Known Issues

## Known Issues for PowerShell on Non-Windows Platforms

Alpha releases of PowerShell on Linux and macOS are mostly functional but do have some significant limitations and usability issues.
Beta releases of PowerShell on Linux and macOS are more functional and stable than alpha releases, but still may be lacking some set of features, and can contain bugs.
In some cases, these issues are simply bugs that haven't been fixed yet.
In other cases (as with the default aliases for ls, cp, etc.), we are looking for feedback from the community regarding the choices we make.

Note: Due to the similarities of many underlying subsystems, PowerShell on Linux and macOS tend to share the same level of maturity in both features and bugs.
Except as noted below, the issues in this section will apply to both operating systems.

### Case-sensitivity in PowerShell

Historically, PowerShell has been uniformly case-insensitive, with few exceptions.
On UNIX-like operating systems, the file system is predominantly case-sensitive and PowerShell adheres to the standard of the file system; this is exposed through a number of ways, obvious and non-obvious.

#### Directly

- When specifying a file in PowerShell, the correct case must be used.

#### Indirectly

- If a script tries to load a module and the module name is not cased correctly, then the module load will fail.
  This may cause a problem with existing scripts if the name by which the module is referenced doesn't match the actual file name.
- Tab-completion will not auto-complete if the file name case is wrong.
  The fragment to complete must be cased properly.
  (Completion is case-insensitive for type name and type member completions.)

### .PS1 File Extensions

PowerShell scripts must end in `.ps1` for the interpreter to understand how to load and run them in the current process.
Running scripts in the current process is the expected usual behavior for PowerShell.
The `#!` magic number may be added to a script that doesn't have a `.ps1` extension, but this will cause the script to be run in a new PowerShell instance preventing the script from working properly when interchanging objects.
(Note: this may be the desirable behavior when executing a PowerShell script from `bash` or another shell.)

### Missing command aliases

On Linux/macOS, the "convenience aliases" for the basic commands `ls`, `cp`, `mv`, `rm`, `cat`, `man`, `mount`, `ps` have been removed.
On Windows, PowerShell provides a set of aliases that map to Linux command names for user convenience.
These aliases have been removed from the default PowerShell on Linux/macOS distributions, allowing the native executable to be run without specifying a path.

There are pros and cons to doing this.
Removing the aliases exposes the native command experience to the PowerShell user, but reduces functionality in the shell because the native commands return strings instead of objects.

> NOTE: This is an area where the PowerShell team is looking for feedback.
> What is the preferred solution? Should we leave it as is or add the convenience aliases back? See
> [Issue #929](https://github.com/PowerShell/PowerShell/issues/929).

### Missing Wildcard (globbing) Support

Currently, PowerShell only does wildcard expansion (globbing) for built-in cmdlets on Windows, and for external commands or binaries as well as cmdlets on Linux.
This means that a command like `ls *.txt` will fail because the asterisk will not be expanded to match file names.
You can work around this by doing `ls (gci *.txt | % name)` or, more simply, `gci *.txt` using the PowerShell built-in equivalent to `ls`.

See [#954](https://github.com/PowerShell/PowerShell/issues/954) to give us feedback on how to improve the globbing experience on Linux/macOS.

### .NET Framework vs .NET Core Framework

PowerShell on Linux/macOS uses .NET Core which is a subset of the full .NET Framework on Microsoft Windows.
This is significant because PowerShell provides direct access to the underlying framework types, methods, etc.
As a result, scripts that run on Windows may not run on non-Windows platforms because of the differences in the frameworks.
For more information about .NET Core Framework, see <https://dotnetfoundation.org/net-core>

With the advent of [.NET Standard 2.0](https://blogs.msdn.microsoft.com/dotnet/2016/09/26/introducing-net-standard/), .NET Core 2.0 will bring back many of the traditional types and methods present in the full .NET Framework.
This means that PowerShell Core will be able to load many traditional Windows PowerShell modules without modification.
You can follow our .NET Standard 2.0 related work [here](https://github.com/PowerShell/PowerShell/projects/4).

### Redirection Issues

Input redirection is not supported in PowerShell on any platform.
[Issue #1629](https://github.com/PowerShell/PowerShell/issues/1629)

Use `Get-Content` to write the contents of a file into the pipeline.

Redirected output will contain the Unicode byte order mark (BOM) when the default UTF-8 encoding is used.
The BOM will cause problems when working with utilities that do not expect it or when appending to a file.
Use `-Encoding Ascii` to write ASCII text (which, not being Unicode, will not have a BOM).
(Note: see [RFC0020](https://github.com/PowerShell/PowerShell-RFC/issues/71) to give us feedback on improving the encoding experience for PowerShell Core across all platforms.
We are working to support UTF-8 without a BOM and potentially changing the encoding defaults for various cmdlets across platforms.)

### Job Control

There is no job-control support in PowerShell on Linux/macOS.
The `fg` and `bg` commands are not available.

For the time being, you can use [PowerShell jobs](https://msdn.microsoft.com/powershell/reference/5.1/microsoft.powershell.core/about/about_jobs) which do work across all platforms.

### Remoting Support

Currently, PowerShell Core supports PowerShell Remoting (PSRP) over WSMan with Basic authentication on macOS and Linux, and with NTLM-based authentication on Linux.
(Kerberos-based authentication is not supported.)

The work for WSMan-based remoting is being done in the [psl-omi-provider](https://github.com/PowerShell/psl-omi-provider) repo.

PowerShell Core also supports PowerShell Remoting (PSRP) over SSH on all platforms (Windows, macOS, and Linux).
While this is not currently supported in production, you can learn more about setting this up [here](https://docs.microsoft.com/powershell/scripting/core-powershell/ssh-remoting-in-powershell-core).

### Just-Enough-Administration (JEA) Support

The ability to create constrained administration (JEA) remoting endpoints is not currently available in PowerShell on Linux/macOS.
This feature is currently not in scope for 6.0 and something we will consider post 6.0 as it requires significant design work.

### `sudo`, `exec`, and PowerShell

Because PowerShell runs most commands in memory (like Python or Ruby), you can't use sudo directly with PowerShell built-ins.
(You can, of course, run `powershell` from sudo.)
If it is necessary to run a PowerShell cmdlet from within PowerShell with sudo, for example, `sudo Set-Date 8/18/2016`, then you would do `sudo powershell Set-Date 8/18/2016`.
Likewise, you can't exec a PowerShell built-in directly.
Instead you would have to do `exec powershell item_to_exec`.

This issue is currently being tracked as part of #3232.

### Missing Cmdlets

A large number of the commands (cmdlets) normally available in PowerShell are not available on Linux/macOS.
In many cases, these commands make no sense on these platforms (e.g. Windows-specific features like the registry).
Other commands like the service control commands (Get/Start/Stop-Service) are present, but not functional.
Future releases will correct these problems, fixing the broken cmdlets and adding new ones over time.

### Command Availability

The following table lists commands that are known not to work in PowerShell on Linux/macOS.

<table>
<th>Commands</th><th>Operational State</th><th>Notes</th>
<tr>
<td>Get-Service, New-Service, Restart-Service, Resume-Service, Set-Service, Start-Service, Stop-Service, Suspend-Service
<td>Not available.
<td>These commands will not be recognized. This should be fixed in a future release.
</tr>
<tr>
<td>Get-Acl, Set-Acl
<td>Not available.
<td>These commands will not be recognized. This should be fixed in a future release.
</tr>
<tr>
<td>Get-AuthenticodeSignature, Set-AuthenticodeSignature
<td>Not available.
<td>These commands will not be recognized. This should be fixed in a future release.
</tr>
<tr>
<td>Wait-Process
<td>Available, doesn't work properly. <td>For example `Start-Process gvim -PassThru | Wait-Process` doesn't work; it fails to wait for the process.
</tr>
<tr>
<td>Register-PSSessionConfiguration, Unregister-PSSessionConfiguration, Get-PSSessionConfiguration
<td>Available but doesn't work.
<td>Writes an error message indicating that the commands are not working. These should be fixed in a future release.
</tr>
<tr>
<td>Get-Event, New-Event, Register-EngineEvent, Register-WmiEvent, Remove-Event, Unregister-Event
<td>Available but no event sources are available.
<td>The PowerShell eventing commands are present but most of the event sources used with the commands (such as System.Timers.Timer) are not available on Linux making the commands useless in the Alpha release.
</tr>
<tr>
<td>Set-ExecutionPolicy
<td>Available but doesn't work.
<td>Returns a message saying not supported on this platform. Execution policy is a user-focused "safety belt" that helps prevent the user from making expensive mistakes. It is not a security boundary.
</tr>
<tr>
<td>New-PSSessionOption, New-PSTransportOption
<td>Available but New-PSSession doesn't work.
<td>New-PSSessionOption and New-PSTransportOption are not currently verified to work now that New-PSSession works.
</tr>
</table>
