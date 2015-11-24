namespace Microsoft.PowerShell.Linux.Host
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Management.Automation;
    using System.Reflection;
    using Microsoft.Extensions.PlatformAbstractions;
    using System.Management.Automation.Runspaces;

    internal class TypeCatalog
    {
        internal static void GenerateTypeCatalog() {
            TrackPackage("System.Management.Automation");
            TrackPackage("Microsoft.PowerShell.Commands.Management");
            TrackPackage("Microsoft.PowerShell.Commands.Utility");
            TrackPackage("Microsoft.PowerShell.Security");

            TrackAssembly(typeof(System.String).GetTypeInfo().Assembly);
            System.Management.Automation.ClrHost.TypeCatalog = types;
            TrackAssembly(typeof (System.Diagnostics.FileVersionInfo).GetTypeInfo().Assembly);

            TrackType("CmdletBindingAttribute",typeof(CmdletBindingAttribute));
            TrackType("ParameterAttribute", typeof(ParameterAttribute));
            TrackType("string", typeof(string));
            TrackType("int", typeof(int));
            TrackType("long", typeof(long));
            TrackType("Hashtable", typeof(Hashtable));
            TrackType("ValidateSetAttribute", typeof(ValidateSetAttribute));
            TrackType("switch", typeof(SwitchParameter));
            TrackType("OutputTypeAttribute", typeof(OutputTypeAttribute));
            TrackType("AllowNullAttribute", typeof(AllowNullAttribute));
            TrackType("AllowEmptyStringAttribute", typeof(AllowEmptyStringAttribute));
            TrackType("PSObject", typeof(PSObject));
            TrackType("ValidateRangeAttribute", typeof(ValidateRangeAttribute));
        }

        private static void TrackPackage(string packageName)
        {
            if (!packages.Contains(packageName))
            {
                packages.Add(packageName);
                var package = PlatformServices.Default.LibraryManager.GetLibrary(packageName);
                if (package != null)
                {
                    foreach (var dependency in package.Dependencies)
                    {
                        TrackPackage(dependency);
                    }

                    foreach (var asm in package.Assemblies)
                    {
                        TrackAssembly(asm);
                    }
                }
            }
        }

        private static HashSet<string> assemblies = new HashSet<string>();
        private static HashSet<string> packages = new HashSet<string>();
        private static Dictionary<string, Assembly> types = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);

        private static void TrackAssembly(AssemblyName assemblyname)
        {
            if (!assemblies.Contains(assemblyname.FullName))
            {
                assemblies.Add(assemblyname.FullName);
                var assembly = Assembly.Load(assemblyname);
                TrackAssembly(assembly);
            }
        }

        private static void TrackAssembly(Assembly assembly)
        {
            if (assembly != null)
            {
                try
                {
                    // Console.WriteLine(assembly.Location);
                    // Console.WriteLine(assembly.ExportedTypes.Count());
                    foreach (var type in assembly.ExportedTypes)
                    {
                        TrackType(type.FullName, assembly);
                    }
                }
                catch (System.IO.FileNotFoundException)
                {
                    Debug.WriteLine("Could not track assembly {0}", assembly.Location);
                }
            }
        }

        private static void TrackType(string typeName, Type type)
        {
            TrackType(typeName, type.GetTypeInfo().Assembly);
        }

        private static void TrackType(string typeName,Assembly assembly)
        {
            if (!types.ContainsKey(typeName))
            {
                types.Add(typeName, assembly);
            }
        }
    }
}
