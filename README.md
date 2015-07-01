# PowerShell for Linux

## Getting started

These instructions assume Ubuntu 14.04 LTS, the same as our dependency, [CoreCLR][]. Fortunately you do not have to [build CoreCLR][], as we bundle the dependencies in submodules.

[CoreCLR]: https://github.com/dotnet/coreclr
[build CoreCLR]: https://github.com/dotnet/coreclr/blob/master/Documentation/building/linux-instructions.md

### Installing dependencies

1. Setup the Mono package [repository][] because Ubuntu's Mono
   packages are out of date.

```sh
sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
echo "deb http://download.mono-project.com/repo/debian wheezy main" | sudo tee /etc/apt/sources.list.d/mono-xamarin.list
sudo apt-get update
```

2. Install necessary packages.

- [Git][], the version control system
- [Mono][], the C# compiler for Linux
- Nuget, the C# package manager
- libunwind8, used to determine the call-chain
- GCC and G++, for compiling C and C++ native code
- [GNU Make][], for building `monad-linux`
- [CMake][], for building `src/monad-native`
- Node.js, to run the Visual Studio Online mshttps Git remote helper
- smbclient, to obtain mshttps
- ntpdate, to update the system time

```sh
sudo apt-get install git mono-devel nuget libunwind8 gcc g++ make cmake nodejs nodejs-legacy smbclient ntpdate
```

[repository]: http://www.mono-project.com/docs/getting-started/install/linux/#debian-ubuntu-and-derivatives
[Git]: https://git-scm.com/documentation
[Mono]: http://www.mono-project.com/docs/
[GNU Make]: https://www.gnu.org/software/make/manual/make.html
[CMake]: http://www.cmake.org/cmake/help/v2.8.12/cmake.html

### Obtaining sources

1. Configure Git.

The user name and email must be set to do just about anything with Git. The URL mapping (and mshttps itself) is needed for the two factor authentication that internal VSO imposes.

```sh
git config --global user.name "First Last"
git config --global user.email "alias@microsoft.com"
git config --global url.mshttps://msostc.visualstudio.com/.insteadof https://msostc.visualstudio.com/
git config --global url.mshttps://microsoft.visualstudio.com/.insteadof https://microsoft.visualstudio.com/
```

2. Install VSO's Git mshttps remote helper, and update the system time (necessary for authentication with VSO).

```sh
smbclient --user=domain\\username --directory=drops\\RemoteHelper.NodeJS\\latest \\\\gitdrop\\ProjectJ -c "get git-remote-mshttps.tar.gz"
sudo tar -xvf git-remote-mshttps.tar.gz -C /usr/local/bin
sudo chmod +x /usr/local/bin/git-remote-mshttps
sudo ntpdate time.nist.gov
```

If the file transfer fails with `tree connect failed: NT_STATUS_DUPLICATE_NAME`, use `nslookup gitdrop` to obtain its canonical name (currently `osgbldarcfs02.redmond.corp.microsoft.com`) and use it instead.

3. Clone our [monad-linux][] source from Visual Studio Online. We use the `develop` branch, and several submodules, necessitating the arguments.

```sh
git clone -b develop --recursive https://msostc.visualstudio.com/DefaultCollection/PS/_git/monad-linux
```

[monad-linux]: https://msostc.visualstudio.com/DefaultCollection/PS/_git/monad-linux

### Building

1. `cd scripts` since it contains the `Makefile`
2. `make prepare` will use Nuget to download several dependencies
3. `make all` will build PowerShell for Linux
4. `make run` will execute a demo, `"a","b","c","a","a" | Select-Object -Unique`
5. `make run-interactive` will open an interactive PowerShell console
6. `make test` will execute the unit tests
7. `make clean` will remove the built objects
8. `make cleanall` will also remove the Nuget packages

## TODO: Unit tests

## TODO: Docker setup

## TODO: Architecture
