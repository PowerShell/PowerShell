# Host PowerShell Core in .NET Core Applications

## PowerShell Core v6.0.1 and Later

The runtime assemblies for Windows, Linux and OSX are now published in NuGet package version 6.*.

Please see the [.NET Core Sample Application](#net-core-sample-application) section for an example that uses PowerShell Core NuGet packages.

[CorePsAssemblyLoadContext.cs]: https://docs.microsoft.com/dotnet/api/system.management.automation.powershellassemblyloadcontextinitializer.setpowershellassemblyloadcontext
[Resolving]: https://github.com/dotnet/corefx/blob/d6678e9653defe3cdfff26b2ff62135b6b22c77f/src/System.Runtime.Loader/ref/System.Runtime.Loader.cs#L38

## .NET Core Sample Application

Note: The .NET Core `2.1` runtime and .NET Core SDK `2.1` or higher is required for the examples below:

- [sample-windows](./sample-windows)
- [sample-crossplatform](./sample-crossplatform)

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

## PowerShell Core v6.0.0-beta.2 and Prior

Due to the lack of necessary APIs for manipulating assemblies in .NET Core 1.1 and prior,	
PowerShell Core needs to control assembly loading via our customized `AssemblyLoadContext` ([CorePsAssemblyLoadContext.cs][]) in order to do tasks like type resolution.	
So applications that want to host PowerShell Core (using PowerShell APIs) need to be bootstrapped from `PowerShellAssemblyLoadContextInitializer`.	

 `PowerShellAssemblyLoadContextInitializer` exposes 2 APIs for this purpose:	
`SetPowerShellAssemblyLoadContext` and `InitializeAndCallEntryMethod`.	
They are for different scenarios:	

 - `SetPowerShellAssemblyLoadContext` - It's designed to be used by a native host	
  whose Trusted Platform Assemblies (TPA) do not include PowerShell assemblies,	
  such as the in-box `powershell.exe` and other native CoreCLR host in Nano Server.	
  When using this API, instead of setting up a new load context,	
  `PowerShellAssemblyLoadContextInitializer` will register a handler to the [Resolving][] event of the default load context.	
  Then PowerShell Core will depend on the default load context to handle TPA and the `Resolving` event to handle other assemblies.	

 - `InitializeAndCallEntryMethod` - It's designed to be used with `dotnet.exe`	
  where the TPA list includes PowerShell assemblies.	
  When using this API, `PowerShellAssemblyLoadContextInitializer` will set up a new load context to handle all assemblies.	
  PowerShell Core itself also uses this API for [bootstrapping][].	

 This documentation only covers the `InitializeAndCallEntryMethod` API,	
as it's what you need when building a .NET Core application with .NET CLI.
