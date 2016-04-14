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
| OS X 10.11   | [![Build Status](https://travis-ci.com/PowerShell/PowerShell.svg?token=31YifM4jfyVpBmEGitCm&branch=master)](https://travis-ci.com/PowerShell/PowerShell) |
| Windows      | [![Build status](https://ci.appveyor.com/api/projects/status/wb0a0apbn4aiccp1/branch/master?svg=true)](https://ci.appveyor.com/project/PowerShell/powershell-linux/branch/master) |

Get PowerShell
--------------

|                       | Linux | Windows .NET Core | Windows .NET Full | OS X | PSRP |
|-----------------------|-------|-------------------|-------------------|------|------|
| Build from **Source** | [Instructions](docs/building/linux.md) | [Instructions](docs/building/windows-core.md) | [Instructions](docs/building/windows-full.md) | [Instructions](docs/building/osx.md) | [Instructions][psrp] |
| Get **Binaries**      | [Releases][] | [Artifacts][] | [Artifacts][] | [Releases][] | TBD |

Building summary: `Start-PSBuild` from the module
`./PowerShellGitHubDev.psm1` (self-host on Linux / OS X)

See [Linux releases](docs/installation/linux.md) and
[Windows artifacts](docs/installation/windows.md) installation
instructions.

[releases]: https://github.com/PowerShell/PowerShell/releases
[artifacts]: https://ci.appveyor.com/project/PowerShell/powershell-linux/build/artifacts
[psrp]: https://github.com/PowerShell/psl-omi-provider

Team coordination
-----------------

- [PSCore Slack chat](https://pscore.slack.com/)
- [Waffle.io scrum board](https://waffle.io/PowerShell/PowerShell)

If you encounter any problems, see the [known issues](KNOWNISSUES.md),
search the [issues][], and if all else fails, open a new issue.

[issues]: https://github.com/PowerShell/PowerShell/issues

Obtain the source code
----------------------

### Setup Git

Install [Git][], the version control system.

See the [Contributing Guidelines](CONTRIBUTING.md) for more Git
information, such as our installation instructions, contributing
rules, and Git best practices.

[Git]: https://git-scm.com/documentation

### Download source code

Clone this repository. It is a "superproject" and has a number of
other repositories embedded within it as submodules. *Please* see the
contributing guidelines and learn about submodules. To make things
easy, we can just clone recursively.

```sh
git clone --recursive https://github.com/PowerShell/PowerShell.git
```

You can verify that the submodules were initialized properly with:

```sh
git submodule status
```

If they're initialized, it will look like this:

```
 f23641488f8d7bf8630ca3496e61562aa3a64009 src/Modules/Pester (f23641488)
 c99458533a9b4c743ed51537e25989ea55944908 src/libpsl-native/test/googletest (release-1.7.0)
 e6bf85694ae8352d77175c4c7d304946e018808c src/windows-build (monad/cc6afbeb-3/31)
```

If they're not, there will be minuses in front (and the folders will
be empty):

```
-f23641488f8d7bf8630ca3496e61562aa3a64009 src/Modules/Pester (f23641488)
-c99458533a9b4c743ed51537e25989ea55944908 src/libpsl-native/test/googletest (release-1.7.0)
-e6bf85694ae8352d77175c4c7d304946e018808c src/windows-build (monad/cc6afbeb-3/31)
```
