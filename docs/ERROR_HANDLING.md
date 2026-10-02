# Error Handling

> Parent: [../AGENTS.md](../AGENTS.md)

PowerShell has a rich, object-based error model. Errors are first-class objects
(`ErrorRecord`), not just strings or exit codes. This document describes the model that both
the engine and every cmdlet follow.

## The `ErrorRecord` Model

The canonical error object is `System.Management.Automation.ErrorRecord`
(`src/System.Management.Automation/engine/ErrorPackage.cs`). Cmdlets construct one like this
(pattern taken from `ConvertFrom-SddlString.cs`):

```csharp
ThrowTerminatingError(
    new ErrorRecord(
        exception,                       // the underlying System.Exception
        "InvalidSDDL",                   // stable, searchable error id string
        ErrorCategory.InvalidArgument,   // category (see table below)
        Sddl));                          // the target object the error is about
```

An `ErrorRecord` surfaces to the user with these key members:

| Member | Meaning |
|---|---|
| `Exception` | The underlying .NET exception. |
| `FullyQualifiedErrorId` | `errorId` + originating cmdlet/type — the stable string to search on. |
| `CategoryInfo` | Category, activity, reason, target name/type. |
| `TargetObject` | The object the error relates to. |
| `ErrorDetails` | Optional friendlier message / recommended action. |
| `InvocationInfo` / `ScriptStackTrace` | Where in the script the error occurred. |

## Terminating vs. Non-Terminating Errors

This is the central distinction in PowerShell error handling:

| | Terminating | Non-terminating |
|---|---|---|
| Emitted via | `ThrowTerminatingError(errorRecord)` or `throw` | `WriteError(errorRecord)` |
| Stops the command? | Yes | No - continues to next pipeline item |
| Caught by `try/catch`? | Yes | Only if `-ErrorAction Stop` |
| Typical use | Invalid arguments, unrecoverable state | Per-item failures in a batch |

Both patterns are used heavily across the command modules (see, e.g.,
`src/Microsoft.PowerShell.Commands.Utility/commands/utility/AddMember.cs` and `CsvCommands.cs`,
which use `ThrowTerminatingError` for fatal input problems and `WriteError` for per-item issues).

## `ErrorCategory` Values

`ErrorCategory` (`engine/ErrorPackage.cs`) classifies the failure. Common values:

| Category | Use for |
|---|---|
| `InvalidArgument` | Bad parameter value. |
| `InvalidOperation` | Operation not valid in current state. |
| `InvalidData` | Malformed input data. |
| `ObjectNotFound` | Target item does not exist. |
| `PermissionDenied` | Access/authorization failure. |
| `SecurityError` | Security policy violation (e.g. execution policy, CLM). |
| `ResourceUnavailable` | Resource missing/busy. |
| `NotImplemented` / `NotSpecified` | Fallbacks. |

## How Errors Reach the User

```
 cmdlet WriteError / ThrowTerminatingError
        |
        v
 MshCommandRuntime  (engine/MshCommandRuntime.cs)
        |  honors -ErrorAction, -ErrorVariable
        v
 $Error automatic variable  (most-recent-first list)
   +  error stream (stream #2)
        |
        v
 ErrorView formatting (ConciseView default) -> host
```

- The `$Error` collection holds recent errors (newest first).
- `$?` reflects whether the last operation succeeded.
- `-ErrorAction` (`Continue`, `Stop`, `SilentlyContinue`, `Ignore`, `Inquire`) and
  `-ErrorVariable` are common parameters handled by the runtime, not each cmdlet.
- `Get-Error` renders the full details of the most recent error(s).

## Exit Codes (native / process boundary)

- `$LASTEXITCODE` holds the exit code of the last native executable.
- `pwsh` itself returns an exit code from `-Command`/`-File`; a script's `exit N` sets it.

## Guidance for New Code

- Prefer `WriteError` for recoverable, per-item failures so pipelines keep flowing.
- Reserve `ThrowTerminatingError` for conditions that make the whole command meaningless.
- Always supply a **stable, unique `errorId`** and the correct `ErrorCategory`; users and
  tests match on `FullyQualifiedErrorId`.
- Localize error messages via the module `resources/*.resx` (see `Start-ResGen`), never
  hard-code user-facing English strings.
