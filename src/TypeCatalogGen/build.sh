. mono-snapshot mono
export DOTNET_REFERENCE_ASSEMBLIES_PATH=$MONO_PREFIX/lib/mono/xbuild-frameworks/

pushd ../TypeCatalogParser
dotnet restore
dotnet run
popd

dotnet restore
dotnet run ../Microsoft.PowerShell.CoreCLR.AssemblyLoadContext/CorePsTypeCatalog.cs powershell.inc
