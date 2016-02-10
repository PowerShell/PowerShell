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
pushd src/Microsoft.PowerShell.Linux.Host
dotnet publish --framework dnxcore50 --output $BIN --configuration Linux
popd
