#!/usr/bin/env bash

cd src/Microsoft.PowerShell.Linux.UnitTests
source ~/.dnx/dnvm/dnvm.sh
dnvm use 1.0.0-rc2-16237 -r coreclr
dnx test
cd ../..
