#!/bin/bash

SCRIPTDIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)

cd $SCRIPTDIR
echo "launching PS now"
PSMODULEPATH=$SCRIPTDIR/Modules LD_LIBRARY_PATH=. ./host_cmdline -c ../coreclr -alc Microsoft.PowerShell.CoreCLR.AssemblyLoadContext.dll -tpa powershell-simple.exe "powershell-simple, version=1.0.0.0, culture=neutral, PublicKeyToken=null" "ps_hello_world.Program" "UnmanagedMain" "$@"

