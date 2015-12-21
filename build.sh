#!/usr/bin/env bash

export BIN=$(pwd)/bin

mkdir -p $BIN/Modules

# Deploy PowerShell modules
(
    cd $BIN/Modules
    ln -sf ../../ext-src/pester Pester
    ln -sf ../../src/monad/monad/miscfiles/modules/Microsoft.PowerShell.Utility .
)

# Build native components
(
    cd src/monad-native
    cmake -DCMAKE_BUILD_TYPE=Debug .
    make -j
    ctest -V
    # Deploy development copy of libpsnative
    cp native/libpsnative.so $BIN
)

# Build registry stub (this should go away, again)
(
    cd src/registry-stub
    make
    cp api-ms-win-core-registry-l1-1-0.dll $BIN
)

# Publish PowerShell
(
    cd src/Microsoft.PowerShell.Linux.Host
    dotnet publish --framework dnxcore50 --runtime ubuntu.14.04-x64 --output $BIN
    # Copy files that dotnet-publish doesn't currently deploy
    cp *.ps1xml *_profile.ps1 $BIN
)
