#if CORECLR
/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

namespace Microsoft.PowerShell.CoreCLR
{
    using System.IO;
    using System.Reflection;
    using System.Management.Automation;

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
        public static Assembly LoadFrom(string assemblyPath)
        {
            return ClrFacade.LoadFrom(assemblyPath);
        }

        /// <summary>
        /// Load an assembly given its byte stream
        /// </summary>
        /// <param name="assembly">The byte stream of assembly</param>
        /// <returns>The loaded assembly</returns>
        public static Assembly LoadFrom(Stream assembly)
        {
            return ClrFacade.LoadFrom(assembly);
        }
    }
}

#endif
