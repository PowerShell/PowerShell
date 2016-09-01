using System;
using System.IO;
using System.Linq;
using NuGet.Frameworks;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.ProjectModel.Graph;
using Microsoft.Extensions.DependencyModel.Resolution;

namespace TypeCatalogParser
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // These are packages that are not part of .NET Core and must be excluded
            string[] excludedPackages = {
                "Microsoft.Management.Infrastructure",
                "Microsoft.Management.Infrastructure.Native",
                "Microsoft.mshtml"
            };

            // The TypeCatalogGen project takes this as input
            var outputPath = "../TypeCatalogGen/powershell.inc";

            // Get a context for our top level project
            var context = ProjectContext.Create("../Microsoft.PowerShell.SDK", NuGetFramework.Parse("netstandard1.6"));

            System.IO.File.WriteAllLines(outputPath,
                                         // Get the target for the current runtime
                                         from t in context.LockFile.Targets where t.RuntimeIdentifier == context.RuntimeIdentifier
                                         // Get the packages (not projects)
                                         from x in t.Libraries where (x.Type == "package" && !excludedPackages.Contains(x.Name))
                                         // Get the real reference assemblies
                                         from y in x.CompileTimeAssemblies where y.Path.EndsWith(".dll")
                                         // Construct the path to the assemblies
                                         select $"{context.PackagesDirectory}/{x.Name.ToLower()}/{x.Version}/{y.Path};");

            Console.WriteLine($"List of reference assemblies written to {outputPath}");
        }
    }
}
