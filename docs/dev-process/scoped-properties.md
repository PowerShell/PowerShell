# Scoped properties prototype

Run `Enable-ExperimentalFeature PSScopedProperties` and start a new PowerShell process to activate the feature. If you have no enabled experimental features in your user configuration, you can also save the following as `scoped-properties.json` and start a prototype build with `pwsh -NoProfile -SettingsFile ./scoped-properties.json`. A nonempty list of experimental features in the user configuration takes precedence over the list provided by `-SettingsFile`.

```json
{"ExperimentalFeatures":["PSScopedProperties"]}
```

## Using scoped settings

The feature registers a constant, AllScope `$PS` variable. Its writable action preference properties share typed `ActionPreference` values with their corresponding legacy variables.

| Facade property | Legacy variable | Default |
| --- | --- | --- |
| `$PS.ActionPreference.Error` | `$ErrorActionPreference` | `Continue` |
| `$PS.ActionPreference.Warning` | `$WarningPreference` | `Continue` |
| `$PS.ActionPreference.Information` | `$InformationPreference` | `SilentlyContinue` |
| `$PS.ActionPreference.Verbose` | `$VerbosePreference` | `SilentlyContinue` |
| `$PS.ActionPreference.Debug` | `$DebugPreference` | `SilentlyContinue` |

Each of these five preferences and `$PS.Style.Progress.View` inherits independently through PowerShell scopes. Assignment in a scope without a binding creates a local override. Captured grouping objects resolve their properties against the active scope at each use.

```powershell
$PS.ActionPreference.Error = 'Continue'
$PS.Style.Progress.View = 'Minimal'

& {
    $ErrorActionPreference = 'Stop'
    $PSStyle.Progress.View = 'Classic'
    $PS.ActionPreference.Error       # Stop
    $PS.Style.Progress.View          # Classic

    $PS.Reset('ActionPreference.Error')
    $PS.ActionPreference.Error       # Continue
    $PS.Style.Progress.View          # Classic
}

$PS.Style.Progress.View              # Minimal
```

Use `$PS.AtScope('Local')`, `$PS.AtScope('Script')`, or `$PS.AtScope('Global')` to target a concrete scope. For example, `$PS.AtScope('Script').Style.Progress.View = 'Classic'` writes to the script scope. Scope views expire with their target and belong to the runspace that created them. Use `AtScope()` for scope targeting because qualified expressions such as `$script:PS` are rejected.

`Reset()` takes a case-insensitive path such as `ActionPreference.Warning` or `Style.Progress.View`. It removes the target override, subject to variable protections, and resumes inheritance. Calling it with no override present has no effect. It works with literal and dynamically supplied method names.
`Remove-Variable WarningPreference -Scope Local` and `$PS.Reset('ActionPreference.Warning')` both remove a local warning preference.

The console host uses the effective progress view from the latest update for its shared progress pane and may render in classic mode for terminals without the required capabilities.

The prototype covers the five action preferences and the progress rendering view. Progress action, Confirm, WhatIf, other configuration variables, public authoring APIs, mutable collections, and deprecation configuration are outside its scope. Access to the facade requires an active PowerShell execution context and remains subject to constrained language restrictions.

## Baseline before canonical Error storage

Before the canonical storage changes, the build passed with the .NET 11 SDK. The published binaries passed all 19 existing scoped-properties Pester cases and all four xUnit cases in `ScopedPropertiesTests` and `ScopedProgressTests`.
The scoped Pester run used an explicit module path and temporary registry access for its test fixture. No failures were observed in these scoped test runs.

Selected variable, preference, error-handling, automatic-variable, and module Pester suites passed 336 cases with two pending in each feature-enabled and feature-disabled configuration. The disabled run kept the other generated experimental features enabled.
The isolated module fixture was also investigated separately. With `Test.Isolated.Init` built, loading it encountered a version conflict between the fixture's 3.0 assembly and the already bound assembly.

The full xUnit baseline encountered seven locale-sensitive `Utils.DisplayHumanReadableFileSize` failures and a refused localhost connection in `RemoteHyperV`, where the run hung.
An additional run after phase 2 excluded `RemoteHyperV` and reported 417 passes, the same seven locale failures, and six skips. This count includes the five new phase 2 tests.

## Canonical storage contract

When `PSScopedProperties` is enabled, each legacy preference variable uses the same typed entry as the corresponding `$PS` property and engine consumers. `$PSStyle.Progress.View` also shares its typed entry with `$PS.Style.Progress.View`.
Both access paths convert and validate assignments before writing the target entry. The effective value comes from the nearest visible entry.
When no entry is visible, preference resolution uses the default in the table and progress rendering uses `Minimal`. A legacy variable lookup can then return no variable.
The preference variable adapter preserves variable protections and force rules, validation attributes, `Private` visibility, and explicitly configured `AllScope` sharing.
Entries and bound scope views belong to their originating runspace. The console host continues to use the effective progress view, and `PSStyle` retains its context-free fallback.

