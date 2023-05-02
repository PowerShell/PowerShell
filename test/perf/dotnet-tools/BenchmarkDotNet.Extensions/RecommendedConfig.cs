using System.Collections.Immutable;
using System.IO;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters.Json;
using Perfolizer.Horology;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using System.Collections.Generic;
using Reporting;
using BenchmarkDotNet.Loggers;
using System.Linq;
using BenchmarkDotNet.Exporters;

namespace BenchmarkDotNet.Extensions
{
    public static class RecommendedConfig
    {
        public static IConfig Create(
            DirectoryInfo artifactsPath,
            ImmutableHashSet<string> mandatoryCategories,
            int? partitionCount = null,
            int? partitionIndex = null,
            List<string> exclusionFilterValue = null,
            List<string> categoryExclusionFilterValue = null,
            Job job = null,
            bool getDiffableDisasm = false)
        {
            if (job is null)
            {
                job = Job.Default
                    .WithWarmupCount(1) // 1 warmup is enough for our purpose
                    .WithIterationTime(TimeInterval.FromMilliseconds(250)) // the default is 0.5s per iteration, which is slightly too much for us
                    .WithMinIterationCount(15)
                    .WithMaxIterationCount(20) // we don't want to run more that 20 iterations
                    .DontEnforcePowerPlan(); // make sure BDN does not try to enforce High Performance power plan on Windows

                // See https://github.com/dotnet/roslyn/issues/42393
                job = job.WithArguments(new Argument[] { new MsBuildArgument("/p:DebugType=portable") });
            }

            var config = ManualConfig.CreateEmpty()
                .AddLogger(ConsoleLogger.Default) // log output to console
                .AddValidator(DefaultConfig.Instance.GetValidators().ToArray()) // copy default validators
                .AddAnalyser(DefaultConfig.Instance.GetAnalysers().ToArray()) // copy default analysers
                .AddExporter(MarkdownExporter.GitHub) // export to GitHub markdown
                .AddColumnProvider(DefaultColumnProviders.Instance) // display default columns (method name, args etc)
                .AddJob(job.AsDefault()) // tell BDN that this are our default settings
                .WithArtifactsPath(artifactsPath.FullName)
                .AddDiagnoser(MemoryDiagnoser.Default) // MemoryDiagnoser is enabled by default
                .AddFilter(new PartitionFilter(partitionCount, partitionIndex))
                .AddFilter(new ExclusionFilter(exclusionFilterValue))
                .AddFilter(new CategoryExclusionFilter(categoryExclusionFilterValue))
                .AddExporter(JsonExporter.Full) // make sure we export to Json
                .AddColumn(StatisticColumn.Median, StatisticColumn.Min, StatisticColumn.Max)
                .AddValidator(TooManyTestCasesValidator.FailOnError)
                .AddValidator(new UniqueArgumentsValidator()) // don't allow for duplicated arguments #404
                .AddValidator(new MandatoryCategoryValidator(mandatoryCategories))
                .WithSummaryStyle(SummaryStyle.Default.WithMaxParameterColumnWidth(36)); // the default is 20 and trims too aggressively some benchmark results

            if (Reporter.CreateReporter().InLab)
            {
                config = config.AddExporter(new PerfLabExporter());
            }

            if (getDiffableDisasm)
            {
                config = config.AddDiagnoser(CreateDisassembler());
            }

            return config;
        }

        private static DisassemblyDiagnoser CreateDisassembler()
            => new DisassemblyDiagnoser(new DisassemblyDiagnoserConfig(
                maxDepth: 1, // TODO: is depth == 1 enough?
                formatter: null, // TODO: enable diffable format
                printSource: false, // we are not interested in getting C#
                printInstructionAddresses: false, // would make the diffing hard, however could be useful to determine alignment
                exportGithubMarkdown: false,
                exportHtml: false,
                exportCombinedDisassemblyReport: false,
                exportDiff: false));
    }
}
