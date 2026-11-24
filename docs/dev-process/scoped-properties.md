# Scoped properties prototype

Run `Enable-ExperimentalFeature PSScopedProperties` and start a new PowerShell process to activate the feature. If you have no enabled experimental features in your user configuration, you can also save the following as `scoped-properties.json` and start a prototype build with `pwsh -NoProfile -SettingsFile ./scoped-properties.json`. A nonempty list of experimental features in the user configuration takes precedence over the list provided by `-SettingsFile`.

```json
{"ExperimentalFeatures":["PSScopedProperties"]}
```

## Using scoped settings

The feature registers a constant, AllScope `$PS` variable. The typed `ActionPreference.Error` and `Style.Progress.View` properties inherit independently through PowerShell scopes. Assignment creates a local override, and a dot-sourced script runs in the caller's scope. Captured grouping objects resolve their properties against the active scope at each use.

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

`Reset()` takes the full path of either setting and removes its override from the target scope, subject to variable protections. Calling it with no override present has no effect. It works with literal and dynamically supplied method names.
Remove the local error preference with `Remove-Variable ErrorActionPreference -Scope Local` or `$PS.Reset('ActionPreference.Error')` to resume inheritance.

The console host uses the effective progress view from the latest update for its shared progress pane and may render in classic mode for terminals without the required capabilities.

The prototype covers these two engine-owned scalar settings. Public authoring APIs, mutable collections, other configuration variables, and deprecation configuration are deferred. Access to the facade requires an active PowerShell execution context and remains subject to constrained language restrictions.

## Baseline validation

Before the canonical storage changes, the build passed with the .NET 11 SDK. The published binaries passed all 19 existing scoped-properties Pester cases and all four xUnit cases in `ScopedPropertiesTests` and `ScopedProgressTests`.
The scoped Pester run used an explicit module path and temporary registry access for its test fixture. No failures were observed in these scoped test runs.

Selected variable, preference, error-handling, automatic-variable, and module Pester suites passed 336 cases with two pending in each feature-enabled and feature-disabled configuration. The disabled run kept the other generated experimental features enabled.
The isolated module fixture was also investigated separately. With `Test.Isolated.Init` built, loading it encountered a version conflict between the fixture's 3.0 assembly and the already bound assembly.

The full xUnit baseline encountered seven locale-sensitive `Utils.DisplayHumanReadableFileSize` failures and a refused localhost connection in `RemoteHyperV`, where the run hung.
An additional run after phase 2 excluded `RemoteHyperV` and reported 417 passes, the same seven locale failures, and six skips. This count includes the five new phase 2 tests.

## Canonical storage contract

When `PSScopedProperties` is enabled, `$ErrorActionPreference` and `$PSStyle.Progress.View` use the same typed entries as the corresponding `$PS` properties and engine consumers.
Both access paths convert and validate assignments before writing the target entry. The effective value comes from the nearest visible entry.
If no entry remains visible, the `$PS` properties and engine consumers use their descriptor defaults (`Continue` for error action and `Minimal` for progress view). A legacy variable lookup can then find no `$ErrorActionPreference`.
The preference variable adapter preserves variable protections and force rules, validation attributes, `Private` visibility, and explicitly configured `AllScope` sharing.
An explicit command-level `-ErrorAction` still takes precedence, including when a scope override has been reset, and existing command preference caching semantics still apply.
Entries and bound scope views belong to their originating runspace. The console host continues to use the effective progress view, and `PSStyle` retains its context-free fallback.

When the feature is disabled, `$PS` remains an ordinary variable name, `$ErrorActionPreference` follows the existing variable rules, and `$PSStyle.Progress.View` retains its existing behavior.
The canonical design is internal to the two prototype settings. `Hidden`, additional settings, and public setting registration are deferred.

Each error preference entry has a variable adapter that supplies the metadata and exposes the entry through `Value`. Explicit `AllScope` configuration shares the entry and its adapter across scope bindings.
A variable object returned by `Get-Variable` or `New-Variable -PassThru` tracks the live entry. After explicit removal of its last binding, the retained object can be edited independently and leaves the setting absent.

