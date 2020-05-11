# Using Windows PowerShell modules with PowerShell Core

## Windows PowerShell vs PowerShell Core

Existing Windows PowerShell users are familiar with the large number of modules available, however, they are not necessarily compatible with PowerShell Core.
More information regarding compatibility is in a [blog post](https://devblogs.microsoft.com/powershell/powershell-6-0-roadmap-coreclr-backwards-compatibility-and-more/).

Windows PowerShell 5.1 is based on .Net Framework 4.6.1, while PowerShell Core is based on .Net Core 2.x.
Although both adhere to .Net Standard 2.0 and can be compatible, some modules may be using APIs or cmdlets not supported on CoreCLR or using APIs from Windows PowerShell that have been deprecated and removed from PowerShell Core (for example, PSSnapins).

## Importing a Windows PowerShell module

Since compatibility cannot be ensured, PowerShell Core, by default, does not look in the Windows PowerShell module path to find those modules.
However, advanced users can explicitly enable PowerShell Core to include the Windows PowerShell module path and attempt to import those modules.

First, install the [WindowsPSModulePath](https://www.powershellgallery.com/packages/WindowsPSModulePath) module from the PowerShellGallery:

```powershell
Install-Module WindowsPSModulePath -Scope CurrentUser
```

Then run `Add-WindowsPSModulePath` cmdlet to add the Windows PowerShell module path to your PowerShell Core module path:

```powershell
Add-WindowsPSModulePath
```

Note that this is only effective in the current PowerShell session.
If you want to persist this, you can add `Add-WindowsPSModulePath` to your profile:

```powershell
"Add-WindowsPSModulePath" >> $profile
```

Once the module path has been updated, you can list available modules:

```powershell
Get-Module -ListAvailable
```

Note that PowerShell Core is not aware which Windows PowerShell modules will work and which will not so all are listed.
We plan to improve this experience in the future.
You can now import a Windows PowerShell module or just execute a known cmdlet and allow auto-module loading to take care of importing the module:

```powershell
Get-VM
# this will automatically load the Hyper-V module
```

Most of the cmdlets based on CDXML will work just fine, as well as some C# based cmdlets that happen to be .NET Standard 2.0 compatible (for example, Hyper-V module) but the Active Directory module, for example, won't work.

## How you can help

Provide comments on Windows PowerShell modules that work or don't work in our [tracking issue](https://github.com/PowerShell/PowerShell/issues/4062).
