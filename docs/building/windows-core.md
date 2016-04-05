Build PowerShell on Windows for .NET Core
=========================================

This guide will walk you through building PowerShell on Windows,
targetting .NET Core. We'll start by showing how to set up your
environment from scratch.

Environment
===========

These instructions are tested on Windows 10 and Windows Server 2012
R2, though they should work anywhere the dependencies work.

.NET CLI
--------

We use the [.NET Command Line Interface][dotnet-cli] (`dotnet`) to
build PowerShell. The following script will install `dotnet` and add
it to your PowerShell session's path:

```powershell
Invoke-WebRequest -Uri https://raw.githubusercontent.com/dotnet/cli/rel/1.0.0/scripts/obtain/install.ps1 -OutFile install.ps1
./install.ps1 -version 1.0.0-beta-002198
$env:Path += ";$env:LocalAppData\Microsoft\dotnet\cli"
```

If you have any problems installing `dotnet`, please see their
[documentation][cli-docs].

If you are using Windows 7, Windows Server 2008 or Windows Server 2012
you will also need to install
[Visual C++ Redistributable for Visual Studio 2012 Update 4][redist-2012]
and [Visual C++ Redistributable for Visual Studio 2015][redist-2015].

The version of .NET CLI is very important, you want a recent build of
1.0.0 (**not** 1.0.1).

Previous installations of DNX, `dnvm`, or older installations of .NET
CLI can cause odd failures when running. Please check your version.

[dotnet-cli]: https://github.com/dotnet/cli#new-to-net-cli
[cli-docs]: https://dotnet.github.io/getting-started/
[redist-2012]: https://www.microsoft.com/en-us/download/confirmation.aspx?id=30679
[redist-2015]: https://www.microsoft.com/en-us/download/details.aspx?id=48145

Git Setup
---------

Please clone the superproject (this repo) and initialize a subset of
the submodules:

```sh
git clone https://github.com/PowerShell/PowerShell.git
cd PowerShell
git submodule update --init -- src/windows-build src/Modules/Pester
```

Build using our module
======================

We maintain a [PowerShell module](../../PowerShellGitHubDev.psm1) with
the function `Start-PSBuild` to build PowerShell.

```powershell
Import-Module ./PowerShellGitHubDev.psm1
Start-PSBuild
```

Congratulations! If everything went right, PowerShell is now built and
executable as `./src/Microsoft.PowerShell.Host/bin/Debug/netstandardapp1.5/win10-x64/powershell`.

This location is of the form
`./[project]/bin/[configuration]/[framework]/[rid]/[binary name]`, and
our project is `Microsoft.PowerShell.Host`, configuration is `Debug`
by default, framework is `netstandardapp1.5`, runtime identifier is
**probably** `win10-x64` (but will depend on your operating system;
don't worry, `dotnet --info` will tell you what it was), and binary
name is `powershell`. The function `Get-PSOutput` will return the path
to the executable; thus you can execute the development copy via `&
(Get-PSOutput)`.

The `Microsoft.PowerShell.Host` project is the cross-platform host for
PowerShell targetting .NET Core. It is the top level project, so
`dotnet build` transitively builds all its dependencies, and emits a
`powershell` executable. The cross-platform host has built-in
documentation via `--help`.

You can run our cross-platform Pester tests with `Start-PSPester`.
