# ![logo][] PowerShell

Welcome to the PowerShell GitHub Community!
PowerShell Core is a cross-platform (Windows, Linux, and macOS) automation and configuration tool/framework that works well with your existing tools and is optimized
for dealing with structured data (e.g. JSON, CSV, XML, etc.), REST APIs, and object models.
It includes a command-line shell, an associated scripting language and a framework for processing cmdlets.

[logo]: https://raw.githubusercontent.com/PowerShell/PowerShell/master/assets/ps_black_64.svg?sanitize=true

## Windows PowerShell vs PowerShell Core

Although this repository started as a fork of the Windows PowerShell code base, changes made in this repository do not make their way back to Windows PowerShell 5.1 automatically.
This also means that issues tracked here are only for PowerShell Core 6.
Windows PowerShell specific issues should be opened on [UserVoice][].

[UserVoice]: https://windowsserver.uservoice.com/forums/301869-powershell

## New to PowerShell?

If you are new to PowerShell and would like to learn more, we recommend reviewing the [getting started][] documentation.

[getting started]: https://github.com/PowerShell/PowerShell/tree/master/docs/learning-powershell

## Get PowerShell

You can download and install a PowerShell package for any of the following platforms.

| Supported Platform                         | Downloads (stable)      | Downloads (preview)   | How to Install                |
| -------------------------------------------| ------------------------| ----------------------| ------------------------------|
| [Windows (x64)][corefx-win]                | [.msi][rl-windows-64]   | [.msi][pv-windows-64] | [Instructions][in-windows]    |
| [Windows (x86)][corefx-win]                | [.msi][rl-windows-86]   | [.msi][pv-windows-86] | [Instructions][in-windows]    |
| [Ubuntu 18.04][corefx-linux]               |                         | [.deb][pv-ubuntu18]   | [Instructions][in-ubuntu18]   |
| [Ubuntu 16.04][corefx-linux]               | [.deb][rl-ubuntu16]     | [.deb][pv-ubuntu16]   | [Instructions][in-ubuntu16]   |
| [Ubuntu 14.04][corefx-linux]               | [.deb][rl-ubuntu14]     | [.deb][pv-ubuntu14]   | [Instructions][in-ubuntu14]   |
| [Debian 8.7+][corefx-linux]                | [.deb][rl-debian8]      | [.deb][pv-debian8]    | [Instructions][in-deb8]       |
| [Debian 9][corefx-linux]                   | [.deb][rl-debian9]      | [.deb][pv-debian9]    | [Instructions][in-deb9]       |
| [CentOS 7][corefx-linux]                   | [.rpm][rl-centos]       | [.rpm][pv-centos]     | [Instructions][in-centos]     |
| [Red Hat Enterprise Linux 7][corefx-linux] | [.rpm][rl-centos]       | [.rpm][pv-centos]     | [Instructions][in-rhel7]      |
| [OpenSUSE 42.2][corefx-linux]              | [.rpm][rl-centos]       | [.rpm][pv-centos]     | [Instructions][in-opensuse422]|
| [Fedora 27, Fedora 28][corefx-linux]       | [.rpm][rl-centos]       | [.rpm][pv-centos]     | [Instructions][in-fedora]     |
| [macOS 10.12+][corefx-macos]               | [.pkg][rl-macos]        | [.pkg][pv-macos]      | [Instructions][in-macos]      |
| Docker                                     |                         |                       | [Instructions][in-docker]     |

You can download and install a PowerShell package for any of the following platforms, **which are supported by the community.**

| Platform                 | Downloads (stable)      | Downloads (preview)           | How to Install                |
| -------------------------| ------------------------| ----------------------------- | ------------------------------|
| Arch Linux               |                         |                               | [Instructions][in-archlinux]  |
| Kali Linux               | [.deb][rl-ubuntu16]     | [.deb][pv-ubuntu16]           | [Instructions][in-kali]       |
| Many Linux distributions | [Snapcraft][rl-snap]    | [Snapcraft][pv-snap]          |                               |

You can also download the PowerShell binary archives for Windows, macOS and Linux.

