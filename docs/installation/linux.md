# Package installation instructions

Supports [Ubuntu 14.04][u14], [Ubuntu 16.04][u16],
[CentOS 7][cos], [Arch Linux][arch], [many Linux distributions (AppImage)][lai], and [macOS 10.11][mac].
All packages are available on our GitHub [releases][] page.

All of these steps can be done automatically by the [`download.sh`][download] script.
You should *never* run a script without reading it first!

Please **read the [download][] script first**, and then if you want to run it, use:

```sh
bash <(curl -fsSL https://raw.githubusercontent.com/PowerShell/PowerShell/v6.0.0-alpha.17/tools/download.sh)
```

Once the package is installed, run `powershell` from a terminal.

[u14]: #ubuntu-1404
[u16]: #ubuntu-1604
[cos]: #centos-7
[arch]: #arch-linux
[lai]: #linux-appimage
[mac]: #macos-1011
[download]: https://github.com/PowerShell/PowerShell/blob/v6.0.0-alpha.17/tools/download.sh

## Ubuntu 14.04

### Installation via Package Repository - Ubuntu 14.04

PowerShell Core, for Linux, is published to package repositories for easy installation (and updates).
This is the preferred method.

```sh
# Import the public repository GPG keys
curl https://packages.microsoft.com/keys/microsoft.asc | sudo apt-key add -

# Register the Microsoft Ubuntu repository
curl https://packages.microsoft.com/config/ubuntu/14.04/prod.list | sudo tee /etc/apt/sources.list.d/microsoft.list

# Update apt-get
sudo apt-get update

# Install PowerShell
sudo apt-get install -y powershell

# Start PowerShell
powershell
```

After registering the Microsoft repository once as superuser,
from then on, you just need to use `sudo apt-get upgrade powershell` to update it.

### Installation via Direct Download

Using [Ubuntu 14.04][], download the Debian package
`powershell_6.0.0-alpha.17-1ubuntu1.14.04.1_amd64.deb`
from the [releases][] page onto the Ubuntu machine.

Then execute the following in the terminal:

```sh
sudo dpkg -i powershell_6.0.0-alpha.17-1ubuntu1.14.04.1_amd64.deb
sudo apt-get install -f
```

> Please note that `dpkg -i` will fail with unmet dependencies;
> the next command, `apt-get install -f` resolves these
> and then finishes configuring the PowerShell package.

### Uninstallation - Ubuntu 14.04

```sh
sudo apt-get remove powershell
```

[Ubuntu 14.04]: http://releases.ubuntu.com/14.04/

## Ubuntu 16.04

### Installation via Package Repository - Ubuntu 16.04

PowerShell Core, for Linux, is published to package repositories for easy installation (and updates).
This is the preferred method.

```sh
# Import the public repository GPG keys
curl https://packages.microsoft.com/keys/microsoft.asc | sudo apt-key add -

# Register the Microsoft Ubuntu repository
curl https://packages.microsoft.com/config/ubuntu/16.04/prod.list | sudo tee /etc/apt/sources.list.d/microsoft.list

# Update apt-get
sudo apt-get update

# Install PowerShell
sudo apt-get install -y powershell

# Start PowerShell
powershell
```

After registering the Microsoft repository once as superuser,
from then on, you just need to use `sudo apt-get upgrade powershell` to update it.

### Installation via Direct Download - Ubuntu 16.04

Using [Ubuntu 16.04][], download the Debian package
`powershell_6.0.0-alpha.17-1ubuntu1.16.04.1_amd64.deb`
from the [releases][] page onto the Ubuntu machine.

Then execute the following in the terminal:

```sh
sudo dpkg -i powershell_6.0.0-alpha.17-1ubuntu1.16.04.1_amd64.deb
sudo apt-get install -f
```

> Please note that `dpkg -i` will fail with unmet dependencies;
> the next command, `apt-get install -f` resolves these
> and then finishes configuring the PowerShell package.

### Uninstallation - Ubuntu 16.04

```sh
sudo apt-get remove powershell
```

[Ubuntu 16.04]: http://releases.ubuntu.com/16.04/

This works for Debian Stretch (now testing) as well.

## CentOS 7

> This package also works on Oracle Linux 7 and Red Hat Enterprise Linux (RHEL) 7.

### Installation via Package Repository (preferred) - CentOS 7

PowerShell Core for Linux is published to official Microsoft repositories for easy installation (and updates).

```sh
# Register the Microsoft RedHat repository
curl https://packages.microsoft.com/config/rhel/7/prod.repo | sudo tee /etc/yum.repos.d/microsoft.repo

# Install PowerShell
sudo yum install -y powershell

# Start PowerShell
powershell
```

After registering the Microsoft repository once as superuser,
you just need to use `sudo yum update powershell` to update PowerShell.

### Installation via Direct Download - CentOS 7

