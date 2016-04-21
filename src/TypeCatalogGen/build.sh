. mono-snapshot mono
export DOTNET_REFERENCE_ASSEMBLIES_PATH=$MONO_PREFIX/lib/mono/xbuild-frameworks/

pushd ../TypeCatalogParser
dotnet restore -v Warning
dotnet run
popd

dotnet restore -v Warning
dotnet run ../Microsoft.PowerShell.CoreCLR.AssemblyLoadContext/CorePsTypeCatalog.cs powershell.inc
