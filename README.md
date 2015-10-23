# PowerShell for Linux

## Obtain the source code

### Setup Git

Install [Git][], the version control system. If you're new to Git, peruse the documentation and go through some tutorials; I recommend familiarizing yourself with `checkout`, `branch`, `pull`, `push`, `merge`, and after a while, `rebase` and `cherry-pick`.

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

Please read the documentation on [submodules][] if you're not familiar with them.

[monad-linux]: https://msostc.visualstudio.com/DefaultCollection/PS/_git/monad-linux
[submodules]: https://www.git-scm.com/book/en/v2/Git-Tools-Submodules

## Setup build environment

### Docker

See the official [installation documentation][] on how to install Docker, and don't forget to setup a [Docker group][].

The Docker container can be updated with `docker pull andschwa/magrathea`, which downloads it from the [automated build repository][].

This container isolates all our build dependencies, including Ubuntu 14.04 and Mono. See the [Dockerfile][] to look under the hood.

The `monad-docker.sh` script has two Bash functions, `monad-run` and `monad-tty`, which are wrappers that start a temporary container with `monad-linux` mounted and runs the arguments given to the script as your current user, but inside the container. The build artifacts will exist in your local folder and be owned by your user, essentially making the use of the container invisible. The `monad-tty` version also allocates a shell for the container, and so allows you to launch Bash or an interactive PowerShell session. Since these are Bash functions, it is simplest to source the `monad-docker.sh` script directly in your `~/.bashrc`, but the `./build.sh` script will also source it and delegate to `monad-run`.

[Docker group]: https://docs.docker.com/installation/ubuntulinux/#create-a-docker-group
[installation documentation]: https://docs.docker.com/installation/ubuntulinux/
[automated build repository]: https://registry.hub.docker.com/u/andschwa/magrathea/
[Dockerfile]: https://github.com/andschwa/docker-magrathea/blob/master/Dockerfile
[Make]: https://www.gnu.org/software/make/manual/make.html
[CMake]: http://www.cmake.org/cmake/help/v2.8.12/cmake.html

## Building

1. `cd scripts` since it contains the `Makefile` and `monad-run.sh`
2. `source monad-docker.sh` to get the `monad-run` and `monad-tty` Bash functions
2. `monad-run make -j` will build PowerShell for Linux, in parallel with `-j`
3. `monad-run make -j run` will build and execute a demo, `"a","b","c","a","a" | Select-Object -Unique`
4. `monad-run make -j test` will build PowerShell and execute the unit tests
5. `monad-tty make shell` will open an interactive PowerShell console (note the `tty`)
6. `make clean` will remove the PowerShell and CoreCLR execution environment `exec_env`
7. `make clean-native` will remove `libps`
8. `make cleanall` will do the above and also remove the NuGet packages in `buildtemp`

## Adding Pester tests

Pester tests are located in the `src/pester-tests` folder. The makefile targets `test` and `pester-tests` will run all Pester tests.

The steps to add your pester tests are:
- add `*.Tests.ps1` files to `src/pester-tests`
- run `monad-run make pester-tests` to run all the tests

## TODO: Docker shell-in-a-box

## TODO: Architecture
