# PowerShell cmdlet exception-handling analyzer

This opt-in Roslyn analyzer package helps binary cmdlet authors preserve
PowerShell engine control flow. It is not automatically enabled in PowerShell's
own projects. The analyzer targets .NET Standard 2.0 and supports Roslyn 4.8 or
later. Building it and running its tests requires a .NET 8 SDK; the local
`global.json` deliberately makes it independent of the engine's SDK version.

## Preserve engine exceptions

Use the public static `PSCmdlet.IsPowerShellControlFlowException(Exception)` helper
in broad C# exception filters:

```csharp
try
{
    // Work that may call into PowerShell, including WriteObject.
}
catch (Exception e) when (!PSCmdlet.IsPowerShellControlFlowException(e))
{
    // Handle ordinary failures.
}
```

Inside a `PSCmdlet` subclass the helper can be called without qualification.
It recognizes `FlowControlException` (including upstream stopping,
break/continue/return/exit), `PipelineStoppedException`,
`ActionPreferenceStopException`, and `HaltCommandException`. It also looks
through `TargetInvocationException` and `RuntimeException` wrappers. It does
not classify every PowerShell-defined exception as engine control flow.
Passing null throws `ArgumentNullException`.

This pattern requires a version of System.Management.Automation containing the
new helper. It avoids swallowing engine exceptions thrown by engine APIs or
by downstream commands while writing pipeline output.

## Stop upstream commands

Binary cmdlets can call `StopUpstreamCommands()`; advanced script functions can
call `$PSCmdlet.StopUpstreamCommands()`. Write the current result before calling
it, because the method never returns:

```csharp
protected override void ProcessRecord()
{
    try
    {
        WriteObject(InputObject);
        if (HaveEnoughInput())
        {
            StopUpstreamCommands();
        }
    }
    catch (Exception e) when (!IsPowerShellControlFlowException(e))
    {
        // Handle ordinary failures.
    }
}
```

The API preserves the existing `Select-Object -First` semantics: upstream
commands and the requesting command skip end processing, downstream commands
finish, and script `clean` blocks still run. Upstream binary `StopProcessing`
callbacks are not introduced by this API. Use resource disposal or script
`clean` blocks for cleanup, rather than relying on upstream end processing.

Call the method only during command processing on the pipeline thread.
Calling it on an unhosted cmdlet, after processing, or from a worker thread
throws `InvalidOperationException`. The internal upstream-stop exception
remains non-public.

## Diagnostic PSCMD001

The warning applies to catches in classes deriving from `Cmdlet`, including
indirect subclasses. It checks catch-all handlers and exception types that can
catch engine exceptions or their PowerShell/reflection wrappers.
Narrow ordinary-error catches and immediate `throw;` handlers are allowed.
Generated code and non-cmdlet classes are ignored.

The filter must exclude engine exceptions for the **caught variable**, using
the actual `PSCmdlet.IsPowerShellControlFlowException` method. Qualified calls,
aliases, inherited calls, `using static`, parentheses, and comparison with
`false` are recognized. An exclusion combined with `&&` is allowed; an `||`
filter is not considered safe, because its other branch can accept engine
exceptions. Unrelated methods with the same name do not satisfy the rule.

The analyzer deliberately does not prove arbitrary custom predicates or
multi-statement rethrow logic. Such handlers can be rewritten using the
recommended filter, or suppressed with a justification. No code fix rewrites
user exception-handling logic automatically.

## Build, test, and package

Run from this directory:

```powershell
dotnet test tests\CmdletAnalyzers.Tests.csproj -c Release
dotnet pack Microsoft.PowerShell.CmdletAnalyzers.csproj -c Release
```

The generated package places the analyzer in `analyzers/dotnet/cs`, with no
runtime dependency on System.Management.Automation. To opt in, publish or
place the built package in a configured NuGet feed, then reference it in a
cmdlet project:

```xml
<PackageReference Include="Microsoft.PowerShell.CmdletAnalyzers"
                  Version="0.1.0"
                  PrivateAssets="all" />
```

This change does not publish the package to a public feed.
Configure severity using EditorConfig if needed:

```ini
[*.cs]
dotnet_diagnostic.PSCMD001.severity = warning
```

The engine integration tests live in
[test_PipelineControl.cs](../../test/xUnit/csharp/test_PipelineControl.cs).
With the engine's pinned SDK installed, run from the repository root:

```powershell
Import-Module .\build.psm1
Start-PSBuild -Configuration Release
dotnet test test\xUnit\xUnit.tests.csproj -c Release --filter FullyQualifiedName~PipelineControlTests -p:DefaultItemExcludesInProjectFolder='**\gen\SourceGenerated\**'
```

The test-build exclusion prevents previous source-generator outputs from being
included as ordinary source files; the engine regenerates them during builds.
