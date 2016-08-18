Debugging
=========

Visual Studio Code
=======

[Experimental .NET Core Debugging in VS Code][core-debug] enables
cross-platform debugging with the [Visual Studio Code][vscode] editor.
This is made possible by the [OmniSharp][] extension for VS Code.

Please review their [detailed instructions][vscclrdebugger]. In
addition to being able to build PowerShell, you need:

- C# Extension for VS Code installed
- .NET Core debugger installed (semi-automatic)
- `powershell` executable in your path (self-host if not on Windows)

Once the extension is installed, you have to open a C# file to force VS Code to
install the actual .NET Core debugger (the editor will tell you to do this if
you attempt to debug and haven't already open a C# file).

The committed `.vscode` folder in the root of this repository contains
the `launch.json` and `tasks.json` files which provide Core PowerShell
debugging configurations and a build task.

The "build" task will run `Start-PSBuild`, emitting the executable to
`PowerShell/debug/powershell` so that the debugger always knows where to find it
(regardless of platform). If you edit this, please do not commit it, as the
default is meant to "just work" for anyone.

The ".NET Core Launch" configuration will build and start a `powershell`
process, with `justMyCode` disabled, and `stopAtEntry` enabled, thus PowerShell
will stop right at `Main`, and you need to click the green arrow to continue.

With either Gnome Terminal or XTerm installed, the launch configuration will
launch an external console with PowerShell running interactively. If neither of
these installed, the editor will tell you to do so.

Alternatively, the ".NET Core Attach" configuration will start listening for a
process named `powershell`, and will attach to it. If you need more fine grained
control, replace `processName` with `processId` and provide a PID. (Please be
careful not to commit such a change.)

[core-debug]: https://blogs.msdn.microsoft.com/visualstudioalm/2016/03/10/experimental-net-core-debugging-in-vs-code/
[vscode]: https://code.visualstudio.com/
[OmniSharp]: https://github.com/OmniSharp/omnisharp-vscode
[vscclrdebugger]: http://aka.ms/vscclrdebugger

PowerShell
==========

The `Trace-Command` cmdlet can be used to enable tracing of certain PowerShell
subsystems. Use `Get-TraceSource` for a list of tracers:

* CmdletProviderClasses
* CommandDiscovery
* CommandSearch
* ConsoleHost
* ConsoleHostRunspaceInit
* ConsoleHostUserInterface
* ConsoleLineOutput
* DisplayDataQuery
* ETS
* FileSystemProvider
* FormatFileLoading
* FormatViewBinding
* LocationGlobber
* MemberResolution
* Modules
* MshSnapinLoadUnload
* ParameterBinderBase
* ParameterBinderController
* ParameterBinding
* PathResolution
* PSDriveInfo
* PSSnapInLoadUnload
* RunspaceInit
* SessionState
* TypeConversion
* TypeMatch

Then trace it like this:

```powershell
Trace-Command -Expression { Get-ChildItem . } -Name PathResolution -PSHost
```

The `-PSHost` specifies the sink, in this case the console host, so we can see
the tracing messages.

LLDB with SOS plugin
====================

The `./tools/debug.sh` script can be used to launch PowerShell inside of LLDB
with the SOS plugin provided by .NET Core. This provides an additional way to
debug PowerShell on Linux, but VS Code is recommended for a better user
experience (and its single-stepping capabilities).

The script is self-documented and contains a link to the
[CoreCLR debugging help][clr-debug] .

[clr-debug]: https://github.com/dotnet/coreclr/blob/master/Documentation/building/debugging-instructions.md#debugging-coreclr-on-linux

corehost
========

The native executable produced by .NET CLI will produce trace output
if launched with `COREHOST_TRACE=1 ./powershell`.

CoreCLR PAL
===========

The native code in the CLR has debug channels to selectively output
information to the console. These are controlled by the
`PAL_DBG_CHANNELS`, e.g., `export PAL_DBG_CHANNELS="+all.all"`, as
detailed in the `dbgmsg.h` [header][].

[header]: https://github.com/dotnet/coreclr/blob/release/1.0.0-rc2/src/pal/src/include/pal/dbgmsg.h
