# Package installation instructions

Supports [Ubuntu 14.04][u14], [Ubuntu 16.04][u16], [Ubuntu 17.04][u17], [Debian 8][deb8], [Debian 9][deb9],
[CentOS 7][cos], [Red Hat Enterprise Linux (RHEL) 7][rhel7], [OpenSUSE 42.2][opensuse], [Fedora 25][fed25],
[Fedora 26][fed26], [Arch Linux][arch], and [macOS 10.12][mac].

For Linux distributions that are not officially supported,
you can try using the [PowerShell AppImage][lai].
You can also try deploying PowerShell binaries directly using the Linux [`tar.gz` archive][tar],
but you would need to set up the necessary dependencies based on the OS in separate steps.

All packages are available on our GitHub [releases][] page.
Once the package is installed, run `pwsh` from a terminal.

[u14]: #ubuntu-1404
[u16]: #ubuntu-1604
[u17]: #ubuntu-1704
[deb8]: #debian-8
[deb9]: #debian-9
[cos]: #centos-7
[rhel7]: #red-hat-enterprise-linux-rhel-7
[opensuse]: #opensuse-422
[fed25]: #fedora-25
[fed26]: #fedora-26
[arch]: #arch-linux
[lai]: #linux-appimage
[mac]: #macos-1012
[tar]: #binary-archives

## Ubuntu 14.04

### Installation via Package Repository - Ubuntu 14.04

PowerShell Core, for Linux, is published to package repositories for easy installation (and updates).
This is the preferred method.

```sh
# Import the public repository GPG keys
curl https://packages.microsoft.com/keys/microsoft.asc | sudo apt-key add -

# Register the Microsoft Ubuntu repository
curl https://packages.microsoft.com/config/ubuntu/14.04/prod.list | sudo tee /etc/apt/sources.list.d/microsoft.list

# Update the list of products
sudo apt-get update

# Install PowerShell
sudo apt-get install -y powershell

# Start PowerShell
pwsh
```

After registering the Microsoft repository once as superuser,
from then on, you just need to use `sudo apt-get upgrade powershell` to update it.

### Installation via Direct Download - Ubuntu 14.04

Download the Debian package
`powershell_6.0.0-beta.9-1.ubuntu.14.04_amd64.deb`
from the [releases][] page onto the Ubuntu machine.

Then execute the following in the terminal:

```sh
sudo dpkg -i powershell_6.0.0-beta.9-1.ubuntu.14.04_amd64.deb
sudo apt-get install -f
```

> Please note that `dpkg -i` will fail with unmet dependencies;
> the next command, `apt-get install -f` resolves these
> and then finishes configuring the PowerShell package.

### Uninstallation - Ubuntu 14.04

```sh
sudo apt-get remove powershell
```

## Ubuntu 16.04

### Installation via Package Repository - Ubuntu 16.04

PowerShell Core, for Linux, is published to package repositories for easy installation (and updates).
This is the preferred method.

```sh
# Import the public repository GPG keys
curl https://packages.microsoft.com/keys/microsoft.asc | sudo apt-key add -

# Register the Microsoft Ubuntu repository
curl https://packages.microsoft.com/config/ubuntu/16.04/prod.list | sudo tee /etc/apt/sources.list.d/microsoft.list

# Update the list of products
sudo apt-get update

# Install PowerShell
sudo apt-get install -y powershell

# Start PowerShell
pwsh
```

After registering the Microsoft repository once as superuser,
from then on, you just need to use `sudo apt-get upgrade powershell` to update it.

### Installation via Direct Download - Ubuntu 16.04

Download the Debian package
`powershell_6.0.0-beta.9-1.ubuntu.16.04_amd64.deb`
from the [releases][] page onto the Ubuntu machine.

Then execute the following in the terminal:

```sh
sudo dpkg -i powershell_6.0.0-beta.9-1.ubuntu.16.04_amd64.deb
sudo apt-get install -f
```

> Please note that `dpkg -i` will fail with unmet dependencies;
> the next command, `apt-get install -f` resolves these
> and then finishes configuring the PowerShell package.

### Uninstallation - Ubuntu 16.04

