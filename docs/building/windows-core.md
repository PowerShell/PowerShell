# Build PowerShell on Windows for .NET Core

This guide will walk you through building PowerShell on Windows, targeting .NET Core.
We'll start by showing how to set up your environment from scratch.

## Environment

These instructions are tested on Windows 10 and Windows Server 2012
R2, though they should work anywhere the dependencies work.

### Git Setup

Using Git requires it to be set up correctly; refer to the
[Readme](../../README.md) and
[Contributing Guidelines](../../.github/CONTRIBUTING.md).

This guide assumes that you have recursively cloned the PowerShell repository and `cd`ed into it.

### Visual Studio

Install [Visual Studio 2019](https://visualstudio.microsoft.com/downloads/). The Community edition is available free of charge.

The PowerShell/PowerShell repository requires at least Visual Studio 2019 16.7.

### Visual Studio Code

Building PowerShell using [Visual Studio Code](https://code.visualstudio.com/) depends on the PowerShell executable to be called `pwsh` which means
that you must have PowerShell Core 6 Beta.9 (or newer) installed to successfully build this project (typically for the purpose of debugging).

### .NET CLI

We use the [.NET command-line interface][dotnet-cli] (`dotnet`) to build PowerShell.
The version we are currently using is mentioned in [`global.json`](../../global.json#L3) at the root of this repository.
The `Start-PSBootstrap` function will automatically install it and add it to your path:

```powershell
Import-Module ./build.psm1
Start-PSBootstrap
```

Or you can call `Install-Dotnet` directly:

```powershell
Install-Dotnet
```

It removes the previously installed version of .NET CLI and installs the version that PowerShell Core depends on.
If you have any problems installing `dotnet`, please see their [documentation][cli-docs].

[dotnet-cli]: https://learn.microsoft.com/dotnet/core/tools/
[cli-docs]: https://www.microsoft.com/net/core#windowscmd

## Build using our module

We maintain a [PowerShell module](../../build.psm1) with the function `Start-PSBuild` to build PowerShell.

```powershell
Import-Module ./build.psm1
Start-PSBuild -Clean -PSModuleRestore -UseNuGetOrg
```

Congratulations! If everything went right, PowerShell is now built and executable as `./src/powershell-win-core/bin/Debug/net6.0/win7-x64/publish/pwsh.exe`.

This location is of the form `./[project]/bin/[configuration]/[framework]/[rid]/publish/[binary name]`,
and our project is `powershell`, configuration is `Debug` by default,
framework is `net6.0`, runtime identifier is `win7-x64` by default,
and binary name is `pwsh`.
The function `Get-PSOutput` will return the path to the executable;
thus you can execute the development copy via `& (Get-PSOutput)`.

The `powershell` project is the .NET Core PowerShell host.
It is the top-level project, so `dotnet build` transitively builds all its dependencies,
and emits a `pwsh` executable.
The cross-platform host has built-in documentation via `--help`.

You can run our cross-platform Pester tests with `Start-PSPester`.

```powershell
Import-Module ./build.psm1
Start-PSPester -UseNuGetOrg
```

## Building in Visual Studio

We currently have the issue [#3400](https://github.com/PowerShell/PowerShell/issues/3400) tracking this task.
