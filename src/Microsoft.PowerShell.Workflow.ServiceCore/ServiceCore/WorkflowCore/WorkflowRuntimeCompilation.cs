/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

namespace Microsoft.PowerShell.Workflow
{
    using System;
    using System.Globalization;
    using System.Diagnostics.CodeAnalysis;
    using System.Management.Automation.Tracing;

#if _NOTARMBUILD_
    using Microsoft.Build.Evaluation;
    using Microsoft.Build.Logging;
#endif
    using System.Activities.XamlIntegration;
    using System.IO;
    using System.Xaml;
    using System.Xml;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;

    /// <summary>
    /// This class is responsible for compiling dependent workflows.
    /// </summary>
    internal class WorkflowRuntimeCompilation
    {
        private static readonly PowerShellTraceSource Tracer = PowerShellTraceSourceFactory.GetTraceSource();

        /// <summary>
        /// The template project which is used for default project values.
        /// </summary>
        private const string Template_Project = @"<?xml version=""1.0"" encoding=""utf-8""?><Project ToolsVersion=""4.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003""><PropertyGroup><Configuration Condition="" '$(Configuration)' == '' "">Release</Configuration><Platform>AnyCPU</Platform><ProductVersion>10.0</ProductVersion><SchemaVersion>2.0</SchemaVersion><OutputType>Library</OutputType><AppDesignerFolder>Properties</AppDesignerFolder><TargetFrameworkVersion>v4.5</TargetFrameworkVersion><TargetFrameworkProfile></TargetFrameworkProfile><FileAlignment>512</FileAlignment></PropertyGroup><PropertyGroup><DebugSymbols>true</DebugSymbols><DebugType>full</DebugType><Optimize>false</Optimize><OutputPath>bin\Debug\</OutputPath><DefineConstants>DEBUG;TRACE</DefineConstants><ErrorReport>prompt</ErrorReport><WarningLevel>4</WarningLevel></PropertyGroup><PropertyGroup><DebugType>pdbonly</DebugType><Optimize>true</Optimize><OutputPath>bin\Release\</OutputPath><DefineConstants>TRACE</DefineConstants><ErrorReport>prompt</ErrorReport><WarningLevel>4</WarningLevel></PropertyGroup><ItemGroup><Reference Include=""Microsoft.CSharp"" /><Reference Include=""System"" /><Reference Include=""System.Activities"" /><Reference Include=""System.Core"" /><Reference Include=""System.Data"" /><Reference Include=""System.ServiceModel"" /><Reference Include=""System.ServiceModel.Activities"" /><Reference Include=""System.Xaml"" /><Reference Include=""System.Xml"" /><Reference Include=""System.Xml.Linq"" /><Reference Include=""System.Management"" /><Reference Include=""System.Management.Automation"" /><Reference Include=""Microsoft.PowerShell.Workflow.ServiceCore"" /></ItemGroup><Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" /><!-- To modify your build process, add your task inside one of the targets below and uncomment it.  Other similar extension points exist, see Microsoft.Common.targets. <Target Name=""BeforeBuild""></Target><Target Name=""AfterBuild""></Target>--></Project>";

        /// <summary>
        /// The runtime generated project name.
        /// </summary>
        internal string ProjectName { get; set; }

        /// <summary>
        /// The runtime generated Project folder path.
        /// </summary>
        internal string ProjectFolderPath { get; set; }

        /// <summary>
        /// The runtime generated Project file path (.csprj file).
        /// </summary>
        internal string ProjectFilePath { get; set; }

        /// <summary>
        /// The runtime generated build log path.
        /// </summary>
        internal string BuildLogPath { get; set; }

        /// <summary>
        /// The runtime generated assembly name.
        /// </summary>
        internal string AssemblyName { get; set; }

        /// <summary>
        /// The runtime generated assembly path.
        /// </summary>
        internal string AssemblyPath { get; set; }

        /// <summary>
        /// The returned code of project.Build.
        /// </summary>
        internal bool BuildReturnedCode { get; set; }

