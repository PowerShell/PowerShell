#!/usr/bin/env bash

pushd Microsoft.PowerShell.Linux.Host
dnu publish --out ../bin --runtime dnx-coreclr-linux-x64.1.0.0-rc2-16177 --no-source
popd
