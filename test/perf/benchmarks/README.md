## Micro Benchmarks

This folder contains micro benchmarks that test the performance of PowerShell Engine.

### Requirement

1. A good suite of benchmarks
   Something that measures only the thing that we are interested in and _produces accurate, stable and repeatable results_.
2. A set of machine with the same configurations.
3. Automation for regression detection.

### Design Decision

1. This project is internal visible to `System.Management.Automation`.
   We want to be able to target some internal APIs to get measurements on specific scoped scenarios,
   such as measuring the time to compile AST to a delegate by the compiler.
2. This project makes `ProjectReference` to other PowerShell assemblies.
   This makes it easy to run benchmarks with the changes made in the codebase.
   To run benchmarks with a specific version of PowerShell,
   just replace the `ProjectReference` with a `PackageReference` to the `Microsoft.PowerShell.SDK` NuGet package of the corresponding version.

### Quick Start

You can run the benchmarks directly using `dotnet run` in this directory:
1. To run the benchmarks in Interactive Mode, where you will be asked which benchmark(s) to run:
   ```
   dotnet run -c Release -f net6.0
   ```

2. To list all available benchmarks ([read more](https://github.com/dotnet/performance/blob/main/docs/benchmarkdotnet.md#Listing-the-Benchmarks)):
   ```
   dotnet run -c Release -f net6.0 --list [flat/tree]
   ```

3. To filter the benchmarks using a glob pattern applied to `namespace.typeName.methodName` ([read more](https://github.com/dotnet/performance/blob/main/docs/benchmarkdotnet.md#Filtering-the-Benchmarks)]):
   ```
   dotnet run -c Release -f net6.0 --filter *script* --list flat
   ```

4. To profile the benchmarked code and produce an ETW Trace file ([read more](https://github.com/dotnet/performance/blob/main/docs/benchmarkdotnet.md#Profiling))
   ```
   dotnet run -c Release -f net6.0 --filter *script* --profiler ETW
   ```

You can also use the function `Start-Benchmarking` from the module [`perf.psm1`](../perf.psm1) to run the benchmarks:
```powershell
Start-Benchmarking [-TargetFramework <string>] [-List <string>] [-Filter <string[]>] [-Artifacts <string>] [-KeepFiles] [<CommonParameters>]

Start-Benchmarking [-TargetPSVersion <string>] [-Filter <string[]>] [-Artifacts <string>] [-KeepFiles] [<CommonParameters>]

Start-Benchmarking -Runtime <string[]> [-Filter <string[]>] [-Artifacts <string>] [-KeepFiles] [<CommonParameters>]
```
Run `Get-Help Start-Benchmarking -Full` to see the description of each parameter.

### FileSystem provider metadata comparison

The experimental feature is read once during process initialization. The helper compares
separate `pwsh` processes with explicit settings files:

```powershell
./ProviderMetadata.ProcessBenchmark.ps1 -PwshPath <candidate-pwsh> -FileCount 100 -Iterations 20 -Repetitions 3 -WarmupIterations 3 -ProcessRuns 3 -OutputPath .\provider-metadata.json
```

`-BaselinePwshPath` defaults to `-PwshPath`. The baseline always receives a settings file
with the feature disabled; it may be a build that does not know the feature, in which
case the report records `unavailable-off`. The candidate receives a settings file with
the feature enabled and must expose and enable it. Use two paths to compare against a
pre-feature build:

```powershell
./ProviderMetadata.ProcessBenchmark.ps1 -BaselinePwshPath <pre-feature-pwsh> -PwshPath <candidate-pwsh>
```

Using the same build for both sides with the feature off and on isolates the feature's
effect in that build. A separate pre-feature build can show overhead shared by both
feature states, but differences in PowerShell, CLR, OS, or architecture can confound the
comparison. The JSON records executable paths, PowerShell and CLR versions, OS,
architecture, and an assembly-attribute-based debug status where available. Set the
build configuration through the build itself; the helper reports it and does not infer
or change it.

For each process run, the baseline and candidate execute in alternating order. Each
fresh process warms every scenario before collecting `-Repetitions` raw samples. Scenarios
cover filesystem item emission, direct `PSPath` reads, ten repeated `PSPath` reads per
item, `ForEach-Object -MemberName FullName`, `ForEach-Object -MemberName PSParentPath`, default formatting, and `Select-Object *`. Metadata scenarios accumulate path
lengths into a checksum so each property value is consumed; the report records the
metadata read count per emitted item. Comparisons summarize median elapsed
time and median allocated bytes per emitted item. Ratios are candidate divided by
baseline; percent change is `(ratio - 1) * 100`, so negative values mean a lower measured
value for the candidate. A zero baseline produces a JSON `null` ratio and percent change.

The report includes the effective settings, observed feature state, dataset count,
process order, and all raw samples. `-OutputPath` is optional; when set, the exact JSON
printed to stdout is also written there, and its parent directory must already exist.
The helper creates and removes only its guarded temporary dataset and settings files;
it does not edit the current PowerShell configuration.

Retained emission is reported separately as an approximate retained managed-heap delta
per emitted object after forced collections. It is not process RSS and should not be
treated as a significance estimate. Allocations come from
`GC.GetAllocatedBytesForCurrentThread`, so they measure allocations on the child
PowerShell thread rather than all process threads.

### Regression Detection

We use the tool [`ResultsComparer`](../dotnet-tools/ResultsComparer) to compare the provided benchmark results.
See the [README.md](../dotnet-tools/ResultsComparer/README.md) for `ResultsComparer` for more details.

The module `perf.psm1` also provides `Compare-BenchmarkResult` that wraps `ResultsComparer`.
Here is an example of using it:

```
## Run benchmarks targeting the current code base
PS:1> Start-Benchmarking -Filter *script* -Artifacts C:\arena\tmp\BenchmarkDotNet.Artifacts\current\

## Run benchmarks targeting the 7.1.3 version of PS package
PS:2> Start-Benchmarking -Filter *script* -Artifacts C:\arena\tmp\BenchmarkDotNet.Artifacts\7.1.3 -TargetPSVersion 7.1.3

## Compare the results using 5% threshold
PS:3> Compare-BenchmarkResult -BaseResultPath C:\arena\tmp\BenchmarkDotNet.Artifacts\7.1.3\ -DiffResultPath C:\arena\tmp\BenchmarkDotNet.Artifacts\current\ -Threshold 1%
summary:
better: 4, geomean: 1.057
total diff: 4

No Slower results for the provided threshold = 1% and noise filter = 0.3ns.

| Faster                                                                           | base/diff | Base Median (ns) | Diff Median (ns) | Modality|
| -------------------------------------------------------------------------------- | ---------:| ----------------:| ----------------:| --------:|
| Engine.Scripting.InvokeMethod(Script: "$fs=New-Object -ComObject scripting.files |      1.07 |         50635.77 |         47116.42 |         |
| Engine.Scripting.InvokeMethod(Script: "$sh=New-Object -ComObject Shell.Applicati |      1.07 |       1063085.23 |        991602.08 |         |
| Engine.Scripting.InvokeMethod(Script: "'String'.GetType()")                      |      1.06 |          1329.93 |          1252.51 |         |
| Engine.Scripting.InvokeMethod(Script: "[System.IO.Path]::HasExtension('')")      |      1.02 |          1322.04 |          1297.72 |         |

No file given
```

## References

- [Getting started with BenchmarkDotNet](https://benchmarkdotnet.org/articles/guides/getting-started.html)
- [Micro-benchmark Design Guidelines](https://github.com/dotnet/performance/blob/main/docs/microbenchmark-design-guidelines.md)
- [Adam SITNIK: Powerful benchmarking in .NET](https://www.youtube.com/watch?v=pdcrSG4tOLI&t=351s)
