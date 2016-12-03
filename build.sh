#!/usr/bin/env bash
set -e

if hash powershell 2>/dev/null; then
    echo 'Continuing with `powershell -noprofile -c Start-PSBuild`'
    powershell -noprofile -c "Import-Module ./build.psm1; Start-PSBuild"
else
   echo 'Continuing with full manual build'
   ./build-from-scratch.sh
fi
