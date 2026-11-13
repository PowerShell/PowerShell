// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using CommandLine;
using CommandLine.Text;

namespace ResultsComparer
{
    public class CommandLineOptions
    {
        [Option("base", HelpText = "Path to the folder/file with base results.")]
        public string BasePath { get; set; }

        [Option("diff", HelpText = "Path to the folder/file with diff results.")]
        public string DiffPath { get; set; }

        [Option("threshold", Required = true, HelpText = "Threshold for Statistical Test. Examples: 5%, 10ms, 100ns, 1s.")]
        public string StatisticalTestThreshold { get; set; }

        [Option("noise", HelpText = "Noise threshold for Statistical Test. The difference for 1.0ns and 1.1ns is 10%, but it's just a noise. Examples: 0.5ns 1ns.", Default = "0.3ns" )]
        public string NoiseThreshold { get; set; }

        [Option("top", HelpText = "Filter the diff to top/bottom N results. Optional.")]
        public int? TopCount { get; set; }

        [Option("csv", HelpText = "Path to exported CSV results. Optional.")]
        public FileInfo CsvPath { get; set; }

        [Option("xml", HelpText = "Path to exported XML results. Optional.")]
        public FileInfo XmlPath { get; set; }

        [Option('f', "filter", HelpText = "Filter the benchmarks by name using glob pattern(s). Optional.")]
        public IEnumerable<string> Filters { get; set; }

        [Usage(ApplicationAlias = "")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example(@"Compare the results stored in 'C:\results\win' (base) vs 'C:\results\unix' (diff) using 5% threshold.",
                    new CommandLineOptions { BasePath = @"C:\results\win", DiffPath = @"C:\results\unix", StatisticalTestThreshold = "5%" });
                yield return new Example(@"Compare the results stored in 'C:\results\win' (base) vs 'C:\results\unix' (diff) using 5% threshold and show only top/bottom 10 results.",
                    new CommandLineOptions { BasePath = @"C:\results\win", DiffPath = @"C:\results\unix", StatisticalTestThreshold = "5%", TopCount = 10 });
                yield return new Example(@"Compare the results stored in 'C:\results\win' (base) vs 'C:\results\unix' (diff) using 5% threshold and 0.5ns noise filter.",
                    new CommandLineOptions { BasePath = @"C:\results\win", DiffPath = @"C:\results\unix", StatisticalTestThreshold = "5%", NoiseThreshold = "0.5ns" });
                yield return new Example(@"Compare the System.Math benchmark results stored in 'C:\results\ubuntu16' (base) vs 'C:\results\ubuntu18' (diff) using 5% threshold.",
                    new CommandLineOptions { Filters = new[] { "System.Math*" }, BasePath = @"C:\results\win", DiffPath = @"C:\results\unix", StatisticalTestThreshold = "5%" });
            }
        }
    }
}
