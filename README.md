# PowerShell for Linux

## Obtain the source code

### Setup Git

Install [Git][], the version control system. If you're new to Git, peruse the documentation and go through some tutorials; I recommend familiarizing yourself with `checkout`, `branch`, `pull`, `push`, `merge`, and after a while, `rebase` and `cherry-pick`. Please commit early and often.

The user name and email must be set to do just about anything with Git.

```sh
git config --global user.name "First Last"
git config --global user.email "alias@microsoft.com"
```

I highly recommend these configurations to help deal with whitespace, rebasing, and general use of Git.

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

### Download source code

Clone our [monad-linux][] source from Visual Studio Online, it's the superproject with a number of submodules.

```sh
git clone --recursive https://msostc.visualstudio.com/DefaultCollection/PS/_git/monad-linux
```

Please read the documentation on [submodules][] if you're not familiar with them. Note that because VSO's "Complete Pull Request" button merges with `--no-ff`, an extra merge commit will always be created. This can be annoying when trying to commit updates to submodules. When a submodule PR is approved, you can "complete" it without a merge commit by merging it to develop manually and pushing the updated head.

Our convention is to create feature branches `dev/feature` off our integration branch `develop`. We then merge `develop` to `master` every few weeks when it is stable.

[monad-linux]: https://msostc.visualstudio.com/DefaultCollection/PS/_git/monad-linux
[submodules]: https://www.git-scm.com/book/en/v2/Git-Tools-Submodules

## Setup build environment

We use the [.NET Command Line Interface][dotnet-cli] (`dotnet-cli`) to build the managed components, and [CMake][] to build the native components. Install `dotnet-cli` by following their documentation. Then install the following dependencies (assuming Ubuntu 14.04):

```sh
sudo apt-get install g++ cmake make libicu-dev libboost-filesystem-dev lldb-3.6 strace
```

### OMI

To develop on the PowerShell Remoting Protocol (PSRP), you'll need to be able to compile OMI, which additionally requires:

```sh
sudo apt-get install libpam0g-dev libssl-dev libcurl4-openssl-dev
```

[dotnet-cli]: https://github.com/dotnet/cli
[CMake]: https://cmake.org/cmake/help/v2.8.12/cmake.html

## Building

### Native

```sh
cd src/monad-native
cmake -DCMAKE_BUILD_TYPE=Debug .
VERBOSE=1 make -j
ctest -V
```

### Managed

```sh
dotnet restore
cd src/Microsoft.PowerShell.Linux.Host
dotnet publish --framework dnxcore50 --runtime ubuntu.14.04-x64 --output ../../bin
```

Now run with `./powershell`.

## Adding Pester tests

Pester tests are located in the `src/pester-tests` folder. The makefile targets `test` and `pester-tests` will run all Pester tests.

The steps to add your pester tests are:
- add `*.Tests.ps1` files to `src/pester-tests`
- run `make test` to run all the tests
