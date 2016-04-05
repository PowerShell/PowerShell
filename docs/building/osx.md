Build PowerShell on OS X
========================

This guide supplements the [Linux instructions](./linux.md), as
building on OS X is almost identical.

Please keep in mind that we do not yet routinely test on OS X, but
some developers use PowerShell on 10.10 and 10.11.

Environment
===========

You will want [Homebrew](http://brew.sh/), the missing package manager
for OS X. Once installed, use `brew` to install the following
dependencies.

```sh
brew install openssl cmake wget
```

Instead of using Linux `./bootstrap.sh` to install .NET CLI's Ubuntu
packages, you will want to use the official PKG installer for OS X.

```sh
wget https://dotnetcli.blob.core.windows.net/dotnet/beta/Installers/Latest/dotnet-dev-osx-x64.latest.pkg
open ./dotnet-dev-osx-x64.latest.pkg
```

Then follow the wizard's instructions to complete installation.

Build using our module
======================

Instead of installing the Ubuntu package of PowerShell, download the
`pkg` from our GitHub releases page using your browser, complete the
wizard, start a `powershell` session, and use `Start-PSBuild` from the
module.

The output directory will be slightly different because your runtime
identifier is different. PowerShell will be at
`./src/Microsoft.PowerShell.Host/bin/Linux/netstandardapp1.5/osx.10.11-x64/powershell`,
or `osx.10.10` depending on your operating system version. Note that
configration is still `Linux` because it would be silly to make yet
another separate configuration when it's used soley to work-around a
CLI issue.
