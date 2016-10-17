Internals of build process
==========================

The purpose of this document is to explain build process **internals** with subtle nuances.
This document is not by any means complete.
The ultimate source of truth is the code in `.\build.psm1` that's getting executed on the corresponding CI system.

This document assumes that you can successfully build PowerShell from sources for your platform.


Top directory
-------------

We are calling `dotnet` tool build for `$Top` directory

- `src\powershell-win-core` for CoreCLR on Windows.
- `src\powershell-unix` for CoreCLR on Linux and macOS.
- `src\powershell-win-full` for FullCLR builds (Windows only)

### Dummy dependencies

We use dummy dependencies between project.json files to leverage `dotnet` build functionality.
For example, `src\powershell-win-core\project.json` has dependency on `Microsoft.PowerShell.PSReadLine`,
but in reality, there is no build dependency.

Dummy dependencies allows us to build just `$Top` folder, instead of building several folders.

### Dummy dependencies rules

* If assembly is part of FullCLR build,
it should be listed as a dependency for FullCLR $Top folder (src\powershell-win-full)

* If assembly is part of CoreCLR build,
it should be listed as a dependency for $Top folder (src\powershell-unix or src\powershell-win-core)

Preliminary steps
-----------------

### ResGen

Until the .NET CLI `dotnet-resgen` tool supports the generation of strongly typed classes,
we run our own tool C# [ResGen tool](../../src/ResGen).
While the `Start-PSBuild` command runs this automatically via the `Start-ResGen` function,
it does *not* require PowerShell.
The same command can be run manually:

```sh
dotnet restore
cd src/ResGen
dotnet run
```

Running the program does everything else:

* for each project, given a `resources` folder
  * creates a `gen` folder
  * for each `*.resx` file
    * fills in a strongly typed C# class
    * writes it out to the corresponding `*.cs` file

These files are *not* automatically updated on each build,
as the project lacks the ability to detect changes.
Thus, running it for every build would break incremental recompilation.

If you pull new commits and get an error about missing strings,
you likely need to delete the `gen` folders and re-run the tool.

### Type Catalog

As a work-around for the lack of `GetAssemblies()`,
our custom assembly load context takes a pre-generated catalog of C# types
(for PowerShell type resolution).
Generating this catalog is a pre-build step that is run via `Start-TypeGen`,
which `Start-PSBuild` calls.
Again, however, PowerShell is not required.
The necessary steps can be run manually:

```sh
dotnet restore
cd src/TypeCatalogParser
dotnet run
cd ../TypeCatalogGen
dotnet run ../Microsoft.PowerShell.CoreCLR.AssemblyLoadContext/CorePsTypeCatalog.cs powershell.inc
```

The [`TypeCatalogParser`](../../src/TypeCatalogParser)
parses the `src/Microsoft.PowerShell.SDK/project.lock.json` file
(created by `dotnet restore`),
which contains the necessary data to resolve the paths to the DLLs of each dependency of PowerShell.
It produces a list of the location of all the DLLs that have types to be cataloged
(the output file is `powershell.inc`).
This list is taken as input to the [`TypeCatalogGen`](../../src/TypeCatalogGen) tool,
which generates a source file `CorePsTypeCatalog.cs` for the `Microsoft.PowerShell.CoreCLR.AssemblyLoadContext` project.

The error `The name 'InitializeTypeCatalog' does not exist in the current context`
indicates that the `CorePsTypeCatalog.cs` source file does not exist,
so follow the steps to generate it.
