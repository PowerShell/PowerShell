Build PowerShell on Linux
=========================

This guide will walk you through building PowerShell on Linux. We'll
start by showing how to set up your environment from scratch.

Environment
===========

These instructions are written assuming the Ubuntu 14.04 LTS, since
that's the distro the team uses.

Toolchain Setup
---------------

We use the [.NET Command Line Interface][dotnet-cli] (`dotnet`) to
build the managed components, and [CMake][] to build the native
components. Install the following packages for the toolchain:

- `dotnet`
- `cmake`
- `make`
- `g++`
- `libunwind8`
- `libicu52`

And for debgging:

- `strace`
- `lldb-3.6`

In order to get `dotnet`, we need to add an additional package source:

```sh
sudo sh -c 'echo "deb [arch=amd64] http://apt-mo.trafficmanager.net/repos/dotnet/ trusty main" > /etc/apt/sources.list.d/dotnetdev.list'
sudo apt-key adv --keyserver apt-mo.trafficmanager.net --recv-keys 417A0893
sudo apt-get update
```

Then install the packages you need:

```sh
sudo apt-get install dotnet cmake make g++ libunwind8 libicu52 strace lldb-3.6
```

[dotnet-cli]: https://github.com/dotnet/cli#new-to-net-cli
[CMake]: https://cmake.org/cmake/help/v2.8.12/cmake.html

.NET CLI
--------

If you have any problems installing `dotnet`, please see their
[documentation][cli-docs].

The version of .NET CLI is very important, you want a recent build of
1.0.0 (**not** 1.0.1).

Previous installations of DNX, `dnvm`, or older installations of .NET
CLI can cause odd failures when running. Please check your version.

The drawback of using the feed is that it gets out of date. To upgrade
the package, install it by hand. Unfortunately, `dpkg` does not handle
dependency resolution, so it is recommended to first install the older
version from the feed, and then upgrade it.

```sh
wget https://dotnetcli.blob.core.windows.net/dotnet/beta/Installers/Latest/dotnet-ubuntu-x64.latest.deb
sudo dpkg -i ./dotnet-ubuntu-x64.latest.deb
```

[cli-docs]: https://dotnet.github.io/getting-started/

Git Setup
---------

Please clone the superproject (this repo) and initialize a subset of
the submodules:

```sh
git clone https://github.com/PowerShell/PowerShell.git
cd PowerShell
git submodule update --init --recursive -- src/windows-build src/libpsl-native src/Microsoft.PowerShell.Linux.Host/Modules/Pester
```

Build using our module
======================

We maintain a `PowerShellGitHubDev.psm1` PowerShell module with the
function `Start-PSBuild` to build PowerShell. Since this is PowerShell
code, it requires self-hosting. Fortunately, this is as easy as
downloading and installing the package. Unfortunately, while the
repository is still private, the package cannot be downloaded as
simply as with `wget`. We have a script that wraps the GitHub API and
uses a personal access token to authorize and obtain the package.

> You can alternativelly download via a browser and upload it to your
> box via some other method

```sh
GITHUB_TOKEN=<replace with your token> ./download.sh
sudo dpkg -i ./powershell.deb
powershell
```

You should now be in a `powershell` console host that is installed
separately from any development copy you're about to build. Just
import our module and build!

```powershell
Import-Module ./PowerShellGitHubDev.psm1
Start-PSBuild
```

Congratulations! If everything went right, PowerShell is now built and
executable as `./bin/powershell`.

> Note that the `./build.sh` script is deprecated and may be removed

You can run our cross-platform Pester tests with `./bin/powershell -c
"Invoke-Pester test/powershell"`.

Build manually
==============

The following goes into detail about what `Start-PSBuild` does.

Build the native library
------------------------

The `libpsl-native.so` library consists of native functions that
`CorePsPlatform.cs` P/Invokes.

```sh
pushd src/libpsl-native
cmake -DCMAKE_BUILD_TYPE=Debug .
make -j
make test
popd
```

This library will be emitted in the
`src/Microsoft.PowerShell.Linux.Host` project, where `dotnet` consumes
it as "content" and thus automatically deploys it.

Build the managed projects
--------------------------

The `Linux.Host`, while poorly named, is the cross-platform host for
PowerShell targetting .NET Core. It is the top level project, so
`dotnet publish` transitively builds all its dependencies, and emits a
`powershell` executable and all necessary libraries (both native and
managed) in a flat directory (specified with `--output`, otherwise
automatically nested depending on runtime, configuration, and
framework, see [issue #685][]). The `--configuration Linux` flag is
necessary to ensure that the preprocessor definition `LINUX` is
defined (see [issue #673][]).

```sh
dotnet restore
dotnet publish --output bin --configuration Linux src/Microsoft.PowerShell.Linux.Host
```

PowerShell and all necessary components should now be in the `bin` folder.

[issue #673]: https://github.com/PowerShell/PowerShell/issues/673
[issue #685]: https://github.com/PowerShell/PowerShell/issues/685
