# Scoped properties prototype

This document describes the implemented experimental feature. The [expansion plan](scoped-properties-expansion-plan.md) records the design and staged delivery sequence.

Run `Enable-ExperimentalFeature PSScopedProperties` and start a new PowerShell process to activate the feature. If you have no enabled experimental features in your user configuration, you can also save the following as `scoped-properties.json` and start a prototype build with `pwsh -NoProfile -SettingsFile ./scoped-properties.json`. A nonempty list of experimental features in the user configuration takes precedence over the list provided by `-SettingsFile`.

```json
{"ExperimentalFeatures":["PSScopedProperties"]}
```

## Using scoped settings

The feature registers a constant, AllScope `$PS` variable. Its writable preference properties share typed values with their corresponding legacy variables. Action preferences use `ActionPreference`; ShouldProcess settings use the types shown below.

| Facade property | Legacy variable | Default |
| --- | --- | --- |
| `$PS.ActionPreference.Error` | `$ErrorActionPreference` | `Continue` |
| `$PS.ActionPreference.Warning` | `$WarningPreference` | `Continue` |
| `$PS.ActionPreference.Information` | `$InformationPreference` | `SilentlyContinue` |
| `$PS.ActionPreference.Verbose` | `$VerbosePreference` | `SilentlyContinue` |
| `$PS.ActionPreference.Debug` | `$DebugPreference` | `SilentlyContinue` |
| `$PS.ActionPreference.Progress` | `$ProgressPreference` | `Continue` |
| `$PS.ShouldProcess.Confirm` | `$ConfirmPreference` | `High` (`ConfirmImpact`) |
| `$PS.ShouldProcess.WhatIf` | `$WhatIfPreference` | `false` (`Boolean`) |

Each of these eight preferences and `$PS.Style.Progress.View` inherits independently through PowerShell scopes. Assignment in a scope without a binding creates a local override. Captured grouping objects resolve their properties against the active scope at each use.

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

The prototype covers six action preferences, Confirm, WhatIf, scalar and mutable configuration, the complete style surface, and live runtime views. Access to the facade requires an active PowerShell execution context and remains subject to constrained language restrictions.

### Scalar configuration

These properties use the same inheritance, assignment, and reset rules. Paths
are relative to `$PS`; reads of an absent legacy binding do not create one.

| Path | Legacy variable | Default or fallback |
| --- | --- | --- |
| `Formatting.ErrorView` | `ErrorView` | `ConciseView` |
| `Formatting.EnumerationLimit` | `FormatEnumerationLimit` | 4 |
| `Formatting.FieldSeparator` | `OFS` | Space; assigned objects retain ordinary references |
| `NativeCommand.Encoding.ToProcess` | `OutputEncoding` | `Encoding.Default`, subject to existing host initialization |
| `NativeCommand.Encoding.FromProcess` | `PSApplicationOutputEncoding` | Null; native reader falls back to console output encoding |
| `NativeCommand.ArgumentPassing` | `PSNativeCommandArgumentPassing` | Windows on Windows, Standard elsewhere |
| `NativeCommand.UseErrorActionPreference` | `PSNativeCommandUseErrorActionPreference` | False |
| `Module.AutoLoadingPreference` | `PSModuleAutoLoadingPreference` | Environment preference, otherwise All |
| `History.MaximumCount` | `MaximumHistoryCount` | 4096, valid range 1–32767 |
| `Transcription.Path` | `TRANSCRIPT` | Null; transcript command generates its normal filename |
| `Mail.Server` | `PSEmailServer` | Empty string |
| `Help.VerboseErrors` | `VerboseHelpErrors` | False |
| `Logging.EngineHealthEvent`, `.EngineLifecycleEvent` | Corresponding `Log*Event` variables | True |
| `Logging.CommandHealthEvent`, `.CommandLifecycleEvent` | Corresponding `Log*Event` variables | False |
| `Logging.ProviderHealthEvent`, `.ProviderLifecycleEvent`, `.SettingsEvent` | Corresponding `Log*Event` variables | True |
| `Logging.UserData` | `PSLogUserData` | Null; assigned objects retain ordinary references |
| `Remoting.ConfigurationName` | `PSSessionConfigurationName` | `http://schemas.microsoft.com/powershell/Microsoft.PowerShell` |
| `Remoting.ApplicationName` | `PSSessionApplicationName` | `wsman` |

Nullable logging and module preferences preserve explicitly assigned null.
Their consumers apply the existing event default or environment fallback.
Nullable string settings accept null without turning it into an empty string.
The native error preference normalizes assignments to Boolean; untyped legacy
values such as `'Yeah'` and `0` become true and false respectively.

Formatting, module discovery, transcription, and remoting-name consumers now
resolve the caller's effective setting when enabled. Explicit remoting and
transcription parameters still take precedence. Native encoding readers,
history, logging, and help use the shared unqualified context lookup; their
existing caching and read timing remain intact. Explicitly qualified variable
lookups retain their scope. The `PSApplicationPrivateData` consumer retains
its global, startup-only boundary as described below.

The final consumer audit confirmed these routes:

