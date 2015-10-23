#!/bin/bash

CWD=$(pwd)
SCRIPTDIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)

cd "$SCRIPTDIR"
CORE_ROOT=../coreclr PWRSH_ROOT=. PSMODULEPATH="$SCRIPTDIR/Modules" LD_LIBRARY_PATH="$SCRIPTDIR" ./host_cmdline -alc Microsoft.PowerShell.CoreCLR.AssemblyLoadContext.dll powershell-run.exe "$@"
