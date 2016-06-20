using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PackageManagement.Provider.Utility;

namespace Microsoft.PackageManagement.NuGetProvider
{
    /// <summary>
    /// This class represents PackageDependency
    /// </summary>
    public sealed class PackageDependency
    {
        /// <summary>
        /// Name of the dependency
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The dependency version required. this may include min and max version
        /// </summary>
        public DependencyVersion DependencyVersion { get; set; }
    }
}