| Consumer | Enabled resolution and retained timing |
| --- | --- |
| General scalar readers | `ExecutionContext.GetVariableValue` and its default-value overload route registered, unqualified names through the current scope's descriptor. Explicitly qualified lookups retain their target. `GetBooleanPreference` also uses typed descriptors. |
| Formatting and stringification | `InnerFormatShapeCommand.FormatEnumerationLimit` uses the scoped descriptor before its legacy global fallback. `MshObject` resolves `OFS` through the context lookup. |
| Native processes | `NativeCommandProcessor` obtains input/output encodings and native-error policy through the context hooks; `NativeCommandParameterBinder` resolves argument-passing policy there too. Existing console fallbacks remain. |
| Module discovery, transcription, remoting names | Their enabled branches use unqualified context lookups; disabled branches retain legacy global behavior. Explicit command parameters still win. |
| History, help, mail, logging | Existing unqualified variable/context reads reach canonical settings. History limits, nullable logging defaults, and consumer cache lifetimes remain intact. |
| Command defaults and session options | The pipeline captures the caller's dictionary before entering command frames. Remoting cmdlets retain their per-invocation options snapshot. |
| Style and startup private data | Engine style access uses the scoped backing when a context exists and host fallback fields otherwise. Application private data deliberately remains a global startup-only consumer. |

### Mutable configuration

`Command.DefaultParameterValues`, `Remoting.SessionOptions`, and
`Remoting.ApplicationPrivateData` share settings with `PSDefaultParameterValues`,
`PSSessionOption`, and `PSApplicationPrivateData`. Both access paths expose
scope-aware objects. A successful property or dictionary mutation creates the
target scope's override; a failed mutation publishes nothing. Reset removes
that override. Dictionary entries and built-in option properties are isolated,
while arbitrary objects stored in those entries retain normal reference
semantics. Session options also isolate entries in their `ApplicationArguments`
dictionary. Explicit null remains null.

The public types remain `DefaultParameterDictionary`, `PSSessionOption`, and
`PSStyle`. Standalone instances retain ordinary behavior. Captured unbound
views resolve the caller on every use, including a different runspace; they
do not carry a captured scope's data there. Bound views target their selected
scope, reject foreign runspaces, and expire when that scope ends. The parameter
binder captures the caller's effective dictionary before creating command or
dotted invocation frames. Qualified legacy variable expressions target their
named scope. `PSVariable.Value` and `Get-Variable -ValueOnly` return the actual
represented binding's value, so a retained global variable object cannot start
reading a child's override. A retained variable object from an exited scope
keeps its value; explicitly removing its final binding clears it. ReadOnly and
Constant protect binding replacement while allowing object member mutations.

`$PS.Style` and `$PSStyle` expose the complete existing `PSStyle` surface:
formatting and file styles, extension mappings, progress settings, output
rendering, automatic sizing, colors, and ANSI helpers. Mutable leaves inherit
independently. The ANSI `Style.Reset` string remains distinct from the
facade's `Reset(path)` method. File-extension mutations copy the effective
extension dictionary, leaving other style settings alone.

| Style surface | Members retained from `PSStyle` |
| --- | --- |
| Rendering | `OutputRendering`, `AutoSizeDefaultFormatting` |
| `Progress` | `View`, `Style`, `MaxWidth`, `UseOSCIndicator` |
| `Formatting` | `FormatAccent`, `TableHeader`, `CustomTableHeaderLabel`, `ErrorAccent`, `Error`, `Warning`, `Verbose`, `Debug`, `FeedbackName`, `FeedbackText`, `FeedbackAction` |
| `FileInfo` | `Directory`, `SymbolicLink`, `Executable`, and `Extension` with its indexer, `Add`, `Remove`, `Clear`, `ContainsKey`, and `Keys` |
| `Foreground`, `Background` | Black, Red, Green, Yellow, Blue, Magenta, Cyan, White and their Bright variants; both `FromRgb` overloads and `FromConsoleColor` |
| ANSI attributes | `Reset`; Blink, Bold, Dim, Hidden, Reverse, Italic, Underline, Strikethrough and their Off variants |
| Hyperlinks and static helpers | `FormatHyperlink`; the existing static color-mapping methods remain on the `PSStyle` type |

Application private data preserves delayed conversion: arbitrary values may
be assigned, and the server converts the global value to `PSPrimitiveDictionary`
after a startup script only while the pool is `Opening`. A child override is
not consumed at that boundary. The published dictionary is a snapshot rather
than a live scoped proxy; later configuration changes do not republish it.

### Runtime views

`Invocation`, `Diagnostics`, `Session`, `Runtime`, `Event`, and `Constants`
expose live runtime state. `History.FirstToken` and `.LastToken`,
`Remoting.SenderInfo`, and the top-level `LastStatementSuccessful` and
`LastExitCode` complete the runtime surface. These descriptors are separate
from configuration: they supply no defaults, reject reset, and return ordinary
object references without copy-on-write. Setters follow the underlying
variable's permissions, conversion, visibility, and debugger checks.

Invocation getters follow active dotted and local tuples, then legacy parent
fallback. Captured unbound containers resolve the current caller; bound views
retain their selected scope and its existing lifetime checks. A missing value
returns null outside strict mode, while explicitly assigned null remains
present. Strict mode 1 permits missing values inside interpolated strings;
strict mode 2 reports `VariableIsUndefined`. The compiler handles that narrow
absence signal without suppressing other property or scope errors. Container
display does not enumerate runtime values or consume input.

Runtime writes target the selected scope. In particular, a local `LASTEXITCODE`
or `PWD` assignment can shadow the producer's global value. Culture properties
return the live legacy culture-name strings. `$?` reads the existing success
flag, separately from the initially absent native exit code. Event values and
event-local error lists persist within their subscription. Debugger and nested
prompt views follow their producers' creation and cleanup boundaries.

