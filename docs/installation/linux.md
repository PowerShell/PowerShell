Package installation instructions
=================================

Supports Ubuntu 14.04, CentOS 7.1, and OS X 10.11.

Once the package is installed, `powershell` will be in your path,
ready to be launched from a terminal. It will read
`~/.powershell/profile.ps1` for your user profile, and
`/opt/microsoft/powershell/Microsoft.PowerShell_profile.ps1` for
the host profile.

Similarly, it will search `~/.powershell/Modules` and
`/opt/microsoft/powershell/Modules` for user and system modules.

To place PowerShell in your path, a symlink to the
`/opt/microsoft/powershell/powershell` executable is placed at
`/usr/bin/powershell`.

Ubuntu 14.04
============

Using a stock Ubuntu 14.04 image, download the
`powershell_0.5.0-1_amd64.deb` file, and then execute the following:

```sh
sudo apt-get install libunwind8 libicu52
sudo dpkg -i powershell_0.5.0-1_amd64.deb
```

CentOS 7.1
==========

Using a stock CentOS 7.1 image, download the
`powershell-0.5.0-1.x86_64.rpm` file, and then execute the following:

```sh
sudo yum install powershell-0.5.0-1.x86_64.rpm
```

OS X 10.11
==========

Using an OS X 10.11 machine, download the `powershell-0.5.0.pkg` file,
double-click it, and follow the prompts. Or install it from the
terminal:

```sh
sudo installer -pkg powershell-0.5.0.pkg -target /
```

Note that because OS X is a derivation of BSD, instead of `/opt`, the
prefix used is `/usr/local`; thus, `powershell` lives at
`/usr/local/microsoft/powershell`, and the symlink is placed at
`/usr/local/bin/powershell`. This affects the system modules and
profile as well.
