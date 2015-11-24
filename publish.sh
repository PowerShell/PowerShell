#!/usr/bin/env bash

cd src/Microsoft.PowerShell.Linux.Host
source ~/.dnx/dnvm/dnvm.sh
dnvm use 1.0.0-rc2-16177
dnu publish --out ../.. --runtime dnx-coreclr-linux-x64.1.0.0-rc2-16177 --no-source
cd ../..
