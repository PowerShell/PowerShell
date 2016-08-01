Package installation instructions
=================================

Supports [Ubuntu 14.04](Ubuntu14.04), [CentOS 7.1](CentOS7.1), and [OS X 10.11](OSX10.11).
All packages are available on our GitHub [releases][] page.

Once the package is installed, run `powershell` from a terminal.

> There is a symlink created from `/usr/bin/powershell` to `/opt/microsoft/powershell/powershell` to ensure `powershell` is in your path.

[releases]: https://github.com/PowerShell/PowerShell/releases/latest
[xdg-bds]: https://specifications.freedesktop.org/basedir-spec/basedir-spec-latest.html

[Ubuntu14.04]:./linux.md#ubuntu-1404
[CentOS7.1]:./linux.md#centos-7
[OSX10.11]: ./linux.md#os-x-1011

Ubuntu 14.04
============

Using [Ubuntu 14.04][], download the Debian package `powershell_6.0.0-alpha.7-1_amd64.deb` from the [releases][] page onto the Ubuntu machine.

Then execute the following in the terminal:

```sh
sudo apt-get install libunwind8 libicu52
sudo dpkg -i powershell_6.0.0-alpha.7-1_amd64.deb
```

> Please note that that Ubuntu 16.04 is not yet supported, but coming soon!

[Ubuntu 14.04]: http://releases.ubuntu.com/14.04/

CentOS 7
========

Using [CentOS 7][], download the RPM package `powershell-6.0.0_alpha.7-1.x86_64.rpm` from the [releases][] page onto the CentOS machine.

Then execute the following in the terminal:

```sh
sudo yum install powershell-6.0.0_alpha.7-1.x86_64.rpm
```

> Please note that we have not tested this package on Red Hat Enterprise Linux.

[CentOS 7]: https://www.centos.org/download/

OS X 10.11
==========

Using OS X 10.11, download the PKG package `powershell-6.0.0-alpha.7.pkg` from the [releases][] page onto the OS X machine.

Either double-click the file and follow the prompts, or install it from the terminal:

```sh
sudo installer -pkg powershell-6.0.0-alpha.7.pkg -target /
```

> Note that because OS X is a derivation of BSD, instead of `/opt`, the prefix used is `/usr/local`.
> Thus, `powershell` lives at `/usr/local/microsoft/powershell`, and the symlink is placed at `/usr/local/bin/powershell`.
> This affects the system modules and profiles as well.

Paths
=====

* User profiles will be read from `~/.config/powershell/profile.ps1`.
* User modules will be read from `~/.local/share/powershell/Modules`
* PSReadLine history will be recorded to `~/.local/share/powershell/PSReadLine/ConsoleHost_history.txt`
* System profiles will be read from `/opt/microsoft/powershell/profile.ps1`.
* System modules will be read from `/opt/microsoft/powershell/Modules`

The profiles respect PowerShell's per-host configuration, so the default host-specific profiles exists at `Microsoft.PowerShell_profile.ps1` in the same locations.

On Linux and OS X, the [XDG Base Directory Specification][xdg-bds] is respected.
