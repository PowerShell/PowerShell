/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

namespace Microsoft.PowerShell.CoreCLR
{
    using System;
    using System.Reflection;

    /// <summary>
    /// AssemblyExtensions
    /// </summary>
    public static class AssemblyExtensions
    {
        /// <summary>
        /// Load an assembly given its file path.
        /// </summary>
        /// <param name="assemblyPath">The path of the file that contains the manifest of the assembly.</param>
        /// <returns>The loaded assembly.</returns>
        [ObsoleteAttribute("This method is obsolete. Call Assembly.LoadFrom instead", false)]
        public static Assembly LoadFrom(string assemblyPath)
        {
            return Assembly.LoadFrom(assemblyPath);
        }
    }
}
