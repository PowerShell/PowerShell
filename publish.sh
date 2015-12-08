#!/usr/bin/env bash

cd src/Microsoft.PowerShell.Linux.Host
dotnet publish --framework dnxcore50 --runtime ubuntu.14.04-x64 --output ../../bin
cp *.ps1xml ../../bin
cd ../..
