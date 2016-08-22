Known Issues for PowerShell on Linux
====================================

The first Alpha release of PowerShell on Linux is mostly functional but
does have some significant limitations and usability issues. 
In some cases, these issues are simply bugs that haven't been fixed yet. 
In other cases (as with the default aliases for ls, cp, etc.) we are
looking for feedback from the community regarding the choices we make.

Case-sensitivity in PowerShell
-------------------------------

Historically, PowerShell has uniformly been case-insensitive. 
On UNIX, the file system is case-sensitive and this is exposed through a number
of ways, obvious and non-obvious.

### Directly:

-   When specifying a file in PowerShell the correct case must be used.

-   Only forward slashes can be used in path. 
    (On Windows either forward or backward slashes can be used.)

### Indirectly:

-   If a script tries to load a module and the module name is not cased
    correctly, then the module load will fail. 
    This may cause a problem with existing scripts if the name by which the module is referenced
    doesn't match the actual file name.

-   Tab-completion will not automatically auto-complete if the file name case is wrong. 
    The fragment to complete must be cased properly.
    (Completion is case-insensitive for type name and type member completions.)

.PS1 File Extensions
--------------------

PowerShell scripts must end in `.ps1` for the interpreter to understand
how to load and run them in the current process. 
Running scripts in the current process is the expected usual behavior for PowerShell. 
The `#!` magic number may be added to a script that doesn't have a `.ps1` extension,
but this will cause the script to be run in a new PowerShell instance
preventing the script from working properly when interchanging objects.

Missing command aliases
-----------------------

On Linux, the "convenience aliases" for the basic commands `ls`, `cp`,
`mv`, `rm`, `cat`, `man`, `mount`, `ps` have been removed. 
On Windows, PowerShell provides a set of aliases that map to UNIX/Linux command
names for user convenience. 
These aliases have been removed from the default PowerShell on Linux distribution, 
allowing the native executable to be run instead. 
There are pros and cons to having do this. 
It exposes the native command experience to the PowerShell on Linux user but reduces
functionality in the shell because the native commands return strings not objects.

