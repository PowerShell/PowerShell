#!/usr/bin/env bash

export BIN=$(pwd)/bin

# Build native components
(
    cd src/libpsl-native
    cmake -DCMAKE_BUILD_TYPE=Debug .
    make -j
    ctest -V
    cp src/libpsl-native.* $BIN
)

# Publish PowerShell
(
    cd src/Microsoft.PowerShell.Linux.Host
    dotnet publish --framework dnxcore50 --output $BIN --configuration Linux
    # Copy files that dotnet-publish does not currently deploy
)
