# Scoped properties prototype

Run `Enable-ExperimentalFeature PSScopedProperties` and start a new PowerShell process to activate the feature. If you have no enabled experimental features in your user configuration, you can also save the following as `scoped-properties.json` and start a prototype build with `pwsh -NoProfile -SettingsFile ./scoped-properties.json`. A nonempty list of experimental features in the user configuration takes precedence over the list provided by `-SettingsFile`.

```json
{"ExperimentalFeatures":["PSScopedProperties"]}
```

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
