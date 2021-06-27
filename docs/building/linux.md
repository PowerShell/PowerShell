# Build PowerShell on Linux

This guide will walk you through building PowerShell on Linux.
We'll start by showing how to set up your environment from scratch.

## Environment

These instructions are written assuming the Ubuntu 16.04 LTS, since that's the distro the team uses.
The build module works on a best-effort basis for other distributions.

### Git Setup

Using Git requires it to be set up correctly;
refer to the [Working with the PowerShell Repository](../git/README.md),
[README](../../README.md), and [Contributing Guidelines](../../.github/CONTRIBUTING.md).

**This guide assumes that you have recursively cloned the PowerShell repository and `cd`ed into it.**

### Toolchain Setup

We use the [.NET Command-Line Interface][dotnet-cli] (`dotnet`) to build the managed components,
and [CMake][] to build the native components.

Installing the toolchain is as easy as running `Start-PSBootstrap` in PowerShell.
Of course, this requires a self-hosted copy of PowerShell on Linux.

Fortunately, this is as easy as [downloading and installing the package](https://docs.microsoft.com/powershell/scripting/install/installing-powershell-core-on-linux#binary-archives).
The `./tools/install-powershell.sh` script will also install the PowerShell package.

In Bash:

```sh
./tools/install-powershell.sh

pwsh
```

You should now be in a PowerShell console host that is installed.
Just import our module, bootstrap the dependencies, and build!

In PowerShell:

```powershell
Import-Module ./build.psm1
Start-PSBootstrap
```

The `Start-PSBootstrap` function does the following:

- Adds the LLVM package feed
- Installs our dependencies combined with the dependencies of the .NET CLI toolchain via `apt-get`
- Uninstalls any prior versions of .NET CLI
- Downloads and installs the .NET Core SDK 2.0.0 to `~/.dotnet`

If you want to use `dotnet` outside of `Start-PSBuild`, add `~/.dotnet` to your `PATH` environment variable.

[dotnet-cli]: https://docs.microsoft.com/dotnet/core/tools/
[CMake]: https://cmake.org/cmake/help/v2.8.12/cmake.html

## Build using our module

We maintain a [PowerShell module](../../build.psm1) with the function `Start-PSBuild` to build PowerShell.
Since this is PowerShell code, it requires self-hosting.
If you have followed the toolchain setup section above, you should have PowerShell Core installed.

```powershell
Import-Module ./build.psm1
Start-PSBuild
```

Congratulations! If everything went right, PowerShell is now built.
The `Start-PSBuild` script will output the location of the executable:

`./src/powershell-unix/bin/Debug/net5.0/linux-x64/publish/pwsh`.

You should now be running the PowerShell Core that you just built, if you run the above executable.
You can run our cross-platform Pester tests with `Start-PSPester`, and our xUnit tests with `Start-PSxUnit`.
