#!/usr/bin/env bash

export BIN=$(pwd)/bin
mkdir -p $BIN

# Build native components
pushd src/libpsl-native
cmake -DCMAKE_BUILD_TYPE=Debug .
make -j
ctest -V
cp src/libpsl-native.* $BIN
popd

# Publish PowerShell
dotnet publish --output $BIN --configuration Linux src/Microsoft.PowerShell.Linux.Host
