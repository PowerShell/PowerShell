Build PowerShell on Linux
=========================

This guide will walk you through building PowerShell on Linux. 
We'll start by showing how to set up your environment from scratch.

Environment
===========

These instructions are written assuming the Ubuntu 14.04 LTS, since that's the distro the team uses. 
The build module works on a best-effort basis for other distributions.

Git Setup
---------

Using Git requires it to be set up correctly; 
refer to the [Working with the PowerShell Repository](../git/README.md),
[README](../../README.md), and [Contributing Guidelines](../../.github/CONTRIBUTING.md).

**This guide assumes that you have recursively cloned the PowerShell repository and `cd`ed into it.**

Toolchain Setup
---------------

We use the [.NET Command-Line Interface][dotnet-cli] (`dotnet`) to build the managed components, 
and [CMake][] to build the native components. 
Install the following packages for the toolchain:

- `dotnet`: Must be installed from the `Start-PSBootstrap` module as described below.
- `cmake`
- `make`
- `g++`

Unfortunately, the `apt-get` feed for `dotnet` has been deprecated,
and the latest version is only distributed in the form of three separate packages, 
which require manual dependency resolution.

Installing the toolchain is as easy as running `Start-PSBootstrap` in PowerShell. 
Of course, this requires a self-hosted copy of PowerShell on Linux.

Fortunately, this is as easy as [downloading and installing the package](../installation/linux.md). 
The `./tools/download.sh` script will also install the PowerShell package.

In Bash:

```sh
./tools/download.sh

powershell
```

You should now be in a `powershell` console host that is installed separately from any development copy you're about to build. 
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
- Downloads and installs the latest .NET CLI 1.0.0-preview3 SDK to `~/.dotnet`

If you want to use `dotnet` outside of `Start-PSBuild`, add `~/.dotnet` to your `PATH` environment variable.

[dotnet-cli]: https://github.com/dotnet/cli#new-to-net-cli
[CMake]: https://cmake.org/cmake/help/v2.8.12/cmake.html

.NET CLI
--------

If you have any problems installing `dotnet`, please see their [documentation][cli-docs].

The version of .NET CLI is very important; you need a recent build of 1.0.0 (**not** 1.0.1).

Previous installations of DNX, `dnvm`, or older installations of .NET CLI can cause odd failures when running. 
Please check your version and uninstall prior any prior versions.

[cli-docs]: https://dotnet.github.io/getting-started/

Build using our module
======================

We maintain a [PowerShell module](../../build.psm1) with the function `Start-PSBuild` to build PowerShell. 
Since this is PowerShell code, it requires self-hosting. 
If you have followed the toolchain setup section above, you should have `powershell` installed.

> If you cannot or do not want to self-host, `Start-PSBuild` is just a
> convenience; you can execute each step of the build process yourself
> in Bash; see [Build manually](#build-manually) below.

```powershell
Import-Module ./build.psm1
Start-PSBuild
```
Congratulations! If everything went right, PowerShell is now built.
The `Start-PSBuild` script will output the location of the executable:

`./src/powershell-unix/bin/Linux/netcoreapp1.0/ubuntu.14.04-x64/powershell`.

You should now be running the `powershell` that you just built, if your run the above executable.
You can run our cross-platform Pester tests with `Start-PSPester`, and our xUnit tests with `Start-PSxUnit`.

Build manually
==============

The following goes into detail about what `Start-PSBuild` does.

Build the native library
------------------------

The `libpsl-native.so` library consists of native functions that `CorePsPlatform.cs` P/Invokes.

```sh
pushd src/libpsl-native
cmake -DCMAKE_BUILD_TYPE=Debug .
make -j
make test
popd
```

This library will be emitted in the `src/powershell-unix` project,
where `dotnet` consumes it as "content" and thus automatically deploys it.

Build the managed projects
--------------------------

The `powershell` project is the .NET Core PowerShell host. 
It is the top level project, so `dotnet build` transitively builds all its dependencies, and emits a `powershell` executable. 
The `--configuration Linux` flag is necessary to ensure that the preprocessor definition `LINUX` is defined (see [issue #673][]).

```sh
dotnet restore
cd src/powershell-unix
dotnet build --configuration Linux
```

The executable will be in `./bin/[configuration]/[framework]/[rid]/[binary name]`, 
where our configuration is `Linux`, framework is `netcoreapp1.0`, 
runtime identifier is `ubuntu.14.04-x64`, and binary name is `powershell`. 
The function `Get-PSOutput` will return the path to the executable; 
thus you can execute the development copy via `& (Get-PSOutput)`.

For deploying PowerShell, `dotnet publish` will emit a `publish` directory that contains a flat list of every dependency required for
PowerShell. 
This can be copied to, for example, `/usr/local/share/powershell` or packaged.

[issue #673]: https://github.com/PowerShell/PowerShell/issues/673
