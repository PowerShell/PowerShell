# Ubuntu 14.04

Note that some of these dependencies are only required for building
CoreCLR and CoreFX on Linux. We should find a reduced set for
PowerShell on Linux itself.

Note that the distributed version of Mono is too old for .NET
projects, the [CoreCLR][] docs point to the [Mono][] docs on how to
install an up-to-date version.

Also note that the distributed version of Git has a bug with `git
clean -fdx` and submodules. I would recommned upgrading.

[CoreCLR]: https://github.com/dotnet/coreclr/blob/master/Documentation/building/linux-instructions.md
[Mono]: http://www.mono-project.com/docs/getting-started/install/linux/

```sh
sudo su

echo "Adding Mono Project repository"
echo "deb http://download.mono-project.com/repo/debian wheezy main" | tee /etc/apt/sources.list.d/mono-xamarin.list
apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF

apt-get update

apt-get install -y \
	git \
	wget \
    mono-devel \
    gcc \
    g++ \
    llvm-3.5 \
    clang-3.5 \
    lldb-3.6 lldb-3.6-dev \
    strace \
    libicu-dev \
    libunwind8 libunwind8-dev \
    libssl-dev \
    libcurl4-openssl-dev \
    libpam0g-dev \
    make \
    cmake \
    gettext
```

# Arch Linux

It's Arch, everything is already new enough.

```
sudo pacman --noconfirm -S git wget gcc mono make cmake icu pam lldb strace
```
