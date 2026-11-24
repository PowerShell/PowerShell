# Scoped properties prototype

Run `Enable-ExperimentalFeature PSScopedProperties` and start a new PowerShell process to activate the feature. If you have no enabled experimental features in your user configuration, you can also save the following as `scoped-properties.json` and start a prototype build with `pwsh -NoProfile -SettingsFile ./scoped-properties.json`. A nonempty list of experimental features in the user configuration takes precedence over the list provided by `-SettingsFile`.

```json
{"ExperimentalFeatures":["PSScopedProperties"]}
```

## Current prototype behavior

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

`Reset()` takes the full path of either setting and removes its override from the target scope. Calling it with no override present has no effect. Error preference writes and resets use the legacy variable's assignment and removal rules. Reset can fail when the variable is read-only, constant, or stored in an optimized local variable slot, such as an advanced function's implicit preference from `-ErrorAction`.

Like `Remove-Variable`, a statically named `Reset()` call keeps the ordinary error preference removable. Dynamically invoked method names retain the existing optimized-variable removal restrictions.

`$ErrorActionPreference` and `$PSStyle.Progress.View` remain available. The typed Error property returns the effective preference, while the legacy variable can contain a raw string, null, or invalid value. The scoped progress view defaults to `Minimal`. The console host uses the effective view from the latest update for its shared progress pane and may render in classic mode for terminals without the required capabilities.

The prototype covers these two engine-owned scalar settings. Public authoring APIs, mutable collections, other configuration variables, and deprecation configuration are deferred. Access to the facade requires an active PowerShell execution context and remains subject to constrained language restrictions.

### Baseline validation

Before the canonical storage changes, the build passed with the .NET 11 SDK. The published binaries passed all 19 existing scoped-properties Pester cases and all four xUnit cases in `ScopedPropertiesTests` and `ScopedProgressTests`.
The Pester run used an explicit module path and temporary registry access for its test fixture. No product failures were observed.

## Canonical storage contract

The planned cutover keeps the public `$PS` API, `PSScopedProperties` feature gate, and scope targeting rules. Each setting has a declared type and default.
Its effective value comes from the nearest visible scope entry, or from the declared default (`Continue` for error action and `Minimal` for progress view) if no entry is visible.
An assignment to either access path converts and validates before writing the target scope's entry. `Reset()` removes the target scope's entry when present, subject to variable protections, so the setting resumes inheritance.

`$ErrorActionPreference` and `$PSStyle.Progress.View` will use the same entries as the corresponding `$PS` properties and engine consumers.
The preference variable adapter preserves variable protections and force rules, validation attributes, `Private` visibility, and explicitly configured `AllScope` sharing.
An explicit command-level `-ErrorAction` still takes precedence, including when a scope override has been reset, and existing command preference caching semantics still apply.
Entries and bound scope views belong to their originating runspace. The console host continues to use the effective progress view, and `PSStyle` retains its context-free fallback.

When the feature is disabled, `$PS` remains an ordinary variable name, `$ErrorActionPreference` follows the existing variable rules, and `$PSStyle.Progress.View` retains its existing behavior.
The canonical design is internal to the two prototype settings. `Hidden`, additional settings, and public setting registration are deferred.

## Compatibility decisions

### Typed legacy error preference assignments

The built-in `$ErrorActionPreference` currently converts assignments through its type converter. A child can create an untyped shadow that retains raw values.
The scoped getter can then interpret those values or return its default even though a legacy read shows something else.
After the cutover, all legacy writes to the setting will use the scoped property's conversion and validation rules before updating the entry.

```powershell
$ErrorActionPreference = 'sToP'       # Current and planned: ActionPreference Stop
& {
    $ErrorActionPreference = 'cOnTiNuE'
    $ErrorActionPreference            # Current: String cOnTiNuE. Planned: ActionPreference Continue.

    $ErrorActionPreference = 1
    $ErrorActionPreference            # Current: Int32 1. Planned: ActionPreference Stop.

    try { $ErrorActionPreference = $null } catch { 'null rejected' }
    $ErrorActionPreference            # Current: null. Planned: Stop after a failed conversion.

    try { $ErrorActionPreference = 'invalid preference' } catch { 'invalid value rejected' }
    $ErrorActionPreference            # Current: String invalid preference. Planned: Stop after a failed conversion.
}
```

