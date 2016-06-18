using System;
using System.Reflection;
using Codeblast;
using PSxUnit;
using Xunit.Abstractions;
using Xunit.Sdk;
using Xunit;
using Xunit.Runners;

public enum TestType {
    CiFact,
    CiFeature,
    CiScenario,
    All
}

namespace PSxUnit
{
    public class Options : CommandLineOptions 
    {
        public Options(string[]args) : base(args) { }

        [Option(Description = "provide a list of tests to execute")]
        public string[] TestList;

        [Option(Mandatory = true, Description = "assembly which contains the test")]
        public string Assembly;

        [Option(Description = "the type of test to execute")]
        public TestType TestType = TestType.All; 

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
        protected Options opt;
        protected XunitFrontController controller;
        public static void Main(string[] args)
        {
            Program p = new Program();
            p.Go(args);
            Environment.Exit(0);
        }

        public void GetTests(Options o)
        {
            // JWT - 
            // Console.WriteLine(typeof(AssemblyRunner).GetTypeInfo().FullName);
            // Console.WriteLine(typeof(XunitFrontController).GetTypeInfo().FullName);
            var nullMessage = new Xunit.NullMessageSink();
            var discoveryOptions = TestFrameworkOptions.ForDiscovery();
            using(var c = new XunitFrontController(AppDomainSupport.Denied, o.Assembly, null, false))
            {
               //  foreach(var t in 
                c.Find(true, nullMessage, discoveryOptions);
                foreach(var t in c.GetType().GetTypeInfo().GetMembers()) {
                    Console.WriteLine(t);
                }
                foreach(var p in c.GetType().GetTypeInfo().GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic))
                {
                    Console.WriteLine("{0}:{1}", p.Name,p.GetValue(c));
                }
            }
        }
        public void Go(string[] args)
        {
            opt = new Options(args);
            GetTests(opt);
            if (opt.help)
            {
                opt.Help();
            }
            else
            {
                if ( opt.Assembly != null ) {
                    Console.WriteLine("assembly: {0}", opt.Assembly);
                }
                Console.WriteLine("TestType: {0}", opt.TestType);
                if ( opt.TestList != null )
                {
                    foreach(string s in opt.TestList)
                    {
                        Console.WriteLine("list element: {0}", s);
                    }
                }
            }
        }
    }
}