| Platform                            | Downloads (stable)                               | Downloads (preview)                             | How to Install                                 |
| ------------------------------------| ------------------------------------------------ | ------------------------------------------------| -----------------------------------------------|
| Windows                             | [32-bit][rl-winx86-zip]/[64-bit][rl-winx64-zip]  | [32-bit][pv-winx86-zip]/[64-bit][pv-winx64-zip] | [Instructions][in-windows-zip]                 |
| macOS                               | [64-bit][rl-macos-tar]                           | [64-bit][pv-macos-tar]                          | [Instructions][in-tar-macos]                   |
| Linux                               | [64-bit][rl-linux-tar]                           | [64-bit][pv-linux-tar]                          | [Instructions][in-tar-linux]                   |
| Windows (arm) **Experimental**      | [32-bit][rl-winarm]/[64-bit][rl-winarm64]        | [32-bit][pv-winarm]/[64-bit][pv-winarm64]       | [Instructions][in-arm]                         |
| Raspbian (Stretch) **Experimental** | [.tgz][rl-raspbian]                              | [.tgz][pv-raspbian]                             | [Instructions][in-raspbian]                    |

[rl-windows-64]: https://github.com/PowerShell/PowerShell/releases/download/v6.0.4/PowerShell-6.0.4-win-x64.msi
[rl-windows-86]: https://github.com/PowerShell/PowerShell/releases/download/v6.0.4/PowerShell-6.0.4-win-x86.msi
[rl-ubuntu16]: https://github.com/PowerShell/PowerShell/releases/download/v6.0.4/powershell_6.0.4-1.ubuntu.16.04_amd64.deb
[rl-ubuntu14]: https://github.com/PowerShell/PowerShell/releases/download/v6.0.4/powershell_6.0.4-1.ubuntu.14.04_amd64.deb
[rl-debian8]: https://github.com/PowerShell/PowerShell/releases/download/v6.0.4/powershell_6.0.4-1.debian.8_amd64.deb
[rl-debian9]: https://github.com/PowerShell/PowerShell/releases/download/v6.0.4/powershell_6.0.4-1.debian.9_amd64.deb
[rl-centos]: https://github.com/PowerShell/PowerShell/releases/download/v6.0.4/powershell-6.0.4-1.rhel.7.x86_64.rpm
[rl-macos]: https://github.com/PowerShell/PowerShell/releases/download/v6.0.4/powershell-6.0.4-osx.10.12-x64.pkg
[rl-winarm]: https://github.com/PowerShell/PowerShell/releases/download/v6.0.4/PowerShell-6.0.4-win-arm32.zip
[rl-winarm64]: https://github.com/PowerShell/PowerShell/releases/download/v6.0.4/PowerShell-6.0.4-win-arm64.zip
[rl-winx86-zip]: https://github.com/PowerShell/PowerShell/releases/download/v6.0.4/PowerShell-6.0.4-win-x86.zip
[rl-winx64-zip]: https://github.com/PowerShell/PowerShell/releases/download/v6.0.4/PowerShell-6.0.4-win-x64.zip
[rl-macos-tar]: https://github.com/PowerShell/PowerShell/releases/download/v6.0.4/powershell-6.0.4-osx-x64.tar.gz
[rl-linux-tar]: https://github.com/PowerShell/PowerShell/releases/download/v6.0.4/powershell-6.0.4-linux-x64.tar.gz
[rl-raspbian]: https://github.com/PowerShell/PowerShell/releases/download/v6.0.4/powershell-6.0.4-linux-arm32.tar.gz
[rl-snap]: https://snapcraft.io/powershell

