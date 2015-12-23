#!/usr/bin/env bash

export BIN=$(pwd)/bin

mkdir -p $BIN/Modules

# Deploy PowerShell modules
(
    cd $BIN/Modules
    ln -sf ../../ext-src/pester Pester
    ln -sf ../../src/monad/monad/miscfiles/modules/Microsoft.PowerShell.Utility .
    OMI=Microsoft.PowerShell.Commands.Omi
    mkdir -p $OMI
    ln -sf $BIN/$OMI.dll $OMI/
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
    # Copy files that dotnet-publish does not currently deploy
    cp *_profile.ps1 $BIN
)

# Symlink types and format files to correct names
(
    cd $BIN

    ln -sf ../src/monad/monad/miscfiles/types/CoreClr/types.ps1xml .
    ln -sf ../src/monad/monad/miscfiles/types/CoreClr/typesv3.ps1xml .

    ln -sf ../src/monad/monad/miscfiles/display/Certificate.format.ps1xml .
    ln -sf ../src/monad/monad/miscfiles/display/Diagnostics.Format.ps1xml Diagnostics.format.ps1xml
    ln -sf ../src/monad/monad/miscfiles/display/DotNetTypes.format.ps1xml .
    ln -sf ../src/monad/monad/miscfiles/display/Event.format.ps1xml .
    ln -sf ../src/monad/monad/miscfiles/display/FileSystem.format.ps1xml .
    ln -sf ../src/monad/monad/miscfiles/display/Help.format.ps1xml .
    ln -sf ../src/monad/monad/miscfiles/display/HelpV3.format.ps1xml .
    ln -sf ../src/monad/monad/miscfiles/display/PowerShellCore.format.ps1xml .
    ln -sf ../src/monad/monad/miscfiles/display/PowerShellTrace.format.ps1xml .
    ln -sf ../src/monad/monad/miscfiles/display/Registry.format.ps1xml .
    ln -sf ../src/monad/monad/miscfiles/display/WSMan.format.ps1xml .
)
