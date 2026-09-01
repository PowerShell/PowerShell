# Developer Guide

> Parent: [../AGENTS.md](../AGENTS.md)

How to build, test, and add code to PowerShell following the repo's conventions.

## Prerequisites & Bootstrap

The build is driven by the `build.psm1` PowerShell module. From an existing PowerShell
(`pwsh` or Windows PowerShell) at the repo root:

```powershell
Import-Module ./build.psm1
Start-PSBootstrap        # installs the pinned .NET SDK (global.json: 11.0.100-preview.6) + prerequisites
```

Platform-specific setup: `docs/building/windows-core.md`, `docs/building/linux.md`,
`docs/building/macos.md`. Build internals: `docs/building/internals.md`.

## Build

```powershell
Import-Module ./build.psm1
Start-PSBuild -Clean -PSModuleRestore -UseNuGetOrg
```

- Output binary path is returned by `Get-PSOutput`; run the dev build with `& (Get-PSOutput)`.
- Top-level build project is `src/powershell-win-core` (Windows) or `src/powershell-unix`
  (Linux/macOS); `dotnet` builds dependencies transitively.
- Target framework is `net11.0`; default configuration is `Debug`, default RID `win-x64`.
- `Start-PSBuild` runs pre-build codegen (`Start-ResGen`, `Start-TypeGen`) automatically.

## Test

PowerShell has two test suites:

```powershell
Import-Module ./build.psm1

# Pester (behavioral / feature tests) - test/powershell/**/*.Tests.ps1
Start-PSPester -UseNuGetOrg

# Run a subset
Start-PSPester -Path test/powershell/Modules/Microsoft.PowerShell.Utility

# xUnit (C# unit tests) - test/xUnit
Start-PSxUnit
```

- Pester tests are tagged; `CI`, `Feature`, `Scenario`, `Slow` (see the `-Tag`/`-ExcludeTag`
  parameters of `Start-PSPester`).
- Test results are validated with `Test-PSPesterResults` / `Test-XUnitTestResults`.

## Conventions

### C# (engine + cmdlets)
- Style is enforced by `.editorconfig`, `Settings.StyleCop`, `stylecop.json`, and
  `Analyzers.props` (analyzers run during build). Follow existing patterns.
- Cmdlet classes are named `<Verb><Noun>Command` (e.g. `AddMemberCommand`) and carry
  `[Cmdlet(VerbsCommon.Add, "Member")]`.
- Verbs must be approved verbs (`Get-Verb`); nouns are singular.
- User-facing strings live in `resources/*.resx`, accessed via generated strongly-typed
  classes (`Start-ResGen`) - never hard-code English.

### PowerShell scripts / modules
Repo instruction files under `.github/instructions/` are authoritative and apply to
`**/*.ps1` / `**/*.psm1`. Key ones:
- `powershell-parameter-naming.instructions.md` - PascalCase, singular nouns, units in names
  (`TimeoutSec`), align with built-in cmdlet parameters.
- `powershell-automatic-variables.instructions.md` - correct use of automatic variables.
- `start-native-execution.instructions.md` - invoking native commands in build scripts.
- `pester-set-itresult-pattern.instructions.md` - Pester `Set-ItResult` usage.

### Cmdlet lifecycle & guards
- Implement `BeginProcessing` / `ProcessRecord` / `EndProcessing` as needed.
- Gate state changes behind `ShouldProcess` to support `-WhatIf`/`-Confirm`.
- Emit errors with `WriteError` / `ThrowTerminatingError` and an `ErrorRecord`
  (see [ERROR_HANDLING.md](ERROR_HANDLING.md)).

## Recipe: Add a New Cmdlet

1. **Pick the module.** Utility-type command -> `src/Microsoft.PowerShell.Commands.Utility`;
   item/provider/OS command -> `src/Microsoft.PowerShell.Commands.Management`; security -> 
   `src/Microsoft.PowerShell.Security`.
2. **Create the class** under that project's `commands/` folder. Study an existing,
   similarly-shaped cmdlet as a template, e.g.
   `src/Microsoft.PowerShell.Commands.Utility/commands/utility/ConvertFrom-SddlString.cs`
   (simple) or `.../AddMember.cs` (parameter sets + errors).

   ```csharp
   using System.Management.Automation;

   namespace Microsoft.PowerShell.Commands
   {
       [Cmdlet(VerbsCommon.Get, "Example", DefaultParameterSetName = "ByName")]
       [OutputType(typeof(string))]
       public sealed class GetExampleCommand : PSCmdlet
       {
           [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
           [ValidateNotNullOrEmpty]
           public string Name { get; set; }

           protected override void ProcessRecord()
           {
               try
               {
                   WriteObject($"Hello {Name}");
               }
               catch (System.Exception ex)
               {
                   WriteError(new ErrorRecord(ex, "GetExampleFailed",
                       ErrorCategory.NotSpecified, Name));
               }
           }
       }
   }
   ```

3. **Add user-facing strings** to the module's `resources/*.resx` (re-run `Start-ResGen` or
   let `Start-PSBuild` do it).
4. **Export it** if needed via the module's manifest (`*.psd1`) `CmdletsToExport`.
5. **Add tests** - a Pester file `test/powershell/Modules/<Module>/<Command>.Tests.ps1`
   (one file per command is the repo convention). Minimal shape:

   ```powershell
   # Copyright (c) Microsoft Corporation.
   # Licensed under the MIT License.
   Describe "Get-Example" -Tags "CI" {
       It "returns a greeting" {
           Get-Example -Name "World" | Should -BeExactly "Hello World"
       }
   }
   ```

6. **Build & test**: `Start-PSBuild` then
   `Start-PSPester -Path test/powershell/Modules/<Module>/<Command>.Tests.ps1`.

## Experimental Features

New/behavior-changing features are often gated as experimental features (see
`engine/ExperimentalFeature` and `experimental-feature-windows.json` /
`experimental-feature-linux.json`). Users toggle them with `Enable-ExperimentalFeature`.

## Key Files by Concern

| Concern | Location |
|---|---|
| Build/test automation | `build.psm1` (root) |
| CI helpers | `tools/ci.psm1` |
| Parser / language | `src/System.Management.Automation/engine/parser` |
| Pipeline / command execution | `engine/CommandProcessor.cs`, `pipeline.cs`, `MshCommandRuntime.cs` |
| Parameter binding | `engine/*ParameterBinder*.cs` |
| Session state / providers | `engine/SessionState*.cs`, `engine/*CmdletProviderInterfaces.cs` |
| Error model | `engine/ErrorPackage.cs` |
| Security | `src/System.Management.Automation/security` |
| Logging/telemetry | `src/System.Management.Automation/logging` |
| Resource generation | `src/ResGen` |
| Type catalog generation | `src/TypeCatalogGen` |
| CI/CD pipelines | `.pipelines/`, `.github/workflows/`, `.vsts-ci/` |