[pv-windows-64]: https://github.com/PowerShell/PowerShell/releases/download/v6.1.0-rc.1/PowerShell-6.1.0-rc.1-win-x64.msi
[pv-windows-86]: https://github.com/PowerShell/PowerShell/releases/download/v6.1.0-rc.1/PowerShell-6.1.0-rc.1-win-x86.msi
[pv-ubuntu18]: https://github.com/PowerShell/PowerShell/releases/download/v6.1.0-rc.1/powershell-preview_6.1.0.rc.1-1.ubuntu.18.04_amd64.deb
[pv-ubuntu16]: https://github.com/PowerShell/PowerShell/releases/download/v6.1.0-rc.1/powershell-preview_6.1.0.rc.1-1.ubuntu.16.04_amd64.deb
[pv-ubuntu14]: https://github.com/PowerShell/PowerShell/releases/download/v6.1.0-rc.1/powershell-preview_6.1.0.rc.1-1.ubuntu.14.04_amd64.deb
[pv-debian8]: https://github.com/PowerShell/PowerShell/releases/download/v6.1.0-rc.1/powershell-preview_6.1.0.rc.1-1.debian.8_amd64.deb
[pv-debian9]: https://github.com/PowerShell/PowerShell/releases/download/v6.1.0-rc.1/powershell-preview_6.1.0.rc.1-1.debian.9_amd64.deb
[pv-centos]: https://github.com/PowerShell/PowerShell/releases/download/v6.1.0-rc.1/powershell-preview-6.1.0.rc.1-1.rhel.7.x86_64.rpm
[pv-macos]: https://github.com/PowerShell/PowerShell/releases/download/v6.1.0-rc.1/powershell-6.1.0-rc.1-osx-x64.pkg
[pv-winarm]: https://github.com/PowerShell/PowerShell/releases/download/v6.1.0-rc.1/PowerShell-6.1.0-rc.1-win-arm32.zip
[pv-winarm64]: https://github.com/PowerShell/PowerShell/releases/download/v6.1.0-rc.1/PowerShell-6.1.0-rc.1-win-arm64.zip
[pv-winx86-zip]: https://github.com/PowerShell/PowerShell/releases/download/v6.1.0-rc.1/PowerShell-6.1.0-rc.1-win-x86.zip
[pv-winx64-zip]: https://github.com/PowerShell/PowerShell/releases/download/v6.1.0-rc.1/PowerShell-6.1.0-rc.1-win-x64.zip
[pv-macos-tar]: https://github.com/PowerShell/PowerShell/releases/download/v6.1.0-rc.1/powershell-6.1.0-rc.1-osx-x64.tar.gz
[pv-linux-tar]: https://github.com/PowerShell/PowerShell/releases/download/v6.1.0-rc.1/powershell-6.1.0-rc.1-linux-x64.tar.gz
[pv-raspbian]: https://github.com/PowerShell/PowerShell/releases/download/v6.1.0-rc.1/powershell-6.1.0-rc.1-linux-arm32.tar.gz
[pv-snap]: https://snapcraft.io/powershell-preview

[in-windows]: https://docs.microsoft.com/powershell/scripting/setup/installing-powershell-core-on-windows?view=powershell-6
[in-ubuntu14]: https://docs.microsoft.com/powershell/scripting/setup/installing-powershell-core-on-linux?view=powershell-6#ubuntu-1404
[in-ubuntu16]: https://docs.microsoft.com/powershell/scripting/setup/installing-powershell-core-on-linux?view=powershell-6#ubuntu-1604
[in-ubuntu18]: https://docs.microsoft.com/powershell/scripting/setup/installing-powershell-core-on-linux?view=powershell-6#ubuntu-1804
[in-deb8]: https://docs.microsoft.com/powershell/scripting/setup/installing-powershell-core-on-linux?view=powershell-6#debian-8
[in-deb9]: https://docs.microsoft.com/powershell/scripting/setup/installing-powershell-core-on-linux?view=powershell-6#debian-9
[in-centos]: https://docs.microsoft.com/powershell/scripting/setup/installing-powershell-core-on-linux?view=powershell-6#centos-7
[in-rhel7]: https://docs.microsoft.com/powershell/scripting/setup/installing-powershell-core-on-linux?view=powershell-6#red-hat-enterprise-linux-rhel-7
[in-opensuse422]: https://docs.microsoft.com/powershell/scripting/setup/installing-powershell-core-on-linux?view=powershell-6#opensuse-422
[in-fedora]: https://docs.microsoft.com/powershell/scripting/setup/installing-powershell-core-on-linux?view=powershell-6#fedora
[in-archlinux]: https://docs.microsoft.com/powershell/scripting/setup/installing-powershell-core-on-linux?view=powershell-6#arch-linux
[in-macos]: https://docs.microsoft.com/powershell/scripting/setup/installing-powershell-core-on-macos?view=powershell-6
[in-docker]: https://github.com/PowerShell/PowerShell-Docker
[in-kali]: https://docs.microsoft.com/powershell/scripting/setup/installing-powershell-core-on-linux?view=powershell-6#kali
[in-windows-zip]: https://docs.microsoft.com/powershell/scripting/setup/installing-powershell-core-on-windows?view=powershell-6#zip
[in-tar-linux]: https://docs.microsoft.com/powershell/scripting/setup/installing-powershell-core-on-linux?view=powershell-6#binary-archives
[in-tar-macos]: https://docs.microsoft.com/powershell/scripting/setup/installing-powershell-core-on-macos?view=powershell-6#binary-archives
[in-raspbian]: https://docs.microsoft.com/powershell/scripting/setup/installing-powershell-core-on-linux?view=powershell-6#raspbian
[in-arm]: https://docs.microsoft.com/powershell/scripting/setup/powershell-core-on-arm?view=powershell-6
[corefx-win]:https://github.com/dotnet/core/blob/master/release-notes/2.0/2.0-supported-os.md#windows
[corefx-linux]:https://github.com/dotnet/core/blob/master/release-notes/2.0/2.0-supported-os.md#linux
[corefx-macos]:https://github.com/dotnet/core/blob/master/release-notes/2.0/2.0-supported-os.md#macos

