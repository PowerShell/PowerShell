#!/bin/sh

repoRoot=$1
destination=$2
releaseTag=$3

# Build libpsl-native
cd $repoRoot/src/libpsl-native

cmake -DCMAKE_BUILD_TYPE=Debug .
make -j

# Restore packages
cd ../..
dotnet restore

# Add telemetry file
touch DELETE_ME_TO_DISABLE_CONSOLEHOST_TELEMETRY

# run ResGen
cd src/ResGen
dotnet run

# Create typeCatalog
cd ..
targetFile="Microsoft.PowerShell.SDK/obj/Microsoft.PowerShell.SDK.csproj.TypeCatalog.targets"
cat > $targetFile <<-"EOF"
<Project>
  <Target Name="_GetDependencies"
          DependsOnTargets="ResolveAssemblyReferencesDesignTime">
    <ItemGroup>
      <_RefAssemblyPath Include="%(_ReferencesFromRAR.ResolvedPath)%3B" Condition=" '%(_ReferencesFromRAR.Type)' == 'assembly' And '%(_ReferencesFromRAR.PackageName)' != 'Microsoft.Management.Infrastructure' " />
    </ItemGroup>
    <WriteLinesToFile File="$(_DependencyFile)" Lines="@(_RefAssemblyPath)" Overwrite="true" />
  </Target>
</Project>
EOF

dotnet msbuild Microsoft.PowerShell.SDK/Microsoft.PowerShell.SDK.csproj /t:_GetDependencies "/property:DesignTimeBuild=true;_DependencyFile=$(pwd)/TypeCatalogGen/powershell.inc" /nologo

cd TypeCatalogGen
dotnet run ../System.Management.Automation/CoreCLR/CorePsTypeCatalog.cs powershell.inc

# build PowerShell
cd ../powershell-unix
dotnet publish --configuration Linux --runtime linux-x64

# add libpsl-native to build
mv libpsl-native.so bin/Linux/netcoreapp2.0/linux-x64

# tar build for output
cd bin/Linux/netcoreapp2.0/linux-x64

# TODO make format of file name the same as other packages
tar -czvf $destination/powershell-alpine.3.tar.gz .
