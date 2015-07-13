# PowerShell for Linux

## Getting started

These instructions assume Ubuntu 14.04 LTS, the same as our dependency, [CoreCLR][]. Fortunately you do not have to [build CoreCLR][], as we bundle the dependencies in submodules.

[CoreCLR]: https://github.com/dotnet/coreclr
[build CoreCLR]: https://github.com/dotnet/coreclr/blob/master/Documentation/building/linux-instructions.md

### Obtain the source code

#### Install source control tools

- [Git][], the version control system
- Node.js, to run the Visual Studio Online `mshttps` Git remote helper
- `smbclient`, to obtain mshttps
- `ntpdate`, to update the system time

```sh
sudo apt-get install git nodejs nodejs-legacy smbclient ntpdate
```

#### Setup Git

The user name and email must be set to do just about anything with Git.

```sh
git config --global user.name "First Last"
git config --global user.email "alias@microsoft.com"
```

#### Setup Visual Studio Online

Teach Git to use the `mshttps` protocol for Visual Studio Online. The URL mapping (and `mshttps` itself) is needed for the two factor authentication that internal VSO imposes.

```sh
git config --global url.mshttps://msostc.visualstudio.com/.insteadof https://msostc.visualstudio.com/
git config --global url.mshttps://microsoft.visualstudio.com/.insteadof https://microsoft.visualstudio.com/
```

Download `mshttps` using SMB from a Windows share.

> Alternatively you can get `git-remote-mshttps.tar.gz` on Windows and upload it to your Linux machine.

```sh
smbclient --user=domain\\username --directory=drops\\RemoteHelper.NodeJS\\latest \\\\gitdrop\\ProjectJ -c "get git-remote-mshttps.tar.gz"
```

> If the file transfer fails with `tree connect failed: NT_STATUS_DUPLICATE_NAME`, use `nslookup gitdrop` to obtain its canonical name (currently `osgbldarcfs02.redmond.corp.microsoft.com`) and use it instead.

Install `mshttps`, and update the system time (necessary for authentication with VSO).

```sh
sudo tar -xvf git-remote-mshttps.tar.gz -C /usr/local/bin
sudo chmod +x /usr/local/bin/git-remote-mshttps
sudo ntpdate time.nist.gov
```

#### Download source code

Clone our [monad-linux][] source from Visual Studio Online. We use the `develop` branch, and several submodules, necessitating the arguments.

```sh
git clone -b develop --recursive https://msostc.visualstudio.com/DefaultCollection/PS/_git/monad-linux
```

[monad-linux]: https://msostc.visualstudio.com/DefaultCollection/PS/_git/monad-linux

### Setup build environment

If you use Docker, this part is already done for you; just prefix your build commands with `./build.sh`. But you do need Docker.

#### Use Docker

Setting up Docker has been made as simple as running a script.

```sh
wget -qO- https://get.docker.com/ | sh
```

To make Docker work better on Ubuntu, you should also setup a [Docker group][].

```sh
sudo usermod -aG docker <your local user>
```

Then log out and back in. This eliminates the need to `sudo` before every Docker command.

Check the official [installation documentation][] first if you have problems setting it up.

[Docker group]: https://docs.docker.com/installation/ubuntulinux/#create-a-docker-group
[installation documentation]: https://docs.docker.com/installation/ubuntulinux/

##### Technical info

We have an [automated build repository][] on the Docker Hub that provisions an image from this [Dockerfile][]. This image contains all the necessary build dependencies, and is based on Ubuntu 14.04.

Using this image amounts to running an ephemeral container with the local source code mounted as a shared volume, which is precisely what `build.sh` does (as well as pass on command-line arguments). If the `andschwa/magrathea` image is not already present, it is automatically pulled from the Hub.

```sh
docker run --rm --interactive --tty --volume /absolute/path/to/monad-linux/:/opt/monad --workdir /opt/monad/scripts andschwa/magrathea make run
```

It is run interactively with a tty, and so acts as if a shell had been opened to the container. The actual compilation takes place in the mounted volume, that is, the host machine's local source code repository. The magic of Docker is that the compilation processes take place in the context of the container, and so have all the dependencies satisfied. To prevent literring the host with containers, it is automatically removed when it exits; this is not a problem because all side effects happen on the host's file system, and similarly creating the container requires very minimal overhead.

[automated build repository]: https://registry.hub.docker.com/u/andschwa/magrathea/
[Dockerfile]: https://github.com/andschwa/docker-magrathea/blob/master/Dockerfile

#### Manually install dependencies

> Skip this section if you installed Docker.

Setup the Mono package [repository][] because Ubuntu's Mono packages are out of date.

```sh
sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
echo "deb http://download.mono-project.com/repo/debian wheezy main" | sudo tee /etc/apt/sources.list.d/mono-xamarin.list
sudo apt-get update
```

Install necessary packages.

- [Mono][], the C# compiler for Linux
- Nuget, the C# package manager
- libunwind8, used to determine the call-chain
- GCC and G++, for compiling C and C++ native code
- [GNU Make][], for building `monad-linux`
- [CMake][], for building `src/monad-native`

```sh
sudo apt-get install mono-devel nuget libunwind8 gcc g++ make cmake
```

[repository]: http://www.mono-project.com/docs/getting-started/install/linux/#debian-ubuntu-and-derivatives
[Git]: https://git-scm.com/documentation
[Mono]: http://www.mono-project.com/docs/
[GNU Make]: https://www.gnu.org/software/make/manual/make.html
[CMake]: http://www.cmake.org/cmake/help/v2.8.12/cmake.html


### Building

If you're using the Docker container, just prefix all build steps like so: `./build.sh make test`

1. `cd scripts` since it contains the `Makefile` and `build.sh`
2. `make prepare` will use Nuget to download several dependencies
3. `make all` will build PowerShell for Linux
4. `make run` will execute a demo, `"a","b","c","a","a" | Select-Object -Unique`
5. `make run-interactive` will open an interactive PowerShell console
6. `make test` will execute the unit tests
7. `make clean` will remove the built objects
8. `make cleanall` will also remove the Nuget packages

## TODO: Unit tests

### Adding Pester tests

Pester tests are located in the src/pester-tests folder. The makefile targets "test" and "pester-tests" will run all Pester-based tests.

The steps to add your pester tests are:
- add *.Tests.ps1  files to src/pester-tests
- run "make pester-tests" to run the tests

## TODO: Docker shell-in-a-box

## TODO: Architecture