## Runtime map: legacy source, producer, presence, owner, writes

The `ScopedRuntimeProperties` descriptor registry maps these paths; non-automatic entries resolve with `SessionStateInternal.GetVariableItem`, automatic tuple entries use `SessionStateScope.GetAutomaticVariableValue`, and `?` reads `ExecutionContext.QuestionMarkVariableValue`. `RuntimePropertyResolvers.Write` targets the facade's selected scope and calls `SessionStateScope.SetVariable`; it does not implement configuration inheritance/COW. The “initial/presence” column describes the legacy producer; a facade read never invents a runtime value to fill an absent slot.

| Facade path | Legacy variable / producer and presence | Owner/lifetime and write implications |
|---|---|---|
| `LastStatementSuccessful` | `$?`; always present from `ExecutionContext.QuestionMarkVariableValue` | Current execution context; read-only facade. It is not `$LASTEXITCODE`. |
| `LastExitCode` | `$LASTEXITCODE`; not initialized to zero; first native exit creates it | Legacy global producer/variable ownership. Writable through facade with normal variable setter rules; absence remains absent until a producer/assignment creates it. |
| `Invocation.Arguments`, `.Input`, `.This`, `.Cmdlet`, `.BoundParameters`, `.Info`, `.ScriptRoot`, `.CommandPath` | `args`, `input`, `this`, `PSCmdlet`, `PSBoundParameters`, `MyInvocation`, `PSScriptRoot`, `PSCommandPath`; automatic tuple indices Args=1, This=2, Input=3, PSCmdlet=4, PSBoundParameters=5, MyInvocation=6, PSScriptRoot=7, PSCommandPath=8 | Resolve the active dotted invocation tuple first and then the scope tuple/fallback. Nested invocations can have distinct tuples inside one scope. Input returns the active enumerator/reference and must not be consumed by read/display. `Invocation.ScriptRoot` and `.CommandPath` describe the current script. `Invocation.Info.PSScriptRoot` and `.PSCommandPath` are caller fields from `MyInvocation`; they are intentionally different. Existing automatic-variable tuple setters remain writable where legacy assignment permits. |
| `Invocation.ForEachEnumerator`, `.SwitchEnumerator`, `.Matches` | `foreach`, `switch`, `Matches`; loop variables are producer-managed; `$Matches` is retained from last successful match and not cleared by a later nonmatch | Current loop/script runtime; ordinary value/reference semantics. Reads do not call MoveNext. Writes use ordinary selected-scope variable setter, not configuration COW. |
| `Diagnostics.Errors` | `$Error`; global error history normally, event action script-scope persistent list while executing an event action | Error list is a mutable reference; facade does not copy it. Event `$Error` is separate from global `$Error` and persists with event subscription/action script state. Variable replacement follows normal setter/permissions; mutating returned list is ordinary shared mutation. |
| `Diagnostics.StackTrace` | `$StackTrace`; initialized null in initial session state | Runspace/session variable; null is present, not absent. Normally mutable variable. |
| `Diagnostics.DebugContext` | `$PSDebugContext`; debugger sets on stop and removes in resume cleanup | Debugger-stop scope/lifetime; absent outside a stop unless user creates a binding. Strict handling distinguishes absent from present null. |
| `History.FirstToken`, `.LastToken` | `^` and `$$` (`$`/`SpecialVariables.LastToken` descriptor); initialized null in initial session state | History/interactive token producer. Null is present; both remain ordinary variable writes under legacy permissions. |
| `Session.Host` | `$Host`; initialized from engine host | Runspace host object; Constant/AllScope variable: replacement rejected, object itself remains the legacy reference. |
| `Session.ExecutionContext` | `$ExecutionContext`; initialized as `EngineIntrinsics` wrapper | Runspace engine intrinsics; Constant/AllScope replacement rejected. |
| `Session.Home` | `$HOME`; initialized from home environment | ReadOnly/AllScope string; replacement rejected. |
| `Session.CurrentLocation` | `$PWD`; location API sets it to `CurrentLocation`, usually global path | Runspace-owned current location plus legacy variable path. The facade setter follows the legacy variable-assignment route for `$PWD` (scope `SetVariable`); it does not call `Set-Location` or independently fire a location-change event. This preserves the variable-path setter contract rather than making the property a second location API. |
| `Session.Profile` | `$PROFILE`; optional, normally host-created | Host/script state; no default is synthesized. Missing remains missing (strict-mode behavior applies). |
| `Session.NestedPromptLevel` | `$NestedPromptLevel`; initialized to zero and changed by nested-prompt producer | Runspace/session state; mutable legacy variable. |
| `Session.CurrentlyExecutingCommand` | `$CurrentlyExecutingCommand`; optional prompt producer | Nested prompt saves/restores this state. Existing producer may write null on exit even when no value was initially present, so a post-prompt present-null binding can be observable. Do not claim exit always restores absence. |
| `Runtime.ProcessId` | `$PID`; initialized as constant/all-scope process id | Process identity; Constant/AllScope replacement rejected. |
| `Runtime.InstallationDirectory` | `$PSHOME`; initialized constant/all-scope | Installation identity; replacement rejected. |
| `Runtime.ShellId` | `$ShellId`; initialized constant/all-scope | Runspace/shell identity; replacement rejected. |
| `Runtime.VersionTable` | `$PSVersionTable`; initialized constant/all-scope `PSVersionHashTable` | Replacement rejected; returned hashtable remains mutable by ordinary reference semantics. No COW. |
| `Runtime.Edition` | `$PSEdition`; initialized constant/all-scope | Replacement rejected. |
| `Runtime.EnabledExperimentalFeatures` | `$EnabledExperimentalFeatures`; initialized constant/all-scope read-only bag | Replacement rejected; actual exposed bag follows legacy type/reference behavior. |
| `Runtime.Culture`, `.UICulture` | `$PSCulture`, `$PSUICulture`; specialized `CultureVariable` getters return current thread culture names | Live runspace execution thread, not a copied `CultureInfo` or process-global default. Variables are ReadOnly/AllScope; replacement rejected. |
| `Runtime.IsWindows`, `.IsLinux`, `.IsMacOS`, `.IsCoreCLR` | Corresponding initialized read-only/all-scope variables | Process/runtime identity, read-only variable writes rejected. |
| `Event.Record`, `.Subscriber`, `.Arguments`, `.Sender` | `Event`, `EventSubscriber`, `EventArgs`, `Sender`; `EventManager.PSEventJob` populates action session state | Per event action/subscription script state. Values persist between callbacks and are not global or automatically cleared at callback return. Existing producer types and references are returned; writes follow ordinary variable setter. |
| `Remoting.SenderInfo` | `$PSSenderInfo`; optional remoting-owned variable | Remoting/session lifetime; absent outside remoting unless user creates it. No facade-created default. |
| `Constants.True`, `.False`, `.Null` | `$true`, `$false`, `$null` | Language constants; read-only facade. `$null` is special/present-null. The underlying constant objects and option semantics remain engine-defined. |

