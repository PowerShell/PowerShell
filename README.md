![logo][] PowerShell
====================

Welcome to the PowerShell GitHub Community!
PowerShell is a cross-platform (Windows, Linux, and macOS) automation and configuration tool/framework that works well with your existing tools and is optimized for dealing with structured data (e.g. JSON, CSV, XML, etc.), REST APIs, and object models.
It includes a command-line shell, an associated scripting language and a framework for processing cmdlets.


[logo]: assets/Powershell_64.png

New to PowerShell?
------------------

If you are new to PowerShell and would like to learn more, we recommend reviewing the [getting started][] documentation.

[getting started]: docs/learning-powershell

Get PowerShell
--------------

You can download and install a PowerShell package for any of the following platforms.

| Platform                           | Downloads              | How to Install                |
| ---------------------------------- | ---------------------- | ----------------------------- |
| Windows 10 / Server 2016 (x64)     | [.msi][rl-windows10]   | [Instructions][in-windows]    |
| Windows 8.1 / Server 2012 R2 (x64) | [.msi][rl-windows81]   | [Instructions][in-windows]    |
| Windows 7 (x64)                    | [.msi][rl-windows7-64] | [Instructions][in-windows]    |
| Windows 7 (x86)                    | [.msi][rl-windows7-86] | [Instructions][in-windows]    |
| Ubuntu 16.04                       | [.deb][rl-ubuntu16]    | [Instructions][in-ubuntu16]   |
| Ubuntu 14.04                       | [.deb][rl-ubuntu14]    | [Instructions][in-ubuntu14]   |
| CentOS 7                           | [.rpm][rl-centos]      | [Instructions][in-centos]     |
| macOS 10.11                        | [.pkg][rl-macos]       | [Instructions][in-macos]      |
| Docker                             |                        | [Instructions][in-docker]     |

[rl-windows10]: https://github.com/PowerShell/PowerShell/releases/download/v6.0.0-alpha.14/PowerShell_6.0.0.14-alpha.14-win10-x64.msi
[rl-windows81]: https://github.com/PowerShell/PowerShell/releases/download/v6.0.0-alpha.14/PowerShell_6.0.0.14-alpha.14-win81-x64.msi
[rl-windows7-64]: https://github.com/PowerShell/PowerShell/releases/download/v6.0.0-alpha.14/PowerShell_6.0.0.14-alpha.14-win7-x64.msi
[rl-windows7-86]: https://github.com/PowerShell/PowerShell/releases/download/v6.0.0-alpha.14/PowerShell_6.0.0.14-alpha.14-win7-x86.msi
[rl-ubuntu16]: https://github.com/PowerShell/PowerShell/releases/download/v6.0.0-alpha.14/powershell_6.0.0-alpha.14-1ubuntu1.16.04.1_amd64.deb
[rl-ubuntu14]: https://github.com/PowerShell/PowerShell/releases/download/v6.0.0-alpha.14/powershell_6.0.0-alpha.14-1ubuntu1.14.04.1_amd64.deb
[rl-centos]: https://github.com/PowerShell/PowerShell/releases/download/v6.0.0-alpha.14/powershell-6.0.0_alpha.14-1.el7.centos.x86_64.rpm
[rl-macOS]: https://github.com/PowerShell/PowerShell/releases/download/v6.0.0-alpha.14/powershell-6.0.0-alpha.14.pkg

[installation]: docs/installation
[in-windows]: docs/installation/windows.md#msi
[in-ubuntu14]: docs/installation/linux.md#ubuntu-1404
[in-ubuntu16]: docs/installation/linux.md#ubuntu-1604
[in-centos]: docs/installation/linux.md#centos-7
[in-macos]: docs/installation/linux.md#macos-1011
[in-docker]: docker