```sh
sudo apt-get remove powershell
```

## Ubuntu 17.04

### Installation via Package Repository - Ubuntu 17.04

PowerShell Core, for Linux, is published to package repositories for easy installation (and updates).
This is the preferred method.

```sh
# Import the public repository GPG keys
curl https://packages.microsoft.com/keys/microsoft.asc | sudo apt-key add -

# Register the Microsoft Ubuntu repository
curl https://packages.microsoft.com/config/ubuntu/17.04/prod.list | sudo tee /etc/apt/sources.list.d/microsoft.list

# Update the list of products
sudo apt-get update

# Install PowerShell
sudo apt-get install -y powershell

# Start PowerShell
pwsh
```

After registering the Microsoft repository once as superuser,
from then on, you just need to use `sudo apt-get upgrade powershell` to update it.

### Installation via Direct Download - Ubuntu 17.04

Download the Debian package
`powershell_6.0.0-beta.9-1.ubuntu.17.04_amd64.deb`
from the [releases][] page onto the Ubuntu machine.

Then execute the following in the terminal:

```sh
sudo dpkg -i powershell_6.0.0-beta.9-1.ubuntu.17.04_amd64.deb
sudo apt-get install -f
```

> Please note that `dpkg -i` will fail with unmet dependencies;
> the next command, `apt-get install -f` resolves these
> and then finishes configuring the PowerShell package.

### Uninstallation - Ubuntu 17.04

```sh
sudo apt-get remove powershell
```

## Debian 8

### Installation via Package Repository - Debian 8

PowerShell Core, for Linux, is published to package repositories for easy installation (and updates).
This is the preferred method.

```sh
# Install system components
sudo apt-get update
sudo apt-get install curl apt-transport-https

# Import the public repository GPG keys
curl https://packages.microsoft.com/keys/microsoft.asc | sudo apt-key add -

# Register the Microsoft Product feed
sudo sh -c 'echo "deb [arch=amd64] https://packages.microsoft.com/repos/microsoft-debian-jessie-prod jessie main" > /etc/apt/sources.list.d/microsoft.list'

# Update the list of products
sudo apt-get update

# Install PowerShell
sudo apt-get install -y powershell

# Start PowerShell
pwsh
```

After registering the Microsoft repository once as superuser,
from then on, you just need to use `sudo apt-get upgrade powershell` to update it.

### Installation via Direct Download - Debian 8

Download the Debian package
`powershell_6.0.0-beta.9-1.debian.8_amd64.deb`
from the [releases][] page onto the Debian machine.

Then execute the following in the terminal:

```sh
sudo dpkg -i powershell_6.0.0-beta.9-1.debian.8_amd64.deb
sudo apt-get install -f
```

> Please note that `dpkg -i` will fail with unmet dependencies;
> the next command, `apt-get install -f` resolves these
> and then finishes configuring the PowerShell package.

### Uninstallation - Debian 8

```sh
sudo apt-get remove powershell
```

## Debian 9

### Installation via Package Repository - Debian 9

PowerShell Core, for Linux, is published to package repositories for easy installation (and updates).
This is the preferred method.

```sh
# Install system components
sudo apt-get update
sudo apt-get install curl gnupg apt-transport-https

# Import the public repository GPG keys
curl https://packages.microsoft.com/keys/microsoft.asc | sudo apt-key add -

# Register the Microsoft Product feed
sudo sh -c 'echo "deb [arch=amd64] https://packages.microsoft.com/repos/microsoft-debian-stretch-prod stretch main" > /etc/apt/sources.list.d/microsoft.list'

# Update the list of products
sudo apt-get update

# Install PowerShell
sudo apt-get install -y powershell

# Start PowerShell
pwsh
```

After registering the Microsoft repository once as superuser,
from then on, you just need to use `sudo apt-get upgrade powershell` to update it.

### Installation via Direct Download - Debian 9

Download the Debian package
`powershell_6.0.0-beta.9-1.debian.9_amd64.deb`
from the [releases][] page onto the Debian machine.

Then execute the following in the terminal:

```sh
sudo dpkg -i powershell_6.0.0-beta.9-1.debian.9_amd64.deb
sudo apt-get install -f
```

