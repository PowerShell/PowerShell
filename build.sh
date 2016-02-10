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