### Class, strict-mode, and visibility boundaries

The runtime facade does not bypass parser or language restrictions. Existing class rules prohibit unqualified `$PS` variable access in class bodies, and the feature parser rejects qualified `$script:PS`; code needing runtime facade access in a class should receive the facade as an explicit parameter. Module/private visibility is resolved using the normal session-state variable lookup and caller origin. Missing variable behavior remains legacy-compatible in ordinary reads and strict modes. In expandable strings, only the new sentinel representing an absent runtime backing is softened for strict mode 1; strict mode 2 still throws. A present `$null` is not converted into absence.

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

Ordinary and advanced functions bind parameters named after a registered legacy variable as typed preferences. Advanced functions initialize invocation-owned values from explicit `-ErrorAction`, `-WarningAction`, `-InformationAction`, and `-ProgressAction` arguments. Explicit `-Verbose` or `-Debug` supplies `Continue`, and an explicit false supplies `SilentlyContinue`. Explicit `-Confirm` supplies `Low`, and `-Confirm:$false` supplies `None`; `-WhatIf` supplies its Boolean value. Omitting a common parameter leaves the preference to the named parameter or normal scope inheritance. When both parameters supply the same preference, the explicit common parameter takes precedence.

With the feature enabled, the invocation's `$WhatIfPreference` is a Boolean even when supplied by a common parameter whose argument is a `SwitchParameter`. Its true/false value is preserved, including explicit false. The disabled feature retains the legacy `SwitchParameter` invocation binding. Confirm assignments reject undefined enum values before changing the override.

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

`ScopedProperties.Tests.ps1` parameterizes the typed conversion, variable cmdlet, and variable provider cases over the registered action preferences.
The feature-disabled, forced-write, provider-content, host initialization, attribute, and protection cases cover the compatibility boundaries.
`ActionPreference.Tests.ps1` covers legacy `Suspend` handling. `ScopedProperties.Enabled.Tests.ps1` covers assignment-time rejection of enum and string `Suspend` values for all six scoped action preferences.

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

## Historical expansion feasibility inventory (stage zero)

This historical snapshot records the starting architecture before expansion.
Its prospective wording describes the work identified at stage zero, rather
than outstanding implementation tasks. The implemented mapping above and the
staged validation below supersede those feasibility caveats.

### Configuration paths