> Please note that `dpkg -i` will fail with unmet dependencies;
> the next command, `apt-get install -f` resolves these
> and then finishes configuring the PowerShell package.

### Uninstallation - Debian 9

```sh
sudo apt-get remove powershell
```

## CentOS 7

> This package also works on Oracle Linux 7.

### Installation via Package Repository (preferred) - CentOS 7

PowerShell Core for Linux is published to official Microsoft repositories for easy installation (and updates).

```sh
# Register the Microsoft RedHat repository
curl https://packages.microsoft.com/config/rhel/7/prod.repo | sudo tee /etc/yum.repos.d/microsoft.repo

# Install PowerShell
sudo yum install -y powershell

# Start PowerShell
pwsh
```

After registering the Microsoft repository once as superuser,
you just need to use `sudo yum update powershell` to update PowerShell.

### Installation via Direct Download - CentOS 7

Using [CentOS 7][], download the RPM package
`powershell-6.0.0_beta.9-1.rhel.7.x86_64.rpm`
from the [releases][] page onto the CentOS machine.

Then execute the following in the terminal:

```sh
sudo yum install powershell-6.0.0_beta.9-1.rhel.7.x86_64.rpm
```

You can also install the RPM without the intermediate step of downloading it:

```sh
sudo yum install https://github.com/PowerShell/PowerShell/releases/download/v6.0.0-beta.9/powershell-6.0.0_beta.9-1.rhel.7.x86_64.rpm
```

### Uninstallation - CentOS 7

```sh
sudo yum remove powershell
```

[CentOS 7]: https://www.centos.org/download/

## Red Hat Enterprise Linux (RHEL) 7

### Installation via Package Repository (preferred) - Red Hat Enterprise Linux (RHEL) 7

PowerShell Core for Linux is published to official Microsoft repositories for easy installation (and updates).

```sh
# Register the Microsoft RedHat repository
curl https://packages.microsoft.com/config/rhel/7/prod.repo | sudo tee /etc/yum.repos.d/microsoft.repo

# Install PowerShell
sudo yum install -y powershell

# Start PowerShell
pwsh
```

After registering the Microsoft repository once as superuser,
you just need to use `sudo yum update powershell` to update PowerShell.

### Installation via Direct Download - Red Hat Enterprise Linux (RHEL) 7

Download the RPM package
`powershell-6.0.0_beta.9-1.rhel.7.x86_64.rpm`
from the [releases][] page onto the Red Hat Enterprise Linux machine.

Then execute the following in the terminal:

```sh
sudo yum install powershell-6.0.0_beta.9-1.rhel.7.x86_64.rpm
```

You can also install the RPM without the intermediate step of downloading it:

```sh
sudo yum install https://github.com/PowerShell/PowerShell/releases/download/v6.0.0-beta.9/powershell-6.0.0_beta.9-1.rhel.7.x86_64.rpm
```

### Uninstallation - Red Hat Enterprise Linux (RHEL) 7

```sh
sudo yum remove powershell
```

## OpenSUSE 42.2

> **Note:** When installing PowerShell Core, OpenSUSE may report that nothing provides `libcurl`.
`libcurl` should already be installed on supported versions of OpenSUSE.
Run `zypper search libcurl` to confirm.
The error will present 2 'solutions'. Choose 'Solution 2' to continue installing PowerShell Core.

### Installation via Package Repository (preferred) - OpenSUSE 42.2

PowerShell Core for Linux is published to official Microsoft repositories for easy installation (and updates).

```sh
# Register the Microsoft signature key
sudo rpm --import https://packages.microsoft.com/keys/microsoft.asc

# Add the Microsoft Product feed
curl https://packages.microsoft.com/config/rhel/7/prod.repo | sudo tee /etc/zypp/repos.d/microsoft.repo

# Update the list of products
sudo zypper update

# Install PowerShell
sudo zypper install powershell

# Start PowerShell
pwsh
```

### Installation via Direct Download - OpenSUSE 42.2

Download the RPM package `powershell-6.0.0_beta.9-1.rhel.7.x86_64.rpm`
from the [releases][] page onto the OpenSUSE machine.

