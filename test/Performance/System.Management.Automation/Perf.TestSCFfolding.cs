// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

using System.Collections.Generic;
using System.Management.Automation.Unicode;

namespace System.Management.Automation.Unicode.Tests
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //var summary = BenchmarkRunner.Run<IntroBenchmarkBaseline>();

            // Run: dotnet run -c release --AllCategories=StringFold
            // Run: dotnet run -c release --AllCategories=StringCompareFolded
            var summary = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
            Console.WriteLine("CaseFolding1".Fold());
            Console.WriteLine("ЯЯЯЯЯЯЯЯЯЯЯ1".Fold());
            Console.WriteLine(SimpleCaseFolding.CompareFolded("CaseFolding1", "ЯЯЯЯЯЯЯЯЯЯЯ1"));
        }
    }

    public class IntroBenchmarkBaseline
    {
        //[Benchmark]
        [Benchmark(Baseline = true)]
        [ArgumentsSource(nameof(Data))]
        public (string, string) ToLowerInvariant(string StrA, string StrB)
        {
            return (StrA.ToLowerInvariant(), StrB.ToLowerInvariant());
        }

        //[Benchmark]
        //[BenchmarkCategory("StringFold")]
        //[ArgumentsSource(nameof(Data))]
        public (string, string) TestStringFoldBase(string StrA, string StrB)
        {
            return (StrA.FoldBase(), StrB.FoldBase());
        }

        [Benchmark]
        [ArgumentsSource(nameof(Data))]
        public (string, string) StringFold(string StrA, string StrB)
        {
            return (StrA.Fold(), StrB.Fold());
        }

        public IEnumerable<object[]> Data()
        {
            yield return new object[] { "CaseFolding1", "CaseFolding" };
            yield return new object[] { "ЯЯЯЯЯЯЯЯЯЯЯ1", "ЯЯЯЯЯЯЯЯЯЯЯ" };
        }
    }
}
