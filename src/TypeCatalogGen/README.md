# TypeCatalogGen

`./build.sh` runs the Core PowerShell `TypeCatalogGen.exe` under Mono
on Linux (using `dotnet-cli`). The `Runtime` folder is a facade
project that, when published, produces a list of the currently used
runtime assemblies for .NET Core. The output is
`CorePsTypeCatalog.cs`, which contains an "initialized" dictionary
that we copy into `typecatalog.cs` of `System.Management.Automation`
