#!/usr/bin/env bash

export BIN=$(pwd)/bin

mkdir -p $BIN/Modules

# Deploy PowerShell modules
(
    cd $BIN/Modules
    cp -r ../../test/Pester .
    cp -r ../../src/monad/monad/miscfiles/modules/Microsoft.PowerShell.Utility .
    cp ../../src/monad/monad/miscfiles/modules/Microsoft.PowerShell.Utility/CoreClr/* Microsoft.PowerShell.Utility
    cp -r ../../src/monad/monad/miscfiles/modules/Microsoft.PowerShell.Security .
    cp -r ../../src/monad/monad/miscfiles/modules/Microsoft.PowerShell.Management .
    cp -r ../../src/monad/monad/miscfiles/modules/PSDiagnostics .
    OMI=Microsoft.PowerShell.Commands.Omi
    mkdir -p $OMI
    ln -sf $BIN/$OMI.dll $OMI/
)

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
