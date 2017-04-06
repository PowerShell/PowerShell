#!/usr/bin/env bash
set -e

if hash powershell 2>/dev/null; then
    echo 'Continuing with `powershell -noprofile -c Start-PSBuild`'
    powershell -noprofile -c "Import-Module ./build.psm1; Start-PSBuild"
else
   echo 'Continuing with full manual build'

   ## Restore
   dotnet restore src/powershell-unix
   dotnet restore src/ResGen
   dotnet restore src/TypeCatalogGen

   ## Setup the build target to gather dependency information
   targetFile="$(pwd)/src/Microsoft.PowerShell.SDK/obj/Microsoft.PowerShell.SDK.csproj.TypeCatalog.targets"
   cat > $targetFile <<-"EOF"
<Project>
    <Target Name="_GetDependencies"
            DependsOnTargets="ResolvePackageDependenciesDesignTime">
        <ItemGroup>
            <_DependentAssemblyPath Include="%(_DependenciesDesignTime.Path)%3B" Condition=" '%(_DependenciesDesignTime.Type)' == 'Assembly' And '%(_DependenciesDesignTime.Name)' != 'Microsoft.Management.Infrastructure.Native.dll' And '%(_DependenciesDesignTime.Name)' != 'Microsoft.Management.Infrastructure.dll' " />
        </ItemGroup>
        <WriteLinesToFile File="$(_DependencyFile)" Lines="@(_DependentAssemblyPath)" Overwrite="true" />
    </Target>
</Project>
EOF
   dotnet msbuild src/Microsoft.PowerShell.SDK/Microsoft.PowerShell.SDK.csproj /t:_GetDependencies "/property:DesignTimeBuild=true;_DependencyFile=$(pwd)/src/TypeCatalogGen/powershell.inc" /nologo

   ## Generate 'powershell.version'
   git --git-dir="$(pwd)/.git" describe --dirty --abbrev=60 > "$(pwd)/powershell.version"

   ## Generate resource binding C# files
   pushd src/ResGen
   dotnet run
   popd

   ## Generate 'CorePsTypeCatalog.cs'
   pushd src/TypeCatalogGen
   dotnet run ../Microsoft.PowerShell.CoreCLR.AssemblyLoadContext/CorePsTypeCatalog.cs powershell.inc
   popd

   ## Build native component
   pushd src/libpsl-native
   cmake -DCMAKE_BUILD_TYPE=Debug .
   make -j
   make test
   popd

   ## Build powershell core
   rawRid="$(dotnet --info | grep RID)"
   rid=${rawRid##* } # retain the part after the last space
   dotnet publish --configuration Linux src/powershell-unix/ --output bin --runtime $rid

   echo 'You can run powershell from bin/, but some modules that are normally added by the Restore-PSModule step will not be available.'
fi
