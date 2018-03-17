#!/bin/sh

apk update
apk add cmake clang build-base git bash
git clone --recursive https://github.com/kasper3/powershell
cd powershell/src/libpsl-native

cmake -DCMAKE_BUILD_TYPE=Debug .
make -j

cd ../..
dotnet restore
touch DELETE_ME_TO_DISABLE_CONSOLEHOST_TELEMETRY

cd src/ResGen
dotnet run

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

cd ../powershell-unix
dotnet publish --configuration Linux --runtime linux-x64
mv libpsl-native.so bin/Linux/netcoreapp2.0
dotnet run -c Linux