        private string _projectRoot;
        /// <summary>
        /// Default constructor.
        /// </summary>
        internal WorkflowRuntimeCompilation()
        {
            if (IsRunningOnProcessorArchitectureARM())
            {
                Tracer.WriteMessage("The workflow Calling workflow is not supported so throwing the exception.");
                throw new NotSupportedException(Resources.WFCallingWFNotSupported);
            }

            this.ProjectName = "Workflow_" + Guid.NewGuid().ToString("N");
            this._projectRoot = Path.Combine(Path.GetTempPath(), @"PSWorkflowCompilation\"+this.ProjectName);
            this.ProjectFolderPath = Path.Combine(this._projectRoot, "Project");
            this.ProjectFilePath = Path.Combine(this.ProjectFolderPath, "RuntimeProject.csproj");
            this.BuildLogPath = Path.Combine(this.ProjectFolderPath, "Build.Log");
            this.AssemblyName = this.ProjectName;
            this.AssemblyPath = Path.Combine(this._projectRoot, this.ProjectName + ".dll");
        }

        /// <summary>
        /// Compiling the workflow xamls into the assembly.
        /// </summary>
        internal void Compile(List<string> dependentWorkflows, Dictionary<string, string> requiredAssemblies)
        {
            if (IsRunningOnProcessorArchitectureARM())
            {
                Tracer.WriteMessage("The workflow Calling workflow is not supported so throwing the exception.");
                throw new NotSupportedException(Resources.WFCallingWFNotSupported);
            }

// Note that the _NOT_ARMBUILD_ flag is not a global build flag and needs to be set in the corresponding sources.inc file as appropriate.
#if _NOTARMBUILD_
            DirectoryInfo folder = new DirectoryInfo(this.ProjectFolderPath);
            folder.Create();

            List<string> workflowFiles = new List<string>();
            try
            {
                // Dump the files
                foreach (string dependentWorkflow in dependentWorkflows)
                {
                    string newFileName = Path.Combine(this.ProjectFolderPath, Path.GetRandomFileName() + ".xaml");
                    File.WriteAllText(newFileName, dependentWorkflow);
                    workflowFiles.Add(newFileName);
                }

                File.WriteAllText(this.ProjectFilePath, Template_Project);
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
                throw;
            }

            using (ProjectCollection projects = new ProjectCollection())
            {
                Project project = projects.LoadProject(this.ProjectFilePath);

                project.SetProperty("AssemblyName", this.AssemblyName);

                HashSet<string> Assemblies = new HashSet<string>();

                foreach (string file in workflowFiles)
                {
                    project.AddItem("XamlAppDef", file);

                    XamlXmlReader reader = new XamlXmlReader(XmlReader.Create(file), new XamlSchemaContext());
                    using (reader)
                    {
                        while (reader.Read())
                        {
                            if (reader.NodeType == XamlNodeType.NamespaceDeclaration)
                            {
                                string _namespace = reader.Namespace.Namespace.ToLowerInvariant();

                                if (_namespace.IndexOf("assembly=", StringComparison.OrdinalIgnoreCase) > -1)
                                {
                                    List<string> filters = new List<string>();
                                    filters.Add("assembly=");
                                    string[] results = _namespace.Split(filters.ToArray(), StringSplitOptions.RemoveEmptyEntries);
                                    if (results.Length > 1 && !string.IsNullOrEmpty(results[1]))
                                    {
                                        string requiredAssemblyLocation;
                                        if (requiredAssemblies != null && requiredAssemblies.Count > 0 && requiredAssemblies.TryGetValue(results[1], out requiredAssemblyLocation))
                                        {
                                            Assemblies.Add(requiredAssemblyLocation);
                                        }
                                        else
                                        {
                                            Assemblies.Add(results[1]);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                foreach (string assembly in Assemblies)
                {
                    project.AddItem("Reference", assembly);
                }

                project.Save(this.ProjectFilePath);

                FileLogger logger = new FileLogger();
                logger.Parameters = "logfile=" + this.BuildLogPath;

                this.BuildReturnedCode = false;

                // According to MSDN, http://msdn.microsoft.com/en-us/library/microsoft.build.evaluation.projectcollection.aspx
                // multiple project collections can exist within an app domain. However, these must not build concurrently.
                // Therefore, we need a static lock to prevent multiple threads from accessing this call.
                lock (_syncObject)
                {
                    this.BuildReturnedCode = project.Build(logger);
                }

                logger.Shutdown();

                // If compilation succeeded, delete the project files.
                //
                if (this.BuildReturnedCode)
                {
                    string generatedAssemblyPath = Path.Combine(this.ProjectFolderPath, @"obj\Release\" + this.ProjectName + ".dll");
                    if (File.Exists(generatedAssemblyPath))
                    {
                        File.Move(generatedAssemblyPath, this.AssemblyPath);
                    }

                    try
                    {
                        System.IO.Directory.Delete(this.ProjectFolderPath, true);
                    }
                    catch (Exception e)
                    {
                        Tracer.TraceException(e);
                        // Ignoring the exceptions from Delete of temp directory.
                    }
                }
            }
#endif
        }

        static object _syncObject = new object();

        /// <summary>
        /// Return true/false to indicate whether the processor architecture is ARM
        /// </summary>
        /// <returns></returns>
        internal static bool IsRunningOnProcessorArchitectureARM()
        {
            // Important:
            // this function has a clone in SMA in admin\monad\src\utils\PsUtils.cs
            // if you are making any changes specific to this function then update the clone as well.

            NativeMethods.SYSTEM_INFO sysInfo = new NativeMethods.SYSTEM_INFO();
            NativeMethods.GetSystemInfo(ref sysInfo);
            return sysInfo.wProcessorArchitecture == NativeMethods.PROCESSOR_ARCHITECTURE_ARM;
        }

        private static class NativeMethods
        {
            // Important:
            // this clone has a clone in SMA in admin\monad\src\utils\PsUtils.cs
            // if you are making any changes specific to this class then update the clone as well.

            internal const ushort PROCESSOR_ARCHITECTURE_INTEL = 0;
            internal const ushort PROCESSOR_ARCHITECTURE_ARM = 5;
            internal const ushort PROCESSOR_ARCHITECTURE_IA64 = 6;
            internal const ushort PROCESSOR_ARCHITECTURE_AMD64 = 9;
            internal const ushort PROCESSOR_ARCHITECTURE_UNKNOWN = 0xFFFF;

            [StructLayout(LayoutKind.Sequential)]
            internal struct SYSTEM_INFO
            {
                public ushort wProcessorArchitecture;
                public ushort wReserved;
                public uint dwPageSize;
                public IntPtr lpMinimumApplicationAddress;
                public IntPtr lpMaximumApplicationAddress;
                public UIntPtr dwActiveProcessorMask;
                public uint dwNumberOfProcessors;
                public uint dwProcessorType;
                public uint dwAllocationGranularity;
                public ushort wProcessorLevel;
                public ushort wProcessorRevision;
            };

            [DllImport("kernel32.dll")]
            internal static extern void GetSystemInfo(ref SYSTEM_INFO lpSystemInfo);
        }
    }
}
