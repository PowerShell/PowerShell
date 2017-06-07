using System;
using System.Collections.Generic;
using System.Reflection;
using Codeblast;
using PSxUnit;
using Xunit.Abstractions;
using Xunit.Sdk;
using Xunit;
using Xunit.Runners;
using System.Collections.Concurrent;
using Xunit.Runner.DotNet;
using Microsoft.Extensions.DependencyModel;
using Microsoft.DotNet.InternalAbstractions;
using Microsoft.DotNet.ProjectModel;
using System.Linq;
using System.Xml.Linq;
using System.IO;
using System.Xml;
using System.Management.Automation;


public enum TestType
{
    CiFact,
    FeatureFact,
    ScenarioFact,
    All
}

namespace PSxUnit
{

    public class Options : CommandLineOptions
    {
        public Options(string[] args) : base(args) { }

        [Option(Description = "provide a list of tests to execute")]
        public string[] TestList;

        [Option(Mandatory = true, Description = "assembly which contains the test")]
        public string Assembly;

        [Option(Description = "the type of test to execute")]
        public TestType TestType = TestType.All;

        [Option(Description = "TimeOut for single test case")]
        public int TimeOut = 5000;

        [Option(Alias = "?", Description = "Get Help")]
        public bool help = false;

        public override void Help()
        {
            base.Help();
            Environment.Exit(1);
        }
        protected override void InvalidOption(string name)
        {
            Console.WriteLine("Invalid Option {0}!", name);
            Help();
        }
    }

    public class Program
    {
        public static Options opt;
        protected XunitFrontController controller;
        public static void Main(string[] args)
        {
            string appBase = @".\\";
            PowerShellAssemblyLoadContextInitializer.SetPowerShellAssemblyLoadContext(appBase);

            Program p = new Program();
            Go(args);
            //Console.ReadKey();
            Environment.Exit(0);
        }
        static List<IRunnerReporter> GetAvailableRunnerReporters()
        {
            var result = new List<IRunnerReporter>();
            var dependencyModel = DependencyContext.Load(typeof(Program).GetTypeInfo().Assembly);

            foreach (var assemblyName in dependencyModel.GetRuntimeAssemblyNames(RuntimeEnvironment.GetRuntimeIdentifier()))
            {
                try
                {
                    var assembly = Assembly.Load(assemblyName);
                    foreach (var type in assembly.DefinedTypes)
                    {
#pragma warning disable CS0618
                        if (type == null || type.IsAbstract || type == typeof(DefaultRunnerReporter).GetTypeInfo() || type == typeof(DefaultRunnerReporterWithTypes).GetTypeInfo() || type.ImplementedInterfaces.All(i => i != typeof(IRunnerReporter)))
                            continue;
#pragma warning restore CS0618
                    
                        var ctor = type.DeclaredConstructors.FirstOrDefault(c => c.GetParameters().Length == 0);
                        if (ctor == null)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"Type {type.FullName} in assembly {assembly} appears to be a runner reporter, but does not have an empty constructor.");
                            Console.ResetColor();
                            continue;
                        }

                        result.Add((IRunnerReporter)ctor.Invoke(new object[0]));
                    }
                }
                catch
                {
                    continue;
                }
            }

            return result;
        }

        public static void RunTests(Options o)
        {
            
            var nullMessage = new Xunit.NullMessageSink();
            var discoveryOptions = TestFrameworkOptions.ForDiscovery();
            using (var c = new XunitFrontController(AppDomainSupport.Denied, o.Assembly, null, false))
            {
                var tv = new TestDiscoverySink();
                var excludeTestCaseSet = new TestDiscoverySink();
                c.Find(true, tv, discoveryOptions);
                tv.Finished.WaitOne();
                foreach (var tc in tv.TestCases)
                {
                    var method = tc.TestMethod.Method;
                    var attributes = method.GetCustomAttributes(typeof(FactAttribute));
                    foreach (ReflectionAttributeInfo at in attributes)
                    {
                        var result = at.GetNamedArgument<string>("Skip");
                        if (result != null)
                        {
                            Console.WriteLine("SKIPPY! {0} because {1}", method, result);
                        }

                        if (o.TestType != TestType.All)
                        {
                            if (!at.ToString().EndsWith(o.TestType.ToString()))
                            {
                                excludeTestCaseSet.TestCases.Add(tc);
                            }
                        }
                    }
                }

                foreach (var tc in excludeTestCaseSet.TestCases)
                {
                    tv.TestCases.Remove(tc);
                }

                Console.WriteLine("TEST COUNT: {0}", tv.TestCases.Count);
                //core execution Sink

                int testCaseCount = tv.TestCases.Count;
                Stream file = new FileStream(".\\result.xml", FileMode.Create);
                int totalResult = 0;
                int totalErrors = 0;
                int totalFailed = 0;
                int totalSkipped = 0;
                for (int i = 0; i < testCaseCount; i++)
                {
                    IExecutionSink resultsSink;
                    ConcurrentDictionary<string, ExecutionSummary> completionMessages = new ConcurrentDictionary<string, ExecutionSummary>();
                    IMessageSinkWithTypes reporterMessageHandler;
                    var reporters = GetAvailableRunnerReporters();
                    var commandLine = CommandLine.Parse(reporters, @"CoreXunit.dll");
                    IRunnerLogger logger = new ConsoleRunnerLogger(!commandLine.NoColor);
                    reporterMessageHandler = MessageSinkWithTypesAdapter.Wrap(commandLine.Reporter.CreateMessageHandler(logger));
                    var xmlElement = new XElement("TestResult");
                    resultsSink = new XmlAggregateSink(reporterMessageHandler, completionMessages, xmlElement, () => true);
                    var message = new Xunit.NullMessageSink();
                    var executionOptions = TestFrameworkOptions.ForExecution();
                    c.RunTests(tv.TestCases.Take<Xunit.Abstractions.ITestCase>(1), resultsSink, executionOptions);
                    resultsSink.Finished.WaitOne(o.TimeOut);
                    tv.TestCases.RemoveAt(0);
                    totalResult++;
                    totalErrors = totalErrors + resultsSink.ExecutionSummary.Errors;
                    totalFailed = totalFailed + resultsSink.ExecutionSummary.Failed;
                    totalSkipped = totalSkipped + resultsSink.ExecutionSummary.Skipped;
                    xmlElement.Save(file);
                    file.Flush();
                }
                file.Dispose();

                Console.WriteLine("Total tests: " + totalResult);
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("Error tests: " + totalErrors);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failed tests: " + totalFailed);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Skipped tests: " + totalSkipped);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Passed tests: " + (totalResult - totalErrors - totalFailed - totalSkipped));
                Console.ResetColor();
            }
        }
        public static void Go(string[] args)
        {
            opt = new Options(args);
            RunTests(opt);
            if (opt.help)
            {
                opt.Help();

            }
            else
            {
                if (opt.Assembly != null)
                {
                    Console.WriteLine("assembly: {0}", opt.Assembly);
                }
                Console.WriteLine("TestType: {0}", opt.TestType);
                if (opt.TestList != null)
                {
                    foreach (string s in opt.TestList)
                    {
                        Console.WriteLine("list element: {0}", s);
                    }
                }
            }
        }
    }
}
