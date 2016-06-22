using System;
using System.Collections.Generic;
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
    public class TestDiscoveryVisitor : Xunit.TestMessageVisitor<IDiscoveryCompleteMessage>
    {
        public TestDiscoveryVisitor() {
            TestCases = new List<ITestCase>();
        }
        public List<ITestCase> TestCases { get; set; }

        protected override bool Visit(ITestCaseDiscoveryMessage testCaseDiscovered)
        {
            TestCases.Add(testCaseDiscovered.TestCase);
            return true;
        }
    }
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
                var tv = new TestDiscoveryVisitor();
                c.Find(true, tv, discoveryOptions);
                tv.Finished.WaitOne();
                var testCasesDiscovered = tv.TestCases.Count;
                Console.WriteLine("TEST COUNT: {0}", testCasesDiscovered);
                foreach(var tc in tv.TestCases) {
                    var method = tc.TestMethod.Method;
                    var attributes = method.GetCustomAttributes(typeof(FactAttribute));
                    foreach(ReflectionAttributeInfo at in attributes) 
                    { 
                        var result = at.GetNamedArgument<string>("Skip");
                        if ( result != null ) 
                        {
                            Console.WriteLine("SKIPPY! {0} because {1}",method, result);
                        }
                    }
                }
                //var tt = tv.TestCases[1];
                //Console.WriteLine("TestCaseType: {0}", tt.GetType().FullName);
                //var tm = tt.TestMethod;
                //Console.WriteLine("TestMethodType: {0}", tm.GetType().FullName);
                //var method = tm.Method;
                //Console.WriteLine("MethodType: {0}", method.GetType().FullName);
                //method.GetCustomAttributes(typeof(FactAttribute));
                //var mn = method.Name;
                //Console.WriteLine("Method name = {0}", mn);
                //Console.WriteLine("key count = {0}", tt.Traits.Keys.Count);
                //Console.WriteLine("file: {0}/{1}", tt.SourceInformation.FileName, tt.SourceInformation.LineNumber);
                //Console.WriteLine("dn: {0}", tt.DisplayName);
                //
                //Console.WriteLine("::: {0}", tm.TestClass.Class.Name);
                // Attribute[] attributes = (Attribute[])method.GetType().GetTypeInfo().GetCustomAttributes(method, true);
                // Console.WriteLine(">>> {0} ({1})", attributes.GetType().FullName, attributes.Length);
                // foreach(var attr in attributes) {
                    // Console.WriteLine("ATT: {0}", attr.GetType().FullName);
                // }
                //foreach(var attr in method.GetCustomAttributes(typeof(FactAttribute)))
                //{
                    //Console.WriteLine("ATR: {0}", attr);
                //}
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
