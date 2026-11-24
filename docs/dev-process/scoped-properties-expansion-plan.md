# Proposed scoped properties expansion plan

> **Status: proposed; not implemented.** This plan describes the planned
> expansion beyond the current prototype. The existing
> [scoped properties prototype](scoped-properties.md) documents implemented
> behavior and its validation history. This file does not change that account.

## Goal and design

Expand `$PS` to cover the remaining built-in configuration and runtime
variables, including names without a `PS` prefix. Keep legacy variables as
compatible access paths. Preserve the current `ActionPreference` and `Style`
APIs, and complete `Style` using the existing `PSStyle` member names, including
formatting, progress, file extensions, output rendering, colors, and ANSI
helpers. Keep the ANSI string `Style.Reset` distinct from the facade method
`Reset(path)`.

Configuration properties may have defaults, scoped inheritance, and reset.
Runtime properties are live views of their existing producer, owner, lifetime,
and permissions. They do not use configuration defaults, reset, or
copy-on-write. Runtime object values keep ordinary reference semantics, including
mutable objects exposed through a constant variable. Give each new property
container a localized, descriptive `ToString()` that neither enumerates
runtime values nor consumes input.

Use a shared inventory and registry for paths, names, metadata, and access
routing, but separate configuration descriptors from runtime descriptors.
Configuration descriptors hold typed defaults, conversion, validation, and
scoping behavior. Runtime descriptors specify explicit resolution, owner,
lifetime, presence behavior, and permitted writes. Do not route engine
consumers through a configuration-default model when they consume runtime
state.

`$_` and `$PSItem` remain variables. Expose `$?` additionally as
`$PS.LastStatementSuccessful`, and use `$PS.NativeCommand.Encoding.ToProcess`
and `.FromProcess` for native-process text encoding. Automatic variables are
conceptually read-only in the documentation, but this plan intentionally
preserves existing runtime setters where legacy behavior permits them.

## Public API mapping

All paths are relative to `$PS`. Existing `ActionPreference` and `Style` APIs
remain. The following inventory preserves the original proposed mapping; the
semantics and boundaries below refine it.

| New path | Legacy variable or source |
|---|---|
| `ActionPreference.Progress` | `ProgressPreference` |
| `ShouldProcess.Confirm`, `.WhatIf` | `ConfirmPreference`, `WhatIfPreference` |
| `Formatting.ErrorView`, `.EnumerationLimit`, `.FieldSeparator` | `ErrorView`, `FormatEnumerationLimit`, `OFS` |
| `NativeCommand.Encoding.ToProcess` | `OutputEncoding`: encode text sent to stdin |
| `NativeCommand.Encoding.FromProcess` | `PSApplicationOutputEncoding`: decode stdout and stderr |
| `NativeCommand.ArgumentPassing`, `.UseErrorActionPreference` | Corresponding `PSNativeCommand*` variables |
| `Command.DefaultParameterValues` | `PSDefaultParameterValues` |
| `Module.AutoLoadingPreference` | `PSModuleAutoLoadingPreference` |
| `Remoting.SessionOptions`, `.ConfigurationName`, `.ApplicationName`, `.ApplicationPrivateData` | Corresponding `PSSession*` variables and `PSApplicationPrivateData` |
| `History.MaximumCount` | `MaximumHistoryCount` |
| `Transcription.Path` | `TRANSCRIPT` |
| `Mail.Server` | `PSEmailServer` |
| `Help.VerboseErrors` | `VerboseHelpErrors` |
| `Logging.EngineHealthEvent`, `.EngineLifecycleEvent`, `.CommandHealthEvent`, `.CommandLifecycleEvent`, `.ProviderHealthEvent`, `.ProviderLifecycleEvent`, `.SettingsEvent`, `.UserData` | Seven `Log*Event` variables and `PSLogUserData` |
| `LastStatementSuccessful`, `LastExitCode` | `?`, `LASTEXITCODE` |
| `Invocation.Arguments`, `.Input`, `.This`, `.Cmdlet`, `.BoundParameters`, `.Info`, `.ScriptRoot`, `.CommandPath` | `args`, `input`, `this`, `PSCmdlet`, `PSBoundParameters`, `MyInvocation`, `PSScriptRoot`, `PSCommandPath` |
| `Invocation.ForEachEnumerator`, `.SwitchEnumerator`, `.Matches` | `foreach`, `switch`, `Matches` |
| `Diagnostics.Errors`, `.StackTrace`, `.DebugContext` | `Error`, `StackTrace`, `PSDebugContext` |
| `History.FirstToken`, `.LastToken` | `^`, `$` |
| `Session.Host`, `.ExecutionContext`, `.Home`, `.CurrentLocation`, `.Profile`, `.NestedPromptLevel`, `.CurrentlyExecutingCommand` | Corresponding host/session variables, including `HOME`, `PWD`, `PROFILE` |
| `Runtime.ProcessId`, `.InstallationDirectory`, `.ShellId`, `.VersionTable`, `.Edition`, `.EnabledExperimentalFeatures` | `PID`, `PSHOME`, `ShellId`, `PSVersionTable`, `PSEdition`, `EnabledExperimentalFeatures` |
| `Runtime.Culture`, `.UICulture`, `.IsWindows`, `.IsLinux`, `.IsMacOS`, `.IsCoreCLR` | Corresponding culture/platform variables |
| `Event.Record`, `.Subscriber`, `.Arguments`, `.Sender` | `Event`, `EventSubscriber`, `EventArgs`, `Sender` |
| `Remoting.SenderInfo` | `PSSenderInfo` |
| `Constants.True`, `.False`, `.Null` | Read-only views of language constants |

