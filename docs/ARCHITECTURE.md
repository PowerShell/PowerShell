# Architecture

> Parent: [../AGENTS.md](../AGENTS.md)

PowerShell is a cross-platform (Windows, Linux, macOS) command-line shell, scripting
language, and automation framework built on .NET (`net11.0`). This document describes how
the pieces fit together and where each responsibility lives in the `src/` tree.

## Systems Context (ASCII)

```
        +-------------------+        +--------------------------+
        |  User / Script /  |        |  Host applications        |
        |  CI / Automation  |        |  (VS Code, Azure Cloud    |
        +---------+---------+        |   Shell, apps embedding    |
                  |                  |   Microsoft.PowerShell.SDK)|
                  v                  +-------------+------------+
        +-------------------+                      |
        |  pwsh executable  | <--------------------+
        | (src/powershell)  |
        +---------+---------+
                  |
                  v
   +------------------------------------------+
   |  System.Management.Automation (SMA)      |
   |  parser -> AST -> compiler -> pipeline   |
   |  engine, providers, remoting, security   |
   +----+--------------------+----------------+
        |                    |
        v                    v
 +--------------+   +-----------------------------+
 | Command      |   | External systems            |
 | modules      |   | - .NET BCL / OS APIs        |
 | (Utility,    |   | - WSMan / SSH remoting      |
 |  Management, |   | - CIM/WMI (MI)              |
 |  Security..) |   | - PSGallery (PSResourceGet) |
 +--------------+   +-----------------------------+
```

## Internal Architecture (ASCII)

```
  Input text
     |
     v
 [ Tokenizer/Parser ]  src/System.Management.Automation/engine/parser
     |  Abstract Syntax Tree (AST)
     v
 [ Compiler ]          engine/parser (compiles AST to executable)
     |
     v
 [ Pipeline Processor ] engine/CommandProcessor.cs, pipeline.cs
     |        \
     |         +--> [ Parameter Binder ]  engine/*ParameterBinder*.cs
     v
 [ Cmdlet / Function / Native command invocation ]
     |        engine/cmdlet.cs, MshCommandRuntime.cs, NativeCommandProcessor.cs
     v
 [ Session State ]  engine/SessionState*.cs  (variables, drives, providers, scopes)
     |
     v
 [ Extended Type System (ETS) + Formatting ]
     |  engine/MshObject.cs, TypeTable.cs ; FormatAndOutput
     v
  Output objects  ->  Host (ConsoleHost) renders to console
```

Cross-cutting engine services live under `src/System.Management.Automation/engine`:

- **Remoting** — `engine/remoting` (WSMan, SSH, named-pipe transports, runspaces/pools).
- **Debugger** — `engine/debugger`.
- **Command completion / IntelliSense** — `engine/CommandCompletion`.
- **Interpreter / language runtime** — `engine/interpreter`, `engine/lang`, `engine/runtime`.
- **Subsystem plugin model** — `engine/Subsystem`.
- **Logging / telemetry** — `src/System.Management.Automation/logging`.
- **Security** — `src/System.Management.Automation/security` (execution policy, WLDP/AppLocker,
  AMSI, Constrained Language Mode).

## Project-to-Layer Mapping

| Project (`src/…`) | Layer / Role |
|---|---|
| `System.Management.Automation` | Core engine (SMA): parser, pipeline, providers, remoting, ETS, security |
| `powershell` | Cross-platform managed host entry point that produces `pwsh` |
| `powershell-win-core` | Top-level build project for Windows (CoreCLR) |
| `powershell-unix` | Top-level build project for Linux/macOS (CoreCLR) |
| `Microsoft.PowerShell.ConsoleHost` | Console host implementation (REPL, host UI) |
| `Microsoft.PowerShell.Commands.Utility` | Utility cmdlets (`Select-Object`, `ConvertTo-Json`, `Format-*`, web cmdlets, …) |
| `Microsoft.PowerShell.Commands.Management` | Provider/management cmdlets (`Get-ChildItem`, `Copy-Item`, services, processes, …) |
| `Microsoft.PowerShell.Commands.Diagnostics` | Diagnostics cmdlets (`Get-WinEvent`, `Get-Counter`) |
| `Microsoft.PowerShell.Security` | Security cmdlets (ACLs, certificates, execution policy, CMS) |
| `Microsoft.PowerShell.LocalAccounts` | Local user/group cmdlets (Windows) |
| `Microsoft.PowerShell.CoreCLR.Eventing` | ETW eventing support |
| `Microsoft.WSMan.Management` / `Microsoft.WSMan.Runtime` | WSMan provider and runtime |
| `Microsoft.Management.Infrastructure.CimCmdlets` | CIM/WMI cmdlets |
| `Microsoft.Management.UI.Internal` | `Out-GridView`/GUI support (Windows) |
| `Microsoft.PowerShell.SDK` | Meta-package for hosting PowerShell in .NET apps |
| `Microsoft.PowerShell.GlobalTool.Shim` / `GlobalTools` | `dotnet tool` packaging shim |
| `Modules` | Script modules bundled with PowerShell (see `Modules/PSGalleryModules.csproj`) |
| `ResGen` | Build-time strongly-typed resource generator |
| `TypeCatalogGen` | Build-time type catalog generator (`CorePsTypeCatalog.cs`) |
| `libpsl-native` | Stub (`README.md`); the native `libpsl-native` source lives in the separate [PowerShell/PowerShell-Native](https://github.com/PowerShell/PowerShell-Native) repo, shipped via the `Microsoft.PowerShell.Native` NuGet package |
| `powershell-native` | Stub (`README.md`, `Install-PowerShellRemoting.ps1`); native components (WinRM `pwrshplugin.dll`, instrumentation) are built in [PowerShell/PowerShell-Native](https://github.com/PowerShell/PowerShell-Native) and consumed as NuGet packages |
| `Schemas`, `signing` | JSON schemas and signing assets |

## Deprecated / Ignore List

- `docs/DOCSMIGRATION.md` documents that most user-facing docs moved to
  [MicrosoftDocs/PowerShell-Docs](https://github.com/MicrosoftDocs/PowerShell-Docs); do not
  add end-user documentation here.
- `graphify-out/` — generated analysis output, not source.
- `test/tools/`, `tools/packaging/projects/reference/` — build/test scaffolding, not product code.
- Auto-generated `gen/` resource folders and `CorePsTypeCatalog.cs` — regenerated by the build
  (`Start-ResGen`, `Start-TypeGen`); do not hand-edit.
```
