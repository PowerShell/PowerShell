// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if NET6_0

using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Language;

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace Engine
{
    [BenchmarkCategory(Categories.Engine, Categories.Internal)]
    public class Compiler
    {
        private static readonly Dictionary<string, ScriptBlockAst> s_scriptBlocksDict;
        private static readonly List<string> s_functionNames;
        private ScriptBlockAst _currentAst;

        static Compiler()
        {
            string pattern = string.Format("{0}test{0}perf{0}benchmarks", Path.DirectorySeparatorChar);
            string location = typeof(Compiler).Assembly.Location;
            string testFilePath = null;

            int start = location.IndexOf(pattern, StringComparison.Ordinal);
            if (start > 0)
            {
                testFilePath = Path.Join(location.AsSpan(0, start + pattern.Length), "assets", "compiler.test.ps1");
            }

            var topScriptBlockAst = Parser.ParseFile(testFilePath, tokens: out _, errors: out _);
            var allFunctions = topScriptBlockAst.FindAll(ast => ast is FunctionDefinitionAst, searchNestedScriptBlocks: false);

            s_scriptBlocksDict = new Dictionary<string, ScriptBlockAst>(capacity: 16);
            s_functionNames = new List<string>(capacity: 16);

            foreach (FunctionDefinitionAst function in allFunctions)
            {
                s_functionNames.Add(function.Name);
                s_scriptBlocksDict.Add(function.Name, function.Body);
            }
        }

        [ParamsSource(nameof(FunctionName))]
        public string FunctionsToCompile { get; set; }

        public IEnumerable<string> FunctionName() => s_functionNames;

        [GlobalSetup(Target = nameof(CompileFunction))]
        public void GlobalSetup()
        {
            _currentAst = s_scriptBlocksDict[FunctionsToCompile];

            // Run it once to get the C# code jitted.
            // The first call to this takes relatively too long, which makes the BDN's heuristic incorrectly
            // believe that there is no need to run many ops in each iteration. However, the subsequent runs
            // of this method is much faster than the first run, and this causes 'MinIterationTime' warnings
            // to our benchmarks and make the benchmark results not reliable.
            // Calling this method once in 'GlobalSetup' is a workaround. 
            // See https://github.com/dotnet/BenchmarkDotNet/issues/837#issuecomment-828600157
            CompileFunction();
        }

        [Benchmark]
        public bool CompileFunction()
        {
            var compiledData = new CompiledScriptBlockData(_currentAst, isFilter: false);
            return compiledData.Compile(true);
        }
    }
}

#endif
