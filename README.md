![logo][] PowerShell
====================

PowerShell is a task automation and configuration management platform.
It consists of a cross-platform command-line shell and associated scripting language.

[logo]: assets/Powershell_64.png

New to PowerShell?
------------------

If you are new to PowerShell and would like to learn more, we recommend reviewing the [getting started][] documentation.

[getting started]: docs/learning-powershell

Get PowerShell
--------------

You can download and install a PowerShell package for any of the following platforms.
Our packages are hosted on the GitHub [releases][] page.

| **Platform** | [Releases][]                                      | Build status             |
|--------------|---------------------------------------------------|--------------------------|
| Windows      | [.msi][rl-windows] [(how to install)][in-windows] | [![av-image][]][av-site] |
| Ubuntu 14.04 | [.deb][rl-ubuntu]  [(how to install)][in-ubuntu]  | [![tv-image][]][tv-site] |
| CentOS 7     | [.rpm][rl-centos]  [(how to install)][in-centos]  | *N/A*                    |
| OS X 10.11   | [.pkg][rl-osx]     [(how to install)][in-osx]     | [![tv-image][]][tv-site] |

[releases]: https://github.com/PowerShell/PowerShell/releases
[rl-windows]: https://github.com/PowerShell/PowerShell/releases/download/v6.0.0-alpha.7/PowerShell_6.0.0.7.msi
[rl-ubuntu]: https://github.com/PowerShell/PowerShell/releases/download/v6.0.0-alpha.7/powershell_6.0.0-alpha.7-1_amd64.deb
[rl-centos]: https://github.com/PowerShell/PowerShell/releases/download/v6.0.0-alpha.7/powershell-6.0.0_alpha.7-1.x86_64.rpm
[rl-osx]: https://github.com/PowerShell/PowerShell/releases/download/v6.0.0-alpha.7/powershell-6.0.0-alpha.7.pkg

[installation]: docs/installation
[in-windows]: docs/installation/windows.md#msi
[in-ubuntu]: docs/installation/linux.md#ubuntu-1404
[in-centos]: docs/installation/linux.md#centos-7
[in-osx]: docs/installation/linux.md#os-x-1011

[tv-image]: https://travis-ci.com/PowerShell/PowerShell.svg?token=31YifM4jfyVpBmEGitCm&branch=master
[tv-site]: https://travis-ci.com/PowerShell/PowerShell/branches
[av-image]: https://ci.appveyor.com/api/projects/status/jtefab3hpngtyesp/branch/master?svg=true
[av-site]: https://ci.appveyor.com/project/PowerShell/powershell/branch/master

Building the Repository
-----------------------

Please refer to the platform-specific [build][] documentation.

| Linux                    | Windows                    | OS X                   |
|--------------------------|----------------------------|------------------------|
| [Instructions][bd-linux] | [Instructions][bd-windows] | [Instructions][bd-osx] |

[build]: docs/building
[bd-linux]: docs/building/linux.md
[bd-windows]: docs/building/windows-core.md
[bd-osx]: docs/building/osx.md

Downloading the Source Code
---------------------------

The PowerShell repository has a number of other repositories embedded as submodules.

To make things easy, we can just clone recursively:

```sh
git clone --recursive https://github.com/PowerShell/PowerShell.git
```

If you already cloned but forgot to use `--recursive`, you can update submodules manually:

```sh
git submodule update --init
```

See [working with the PowerShell repository][powershell-repo-101] for more information.

[powershell-repo-101]: docs/git/powershell-repository-101.md

Developing and Contributing
--------------------------

Please see the [Contribution Guide][] for how to develop and contribute.

If you have any problems, please consult the [known issues][], developer [FAQ][], and [GitHub issues][].
If you do not see your problem captured, please file a [new issue][] and follow the provided template.

[Contribution Guide]: .github/CONTRIBUTING.md
[known issues]: docs/KNOWNISSUES.md
[FAQ]: docs/FAQ.md
[GitHub issues]: https://github.com/PowerShell/PowerShell/issues
[new issue]:https://github.com/PowerShell/PowerShell/issues/new

Code of Conduct
---------------

This project has adopted the [Microsoft Open Source Code of Conduct][conduct-code].
For more information see the [Code of Conduct FAQ][conduct-FAQ] or contact [opencode@microsoft.com][conduct-email] with any additional questions or comments.

[conduct-code]: http://opensource.microsoft.com/codeofconduct/
[conduct-FAQ]: http://opensource.microsoft.com/codeofconduct/faq/
[conduct-email]: mailto:opencode@microsoft.com
