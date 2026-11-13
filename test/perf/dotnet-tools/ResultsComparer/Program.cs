// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using Perfolizer.Mathematics.Multimodality;
using Perfolizer.Mathematics.SignificanceTesting;
using Perfolizer.Mathematics.Thresholds;
using CommandLine;
using DataTransferContracts;
using MarkdownLog;
using Newtonsoft.Json;

namespace ResultsComparer
{
    public sealed class Program
    {
        private const string FullBdnJsonFileExtension = "full.json";

        public static void Main(string[] args)
        {
            // we print a lot of numbers here and we want to make it always in invariant way
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            Parser.Default.ParseArguments<CommandLineOptions>(args).WithParsed(Compare);
        }

        private static void Compare(CommandLineOptions args)
        {
            if (!Threshold.TryParse(args.StatisticalTestThreshold, out var testThreshold))
            {
                Console.WriteLine($"Invalid Threshold {args.StatisticalTestThreshold}. Examples: 5%, 10ms, 100ns, 1s.");
                return;
            }
            if (!Threshold.TryParse(args.NoiseThreshold, out var noiseThreshold))
            {
                Console.WriteLine($"Invalid Noise Threshold {args.NoiseThreshold}. Examples: 0.3ns 1ns.");
                return;
            }

            var notSame = GetNotSameResults(args, testThreshold, noiseThreshold).ToArray();

            if (!notSame.Any())
            {
                Console.WriteLine($"No differences found between the benchmark results with threshold {testThreshold}.");
                return;
            }

            PrintSummary(notSame);

            PrintTable(notSame, EquivalenceTestConclusion.Slower, args);
            PrintTable(notSame, EquivalenceTestConclusion.Faster, args);

            ExportToCsv(notSame, args.CsvPath);
            ExportToXml(notSame, args.XmlPath);
        }

        private static IEnumerable<(string id, Benchmark baseResult, Benchmark diffResult, EquivalenceTestConclusion conclusion)> GetNotSameResults(CommandLineOptions args, Threshold testThreshold, Threshold noiseThreshold)
        {
            foreach ((string id, Benchmark baseResult, Benchmark diffResult) in ReadResults(args)
                .Where(result => result.baseResult.Statistics != null && result.diffResult.Statistics != null)) // failures
            {
                var baseValues = baseResult.GetOriginalValues();
                var diffValues = diffResult.GetOriginalValues();

                var userTresholdResult = StatisticalTestHelper.CalculateTost(MannWhitneyTest.Instance, baseValues, diffValues, testThreshold);
                if (userTresholdResult.Conclusion == EquivalenceTestConclusion.Same)
                    continue;

                var noiseResult = StatisticalTestHelper.CalculateTost(MannWhitneyTest.Instance, baseValues, diffValues, noiseThreshold);
                if (noiseResult.Conclusion == EquivalenceTestConclusion.Same)
                    continue;

                yield return (id, baseResult, diffResult, userTresholdResult.Conclusion);
            }
        }

        private static void PrintSummary((string id, Benchmark baseResult, Benchmark diffResult, EquivalenceTestConclusion conclusion)[] notSame)
        {
            var better = notSame.Where(result => result.conclusion == EquivalenceTestConclusion.Faster);
            var worse = notSame.Where(result => result.conclusion == EquivalenceTestConclusion.Slower);
            var betterCount = better.Count();
            var worseCount = worse.Count();

            // If the baseline doesn't have the same set of tests, you wind up with Infinity in the list of diffs.
            // Exclude them for purposes of geomean.
            worse = worse.Where(x => GetRatio(x) != double.PositiveInfinity);
            better = better.Where(x => GetRatio(x) != double.PositiveInfinity);

            Console.WriteLine("summary:");

            if (betterCount > 0)
            {
                var betterGeoMean = Math.Pow(10, better.Skip(1).Aggregate(Math.Log10(GetRatio(better.First())), (x, y) => x + Math.Log10(GetRatio(y))) / better.Count());
                Console.WriteLine($"better: {betterCount}, geomean: {betterGeoMean:F3}");
            }

            if (worseCount > 0)
            {
                var worseGeoMean = Math.Pow(10, worse.Skip(1).Aggregate(Math.Log10(GetRatio(worse.First())), (x, y) => x + Math.Log10(GetRatio(y))) / worse.Count());
                Console.WriteLine($"worse: {worseCount}, geomean: {worseGeoMean:F3}");
            }

            Console.WriteLine($"total diff: {notSame.Length}");
            Console.WriteLine();
        }

