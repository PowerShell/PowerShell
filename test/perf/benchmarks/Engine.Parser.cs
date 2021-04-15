// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation.Language;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace Engine.Scripting
{
    [BenchmarkCategory(Categories.Engine, Categories.Public)]
    public class Parser_Public
    {
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
