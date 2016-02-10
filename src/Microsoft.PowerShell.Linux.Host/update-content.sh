#!/usr/bin/env bash

# This script is used to update our current tree's files with those that live
# in `src/monad`. We only need to update them when new changes are merged from
# Source Depot, and the build scripts are much simpler without this logic. With
# the files living here, the `content` key in the `project.json` handles the
# deployment for us in a cross-platform manner.

# Note that many of these should probably live with
# System.Management.Automation, but we're waiting on a bug fix from CLI to move
# them.

# Types files
cp ../monad/monad/miscfiles/types/CoreClr/types.ps1xml .
cp ../monad/monad/miscfiles/types/CoreClr/typesv3.ps1xml .

# Format files
cp ../monad/monad/miscfiles/display/Certificate.format.ps1xml .
cp ../monad/monad/miscfiles/display/Diagnostics.Format.ps1xml Diagnostics.format.ps1xml
cp ../monad/monad/miscfiles/display/DotNetTypes.format.ps1xml .
cp ../monad/monad/miscfiles/display/Event.format.ps1xml .
cp ../monad/monad/miscfiles/display/FileSystem.format.ps1xml .
cp ../monad/monad/miscfiles/display/Help.format.ps1xml .
cp ../monad/monad/miscfiles/display/HelpV3.format.ps1xml .
cp ../monad/monad/miscfiles/display/PowerShellCore.format.ps1xml .
cp ../monad/monad/miscfiles/display/PowerShellTrace.format.ps1xml .
cp ../monad/monad/miscfiles/display/Registry.format.ps1xml .
cp ../monad/monad/miscfiles/display/WSMan.format.ps1xml .

mkdir Modules
cp -r ../monad/monad/miscfiles/modules/Microsoft.PowerShell.Utility Modules 
UTILSCLR=Modules/Microsoft.PowerShell.Utility/CoreClr
mv $UTILSCLR/* Modules/Microsoft.PowerShell.Utility && rmdir $UTILSCLR
cp -r ../monad/monad/miscfiles/modules/Microsoft.PowerShell.Security Modules
cp -r ../monad/monad/miscfiles/modules/Microsoft.PowerShell.Management Modules
cp -r ../monad/monad/miscfiles/modules/PSDiagnostics Modules
