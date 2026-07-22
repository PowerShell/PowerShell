# Dependencies

> Parent: [../AGENTS.md](../AGENTS.md)

PowerShell is a library/runtime, so its dependencies are the **.NET runtime**, a set of
**NuGet packages**, a few **native components**, and **bundled script modules** - not upstream
network services. Versions below are as declared in the `.csproj` files at the time of writing;
treat the `.csproj` as the source of truth.

## Platform

| Dependency | Version | Notes |
|---|---|---|
| .NET SDK | `11.0.100-preview.6.26359.118` | Pinned in `global.json`. |
| Target framework | `net11.0` | `PowerShell.Common.props`. |
| Runtime identifiers | `win-x86`, `win-x64`, plus linux/osx via `powershell-unix` | Windows RIDs in `powershell-win-core.csproj`. |

## Core Engine NuGet Packages (`System.Management.Automation.csproj`)

| Package | Version | Purpose |
|---|---|---|
| `Newtonsoft.Json` | `13.0.4` | JSON serialization used by the engine. |
| `Microsoft.ApplicationInsights` | `2.23.0` | Optional telemetry. |
| `Microsoft.Management.Infrastructure` | `3.0.0` | CIM/WMI (MI) interop. |
| `Microsoft.PowerShell.Native` | `700.0.0` | Native helpers (`libpsl`/WinRM plugin surface). |
| `Microsoft.Security.Extensions` | `1.4.0` | File provenance / trust checks. |
| `System.Security.Cryptography.Pkcs` | `8.0.1` | CMS/crypto. |
| `System.Configuration.ConfigurationManager` | `8.0.1` | Configuration. |
| `System.Diagnostics.DiagnosticSource` | `8.0.1` | Activity/telemetry plumbing. |
| `System.DirectoryServices` | `8.0.0` | Directory access (Windows). |
| `System.Management` | `8.0.0` | WMI (Windows). |
| `Microsoft.Win32.Registry.AccessControl` | `8.0.0` | Registry ACLs (Windows). |
| `System.Text.Encoding.CodePages` | `8.0.0` | Legacy encodings. |
| `System.Formats.Asn1` | `8.0.2` | ASN.1 parsing. |

## Notable Command-Module Packages

| Package | Version | Used by |
|---|---|---|
| `Markdig.Signed` | `1.3.2` | `Microsoft.PowerShell.Commands.Utility` (Markdown cmdlets) |
| `Microsoft.PowerShell.MarkdownRender` | `7.2.1` | Utility (Markdown rendering) |
| `Microsoft.CodeAnalysis.CSharp` | `5.6.0` | Utility (`Add-Type`) |
| `Microsoft.Windows.Compatibility` | `8.0.28` | `Microsoft.PowerShell.SDK` (Windows compat shims) |
| `System.Data.SqlClient` | `4.9.1` | SDK meta-package |
| `System.ServiceModel.*` | `4.10.3` | SDK (WCF client stack) |
| `System.Diagnostics.PerformanceCounter` | `8.0.1` | `Microsoft.PowerShell.Commands.Diagnostics` |
| `System.Diagnostics.EventLog` | `8.0.2` | Multiple modules (Windows event log) |

## Native Components

| Component | Package | Platform |
|---|---|---|
| `libpsl-native.so` / `.dylib` | `libpsl` | Linux / macOS |
| `pwrshplugin.dll` (WinRM remoting plugin) | `psrp.windows` | Windows |
| `PowerShell.Core.Instrumentation.dll` | (same feed) | Windows ETW instrumentation |

Native components are **not built in this repo**. Their source lives in the separate
[PowerShell/PowerShell-Native](https://github.com/PowerShell/PowerShell-Native) repository and is
consumed here as pre-built NuGet packages (`Microsoft.PowerShell.Native`, `psrp.windows`). The
`src/libpsl-native` and `src/powershell-native` folders in this repo are stubs (README plus the
`Install-PowerShellRemoting.ps1` helper). Historical build steps are in `docs/building/internals.md`.

## Bundled Script Modules (`src/Modules/PSGalleryModules.csproj`)

| Module | Version | Purpose |
|---|---|---|
| `Microsoft.PowerShell.PSResourceGet` | `1.3.0-preview1` | Package management from repositories/PSGallery |
| `Microsoft.PowerShell.Archive` | `1.2.5` | Archive cmdlets |

`PSReadLine` and `ThreadJob` are also bundled (see their `test/powershell/Modules` folders).

## Runtime External Systems (optional, feature-dependent)

These are contacted only when the corresponding feature is used - none are required to run
`pwsh`:

| System | When used | Entry point |
|---|---|---|
| PowerShell Gallery (`www.powershellgallery.com`) | `Install-PSResource` / module install | PSResourceGet |
| WSMan endpoints | `Enter-PSSession`/`Invoke-Command` over WinRM | `Microsoft.WSMan.Management`, `engine/remoting` |
| SSH | Remoting over SSH transport | `engine/remoting` |
| CIM/WMI providers | `Get-CimInstance` etc. | `Microsoft.Management.Infrastructure` |
| Update service (`aka.ms`) | `Update-Help`, version notifications | help system / `engine` |

## Ownership / Escalation

**Owner:** PowerShell Team (Microsoft). Source repository:
[github.com/powershell/powershell](https://github.com/powershell/powershell).

This is an open-source project; collaboration happens via GitHub issues and pull requests on
the upstream repository.

Security issues follow a separate path - see [SECURITY.md](SECURITY.md) and
`.github/SECURITY.md` (report via <https://aka.ms/secure-at>).
