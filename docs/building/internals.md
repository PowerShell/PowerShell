Internals of build process
=========================================

The purpose of this document is to explain build process **internals** with subtle nuances. 
This document is not by any means complete.
The ultimate source of truth is the code in `.\build.psm1` that's getting executed on the corresponding CI system.

This document assumes that you can successfully build PowerShell from sources for your platform.


Top directory
-----------

We are calling `dotnet` tool build for `$Top` directory

- `src\powershell` for CoreCLR builds (all platforms)
- `src\Microsoft.PowerShell.ConsoleHost` for FullCLR builds (Windows only)


### Dummy dependencies

We use dummy dependencies between project.json files to leverage `dotnet` build functionality.
For example, `src\Microsoft.PowerShell.ConsoleHost\project.json` has dependency on `Microsoft.PowerShell.PSReadLine`,
but in reality, there is no build dependency.

Dummy dependencies allows us to build just `$Top` folder, instead of building several folders.

### Dummy dependencies rules

* If assembly is part of FullCLR build,
it should be listed as a dependency for FullCLR $Top folder (src\Microsoft.PowerShell.ConsoleHost)

* If assembly is part of CoreCLR build,
it should be listed as a dependency for CoreCLR $Top folder (src\powershell)
