using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PackageManagement.NuGetProvider
{
    /// <summary>
    /// This class represents a set of dependencies within a framework
    /// </summary>
    public sealed class PackageDependencySet
    {
        /// <summary>
        /// The target framework of this dependency set
        /// </summary>
        public string TargetFramework { get; set; }

        /// <summary>
        /// The list of dependencies
        /// </summary>
        public List<PackageDependency> Dependencies { get; set; }
    }
}