To install a specific version, visit [releases](https://github.com/PowerShell/PowerShell/releases).

Chat Room
---------

Want to chat with other members of the PowerShell community?

We have a Gitter Room which you can join below.

[![Join the chat at https://gitter.im/PowerShell/PowerShell](https://badges.gitter.im/PowerShell/PowerShell.svg)](https://gitter.im/PowerShell/PowerShell?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

There is also the community driven PowerShell Slack Team which you can sign up for at [Slack Signup]. 

[Slack Signup]: http://slack.poshcode.org
Building the Repository
-----------------------

| Linux                    | Windows                    | macOS                   |
|--------------------------|----------------------------|------------------------|
| [Instructions][bd-linux] | [Instructions][bd-windows] | [Instructions][bd-macOS] |

If you have any problems building, please consult the developer [FAQ][].

### Build status of master branches

| AppVeyor (Windows)       | Travis CI (Linux / macOS) |
|--------------------------|--------------------------|
| [![av-image][]][av-site] | [![tv-image][]][tv-site] |

### Build status of nightly builds

| AppVeyor (Windows)       | Code Coverage Status |
|--------------------------|----------------------|
| [![av-nightly-image][]][av-nightly-site] | [![cc-image][]][cc-site] |

[bd-linux]: docs/building/linux.md
[bd-windows]: docs/building/windows-core.md
[bd-macOS]: docs/building/macos.md

[FAQ]: docs/FAQ.md

[tv-image]: https://travis-ci.org/PowerShell/PowerShell.svg?branch=master
[tv-site]: https://travis-ci.org/PowerShell/PowerShell/branches
[av-image]: https://ci.appveyor.com/api/projects/status/nsng9iobwa895f98/branch/master?svg=true
[av-site]: https://ci.appveyor.com/project/PowerShell/powershell
[av-nightly-image]: https://ci.appveyor.com/api/projects/status/46yd4jogtm2jodcq?svg=true
[av-nightly-site]: https://ci.appveyor.com/project/PowerShell/powershell-f975h
[cc-site]: https://coveralls.io/github/PowerShell/PowerShell?branch=master
[cc-image]: https://coveralls.io/repos/github/PowerShell/PowerShell/badge.svg?branch=master

Downloading the Source Code
---------------------------

The PowerShell repository has a number of other repositories embedded as submodules.

To make things easy, you can just clone recursively:

```sh
git clone --recursive https://github.com/PowerShell/PowerShell.git
```

If you already cloned but forgot to use `--recursive`, you can update submodules manually:

```sh
git submodule update --init
```

See [working with the PowerShell repository](docs/git) for more information.

Developing and Contributing
--------------------------

Please see the [Contribution Guide][] for how to develop and contribute.

If you have any problems, please consult the [known issues][], developer [FAQ][], and [GitHub issues][].
If you do not see your problem captured, please file a [new issue][] and follow the provided template.
If you are developing .NET Core C# applications targeting PowerShell Core, please [check out our FAQ][] to learn more about the PowerShell SDK NuGet package.

Also make sure to check out our [PowerShell-RFC repository](https://github.com/powershell/powershell-rfc) for request-for-comments (RFC) documents to submit and give comments on proposed and future designs.

[check out our FAQ]: docs/FAQ.md#where-do-i-get-the-powershell-core-sdk-package
[Contribution Guide]: .github/CONTRIBUTING.md
[known issues]: docs/KNOWNISSUES.md
[GitHub issues]: https://github.com/PowerShell/PowerShell/issues
[new issue]:https://github.com/PowerShell/PowerShell/issues/new

Legal and Licensing
-------------------

PowerShell is licensed under the [MIT license][].

[MIT license]: LICENSE.txt

Governance
-------------------

Governance policy for PowerShell project is described [here][].  

[here]: https://github.com/PowerShell/PowerShell/blob/master/docs/community/governance.md

Code of Conduct
---------------

This project has adopted the [Microsoft Open Source Code of Conduct][conduct-code].
For more information see the [Code of Conduct FAQ][conduct-FAQ] or contact [opencode@microsoft.com][conduct-email] with any additional questions or comments.

[conduct-code]: http://opensource.microsoft.com/codeofconduct/
[conduct-FAQ]: http://opensource.microsoft.com/codeofconduct/faq/
[conduct-email]: mailto:opencode@microsoft.com
