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
        public (string, string) ToLowerInvariant(string StrA, string StrB)
        {
            return (StrA.ToLowerInvariant(), StrB.ToLowerInvariant());
        }

        //[Benchmark]
        [ArgumentsSource(nameof(Data))]
        public (string, string) StringFold(string StrA, string StrB)
        {
            return (StrA.SimpleCaseFold(), StrB.SimpleCaseFold());
        }

        [Benchmark]
        [ArgumentsSource(nameof(Data))]
        public (string, string) StringFold_g3(string StrA, string StrB)
        {
            return (StrA.SimpleCaseFold_g3(), StrB.SimpleCaseFold_g3());
        }

        [Benchmark]
        [ArgumentsSource(nameof(Data))]
        public (string, string) StringFold_g2(string StrA, string StrB)
        {
            return (StrA.SimpleCaseFold_g2(), StrB.SimpleCaseFold_g2());
        }

        public IEnumerable<object[]> Data()
        {
            yield return new object[] { "CaseFolding1", "CaseFolding" };
            yield return new object[] { "ЯЯЯЯЯЯЯЯЯЯЯ1", "ЯЯЯЯЯЯЯЯЯЯЯ" };
        }
    }
}
