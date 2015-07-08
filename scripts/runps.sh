#!/bin/bash

SCRIPTDIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)

cd $SCRIPTDIR
echo "launching PS now"
PSMODULEPATH=$SCRIPTDIR/Modules LD_LIBRARY_PATH=. ./corerun powershell-run.exe "$@"

