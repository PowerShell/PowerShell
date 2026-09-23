// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Reflection;
using System.Runtime.Loader;

namespace Test.Isolated.Init
{
    internal sealed class CustomLoadContext : AssemblyLoadContext
    {
        private readonly string _dependencyDirPath;

        public CustomLoadContext(string dependencyDirPath)
            : base("MyCustomALC", isCollectible: false)
        {
            _dependencyDirPath = dependencyDirPath;
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            // We do the simple logic here of looking for an assembly of the given name
            // in the configured dependency directory.
            string assemblyPath = Path.Combine(_dependencyDirPath, $"{assemblyName.Name}.dll");

            if (File.Exists(assemblyPath))
            {
                // The ALC must use inherited methods to load assemblies.
                // Assembly.Load*() won't work here.
                return LoadFromAssemblyPath(assemblyPath);
            }

            // For other assemblies, return null to allow other resolutions to continue.
            return null;
        }
    }

    public class Init : IModuleAssemblyInitializer, IModuleAssemblyCleanup
    {
        private static readonly CustomLoadContext s_context;
        private static readonly HashSet<string> s_moduleAssemblies;

        static Init()
        {
            string dependencyDirPath = Path.Combine(Path.GetDirectoryName(typeof(Init).Assembly.Location), "Dependencies");
            s_context = new CustomLoadContext(dependencyDirPath);
            s_moduleAssemblies = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Test.Isolated.Nested",
                "Test.Isolated.Root"
            };
        }

        public void OnImport()
        {
            // Add the Resolving event handler here.
            AssemblyLoadContext.Default.Resolving += ResolveAlcEngine;
        }

        public void OnRemove(PSModuleInfo psModuleInfo)
        {
            // Remove the Resolving event handler here.
            AssemblyLoadContext.Default.Resolving -= ResolveAlcEngine;
        }

        private static Assembly ResolveAlcEngine(AssemblyLoadContext defaultAlc, AssemblyName assemblyToResolve)
        {
            // We only want to resolve our module assemblies here.
            if (s_moduleAssemblies.Contains(assemblyToResolve.Name))
            {
                // This is where the nested module 'Test.Isolated.Nested.dll' and the root module 'Test.Isolated.Root.dll'
                // gets loaded into our custom ALC and then passed through into PowerShell's ALC.
                return s_context.LoadFromAssemblyName(assemblyToResolve);
            }

            // Let the resolution chain continue for other assemblies.
            return null;
        }
    }
}
