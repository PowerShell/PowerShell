Microsoft PowerShell SDK
========================

This project is a metapackage referencing the PowerShell projects and .NET Core packages that PowerShell ships.

The package dependencies consist of two parts:

1. PowerShell projects: local source code with own sets of dependencies
2. .NET Core packages: the framework libraries that we ensure are present for PowerShell developers at runtime

This second set includes packages that we do not necessarily require at compile-time, but must provide for our users at runtime.
For example, we include the library `System.Runtime.Serialization.Json.dll` so that users of PowerShell can utilize its types and methods,
even though PowerShell does not directly depend on the library.

There are intentionally duplicated dependencies.
Instead of relying on dependency transitivity where A -> B, B -> C, so A -> C,
we explicitly include A -> C if A requires C despite the removal of B.
Additionally, we want to easily identify our complete dependency set without generating a lockfile
(an artifact of `dotnet restore` after it resolves the dependency graph).
For example, `System.Management.Automation` depends on `System.Diagnostics.TraceSource`,
but `Microsoft.PowerShell.SDK` depends on both `System.Diagnostics.TraceSource` and `System.Management.Automation`.

Transitive dependencies not listed in [project.json][] are **not a part of public contract**.
		
[project.json]: project.json
