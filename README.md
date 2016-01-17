# PowerShell on Linux / OS X / Windows

## Obtain the source code

### Setup Git

Install [Git][], the version control system. If you're new to Git, peruse the
documentation and go through some tutorials; I recommend familiarizing yourself
with `checkout`, `branch`, `pull`, `push`, `merge`, and after a while, `rebase`
and `cherry-pick`. Please commit early and often.

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

I highly recommend these configurations to help deal with whitespace, rebasing,
and general use of Git.

> Auto-corrects your command when it's sure (`stats` to `status`)
```sh
git config --global help.autoCorrect -1
```

> Refuses to merge when pulling, and only pushes to branch with same name.
```sh
git config --global pull.ff only
git config --global push.default current
```

> Shows shorter commit hashes and always shows reference names in the log.
```sh
git config --global log.abbrevCommit true
git config --global log.decorate short
```

> Ignores whitespace changes and uses more information when merging.
```sh
git config --global apply.ignoreWhitespace change
git config --global rerere.enabled true
git config --global rerere.autoUpdate true
git config --global am.threeWay true
```

[Git]: https://git-scm.com/documentation
[token]: https://help.github.com/articles/creating-an-access-token-for-command-line-use/

### Download source code

Clone this repository recurisvely, as it's the superproject with a number of
submodules.

```sh
git clone --recursive https://github.com/PowerShell/PowerShell-Linux.git
```

*Read the documentation on [submodules][] if you're not familiar with them.*

Note that because GitHub's "Merge Pull Request" button merges with `--no-ff`,
an extra merge commit will always be created. This can be especially annoying
when trying to commit updates to submodules. Therefore our policy is to merge
using the Git CLI after approval, preferably with a rebase to enable a
fast-forward merge.

[submodules]: https://www.git-scm.com/book/en/v2/Git-Tools-Submodules

## Setup build environment

We use the [.NET Command Line Interface][dotnet-cli] (`dotnet-cli`) to build
the managed components, and [CMake][] to build the native components (on
non-Windows platforms). Install `dotnet-cli` by following their documentation
(make sure to install the `dotnet-dev` package on Linux to get the latest
version). Then install the following dependencies Linux and OS X.

> Note that OS X dependency installation instructions are not yet documented,
> and Windows only needs `dotnet-cli`.

### Linux

Tested on Ubuntu 14.04 and OS X 10.11.


```sh
sudo apt-get install g++ cmake make lldb-3.6 strace
```

#### OMI

To develop on the PowerShell Remoting Protocol (PSRP), you'll need to be able
to compile OMI, which additionally requires:

```sh
sudo apt-get install libpam0g-dev libssl-dev libcurl4-openssl-dev libboost-filesystem-dev 
```

[dotnet-cli]: https://github.com/dotnet/cli#new-to-net-cli
[CMake]: https://cmake.org/cmake/help/v2.8.12/cmake.html

## Building

*The command `dotnet restore` must be done at least once from the top directory
to obtain all the necessary .NET packages.*

Build with `./build.sh` on Linux and OS X, and `./build.ps1` on Windows.

## Running

### Linux / OS X

- launch local shell with `./bin/powershell`
- launch local shell in LLDB with `./debug.sh`
- launch `omiserver` for PSRP (and in LLDB) with `./prsp.sh`, and connect with `Enter-PSSession` from Windows

### Windows

Launch `./bin/powershell.exe`. The console output isn't the prettiest, bet the
vast majority of Pester tests pass.

## Known Issues

### xUnit

Sadly, `dotnet-test` is not fully supported on Linux, so our xUnit tests do not
currently run. We may be able to work around this, or get the `dotnet-cli` team
to fix their xUnit runner. GitHub
[issue](https://github.com/dotnet/cli/issues/18).

## Detailed Build Notes

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

- `libpsnative.so`: native functions that `CorePsPlatform.cs` P/Invokes
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

#### registry-stub

Provides `RegCloseKey()` to satisfy the disposal of `SafeHandle` objects on shutdown.

```sh
cd src/registry-stub
make
cp api-ms-win-core-registry-l1-1-0.dll $BIN
```

### PowerShell Remoting Protocol

PSRP communication is tunneled through OMI using the `omi-provider`.
These build steps are not part of the `./build.sh` script.

> PSRP has been observed working on OS X, but the changes made to OMI to
> accomplish this are not even beta-ready and need to be done correctly.

#### OMI

```sh
cd src/omi/Unix
./configure --dev
make -j
```

#### Provider

The provider uses CMake to build, link, and register with OMI.

```sh
cd src/omi-provider
cmake .
make -j
```

The provider also maintains its own native host library to initialize the CLR,
but there are plans to refactor .NET's packaged host as a shared library.

### DSC

DSC also uses OMI, so build it first, then build DSC against it. Unfortunately,
DSC cannot be configured to look for OMI elsewhere, so for now you need to
symlink it to the expected location.

```sh
ln -s ../omi/Unix/ omi-1.0.8
./configure --no-rpm --no-dpkg --local
make -j
```

