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

### Formatting workloads

The standalone [`Measure-Formatting.ps1`](../Measure-Formatting.ps1) measures
formatting packets, registered views, tables, wrapping, calculated columns, lists,
and wide output without requiring BenchmarkDotNet. Inputs are precreated plain,
ANSI-decorated, and word-rich long fields. Additional inputs compare alternating
and grouped derived type hierarchies sharing the registered table view, insert an
out-of-band list notice after every 100 table rows, and combine long fields with
ANSI decoration at width 40. The default row counts include 1 and 10 to expose
per-pipeline startup costs (not process startup). Run from the repository root using
separate baseline and candidate Release executables:

```powershell
& 'C:\builds\baseline\pwsh.exe' -NoProfile -File .\test\perf\Measure-Formatting.ps1 -RowCount 20000 -RepeatCount 11 -ResultPath C:\results\baseline.json
& 'C:\builds\candidate\pwsh.exe' -NoProfile -File .\test\perf\Measure-Formatting.ps1 -RowCount 20000 -RepeatCount 11 -ResultPath C:\results\candidate.json
```

Use `-InputKind Alternating,Grouped -ScenarioName ViewTextStream` when invoking
the script within PowerShell to isolate hierarchy-cache behavior, or
`-InputKind AnsiLong` for narrow truncation and wrapping. `-ScenarioName` filters the
scenarios supported by each input kind; unsupported combinations are skipped.

The result directory must already exist. JSON contains runtime metadata, the
measured assembly's SHA-256, requested filters, effective scenarios per workload,
and raw samples; a companion CSV contains medians and ranges. `Rows` counts normal
rows; `InputCount` includes inserted notices. Each scenario receives three
warmups and seeded, shuffled repetitions at width 120 (40 for the narrow scenarios).
Construction, metadata hashing, and terminal/file
I/O are excluded. Allocation is process-wide `GC.GetTotalAllocatedBytes(true)`,
not peak or retained memory. Run builds sequentially on an otherwise idle machine
with the same runtime and configuration. Compare each scenario against its own
baseline; the enumeration scenario is a control, not a subtractable pipeline cost.
Measure first-row latency separately when changing formatter buffering.

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
