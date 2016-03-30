PowerShell
==========

This repository is "Project Magrathea": Open PowerShell on GitHub, for
Linux, Windows (.NET Core and Full), and OS X. It is built using the
[.NET Command Line Interface][dotnet-cli] to support targetting every
flavor of PowerShell. It is a collaborative effort among many teams:

- Full PowerShell
- Core PowerShell
- Open Source Technology Center
- .NET Foundation

[dotnet-cli]: https://github.com/dotnet/cli

Build Status
------------

| Platform     | `master` |
|--------------|----------|
| Ubuntu 14.04 | [![Build Status](https://travis-ci.com/PowerShell/PowerShell.svg?token=31YifM4jfyVpBmEGitCm&branch=master)](https://travis-ci.com/PowerShell/PowerShell) |
| Windows      | [![Build status](https://ci.appveyor.com/api/projects/status/wb0a0apbn4aiccp1/branch/master?svg=true)](https://ci.appveyor.com/project/PowerShell/powershell-linux/branch/master) |

Get PowerShell
--------------

|                       | Linux | Windows .NET Core | Windows .NET Full | OS X |
|-----------------------|-------|-------------------|-------------------|------|
| Build from **Source** | [Instructions](docs/building/linux.md) | [Instructions](docs/building/windows-core.md) | [Instructions](docs/building/windows-full.md) | [Instructions](docs/building/osx.md) |
| Get **Binaries**      | [Releases][] | [Artifacts][] | [Artifacts][] | [Releases][] |

Building summary: `Start-PSBuild` from the module
`./PowerShellGitHubDev.psm1` (self-host on Linux / OS X)

[releases]: https://github.com/PowerShell/PowerShell/releases
[artifacts]: https://ci.appveyor.com/project/PowerShell/powershell-linux/build/artifacts

Team coordination
-----------------

- [PSCore Slack chat](https://pscore.slack.com/)
- [Waffle.io scrum board](https://waffle.io/PowerShell/PowerShell)

If you encounter any problems, see the [known issues](KNOWNISSUES.md),
search the [issues][], and if all else fails, open a new issue.

[issues]: https://github.com/PowerShell/PowerShell/issues

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
contributing guidelines and learn about submodules. Not every
submodule is required on every system; see the individual build
instructions for the necessary subsets.

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

## Running from CI server

We publish an archive with FullCLR bits on every CI build with [AppVeyor][].

* Download zip package from **artifacts** tab of the particular build.
* Unblock zip file: right-click in file explorer -> properties -> check
  'Unblock' checkbox -> apply
* Extract zip file to `$bin` directory
* `Start-DevPSGithub -binDir $bin`

[appveyor]: https://ci.appveyor.com/project/PowerShell/powershell-linux
