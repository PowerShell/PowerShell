# Host PowerShell Core in .NET Core Applications

> This documentation is based on PowerShell Core packages built against .NET Core 1.1 and prior. 
> Things may change after we move to .NET Core 2.0.

## PowerShell Core targeting .NET Core 1.1 and Prior

### Overview

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

### Comparison - Hosting Windows PowerShell vs. Hosting PowerShell Core

The following code demonstrates how to host Windows PowerShell in an application.  
As shown below, you can insert your business logic code directly in the `Main` method.

```CSharp
// MyApp.exe
using System;
using System.Management.Automation;

public class Program
{
    static void Main(string[] args)
    {
        // My business logic code
        using (PowerShell ps = PowerShell.Create())
        {
            var results = ps.AddScript("Get-Command Write-Output").Invoke();
            Console.WriteLine(results[0].ToString());
        }
    }
}
```

However, when it comes to hosting PowerShell Core, there will be a layer of redirection for the PowerShell load context to take effect.
In a .NET Core application, the entry point assembly that contains the `Main` method is loaded in the default load context,
and thus all assemblies referenced by the entry point assembly, implicitly or explicitly, will also be loaded into the default load context.

In order to have the PowerShell load context to control assembly loading for the execution of an application,
the business logic code needs to be extracted out of the entry point assembly and put into a different assembly, say `Logic.dll`.
The entry point `Main` method shall do one thing only -- let the PowerShell load context load `Logic.dll` and start the execution of the business logic.
Once the execution starts this way, all further assembly loading requests will be handled by the PowerShell load context.

So the above example needs to be altered as follows in a .NET Core application:

```CSharp
// MyApp.exe
using System.Management.Automation;
using System.Reflection;

namespace Application.Test
{
    public class Program 
    {
        /// <summary>
        /// Managed entry point shim, which starts the actual program
        /// </summary>
        public static int Main(string[] args)
        {
            // Application needs to use PowerShell AssemblyLoadContext if it needs to create PowerShell runspace
            // PowerShell engine depends on PS ALC to provide the necessary assembly loading/searching support that is missing from .NET Core 
            string appBase = System.IO.Path.GetDirectoryName(typeof(Program).GetTypeInfo().Assembly.Location);
            System.Console.WriteLine("\nappBase: {0}", appBase);
            
            // Initialize the PS ALC and let it load 'Logic.dll' and start the execution
            return (int)PowerShellAssemblyLoadContextInitializer.
                           InitializeAndCallEntryMethod(
                               appBase,
                               new AssemblyName("Logic, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"),
                               "Application.Test.Logic",
                               "Start",
                               new object[] { args });
        }
    }
}

// Logic.dll
using System;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace Application.Test
{
    public sealed class Logic
    {
        /// <summary>
        /// Start the actual logic
        /// </summary>
        public static int Start(string[] args)
        {
            // My business logic code
            using (PowerShell ps = PowerShell.Create())
            {
                var results = ps.AddScript("Get-Command Write-Output").Invoke();
                Console.WriteLine(results[0].ToString());
            }
            return 0;
        }
    }
}
```

### .NET Core Sample Application

You can find the sample application project `"MyApp"` under [sample-dotnet1.1](./sample-dotnet1.1). 
To build the sample project, run the following commands ([.NET Core SDK 1.0.1](https://github.com/dotnet/cli/releases/tag/v1.0.1) is required):

```powershell
dotnet restore .\MyApp\MyApp.csproj
dotnet publish .\MyApp -c release -r win10-x64
```

Then you can run `MyApp.exe` from the publish folder and see the results:

```
PS:> .\MyApp.exe

Evaluating 'Get-Command Write-Output' in PS Core Runspace

Write-Output

Evaluating '([S.M.A.ActionPreference], [S.M.A.AliasAttribute]).FullName' in PS Core Runspace

System.Management.Automation.ActionPreference
System.Management.Automation.AliasAttribute
```

### Remaining Issue

PowerShell Core builds separately for Windows and Unix, so the assemblies are different between Windows and Unix platforms.
Unfortunately, all PowerShell NuGet packages that have been published so far only contain PowerShell assemblies built specifically for Windows.
The issue [#3417](https://github.com/PowerShell/PowerShell/issues/3417) was opened to track publishing PowerShell NuGet packages for Unix platforms.

[CorePsAssemblyLoadContext.cs]: https://github.com/PowerShell/PowerShell/blob/master/src/Microsoft.PowerShell.CoreCLR.AssemblyLoadContext/CoreCLR/CorePsAssemblyLoadContext.cs
[Resolving]: https://github.com/dotnet/corefx/blob/ec2a6190efa743ab600317f44d757433e44e859b/src/System.Runtime.Loader/ref/System.Runtime.Loader.cs#L35
[bootstrapping]: https://github.com/PowerShell/PowerShell/blob/master/src/powershell/Program.cs#L27