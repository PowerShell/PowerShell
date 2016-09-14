Package installation instructions
=================================

Supports [Ubuntu 14.04][u14], [Ubuntu 16.04][u16],
[CentOS 7][cos], and [macOS 10.11][osx].
All packages are available on our GitHub [releases][] page.

Once the package is installed, run `powershell` from a terminal.

[u14]: #ubuntu-1404
[u16]: #ubuntu-1604
[cos]: #centos-7
[osx]: #os-x-1011
[paths]:#paths

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
**Uninstallation**

`sudo apt-get remove powershell`

or

`sudo dpkg -r powershell`

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
**Uninstallation**

`sudo apt-get remove powershell`

or

`sudo dpkg -r powershell`

[Ubuntu 16.04]: http://releases.ubuntu.com/16.04/

This works for Debian Stretch (now testing) as well.

CentOS 7
========

Using [CentOS 7][], download the RPM package
`powershell-6.0.0_alpha.9-1.el7.centos.x86_64.rpm`
from the [releases][] page onto the CentOS machine.

Then execute the following in the terminal:

```sh
sudo yum install powershell-6.0.0_alpha.9-1.el7.centos.x86_64.rpm
```

You can also install the RPM without the intermediate step of downloading it:



```sh
sudo yum install https://github.com/PowerShell/PowerShell/releases/download/v6.0.0-alpha.9/powershell-6.0.0_alpha.9-1.el7.centos.x86_64.rpm
```

> This package works on Oracle Linux 7. It should work on Red Hat Enterprise Linux 7 too.

**Uninstallation**

`sudo yum remove powershell`

[CentOS 7]: https://www.centos.org/download/

macOS 10.11
===========

Using macOS 10.11, download the PKG package
`powershell-6.0.0-alpha.9.pkg`
from the [releases][] page onto the macOS machine.

Either double-click the file and follow the prompts,
or install it from the terminal:

```sh
sudo installer -pkg powershell-6.0.0-alpha.9.pkg -target /
```

**Uninstallation**

PowerShell on MacOS must be removed manually.

To remove the installed package:
```sh
sudo rm -rf /usr/local/bin/powershell usr/local/microsoft/powershell
```
To uninstall the additional PowerShell paths (such as the user profile path) please see the [paths][paths] section below in this document and remove the desired the paths with `sudo rm`.

OpenSSL
-------

Also install [Homebrew's OpenSSL][openssl]:

```
brew install openssl
```

[Homebrew][brew] is the missing package manager for macOS.
If the `brew` command was not found,
you need to install Homebrew following [their instructions][brew].

.NET Core requires Homebrew's OpenSSL because the "OpenSSL" system libraries on macOS are not OpenSSL,
as Apple deprecated OpenSSL in favor of their own libraries.
This requirement is not a hard requirement for all of PowerShell;
however, most networking functions (such as `Invoke-WebRequest`)
do require OpenSSL to work properly.

**Please ignore** .NET Core's installation instructions to manually link the OpenSSL libraries.
This is **not** required for PowerShell as we patch .NET Core's cryptography libraries to find Homebrew's OpenSSL in its installed location.
Again, **do not** run `brew link --force` nor `ln -s` for OpenSSL, regardless of other instructions.

Homebrew previously allowed OpenSSL libraries to be linked to the system library location;
however, this created major security holes and is [no longer allowed][homebrew-patch].
Because .NET Core's 1.0.0 release libraries still look in the prior system location for OpenSSL,
they will fail to work unless the libraries are manually placed there (security risk),
or their libraries are patched (which we do).
To patch .NET Core's cryptography libraries, we use `install_name_tool`:

```
find ~/.nuget -name System.Security.Cryptography.Native.dylib | xargs sudo install_name_tool -add_rpath /usr/local/opt/openssl/lib
```

This updates .NET Core's library to look in Homebrew's OpenSSL installation location instead of the system library location.
The PowerShell macOS package come with the necessary libraries patched,
and the build script patches the libraries on-the-fly when building from source.
You *can* run this command manually if you're having trouble with .NET Core's cryptography libraries.


[openssl]: https://github.com/Homebrew/homebrew-core/blob/master/Formula/openssl.rb
[brew]: http://brew.sh/
[homebrew-patch]: https://github.com/Homebrew/brew/pull/597

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

On Linux and macOS, the [XDG Base Directory Specification][xdg-bds] is respected.


Note that because macOS is a derivation of BSD,
instead of `/opt`, the prefix used is `/usr/local`.
Thus, `$PSHOME` is `/usr/local/microsoft/powershell/6.0.0-alpha.9/`,
and the symlink is placed at `/usr/local/bin/powershell`.

[releases]: https://github.com/PowerShell/PowerShell/releases/latest
[xdg-bds]: https://specifications.freedesktop.org/basedir-spec/basedir-spec-latest.html
