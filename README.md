# ![logo][] PowerShell

Welcome to the PowerShell GitHub Community!
PowerShell Core is a cross-platform (Windows, Linux, and macOS) automation and configuration tool/framework that works well with your existing tools and is optimized
for dealing with structured data (e.g. JSON, CSV, XML, etc.), REST APIs, and object models.
It includes a command-line shell, an associated scripting language and a framework for processing cmdlets.

[logo]: https://raw.githubusercontent.com/PowerShell/PowerShell/master/assets/Powershell_black_64.png

## Windows PowerShell vs PowerShell Core

Although this repo started as a fork of the Windows PowerShell code base, changes made in this repo do not make their way back to Windows PowerShell 5.1 automatically.
This also means that issues tracked here are only for PowerShell Core 6.0.
Windows PowerShell specific issues should be opened on [UserVoice][].

[UserVoice]: https://windowsserver.uservoice.com/forums/301869-powershell

## New to PowerShell?

If you are new to PowerShell and would like to learn more, we recommend reviewing the [getting started][] documentation.

[getting started]: https://github.com/PowerShell/PowerShell/tree/master/docs/learning-powershell

## Get PowerShell

You can download and install a PowerShell package for any of the following platforms.

| Platform                           | Downloads              | How to Install                |
| ---------------------------------- | ---------------------- | ----------------------------- |
| Windows (x64)                      | [.msi][rl-windows-64]  | [Instructions][in-windows]    |
| Windows (x86)                      | [.msi][rl-windows-86]  | [Instructions][in-windows]    |
| Ubuntu 17.04                       | [.deb][rl-ubuntu17]    | [Instructions][in-ubuntu17]   |
| Ubuntu 16.04                       | [.deb][rl-ubuntu16]    | [Instructions][in-ubuntu16]   |
| Ubuntu 14.04                       | [.deb][rl-ubuntu14]    | [Instructions][in-ubuntu14]   |
| Debian 8                           | [.deb][rl-debian8]     | [Instructions][in-deb8]       |
| Debian 9                           | [.deb][rl-debian9]     | [Instructions][in-deb9]       |
| CentOS 7                           | [.rpm][rl-centos]      | [Instructions][in-centos]     |
| Red Hat Enterprise Linux 7         | [.rpm][rl-centos]      | [Instructions][in-rhel7]      |
| OpenSUSE 42.2                      | [.rpm][rl-centos]      | [Instructions][in-opensuse422]|
| Fedora 25                          | [.rpm][rl-centos]      | [Instructions][in-fedora25]   |
| Fedora 26                          | [.rpm][rl-centos]      | [Instructions][in-fedora26]   |
| Arch Linux                         |                        | [Instructions][in-archlinux]  |
| Many Linux distributions           | [.AppImage][rl-ai]     | [Instructions][in-appimage]   |
| macOS 10.12                        | [.pkg][rl-macos]       | [Instructions][in-macos]      |
| Docker                             |                        | [Instructions][in-docker]     |
| Kali Linux                         | [.deb][rl-ubuntu16]    | [Instructions][in-kali]       |

You can also download the PowerShell binary archives for Windows, macOS and Linux.

| Platform     | Downloads                                       | How to Install                 |
| ------------ | ----------------------------------------------- | ------------------------------ |
| Windows      | [32-bit][rl-winx86-zip]/[64-bit][rl-winx64-zip] | [Instructions][in-windows-zip] |
| macOS        | [64-bit][rl-macos-tar]                          | [Instructions][in-tar]   |
| Linux        | [64-bit][rl-linux-tar]                          | [Instructions][in-tar]   |

