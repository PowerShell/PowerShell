# Business Logic / Core Domain

> Parent: [../AGENTS.md](../AGENTS.md)

For a language runtime the "business logic" is the **command execution model** and the
**language semantics**. This document describes those flows and the domain vocabulary.

## Core Flow: Script Text → Output

```
 "Get-Process | Where-Object CPU -gt 10 | Sort-Object CPU"
        |
        v
 (1) Tokenize + Parse   engine/parser  ->  AST
        |
        v
 (2) Compile            AST -> executable delegate tree
        |
        v
 (3) Build pipeline     engine/pipeline.cs, CommandProcessor.cs
        |   one CommandProcessor per pipeline element
        v
 (4) Bind parameters    engine/*ParameterBinder*.cs
        |   coerce/validate args, resolve parameter sets
        v
 (5) Execute per record BeginProcessing -> ProcessRecord* -> EndProcessing
        |   objects stream element-to-element via Pipe.cs
        v
 (6) Resolve members    Extended Type System (ETS): MshObject.cs, TypeTable.cs
        |
        v
 (7) Format + render     FormatAndOutput  ->  ConsoleHost
```

## Command Types

PowerShell resolves a command name (`engine/CommandDiscovery.cs`, `CommandSearcher.cs`) to one
of several command kinds, each with its own processor:

| Command type | Source | Processor |
|---|---|---|
| Cmdlet | Compiled C# `[Cmdlet]` class | `CommandProcessor.cs` |
| Function / Filter | Script text | `ScriptCommandProcessor.cs` |
| Script (`.ps1`) | File | `ExternalScriptInfo.cs` |
| Native command | External executable | `NativeCommandProcessor.cs` |
| Alias | Name → command | resolved then delegated |

## Provider & Drive Model

PowerShell abstracts hierarchical stores (filesystem, registry, certificates, variables,
functions, environment) as **providers** exposing **drives**. Session state APIs live in
`engine/SessionState*.cs`; provider interfaces in `engine/*CmdletProviderInterfaces.cs`.
Cmdlets like `Get-ChildItem`/`Get-Item`/`Set-Location` operate uniformly across any provider.

## Pipeline Semantics (constraints)

- **Streaming** — objects flow one at a time; a downstream cmdlet's `ProcessRecord` runs as
  soon as an upstream object is available (not batched).
- **Object-based** — the pipeline passes .NET objects, not text.
- **Parameter binding order** — by parameter set, then by pipeline value (by-value, then
  by-property-name), then positional.
- **Common parameters** — every advanced command inherits `-Verbose`, `-Debug`,
  `-ErrorAction`, `-WarningAction`, `-ErrorVariable`, `-WhatIf`, `-Confirm`, etc.
  (`engine/CommonCommandParameters.cs`).
- **`ShouldProcess`** — state-changing cmdlets must gate mutations behind `ShouldProcess`
  to honor `-WhatIf`/`-Confirm`.

## Language / Execution Modes

`ExecutionContext` (`engine/ExecutionContext.cs`) carries the language mode that constrains
what a script may do:

| Mode | Meaning |
|---|---|
| `FullLanguage` | All language features allowed (default interactive). |
| `ConstrainedLanguage` | Restricted type/member access; enforced under application control policies (WLDP/AppLocker). |
| `RestrictedLanguage` | Data-only; used by data sections / manifests. |
| `NoLanguage` | Only pre-defined commands, no script text (e.g. JEA endpoints). |

These are security boundaries — see [SECURITY.md](SECURITY.md).

## Extensibility Points

- **Modules** — `.psm1`/`.psd1` and binary modules loaded via `Import-Module`
  (`engine/Modules`).
- **Experimental features** — gated features toggled via `Enable-ExperimentalFeature`
  (`engine/ExperimentalFeature`; declared in `experimental-feature-*.json`).
- **Subsystems** — pluggable engine services (e.g. predictors) under `engine/Subsystem`.
- **Extended Type System** — add members/type data at runtime (`Update-TypeData`,
  `*.ps1xml` type/format files).

## Domain Glossary

| Term | Meaning |
|---|---|
| Cmdlet | Compiled command implemented as a `[Cmdlet]` C# class. |
| Advanced function | Script function that opts into cmdlet-like binding via `[CmdletBinding()]`. |
| Runspace | An isolated execution context (engine + session state). |
| Session state | Variables, drives, functions, aliases, providers for a scope. |
| Provider | Adapter exposing a data store as drives/paths. |
| ETS | Extended Type System — runtime member/type extension over .NET objects. |
| PSObject / `MshObject` | Wrapper adding ETS members to any object. |
| Pipeline | Sequence of commands connected by `|`, streaming objects. |
| Parameter set | A named, mutually-exclusive group of parameters on a command. |
| AST | Abstract Syntax Tree produced by the parser. |
```