> NOTE: This is an area where the PowerShell team is looking for feedback. 
> What is the preferred solution? Should we leave it as is or add the 
> convenience aliases back? See 
> [Issue #929](https://github.com/PowerShell/PowerShell/issues/929).

Missing Wildcard (globbing) Support 
------------------------------------

Currently, PowerShell only does wildcard expansion (globbing) for the
built-ins but not for external commands. 
This means that a command like `ls *.txt` will fail because the asterisk will not be 
expanded to match file names. 
You can work around this by doing `ls (gci *.txt | % name)` or, more simply, 
`gci *.txt` using the PowerShell built-in equivalent to `ls`.

.NET Framework vs .NET Core Framework
-----------------

PowerShell on Linux uses the .NET Core which is a subset of the full
.NET Framework on Microsoft Windows. 
This is significant because PowerShell provides direct access to the underlying framework types,
methods etc. 
As a result, scripts that run on Windows may not run on Linux because of the differences in the frameworks. 
For more information about .NET Core Framework, see <https://www.dotnetfoundation.org/netcore>

Redirection Issues
------------------

Input redirection is not supported in PowerShell on any platform. 
Use either `Get-Content` to write the contents of a file into the pipeline.

PowerShell does not currently support "direct pipelining" external commands. 
Although the pipeline works properly for built-in PowerShell commands, 
with external (also called native) commands, each individual
command in the pipeline is run to completion and then the aggregated
data is passed to the next command. 
(This behavior is intended to be fixed in a later release.)

Redirected output will contain the Unicode byte order mark (BOM) when the default UTF-8 encoding is used. 
The BOM will cause problems when working with utilities that do not expect it or when appending to a file.

Use `-Encoding ascii` to write ASCII text (which, not being Unicode, will not have a BOM).

Job Control
-----------

There is no job-control support in PowerShell on Linux. 
The `fg` and `bg` commands are not available.
`Ctrl-Z` sends the `powershell` process to the background.

Remoting Support
----------------

Client-side remoting from Linux is not supported with the initial package. 
This will be enabled shortly after the Alpha release by installing an additional package.

Just-Enough-Administration (JEA) Support
----------------------------------------

The ability to create constrained administration (JEA) remoting
endpoints is not currently available in PowerShell on Linux. 
This feature will be enabled shortly after the Alpha release by installing new package.

sudo, exec, and PowerShell
-------------------------

Because PowerShell runs most commands in memory (like Python or Ruby)
you can't use sudo directly with PowerShell built-ins. 
(You can, of course, run `powershell` from sudo.) 
If it is necessary to run a PowerShell cmdlet from within PowerShell with sudo,
for example `sudo Set-Date 8/18/2016`,
then you would do `sudo powershell Set-Date 8/18/2016`. 
Likewise, you can't exec a PowerShell built-in directly. 
Instead you would have to do `exec powershell item_to_exec`.

Missing Cmdlets
---------------

A large number of the commands (cmdlets) normally available in PowerShell are not available on Linux. 
In many cases, these commands make no sense on Linux (e.g. Windows-specific features like the registry). 
Other commands like the service control commands (get/start/stop-service are present but not functional.) 
Future releases will correct these problems, fixing the broken cmdlets and adding new ones over time.

Command Availability
--------------------

The following table lists commands that are known not to work in PowerShell on Linux.

<table>
<th>Commands<td><b>Operational State<td><b>Notes</th>
<Tr>
<td>Get-Service New-Service Restart-Service Resume-Service Set-Service Start-Service Stop-Service Suspend-Service
<td>Not available.
<td>These commands will not be recognized. This will be fixed in a future release.
</tr>
<tr>
<td>Get-Acl, Set-Acl 
<td>Not available.
<td>These commands will not be recognized. This will be fixed in a future release.
</tr>
<tr>
<td>Get-AuthenticodeSignature, Set-AuthenticodeSignature
<td>Not available.
<td>These commands will not be recognized. This will be fixed in a future release.
</tr>
<tr>
<td>Wait-Process
<td>Available, doesn't work properly. <td>For example `Start-Process gvim -PassThru | Wait-Process` doesn't work; it fails to wait for the process.
</tr>
<tr>
<td>Update-Help
<td>Available but doesn't work.
<td>`CabinetExtractorFactory` generates an `InvalidOperation` exception. These will be fixed in a future release.
</tr>
<tr>
<td>Register-PSSessionConfiguration, Unregister-PSSessionConfiguration, Get-PSSessionConfiguration
<td>Available but doesn't work.
<td>Writes an error message indicating that the commands are not working. These will be fixed in a future release.
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
<td>Select-Xml
<td>Available but doesn't work.
<td>The underlying Select.Xml.Node classes are missing on Linux/.NET Core. This is unlikely to be fixed in the near term so this cmdlet will be probably be removed.
</tr>
<tr>
<td>New-PSSession, New-PSSessionOption, New-PSTransportOption
<td>Available but New-PSSession doesn't work.
<td>New-PSSessionOption and New-PSTransportOption do work but are useless without New-PSSession. The underlying client remoting layer code for WSMan is missing. This will be fixed soon in a future release.
</tr>
<tr>
<td>Start-Job, Get-Job, Receive-Job, Remove-Job, Stop-Job, Wait-Job
<td>The background job cmdlets are available and work with the important exception of Start-Job.
<td>Without the ability to start a background job, the other cmdlets are useless. This will be fixed soon in a future release.
</tr>
</table>

Known Issues for PowerShell on Windows
======================================

Remoting Endpoint Creation on Nano Server TP5
---------------------------------------------

The [script](https://github.com/PowerShell/PowerShell/blob/master/docs/installation/windows.md) to create a new WinRM remoting 
endpoint (`Install-PowerShellRemoting.ps1`) encounters a bug in the in-box PowerShell Core on Nano Server TP5.
The bug causes the script to create an incorrect directory for the plugin and may result in creation of an invalid remoting endpoint.
When the same command is run for the second time, the script executes as expected and successfully creates the WinRM remoting endpoint. 
The bug in in-box PowerShell Core on Nano Server TP5 does not occur in later versions of Nano Server.