[rl-windows-64]: https://github.com/PowerShell/PowerShell/releases/download/v6.0.0-beta.9/PowerShell-6.0.0-beta.9-win-x64.msi
[rl-windows-86]: https://github.com/PowerShell/PowerShell/releases/download/v6.0.0-beta.9/PowerShell-6.0.0-beta.9-win-x86.msi
[rl-ubuntu17]: https://github.com/PowerShell/PowerShell/releases/download/v6.0.0-beta.9/powershell_6.0.0-beta.9-1.ubuntu.17.04_amd64.deb
[rl-ubuntu16]: https://github.com/PowerShell/PowerShell/releases/download/v6.0.0-beta.9/powershell_6.0.0-beta.9-1.ubuntu.16.04_amd64.deb
[rl-ubuntu14]: https://github.com/PowerShell/PowerShell/releases/download/v6.0.0-beta.9/powershell_6.0.0-beta.9-1.ubuntu.14.04_amd64.deb
[rl-debian8]: https://github.com/PowerShell/PowerShell/releases/download/v6.0.0-beta.9/powershell_6.0.0-beta.9-1.debian.8_amd64.deb
[rl-debian9]: https://github.com/PowerShell/PowerShell/releases/download/v6.0.0-beta.9/powershell_6.0.0-beta.9-1.debian.9_amd64.deb
[rl-centos]: https://github.com/PowerShell/PowerShell/releases/download/v6.0.0-beta.9/powershell-6.0.0_beta.9-1.rhel.7.x86_64.rpm
[rl-ai]: https://github.com/PowerShell/PowerShell/releases/download/v6.0.0-beta.9/PowerShell-6.0.0-beta.9-x86_64.AppImage
[rl-macos]: https://github.com/PowerShell/PowerShell/releases/download/v6.0.0-beta.9/powershell-6.0.0-beta.9-osx.10.12-x64.pkg
[rl-winx86-zip]: https://github.com/PowerShell/PowerShell/releases/download/v6.0.0-beta.9/PowerShell-6.0.0-beta.9-win-x86.zip
[rl-winx64-zip]: https://github.com/PowerShell/PowerShell/releases/download/v6.0.0-beta.9/PowerShell-6.0.0-beta.9-win-x64.zip
[rl-macos-tar]: https://github.com/PowerShell/PowerShell/releases/download/v6.0.0-beta.9/powershell-6.0.0-beta.9-osx-x64.tar.gz
[rl-linux-tar]: https://github.com/PowerShell/PowerShell/releases/download/v6.0.0-beta.9/powershell-6.0.0-beta.9-linux-x64.tar.gz

[installation]: https://github.com/PowerShell/PowerShell/tree/master/docs/installation
[in-windows]: https://github.com/PowerShell/PowerShell/tree/master/docs/installation/windows.md#msi
[in-ubuntu14]: https://github.com/PowerShell/PowerShell/tree/master/docs/installation/linux.md#ubuntu-1404
[in-ubuntu16]: https://github.com/PowerShell/PowerShell/tree/master/docs/installation/linux.md#ubuntu-1604
[in-ubuntu17]: https://github.com/PowerShell/PowerShell/tree/master/docs/installation/linux.md#ubuntu-1704
[in-deb8]: https://github.com/PowerShell/PowerShell/tree/master/docs/installation/linux.md#debian-8
[in-deb9]: https://github.com/PowerShell/PowerShell/tree/master/docs/installation/linux.md#debian-9
[in-centos]: https://github.com/PowerShell/PowerShell/tree/master/docs/installation/linux.md#centos-7
[in-rhel7]: https://github.com/PowerShell/PowerShell/tree/master/docs/installation/linux.md#red-hat-enterprise-linux-rhel-7
[in-opensuse422]: https://github.com/PowerShell/PowerShell/tree/master/docs/installation/linux.md#opensuse-422
[in-fedora25]: https://github.com/PowerShell/PowerShell/tree/master/docs/installation/linux.md#fedora-25
[in-fedora26]: https://github.com/PowerShell/PowerShell/tree/master/docs/installation/linux.md#fedora-26
[in-archlinux]: https://github.com/PowerShell/PowerShell/tree/master/docs/installation/linux.md#arch-linux
[in-appimage]: https://github.com/PowerShell/PowerShell/tree/master/docs/installation/linux.md#linux-appimage
[in-macos]: https://github.com/PowerShell/PowerShell/tree/master/docs/installation/linux.md#macos-1012
[in-docker]: https://github.com/PowerShell/PowerShell/tree/master/docker
[in-kali]: https://github.com/PowerShell/PowerShell/tree/master/docs/installation/linux.md#kali
[in-windows-zip]: https://github.com/PowerShell/PowerShell/tree/master/docs/installation/windows.md#zip
[in-tar]: https://github.com/PowerShell/PowerShell/tree/master/docs/installation/linux.md#binary-archives

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

| AppVeyor (Windows)       | Travis CI (Linux) | Travis CI (macOS) | Code Coverage Status |
|--------------------------|-------------------|-------------------|----------------------|
| [![av-nightly-image][]][av-nightly-site] | [![linux-nightly-image][]][tv-site] | [![macOS-nightly-image][]][tv-site] | [![cc-image][]][cc-site] |

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
[known issues]: https://github.com/PowerShell/PowerShell/tree/master/docs/KNOWNISSUES.md
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

By default, PowerShell collects the OS description and the version of PowerShell (equivalent to `$PSVersionTable.OS` and `$PSVersionTable.GitCommitId`) using [Application Insights](https://azure.microsoft.com/en-us/services/application-insights/).
To opt-out of sending telemetry, delete the file `DELETE_ME_TO_DISABLE_CONSOLEHOST_TELEMETRY` before starting PowerShell from the installed location.
The telemetry we collect fall under the [Microsoft Privacy Statement](https://privacy.microsoft.com/en-us/privacystatement/).

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
