PowerShell Remoting Protocol
============================

This guide supplements the [Linux instructions](./linux.md), as
building PowerShell Remoting Protocol (PSRP) support first requires
PowerShell on Linux built.

PSRP communication is tunneled through the Open Management
Infrastructure (OMI) using the [OMI provider][].

> PSRP has been observed working on OS X, but the changes made to OMI to
> accomplish this are not even beta-ready and need to be done correctly. They
> exist on the `andschwa-osx` branch of the OMI repository.

[OMI provider]: https://github.com/PowerShell/psl-omi-provider/

Environment
===========

Toolchain Setup
---------------

PSRP requires the following additional packages:

```sh
sudo apt-get install libpam0g-dev libssl-dev libcurl4-openssl-dev libboost-filesystem-dev
```

Git Setup
---------

Two additional submodules need to be initialized:

```sh
git submodule update --init -- src/omi src/omi-provider
```

The `src/omi` submodule requires your GitHub user to have joined the
Microsoft organization. If it fails to check out, Git will give up and
not check out further submodules either. Please follow the
instructions on the [Open Source Hub][].

[Open Source Hub]: https://opensourcehub.microsoft.com/articles/how-to-join-microsoft-github-org-self-service

Building
========

Run `./omibuild.sh` to build OMI and the provider.

This script first builds OMI in developer mode:

```sh
pushd src/omi/Unix
./configure --dev
make -j
popd
```

Then it builds and registers the provider:

```sh
pushd src/omi-provider
cmake -DCMAKE_BUILD_TYPE=Debug .
make -j
popd
```

The provider maintains its own native host library to initialize the
CLR, but there are plans to refactor .NET's packaged host as a shared
library.

Running
-------

Some initial setup on Windows is required. Open an administrative command
prompt and execute the following:

```cmd
winrm set winrm/config/Client @{AllowUnencrypted="true"}
winrm set winrm/config/Client @{TrustedHosts="*"}
```

> You can also set the `TrustedHosts` to include the target's IP address.

Then on Linux, launch `omiserver` (after building with the
instructions above):

```sh
./psrp.sh
```

Now in a PowerShell prompt on Windows (opened after setting the WinRM client
configurations):

```powershell
Enter-PSSession -ComputerName <IP address of Linux machine> -Credential $cred -Authentication basic
```

> The `$cred` variable can be empty; a credentials prompt will appear, enter
> any fake credentials you wish as authentication is not yet implemented.

The IP address of the Linux machine can be obtained with:

```sh
ip -f inet addr show dev eth0
```
