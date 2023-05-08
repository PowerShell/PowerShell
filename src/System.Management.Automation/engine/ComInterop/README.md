# ComInterop

The ComInterop code shipped in PowerShell comes from [dotnet/runtime](https://github.com/dotnet/runtime) with _a considerable amount of refactoring work_ to make it work properly with PowerShell.

> **NOTE: Do not modify the ComInterop code unless for fixing a bug. We want to keep minimal diffs when comparing with the .NET 5.0 ComInterop code base.**

There are 3 sources of the ComInterop code as our references:

1. [The .NET Framework version](https://github.com/IronLanguages/main/tree/ipy-2.7-maint/Runtime/Microsoft.Dynamic/ComInterop).
   The code was archived in 2012.

2. [The .NET 5.0 version](https://github.com/dotnet/runtime/tree/master/src/libraries/Microsoft.CSharp/src/Microsoft/CSharp/RuntimeBinder/ComInterop).
   It was merged into .NET 5.0 in May 2020 through the PR [dotnet/runtime#33060](https://github.com/dotnet/runtime/pull/33060).
   It's based on the .NET Framework version code with quite amount of refactoring work.

3. [The legacy ComInterop code from Windows PowerShell](https://github.com/PowerShell/PowerShell/tree/v7.0.0/src/System.Management.Automation/engine/ComInterop).
   The legacy code has always been in the repository, but it was excluded from compilation.
   It was based on the .NET Framework version code with a considerable amount of refactoring work to make it work properly with Windows PowerShell.

## Code Refreshing

The ComInterop code was refreshed and enabled in compilation in PowerShell 7.1, August 2020.
It was done manually by:

- A careful **three-way comparison** across all the code sources listed above:
  - Compare (3) to (1) to get the PowerShell specific changes
  - Compare (2) to (1) to get the .NET 5.0 changes

- Applying the PowerShell specific changes from (3) to (2), with necessary further refactoring

- A careful review of the new PowerShell specific changes applied to (2), again using the three-way comparison:
  - Compare the refreshed ComInterop code with (3) to get the differences
  - Analyze the differences to make sure they are either the pure .NET 5.0 changes over the .NET Framework code,
    or the refactoring changes that is necessary to apply the PowerShell specific changes.

## Code Layout Changes

The major changes in code layout are recorded below.

### Removed Source Files

The following source files that existed in (1) and (3) were removed in (2):

```none
ComDispIds.cs
ComEventSink.cs
ComEventSinkProxy.cs
ComParamDesc.cs
ComType.cs
ComTypeLibInfo.cs
ComTypeLibMemberDesc.cs
TypeLibInfoMetaObject.cs
```

I examined them all and it was mainly a clean-up work done in .NET 5.0:

- The code in `ComParamDesc.cs`, `ComType.cs`, `ComTypeLibInfo.cs`, `ComTypeLibMemberDesc.cs` and `TypeLibInfoMetaObject.cs`
  are not used or not needed, and thus removed.
- The code in `ComDispIds.cs`, `ComEventSink.cs` and `ComEventSinkProxy.cs` were moved or refactored to different files.

### New Source Files

Some source files were refactored and moved to the `System.Runtime.InteropServices` namespace in .NET 5.0,
they can be found [here](https://github.com/dotnet/runtime/tree/master/src/libraries/Common/src/System/Runtime/InteropServices).
In PowerShell, the corresponding source files are placed under `engine\ComInterop\InteropServices`.

Additionally, `ComEventsSink.Extended.cs` and `Variant.Extended.cs` under `engine\ComInterop` are new in .NET 5.0.
Combined with the files `ComEventsSink.cs` and `Variant.cs` under `engine\ComInterop\InteropServices`,
they are the refactored replacement of the old `ComEventSink.cs`, `ComEventSinkProxy.cs` and `Variant.cs` files in .NET Framework.
