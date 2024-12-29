## Tools

The tools here are copied from [dotnet/performance](https://github.com/dotnet/performance),
the performance testing repository for the .NET runtime and framework libraries.

- [BenchmarkDotNet.Extensions](https://github.com/dotnet/performance/tree/main/src/harness/BenchmarkDotNet.Extensions)
  - It provides the needed extensions for running benchmarks,
    such as the `RecommendedConfig` which defines the set of recommended configurations for running the dotnet benchmarks.
- [Reporting](https://github.com/dotnet/performance/tree/main/src/tools/Reporting)
  - It provides additional result reporting support
    which may be useful to us when running our benchmarks in lab.
- [ResultsComparer](https://github.com/dotnet/performance/tree/main/src/tools/ResultsComparer)
  - It's a tool for comparing different benchmark results.
    It's very useful to show the regression of new changes by comparing its benchmark results to the baseline results.
