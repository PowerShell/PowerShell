#!/usr/bin/env bash

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
cp -r ../../src/Microsoft.PowerShell.Linux.Host/{Modules,*ps1xml} bin/Debug/dnxcore50/
## Test
dotnet test
popd
