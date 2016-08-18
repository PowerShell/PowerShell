![logo][] PowerShell
====================

Welcome to the PowerShell GitHub Community!
PowerShell is a cross-platform (Windows, Linux and OS X) automation and configuration tool/framework that works well with your existing tools and is optimized for dealing with structured data (e.g. JSON, CSV, XML, etc.), REST APIs, and object models.
It includes a command-line shell, an associated scripting language and a framework for processing cmdlets. 


[logo]: assets/Powershell_64.png

New to PowerShell?
------------------

If you are new to PowerShell and would like to learn more, we recommend reviewing the [getting started][] documentation.

[getting started]: docs/learning-powershell

Get PowerShell
--------------

You can download and install a PowerShell package for any of the following platforms.

| Platform                     | Downloads            | How to Install              |
|------------------------------|----------------------|-----------------------------|
| Windows 10 / Server 2016     | [.msi][rl-windows10] | [Instructions][in-windows]  |
| Windows 8.1 / Server 2012 R2 | [.msi][rl-windows81] | [Instructions][in-windows]  |
| Ubuntu 16.04                 | [.deb][rl-ubuntu16]  | [Instructions][in-ubuntu16] |
| Ubuntu 14.04                 | [.deb][rl-ubuntu14]  | [Instructions][in-ubuntu14] |
| CentOS 7                     | [.rpm][rl-centos]    | [Instructions][in-centos]   |
| OS X 10.11                   | [.pkg][rl-osx]       | [Instructions][in-osx]      |

[rl-windows10]: https://github.com/PowerShell/PowerShell/releases/download/v6.0.0-alpha.9/PowerShell_6.0.0.9-alpha.9-win10-x64.msi
[rl-windows81]: https://github.com/PowerShell/PowerShell/releases/download/v6.0.0-alpha.9/PowerShell_6.0.0.9-alpha.9-win81-x64.msi
[rl-ubuntu16]: https://github.com/PowerShell/PowerShell/releases/download/v6.0.0-alpha.9/powershell_6.0.0-alpha.9-1ubuntu1.16.04.1_amd64.deb
[rl-ubuntu14]: https://github.com/PowerShell/PowerShell/releases/download/v6.0.0-alpha.9/powershell_6.0.0-alpha.9-1ubuntu1.14.04.1_amd64.deb
[rl-centos]: https://github.com/PowerShell/PowerShell/releases/download/v6.0.0-alpha.9/powershell-6.0.0_alpha.9-1.el7.centos.x86_64.rpm
[rl-osx]: https://github.com/PowerShell/PowerShell/releases/download/v6.0.0-alpha.9/powershell-6.0.0-alpha.9.pkg

[installation]: docs/installation
[in-windows]: docs/installation/windows.md#msi
[in-ubuntu14]: docs/installation/linux.md#ubuntu-1404
[in-ubuntu16]: docs/installation/linux.md#ubuntu-1604
[in-centos]: docs/installation/linux.md#centos-7
[in-osx]: docs/installation/linux.md#os-x-1011

To install a specific version, visit [releases](https://github.com/PowerShell/PowerShell/releases).

Chat Room
---------

Want to chat with other members of the PowerShell community?

[![Join the chat at https://gitter.im/PowerShell/PowerShell](https://badges.gitter.im/PowerShell/PowerShell.svg)](https://gitter.im/PowerShell/PowerShell?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

Building the Repository
-----------------------

| Linux                    | Windows                    | OS X                   |
|--------------------------|----------------------------|------------------------|
| [Instructions][bd-linux] | [Instructions][bd-windows] | [Instructions][bd-osx] |

If you have any problems building, please consult the developer [FAQ][].

### Build status of master branches

| AppVeyor (Windows)       | Travis CI (Linux / OS X) |
|--------------------------|--------------------------|
| [![av-image][]][av-site] | [![tv-image][]][tv-site] |

[bd-linux]: docs/building/linux.md
[bd-windows]: docs/building/windows-core.md
[bd-osx]: docs/building/osx.md

[FAQ]: docs/FAQ.md

[tv-image]: https://travis-ci.org/PowerShell/PowerShell.svg?branch=master
[tv-site]: https://travis-ci.org/PowerShell/PowerShell/branches
[av-image]: https://ci.appveyor.com/api/projects/status/nsng9iobwa895f98/branch/master?svg=true
[av-site]: https://ci.appveyor.com/project/PowerShell/powershell

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
