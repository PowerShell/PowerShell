// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if NET6_0

using System;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace Engine
{
    [BenchmarkCategory(Categories.Engine, Categories.Internal)]
    public class Compiler
    {
        private ScriptBlockAst scriptBlockAst;

        #region Compile-Script

        [GlobalSetup(Target = nameof(CompileBuildModule))]
        public void GlobalSetup()
        {
            char dirSeparator = Path.DirectorySeparatorChar;
            string pattern = $"{dirSeparator}test{dirSeparator}perf{dirSeparator}";
            string location = typeof(Compiler).Assembly.Location;
            string buildModulePath = null;

            int start = location.IndexOf(pattern, StringComparison.Ordinal);
            if (start > 0)
            {
                ReadOnlySpan<char> psRepoRootDir = location.AsSpan(0, start);
                buildModulePath = Path.Join(psRepoRootDir, "build.psm1".AsSpan());
            }

            if (!File.Exists(buildModulePath))
            {
                throw new NotSupportedException("Cannot find 'build.psm1'. The 'Compiler' benchmarks depend on scripts in PowerShell repo, so please run benchmarks from with the PowerShell repo directory.");
            }

            scriptBlockAst = Parser.ParseFile(buildModulePath, tokens: out _, errors: out _);

            // Run it once to get the C# code jitted and the script compiled.
            // The first call to this takes relatively too long, which makes the BDN's heuristic incorrectly
            // believe that there is no need to run many ops in each interation. However, the subsequent runs
            // of this method is much faster than the first run, and this causes 'MinIterationTime' warnings
            // to our benchmarks and make the benchmark results not reliable.
            // Calling this method once in 'GlobalSetup' is a workaround. 
            // See https://github.com/dotnet/BenchmarkDotNet/issues/837#issuecomment-828600157
            CompileBuildModule();
        }

        [Benchmark]
        public bool CompileBuildModule()
        {
            var compiledData = new CompiledScriptBlockData(scriptBlockAst, isFilter: false);
            return compiledData.Compile(true);
        }

        #endregion
    }
}

#endif
