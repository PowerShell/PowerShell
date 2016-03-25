using System;
using System.IO;
using System.Linq;
using NuGet.Frameworks;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.ProjectModel.Graph;
using Microsoft.Extensions.DependencyModel.Resolution;

namespace ConsoleApplication
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // The TypeCatalogGen project takes this as input
            var outputPath = "../TypeCatalogGen/powershell.inc";

            // Get a context for our top level project
            var context = ProjectContext.Create("../Microsoft.PowerShell.Linux.Host", NuGetFramework.Parse("netstandardapp1.5"));

            System.IO.File.WriteAllLines(outputPath,
                                         // Get the target for the current runtime
                                         from t in context.LockFile.Targets where t.RuntimeIdentifier == Constants.RuntimeIdentifier
                                         // Get the packages (not projects)
                                         from x in t.Libraries where x.Type == "package"
                                         // Get the real reference assemblies
                                         from y in x.CompileTimeAssemblies where y.Path.EndsWith(".dll")
                                         // Construct the path to the assemblies
                                         select $"{context.PackagesDirectory}/{x.Name}/{x.Version}/{y.Path};");

            Console.WriteLine($"List of reference assemblies written to {outputPath}");
        }
    }
}
