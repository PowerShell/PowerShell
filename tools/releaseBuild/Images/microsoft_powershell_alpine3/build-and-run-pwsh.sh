#!/bin/sh

set -e

# this script needs to be run from within Alpine with dotnet 2.1 SDK installed, example:
# docker run -it -v ~/repos/PowerShell:/PowerShell microsoft/dotnet:2.1-sdk-alpine

# build tools required:
# apk update
# apk add build-base gcc abuild binutils git python bash cmake

# run from root of PowerShell repo:
# tools/releaseBuild/Images/microsoft_powershell_alpine3/build-and-run-pwsh.sh /PowerShell /PowerShell 6.1.0

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
  tarName=$destination/powershell-$releaseTag-linux-musl-$arch.tar.gz
  dotnetArguments=/p:ReleaseTag=$releaseTag;
else
  tarName=$destination/powershell-linux-musl-$arch.tar.gz
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
            <_RefAssemblyPath Include="%(_ReferencesFromRAR.HintPath)%3B"  Condition=" '%(_ReferencesFromRAR.NuGetPackageId)' != 'Microsoft.Management.Infrastructure' "/>
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
dotnet publish --configuration Release --runtime linux-musl-x64 $dotnetArguments

# add libpsl-native to build
mv libpsl-native.so bin/Release/netcoreapp2.1/linux-musl-x64/publish

# tar build for output
cd bin/Release/netcoreapp2.1/linux-musl-x64/publish

tar -czvf $tarName .

echo "Created $tarName"
