# PowerShell global tool

PowerShell is a cross-platform command-line shell and scripting language for
automation and configuration management.

## Installation

With a compatible .NET SDK installed, install PowerShell as a global tool:

```shell
dotnet tool install --global PowerShell
```

For a prerelease version, add `--prerelease` to the installation command.

Start PowerShell with:

```shell
pwsh
```

## Runtime-specific packages

Install the `PowerShell` package rather than a `PowerShell.<rid>` package directly.
The .NET SDK selects the runtime-specific package for your operating system and
architecture. These packages contain the platform-specific PowerShell executable
and its dependencies.

## Learn more

- [PowerShell documentation](https://learn.microsoft.com/powershell/)
- [Source code and issue tracking](https://github.com/PowerShell/PowerShell)
- [MIT license](https://licenses.nuget.org/MIT)