Defined numeric values convert to the corresponding enum member. Null, undefined numbers, invalid names, and the reserved `Suspend` value are rejected.
Failed conversion or validation preserves the previous override, or leaves inheritance in effect if there was no override.
Scripts that used an untyped child shadow to store a raw or invalid value will need to use an ordinary variable for that value.

The decision gives variable readers, the facade, and engine consumers the same type and value. Preserving raw shadows and converting on read would leave the two access paths with different results.
Keeping a raw value alongside the typed value would require defining two observable values for the same setting and synchronizing updates to both.

To reverse this decision, revisit the legacy-name routing and adapter write conversion in session state, as well as the compiler path that routes variable assignments away from independent optimized slots.
Restore raw shadow creation and the lookup conversion and fallback currently implemented by `ExecutionContext.GetEnumPreference` and `ScopedProperties.Error`.
Restore the built-in variable's `ArgumentTypeConverterAttribute` from `InitialSessionState` for its existing typed behavior.
Review `Set-Variable`, provider writes, and retained `PSVariable` writes alongside direct assignment so the reversal applies consistently.

The cutover changes `converts the built-in preference but permits untyped child-scope shadows` and `shares legacy variable operations and preserves raw legacy values` in `ScopedProperties.Tests.ps1`.
The feature-disabled counterpart and the tests for reserved `Suspend`, validation attributes, and variable protection continue to cover the compatibility boundaries.

### Uniform reset

An advanced function currently receives an implicit `$ErrorActionPreference` slot when called with `-ErrorAction`. The prototype reads and writes that slot, but `Reset()` cannot remove it.
A dynamically invoked `Reset()` can also fail on optimized ordinary local variables. After the cutover, an implicit function preference and an ordinary local assignment use removable scope entries, regardless of how `Reset()` is invoked.

```powershell
$PS.ActionPreference.Error = 'Continue'
function Show-Preference {
    [CmdletBinding()]
    param()

    $PS.ActionPreference.Error        # Current and planned: Stop.
    try { $PS.Reset('ActionPreference.Error') } catch { 'reset failed' }
    $PS.ActionPreference.Error        # Current: Stop after the failure. Planned: Continue.
    Write-Error 'expected' -ErrorAction Ignore
}
Show-Preference -ErrorAction Stop

function Show-DynamicReset {
    $ErrorActionPreference = 'Stop'
    $method = 'Reset'
    try { $PS.$method('ActionPreference.Error') } catch { 'reset failed' }
    $PS.ActionPreference.Error        # Current without a breakpoint: Stop after the failure. Planned: Continue.
}
Show-DynamicReset
```

Removing the override lets nested commands use the inherited preference unless they specify their own `-ErrorAction`.
An alternative was to preserve the implicit preference for the lifetime of the advanced function. That would make the same reset call behave differently based on how the preference entered the scope.
Another alternative was to write the default into the target scope. That would keep overriding the parent when it has a different preference.

To reverse this decision while keeping typed entries, track which entries represent an advanced function's implicit `-ErrorAction` and reinstate its removal restriction in the error setting's override removal path.
The current behavior originates in `SetPreferenceVariables()` in `CompiledScriptBlock.cs`, its `MutableTuple` preference slot, and the error removal delegate in `ScopedProperties.Error`.
Restoring the old dynamic reset restriction would also require restoring the ordinary optimized-local distinction that `VariableAnalysis` currently uses for its statically named `Reset()` exception.
Review both reset and legacy removal so the two paths apply the same removal rules.

The cutover changes `observes advanced function preferences and command-level ErrorAction precedence` and `honors variable removal rules when Reset is invoked dynamically` in `ScopedProperties.Tests.ps1`.
Existing tests for ordinary resets, bound scopes, and protected variables continue to cover inheritance, targeting, and removal protections.
