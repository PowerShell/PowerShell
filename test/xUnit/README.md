# xUnit Tests

The folder contains xUnit tests for PowerShell Core project.

## Running xUnit Tests

Go to the top level of the PowerShell repository and run full set of tests:
`Start-PSxUnit` inside a self-hosted copy of PowerShell.

Go to the test project folder and run `dotnet test -c Release`.

Use [`filter`][xunit-filter] parameter to run only needed tests:
```powershell
dotnet test -c Release --filter "FullyQualifiedName~UnitTest1   # Runs tests which have UnitTest1 in FullyQualifiedName
dotnet test --filter Name~TestMethod1   # Runs tests whose name contains TestMethod1
```

## Creating xUnit Tests

Keep the folder structure that is for Pester [../../test/powershell](../../test/powershell) and C# files [../../src](../../src).

Use namespace names started with `PSTests`.
```c#
namespace PSTests.YourNameSpace
{
}
```

[xunit-filter]: https://learn.microsoft.com/dotnet/core/testing/selective-unit-tests
