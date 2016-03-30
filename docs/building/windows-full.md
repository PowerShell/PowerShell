Build PowerShell on Windows for .NET Full
=========================================

This guide supplements the
[Windows .NET Core instructions](./windows-core.md), as building the
.NET 4.5.1 (desktop) version is nearly identical.

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

Use `Start-PSBuild -FullCLR` from the `PowerShellGitHubDev.psm1`
module. The bits will be published to `binFull`.

While building is easy, running FullCLR version is not as simple as
CoreCLR version.

If you just run ~~`.\binFull\powershell.exe`~~, you will get a
`powershell` process, but all the interesting DLLs (i.e.
`System.Management.Automation.dll`) would be loaded from the GAC, not
your `binFull` build directory.

[@lzybkr](https://github.com/lzybkr) wrote a module to deal with it
and run side-by-side.

```powershell
Start-DevPSGithub -binDir $pwd\binFull
```

The default for `powershell.exe` that **we build** is x86. See
[issue #683][].

There is a separate execution policy registry key for x86, and it's
likely that you didn't ~~bypass~~ enable it. From **powershell.exe
(x86)** run:

```
Set-ExecutionPolicy Bypass
```

[issue #683]: https://github.com/PowerShell/PowerShell/issues/683

Build manually
==============

The build logic is relatively simple and contains the following steps:

- building managed DLLs: `dotnet publish --runtime net451`
- generating Visual Studio project: `cmake -G "$cmakeGenerator"`
- building `powershell.exe` from generated solution: `msbuild
  powershell.sln`

Please don't hesitate to experiment.