| Proposed path(s) | Existing producer, consumer, and default/presence behavior | Ownership and mutation boundary |
| --- | --- | --- |
| `ActionPreference.Progress`; `ShouldProcess.Confirm`, `.WhatIf` | `InitialSessionState.cs` registers `$ProgressPreference` (`Continue`), `$ConfirmPreference` (`High`), and `$WhatIfPreference` (`false`). `ExecutionContext.GetEnumPreference` / `GetBooleanPreference` resolve them; `MshCommandRuntime.ProgressPreference`, `.ConfirmPreference`, and `.WhatIf` cache effective values and account for common parameters. `PSScriptCmdlet.SetPreferenceVariables` writes invocation common-parameter slots in `runtime/CompiledScriptBlock.cs`. | Typed session-state preferences with invocation-specific common-parameter precedence and command cache lifetimes. Existing parameter binding and caches must keep working. The current scoped setting registry is typed to `ScopedActionPreferenceSetting`; generic descriptors need differing value types, defaults, tuple indices, and parameter-binding behavior. |
| `Formatting.ErrorView`, `.EnumerationLimit`, `.FieldSeparator` | `InitialSessionState.cs` registers `$ErrorView` (`ConciseView`) and `$FormatEnumerationLimit` (`4`). `ErrorPackage.cs` selects error rendering from `ErrorView`. `BaseFormattingCommand.InnerFormatShapeCommand.FormatEnumerationLimit` explicitly reads `global:FormatEnumerationLimit`, then falls back to `DefaultFormatEnumerationLimit`. `$OFS` is not in the built-in variable table: `MshObject.GetSeparator` resolves it at stringification time and falls back to a single space when absent or null. | Session-state settings with existing validation/conversion; enumeration limit's explicit global lookup and consumer read point need review before scoping. `$OFS` must preserve its absent/null fallback and must be read at stringification time. |
| `NativeCommand.Encoding.ToProcess`, `.FromProcess` | `$OutputEncoding` is registered by `InitialSessionState.cs` as `Encoding.Default`; startup scripts in `InitialSessionState.cs` may set it to `Console.OutputEncoding`. `NativeCommandProcessor.Start` reads it in the current command context for stdin and falls back to `Encoding.Default`. `$PSApplicationOutputEncoding` is registered as null; `NativeCommandProcessor.GetOutputEncoding` reads it for stdout/stderr and falls back to `Console.OutputEncoding`. | Encoding object references belong to session state; preserve the distinct per-command read and the separate fallback behavior in both directions. |
| `NativeCommand.ArgumentPassing`, `.UseErrorActionPreference` | `InitialSessionState.cs` registers `$PSNativeCommandArgumentPassing` with `GetPassingStyle()` (`Standard` on Unix, `Windows` otherwise) and `$PSNativeCommandUseErrorActionPreference` as false. `NativeCommandParameterBinder` reads argument passing with a `Standard` fallback; `NativeCommandProcessor` uses the error-action preference and emits telemetry for unset versus set values. | Typed mutable session-state settings; argument style has platform-dependent default. Preserve the native exit/error policy and its relation to `$?` without deriving either from the other. |
| `Command.DefaultParameterValues` | `SessionState.cs` creates `$PSDefaultParameterValues` as a `DefaultParameterDictionary`. `pipeline.cs` reads the dictionary before command scopes are entered; `CmdletParameterBinderController.cs` applies entries and may remove one-shot entries. | Mutable dictionary, created in runspace session state. Scoped backing must account for mutations and preserve the pipeline's pre-command snapshot/read boundary; ordinary contained object references keep their normal semantics. |
| `Module.AutoLoadingPreference` | `SpecialVariables.PSModuleAutoLoadingPreferenceVarPath` is explicitly `global:`. `CommandDiscovery.GetCommandDiscoveryPreference` checks that variable, then the `PSModuleAutoLoadingPreference` environment variable, then defaults to `All`; invalid conversion also falls back to `All`. Consumers include command discovery, module utilities, and session-state drive APIs. | Existing global setting plus environment fallback. The scoped feature may change the variable lookup boundary when enabled, but must preserve fallback order and disabled behavior. This is an explicit consumer migration task, not a preinstalled ordinary local variable. |
| `Remoting.SessionOptions`, `.ConfigurationName`, `.ApplicationName`, `.ApplicationPrivateData` | `InitialSessionState.cs` registers `$PSSessionOption` (new `PSSessionOption`), global-qualified `$PSSessionConfigurationName` (`http://schemas.microsoft.com/powershell/Microsoft.PowerShell`), and global-qualified `$PSSessionApplicationName` (`wsman`). `PSRemotingCmdlet` reads configuration and application names from those global paths with remoting defaults and converts session options. Server startup reads global `$PSApplicationPrivateData` in `ServerRunspacePoolDriver.InvokeStartupScripts` after scripts, while the pool opens. | Session options are mutable objects. Configuration/application names have legacy global lookup paths. Private data is optional startup data converted to `PSPrimitiveDictionary`; preserve its global, startup-only consumer and established-session stability. |
| `History.MaximumCount` | `History` creates `$MaximumHistoryCount` per runspace with `ValidateRange(1, Int16.MaxValue)` and default `4096`; `History.GetHistorySize` converts the live value and falls back to `4096` when missing, invalid, or nonpositive. | History/runspace-owned setting, read when history capacity is refreshed. Keep the variable validation and fallback distinct from the constant initial buffer allocation. |
| `Transcription.Path` | `StartTranscriptCmdlet` reads `global:TRANSCRIPT`. If absent, the host generates its default transcript path under `$HOME`. No default variable value is installed here. | Optional host/global value. Preserve absence and the host-generated path fallback; the transcript path is consumed by host transcript startup. |
| `Mail.Server`; `Help.VerboseErrors` | `InitialSessionState.cs` installs `$PSEmailServer` as an empty string. `HelpSystem` reads `$VerboseHelpErrors` with false fallback, and help error tracing consumes that property; no corresponding built-in variable entry is registered in the shown built-in table. | Mail value is session state; help preference is optional session state with false default at the consumer. Preserve help's lazy read and fallback. |
| `Logging.*` and `Logging.UserData` | `MshLog.NeedToLog*` reads the event switches with per-consumer defaults: engine health/lifecycle, provider health/lifecycle, and settings use true; command health/lifecycle use false. `LogProvider.GetPSLogUserData` reads optional `$PSLogUserData` and formats it for provider output. The switches/user data are not registered in the built-in variable table. | Session-state reads at logging time, with defaults supplied by the consumer rather than stored bindings. Preserve provider gating, per-event defaults, and optional user-data conversion. |
| Remaining mapped settings and runtime properties | The source inventory above is a feasibility snapshot, not a claim that every proposed member has completed producer/consumer tracing. The remaining inventory must reconcile each row in `scoped-properties-expansion-plan.md`, including any runtime member without a concrete producer cited above, against its variable initialization, fallback, consumer, and tests. `SpecialVariables.cs` gives canonical names and some qualified paths; it does not by itself prove registration, owner, default, consumer timing, or write rules. | Do not infer a universal owner or default from a name in `SpecialVariables`; complete this reconciliation before adding descriptors for the untraced paths. |

