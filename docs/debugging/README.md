# Visual Studio Code

The [Visual Studio Code][vscode] editor supports cross-platform debugging.
This is made possible by the [OmniSharp][] extension for VS Code.

Please review their [detailed instructions][core-debug]. In
addition to being able to build PowerShell, you need:

- C# Extension for VS Code installed
- .NET Core debugger installed (semi-automatic)
- `powershell` executable in your path (self-host if not on Windows)

The .NET CLI tools *must* be on your path for Visual Studio Code.
`Start-PSBootstrap` installs the tools to `~/.dotnet` (non-Windows) or `"$env:LocalAppData\Microsoft\dotnet"` (Windows),
but does not add this to your `PATH`.
You can do this in Bash with `export PATH=$PATH:$HOME/.dotnet` or in PowerShell with `$env:path = $env:path+";$env:LocalAppData\Microsoft\dotnet"`.

Once the extension is installed, you have to open a C# file to force VS Code to
install the actual .NET Core debugger (the editor will tell you to do this if
you attempt to debug and haven't already opened a C# file).

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
process named `powershell`, and will attach to it. If you need more fine-grained
control, replace `processName` with `processId` and provide a PID. (Please be
careful not to commit such a change.)

[core-debug]: https://docs.microsoft.com/dotnet/core/tutorials/with-visual-studio-code#debug
[vscode]: https://code.visualstudio.com/
[OmniSharp]: https://github.com/OmniSharp/omnisharp-vscode

## PowerShell

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

The `-PSHost` specifies the sink, in this case the console host,
so we can see the tracing messages.
The `-Name` chooses the list of tracers to enable.

## LLDB with SOS plug-in

The `./tools/debug.sh` script can be used to launch PowerShell inside of LLDB
with the SOS plug-in provided by .NET Core. This provides an additional way to
debug PowerShell on Linux, but VS Code is recommended for a better user
experience (and its single-stepping capabilities).

The script is self-documented and contains a link to the
[CoreCLR debugging help][clr-debug] .

[clr-debug]: https://github.com/dotnet/coreclr/blob/master/Documentation/building/debugging-instructions.md#debugging-coreclr-on-linux

## `corehost`

The native executable produced by .NET CLI will produce trace output
if launched with `COREHOST_TRACE=1 ./powershell`.

## CoreCLR PAL

The native code in the CLR has debug channels to selectively output
information to the console. These are controlled by the
`PAL_DBG_CHANNELS`, e.g., `export PAL_DBG_CHANNELS="+all.all"`, as
detailed in the `dbgmsg.h` [header][].

Enabling `+all.all` is *incredibly* noisy;
you will need to narrow your scope.

[header]: https://github.com/dotnet/coreclr/blob/release/1.0.0/src/pal/src/include/pal/dbgmsg.h

## Debugging .NET Core

The .NET Core libraries downloaded from NuGet and shipped with PowerShell are release versions.
This means that `PAL_DBG_CHANNELS` will not work with them,
and instead you must build and deploy .NET Core built in debug mode.
These instructions are not meant to be comprehensive,
but should prove useful.

They are currently written for Linux and are meant only as a shortcut means to debug.

## Build and deploy CoreCLR

* Clone CoreCLR: `git clone -b release/1.0.0 https://github.com/dotnet/coreclr.git`
* Follow [building instructions](https://github.com/dotnet/coreclr/blob/release/1.0.0/Documentation/building/linux-instructions.md)
* Wait for `./build.sh` to finish
* Overwrite PowerShell libraries: `cp bin/Product/Linux.x64.Debug/*{so,dll} /path/to/powershell/`

## Build and deploy CoreFX

* Clone CoreFX: `git clone -b release/1.0.0 https://github.com/dotnet/corefx.git`
* Follow [building instructions](https://github.com/dotnet/corefx/blob/release/1.0.0/Documentation/building/unix-instructions.md)
* Wait for `./build.sh skiptests` to finish
* Overwrite PowerShell libraries:

> This must be done in a particular order to get the most specific build,
> and each phase must be allowed to overwrite both the previous phase
> and any files previously found (hence the use of `-exec cp`).
> The glob cannot go more than one directory deep,
> as subdirectories can have alternative and unwanted implementations
> of libraries with the same name.

```sh
dest=/path/to/powershell/
find bin/AnyOS.AnyCPU.Debug/*/*.dll -exec cp -p {} $dest \;
find bin/Unix.AnyCPU.Debug/*/*.dll -exec cp -p {} $dest \;
find bin/Linux.AnyCPU.Debug/*/*.dll -exec cp -p {} $dest \;
find bin/Linux.x64.Debug/ -name *.so -exec cp -p {} $dest \;
```
