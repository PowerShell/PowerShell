Building a C# Cmdlet
====================

This example project demonstrates how to build your own C# cmdlet for PowerShell.
When built in the following manner, the resulting DLL can be imported everywhere:
Windows PowerShell with Desktop .NET (FullCLR) and PowerShell on Windows, Linux, and macOS with .NET Core (CoreCLR).

Setup
-----

We use the [.NET Command-Line Interface][dotnet-cli] (`dotnet`) to build the cmdlet library.
Install the `dotnet` tool and ensure `dotnet --version` is at least `1.0.0-rc2`.

.NET CLI uses a `project.json` file for build specifications:

```json
{
    "name": "SendGreeting",
    "description": "Example C# Cmdlet project",
    "version": "1.0.0-*",

    "dependencies": {
        "Microsoft.PowerShell.5.ReferenceAssemblies": "1.0.0-*"
    },

    "frameworks": {
        "netstandard1.3": {
            "imports": [ "net40" ],
            "dependencies": {
                "Microsoft.NETCore": "5.0.1-*",
                "Microsoft.NETCore.Portable.Compatibility": "1.0.1-*"
            }
        }
    }
}
```

Note that no source files are specified.
.NET CLI automatically will build all `.cs` files in the project directory.

Going through this step-by-step:

- `"name": "SendGreeting"`: Name of the assembly to output (otherwise it defaults to the name of the containing folder).

- `"version": "1.0.0-*"`: The wild-card can be replaced using the `--version-suffix` flag to `dotnet build`.

- [Microsoft.PowerShell.5.ReferenceAssemblies][powershell]: Contains the SDK reference assemblies for PowerShell version 5.
  Targets the `net40` framework.

- [netstandard1.3][]: The target framework for .NET Core portable libraries.
  This is an abstract framework that will work anywhere its dependencies work.
  Specifically, the 1.3 version allows this assembly to work even on Windows PowerShell with Desktop .NET.

- `"imports": [ "net4" ]`: Since the PowerShell reference assemblies target the older `net40` framework,
  we `import` it here to tell `dotnet restore` that we know we're loading a possibly-incompatible package.

- [Microsoft.NETCore][netcore]: Provides a set of packages that can be used when building portable
  libraries on .NETCore-based platforms.

- [Microsoft.NETCore.Portable.Compatibility][portable]: Enables compatibility
  with portable libraries targeting previous .NET releases like .NET Framework 4.0.
  Required to build against the PowerShell reference assemblies package.

Other dependencies can be added as needed;
refer to the [.NET Core package gallery][myget] for package availability, name, and version information.

Because the .NET Core packages are not yet released to NuGet.org,
you also need this `NuGet.config` file to setup the [.NET Core MyGet feed][myget]:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="CI Builds (dotnet-core)" value="https://www.myget.org/F/dotnet-core/api/v3/index.json" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
```

[dotnet-cli]: https://github.com/dotnet/cli#new-to-net-cli
[powershell]: https://www.nuget.org/packages/Microsoft.PowerShell.5.ReferenceAssemblies
[netstandard1.3]: https://github.com/dotnet/corefx/blob/master/Documentation/architecture/net-standard-applications.md
[netcore]: https://dotnet.myget.org/feed/dotnet-core/package/nuget/Microsoft.NETCore
[portable]: https://dotnet.myget.org/feed/dotnet-core/package/nuget/Microsoft.NETCore.Portable.Compatibility
[myget]: https://dotnet.myget.org/gallery/dotnet-core

Building
--------

.NET Core is a package-based platform, so the correct dependencies first need to be resolved:

```
dotnet restore
```

This reads the `project.json` and `NuGet.config` files and uses NuGet to restore the necessary packages.
The generated `project.lock.json` lockfile contains the resolved dependency graph.

Once packages are restored, building is simple:

```
dotnet build
```

This will produce the assembly `./bin/Debug/netstandard1.3/SendGreeting.dll`.

This build/restore process should work anywhere .NET Core works, including Windows, Linux, and macOS.

Deployment
----------

In PowerShell, check `$env:PSMODULEPATH` and install the new cmdlet in its own
module folder, such as, on Linux,
`~/.powershell/Modules/SendGreeting/SendGreeting.dll`.

Then import and use the module:

```powershell
> Import-Module SendGreeting # Module names are case-sensitive on Linux
> Send-Greeting -Name World
Hello World!
```

You can also import by the path:

```powershell
> Import-Module ./bin/Debug/netstandard1.3/SendGreeting.dll
```
