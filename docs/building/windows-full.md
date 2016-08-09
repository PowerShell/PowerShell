Build PowerShell on Windows for .NET Full
=========================================

This guide supplements the
[Windows .NET Core instructions](./windows-core.md), as building the
.NET 4.5.1 (desktop) version is pretty similar.

Environment
===========

In addition to the dependencies specified in the .NET Core
instructions, you'll need to:

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

The output location of `powershell.exe` will be

```
.\src\powershell-win-full\bin\Debug\net451\win10-x64\publish\powershell.exe
```

Build manually
==============

The build contains the following steps:

- generating Visual Studio project: `cmake`
- building `powershell.exe` from generated solution: `msbuild
  powershell.sln`
- building managed DLLs: `dotnet publish --runtime net451`


What can you do with the produced binaries?
=========================================

**Important**: "We don’t support production deployments of these binaries on any platform". For PowerShell .NET (aka: FullCLR PowerShell) our recommendation is to continue using the PowerShell .NET version already shipping in Windows Client and Windows Server.

The primary reason to build the PowerShell FullCLR binaries is to test backward compatibility, and interoperability between .NET and CoreCLR.  It is also important to mention some features like PowerShell Workflows are not currently available in the CoreCLR version. So we want to provide the ability for the Community to test CoreCLR PowerShell code changes while validating that these changes don't introduce regressions in .NET PowerShell (aka: as FullCLR PowerShell)

To run (for test purposes) the dev version of these binaries please follow the following steps:


Running Dev version of FullCLR PowerShell
-----------------------------------------

Running FullCLR version is not as simple as CoreCLR version.

If you just run `./powershell.exe`, you will get a `powershell`
process, but all the interesting DLLs (such as
`System.Management.Automation.dll`) would be loaded from the Global
Assembly Cache (GAC), not your output directory.

Use `Start-DevPowerShell` helper function to workaround it with `$env:DEVPATH`

```powershell
Start-DevPowerShell -FullCLR
```

This command has a reasonable default to run `powershell.exe` from the build output folder.
If you are building an unusual configuration (i.e. not `Debug`), you can explicitly specify path to the bin directory

```powershell
Start-DevPowerShell -FullCLR -binDir .\src\powershell-win-full\bin\Debug\net451\win10-x64\publish
```

Or more programmatically:

```powershell
Start-DevPowerShell -FullCLR -binDir (Split-Path -Parent (Get-PSOutput))
```

The default for produced `powershell.exe` is x64.
You can control it with `Start-PSBuild -FullCLR -NativeHostArch x86`

