# PowerShell for Linux

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

### Setup Visual Studio Online authentication

To use Git's `https` protocol with VSO, you'll want to setup tokens, and have Git remember them.

1. `git config --global credential.helper store`
2. Login to <https://msostc.visualstudio.com>
3. Click your name in the upper left corner and click 'My profile'
4. Click the "Security" tab in the left pane (under "Details")
5. Click "Add"
6. Enter "msostc" for "Description"
7. Set "Expires In" to "1 year"
8. Choose " msostc" for "Accounts"
9. Choose "All scopes"
10. Click "Create Token" (you may want to copy this token somewhere safe, as VSO will not show it again!)
11. Use this token as the password when cloning (and your username for the username)

### Setup GitHub authentication

We are working to move all our repositories to GitHub. Unfortunately, this
transition is incomplete, but in progress. The DSC and OMI submodules are
hosted on GitHub, and while the former is a public repository, the latter is
private, thus requiring authentication.

You should already have your credential helper setup to "store," and so should
use a token with GitHub (instead of your plaintext password). Follow [their
instructions](https://help.github.com/articles/creating-an-access-token-for-command-line-use/).

### Download source code

Clone our [monad-linux][] source from Visual Studio Online, it's the
superproject with a number of submodules.

```sh
git clone --recursive https://msostc.visualstudio.com/DefaultCollection/PS/_git/monad-linux
```

Please read the documentation on [submodules][] if you're not familiar with
them. Note that because VSO's "Complete Pull Request" button merges with
`--no-ff`, an extra merge commit will always be created. This can be annoying
when trying to commit updates to submodules. When a submodule PR is approved,
you can "complete" it without a merge commit by merging it to develop manually
and pushing the updated head.

Our convention is to create feature branches `dev/feature` off `master`, except
in `src/monad` where we branch off `develop`.

[monad-linux]: https://msostc.visualstudio.com/DefaultCollection/PS/_git/monad-linux
[submodules]: https://www.git-scm.com/book/en/v2/Git-Tools-Submodules

## Setup build environment

We use the [.NET Command Line Interface][dotnet-cli] (`dotnet-cli`) to build
the managed components, and [CMake][] to build the native components. Install
`dotnet-cli` by following their documentation (make sure to install the
`dotnet-dev` package to get the latest version). Then install the following
dependencies (assuming Ubuntu 14.04):

```sh
sudo apt-get install g++ cmake make lldb-3.6 strace
```

### OMI

To develop on the PowerShell Remoting Protocol (PSRP), you'll need to be able
to compile OMI, which additionally requires:

```sh
sudo apt-get install libpam0g-dev libssl-dev libcurl4-openssl-dev libboost-filesystem-dev 
```

[dotnet-cli]: https://github.com/dotnet/cli#new-to-net-cli
[CMake]: https://cmake.org/cmake/help/v2.8.12/cmake.html

## Building

The command `dotnet restore` must be done at least once from the top directory
to obtain all the necessary .NET packages.

Build with `./build.sh`, which does the following steps.

> The variable `$BIN` is the output directory, `bin`.

### Native

- `libpsnative.so`: native functions that `CorePsPlatform.cs` P/Invokes
- `api-ms-win-core-registry-l1-1-0.dll`: registry stub to prevent missing DLL error on shutdown

#### monad-native

Driven by CMake, with its own unit tests using Google Test.

```sh
cd src/monad-native
cmake -DCMAKE_BUILD_TYPE=Debug .
make -j
ctest -V
# Deploy development copy of libpsnative
cp native/libpsnative.so $BIN
```

#### registry-stub

Provides `RegCloseKey()` to satisfy the disposal of `SafeHandle` objects on shutdown.

```sh
cd src/registry-stub
make
cp api-ms-win-core-registry-l1-1-0.dll $BIN
```

### Managed

Builds with `dotnet-cli`. Publishes all dependencies into the `bin` directory.
Emits its own native host as `bin/Microsoft.PowerShell.Linux.Host`.

```sh
cd src/Microsoft.PowerShell.Linux.Host
dotnet publish --framework dnxcore50 --runtime ubuntu.14.04-x64 --output $BIN
# Copy files that dotnet-publish doesn't currently deploy
cp *.ps1xml *_profile.ps1 $BIN
```

### PowerShell Remoting Protocol

PSRP communication is tunneled through OMI using the `monad-omi-provider`.
These build steps are not part of the `./build.sh` script.

#### OMI

```sh
cd src/omi/Unix
./configure --dev
make -j
```

#### Provider

The provider uses CMake to build, link, and register with OMI.

```sh
cd src/monad-omi-provider
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

## Running

- launch local shell with `./bin/Microsoft.PowerShell.Linux.Host`
- launch local shell in LLDB with `./debug.sh`
- launch `omiserver` for PSRP (and in LLDB) with `./prsp.sh`, and connect with `Enter-PSSession` from Windows

## Known Issues

### xUnit

Sadly, `dotnet-test` is not fully supported on Linux, so our xUnit tests do not
currently run. We may be able to work around this, or get the `dotnet-cli` team
to fix their xUnit runner. GitHub
[issue](https://github.com/dotnet/cli/issues/407).
