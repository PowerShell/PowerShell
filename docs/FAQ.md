# Frequently Asked Questions

## Where can I learn PowerShell's syntax?

[SS64.com](https://ss64.com/ps/syntax.html) is a good resource.
[Microsoft Docs](https://docs.microsoft.com/powershell/scripting/overview) is another excellent resource.

## What are the best practices and style?

The [PoshCode][] unofficial guide is our reference.

[PoshCode]: https://github.com/PoshCode/PowerShellPracticeAndStyle

## What are PowerShell's scoping rules?

- Variables are created in your current scope unless explicitly indicated.
- Variables are visible in a child scope unless explicitly indicated.
- Variables created in a child scope are not visible to a parent unless
  explicitly indicated.
- Variables may be placed explicitly in a scope.

### Things that create a scope

- [functions](https://ss64.com/ps/syntax-functions.html)
- [call operator](https://ss64.com/ps/call.html) (`& { }`)
- [script invocations](https://ss64.com/ps/syntax-run.html)

### Things that operate in the current scope

- [source operator](https://ss64.com/ps/source.html) (`. { }`)
- [statements](https://ss64.com/ps/statements.html) (`if .. else`, `for`, `switch`, etc.)

## Why didn't an error throw an exception?

Error handling in PowerShell is a bit weird, as not all errors result in catchable exceptions by default.
Setting `$ErrorActionPreference = 'Stop'` will likely do what you want;
that is, cause non-terminating errors instead to terminate.
Read [An Introduction To Error Handling in PowerShell][error] for more information.

[error]: https://gist.github.com/TravisEz13/9bb811c63b88501f3beec803040a9996

## Where do I get the PowerShell Core SDK package?

The SDK NuGet package `Microsoft.PowerShell.SDK` is provided for developers to write .NET Core C# code targeting PowerShell Core.
PowerShell NuGet packages for releases starting from v6.0.0-alpha.9 will be published to the [powershell-core][] myget feed.

To use the `Microsoft.PowerShell.SDK` NuGet package, declare `PackageReference` tags in your `.csproj` file as follows:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.PowerShell.SDK" Version="6.0.0-beta.9" />
  <PackageReference Include="Microsoft.PowerShell.Commands.Diagnostics" Version="6.0.0-beta.9" />
  <PackageReference Include="Microsoft.WSMan.Management" Version="6.0.0-beta.9"/>
</ItemGroup>
```

[powershell-core]: https://powershell.myget.org/gallery/powershell-core

## Why did my build fail?

There are few common issues with the build.
The easiest way to resolve most issues with the build is to run `Start-PSBuild -Clean`.

### Dependency changed

If package dependencies were changed in any `project.json`, you need to manually
run `dotnet restore` to update your local dependency graphs.
`Start-PSBuild -Restore` can automatically do this.

### Resource changed

`Start-PSBuild` automatically calls `Start-ResGen` on the very first run.
On subsequent runs, you may need to explicitly use `Start-PSBuild -ResGen` command.

Try it, when you see compilation error about *strings.

[More details](dev-process/resx-files.md) about resource.

### TypeGen

Similar to `-ResGen` parameter, there is `-TypeGen` parameter that triggers regeneration of type catalog.

## Why did `Start-PSBuild` tell me to update `dotnet`?

We depend on the latest version of the .NET CLI, as we use the output of `dotnet
--info` to determine the current runtime identifier.
Without this information, our build function can't know where `dotnet` is going to place the build artifacts.

You can automatically install this using `Start-PSBootstrap`.
**However, you must first manually uninstall other versions of the CLI.**

If you have installed by using any of the following means:

- `MSI`
- `exe`
- `apt-get`
- `pkg`

You *must* manually uninstall it.

Additionally, if you've just unzipped their binary drops (or used their obtain
scripts, which do essentially the same thing), you must manually delete the
folder, as the .NET CLI team re-engineered how their binaries are setup, such
that new packages' binaries get stomped on by old packages' binaries.
