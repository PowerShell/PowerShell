Debugging
=========

VS Code
-------

[Experimental .NET Core Debugging in VS Code][core-debug] enables
cross-platform debugging with the [Visual Studio Code][vscode] editor.
This is made possible by the [OmniSharp][] extension for VS Code.

Please review their [detailed instructions][vscclrdebugger]. In
addition to being able to build PowerShell, you need:

- C# Extension for VS Code installed
- `powershell` executable in your path (self-host if not on Windows)

The committed `.vscode` folder in the root of this repository contains
the `launch.json` and `tasks.json` files which provide Core PowerShell
debugging configurations and a build task.

The "build" task will run `Start-PSBuild`.

The ".NET Core Launch" configuration will build and start a
`powershell` process, with `justMyCode` disabled, and `stopAtEntry`
enabled. The debugger is highly experimental, so if it does not break
at `Main`, try again.

Note that the debugger does not yet provide `stdin` handles, so once
`ReadKey` is called in the `ReadLine` loop, `System.Console` will
throw exceptions. The options around this are 1) provide
`[ "-c", "... ; exit" ]` to the "Launch" configuration's `args` so
that the `ReadLine` listener is never called, 2) ignore the exceptions
and only debug code before the listener, or 3) use the "Attach"
configuration.

The ".NET Core Attach" configuration will start listening for a
process named `powershell`, and will attach to it. If you need more
fine grained control, replace `processName` with `processId` and
provide a PID. (Please be careful not to commit such a change).

[core-debug]: https://blogs.msdn.microsoft.com/visualstudioalm/2016/03/10/experimental-net-core-debugging-in-vs-code/
[vscode]: https://code.visualstudio.com/
[OmniSharp]: https://github.com/OmniSharp/omnisharp-vscode
[vscclrdebugger]: http://aka.ms/vscclrdebugger

corehost
--------

The native executable prouduced by .NET CLI will produce trace output
if launched with `COREHOST_TRACE=1 ./powershell`.

CoreCLR PAL
-----------

The native code in the CLR has debug channels to selectively output
information to the console. These are controlled by the
`PAL_DBG_CHANNELS`, e.g., `export PAL_DBG_CHANNELS="+all.all"`, as
detailed in the `dbgmsg.h` [header][].

[header]: https://github.com/dotnet/coreclr/blob/release/1.0.0-rc2/src/pal/src/include/pal/dbgmsg.h
