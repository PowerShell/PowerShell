# Results Comparer

This simple tool allows for easy comparison of provided benchmark results.

It can be used to compare:
* historical results (eg. before and after my changes)
* results for different OSes (eg. Windows vs Ubuntu)
* results for different CPU architectures (eg. x64 vs ARM64)
* results for different target frameworks (eg. .NET Core 3.1 vs 5.0)

All you need to provide is:
* `--base` - path to folder/file with baseline results
* `--diff` - path to folder/file with diff results
* `--threshold`  - threshold for Statistical Test. Examples: 5%, 10ms, 100ns, 1s

Optional arguments:
* `--top` - filter the diff to top/bottom `N` results
* `--noise` - noise threshold for Statistical Test. The difference for 1.0ns and 1.1ns is 10%, but it's just a noise. Examples: 0.5ns 1ns. The default value is 0.3ns.
* `--csv` - path to exported CSV results. Optional.
* `-f|--filter` - filter the benchmarks by name using glob pattern(s). Optional.

Sample: compare the results stored in `C:\results\windows` vs `C:\results\ubuntu` using `1%` threshold and print only TOP 10.

```cmd
dotnet run --base "C:\results\windows" --diff "C:\results\ubuntu" --threshold 1% --top 10
```

**Note**: the tool supports only `*full.json` results exported by BenchmarkDotNet. This exporter is enabled by default in this repository.

## Sample results

| Slower                                                          | diff/base | Base Median (ns) | Diff Median (ns) | Modality|
| --------------------------------------------------------------- | ---------:| ----------------:| ----------------:| -------:|
| PerfLabTests.BlockCopyPerf.CallBlockCopy(numElements: 100)      |      1.60 |             9.22 |            14.76 |         |
| System.Tests.Perf_String.Trim_CharArr(s: "Test", c: [' ', ' ']) |      1.41 |             6.18 |             8.72 |         |

| Faster                              | base/diff | Base Median (ns) | Diff Median (ns) | Modality|
| ----------------------------------- | ---------:| ----------------:| ----------------:| -------:|
| System.Tests.Perf_Array.ArrayCopy3D |      1.31 |           372.71 |           284.73 |         |

If there is no difference or if there is no match (we use full benchmark names to match the benchmarks), then the results are omitted.
