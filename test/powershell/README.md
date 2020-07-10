# Pester Testing Test Guide

Also see the [Writing Pester Tests](../../docs/testing-guidelines/WritingPesterTests.md)
document.

## Running Pester Tests

Go to the top level of the PowerShell repository and run: `Start-PSPester`
inside a self-hosted copy of PowerShell.

You can use `Start-PSPester -Tests SomeTestSuite*` to limit the tests run.

## Testing new `powershell` processes

Any launch of a new `powershell` process must include `-noprofile` so that
modified user and system profiles do not causes tests to fail. You also must
take care to call the development copy of PowerShell, which is *not* the first
one on the path.

Example:

```powershell
    $powershell = Join-Path -Path $PsHome -ChildPath "pwsh"
    & $powershell -noprofile -command "ExampleCommand" | Should Be "ExampleOutput"
```

## Portability

Some tests simply must be tied to certain platforms. Use Pester's
`-Skip` directive on an `It` statement to do this. For instance to run
the test only on Windows:

```powershell
It "Should do something on Windows" -Skip:($IsLinux -Or $IsMacOS) { ... }
```

Or only on Linux and OS X:

```powershell
It "Should do something on Linux" -Skip:$IsWindows { ... }
```

## Pending

When writing a test that should pass, but does not, please do not skip or delete
the test, but use `It "Should Pass" -Pending` to mark the test as pending, and
file an issue on GitHub.