        private static void PrintTable((string id, Benchmark baseResult, Benchmark diffResult, EquivalenceTestConclusion conclusion)[] notSame, EquivalenceTestConclusion conclusion, CommandLineOptions args)
        {
            var data = notSame
                .Where(result => result.conclusion == conclusion)
                .OrderByDescending(result => GetRatio(conclusion, result.baseResult, result.diffResult))
                .Take(args.TopCount ?? int.MaxValue)
                .Select(result => new
                {
                    Id = result.id.Length > 80 ? result.id.Substring(0, 80) : result.id,
                    DisplayValue = GetRatio(conclusion, result.baseResult, result.diffResult),
                    BaseMedian = result.baseResult.Statistics.Median,
                    DiffMedian = result.diffResult.Statistics.Median,
                    Modality = GetModalInfo(result.baseResult) ?? GetModalInfo(result.diffResult)
                })
                .ToArray();

            if (!data.Any())
            {
                Console.WriteLine($"No {conclusion} results for the provided threshold = {args.StatisticalTestThreshold} and noise filter = {args.NoiseThreshold}.");
                Console.WriteLine();
                return;
            }

            var table = data.ToMarkdownTable().WithHeaders(conclusion.ToString(), conclusion == EquivalenceTestConclusion.Faster ? "base/diff" : "diff/base", "Base Median (ns)", "Diff Median (ns)", "Modality");

            foreach (var line in table.ToMarkdown().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
                Console.WriteLine($"| {line.TrimStart()}|"); // the table starts with \t and does not end with '|' and it looks bad so we fix it

            Console.WriteLine();
        }

        private static IEnumerable<(string id, Benchmark baseResult, Benchmark diffResult)> ReadResults(CommandLineOptions args)
        {
            var baseFiles = GetFilesToParse(args.BasePath);
            var diffFiles = GetFilesToParse(args.DiffPath);

            if (!baseFiles.Any() || !diffFiles.Any())
                throw new ArgumentException($"Provided paths contained no {FullBdnJsonFileExtension} files.");

            var baseResults = baseFiles.Select(ReadFromFile);
            var diffResults = diffFiles.Select(ReadFromFile);

            var filters = args.Filters.Select(pattern => new Regex(WildcardToRegex(pattern), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).ToArray();

            var benchmarkIdToDiffResults = diffResults
                .SelectMany(result => result.Benchmarks)
                .Where(benchmarkResult => !filters.Any() || filters.Any(filter => filter.IsMatch(benchmarkResult.FullName)))
                .ToDictionary(benchmarkResult => benchmarkResult.FullName, benchmarkResult => benchmarkResult);

            return baseResults
                .SelectMany(result => result.Benchmarks)
                .ToDictionary(benchmarkResult => benchmarkResult.FullName, benchmarkResult => benchmarkResult) // we use ToDictionary to make sure the results have unique IDs
                .Where(baseResult => benchmarkIdToDiffResults.ContainsKey(baseResult.Key))
                .Select(baseResult => (baseResult.Key, baseResult.Value, benchmarkIdToDiffResults[baseResult.Key]));
        }

        private static void ExportToCsv((string id, Benchmark baseResult, Benchmark diffResult, EquivalenceTestConclusion conclusion)[] notSame, FileInfo csvPath)
        {
            if (csvPath == null)
                return;

            if (csvPath.Exists)
                csvPath.Delete();

            using (var textWriter = csvPath.CreateText())
            {
                foreach (var (id, baseResult, diffResult, conclusion) in notSame)
                {
                    textWriter.WriteLine($"\"{id.Replace("\"", "\"\"")}\";base;{conclusion};{string.Join(';', baseResult.GetOriginalValues())}");
                    textWriter.WriteLine($"\"{id.Replace("\"", "\"\"")}\";diff;{conclusion};{string.Join(';', diffResult.GetOriginalValues())}");
                }
            }