To install a specific version, visit [releases](https://github.com/PowerShell/PowerShell/releases).

## Community Dashboard

[Dashboard](https://aka.ms/psgithubbi) with visualizations for community contributions and project status using PowerShell, Azure, and PowerBI.

For more information on how and why we built this dashboard, check out this [blog post](https://blogs.msdn.microsoft.com/powershell/2017/01/31/powershell-open-source-community-dashboard/).

## Chat Room

Want to chat with other members of the PowerShell community?

We have a Gitter Room which you can join below.

[![Join the chat at https://gitter.im/PowerShell/PowerShell](https://badges.gitter.im/PowerShell/PowerShell.svg)](https://gitter.im/PowerShell/PowerShell?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

There is also the community driven PowerShell Slack Team which you can sign up for at [Slack Sign up].

[Slack Sign up]: http://slack.poshcode.org

## Add-ons and libraries

[Awesome PowerShell](https://github.com/janikvonrotz/awesome-powershell) is a great curated list of add-ons and resources.

## Building the Repository

| Linux                    | Windows                    | macOS                   |
|--------------------------|----------------------------|------------------------|
| [Instructions][bd-linux] | [Instructions][bd-windows] | [Instructions][bd-macOS] |

If you have any problems building, please consult the developer [FAQ][].

### Build status of master branches

| AppVeyor (Windows)       | Travis CI (Linux / macOS) |
|--------------------------|--------------------------|
| [![av-image][]][av-site] | [![tv-image][]][tv-site] |

### Build status of nightly builds

| AppVeyor (Windows)       | Travis CI (Linux) | Travis CI (macOS) | Code Coverage Status | Code Factor Status |
|--------------------------|-------------------|-------------------|----------------------|--------------------|
| [![av-nightly-image][]][av-nightly-site] | [![linux-nightly-image][]][tv-site] | [![macOS-nightly-image][]][tv-site] | [![cc-image][]][cc-site] | [![cf-image][]][cf-site] |

[bd-linux]: https://github.com/PowerShell/PowerShell/tree/master/docs/building/linux.md
[bd-windows]: https://github.com/PowerShell/PowerShell/tree/master/docs/building/windows-core.md
[bd-macOS]: https://github.com/PowerShell/PowerShell/tree/master/docs/building/macos.md

[FAQ]: https://github.com/PowerShell/PowerShell/tree/master/docs/FAQ.md

[tv-image]: https://travis-ci.org/PowerShell/PowerShell.svg?branch=master
[tv-site]: https://travis-ci.org/PowerShell/PowerShell/branches
[av-image]: https://ci.appveyor.com/api/projects/status/nsng9iobwa895f98/branch/master?svg=true
[av-site]: https://ci.appveyor.com/project/PowerShell/powershell
[linux-nightly-image]: https://jimtru1979.blob.core.windows.net/badges/DailyBuildStatus.Linux.svg
[macOS-nightly-image]: https://jimtru1979.blob.core.windows.net/badges/DailyBuildStatus.OSX.svg
[av-nightly-image]: https://ci.appveyor.com/api/projects/status/46yd4jogtm2jodcq?svg=true
[av-nightly-site]: https://ci.appveyor.com/project/PowerShell/powershell-f975h
[cc-site]: https://codecov.io/gh/PowerShell/PowerShell
[cc-image]: https://codecov.io/gh/PowerShell/PowerShell/branch/master/graph/badge.svg
[cf-site]: https://www.codefactor.io/repository/github/powershell/powershell
[cf-image]: https://www.codefactor.io/repository/github/powershell/powershell/badge

## Downloading the Source Code

The PowerShell repository has a number of other repositories embedded as submodules.

To make things easy, you can just clone recursively:

```sh
git clone --recursive https://github.com/PowerShell/PowerShell.git
```

If you already cloned but forgot to use `--recursive`, you can update submodules manually:

```sh
git submodule update --init
```

See [working with the PowerShell repository](https://github.com/PowerShell/PowerShell/tree/master/docs/git) for more information.

## Developing and Contributing

Please see the [Contribution Guide][] for how to develop and contribute.

If you have any problems, please consult the [known issues][], developer [FAQ][], and [GitHub issues][].
If you do not see your problem captured, please file a [new issue][] and follow the provided template.
If you are developing .NET Core C# applications targeting PowerShell Core, please [check out our FAQ][] to learn more about the PowerShell SDK NuGet package.

Also make sure to check out our [PowerShell-RFC repository](https://github.com/powershell/powershell-rfc) for request-for-comments (RFC) documents to submit and give comments on proposed and future designs.

[check out our FAQ]: https://github.com/PowerShell/PowerShell/tree/master/docs/FAQ.md#where-do-i-get-the-powershell-core-sdk-package
[Contribution Guide]: https://github.com/PowerShell/PowerShell/tree/master/.github/CONTRIBUTING.md
[known issues]: https://docs.microsoft.com/powershell/scripting/whats-new/known-issues-ps6?view=powershell-6
[GitHub issues]: https://github.com/PowerShell/PowerShell/issues
[new issue]:https://github.com/PowerShell/PowerShell/issues/new

## Legal and Licensing

PowerShell is licensed under the [MIT license][].

[MIT license]: https://github.com/PowerShell/PowerShell/tree/master/LICENSE.txt

### Windows Docker Files and Images

License: By requesting and using the Container OS Image for Windows containers, you acknowledge, understand, and consent to the Supplemental License Terms available on Docker hub:

- [Window Server Core](https://hub.docker.com/r/microsoft/windowsservercore/)
- [Nano Server](https://hub.docker.com/r/microsoft/nanoserver/)

### Telemetry

By default, PowerShell collects the OS description and the version of PowerShell (equivalent to `$PSVersionTable.OS` and `$PSVersionTable.GitCommitId`) using [Application Insights](https://azure.microsoft.com/services/application-insights/).
To opt-out of sending telemetry, create an environment variable called `POWERSHELL_TELEMETRY_OPTOUT` set to a value of `1` before starting PowerShell from the installed location.
The telemetry we collect fall under the [Microsoft Privacy Statement](https://privacy.microsoft.com/privacystatement/).

## Governance

Governance policy for PowerShell project is described [here][].

[here]: https://github.com/PowerShell/PowerShell/blob/master/docs/community/governance.md

## [Code of Conduct][conduct-md]

This project has adopted the [Microsoft Open Source Code of Conduct][conduct-code].
For more information see the [Code of Conduct FAQ][conduct-FAQ] or contact [opencode@microsoft.com][conduct-email] with any additional questions or comments.

[conduct-code]: http://opensource.microsoft.com/codeofconduct/
[conduct-FAQ]: http://opensource.microsoft.com/codeofconduct/faq/
[conduct-email]: mailto:opencode@microsoft.com
[conduct-md]: https://github.com/PowerShell/PowerShell/tree/master/./CODE_OF_CONDUCT.md