The generic storage substrate already exists in `ScopedSetting<T>` and
`ScopedSettingEntry<T>` in `ScopedProperties.cs`; the registry, legacy-name
index, command parameter binding, and preference tuple setup are not yet
generic. Relevant hooks are `ScopedProperties.TryGetPreferenceSetting` and
`TryGetSetting`, `SessionStateScope` variable get/set/remove and scoped-entry
methods, `ExecutionContext.GetEnumPreference`, and
`PSScriptCmdlet.SetPreferenceVariables` / `TryBindScopedSettingParameter`.
For the scalar stage, a bounded change is to generalize descriptors and
registry lookup to typed settings with optional legacy variable path, default,
validation, common-parameter binding, and tuple metadata; then route only
Progress/Confirm/WhatIf through those descriptors while preserving their
existing consumers and command-level cache behavior.

### Runtime paths and producer boundaries

| Proposed runtime group | Source producer / resolver | Boundary that a facade must preserve |
| --- | --- | --- |
| Invocation arguments, input, `this`, `PSCmdlet`, bound parameters, invocation info, and script paths | `CompiledScriptBlock.cs` initializes automatic-variable tuple slots at invocation setup and command processing (`Input`, `This`, `Args`, `MyInvocation`, `PSBoundParameters`, `PSCmdlet`, and script paths). `MshMemberInfo.InvokeGetter` / `.InvokeSetter` and `lang/scriptblock.cs` supply script-property/method `this` contexts. `SessionStateScope.GetAutomaticVariableValue` selects active dotted frames before the current locals tuple; `SessionStateVariableAPIs.GetAutomaticVariableValue` walks parent scopes. The compiler emits automatic reads through `GetAutomaticVariable` and `VariableOps.GetAutomaticVariableValue`. | These values belong to a caller invocation frame, not merely a lexical scope. Dotted scripts, nested invocations, modules, and class/script contexts can have distinct tuples. Resolve on each property access; do not capture a completed tuple or synthesize values. `Input` is a live enumerator and reads/display must not consume it. Legacy runtime setters remain where their variables allow them. |
| `foreach`, `switch`, and `$Matches` | `Compiler.GenerateIteratorStatement` and variable compiler paths save, install, and restore iterator automatic variables; regex operators and switch execution produce `$Matches` in compiler/runtime operations. The compiler also treats automatic-variable slots specially when enumerating variable names. | Preserve nested loop save/restore on exceptions and early exits. `$Matches` is persistent after a successful match and is not cleared by a later nonmatch. Distinguish unset from assigned null. |
| Diagnostics: `$Error`, stack trace, debugger context | `$Error` is backed by `ExecutionContext.DollarErrorVariable`; debugger stop installs `$PSDebugContext` in `debugger/debugger.cs` and removes it on resume. Event action error storage is additionally initialized by `EventManager.cs` (`CreateBoundScriptBlock` / `PSEventJob.Invoke`). | `$Error` is a mutable reference and is not copy-on-write. Event action error history is a script-scope persistent list per subscription, distinct from global error history. Debug context exists only during a stop and is removed in cleanup; non-strict null does not imply stored presence. |
| Events and remoting sender information | `EventManager.PSEventJob.Invoke` populates event action variables; `CreateBoundScriptBlock` creates the event script scope and its persistent `$Error` list. `Get-Command` reads `$PSSenderInfo` in `GetCommandCommand.cs`; `PSRemotingCmdlet` and remoting command paths own sender data. | Event values are session/action-owned, persist between callbacks for that action, and are not global or automatically cleared at callback return. Preserve event values and sender data as ordinary references; preserve absence when no producer exists. |
| Session and host: host, execution context, home, current location, profile, nested prompt, current command | Host/session variables are installed by runspace initialization. `InternalHost.cs` saves/restores `$CurrentlyExecutingCommand` and `$PSBoundParameters` around nested prompt entry/exit. `$PWD`/current location are backed by `SessionState.Path.CurrentLocation`; culture and UI culture come from the live runspace thread. `PROFILE` is supplied by host startup, not an unconditional engine value. | Current location is runspace-owned, not process-global. Culture remains live thread state. Nested prompt values must be restored through nested entries. Optional profile and nested-prompt values remain absent when no producer supplies them. |
| Runtime identity, version, platform, and language constants | Platform booleans and `$NestedPromptLevel` are installed in the `InitialSessionState` built-in table with read-only/all-scope options for the platform flags. Other runtime identity values come from the owning runspace/process/runtime APIs; `SpecialVariables.cs` defines names and `SessionState.cs` initializes `$Error` and `$PSDefaultParameterValues`. `$true`, `$false`, and `$null` are compiled as constants in `parser/Compiler.cs`. | Owner is the relevant runspace/runtime or process. Platform flags/constants are read-only; do not infer that every listed runtime member is an ordinary preinstalled variable. Preserve restricted-language visibility checks and avoid exposing a value solely because a facade path exists. |
| `$?` and `$LASTEXITCODE` | `$?` is `ExecutionContext.QuestionMarkVariableValue`; the legacy `QuestionMarkVariable.Value` getter returns that value. Compiler special-cases `$?` in `parser/Compiler.cs`: optimized reads directly access the execution-context property, unoptimized reads use variable lookup for debugger checks, and `&&`/`||` chains use the same property. Pipeline/command code updates it in `CommandProcessorBase.cs`, `runtime/Operations/MiscOps.cs`, and debugger save/restore. `$LASTEXITCODE` is separately produced by native command execution. | `$?` is runspace execution state, read-only through the proposed facade, and must remain distinct from `$LASTEXITCODE`. A facade getter should read the same live execution-context flag at each access; it must not infer success from the last exit code or initialize that code. The property read itself does not need a new automatic-variable slot, but expression timing and evaluation contexts still need validation. |

