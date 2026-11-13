# Host PowerShell Core in .NET Core Applications

## PowerShell Core v6.0.1 and Later

The runtime assemblies for Windows, Linux and OSX are now published in NuGet package version 6.*.

Please see the [.NET Core Sample Application](#net-core-sample-application) section for an example that uses PowerShell Core NuGet packages.

## .NET Core Sample Application

Note: The .NET Core `2.1` runtime and .NET Core SDK `2.1` or higher is required for the examples below:

- [sample](./sample)

You can find the sample application project `MyApp` in each of the above 2 sample folders. You can quickly test-run it using `dotnet run`.
To build the sample project properly for distribution, run the following command (make sure the required .NET Core SDK is in use):

```powershell
dotnet publish .\MyApp --configuration release
```

This builds it for the runtimes specified by the `RuntimeIdentifiers` property in the `.csproj` file.
Then you can run the `MyApp` binary from the publish folder and see the results:

```none
PS:> .\MyApp.exe

Evaluating 'Get-Command Write-Output' in PS Core Runspace

Write-Output

Evaluating '([S.M.A.ActionPreference], [S.M.A.AliasAttribute]).FullName' in PS Core Runspace

System.Management.Automation.ActionPreference
System.Management.Automation.AliasAttribute
```

## Special Hosting Scenario For Native Host

There is a special hosting scenario for native hosts,
where Trusted Platform Assemblies (TPA) do not include PowerShell assemblies,
such as the in-box `powershell.exe` in Nano Server and the Azure DSC host.

For such hosting scenarios, the native host needs to bootstrap by calling [`PowerShellAssemblyLoadContextInitializer.SetPowerShellAssemblyLoadContext`](https://learn.microsoft.com/dotnet/api/system.management.automation.powershellassemblyloadcontextinitializer.setpowershellassemblyloadcontext).
When using this API, the native host can pass in the path to the directory that contains PowerShell assemblies.
A handler will then be registered to the [`Resolving`](https://github.com/dotnet/corefx/blob/d6678e9653defe3cdfff26b2ff62135b6b22c77f/src/System.Runtime.Loader/ref/System.Runtime.Loader.cs#L38)
event of the default load context to deal with the loading of assemblies from that directory.
