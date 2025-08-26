# Working Group Definitions

This document maintains a list of the current PowerShell [Working Groups (WG)](working-group.md),
as well as their definitions, membership, and a non-exhaustive set of examples of topics that fall
within the purview of that WG.

For an up-to-date list of the issue/PR labels associated with these WGs,
see [Issue Management](../maintainers/issue-management.md)

## Desired State Configuration (DSC)

The DSC Working Group primarily focuses around the development & adoption of the DSC v3
specification, whilst maintaining where needed limited support of previous versions,
including any required interoptability between versions.

DSC v3 is maintained in the PowerShell/DSC repo & is no longer dependant on PowerShell,
whilst enabling the use of PowerShell & Windows PowerShell where requested by
configuration authors. This WG is also responsible for the `PSDesiredStateConfiguration`
module & interactions with teams like Azure Machine Config & other 3rd party organisations.

### Members

* @theJasonHelmick
* @gaelcolas
* @michaeltlombardi
* @SteveL-MSFT
* @GijsReyn
* @ThomasNieto
* @tgauth
* @adityapatwardhan

## Developer Experience

The PowerShell developer experience includes the **development of modules** (in C#, PowerShell script, etc.),
as well as the experience of **hosting PowerShell and its APIs** in other applications and language runtimes.
Special consideration should be given to topics like **backwards compatibility** with Windows PowerShell
(e.g. with **PowerShell Standard**) and **integration with related developer tools**
(e.g. .NET CLI or the PowerShell extension for Visual Studio Code).

### Members

* @JamesWTruher (PS Standard, module authoring)
* @adityapatwardhan (SDK)
* @michaeltlombardi
* @SeeminglyScience
* @bergmeister

## Engine

The PowerShell engine is one of the largest and most complex aspects of the codebase.
The Engine WG should be focused on the
**implementation and maintenance of core PowerShell engine code**.
This includes (but is not limited to):

* The language parser
* The command and parameter binders
* The module and provider systems
  * `*-Item` cmdlets
  * Providers
* Performance
* Componentization
* AssemblyLoadContext

It's worth noting that the Engine WG is not responsible for the definition of the PowerShell language.
This should be handled by the Language WG instead.
However, it's expected that many issues will require input from both WGs.

### Members

* @daxian-dbw
* @JamesWTruher
* @rkeithhill
* @vexx32
* @SeeminglyScience
* @IISResetMe
* @powercode
* @kilasuit

## Interactive UX

While much of PowerShell can be used through both interactive and non-interactive means,
some of the PowerShell user experience is exclusively interactive.
These topics include (but are not limited to):

* Console
* Help System
* Tab completion / IntelliSense
* Markdown rendering
* PSReadLine
* Debugging

### Members

* @theJasonHelmick
* @daxian-dbw (PSReadline / IntelliSense)
* @adityapatwardhan (Markdown / help system)
* @JamesWTruher (cmdlet design)
* @SeeminglyScience
* @sdwheeler
* @kilasuit
* @FriedrichWeinmann
* @StevenBucher98

## Language

The Language WG is distinct from the Engine WG in that they deal with the abstract definition
of the PowerShell language itself.
While all WGs will be working closely with the PowerShell Committee (and may share members),
it's likely that the Language WG will work especially close with them,
particularly given the long-lasting effects of language decisions.

### Members

* @JamesWTruher
* @daxian-dbw
* @SeeminglyScience

## Remoting

The Remoting WG should focus on topics like the **PowerShell Remoting Protocol (PSRP)**,
the **protocols implemented under PSRP** (e.g. WinRM and SSH),
and **other protocols used for remoting** (e.g. "pure SSH" as opposed to SSH over PSRP).
Given the commonality of serialization boundaries, the Remoting WG should also focus on
**the PowerShell job system**.

### Members

* @anmenaga
* @TravisEz13

## Cmdlets and Modules

The Cmdlet WG should focus on core/inbox modules whose source code lives within the
`PowerShell/PowerShell` repository,
including the proposal of new cmdlets and parameters, improvements and bugfixes to existing
cmdlets/parameters, and breaking changes.

However, some modules that ship as part of the PowerShell package are managed in other source repositories.
These modules are owned by the maintainers of those individual repositories.
These modules include:

* [`Microsoft.PowerShell.Archive`](https://github.com/PowerShell/Microsoft.PowerShell.Archive)
* [`PackageManagement` (formerly `OneGet`)](https://github.com/OneGet/oneget)
* [`PowerShellGet`](https://github.com/PowerShell/PowerShellGet)
* [`PSDesiredStateConfiguration`](https://github.com/PowerShell/xPSDesiredStateConfiguration)
  (Note: this community repository maintains a slightly different version of this module on the Gallery,
  but should be used for future development of `PSDesiredStateConfiguration`.)
* [`PSReadLine`](https://github.com/PowerShell/PSReadLine)
* [`ThreadJob`](https://github.com/PowerShell/Modules/tree/master/Modules/Microsoft.PowerShell.ThreadJob)

### Members

* @JamesWTruher
* @SteveL-MSFT
* @jdhitsolutions
* @TobiasPSP
* @doctordns
* @kilasuit

## Security

The Security WG should be brought into any issues or pull requests which may have security implications
in order to provide their expertise, concerns, and guidance.

### Members

* @TravisEz13
* @SydneySmithReal
* @anamnavi
* @SteveL-MSFT

## Explicitly not Working Groups

Some areas of ownership in PowerShell specifically do not have Working Groups.
For the sake of completeness, these are listed below:

### Build

Build includes everything that is needed to build, compile, and package PowerShell.
This bucket is also not oriented a customer-facing deliverable and is already something handled by Maintainers,
so we don't need to address it as part of the WGs.

* Build
  * `build.psm1`
  * `install-powershell.ps1`
  * Build infrastructure and automation
* Packaging
  * Scripts
  * Infrastructure

### Quality

Similar to the topic of building PowerShell, quality
(including **test code**, **test infrastructure**, and **code coverage**)
should be managed by the PowerShell Maintainers.

* Test code
  * Pester unit tests
  * xUnit unit tests
* Test infrastructure
  * Nightlies
  * CI
* Code coverage
* Pester