```sh
sudo rpm --import https://packages.microsoft.com/keys/microsoft.asc
sudo zypper install powershell-6.0.0_beta.9-1.rhel.7.x86_64.rpm
```

You can also install the RPM without the intermediate step of downloading it:

```sh
sudo rpm --import https://packages.microsoft.com/keys/microsoft.asc
sudo zypper install https://github.com/PowerShell/PowerShell/releases/download/v6.0.0-beta.9/powershell-6.0.0_beta.9-1.rhel.7.x86_64.rpm
```

### Uninstallation - OpenSUSE 42.2

```sh
sudo zypper remove powershell
```

## Fedora 25

### Installation via Package Repository (preferred) - Fedora 25

PowerShell Core for Linux is published to official Microsoft repositories for easy installation (and updates).

```sh
# Register the Microsoft signature key
sudo rpm --import https://packages.microsoft.com/keys/microsoft.asc

# Register the Microsoft RedHat repository
curl https://packages.microsoft.com/config/rhel/7/prod.repo | sudo tee /etc/yum.repos.d/microsoft.repo

# Update the list of products
sudo dnf update

# Install PowerShell
sudo dnf install -y powershell

# Start PowerShell
pwsh
```

### Installation via Direct Download - Fedora 25

Download the RPM package
`powershell-6.0.0_beta.9-1.rhel.7.x86_64.rpm`
from the [releases][] page onto the Fedora machine.

Then execute the following in the terminal:

```sh
sudo dnf install powershell-6.0.0_beta.9-1.rhel.7.x86_64.rpm
```

You can also install the RPM without the intermediate step of downloading it:

```sh
sudo dnf install https://github.com/PowerShell/PowerShell/releases/download/v6.0.0-beta.9/powershell-6.0.0_beta.9-1.rhel.7.x86_64.rpm
```

### Uninstallation - Fedora 25

```sh
sudo dnf remove powershell
```

## Fedora 26

### Installation via Package Repository (preferred) - Fedora 26

PowerShell Core for Linux is published to official Microsoft repositories for easy installation (and updates).

```sh
# Register the Microsoft signature key
sudo rpm --import https://packages.microsoft.com/keys/microsoft.asc

# Register the Microsoft RedHat repository
curl https://packages.microsoft.com/config/rhel/7/prod.repo | sudo tee /etc/yum.repos.d/microsoft.repo

# Update the list of products
sudo dnf update

# Install a system component
sudo dnf install compat-openssl10

# Install PowerShell
sudo dnf install -y powershell

# Start PowerShell
pwsh
```

### Installation via Direct Download - Fedora 26

Download the RPM package
`powershell-6.0.0_beta.9-1.rhel.7.x86_64.rpm`
from the [releases][] page onto the Fedora machine.

Then execute the following in the terminal:

```sh
sudo dnf update
sudo dnf install compat-openssl10
sudo dnf install powershell-6.0.0_beta.9-1.rhel.7.x86_64.rpm
```

You can also install the RPM without the intermediate step of downloading it:

```sh
sudo dnf update
sudo dnf install compat-openssl10
sudo dnf install https://github.com/PowerShell/PowerShell/releases/download/v6.0.0-beta.9/powershell-6.0.0_beta.9-1.rhel.7.x86_64.rpm
```

### Uninstallation - Fedora 26

```sh
sudo dnf remove powershell
```

## Arch Linux

PowerShell is available from the [Arch Linux][] User Repository (AUR).

* It can be compiled with the [latest tagged release][arch-release]
* It can be compiled from the [latest commit to master][arch-git]
* It can be installed using the [latest release binary][arch-bin]

Packages in the AUR are community maintained - there is no official support.