            Console.WriteLine($"CSV results exported to {csvPath.FullName}");
        }

        private static void ExportToXml((string id, Benchmark baseResult, Benchmark diffResult, EquivalenceTestConclusion conclusion)[] notSame, FileInfo xmlPath)
        {
            if (xmlPath == null)
            {
                Console.WriteLine("No file given");
                return;
            }

             if (xmlPath.Exists)
                xmlPath.Delete();

            using (XmlWriter writer = XmlWriter.Create(xmlPath.Open(FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write)))
            {
                writer.WriteStartElement("performance-tests");
                foreach (var (id, baseResult, diffResult, conclusion) in notSame.Where(x => x.conclusion == EquivalenceTestConclusion.Slower))
                {
                    writer.WriteStartElement("test");
                    writer.WriteAttributeString("name", id);
                    writer.WriteAttributeString("type", baseResult.Type);
                    writer.WriteAttributeString("method", baseResult.Method);
                    writer.WriteAttributeString("time", "0");
                    writer.WriteAttributeString("result", "Fail");
                    writer.WriteStartElement("failure");
                    writer.WriteAttributeString("exception-type", "Regression");
                    writer.WriteElementString("message", $"{id} has regressed, was {baseResult.Statistics.Median} is {diffResult.Statistics.Median}.");
                    writer.WriteEndElement();
                }

                foreach (var (id, baseResult, diffResult, conclusion) in notSame.Where(x => x.conclusion == EquivalenceTestConclusion.Faster))
                {
                    writer.WriteStartElement("test");
                    writer.WriteAttributeString("name", id);
                    writer.WriteAttributeString("type", baseResult.Type);
                    writer.WriteAttributeString("method", baseResult.Method);
                    writer.WriteAttributeString("time", "0");
                    writer.WriteAttributeString("result", "Skip");
                    writer.WriteElementString("reason", $"{id} has improved, was {baseResult.Statistics.Median} is {diffResult.Statistics.Median}.");
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
                writer.Flush();
            }

            Console.WriteLine($"XML results exported to {xmlPath.FullName}");
        }

        private static string[] GetFilesToParse(string path)
        {
            if (Directory.Exists(path))
                return Directory.GetFiles(path, $"*{FullBdnJsonFileExtension}", SearchOption.AllDirectories);
            else if (File.Exists(path) || !path.EndsWith(FullBdnJsonFileExtension))
                return new[] { path };
            else
                throw new FileNotFoundException($"Provided path does NOT exist or is not a {path} file", path);
        }

        // code and magic values taken from BenchmarkDotNet.Analysers.MultimodalDistributionAnalyzer
        // See http://www.brendangregg.com/FrequencyTrails/modes.html
        private static string GetModalInfo(Benchmark benchmark)
        {
            if (benchmark.Statistics.N < 12) // not enough data to tell
                return null;

            double mValue = MValueCalculator.Calculate(benchmark.GetOriginalValues());
            if (mValue > 4.2)
                return "multimodal";
            else if (mValue > 3.2)
                return "bimodal";
            else if (mValue > 2.8)
                return "several?";

            return null;
        }

        private static double GetRatio((string id, Benchmark baseResult, Benchmark diffResult, EquivalenceTestConclusion conclusion) item) => GetRatio(item.conclusion, item.baseResult, item.diffResult);

        private static double GetRatio(EquivalenceTestConclusion conclusion, Benchmark baseResult, Benchmark diffResult)
            => conclusion == EquivalenceTestConclusion.Faster
                ? baseResult.Statistics.Median / diffResult.Statistics.Median
                : diffResult.Statistics.Median / baseResult.Statistics.Median;

        private static BdnResult ReadFromFile(string resultFilePath)
        {
            try
            {
                return JsonConvert.DeserializeObject<BdnResult>(File.ReadAllText(resultFilePath));
            }
            catch (JsonSerializationException)
            {
                Console.WriteLine($"Exception while reading the {resultFilePath} file.");

                throw;
            }
        }

        // https://stackoverflow.com/a/6907849/5852046 not perfect but should work for all we need
        private static string WildcardToRegex(string pattern) => $"^{Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".")}$";
    }
}
