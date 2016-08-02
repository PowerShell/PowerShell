Build PowerShell on OS X
========================

This guide supplements the [Linux instructions](./linux.md), as
building on OS X is almost identical.

.NET Core (and by transitivity, us) only supports OS X 10.11, per
CoreFX issue #[7731][].

[7731]: https://github.com/dotnet/corefx/issues/7731

Environment
===========

You will want [Homebrew](http://brew.sh/), the missing package manager for OS X.
Once installed, follow the same instructions to download and
install a self-hosted copy of PowerShell on your OS X machine,
and use`Start-PSBootstrap` to install the dependencies.

The `Start-PSBootstrap` function does the following:

- Uses `brew` to install CMake, OpenSSL, and GNU WGet
- Links OpenSSL
- Uninstalls any prior versions of .NET CLI
- Downloads and installs the latest .NET CLI 1.0.0-preview2 SDK to `~/.dotnet`

If you want to use `dotnet` outside of `Start-PSBuild`,
add `~/.dotnet` to your `PATH` environment variable.

error: Too many open files
--------------------------

Due to a [bug][809] in NuGet, the `dotnet restore` command will fail without the limit increased.
Run `ulimit -n 2048` to fix this in your session;
add it your shell's profile to fix it permanently.

We cannot do this for you in in the build module due to #[847][].

[809]: https://github.com/dotnet/cli/issues/809
[847]: https://github.com/PowerShell/PowerShell/issues/847

error: dotnet restore
-------------------------

If you run `dotnet restore` and get error like

```
PS /Users/vors/dev/PowerShell> dotnet restore                                                                                         
log  : Restoring packages for /Users/vors/dev/PowerShell/src/TypeCatalogGen/project.json...
log  : Restoring packages for /Users/vors/dev/PowerShell/src/TypeCatalogParser/project.json...
log  : Restoring packages for /Users/vors/dev/PowerShell/test/PSReadLine/project.json...
log  : Restoring packages for /Users/vors/dev/PowerShell/test/csharp/project.json...
error: Unable to load the service index for source http://www.myget.org/F/dotnet-core/api/v3/index.json.
error:   The type initializer for 'Crypto' threw an exception.
error:   The type initializer for 'CryptoInitializer' threw an exception.
error:   Unable to load DLL 'System.Security.Cryptography.Native': The specified module could not be found.
error:    (Exception from HRESULT: 0x8007007E)

```

Try the following

* Make sure you have latest opensll and re-link it

```
brew update
brew install openssl
brew link --force openssl
```

Build using our module
======================

Instead of installing the Ubuntu package of PowerShell,
download the `pkg` from our GitHub releases page using your browser, complete the wizard,
start a `powershell` session, and use `Start-PSBuild` from the module.

The output directory will be slightly different because your runtime identifier is different.
PowerShell will be at `./src/powershell-unix/bin/Linux/netcoreapp1.0/osx.10.11-x64/powershell`,
or `osx.10.10` depending on your operating system version.
Note that configration is still `Linux` because it would be silly to make yet another separate configuration when it's used soley to work-around a CLI issue.
