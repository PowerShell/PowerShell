pushd ../TypeCatalogParser
dotnet restore -v Warning
dotnet run
popd

dotnet restore -v Warning
dotnet run ../Microsoft.PowerShell.CoreCLR.AssemblyLoadContext/CorePsTypeCatalog.cs powershell.inc