Using [CentOS 7][], download the RPM package
`powershell-6.0.0_alpha.17-1.el7.centos.x86_64.rpm`
from the [releases][] page onto the CentOS machine.

Then execute the following in the terminal:

```sh
sudo yum install ./powershell-6.0.0_alpha.17-1.el7.centos.x86_64.rpm
```

You can also install the RPM without the intermediate step of downloading it:

```sh
sudo yum install https://github.com/PowerShell/PowerShell/releases/download/v6.0.0-alpha.17/powershell-6.0.0_alpha.17-1.el7.centos.x86_64.rpm
```

### Uninstallation

```sh
sudo yum remove powershell
```

[CentOS 7]: https://www.centos.org/download/

## Arch Linux

PowerShell is available from the [Arch Linux][] User Repository (AUR) as a [release][arch-release] or the [latest development build][arch-git].

Packages in the AUR are community maintained - there is no official support.

For more information on installing packages from the AUR, see the [Arch Linux wiki](https://wiki.archlinux.org/index.php/Arch_User_Repository#Installing_packages).

[Arch Linux]: https://www.archlinux.org/download/
[arch-release]: https://aur.archlinux.org/packages/powershell/
[arch-git]: https://aur.archlinux.org/packages/powershell-git/

## Linux AppImage

Using a recent Linux distribution,
download the AppImage `PowerShell-x86_64.AppImage`
from the [releases][] page onto the Linux machine.

Then execute the following in the terminal:

```bash
chmod a+x PowerShell-x86_64.AppImage
./PowerShell-x86_64.AppImage
```

The [AppImage][] lets you run PowerShell without installing it.
It is a portable application that bundles PowerShell and its dependencies
(including .NET Core's system dependencies) into one cohesive package.
This package works independently of the user's Linux distribution,
and is a single binary.

[appimage]: http://appimage.org/

## macOS 10.11

Using macOS 10.11, download the PKG package
`powershell-6.0.0-alpha.17.pkg`
from the [releases][] page onto the macOS machine.

Either double-click the file and follow the prompts,
or install it from the terminal:

```sh
sudo installer -pkg powershell-6.0.0-alpha.17.pkg -target /
```

### Uninstallation - macOS 10.11

PowerShell on MacOS must be removed manually.

To remove the installed package:

```sh
sudo rm -rf /usr/local/bin/powershell /usr/local/microsoft/powershell
```

To uninstall the additional PowerShell paths (such as the user profile path)
please see the [paths][paths] section below in this document
and remove the desired the paths with `sudo rm`.

[paths]:#paths

## OpenSSL

Also install [Homebrew's OpenSSL][openssl]:

```bash
brew install openssl
brew install curl --with-openssl
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

```bash
find ~/.nuget -name System.Security.Cryptography.Native.dylib | xargs sudo install_name_tool -add_rpath /usr/local/opt/openssl/lib
find ~/.nuget -name System.Net.Http.Native.dylib | xargs sudo install_name_tool -change /usr/lib/libcurl.4.dylib /usr/local/opt/curl/lib/libcurl.4.dylib
```

This updates .NET Core's library to look in Homebrew's OpenSSL installation location instead of the system library location.
The PowerShell macOS package come with the necessary libraries patched,
and the build script patches the libraries on-the-fly when building from source.
You *can* run this command manually if you're having trouble with .NET Core's cryptography libraries.

[openssl]: https://github.com/Homebrew/homebrew-core/blob/master/Formula/openssl.rb
[brew]: http://brew.sh/
[homebrew-patch]: https://github.com/Homebrew/brew/pull/597

## Paths

* `$PSHOME` is `/opt/microsoft/powershell/6.0.0-alpha.17/`
* User profiles will be read from `~/.config/powershell/profile.ps1`
* Default profiles will be read from `$PSHOME/profile.ps1`
* User modules will be read from `~/.local/share/powershell/Modules`
* Shared modules will be read from `/usr/local/share/powershell/Modules`
* Default modules will be read from `$PSHOME/Modules`
* PSReadline history will be recorded to `~/.local/share/powershell/PSReadLine/ConsoleHost_history.txt`

The profiles respect PowerShell's per-host configuration,
so the default host-specific profiles exists at `Microsoft.PowerShell_profile.ps1` in the same locations.

On Linux and macOS, the [XDG Base Directory Specification][xdg-bds] is respected.

Note that because macOS is a derivation of BSD,
instead of `/opt`, the prefix used is `/usr/local`.
Thus, `$PSHOME` is `/usr/local/microsoft/powershell/6.0.0-alpha.17/`,
and the symlink is placed at `/usr/local/bin/powershell`.

[releases]: https://github.com/PowerShell/PowerShell/releases/latest
[xdg-bds]: https://specifications.freedesktop.org/basedir-spec/basedir-spec-latest.html
