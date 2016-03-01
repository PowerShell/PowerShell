. mono-snapshot mono
export DOTNET_REFERENCE_ASSEMBLIES_PATH=$MONO_PREFIX/lib/mono/xbuild-frameworks/

pushd Runtime
dotnet restore
dotnet publish --output bin
popd

ls Runtime/bin/*.dll | sed ':a;N;$!ba;s/\n/;/g' > powershell.inc

dotnet restore
dotnet run CorePsTypeCatalog.cs powershell.inc
cp CorePsTypeCatalog.cs ../Microsoft.PowerShell.CoreCLR.AssemblyLoadContext/