A dot-sourced advanced invocation with an explicit `-ErrorAction` uses a temporary preference binding. Reset removes the temporary override and reveals the caller's preference. Later writes in the same invocation stay temporary until it exits.
Dot-sourcing without that explicit common parameter writes the caller's scope in the usual way.

## Compatibility decisions

### Typed legacy error preference assignments

The built-in `$ErrorActionPreference` already converted assignments through its type converter before the cutover. A child could create an untyped shadow that retained raw values. The scoped getter interpreted that shadow or used its default.
All legacy writes now use the scoped property's conversion and validation rules before updating the entry.

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
Scripts that used an untyped child shadow to store a raw or invalid value will need to use an ordinary variable for that value.

The decision gives variable readers, the facade, and engine consumers the same type and value. Preserving raw shadows and converting on read would leave the two access paths with different results.
Keeping a raw value alongside the typed value would require defining two observable values for the same setting and synchronizing updates to both.

To reverse this decision, restore the ordinary variable lookup, creation and assignment paths in `SessionStateScope.TryGetVariable` and `SessionStateScope.SetVariable`. Restore the previous variable analysis and compiler treatment for the name.
Route scoped and engine reads through the legacy `ExecutionContext.GetEnumPreference` conversion and fallback, and preserve the built-in variable's `ArgumentTypeConverterAttribute` in `InitialSessionState`.
Review the `ScopedSettingVariable` conversion and raw write paths, `SetVariableCommand` and `ClearVariableCommand` force handling, and the variable provider so the reversal applies consistently.

`ScopedProperties.Tests.ps1` covers this decision in `uses typed conversion for the built-in preference and child-scope assignments`, `normalizes variable cmdlet writes and resumes inheritance after removal`, and `shares the canonical error preference with variable-provider item operations`.
The feature-disabled, forced-write, provider, reserved-value, attribute, and protection cases cover the compatibility boundaries.

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
`ImportAndReplacementPreserveMetadataAndRejectInvalidUpdatesAtomically` in `ScopedSettingVariableTests` covers the adapter boundary. The host initialization and session-state API case in `ScopedProperties.Tests.ps1` covers the public setter path.

Explicitly exported module variables continue to share their original object and metadata when imported into the same runspace. Before the cutover, importing a live module object into another runspace also shared the original variable. A write there could change the source module's preference.
An import into another runspace now copies the exported value and metadata into a separate adapter. `Remove-Module` removes the imported binding in either case. A still-bound source entry keeps its value and metadata under the same removal rule as `AllScope`.
Cross-runspace sharing would allow one runspace's commands to affect settings in another. To restore that behavior, remove the runspace check in `SessionStateScope.ImportVariable` and revert the corresponding import-provenance check used by `ModuleCmdletBase.RemoveModule`.
The `shares explicitly exported module preferences only within the same runspace` case in `ScopedProperties.Tests.ps1` verifies shared same-runspace writes, isolated foreign writes, and module removal on both paths.

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

To reverse this decision while keeping typed entries, track which entries represent an advanced function's implicit `-ErrorAction` in `SetPreferenceVariables()` in `CompiledScriptBlock.cs` and reinstate the removal restriction in `ScopedProperties.ResetErrorActionPreference` and `SessionStateScope.RemoveScopedSettingVariable`.
Restore the ordinary optimized-local distinction in `VariableAnalysis` to reinstate the old dynamic reset restriction. Restoring the shared-object clearing behavior would require changing when `ScopedSettingEntry.ReleaseBinding` detaches its adapter.

`ScopedProperties.Tests.ps1` covers reset in `resets implicit advanced-function preferences and keeps explicit command-level precedence`, `resets error overrides in optimized functions with and without a variable breakpoint`, and the dot-sourcing and `AllScope` cases.
The ordinary reset, bound-scope, and protected-variable cases cover inheritance, targeting, and removal protections.
