PowerShell
==========

Build Status
------------

| Platform     | `master` |
|--------------|----------|
| Ubuntu 14.04 | [![Build Status](https://travis-ci.com/PowerShell/PowerShell.svg?token=31YifM4jfyVpBmEGitCm&branch=master)](https://travis-ci.com/PowerShell/PowerShell) |
| Windows      | [![Build status](https://ci.appveyor.com/api/projects/status/wb0a0apbn4aiccp1/branch/master?svg=true)](https://ci.appveyor.com/project/PowerShell/powershell-linux/branch/master) |

Team coordination
-----------------

- [PSCore Slack chat](https://pscore.slack.com/)
- [Waffle.io scrum board](https://waffle.io/PowerShell/PowerShell)

## Obtain the source code

### Setup Git

Install [Git][], the version control system.

See the [Contributing Guidelines](CONTRIBUTING.md) for more Git
information, such as our installation instructions, contributing
rules, and Git best practices.

[Git]: https://git-scm.com/documentation

### Download source code

Clone this repository. It is a "superproject" and has a number of
other repositories embedded within it as submodules. *Please* see the
contributing guidelines and learn about submodules.

```sh
git clone https://github.com/PowerShell/PowerShell.git
cd PowerShell
```

#### Windows

On Windows, many fewer submodules are needed, so specify them:

```sh
git submodule update --init --recursive -- src/windows-build src/Microsoft.PowerShell.Linux.Host/Modules/Pester
```

## Setup build environment

### Windows

Tested on Windows 10 and Windows Server 2012 R2.

```powershell
Invoke-WebRequest -Uri https://raw.githubusercontent.com/dotnet/cli/rel/1.0.0/scripts/obtain/install.ps1 -OutFile install.ps1
./install.ps1 -version 1.0.0.001888
$env:Path += ";$env:LocalAppData\Microsoft\dotnet\cli
```

If you meet `Unable to cast COM object of type 'System.__ComObject' to
interface type 'Microsoft.Cci.ISymUnmanagedWriter5'`, please install
[Visual C++ Redistributable for Visual Studio 2015][redist].

[redist]: https://www.microsoft.com/en-hk/download/details.aspx?id=48145

## Building

**The command `dotnet restore` must be done at least once from the top directory
to obtain all the necessary .NET packages.**

`Start-PSBuild` from module `./PowerShellGitHubDev.psm1` on Windows
and Linux / OS X, if you are self-hosting PowerShell.

Specifically:

### Windows

In PowerShell:

```powershell
cd PowerShell
dotnet restore
Import-Module .\PowerShellGitHubDev.psm1
Start-PSBuild # build CoreCLR version
Start-PSBuild -FullCLR # build FullCLR version
```

**Tip:** use `Start-PSBuild -Verbose` switch to see more information
about build process.

## Running

If you encounter any problems, see the [known issues](KNOWNISSUES.md),
otherwise open a new issue on GitHub.

The local managed host has built-in documentation via `--help`.

### Windows

- launch `./bin/powershell.exe`
- run tests with `./bin/powershell.exe -c "Invoke-Pester test/powershell"`

## Debugging

To enable debugging on Linux, follow the installation instructions for
[Experimental .NET Core Debugging in VS Code][VS Code]. You will also
want to review their [detailed instructions][vscclrdebugger].

VS Code will place a `.vscode` directory in the PowerShell folder.
This contains the `launch.json` file, which you will customize using
the instructions below. You will also be prompted to create a
`tasks.json` file.

Currently, debugging supports attaching to a currently running
powershell process. Assuming you've created a `launch.json` file
correctly, within the "configuration" section, use the below settings:

```json
"configurations": [
    {
        "name": "powershell",
        "type": "coreclr",
        "request": "attach",
        "processName": "powershell"
    }
]
```

VS Code will now attach to a running `powershell` process. Start
powershell, then (in VS Code) press `F5` to begin the debugger.

[VS Code]: https://blogs.msdn.microsoft.com/visualstudioalm/2016/03/10/experimental-net-core-debugging-in-vs-code/
[vscclrdebugger]: http://aka.ms/vscclrdebugger

## PowerShell Remoting Protocol

PSRP communication is tunneled through OMI using the `omi-provider`.

> PSRP has been observed working on OS X, but the changes made to OMI to
> accomplish this are not even beta-ready and need to be done correctly. They
> exist on the `andschwa-osx` branch of the OMI repository.

PSRP support is *not* built automatically. See the detailed notes on
how to enable it.

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

## Detailed Build Script Notes

> This sections explains the build scripts.

The variable `$BIN` is the output directory, `bin`.

### PSRP

#### OMI

**PSRP support is not built by `./build.sh`**

To develop on the PowerShell Remoting Protocol (PSRP) for Linux, you'll need to
be able to compile OMI, which additionally requires:

```sh
sudo apt-get install libpam0g-dev libssl-dev libcurl4-openssl-dev libboost-filesystem-dev
```

Note that the OMI build steps can be done with `./omibuild.sh`.

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

### FullCLR PowerShell

On Windows, we also build Full PowerShell for .NET 4.5.1

#### Setup environment

* Install the Visual C++ Compiler via Visual Studio 2015.

This component is required to compile the native `powershell.exe` host.

This is an optionally installed component, so you may need to run the
Visual Studio installer again.

If you don't have any Visual Studio installed, you can use
[Visual Studio 2015 Community Edition][vs].

> Compiling with older versions should work, but we don't test it.

**Troubleshooting note:** If `cmake` says that it cannot determine the
`C` and `CXX` compilers, you either don't have Visual Studio, or you
don't have the Visual C++ Compiler component installed.

* Install CMake and add it to `PATH.`

You can install it from [Chocolatey][] or [manually][].

```
choco install cmake.portable
```

* Install .NET CLI via their [documentation][cli-docs]

[vs]: https://www.visualstudio.com/en-us/products/visual-studio-community-vs.aspx
[Chocolatey]: https://chocolatey.org/packages/cmake.portable
[manually]: https://cmake.org/download/

#### Building

```powershell
Start-PSBuild -FullCLR
```

**Troubleshooting:** the build logic is relatively simple and contains following steps:
- building managed DLLs: `dotnet publish --runtime net451`
- generating Visual Studio project: `cmake -G "$cmakeGenerator"`
- building `powershell.exe` from generated solution: `msbuild powershell.sln`

All this steps can be run separately from `Start-PSBuild`, don't
hesitate to experiment.

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
