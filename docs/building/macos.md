# Build PowerShell on macOS

This guide supplements the [Linux instructions](./linux.md), as
building on macOS is almost identical.

.NET Core 2.x (and by transitivity, us) only supports macOS 10.13+.

## Environment

You will want [Homebrew](https://brew.sh/) or [MacPorts](https://www.macports.org/), the missing package manager for macOS.
Once installed, follow the same instructions to download and
install a self-hosted copy of PowerShell on your macOS machine.
From `pwsh.exe`, run `Import-Module ./build.psm1` and use `Start-PSBootstrap` to install the dependencies.

The `Start-PSBootstrap` function does the following:

- Uses `brew` or `port` to install OpenSSL, and GNU WGet
- Uninstalls any prior versions of .NET CLI
- Downloads and installs .NET Core SDK to `~/.dotnet`

If you want to use `dotnet` outside of `Start-PSBuild`,
add `~/.dotnet` to your `PATH` environment variable.

### error: Too many open files

Due to a [bug][809] in NuGet, the `dotnet restore` command will fail without the limit increased.
Run `ulimit -n 2048` to fix this in your session;
add it to your shell's profile to fix it permanently.

We cannot do this for you in the build module due to #[847][].

[809]: https://github.com/dotnet/cli/issues/809
[847]: https://github.com/PowerShell/PowerShell/issues/847

## Build using our module

Start a PowerShell session by running `pwsh`, and then use `Start-PSBuild -UseNuGetOrg` from the module.

After building, PowerShell will be at `./src/powershell-unix/bin/Debug/net6.0/osx-x64/publish/pwsh`.

> The PowerShell project by default references packages from the private Azure Artifacts feed, which requires authentication. The `-UseNuGetOrg` flag reconfigures the build to use the public NuGet.org feed instead.
