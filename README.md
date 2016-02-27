# PowerShell on Linux / OS X / Windows

|         |Ubuntu 14.04 |Windows |
|---------|:------:|:------:|
|master|[![Build Status](https://travis-ci.com/PowerShell/PowerShell.svg?token=31YifM4jfyVpBmEGitCm&branch=master)](https://travis-ci.com/PowerShell/PowerShell)|[![Build status](https://ci.appveyor.com/api/projects/status/wb0a0apbn4aiccp1/branch/master?svg=true)](https://ci.appveyor.com/project/PowerShell/powershell-linux/branch/master)|

## [Waffle.io scrum board](https://waffle.io/PowerShell/PowerShell)

## Obtain the source code

### Setup Git

Install [Git][], the version control system.

```sh
sudo apt-get install git
```

If you do not have a preferred method of authentication, enable the storage
credential helper, which will cache your credentials in plaintext on your
system, so use a [token][].

```sh
git config --global credential.helper store
```

See the [Contributing Guidelines](CONTRIBUTING.md) for more Git information.

[Git]: https://git-scm.com/documentation
[token]: https://help.github.com/articles/creating-an-access-token-for-command-line-use/

### Download source code

Clone this repository recursively, as it's the superproject with a number of
submodules.

```sh
git clone --recursive https://github.com/PowerShell/PowerShell.git
```

The `src/omi` submodule requires your GitHub user to have joined the Microsoft
organization. If it fails to check out, Git will bail and not check out further
submodules either. Please follow the instructions on the [Open Source Hub][].

On Windows, many fewer submodules are needed, so don't use `clone --recursive`.

Instead run:

```
git clone https://github.com/PowerShell/PowerShell.git
git submodule update --init --recursive -- src/monad src/windows-build src/Microsoft.PowerShell.Linux.Host/Modules/Pester
```

[Open Source Hub]: https://opensourcehub.microsoft.com/articles/how-to-join-microsoft-github-org-self-service

## Setup build environment

We use the [.NET Command Line Interface][dotnet] (`dotnet`) to build
the managed components, and [CMake][] to build the native components (on
non-Windows platforms). Install `dotnet` by following their [documentation][].

The version of .NET CLI is very important, you want a recent 1.0.0 beta
(**not** 1.0.1). The following instructions will install precisely
1.0.0.001517, though any 1.0.0 version *should* work.

> Previous installations of DNX, `dnvm`, or older installations of .NET CLI
> can cause odd failures when running. Please check your version.

[dotnet]: https://github.com/dotnet/cli#new-to-net-cli
[documentation]: https://dotnet.github.io/getting-started/
[CMake]: https://cmake.org/cmake/help/v2.8.12/cmake.html

### Linux

Tested on Ubuntu 14.04.

```sh
sudo sh -c 'echo "deb [arch=amd64] http://apt-mo.trafficmanager.net/repos/dotnet/ trusty main" > /etc/apt/sources.list.d/dotnetdev.list'
sudo apt-key adv --keyserver apt-mo.trafficmanager.net --recv-keys 417A0893
sudo apt-get update
sudo apt-get install dotnet=1.0.0.001540-1
```

Then install the following additional build / debug tools:

```sh
sudo apt-get install g++ cmake make lldb-3.6 strace
```

#### OMI

To develop on the PowerShell Remoting Protocol (PSRP) for Linux, you'll need to
be able to compile OMI, which additionally requires:

```sh
sudo apt-get install libpam0g-dev libssl-dev libcurl4-openssl-dev libboost-filesystem-dev
```

### Windows

Tested on Windows 10 and Windows Server 2012 R2.

An MSI installer also exists, but this script avoids touching your system.

```powershell
Invoke-WebRequest -Uri https://raw.githubusercontent.com/dotnet/cli/rel/1.0.0/scripts/obtain/install.ps1 -OutFile install.ps1
./install.ps1 -version 1.0.0.001540 -channel beta
```

Note:
>If you meet "Unable to cast COM object of type 'System.__ComObject' to interface type 'Microsoft.Cci.ISymUnmanagedWriter5'", please install [Visual C++ Redistributable for Visual Studio 2015].

[Visual C++ Redistributable for Visual Studio 2015]:https://www.microsoft.com/en-hk/download/details.aspx?id=48145

### OS X

The OS X dependency installation instructions are not yet documented. You can
try their PKG installer, or their [obtain script][]. We do not (yet) routinely
test on OS X, but some developers use PowerShell on 10.10 and 10.11.

[obtain script]: https://github.com/dotnet/cli/blob/rel/1.0.0/scripts/obtain/install.sh

## Building

**The command `dotnet restore` must be done at least once from the top directory
to obtain all the necessary .NET packages.**

Build with `./build.sh` on Linux and OS X, `./build.ps1` for Core PowerShell on
Windows, and `./build.FullCLR.ps1` for Full PowerShell on Windows.

Specifically:

### Linux

In Bash:
```sh
cd PowerShell
dotnet restore
./build.sh
```

### Windows

In PowerShell:
```powershell
cd PowerShell
dotnet restore
./build.ps1
```

## Running

If you encounter any problems, see the [known issues](KNOWNISSUES.md),
otherwise open a new issue on GitHub.

The local managed host has built-in documentation via `--help`.

### Linux / OS X

- launch local shell with `./bin/powershell`
- run tests with `./pester.sh`

### Windows

- launch `./bin/powershell.exe`
- run tests with `./bin/powershell.exe -c "Invoke-Pester test/powershell"`

## PowerShell Remoting Protocol

PSRP communication is tunneled through OMI using the `omi-provider`.

> PSRP has been observed working on OS X, but the changes made to OMI to
> accomplish this are not even beta-ready and need to be done correctly. They
> exist on the `andschwa-osx` branch of the OMI repository.

### Building

**PSRP support is not built by `./build.sh`**

Build with `./omibuild.sh`.

### Running

Some initial setup on Windows is required. Open an administrative command
prompt and execute the following:

```cmd
winrm set winrm/config/Client @{AllowUnencrypted="true"}
winrm set winrm/config/Client @{TrustedHosts="*"}
```

> You can also set the `TrustedHosts` to include the target's IP address.

Then on Linux, launch `omiserver` in the debugger (after building with the
instructions above):

```sh
./psrp.sh
run
```

> The `run` command is executed inside of LLDB (the debugger) to start the
`omiserver` process.

Now in a PowerShell prompt on Windows (opened after setting the WinRM client
configurations):

```powershell
Enter-PSSession -ComputerName <IP address of Linux machine> -Credential $cred -Authentication basic
```

> The `$cred` variable can be empty; a credentials prompt will appear, enter
> any fake credentials you wish as authentication is not yet implemented.

The IP address of the Linux machine can be obtained with:

```sh
ip -f inet addr show dev eth0
```

### Desired State Configuration

> DSC support is in its infancy.

DSC also uses OMI, so build it first, then build DSC against it. Unfortunately,
DSC cannot be configured to look for OMI elsewhere, so for now you need to
symlink it to the expected location.

```sh
ln -s ../omi/Unix/ omi-1.0.8
./configure --no-rpm --no-dpkg --local
make -j
```

## Detailed Build Script Notes

> This sections explains the build scripts.

The variable `$BIN` is the output directory, `bin`.

### Managed

Builds with `dotnet`. Publishes all dependencies into the `bin` directory.
Emits its own native host as `bin/powershell`. Uses a `Linux` configuration to
add a preprocessor definition. The `CORECLR` definition is added only when
targeting the `netstandard1.5` framework. The `LINUX` definition is added only
when `--configuration Linux` is used.

```sh
cd src/Microsoft.PowerShell.Linux.Host
dotnet publish --configuration Linux
```

### Native

The `libpsl-native.so` library consists of native functions that
`CorePsPlatform.cs` P/Invokes.

#### libpsl-native

Driven by CMake, with its own unit tests using Google Test.

```sh
cd src/libpsl-native
cmake -DCMAKE_BUILD_TYPE=Debug .
make -j
ctest -V
# Deploy development copy of libpsl-native
cp native/libpsl-native.* $BIN
```

The output is a `.so` on Linux and `.dylib` on OS X. It is unnecessary for Windows.

### PSRP

#### OMI

Build OMI from source in developer mode:

```sh
cd src/omi/Unix
./configure --dev
make -j
```

#### Provider

The provider uses CMake to build, link, and register with OMI.

```sh
cd src/omi-provider
cmake .
make -j
```

The provider also maintains its own native host library to initialize the CLR,
but there are plans to refactor .NET's packaged host as a shared library.

# FullCLR PowerShell

On Windows, we also build Full PowerShell for .NET 4.5.1

## Setup environment

* You need Visual Studio to compile the native host `powershell.exe`.

If you don't have any visual studio installed, you can use [Visual Studio 2013
Community edition][vs].

* Add `msbuild` to `PATH` / create PowerShell alias to it.

```powershell
Set-Alias msbuild C:\WINDOWS\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe
```

* Install CMake and add it to `PATH.`

You can install it from [Chocolatey][] or [manually][].

```
choco install cmake.portable
```

* Install dotnet-cli via their [documentation][]

[vs]: https://www.visualstudio.com/en-us/news/vs2013-community-vs.aspx
[chocolately]: https://chocolatey.org/packages/cmake.portable
[manually]: https://cmake.org/download/

## Building

```powershell
.\build.FullCLR.ps1
```

**Troubleshooting:** the build logic is relatively simple and contains following steps:
- building managed DLLs: `dotnet publish --runtime dnx451`
- generating Visual Studio project: `cmake -G "$cmakeGenerator"`
- building `powershell.exe` from generated solution: `msbuild powershell.sln`

All this steps can be run separately from `.\build.FullCLR.ps1`, don't hesitate
to experiment.

## Running

Running FullCLR version is not as simple as CoreCLR version.

If you just run ~~`.\binFull\powershell.exe`~~, you will get a `powershell`
process, but all the interesting DLLs (i.e. `System.Management.Automation.dll`)
would be loaded from the GAC, not your `binFull` build directory.

[@lzybkr](https://github.com/lzybkr) wrote a module to deal with it and run
side-by-side.

```powershell
Import-Module .\PowerShellGithubDev.psm1
Start-DevPSGithub -binDir $pwd\binFull
```

**Troubleshooting:** default for `powershell.exe` that **we build** is x86.

There is a separate execution policy registry key for x86, and it's likely that
you didn't ~~bypass~~ enable it. From **powershell.exe (x86)** run:

```
Set-ExecutionPolicy Bypass
```

## Running from CI server

We publish an archive with FullCLR bits on every CI build with [AppVeyor][].

* Download zip package from **artifacts** tab of the particular build.
* Unblock zip file: right-click in file explorer -> properties -> check
  'Unblock' checkbox -> apply
* Extract zip file to `$bin` directory
* `Start-DevPSGithub -binDir $bin`

[appveyor]: https://ci.appveyor.com/project/PowerShell/powershell-linux
