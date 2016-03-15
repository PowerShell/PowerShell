#!/usr/bin/env bash

# Test for build dependencies
hash cmake 2>/dev/null || { echo >&2 "No cmake, please run 'sudo apt-get install cmake'"; exit 1; }
hash g++ 2>/dev/null || { echo >&2 "No g++, please run 'sudo apt-get install g++'"; exit 1; }
hash dotnet 2>/dev/null || { echo >&2 "No dotnet, please visit https://dotnet.github.io/getting-started/"; exit 1; }

# Test for lock file
test -r test/csharp/project.lock.json || { echo >&2 "Please run 'dotnet restore' to download .NET Core packages"; exit 2; }

# Build native components
pushd src/libpsl-native
cmake -DCMAKE_BUILD_TYPE=Debug .
make -j
ctest -V
## Work-around dotnet/cli#710
export LD_LIBRARY_PATH="$LD_LIBRARY_PATH:$(pwd)/src"
popd

# Run xUnit tests
pushd test/csharp
## Build
dotnet build
## Work-around dotnet/cli#753
cp -r ../../src/Microsoft.PowerShell.Linux.Host/{Modules,*ps1xml} bin/Debug/netstandardapp1.5/ubuntu.14.04-x64
## Test
dotnet test
popd
