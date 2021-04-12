// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation.Language;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace Sma.Language
{
    [BenchmarkCategory(Categories.Parser)]
    public class Perf_Parser
    {   
        [Benchmark]
        public Ast Parse_Empty_NamedBlocks()
        {
            return Parser.ParseInput("begin {} process {} end {}", out _, out _);
        }

        [Benchmark]
        public Ast Parse_UsingStatement()
        {
            string script = @"
                using module moduleA
                using Assembly assemblyA
                using namespace System.IO";
            return Parser.ParseInput(script, out _, out _);
        }
    }
}
