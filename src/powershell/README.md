PowerShell
==========

The `powershell[.exe]` executable for PowerShell is built by
**powershell-unix** and **powershell-windows** projects,
as they are the top dependencies of the graph, and has `emitEntryPoint: true`,
meaning a native executable is produced automatically by CLI (no need to own a
separate native host).

This project is a very simple shim that provides a `Main` function for .NET CLI
to produce an app. It initializes PowerShell's custom `AssemblyLoadContext` and
then delegates to the same `Start` function in
`Microsoft.PowerShell.ConsoleHost` that the original native PowerShell host
executes; thus we share the same entry point and the same PowerShell host, but
use a different native host. This lets us take full advantage of .NET CLI's
native host.

We use this shim so that the `ConsoleHost` project and the original native host
do not have to be changed. Additionally, until .NET CLI bugs surrounding content
file deployment are solved, this shim allows us to continue with our split
`Modules` folders work-around to deploy the correct versions.