Keep `Invocation.ScriptRoot` and `Invocation.CommandPath` distinct from
`Invocation.Info.PSScriptRoot` and `.PSCommandPath`: the former describe the
current script; the latter describe the caller in invocation metadata.

## Descriptor and runtime contracts

### Configuration descriptors

- Maintain one authoritative configuration value when the feature is enabled.
  A descriptor records legacy names, facade paths, defaults, conversion,
  validation, ownership, and consumers. Variable access, facade access,
  compiler paths, and engine consumers use that descriptor.
- Reads inherit through scopes; writes create local overrides unless explicitly
  targeted with `AtScope('Local'|'Script'|'Global')`. `Reset(path)` removes only
  the selected override. With the feature disabled, `$PS` remains an ordinary
  variable name and legacy storage remains unchanged. Preserve forwarding only
  for explicitly constructed facades where supported.
- Retain legacy variable metadata, validation, breakpoints, common-parameter
  behavior, defaults, and console settings. Runtime-compatible setters remain
  writable only where the legacy variable allows writes.
- For mutable configuration, copy on the first successful property or
  container mutation. Isolate built-in setting properties and dictionary
  entries, while arbitrary objects stored inside retain normal reference
  semantics. Both access paths observe the same override. Preserve concrete
  public types through internal scope-aware backing for `PSStyle`,
  `PSSessionOption`, and `DefaultParameterDictionary`; standalone instances
  keep existing behavior.

### Runtime descriptors and presence

- Keep stable, discoverable containers and members. Resolve every value from
  its actual caller frame or owning runtime component; never synthesize a read
  value to make the API appear complete.
- Internally track presence separately from value, so an absent value is
  distinguishable from a binding explicitly assigned `$null`. Preserve the
  legacy missing-variable result, including strict-mode exceptions. Verify
  interpolation and other expression contexts as well as direct reads.
- Preserve runtime write permissions, conversion, breakpoints, and visibility.
  A runtime setter may create a binding when the legacy assignment does.
  Runtime properties reject `Reset()`.