When the feature is disabled, `$PS` remains an ordinary variable name. Legacy preference variables and `$PSStyle.Progress.View` continue to use their existing storage and validation paths.

Each action preference entry has a variable adapter that supplies the metadata and exposes the entry through `Value`. Explicit `AllScope` configuration shares the entry and its adapter across scope bindings.
A variable object returned by `Get-Variable` or `New-Variable -PassThru` tracks the live entry. Explicit removal of its last binding clears its value and variable options and attributes, as with an ordinary removed variable. Editing the retained object afterward leaves that binding absent.
An object captured from a local scope that exits normally retains its value. With no other live binding, changes to that object stay independent of the caller's preference.

### Invocation bindings

Ordinary and advanced functions bind parameters named after a registered legacy variable as typed preferences. Advanced functions initialize invocation-owned values from explicit `-ErrorAction`, `-WarningAction`, and `-InformationAction` arguments. Explicit `-Verbose` or `-Debug` supplies `Continue`, and an explicit false supplies `SilentlyContinue`. Omitting a common parameter leaves the preference to the named parameter or normal scope inheritance. When both parameters supply the same preference, the explicit common parameter takes precedence.

A dot-sourced advanced function uses one temporary frame for all invocation-owned preferences. The scope activates the frame during each `begin`, `process`, `end`, and `clean` stage and deactivates it between stages. Nested dotted frames deactivate in reverse activation order. The current bindings survive between stages and are released when the invocation completes.

Reset removes the selected temporary override. Reads fall through to the next visible binding, and later writes recreate an override in the same invocation frame. Dot-sourced writes without an invocation binding target the caller's scope. Each nested command still applies its own explicit common parameters, stream policy, and preference caching rules.

## Migrating scripts

- Use an `ActionPreference` member, a convertible name, or a defined numeric value when setting a scoped action preference. Move any raw data stored in a child preference variable to an ordinary variable.
- Use `Reset()` or `Remove-Variable -Scope Local` to resume preference inheritance. `Clear-Variable` attempts to assign null.
- After supplying a `PSVariable` through a session-state API, retrieve the installed object with `Get-Variable` if later code needs to edit the setting through `.Value`.
- Put `-ErrorAction` directly on a nested command when that command needs a specific policy after the function resets its own preference.

## Compatibility decisions

### Typed legacy preference assignments

The built-in action preference variables already converted assignments through their type converters before canonical storage. A child could create an untyped shadow that retained raw values until the engine consumed the preference.
With the feature enabled, legacy writes use the corresponding scoped property's conversion and validation rules before updating the entry. The following example uses the error preference.

```powershell
$ErrorActionPreference = 'sToP'       # Before and after: ActionPreference Stop
& {
    $ErrorActionPreference = 'cOnTiNuE'
    $ErrorActionPreference            # Before: String cOnTiNuE. After: ActionPreference Continue.

    $ErrorActionPreference = 1
    $ErrorActionPreference            # Before: Int32 1. After: ActionPreference Stop.

    try { $ErrorActionPreference = $null } catch { 'null rejected' }
    $ErrorActionPreference            # Before: null. After: Stop after a failed conversion.

    try { $ErrorActionPreference = 'invalid preference' } catch { 'invalid value rejected' }
    $ErrorActionPreference            # Before: String invalid preference. After: Stop after a failed conversion.

    try { Clear-Variable ErrorActionPreference -Scope Local } catch { 'clear rejected' }
    $ErrorActionPreference            # Before: null. After: Stop after a failed conversion.
}
```

Defined numeric inputs convert to the corresponding enum member. Null, undefined numeric inputs, invalid names, and the reserved `Suspend` value are rejected.
Failed conversion or validation preserves the previous override, or leaves inheritance in effect if there was no override.
`Clear-Variable` and `Clear-Item Variable:ErrorActionPreference` attempt to assign null and follow the same rules. A failed forced write also preserves the `ReadOnly` option.

The built-in typed variable already rejected `Suspend` at assignment. An untyped shadow could hold `Suspend` until an effective preference read replaced it with the default and raised `NotSupportedException`.
`Set-Variable` now rejects `Suspend` in that scope with `InvalidActionPreference` and preserves the previous value.

The decision gives variable readers, the facade, and engine consumers the same type and value. Preserving raw shadows and converting on read would leave the two access paths with different results.
Keeping a raw value alongside the typed value would require defining two observable values for the same setting and synchronizing updates to both.

