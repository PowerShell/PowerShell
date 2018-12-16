# Host PowerShell Core in .NET Core Applications

## PowerShell Core v6.0.1 and Later

The runtime assemblies for Windows, Linux and OSX are now published in NuGet package version 6.*.

Please see the [.NET Core Sample Application](#net-core-sample-application) section for an example that uses PowerShell Core NuGet packages.

[CorePsAssemblyLoadContext.cs]: https://docs.microsoft.com/dotnet/api/system.management.automation.powershellassemblyloadcontextinitializer.setpowershellassemblyloadcontext
[Resolving]: https://github.com/dotnet/corefx/blob/d6678e9653defe3cdfff26b2ff62135b6b22c77f/src/System.Runtime.Loader/ref/System.Runtime.Loader.cs#L38
## .NET Core Sample Application

- [sample-windows](./sample-windows) - .NET Core `2.1` + PowerShell Core NuGet packages.
  .NET Core SDK `2.1` or higher is required.
- [sample-crossplatform](./sample-crossplatform) - .NET Core `2.1` + PowerShell Core NuGet packages.

You can find the sample application project `MyApp` in each of the above 2 sample folders.
To build the sample project, run the following commands (make sure the required .NET Core SDK is in use):

```powershell
dotnet publish .\MyApp -c release -r win10-x64
```

For cross platform project there is no need to specify `-r win10-x64`.
The runtime for the build machine's platform will automatically be selected.

Then you can run `MyApp.exe` from the publish folder and see the results:

```none
PS:> .\MyApp.exe

Evaluating 'Get-Command Write-Output' in PS Core Runspace

Write-Output

Evaluating '([S.M.A.ActionPreference], [S.M.A.AliasAttribute]).FullName' in PS Core Runspace

System.Management.Automation.ActionPreference
System.Management.Automation.AliasAttribute
```