- Unbound facades and captured containers resolve the caller on every access.
  Bound views start at their selected scope, including that scope's active
  dotted frames and the legacy fallback; they are live
  views, not snapshots or captures of completed invocations. Scope-owned views
  expire with their scope. Runspace/thread/component-owned values retain their
  owner. Reject expired scopes and foreign runspaces.
- Runtime values are ordinary references. Do not apply configuration
  copy-on-write to objects such as `$Error`, event values, or the input
  enumerator. A mutable value behind a constant variable remains mutable.
- Keep current location runspace-owned, not process-global. Culture and UI
  culture stay backed by the live runspace thread.

## Conditional runtime inventory

Runtime groups and members remain present even when no value is available.
Getters follow the existing producers; the facade must not create or prolong
producer state. Preserve each producer's creation and cleanup boundary.

| Group | Required behavior |
|---|---|
| Invocation tuple: arguments, input, `this`, `PSCmdlet`, bound parameters, `MyInvocation`, script paths | Resolve from the active invocation tuple. Scope is not invocation: nested calls and dotted script frames must select the correct tuple and legacy fallback. |
| `this`, ETS script property/method, event delegates | Resolve according to the active class/script/event context. Preserve legacy ownership and binding behavior for script properties, methods, and delegates. |
| Input | Preserve begin/process/end binding, enumerator identity, and consumption state. A getter, container display, and `ToString()` must not call `MoveNext()`, enumerate, or otherwise consume input. |
| `foreach`, `switch`, `Matches` | Preserve loop save/restore, including nesting, exceptions, and early exit. `$Matches` retains the last successful match across a later nonmatch. Cover scalar and collection `-match`, plus `switch -Regex`. |
| `Event.*` | Populate in the action's session state and persist between callbacks. Values are not global and are not automatically cleared at callback end. |
| `$Error` | Keep global error history distinct from the event action's script-scope persistent mutable list per subscription. Preserve list identity and do not copy on write. |
| `PSDebugContext` | Create at debugger stop and remove in `finally` on resume. Documentation reports null outside a stop; internally distinguish absent storage from a non-strict-mode null result. |
| `PROFILE` | Optional, host-created value. Preserve absence when no host supplies it. |
| `CurrentlyExecutingCommand` | Optional nested-prompt value; save and restore across nested prompt entry/exit. |
| `PSSenderInfo` | Optional remoting-owned value. Preserve absence outside remoting. |
| `$?` | Read the same success flag as the legacy variable, read-only through the facade. Verify compiler feasibility and evaluation behavior, including parentheses, subexpressions, arrays, interpolation, and captured facades. Cover write-error vs `PSCmdlet.WriteError` and native exits. Do not equate it with `$LASTEXITCODE` or synthesize an initial exit code. |
| `PSApplicationPrivateData` | Preserve its startup boundary: the server consumes the global after the startup script, only while the pool is `Opening`, and converts it to `PSPrimitiveDictionary`. This is distinct from published application data; changing it later does not update an established session. Preserve this global consumer rather than migrating it to ordinary scoped resolution. |

`PSApplicationPrivateData` is the explicit exception to migrating all global
consumers. Its current server startup contract depends on a global value read
after startup scripts, so preserve the boundary and global consumption.

## Exclusions and security

Exclude environment-provider entries, arbitrary user/module variables,
retired Windows PowerShell-only variables, and a recursive `$PS` self-property.
Do not bypass class, manifest, or language-mode restrictions or expose values
that legacy visibility rules forbid. Property and method access must retain
existing constrained-language checks.

## Delivery sequence

1. Inventory and feasibility: record every producer, resolver, presence rule,
   owner, lifetime, permission, and mutation path. Test active runtime frames
   and `$?` compiler/evaluation feasibility before expanding the registry.
1. Scalar preferences: extend configuration descriptors with Progress, Confirm,
   and WhatIf. Cover compiler slots and invocation-frame behavior.