To reverse this decision, remove the registered-name redirects from the `SessionStateScope` variable APIs, including initialization through `SessionStateInternal.AddSessionStateEntry` and `InitialSessionState.ResetRunspaceState`.
Restore ordinary tuple storage for the registered names in `VariableAnalysis` and `Compiler.DottedScriptCmdletLocalsNameIndexMap`.
Revisit `VariableOps.SetVariableValue`, `ScriptBlock.TryBindScopedSettingParameter`, and `ScriptParameterBinder.BindScopedSettingParameter` for direct assignments and parameters.
Route the registered preferences through the legacy branch of `ExecutionContext.GetEnumPreference` and preserve the built-in variables' `ArgumentTypeConverterAttribute` in `InitialSessionState`.
Retire the shared `ScopedActionPreferenceSetting` validation, the associated `ScopedSettingVariable` write paths, and the scoped handling in `SetVariableCommand` and `ClearVariableCommand`. Verify provider writes through `VariableProvider.SetSessionStateItem` as well as the direct paths.

`ScopedProperties.Tests.ps1` parameterizes the typed conversion, variable cmdlet, and variable provider cases over the registered preferences.
The feature-disabled, forced-write, provider-content, host initialization, attribute, and protection cases cover the compatibility boundaries.
`ActionPreference.Tests.ps1` covers legacy `Suspend` handling. `ScopedProperties.Enabled.Tests.ps1` covers assignment-time rejection of enum and string `Suspend` values for all five scoped preferences.

### Imported variable objects

Before the cutover, installing an ordinary `PSVariable` in a fresh scope could make the supplied object the variable stored in session state. The registered setting now creates a scope-owned adapter from the supplied value and metadata.

```powershell
& {
    $supplied = [psvariable]::new('ErrorActionPreference', 'Stop')
    $ExecutionContext.SessionState.PSVariable.Set($supplied)
    $supplied.Value = 'SilentlyContinue'
    $PS.ActionPreference.Error        # Before: SilentlyContinue. After: Stop.

    $stored = Get-Variable ErrorActionPreference -Scope Local
    $stored.Value = 'Continue'
    $PS.ActionPreference.Error        # Before and after: Continue.
}
```

The adapter makes every write to the installed variable go through the target entry's conversion and protection rules. Keeping the supplied object as the stored variable would allow its setter to write an independent raw value or, when the object came from another runspace, modify that runspace's entry.
To reverse the ownership change, change `ScopedSettingEntry<T>.Create` and `SessionStateScope.SetScopedSettingVariable` to reuse the source object, and provide a way for its `Value` and metadata to forward safely to the destination entry.
`ImportAndReplacementPreserveMetadataAndRejectInvalidUpdatesAtomically` in `ScopedSettingVariableTests` covers the adapter boundary. The case for copying ordinary and foreign variable objects in `ScopedProperties.Tests.ps1` covers the public setter path and runspace isolation.

Explicitly exported module variables continue to share their original object and metadata when imported into the same runspace. Before the cutover, importing a live module object into another runspace also shared the original variable. A write there could change the source module's preference.
An import into another runspace now copies the exported value and metadata into a separate adapter. `Remove-Module` removes the imported binding in either case. A still-bound source entry keeps its value and metadata under the same removal rule as `AllScope`.
Cross-runspace sharing would allow one runspace's commands to affect settings in another. To restore that behavior, remove the runspace check in `SessionStateScope.ImportVariable` and revert `SessionStateScope.IsImportedVariable` and `ScopedSettingVariable.ModuleImportSource`, used by `ModuleCmdletBase.RemoveModule`.
The exported module preference cases in `ScopedProperties.Tests.ps1` verify shared same-runspace writes, isolated foreign writes, and module removal on both paths.

### Uniform reset

Before the cutover, advanced functions used an implicit `$ErrorActionPreference` slot when called with `-ErrorAction`, and `Reset()` could fail on that slot or on an optimized ordinary local variable.
Implicit function preferences and ordinary local assignments now use removable scope entries, regardless of how `Reset()` is invoked.

```powershell
$PS.ActionPreference.Error = 'Continue'
function Show-Preference {
    [CmdletBinding()]
    param()

    $PS.ActionPreference.Error        # Before and after: Stop.
    try { $PS.Reset('ActionPreference.Error') } catch { 'reset failed' }
    $PS.ActionPreference.Error        # Before: Stop after the failure. After: Continue.
    Write-Error 'expected' -ErrorAction Ignore
}
Show-Preference -ErrorAction Stop
. Show-Preference -ErrorAction Stop  # The same reset outcome applies in a dot-sourced invocation.
$PS.ActionPreference.Error          # Before and after: Continue in the caller.

function Show-DynamicReset {
    $ErrorActionPreference = 'Stop'
    $method = 'Reset'
    try { $PS.$method('ActionPreference.Error') } catch { 'reset failed' }
    $PS.ActionPreference.Error        # Before without a breakpoint: Stop after failure. After: Continue.
}
Show-DynamicReset
```

