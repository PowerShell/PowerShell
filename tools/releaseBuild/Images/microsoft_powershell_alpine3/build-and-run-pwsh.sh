#!/bin/sh

repoRoot=$1
destination=$2
releaseTag=$3

# in Alpine, the currently supported architectures are:

# x86_64
# x86
# aarch64
# armhf
# ppc64le
# s390x

# from https://pkgs.alpinelinux.org/packages (Arch dropdown menu)

arch=`uname -m`

case $arch in
    x86_64)
        arch=x64
        ;;
    aarch64)
        arch=arm64
        ;;
    armhf)
        arch=arm
        ;;
    *)
        echo "Error: Unsupported OS architecture $arch detected"
        exit 1
        ;;
esac

# set variables depending on releaseTag
# remove v from release tag (v3.5 => 3.5)
if [ "${releaseTag:0:1}" = "v" ]; then
  releaseTag=${releaseTag:1}
  tarName=$destination/powershell-$releaseTag-alpine.3-$arch.tar.gz
  dotnetArguments=/p:ReleaseTag=$releaseTag;
else
  tarName=$destination/powershell-alpine.3-$arch.tar.gz
fi

# Build libpsl-native
cd $repoRoot/src/libpsl-native

cmake -DCMAKE_BUILD_TYPE=Debug .
make -j

# Restore packages
cd ../..
dotnet restore $dotnetArguments

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
dotnet publish --configuration Linux --runtime linux-x64 $dotnetArguments

# add libpsl-native to build
mv libpsl-native.so bin/Linux/netcoreapp2.0/linux-x64

# tar build for output
cd bin/Linux/netcoreapp2.0/linux-x64

tar -czvf $tarName .
