using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Xunit.Runner.DotNet
{
    public class CommandLine
    {
        readonly Stack<string> arguments = new Stack<string>();
        readonly IReadOnlyList<IRunnerReporter> reporters;

        protected CommandLine(IReadOnlyList<IRunnerReporter> reporters, string[] args, Predicate<string> fileExists = null)
        {
            this.reporters = reporters;

            if (fileExists == null)
                fileExists = fileName => File.Exists(fileName);

            for (var i = args.Length - 1; i >= 0; i--)
                arguments.Push(args[i]);

            DesignTimeTestUniqueNames = new List<string>();
            Project = Parse(fileExists);
            Reporter = reporters.FirstOrDefault(r => r.IsEnvironmentallyEnabled) ?? Reporter ?? new DefaultRunnerReporterWithTypes();
        }

        public AppDomainSupport? AppDomains { get; set; }

        public bool DiagnosticMessages { get; set; }

        public bool Debug { get; set; }

        public bool DesignTime { get; set; }

        // Used with --designtime - to specify specific tests by uniqueId.
        public List<string> DesignTimeTestUniqueNames { get; private set; }

        public bool FailSkips { get; protected set; }

        public bool List { get; set; }

        public int? MaxParallelThreads { get; set; }

        public bool NoColor { get; set; }

        public bool NoLogo { get; set; }

        public XunitProject Project { get; protected set; }

        public bool? ParallelizeAssemblies { get; set; }

        public bool? ParallelizeTestCollections { get; set; }

        public IRunnerReporter Reporter { get; protected set; }

        public bool Wait { get; protected set; }

        public int? Port { get; set; }

        public bool WaitCommand { get; set; }

        static XunitProject GetProjectFile(List<Tuple<string, string>> assemblies)
        {
            var result = new XunitProject();

            foreach (var assembly in assemblies)
                result.Add(new XunitProjectAssembly2
                {
                    AssemblyFilename = Path.GetFullPath(assembly.Item1),
                    ConfigFilename = assembly.Item2 != null ? Path.GetFullPath(assembly.Item2) : null,
                });

            return result;
        }

        static void GuardNoOptionValue(KeyValuePair<string, string> option)
        {
            if (option.Value != null)
                throw new ArgumentException(string.Format("error: unknown command line option: {0}", option.Value));
        }

        public static CommandLine Parse(IReadOnlyList<IRunnerReporter> reporters, params string[] args)
            => new CommandLine(reporters, args);

        protected XunitProject Parse(Predicate<string> fileExists)
        {
            if (arguments.Count == 0)
                throw new ArgumentException("must specify at least one assembly");

            var assemblyFile = arguments.Pop();
            string configFile = null;
            if (arguments.Count > 0)
            {
                var value = arguments.Peek();
                if (!value.StartsWith("-") && value.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    configFile = arguments.Pop();
                    if (!fileExists(configFile))
                        throw new ArgumentException(string.Format("config file not found: {0}", configFile));
                }
            }

            var assemblies = new List<Tuple<string, string>> { Tuple.Create(assemblyFile, configFile) };
            var project = GetProjectFile(assemblies);

            while (arguments.Count > 0)
            {
                var option = PopOption(arguments);
                var optionName = option.Key.ToLowerInvariant();

                if (!optionName.StartsWith("-"))
                    throw new ArgumentException(string.Format("unknown command line option: {0}", option.Key));

                optionName = optionName.Substring(1);

                if (optionName == "nologo")
                {
                    GuardNoOptionValue(option);
                    NoLogo = true;
                }
                else if (optionName == "failskips")
                {
                    GuardNoOptionValue(option);
                    FailSkips = true;
                }
                else if (optionName == "nocolor")
                {
                    GuardNoOptionValue(option);
                    NoColor = true;
                }
                else if (optionName == "appdomain")
                {
                    if (option.Value == null)
                        throw new ArgumentException("missing argument for -appdomain");

                    switch (option.Value)
                    {
                        case "on":
#if NETCOREAPP1_0
                            throw new ArgumentException("AppDomain support is not available on .NET Core");
#else
                            AppDomains = AppDomainSupport.Required;
                            break;
#endif

                        case "off":
                            AppDomains = AppDomainSupport.Denied;
                            break;

                        default:
                            throw new ArgumentException("incorrect argument value for -appdomain (must be 'on' or 'off')");
                    }
                }
                else if (optionName == "debug")
                {
                    GuardNoOptionValue(option);
                    Debug = true;
                }
                else if (optionName == "wait")
                {
                    GuardNoOptionValue(option);
                    Wait = true;
                }
                else if (optionName == "diagnostics")
                {
                    GuardNoOptionValue(option);
                    DiagnosticMessages = true;
                }
                else if (optionName == "maxthreads")
                {
                    if (option.Value == null)
                        throw new ArgumentException("missing argument for -maxthreads");

                    switch (option.Value)
                    {
                        case "default":
                            MaxParallelThreads = 0;
                            break;

                        case "unlimited":
                            MaxParallelThreads = -1;
                            break;

                        default:
                            int threadValue;
                            if (!int.TryParse(option.Value, out threadValue) || threadValue < 1)
                                throw new ArgumentException("incorrect argument value for -maxthreads (must be 'default', 'unlimited', or a positive number)");

                            MaxParallelThreads = threadValue;
                            break;
                    }
                }
                else if (optionName == "parallel")
                {
                    if (option.Value == null)
                        throw new ArgumentException("missing argument for -parallel");

                    ParallelismOption parallelismOption;
                    if (!Enum.TryParse(option.Value, ignoreCase: true, result: out parallelismOption))
                        throw new ArgumentException("incorrect argument value for -parallel");

                    switch (parallelismOption)
                    {
                        case ParallelismOption.All:
                            ParallelizeAssemblies = true;
                            ParallelizeTestCollections = true;
                            break;

                        case ParallelismOption.Assemblies:
                            ParallelizeAssemblies = true;
                            ParallelizeTestCollections = false;
                            break;

                        case ParallelismOption.Collections:
                            ParallelizeAssemblies = false;
                            ParallelizeTestCollections = true;
                            break;

                        case ParallelismOption.None:
                        default:
                            ParallelizeAssemblies = false;
                            ParallelizeTestCollections = false;
                            break;
                    }
                }
                else if (optionName == "noshadow")
                {
                    GuardNoOptionValue(option);
                    foreach (var assembly in project.Assemblies)
                        assembly.Configuration.ShadowCopy = false;
                }
                else if (optionName == "trait")
                {
                    if (option.Value == null)
                        throw new ArgumentException("missing argument for -trait");

                    var pieces = option.Value.Split('=');
                    if (pieces.Length != 2 || string.IsNullOrEmpty(pieces[0]) || string.IsNullOrEmpty(pieces[1]))
                        throw new ArgumentException("incorrect argument format for -trait (should be \"name=value\")");

                    var name = pieces[0];
                    var value = pieces[1];
                    project.Filters.IncludedTraits.Add(name, value);
                }
                else if (optionName == "notrait")
                {
                    if (option.Value == null)
                        throw new ArgumentException("missing argument for -notrait");

                    var pieces = option.Value.Split('=');
                    if (pieces.Length != 2 || string.IsNullOrEmpty(pieces[0]) || string.IsNullOrEmpty(pieces[1]))
                        throw new ArgumentException("incorrect argument format for -notrait (should be \"name=value\")");

                    var name = pieces[0];
                    var value = pieces[1];
                    project.Filters.ExcludedTraits.Add(name, value);
                }
                else if (optionName == "class")
                {
                    if (option.Value == null)
                        throw new ArgumentException("missing argument for -class");

                    project.Filters.IncludedClasses.Add(option.Value);
                }
                else if (optionName == "method")
                {
                    if (option.Value == null)
                        throw new ArgumentException("missing argument for -method");

                    project.Filters.IncludedMethods.Add(option.Value);
                }
                else if (optionName == "namespace")
                {
                    if (option.Value == null)
                        throw new ArgumentException("missing argument for -namespace");

                    project.Filters.IncludedNameSpaces.Add(option.Value);
                }
                // BEGIN: Special command line switches for dotnet <=> Visual Studio integration
                else if (optionName == "test" || optionName == "-test")
                {
                    if (option.Value == null)
                        throw new ArgumentException("missing argument for --test");

                    DesignTimeTestUniqueNames.Add(option.Value);
                }
                else if (optionName == "list" || optionName == "-list")
                {
                    GuardNoOptionValue(option);
                    List = true;
                }
                else if (optionName == "designtime" || optionName == "-designtime")
                {
                    GuardNoOptionValue(option);
                    DesignTime = true;
                }
                else if (optionName == "port" || optionName == "-port")
                {
                    if (option.Value == null)
                    {
                        throw new ArgumentException("missing argument for -port");
                    }

                    int port;
                    if (!int.TryParse(option.Value, out port) || port < 0)
                    {
                        throw new ArgumentException("incorrect argument value for -port (must be a positive number)");
                    }

                    Port = port;
                }
                else if (optionName == "wait-command" || optionName == "-wait-command")
                {
                    GuardNoOptionValue(option);
                    WaitCommand = true;
                }
                // END: Special command line switches for dotnet <=> Visual Studio integration
                else
                {
                    // Might be a reporter...
                    var reporter = reporters.FirstOrDefault(r => string.Equals(r.RunnerSwitch, optionName, StringComparison.OrdinalIgnoreCase));
                    if (reporter != null)
                    {
                        GuardNoOptionValue(option);
                        if (Reporter != null)
                            throw new ArgumentException("only one reporter is allowed");

                        Reporter = reporter;
                    }
                    // ...or an result output file
                    else
                    {
                        if (!TransformFactory.AvailableTransforms.Any(t => t.CommandLine.Equals(optionName, StringComparison.OrdinalIgnoreCase)))
                            throw new ArgumentException($"unknown option: {option.Key}");

                        if (option.Value == null)
                            throw new ArgumentException(string.Format("missing filename for {0}", option.Key));

                        project.Output.Add(optionName, option.Value);
                    }
                }
            }

            if (WaitCommand && !Port.HasValue)
                throw new ArgumentException("when specifing --wait-command you must also pass a port using --port");

            return project;
        }

        static KeyValuePair<string, string> PopOption(Stack<string> arguments)
        {
            var option = arguments.Pop();
            string value = null;

            if (arguments.Count > 0 && !arguments.Peek().StartsWith("-"))
                value = arguments.Pop();

            return new KeyValuePair<string, string>(option, value);
        }
    }
}
