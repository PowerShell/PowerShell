Package installation instructions
=================================

Supports [Ubuntu 14.04][u14], [Ubuntu 16.04][u16],
[CentOS 7][cos], and [OS X 10.11][osx].
All packages are available on our GitHub [releases][] page.

Once the package is installed, run `powershell` from a terminal.

[u14]: #ubuntu-1404
[u16]: #ubuntu-1604
[cos]: #centos-7
[osx]: #os-x-1011

Ubuntu 14.04
============

Using [Ubuntu 14.04][], download the Debian package
`powershell_6.0.0-alpha.9-1ubuntu1.14.04.1_amd64.deb`
from the [releases][] page onto the Ubuntu machine.

Then execute the following in the terminal:

```sh
sudo apt-get install libunwind8 libicu52
sudo dpkg -i powershell_6.0.0-alpha.9-1ubuntu1.14.04.1_amd64.deb
```

[Ubuntu 14.04]: http://releases.ubuntu.com/14.04/

Ubuntu 16.04
============

Using [Ubuntu 16.04][], download the Debian package
`powershell_6.0.0-alpha.9-1ubuntu1.16.04.1_amd64.deb`
from the [releases][] page onto the Ubuntu machine.

Then execute the following in the terminal:

> Please note the different libicu package dependency!

```sh
sudo apt-get install libunwind8 libicu55
sudo dpkg -i powershell_6.0.0-alpha.9-1ubuntu1.16.04.1_amd64.deb
```

[Ubuntu 16.04]: http://releases.ubuntu.com/16.04/


CentOS 7
========

Using [CentOS 7][], download the RPM package
`powershell-6.0.0_alpha.9-1.el7.centos.x86_64.rpm`
from the [releases][] page onto the CentOS machine.

Then execute the following in the terminal:

```sh
sudo yum install powershell-6.0.0_alpha.9-1.el7.centos.x86_64.rpm
```

> This package should work on Red Hat Enterprise Linux 7 too.

[CentOS 7]: https://www.centos.org/download/

OS X 10.11
==========

Using OS X 10.11, download the PKG package `powershell-6.0.0-alpha.9-osx.10.11-x64.pkg` from the [releases][] page onto the OS X machine.

Either double-click the file and follow the prompts,
or install it from the terminal:

```sh
sudo installer -pkg powershell-6.0.0-alpha.9-osx.10.11-x64.pkg -target /
```

Paths
=====

* `$PSHOME` is `/opt/microsoft/powershell/6.0.0-alpha.9/`
* User profiles will be read from `~/.config/powershell/profile.ps1`
* Default profiles will be read from `$PSHOME/profile.ps1`
* User modules will be read from `~/.local/share/powershell/Modules`
* Shared modules will be read from `/usr/local/share/powershell/Modules`
* Default modules will be read from `$PSHOME/Modules`
* PSReadLine history will be recorded to `~/.local/share/powershell/PSReadLine/ConsoleHost_history.txt`

The profiles respect PowerShell's per-host configuration,
so the default host-specific profiles exists at `Microsoft.PowerShell_profile.ps1` in the same locations.

On Linux and OS X, the [XDG Base Directory Specification][xdg-bds] is respected.


Note that because OS X is a derivation of BSD,
instead of `/opt`, the prefix used is `/usr/local`.
Thus, `$PSHOME` is `/usr/local/microsoft/powershell/6.0.0-alpha.9/`,
and the symlink is placed at `/usr/local/bin/powershell`.

[releases]: https://github.com/PowerShell/PowerShell/releases/latest
[xdg-bds]: https://specifications.freedesktop.org/basedir-spec/basedir-spec-latest.html
