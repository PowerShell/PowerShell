Internals of build process
=========================================

The purpose of this document is to explain build process **internals** with subtle nuances. 
This document is not by any means complete.
The ultimate source of truth is the code in `.\build.psm1` that's getting executed on the corresponding CI system.

This document assumes, that you can successfully build PowerShell from sources for your platform.


Top directory
-----------

We are calling `dotnet` tool build for `$Top` directory

- `src\powershell` for CoreCLR builds (all platforms)
- `src\Microsoft.PowerShell.ConsoleHost` for FullCLR builds (Windows only)

Modules
----------

There are 3 modules directories with the same purpose: they have **content** files (i.e. `*.psm1`, `*.psd1`)
which would be binplaced by `dotnet`

- `src\Modules` shared between all flavours
- `src\Microsoft.PowerShell.ConsoleHost\Modules` FullCLR (Windows)
- `src\powershell\Modules` CoreCLR (all platforms)
