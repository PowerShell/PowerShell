#!/usr/bin/env bash

if hash powershell 2>/dev/null; then
    echo 'Continuing with `powershell -noprofile -c Start-PSBuild`'
    powershell -noprofile -c "Import-Module ./build.psm1; Start-PSBuild"
else
    echo 'No `powershell`, see docs/building/linux.md or macos.md to build PowerShell!'
fi
