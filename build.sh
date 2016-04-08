#!/usr/bin/env bash

if hash powershell 2>/dev/null; then
    echo 'Continuing with `powershell -c Start-PSBuild`'
    powershell -c Start-PSBuild
else
    echo 'No `powershell`, see docs/building/linux.md or osx.md to build PowerShell!'
fi
