# API Surface

> Parent: [../AGENTS.md](../AGENTS.md)

PowerShell is a language runtime, not an HTTP service, so its "API surface" is:

1. The **cmdlet/command surface** exposed to scripts, grouped by module.
2. The **.NET hosting API** exposed to applications via `Microsoft.PowerShell.SDK`.

Cmdlets are C# classes marked with the `[Cmdlet(verb, noun)]` attribute deriving from
`Cmdlet` / `PSCmdlet`. To enumerate the live surface from a build, run
`Get-Command -Module <name>` in the built `pwsh`.

## Command Surface by Module

| Module (project) | Representative commands |
|---|---|
| `Microsoft.PowerShell.Core` (in SMA) | `Get-Command`, `Get-Module`, `Import-Module`, `Where-Object`, `ForEach-Object`, `Get-Error`, `Start-Job`, `New-PSSession`, `Enter-PSHostProcess` |
| `Microsoft.PowerShell.Utility` | `Select-Object`, `Sort-Object`, `Measure-Object`, `ConvertTo-Json`/`ConvertFrom-Json`, `ConvertTo-Csv`/`Import-Csv`, `Format-Table`/`Format-List`/`Format-Wide`, `Write-Output`/`Write-Error`/`Write-Host`, `Invoke-WebRequest`/`Invoke-RestMethod`, `New-Guid`, `Get-FileHash`, `Add-Member`, `Add-Type` |
| `Microsoft.PowerShell.Management` | `Get-ChildItem`, `Get-Content`/`Set-Content`, `Copy-Item`/`Move-Item`/`Remove-Item`, `Get-Process`/`Stop-Process`, `Get-Service`/`Set-Service`, `Get-PSDrive`, `Test-Path`, `Get-ComputerInfo`, `Start-Process` |
| `Microsoft.PowerShell.Security` | `Get-Acl`/`Set-Acl`, `Get-ExecutionPolicy`/`Set-ExecutionPolicy`, `Get-Credential`, `ConvertTo-SecureString`, `Get-PfxCertificate`, CMS message cmdlets |
| `Microsoft.PowerShell.Diagnostics` | `Get-WinEvent`, `New-WinEvent`, `Get-Counter`, `Export-Counter`, `Import-Counter` |
| `Microsoft.PowerShell.LocalAccounts` (Windows) | `Get-LocalUser`/`New-LocalUser`, `Get-LocalGroup`, `Add-LocalGroupMember` |
| `CimCmdlets` | `Get-CimInstance`, `Get-CimClass`, `New-CimSession` |
| `Microsoft.WSMan.Management` | `Test-WSMan`, `Connect-WSMan`, WSMan config provider |

The authoritative, exhaustive per-command list is discoverable from the source under
`src/Microsoft.PowerShell.Commands.Utility/commands/utility`,
`src/Microsoft.PowerShell.Commands.Management/commands/management`, and the corresponding
`test/powershell/Modules/<Module>/*.Tests.ps1` files (one test file per command).

### Command shape (contract)

Every compiled command follows this contract:

- **Verb-Noun name** — the verb must come from the approved verb list (`Get-Verb`).
- **Parameters** — public properties marked `[Parameter]`, optionally grouped into parameter
  sets, with validation attributes (`[ValidateNotNullOrEmpty]`, `[ValidateRange]`, …).
- **Pipeline input** — `[Parameter(ValueFromPipeline)]` /
  `[Parameter(ValueFromPipelineByPropertyName)]`.
- **Lifecycle** — `BeginProcessing()` → `ProcessRecord()` (per pipeline item) →
  `EndProcessing()`; `StopProcessing()` for cancellation.
- **Output** — objects emitted via `WriteObject` / `WriteError` (see
  [ERROR_HANDLING.md](ERROR_HANDLING.md)).

See [DEV_GUIDE.md](DEV_GUIDE.md) for a full "add a cmdlet" recipe.

## Bundled Script Modules

Modules shipped with PowerShell but consumed from the PowerShell Gallery are declared in
`src/Modules/PSGalleryModules.csproj`:

| Module | Purpose |
|---|---|
| `Microsoft.PowerShell.PSResourceGet` | Install/manage modules & scripts from repositories |
| `Microsoft.PowerShell.Archive` | `Compress-Archive`/`Expand-Archive` |
| `PSReadLine` | Command-line editing/completion (bundled, see `test/powershell/Modules/PSReadLine`) |
| `ThreadJob` | `Start-ThreadJob` |

## .NET Hosting API (`Microsoft.PowerShell.SDK`)

Applications embed PowerShell through the public SMA types:

- `System.Management.Automation.PowerShell` — fluent API to build and invoke pipelines
  (`PowerShell.Create().AddCommand("Get-Process").Invoke()`).
- `Runspace` / `RunspacePool` (`engine/hostifaces`, `engine/remoting`) — execution contexts.
- `InitialSessionState` (`engine/InitialSessionState.cs`) — configure the commands, variables,
  and language mode available to a runspace.
- `PSHost` and related host interfaces (`engine/hostifaces`) — implemented by hosts such as
  `Microsoft.PowerShell.ConsoleHost`.

A minimal hosting sample lives at `docs/host-powershell/sample/MyApp/`.
```