### Presence, strict mode, and feasibility blockers

Automatic-variable tuple slots track presence separately with a `BitArray` in
`MutableTuple.IsValueSet`; generated slot setters set the bit even when the
stored reference is null. `SessionStateScope.GetAutomaticVariableValue` checks
active dotted tuples and its local tuple, then returns `AutomationNull.Value`
when no slot is set; the session-state API walks parent scopes until it finds a
non-sentinel result. Thus an unset slot and an explicitly assigned `$null` are
distinguishable in the current tuple machinery. A facade resolver must retain
the selected frame and use `IsValueSet` rather than treating a null value as
absence. Producers may also deliberately store `AutomationNull.Value` as an
internal no-input sentinel, which remains a producer-specific semantic to
preserve.

`VariableOps.ThrowStrictModeUndefinedVariable` receives the original
`VariableExpressionAst`. Strict mode 2 treats an unset automatic variable as
undefined; strict mode 1 does so except when the variable AST is under an
`ExpandableStringExpressionAst` (interpolation). A facade getter alone has no
such AST context. Therefore missing-versus-null behavior cannot be faithfully
implemented only in a generic container getter: compiler/binder integration
must pass expression context, or a narrowly scoped compiler hook must preserve
the existing strict-mode rule for facade member access while leaving ordinary
properties alone.

For `$?`, a direct getter on the facade is feasible because the engine already
has a single canonical bool source and the optimized compiler path reads that
same source. The executed comparison is described below; its helper reads the
active context directly through reflection. Separate runspaces prevent one
expression's status update from contaminating another expression's comparison.

The executable probe is
[`scoped-properties-feasibility.ps1`](scoped-properties-feasibility.ps1). It
uses reflection in a C# helper to call the active thread's
`LocalPipeline.GetExecutionContextFromTLS()` and read
`ExecutionContext.QuestionMarkVariableValue`, then executes each expression
comparison in a fresh runspace. Against the repository's published
`7.7.0-preview.5` host, all 20 comparisons matched for success, `Write-Error`,
a test cmdlet calling `PSCmdlet.WriteError`, and native exit code 7 across
direct, parenthesized, subexpression, array, and interpolated forms. The
separate native check returned `$LASTEXITCODE = 7`. The probe also asserted
nested and dotted `$args`, `$MyInvocation.InvocationName`, script path/root,
outer-argument restoration after dot sourcing, and StrictMode 2 behavior for
unset `$this` versus `$this = $null`; all assertions passed. A diagnostic JSON
capture from that run is in `artifacts/scoped-properties/expansion-stage0-feasibility.json`.
The helper getter confirms the canonical source and expression timing on the
current host; it does not prove that a future facade has correct runspace,
scope, or lifetime resolution. Those remain engine integration requirements.

The first runtime implementation should be bounded to one producer group at
a time. Invocation tuple properties are the best initial probe target because
the tuple writers and active-frame resolver are identifiable, but a complete
runtime layer still needs to define owner/lifetime checks, bound versus
unbound-view resolution, presence-aware strict-mode behavior, debugger checks,
and language-mode visibility. Event callback persistence, debugger cleanup,
and host/remoting optional values should follow only after those resolver
contracts are established. Feature-specific assertions belong in the existing
metadata-registered scoped-properties suites; do not add feature branches to
generic suites.

## Progress and ShouldProcess expansion validation

The .NET 11 build and publish succeeded. All 49 focused C# cases passed in
each verified feature state, including captured ShouldProcess groups from
expired scopes and foreign runspaces. The build used
`-p:EmitCompilerGeneratedFiles=false` to avoid the existing generated-file
cleanup/glob conflict; generators still ran in the compiler.

The first affected Pester regression runs reported 909 passes and five
failures with the feature enabled, and 890 passes and one failure with it
disabled. Two enabled failures were legacy Progress Suspend expectations;
Progress now joins the existing skip list, with assignment-time rejection
covered in the dedicated feature suite. The corrected generic suite passed
53 cases with 12 skips enabled and 65 cases disabled. Three executable-module
fixtures initially hit Windows antivirus error `0x800700E1`; all six binary
module cases passed on rerun. The disabled Warning child-shadow case passed
ten isolated repetitions and its complete 160-case scoped suite passed on
rerun. A narrowed rerun initially omitted the standard harness's process-only
execution policy and blocked temporary scripts; the corrected rerun passed.
These results establish coverage through targeted reruns, not an uninterrupted
clean regression run. Logs and XML results are under
`artifacts/scoped-properties/expansion-stage1`.

## Scalar configuration expansion validation

The .NET 11 build and publish succeeded with no compiler warnings. The focused
C# suite passed all 51 cases in each verified feature state, including null
logging fallback, qualified global reads, and a remoting resolver cmdlet that
uses no transport. The final scalar Pester suite passed 29 cases and the
enabled-host suite passed 34. The complete disabled scoped run passed 160
cases with 62 skips before the additional enabled-only native case. Initial
scalar failures were corrected test assertions, including ignored-variable
probes setting `HadErrors`, static-member argument parsing, Pester 4 matcher
support, and changing error invocation metadata between formatting samples.

