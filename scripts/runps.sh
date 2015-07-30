#!/bin/bash

SCRIPTDIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)

cd $SCRIPTDIR
echo "launching PS now"
PSMODULEPATH=$SCRIPTDIR/Modules LD_LIBRARY_PATH=. ./host_cmdline -c ../coreclr -alc Microsoft.PowerShell.CoreCLR.AssemblyLoadContext.dll -tpa powershell-run.exe "powershell-run, version=1.0.0.0, culture=neutral, PublicKeyToken=null" "Microsoft.Samples.PowerShell.Host.PSListenerConsoleSample" "UnmanagedMain" "$@"

