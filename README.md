# PowerShell on Linux / OS X / Windows

## Obtain the source code

### Setup Git

Install [Git][], the version control system. If you're new to Git, work through
the [guides][] until you are familiar with the following commands: `checkout`,
`branch`, `pull`, `push`, `merge`. Don't forget to commit early and often!

The user name and email must be set to do just about anything with Git.

```sh
git config --global user.name "First Last"
git config --global user.email "alias@microsoft.com"
```

If you do not have a preferred method of authentication, enable the storage
credential helper, which will cache your credentials in plaintext on your
system, so use a [token][].

```sh
git config --global credential.helper store
```

**Read the documentation on [submodules][]!**

See the [Contributing Guidelines](CONTRIBUTING.md) for more Git information.

[Git]: https://git-scm.com/documentation
[guides]: https://guides.github.com/activities/hello-world/
[token]: https://help.github.com/articles/creating-an-access-token-for-command-line-use/
[submodules]: https://www.git-scm.com/book/en/v2/Git-Tools-Submodules

### Download source code

Clone this repository recursively, as it's the superproject with a number of
submodules.

```sh
git clone --recursive https://github.com/PowerShell/PowerShell-Linux.git
```

The `src/omi` submodule requires your GitHub user to have joined the Microsoft
organization. If it fails to check out, Git will bail and not check out further
submodules either.

On Windows, many fewer submodules are needed, so don't use `clone --recursive`.

Instead run:

```
git clone https://github.com/PowerShell/PowerShell-Linux.git
git submodule update --init --recursive -- src/monad src/windows-build test/Pester
```

## Setup build environment

We use the [.NET Command Line Interface][dotnet-cli] (`dotnet-cli`) to build
the managed components, and [CMake][] to build the native components (on
non-Windows platforms). Install `dotnet-cli` by following their documentation
(make sure to install the `dotnet-nightly` package on Linux to get the latest
version). Then install the following dependencies Linux and OS X.

> Note that OS X dependency installation instructions are not yet documented,
> and Windows only needs `dotnet-cli`.

> Previous installations of DNX or `dnvm` can cause `dotnet-cli` to fail.

### Linux

Tested on Ubuntu 14.04 and OS X 10.11.

```sh
sudo apt-get install g++ cmake make lldb-3.6 strace
```

#### OMI

To develop on the PowerShell Remoting Protocol (PSRP) for Linux, you'll need to
be able to compile OMI, which additionally requires:

```sh
sudo apt-get install libpam0g-dev libssl-dev libcurl4-openssl-dev libboost-filesystem-dev 
```

[dotnet-cli]: https://github.com/dotnet/cli#new-to-net-cli
[CMake]: https://cmake.org/cmake/help/v2.8.12/cmake.html

## Building

**The command `dotnet restore` must be done at least once from the top directory
to obtain all the necessary .NET packages.**

Build with `./build.sh` on Linux and OS X, and `./build.ps1` on Windows.

Specifically:

### Linux

> In Bash:
```sh
cd PowerShell-Linux
dotnet restore
./build.sh
```

### Windows

> In PowerShell:
```powershell
cd PowerShell-Linux
dotnet restore
./build.ps1
```

## Running

### Linux / OS X

- launch local shell with `./bin/powershell`
- launch local shell in LLDB with `./debug.sh`
- launch `omiserver` for PSRP (and in LLDB) with `./prsp.sh`, and connect with `Enter-PSSession` from Windows

### Windows

Launch `./bin/powershell.exe`. The console output isn't the prettiest, but the
vast majority of Pester tests pass. Run them in the console with `Invoke-Pester
test/powershell`.

## PowerShell Remoting Protocol

PSRP communication is tunneled through OMI using the `omi-provider`.

**These build steps are not part of the `./build.sh` script.**

> PSRP has been observed working on OS X, but the changes made to OMI to
> accomplish this are not even beta-ready and need to be done correctly. They
> exist on the `andschwa-osx` branch of the OMI repository.

### Build OMI

```sh
cd src/omi/Unix
./configure --dev
make -j
cd ../../..
```

### Build Provider

The provider uses CMake to build, link, and register with OMI.

```sh
cd src/omi-provider
cmake .
make -j
cd ../..
```

The provider also maintains its own native host library to initialize the CLR,
but there are plans to refactor .NET's packaged host as a shared library.

### Running

Some initial setup on Windows is required. Open an administrative command
prompt and execute the following:

```cmd
winrm set winrm/config/Client @{AllowUnencrypted="true"}
winrm set winrm/config/Client @{TrustedHosts="*"}
```

Then on Linux, launch `omiserver` in the debugger (after building with the
instructions above):

```sh
./psrp.sh
run
```

> The `run` command is executed inside of LLDB (the debugger) to start the
`omiserver` process.

Now in a PowerShell prompt on Windows (opened after setting the WinRM client
configurations):

```powershell
Enter-PSSession -ComputerName <IP address of Linux machine> -Credential $cred -Authentication basic
```

> The `$cred` variable can be empty; a credentials prompt will appear, enter
> any fake credentials you wish as authentication is not yet implemented.

### Desired State Configuration

> DSC support is in its infancy.

DSC also uses OMI, so build it first, then build DSC against it. Unfortunately,
DSC cannot be configured to look for OMI elsewhere, so for now you need to
symlink it to the expected location.

```sh
ln -s ../omi/Unix/ omi-1.0.8
./configure --no-rpm --no-dpkg --local
make -j
```

## Detailed Build Script Notes

> This explains `./build.sh`.

The variable `$BIN` is the output directory, `bin`.

### Managed

Builds with `dotnet-cli`. Publishes all dependencies into the `bin` directory.
Emits its own native host as `bin/powershell`.

```sh
cd src/Microsoft.PowerShell.Linux.Host
dotnet publish --framework dnxcore50 --output $BIN
# Copy files that dotnet-publish doesn't currently deploy
cp *.ps1xml *_profile.ps1 $BIN
```

### Native

- `libpsl-native.so`: native functions that `CorePsPlatform.cs` P/Invokes
- `api-ms-win-core-registry-l1-1-0.dll`: registry stub to prevent missing DLL error on shutdown

#### libpsl-native

Driven by CMake, with its own unit tests using Google Test.

```sh
cd src/libpsl-native
cmake -DCMAKE_BUILD_TYPE=Debug .
make -j
ctest -V
# Deploy development copy of libpsl-native
cp native/libpsl-native.* $BIN
```

The output is a `.so` on Linux and `.dylib` on OS X. It is unnecessary for Windows.

#### registry-stub

Provides `RegCloseKey()` to satisfy the disposal of `SafeHandle` objects on shutdown.

```sh
cd src/registry-stub
make
cp api-ms-win-core-registry-l1-1-0.dll $BIN
```