Affected native encoding/error, formatting, history, transcription, and
Join-String regression suites passed 170 cases enabled (five skips, three
pending) and 173 disabled (two skips, three pending). Both states retained the
same six failures: a redirected Documents/default-transcript-path assumption,
two unavailable noninteractive prompts and two subsequent transcript file
locks, and a culture-dependent formatted-number expectation. These failures
are not counted as passes. Generic fixtures now avoid using the reserved
`$ps` name. Three incompatible legacy assertions are skipped when enabled;
dedicated feature cases verify typed Boolean normalization with native exits
and actual scoped enumeration truncation. Results are under
`artifacts/scoped-properties/expansion-stage2`.

## Mutable configuration expansion validation

The .NET 11 build and publish succeeded. All 52 focused C# tests passed in
each feature state. The final enabled scoped Pester run passed 266 cases; the
disabled run passed 160 with 106 feature-only skips. These include concrete
style and remoting types, shallow mutation isolation, explicit null, failed
mutations, dictionary interfaces, retained variable identity, qualified legacy
reads, ReadOnly/Constant bindings, and startup application-data state gating.
Review also caught and corrected recursive application-argument self-assignment
and getter exceptions hidden by normal property binding.

The affected style, rendering, redirection, and session-option regression
suites passed 169 cases with five failures in each feature state. The same
four ANSI word-wrapping failures and one MatchInfo formatting failure reproduce
with the installed, unmodified PowerShell 7.6.10 (which also has two additional
multiline-formatting failures). The test environment initially set `NO_COLOR=1`
and `TERM=dumb`; clearing those settings for the ANSI fixtures resolved their
default-rendering failure and enabled the terminal-dependent tests. The five
remaining failures are not counted as passes. Results and comparison logs are
under `artifacts/scoped-properties/expansion-stage3-accepted`,
`expansion-stage3-reviewed`, and the adjacent stage 3 logs.

## Runtime expansion validation

The .NET 11 build and publish succeeded. The focused C# suite passed 92 cases
in each verified feature state, including 25 success-flag comparisons, captured
facades, debugger stop/resume, a cmdlet-driven nested prompt, and sender-info
installation/removal without a remote transport. Dedicated tests verify tuple
and scope selection, object identity, variable permissions, restricted language,
missing versus null values, and matching `VariableIsUndefined` error IDs.
Strict-mode interpolation also covers compound assignments and expired views;
the implementation uses a catch body supported by both the interpreter and
compiled expression trees.

The complete scoped Pester suites passed 281 cases enabled and 160 disabled
with 121 feature-only skips. Runtime cases cover class/ETS contexts, input
consumption, loop restoration through exceptions and early exit, repeated event
callbacks, subscription-local error histories, and persistent event values.
Classes receive a facade as a method argument, preserving existing class
variable restrictions.

Affected parser, invocation, status, event, debugger, and restricted-language
regressions initially passed 598 cases enabled and 602 disabled, with eight
pending in each mode. An enabled parser fixture used the now-reserved `$ps`
name; renaming it restored its four tokenizer checks. The complete parser
rerun passed 421 cases in each mode, retaining two decimal-separator failures
for floating-point limits. Both failures also reproduce with the installed,
unmodified PowerShell 7.6.10 and are not counted as passes. Results are under
`artifacts/scoped-properties/expansion-stage4-accepted`,
`expansion-stage4-xunit-final`, and adjacent stage 4 logs.

## Final integration validation

The final audit reconciled the configuration registry, runtime descriptors,
legacy producers, and consumer routing against the expansion plan. It also
closed a lifetime gap in the fixed ANSI string properties: a captured bound
style now rejects expired scopes and foreign runspaces for `Reset`, `Bold`,
and the other attribute strings, just as its mutable leaves and helpers do.
Standalone `PSStyle.Instance` and portable unbound views retain their behavior.

The final .NET 11 build and publish succeeded. The complete scoped Pester
suites passed 282 cases enabled and 160 disabled, with 122 feature-only skips
in the disabled run. Both effective feature states were verified. The affected
style, rendering, redirection, and session-option regressions again passed
169 cases with the same five previously reproduced baseline failures in each
mode. No baseline failure is counted as a pass.

The final broad C# run completed with 500 passed, six skipped, and seven
locale-dependent `DisplayHumanReadableFileSize` failures in each verified
feature state. All 92 focused scoped tests passed within each run. The seven
failures expect decimal points while this machine formats decimal commas;
they match the previously recorded baseline. The broad run excluded
`RemoteHyperVTests` after full-suite attempts in both states hit the 90-second
inactivity timeout while `ValidateFailsWhenTokensMismatch` was active. Those
aborted attempts are not described as complete passes.

Initial broad attempts also exposed fixture permission errors and a validation
hook that redirected configuration-policy fixtures. The final rerun used the
required fixture permissions and a testhost-only hook that restores ordinary
configuration paths and caches after initializing the requested feature state;
all configuration-policy cases then passed. Child `pwsh` policy fixtures use
their native configuration.

Final logs and XML are under `artifacts/scoped-properties/expansion-stage5-accepted`
and `expansion-stage5-broad-final`; full-suite timeout evidence is retained in
`expansion-stage5-full-final`. Adjacent stage 5 logs record build, publish, and
affected regression results. Each delivery stage is reviewed and tested before its separate local commit; no changes were pushed.
