#!/usr/bin/env bash
set -e

dotnet restore
pushd src/ResGen
dotnet run
popd

pushd src/TypeCatalogParser
dotnet run
popd

pushd src/TypeCatalogGen
dotnet run ../Microsoft.PowerShell.CoreCLR.AssemblyLoadContext/CorePsTypeCatalog.cs powershell.inc
popd

pushd src/libpsl-native
cmake -DCMAKE_BUILD_TYPE=Debug .
make -j
make test
popd

pushd src/powershell-unix
dotnet build --configuration Linux
popd