For more information on installing packages from the AUR, see the [Arch Linux wiki](https://wiki.archlinux.org/index.php/Arch_User_Repository#Installing_packages) or the community [DockerFile](https://github.com/PowerShell/PowerShell/blob/master/docker/community/archlinux/Dockerfile).

[Arch Linux]: https://www.archlinux.org/download/
[arch-release]: https://aur.archlinux.org/packages/powershell/
[arch-git]: https://aur.archlinux.org/packages/powershell-git/
[arch-bin]: https://aur.archlinux.org/packages/powershell-bin/

## Linux AppImage

Using a recent Linux distribution,
download the AppImage `powershell-6.0.0-beta.9-x86_64.AppImage`
from the [releases][] page onto the Linux machine.

Then execute the following in the terminal:

```bash
chmod a+x powershell-6.0.0-beta.9-x86_64.AppImage
./powershell-6.0.0-beta.9-x86_64.AppImage
```

The [AppImage][] lets you run PowerShell without installing it.
It is a portable application that bundles PowerShell and its dependencies
(including .NET Core's system dependencies) into one cohesive package.
This package works independently of the user's Linux distribution,
and is a single binary.

[appimage]: http://appimage.org/

## macOS 10.12

### Installation via Homebrew (preferred) - macOS 10.12

[Homebrew][brew] is the missing package manager for macOS.
If the `brew` command is not found,
you need to install Homebrew following [their instructions][brew].

Once you've installed Homebrew, installing PowerShell is easy.
First, install [Homebrew-Cask][cask], so you can install more packages:

```sh
brew tap caskroom/cask
```

Now, you can install PowerShell:

```sh
brew cask install powershell
```

When new versions of PowerShell are released,
simply update Homebrew's formulae and upgrade PowerShell:

```sh
brew update
brew cask reinstall powershell
```

> Note: because of [this issue in Cask](https://github.com/caskroom/homebrew-cask/issues/29301), you currently have to do a reinstall to upgrade.

[brew]: http://brew.sh/
[cask]: https://caskroom.github.io/

### Installation via Direct Download - macOS 10.12

Using macOS 10.12, download the PKG package
`powershell-6.0.0-beta.9-osx.10.12-x64.pkg`
from the [releases][] page onto the macOS machine.

Either double-click the file and follow the prompts,
or install it from the terminal:

```sh
sudo installer -pkg powershell-6.0.0-beta.9-osx.10.12-x64.pkg -target /
```

### Uninstallation - macOS 10.12

If you installed PowerShell with Homebrew, uninstallation is easy:

```sh
brew cask uninstall powershell
```

If you installed PowerShell via direct download,
PowerShell must be removed manually:

```sh
sudo rm -rf /usr/local/bin/pwsh /usr/local/microsoft/powershell
```

To uninstall the additional PowerShell paths (such as the user profile path)
please see the [paths][paths] section below in this document
and remove the desired the paths with `sudo rm`.
(Note: this is not necessary if you installed with Homebrew.)

[paths]:#paths

## Kali

### Installation

```sh
# Install prerequisites
apt-get install libunwind8 libicu55
wget http://security.debian.org/debian-security/pool/updates/main/o/openssl/libssl1.0.0_1.0.1t-1+deb8u6_amd64.deb
dpkg -i libssl1.0.0_1.0.1t-1+deb8u6_amd64.deb

# Install PowerShell
dpkg -i powershell_6.0.0-beta.9-1.ubuntu.16.04_amd64.deb

# Start PowerShell
pwsh
```

### Run PowerShell in latest Kali (Kali GNU/Linux Rolling) without installing it

```sh
# Grab the latest App Image
wget https://github.com/PowerShell/PowerShell/releases/download/v6.0.0-beta.9/powershell-6.0.0-beta.9-x86_64.AppImage

# Make executable
chmod a+x powershell-6.0.0-beta.9-x86_64.AppImage

# Start PowerShell
./powershell-6.0.0-beta.9-x86_64.AppImage
```

### Uninstallation - Kali

```sh
dpkg -r powershell_6.0.0-beta.9-1.ubuntu.16.04_amd64.deb
```

## Binary Archives

PowerShell binary `tar.gz` archives are provided for macOS and Linux platforms to enable advanced deployment scenarios.

### Dependencies

For Linux, PowerShell builds portable binaries for all Linux distributions.
But .NET Core runtime requires different dependencies on different distributions,
and hence PowerShell does the same.

The following chart shows the .NET Core 2.0 dependencies on different Linux distributions that are officially supported.

| OS                 | Dependencies |
| ------------------ | ------------ |
| Ubuntu 14.04       | libc6, libgcc1, libgssapi-krb5-2, liblttng-ust0, libstdc++6, <br> libcurl3, libunwind8, libuuid1, zlib1g, libssl1.0.0, libicu52 |
| Ubuntu 16.04       | libc6, libgcc1, libgssapi-krb5-2, liblttng-ust0, libstdc++6, <br> libcurl3, libunwind8, libuuid1, zlib1g, libssl1.0.0, libicu55 |
| Ubuntu 17.04       | libc6, libgcc1, libgssapi-krb5-2, liblttng-ust0, libstdc++6, <br> libcurl3, libunwind8, libuuid1, zlib1g, libssl1.0.0, libicu57 |
| Debian 8 (Jessie)  | libc6, libgcc1, libgssapi-krb5-2, liblttng-ust0, libstdc++6, <br> libcurl3, libunwind8, libuuid1, zlib1g, libssl1.0.0, libicu52 |
| Debian 9 (Stretch) | libc6, libgcc1, libgssapi-krb5-2, liblttng-ust0, libstdc++6, <br> libcurl3, libunwind8, libuuid1, zlib1g, libssl1.0.2, libicu57 |
| CentOS 7 <br> Oracle Linux 7 <br> RHEL 7 <br> OpenSUSE 42.2 <br> Fedora 25 | libunwind, libcurl, openssl-libs, libicu |
| Fedora 26          | libunwind, libcurl, openssl-libs, libicu, compat-openssl10 |

In order to deploy PowerShell binaries on Linux distributions that are not officially supported,
you would need to install the necessary dependencies for the target OS in separate steps.
For example, our [Amazon Linux dockerfile][amazon-dockerfile] installs dependencies first,
and then extracts the Linux `tar.gz` archive.

[amazon-dockerfile]: https://github.com/PowerShell/PowerShell/blob/master/docker/community/amazonlinux/Dockerfile

### Installation - Binary Archives

#### Linux

```sh
# Download the powershell '.tar.gz' archive
curl -L -o /tmp/powershell.tar.gz https://github.com/PowerShell/PowerShell/releases/download/v6.0.0-beta.9/powershell-6.0.0-beta.9-linux-x64.tar.gz

# Create the target folder where powershell will be placed
sudo mkdir -p /opt/microsoft/powershell/6.0.0-beta.9

# Expand powershell to the target folder
sudo tar zxf /tmp/powershell.tar.gz -C /opt/microsoft/powershell/6.0.0-beta.9

# Create the symbolic link that points to pwsh
sudo ln -s /opt/microsoft/powershell/6.0.0-beta.9/pwsh /usr/bin/pwsh
```

#### macOS

```sh
# Download the powershell '.tar.gz' archive
curl -L -o /tmp/powershell.tar.gz https://github.com/PowerShell/PowerShell/releases/download/v6.0.0-beta.9/powershell-6.0.0-beta.9-osx-x64.tar.gz

# Create the target folder where powershell will be placed
sudo mkdir -p /usr/local/microsoft/powershell/6.0.0-beta.9

# Expand powershell to the target folder
sudo tar zxf /tmp/powershell.tar.gz -C /usr/local/microsoft/powershell/6.0.0-beta.9

# Create the symbolic link that points to pwsh
sudo ln -s /usr/local/microsoft/powershell/6.0.0-beta.9/pwsh /usr/local/bin/pwsh
```

### Uninstallation - Binary Archives

#### Linux

```sh
sudo rm -rf /usr/bin/pwsh /opt/microsoft/powershell
```

#### macOS

```sh
sudo rm -rf /usr/local/bin/pwsh /usr/local/microsoft/powershell
```

## Paths

* `$PSHOME` is `/opt/microsoft/powershell/6.0.0-beta.9/`
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
Thus, `$PSHOME` is `/usr/local/microsoft/powershell/6.0.0-beta.9/`,
and the symlink is placed at `/usr/local/bin/pwsh`.

[releases]: https://github.com/PowerShell/PowerShell/releases/latest
[xdg-bds]: https://specifications.freedesktop.org/basedir-spec/basedir-spec-latest.html
