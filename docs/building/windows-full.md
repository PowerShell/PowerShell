Build PowerShell on Windows for .NET Full
=========================================

This guide supplements the
[Windows .NET Core instructions](./windows-core.md), as building the
.NET 4.5.1 (desktop) version is pretty similar.

Environment
===========

In addition to the dependencies specified in the .NET Core
instructions, we need:

Install the Visual C++ Compiler via Visual Studio 2015.
-------------------------------------------------------

This component is required to compile the native `powershell.exe` host.

This is an optionally installed component, so you may need to run the
Visual Studio installer again.

If you don't have any Visual Studio installed, you can use
[Visual Studio 2015 Community Edition][vs].

> Compiling with older versions should work, but we don't test it.

**Troubleshooting note:** If `cmake` says that it cannot determine the
`C` and `CXX` compilers, you either don't have Visual Studio, or you
don't have the Visual C++ Compiler component installed.

[vs]: https://www.visualstudio.com/en-us/products/visual-studio-community-vs.aspx

Install CMake and add it to `PATH`.
-----------------------------------

You can install it from [Chocolatey][] or [manually][].

```
choco install cmake.portable
```

[Chocolatey]: https://chocolatey.org/packages/cmake.portable
[manually]: https://cmake.org/download/

Build using our module
======================

Use `Start-PSBuild -FullCLR` from the `build.psm1`
module.

Because the `ConsoleHost` project (*not* the `Host` project) is a
library and not an application (in the sense that .NET CLI does not
emit a native executable using .NET Core's `corehost`), it targets the
framework `netstandard1.6`, *not* `netcoreapp1.0`, and the build
output will *not* have a runtime identifier in the path.

Thus the output location of `powershell.exe` will be
`./src/Microsoft.PowerShell.ConsoleHost/bin/Debug/net451/powershell.exe`

Build manually
==============

The build contains the following steps:

- generating Visual Studio project: `cmake`
- building `powershell.exe` from generated solution: `msbuild
  powershell.sln`
- building managed DLLs: `dotnet publish --runtime net451`


What I can do with the produced binaries?
=========================================

Creating a deployable package out of them is **not a supported scenario**.

The reason why we are building these binaries is
we have components (i.e. workflows) that are not currently available in the CoreClr version.
We want to make sure that CoreClr PowerShell changes don't introduce regressions in FullClr PowerShelll.

It's possible to run (for test purposes) the dev version of these binaries as follows.

Running Dev version of FullClr PowerShell
-----------------------------------------

Running FullCLR version is not as simple as CoreCLR version.

If you just run `./powershell.exe`, you will get a `powershell`
process, but all the interesting DLLs (such as
`System.Management.Automation.dll`) would be loaded from the Global
Assembly Cache (GAC), not your output directory.

Use `Start-DevPowerShell` helper funciton, to workaround it with `$env:DEVPATH`

```powershell
Start-DevPowerShell
```

This command has a reasonable default to run `powershell.exe` from the build output folder.
If you are building an unusual configuration (i.e. not `Debug`), you can explicitly specify path to the bin directory

```powershell
Start-DevPowerShell -binDir .\src\Microsoft.PowerShell.ConsoleHost\bin\Debug\net451
```

Or more programmatically:

```powershell
Start-DevPowerShell -binDir (Split-Path -Parent (Get-PSOutput))
```

The default for produced `powershell.exe` is x64.
You can control it with `Start-PSBuild -FullCLR -NativeHostArch x86`

