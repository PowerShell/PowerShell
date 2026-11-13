# Build PowerShell on macOS

This guide supplements the [Linux instructions](./linux.md), as
building on macOS is almost identical.

For the macOS versions supported by .NET (and by transitivity, us),
see [Install .NET on macOS](https://learn.microsoft.com/dotnet/core/install/macos).

## Environment

You will want [Homebrew](https://brew.sh/) or [MacPorts](https://www.macports.org/), the missing package manager for macOS.
Once installed, follow the same instructions to download and
install a self-hosted copy of PowerShell on your macOS machine.
From `pwsh`, run `Import-Module ./build.psm1` and use `Start-PSBootstrap -Scenario DotNet` to install the dependencies.

The `Start-PSBootstrap` function does the following:

- Uses `brew` or `port` to install OpenSSL and GNU Wget
- Downloads and installs the .NET SDK version specified in `global.json` to `~/.dotnet`

If you want to use `dotnet` outside of `Start-PSBuild`,
add `~/.dotnet` to your `PATH` environment variable.

## Build using our module

Start a PowerShell session by running `pwsh`, and then use `Start-PSBuild -UseNuGetOrg` from the module.

After building, PowerShell will be at `./src/powershell-unix/bin/Debug/net11.0/osx-arm64/publish/pwsh`
(`osx-x64` on Intel-based Macs).

> The PowerShell project by default references packages from the private Azure Artifacts feed, which requires authentication. The `-UseNuGetOrg` flag reconfigures the build to use the public NuGet.org feed instead.