1. Remaining scalar configuration: add formatting, native command encoding and
   behavior, logging, history, modules, help, mail, transcription, and remoting
   settings. Update and inventory each consumer, retaining startup exceptions.
1. Mutable configuration: add scoped backing and mutation isolation for command
   defaults, session options, application private data where compatible with
   its startup contract, and the complete style surface.
1. Runtime views: add invocation, diagnostics, session, event, remoting,
   runtime, constants, and success/exit-status properties according to their
   producer lifetimes.
1. Integration and documentation: publish the complete mapping, explain
   compatibility boundaries, and reconcile every path against producers and
   consumers.

Delegate bounded implementation stages to a cheaper model. The orchestrator
owns architectural integration, diff review, validation, and local commits.
Do not push.

## Validation and acceptance

Keep feature-specific Pester assertions in dedicated,
metadata-registered experimental-feature suites. Generic suites may skip
incompatible legacy cases but must not gain feature-specific assertion
branches. Test before, during, and after each relevant lifetime.

- Verify bidirectional legacy/facade access, defaults, conversion failures,
  metadata, reset, and local/script/global inheritance for configuration.
- Cover functions, modules, dot sourcing, common parameters, named preference
  parameters, nested invocation frames, and independent runspaces.
- Cover mutable access through both interfaces, captured references, failed
  mutations, dictionary methods/indexers, parent isolation, and shared arbitrary
  values.
- Test native stdin encoding and stdout/stderr decoding with non-ASCII text,
  including existing fallback behavior.
- Test missing vs assigned null, strict and non-strict reads, interpolation,
  unbound/bound access, nested and dotted modules, class/ETS contexts, and
  runspace ownership.
- Test iterators and loop state across nesting, exception, and early exit;
  `$Matches` persistence; event callbacks repeated within and across
  subscriptions; event-local error list identity; debugger stop/resume cleanup;
  nested prompts; optional host/remoting values; and current location/culture.
- Test `$?` against the legacy flag after success and failure, including
  parentheses, subexpressions, arrays, interpolation, captured facades,
  `Write-Error`, `PSCmdlet.WriteError`, and native process exits. Test
  `$LASTEXITCODE` separately and verify no initial exit code is invented.
- Test `PSApplicationPrivateData` before and after startup, pool state gating,
  conversion, and established-session stability.
- Verify restricted language and feature-enabled and feature-disabled
  execution, including effective feature state.

Run focused C# and Pester checks for each stage, affected regression suites,
then the broader C# suite. Report known or pre-existing failures separately;
do not describe them as passing. Keep the implementation inventory and contracts
in `docs/dev-process/scoped-properties.md` when work is implemented; this
proposed plan does not change that prototype document.

## Source locations for Stage 0

Inspect the following implementation sites while building the producer and
consumer inventory (paths relative to `src/System.Management.Automation/engine`):

- `SessionStateScope.cs`: `GetAutomaticVariableValue`
- `SessionStateVariableAPIs.cs`: `GetAutomaticVariableValue`
- `runtime/Operations/VariableOps.cs`: `GetAutomaticVariableValue`,
  `ThrowStrictModeUndefinedVariable`
- `parser/Compiler.cs`: `GetAutomaticVariable`, `GenerateIteratorStatement`
- `runtime/CompiledScriptBlock.cs`: `scriptThis` and `PSCmdlet` tuple
- `EventManager.cs`: `PSEventJob.Invoke` persistent variables and
  `CreateBoundScriptBlock` script:`Error`
- `ExecutionContext.cs`: `DollarErrorVariable`
- `debugger/debugger.cs`: `PSDebugContext` set/remove
- `hostifaces/InternalHost.cs`: nested prompt save/restore
- `remoting/server/ServerRunspacePoolDriver.cs`:
  `InvokeStartupScripts` global `PSApplicationPrivateData`

Use the official [automatic variables reference](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_automatic_variables?view=powershell-7.7)
as the public inventory reference, then confirm actual producers and behavior
against the repository source and tests.
