#!/usr/bin/env bash
set -e

if hash powershell 2>/dev/null; then
    echo 'Continuing with `powershell -noprofile -c Start-PSBuild`'
    powershell -noprofile -c "Import-Module ./build.psm1; Start-PSBuild"
else
   echo 'Continuing with full manual build'
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
fi
