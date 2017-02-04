
# Portable Modules

It is possible to build a module that targets Windows PowerShell and PowerShell Core today.

One simply needs to target `netstandard1.3` and include an appropriate reference assembly for `System.Management.Automation`, see this [example](https://github.com/Jaykul/NetCoreModuleProof).

Unfortunately, many (most?) non-trivial modules will not work in with Windows PowerShell unless some [facade assemblies](https://github.com/dotnet/standard/blob/6051a7df6d86353f900c46a64f104593647d2904/docs/history/evolution-of-design-time-assemblies.md) are installed.

.Net Core refactored the assemblies so that commonly used types from `mscorlib.dll` are now in `System.Runtime.dll` and less commonly used types are in other assemblies.
When building against different versions of `netstandard`, an assembly ends up referencing these refactored assemblies instead of `mscorlib`.

A simple module can work on Windows 10 today if it only references the assembly `System.Runtime.dll` because the facade assembly is installed in the GAC on Windows 10.
Other facade assemblies are not shipping in Windows and if new facade assemblies are included, they will only ship in newer versions of Windows.

## System.Management.Automation

We need to produce a set of reference assemblies for `System.Management.Automation.dll`.
In the past, we have published the asmmeta produced reference assemblies because it was simple and expedient, but that will not work going forward.
Note that we shipped multiple reference assemblies (e.g. for `Microsoft.PowerShell.Commands.Utility.dll`) for unclear reasons - many of those assemblies do not contain apis that we really think of as a supported api despite the classes being public.

Current guidance is to use the PowerShell 6 reference assemblies because those target `netstandard1.6`.
This is also not an option because modules may take an accidental dependency on apis not present in older version of PowerShell.

The correct solution is to create a new reference assembly that contains only the apis that are truly portable.
To that end, I started with the V3 version of `System.Management.Automation.dll` and generated C# for the public api surface using an internal tool called `GenApi`.
I then created a project with `dotnet new`, set the target framework to `netstandard1.3`, and removed apis that didn't compile because there is no equivalent api in .Net Core.

To target newer versions of PowerShell, we could take this C#, add the new apis under `#if` so that we have one simple definition of our public api.
We could also take the opportunity to revisit many of the apis - think of this as our way of limiting the public api surface without removing the code.

## Practical Experience

I have successfully built `PSReadline` targeting `netstandard1.3` and the `System.Management.Automation.dll` reference assembly mentioned above.
I needed to binplace 7 facade assemblies to get `PSReadline` to load and work correctly in Windows PowerShell.
These facade assemblies are needed for types like `System.Collections.Hashtable`, `System.Console`, `System.Threading.Thread`, and `System.IO.File`.

The single binary worked correctly in Windows PowerShell with the facade assemblies and in PowerShell Core without the facade assemblies.

## Solutions

I can see several possible solutions, in roughly decreasing order of preference:

* `PowerShellGet` could detect read the metadata from binary modules it is installing and automatically install the facade assemblies that are required.
  Installation should ideally go in a the GAC or alongside `powershell.exe`, but that requires elevation, so installing along with the module is an option.

* We can chose a set of facade assemblies to install on older versions of Windows.
  These assemblies can be installed in either the GAC or alongside `powershell.exe`.

* We can require module authors to package the facade assemblies with their module.
  This is undesirable because the entire module is not portable because the facade assemblies will not work on PowerShell Core.

MSBuild has the same issue as PowerShell - tasks are similar to modules.
See this [issue](https://github.com/Microsoft/msbuild/issues/1542) for discussion on what they are doing.
