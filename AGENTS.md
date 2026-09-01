# PowerShell

## Service / Repo Overview

PowerShell is a cross-platform (Windows, Linux, macOS) command-line shell, scripting language,
and automation framework built on .NET (`net11.0`). It is optimized for working with structured
data (JSON, CSV, XML), REST APIs, and .NET object models, and ships the `pwsh` executable plus
a set of core modules. Unlike text-based shells, its pipeline passes .NET **objects** between
commands.

The engine (`System.Management.Automation`) parses and executes the PowerShell language, hosts
the command pipeline, exposes a provider/drive model over hierarchical stores (filesystem,
registry, certificates, …), and supports remoting over WSMan and SSH. Applications can embed
the engine through `Microsoft.PowerShell.SDK`. Commands are delivered as compiled cmdlets
(C# `[Cmdlet]` classes) grouped into modules such as `Microsoft.PowerShell.Utility`,
`Microsoft.PowerShell.Management`, and `Microsoft.PowerShell.Security`.

The source lives in the open-source repository
[github.com/powershell/powershell](https://github.com/powershell/powershell). Most user-facing
documentation lives in [MicrosoftDocs/PowerShell-Docs](https://github.com/MicrosoftDocs/PowerShell-Docs);
this repo holds engineering docs under `docs/`.

**Owner:** PowerShell Team (Microsoft)
**Security reporting:** <https://aka.ms/secure-at> (MSRC) - see `.github/SECURITY.md`

---

## Quick Reference

```powershell
Import-Module ./build.psm1

Start-PSBootstrap                              # install pinned .NET SDK + prerequisites
Start-PSBuild -Clean -PSModuleRestore -UseNuGetOrg   # build; run with: & (Get-PSOutput)

Start-PSPester -UseNuGetOrg                    # Pester behavioral tests (test/powershell)
Start-PSxUnit                                  # C# unit tests (test/xUnit)
```

Target framework `net11.0`; SDK pinned in `global.json`. Full details in
[docs/DEV_GUIDE.md](docs/DEV_GUIDE.md).

---

## Documentation Index

Read the file(s) relevant to your task. Each file is self-contained with cross-references.

| When you need to... | Read |
|---|---|
| Understand system design, engine layers, project-to-module mapping | [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) |
| See the cmdlet/command surface and the .NET hosting API | [docs/API_SURFACE.md](docs/API_SURFACE.md) |
| Understand the execution model, language modes, provider/pipeline semantics | [docs/BUSINESS_LOGIC.md](docs/BUSINESS_LOGIC.md) |
| Understand .NET/NuGet/native dependencies and optional external systems | [docs/DEPENDENCIES.md](docs/DEPENDENCIES.md) |
| Understand the error model, ErrorRecord, terminating vs non-terminating errors | [docs/ERROR_HANDLING.md](docs/ERROR_HANDLING.md) |
| Build/test, follow conventions, add a cmdlet, find key files by concern | [docs/DEV_GUIDE.md](docs/DEV_GUIDE.md) |
| Check security boundaries, language modes, signing, vuln reporting | [docs/SECURITY.md](docs/SECURITY.md) |

Additional engineering docs already in the repo: `docs/building/`, `docs/testing-guidelines/`,
`docs/debugging/`, `docs/dev-process/`, and cmdlet examples in `docs/cmdlet-example/`.
Repo-wide coding rules live under `.github/instructions/`.

---

## docs/ Tree

```
docs/
  ARCHITECTURE.md     - system context, engine layers, project mapping
  API_SURFACE.md      - command surface by module + .NET hosting API
  BUSINESS_LOGIC.md   - execution model, language modes, providers, glossary
  DEPENDENCIES.md     - .NET/NuGet/native deps, optional external systems
  ERROR_HANDLING.md   - ErrorRecord model, terminating vs non-terminating
  DEV_GUIDE.md        - build/test, conventions, add-a-cmdlet recipe, key files
  SECURITY.md         - security subsystem, boundaries, signing, MSRC reporting
```
