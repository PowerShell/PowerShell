// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation.Language;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace Engine
{
    [BenchmarkCategory(Categories.Engine, Categories.Public)]
    public class Parsing
    {
        [Benchmark]
        public Ast UsingStatement()
        {
            const string Script = @"
                using module moduleA
                using Assembly assemblyA
                using namespace System.IO";
            return Parser.ParseInput(Script, out _, out _);
        }
    }
}
