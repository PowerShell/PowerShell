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
            Console.WriteLine("CaseFolding1".SimpleCaseFold());
            Console.WriteLine("ЯЯЯЯЯЯЯЯЯЯЯ1".SimpleCaseFold());
            Console.WriteLine(SimpleCaseFolding.CompareUsingSimpleCaseFolding("CaseFolding1", "ЯЯЯЯЯЯЯЯЯЯЯ1"));
        }
    }

    //[DisassemblyDiagnoser(printAsm: true, printSource: true)]
    public class IntroBenchmarkBaseline
    {
        //[Benchmark]
        [Benchmark(Baseline = true)]
        [ArgumentsSource(nameof(Data))]
        public int CoreFXCompareOrdinal(string StrA, string StrB)
        {
            return String.CompareOrdinal(StrA, StrB);
        }

        //[Benchmark]
        [ArgumentsSource(nameof(Data))]
        public int CoreFXCompareOrdinalIgnoreCase(string StrA, string StrB)
        {
            return String.Compare(StrA, StrB, StringComparison.OrdinalIgnoreCase);
        }

        //[Benchmark]
        [ArgumentsSource(nameof(Data))]
        public int SimpleCaseFoldCompare_g3(string StrA, string StrB)
        {
            var comparer = new StringComparerUsingSimpleCaseFolding_g3();
            return comparer.Compare(StrA, StrB);
        }

        [Benchmark]
        [ArgumentsSource(nameof(Data))]
        public int SimpleCaseFoldCompare_g2(string StrA, string StrB)
        {
            var comparer = new StringComparerUsingSimpleCaseFolding_g2();
            return comparer.Compare(StrA, StrB);
        }

        public IEnumerable<object[]> Data()
        {
            yield return new object[] { "CaseFolding1", "cASEfOLDING" };
            yield return new object[] { "ЯЯЯЯЯЯЯЯЯЯЯ1", "яяяяяяяяяяя" };
        }
    }
}
