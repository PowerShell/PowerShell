#!/usr/bin/env bash

# Publish PowerShell
cd src/Microsoft.PowerShell.Linux.Host
dotnet publish --framework dnxcore50 --runtime ubuntu.14.04-x64 --output ../../bin
cp *.ps1xml ../../bin
cd ../..

# Copy Pester
mkdir -p bin/Modules/Pester
cp -r ext-src/pester bin/Modules/Pester

# Patch
cp patches/*.dll bin/
cp patches/runtime/*.so bin/
