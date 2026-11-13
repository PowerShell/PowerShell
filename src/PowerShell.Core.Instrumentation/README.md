# PowerShell.Core.Instrumentation

The `PowerShell.Core.Instrumentation.man` has actually been migrated to [PowerShell-Native](https://github.com/PowerShell/PowerShell-Native/tree/master/src/PowerShell.Core.Instrumentation) repository.
The corresponding manifest resource DLL `PowerShell.Core.Instrumentation.dll` is now produced in the release build of `PowerShell-Native` and is shipped in the `Microsoft.PowerShell.Native` NuGet package.

However, we still need to keep `PowerShell.Core.Instrumentation.man` in the PowerShell repository because the PowerShell packages for Windows ship the manifest file and `RegisterManifest.ps1` for users to register the manifest resource DLL if they want to.
Therefore, when making changes to `PowerShell.Core.Instrumentation.man`, make sure your changes are made to both repositories -- **this file needs to be kept in sync between those 2 repositories**.
