# Frequently Asked Questions

## Where can I learn PowerShell's syntax?

- [What is PowerShell?][12]
- [Discover PowerShell][09]
- [PowerShell 101][11]
- [PowerShell learning resources][10]

## What are the best practices and style?

The [PoshCode][03] unofficial guide is our reference.

## What are PowerShell's scoping rules?

- Variables are created in your current scope unless explicitly indicated.
- Variables are visible in a child scope unless explicitly indicated.
- Variables created in a child scope are not visible to a parent unless explicitly indicated.
- Variables may be placed explicitly in a scope.

### Things that create a scope

- [about_Functions_Advanced][04]
- [about Operators - Call operator][06] (`& { }`)
- [about Scopes][08]

### Things that operate in the current scope

- [about Operators - Dot source operator][07] (`. { }`)
- [about Language Keywords][05] (`if .. else`, `for`, `switch`, etc.)

## Why didn't an error throw an exception?

Error handling in PowerShell is a bit weird, as not all errors result in catchable exceptions by
default. Setting `$ErrorActionPreference = 'Stop'` will likely do what you want; that is, cause
non-terminating errors instead to terminate. Read the [GitHub issue][02] for more information.

## Where do I get the PowerShell Core SDK package?

The SDK NuGet package **Microsoft.PowerShell.SDK** is provided for developers to write C# code
targeting PowerShell. PowerShell NuGet packages are published to [Microsoft.PowerShell.SDK][13] on
NuGet.org.

To use the `Microsoft.PowerShell.SDK` NuGet package, declare `PackageReference` tags in your
`.csproj` file as follows:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.PowerShell.SDK" Version="7.3.5" />
  <PackageReference Include="Microsoft.PowerShell.Commands.Diagnostics" Version="7.3.5" />
  <PackageReference Include="Microsoft.WSMan.Management" Version="7.3.5"/>
</ItemGroup>
```

## Why did my build fail?

There are few common issues with the build. The easiest way to resolve most issues with the build is
to run `Start-PSBuild -Clean`.

### Dependency changed

If package dependencies were changed in any `project.json`, you need to manually run
`dotnet restore` to update your local dependency graphs. `Start-PSBuild -Restore` can automatically
do this.

### Resource changed

`Start-PSBuild` automatically calls `Start-ResGen` on the very first run. On subsequent runs, you
may need to explicitly use `Start-PSBuild -ResGen` command.

Try it, when you see compilation error about *strings.

[More details][01] about resource.

### TypeGen

Similar to `-ResGen` parameter, there is `-TypeGen` parameter that triggers regeneration of type
catalog.

## Why did `Start-PSBuild` tell me to update `dotnet`?

We depend on the latest version of the .NET CLI, as we use the output of `dotnet --info` to
determine the current runtime identifier. Without this information, our build function can't know
where `dotnet` is going to place the build artifacts.

You can automatically install this using `Start-PSBootstrap`.
**However, you must first manually uninstall other versions of the CLI.**

If you have installed by using any of the following means:

- `MSI`
- `exe`
- `apt-get`
- `pkg`

You *must* manually uninstall it.

Additionally, if you've just unzipped their binary drops (or used their obtain scripts, which do
essentially the same thing), you must manually delete the folder, as the .NET CLI team re-engineered
how their binaries are setup, such that new packages' binaries get stomped on by old packages'
binaries.

<!-- updated link references -->
[01]: dev-process/resx-files.md
[02]: https://github.com/MicrosoftDocs/PowerShell-Docs/issues/1583
[03]: https://github.com/PoshCode/PowerShellPracticeAndStyle
[04]: https://learn.microsoft.com/powershell/module/microsoft.powershell.core/about/about_functions_advanced
[05]: https://learn.microsoft.com/powershell/module/microsoft.powershell.core/about/about_language_keywords
[06]: https://learn.microsoft.com/powershell/module/microsoft.powershell.core/about/about_operators#call-operator-
[07]: https://learn.microsoft.com/powershell/module/microsoft.powershell.core/about/about_operators#dot-sourcing-operator-
[08]: https://learn.microsoft.com/powershell/module/microsoft.powershell.core/about/about_scopes
[09]: https://learn.microsoft.com/powershell/scripting/discover-powershell
[10]: https://learn.microsoft.com/powershell/scripting/learn/more-powershell-learning
[11]: https://learn.microsoft.com/powershell/scripting/learn/ps101/00-introduction
[12]: https://learn.microsoft.com/powershell/scripting/overview
[13]: https://www.nuget.org/packages/Microsoft.PowerShell.SDK