Removing the override lets nested commands use the inherited preference unless they specify their own `-ErrorAction`.
Previously, removing a child binding to an `AllScope` variable also cleared the shared variable object. Reset and legacy removal now remove the target binding and preserve the value and metadata available through the other bindings. The target scope can then inherit that entry from its parent.

```powershell
& {
    New-Variable ErrorActionPreference 'SilentlyContinue' -Option AllScope
    & { Remove-Variable ErrorActionPreference -Scope Local }
    $ErrorActionPreference            # Before: null. After: SilentlyContinue.
}
```

An alternative was to preserve the implicit preference for the lifetime of the advanced function. That would make the same reset call behave differently based on how the preference entered the scope.
Another alternative was to write the default into the target scope. That would keep overriding the parent when it has a different preference.

To reverse this decision while keeping typed entries, preserve the origin of each invocation preference initialized by `PSScriptCmdlet.SetPreferenceVariables` through `SessionStateScope.InitializeScopedSetting`.
Reinstate the removal restriction in `SessionStateScope.ResetScopedSetting` and `RemoveScopedSettingVariable`.
Restore the optimized-local distinction in `VariableAnalysis.NoteVariable` and the former literal-reset analysis to reinstate the old dynamic restriction. For dot-sourced invocations, review `SessionStateScope.GetScopedSettingVariableForWrite` and `SetVariableCommand.TrySetActiveScopedBinding` as well as the reset path.
Restoring shared-object clearing would require changing when `ScopedSettingEntry.ReleaseBinding` calls `ScopedSettingVariable.Detach`. Preserve normal scope exit behavior in `SessionStateScope.ClearScopedSettings` and `DottedScopedSettings.Release` when changing explicit removal.

`ScopedProperties.Tests.ps1` covers reset for implicit invocation preferences, optimized functions with and without variable breakpoints, temporary frames across command stages, and `AllScope` bindings.
`uses the inherited policy for a nested command immediately after either form of reset` covers command execution directly after removal. The captured-variable cases cover the difference between explicit removal and normal scope exit.

## Canonical Error storage validation

The .NET 11 Windows build passed. The selected Pester suites passed 452 tests in each feature-enabled and feature-disabled run, including all 38 scoped-properties cases, variable cmdlets and provider operations, automatic variables, preference and command error handling, clean blocks, module import/removal, and progress. Each run had one console-dependent skip and four pending cases.

The full xUnit run completed with 446 passes, six skips, and eight locale-dependent failures. Seven were the decimal-format expectations in `UtilsTests.DisplayHumanReadableFileSize` reproduced in the baseline. The remaining failure was the unchanged `RemoteHyperVTests.ValidateTokenTimeoutFails` assertion expecting an English socket error message while Windows returned Swedish. The initial baseline also encountered a socket startup race and hung; the final run completed. These failures were not changed or suppressed. All 15 focused scoped-properties, adapter, and progress xUnit cases passed.

The final source audit checked the scope mutation and initialization paths, compiler and parameter binding, tuple and debugger enumeration, module imports, retained variables, and engine preference consumers. Command-level preference caches remain intentional snapshots; no enabled error-preference tuple slot is exposed as an authoritative value. Local validation logs are retained under `artifacts/scoped-properties`.

## Five-preference expansion validation

Each of the five implementation commits built with the .NET 11 Windows SDK and passed its focused checks. Final focused xUnit coverage passed all 43 cases in each explicitly verified feature state. The selected preference, common-parameter, stream, variable, module, and debugger Pester suites passed 885 cases with the feature enabled and 868 with it disabled. The enabled run had five skips and nine pending cases; the disabled run had 22 skips and nine pending cases, including the 17 enabled-host scoped-property cases. Both metadata-registered scoped suites were included.

The regression fixtures now avoid using the reserved `$PS` name for PowerShell instances or executable paths. The legacy Ignore test runs in a separate runspace so removing its global preference does not change later tests' variable metadata. Module preference fixtures explicitly suppress their own verbose diagnostics while testing exported values, including `Stop`.

Two uninterrupted full xUnit attempts and an isolated socket-suite attempt encountered the previously observed `RemoteHyperV` hangs and were aborted after 90 seconds of test inactivity. Supplemental runs completed every case: the non-socket suite reported 451 passes, six skips, and the seven existing locale-dependent file-size failures; all 24 socket cases completed across the runs, with 23 passes and the existing Swedish-versus-English socket-message failure. Combined coverage was 474 passes, six skips, and eight known locale failures. This does not establish an uninterrupted full-suite pass. The socket tests and `test/xUnit/failed_loc.xml` were left unchanged.

The source audit found no remaining Error-only assumption in shared registration, storage, invocation frames, tuple reservation, or debugger enumeration. Stream-specific runtime policies and their differing cache behavior remain intact. Logs and XML results for this increment are under `artifacts/scoped-properties/expand-*`.
