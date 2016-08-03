/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

#region Using directives

using System;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Reflection;
using System.Text;

#if CORECLR
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.IO;
using Microsoft.CodeAnalysis.Emit;
using System.Collections.Immutable;
#else
using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CSharp;
using System.Collections.Concurrent;
#endif

#endregion

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Languages supported for code generation
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1724:TypeNamesShouldNotMatchNamespaces")]
    public enum Language
    {
        /// <summary>
        /// The C# programming language: latest version.
        /// </summary>
        CSharp,

#if CORECLR
        /// <summary>
        /// The C# programming language v6
        /// </summary>
        CSharpVersion6,

        /// <summary>
        /// The C# programming language v5
        /// </summary>
        CSharpVersion5,

        /// <summary>
        /// The C# programming language v4
        /// </summary>
        CSharpVersion4,
#endif

        /// <summary>
        /// The C# programming language v3 (for Linq, etc)
        /// </summary>
        CSharpVersion3,

        /// <summary>
        /// The C# programming language v2 
        /// </summary>
        CSharpVersion2,

#if CORECLR
        /// <summary>
        /// The C# programming language v1
        /// </summary>
        CSharpVersion1,
#endif

        /// <summary>
        /// The Visual Basic programming language
        /// </summary>
        VisualBasic,

        /// <summary>
        /// The Managed JScript programming language
        /// </summary>
        JScript,
    }

    /// <summary>
    /// Types supported for the OutputAssembly parameter
    /// </summary>
    public enum OutputAssemblyType
    {
        /// <summary>
        /// A Dynamically linked library (DLL)
        /// </summary>
        Library,

        /// <summary>
        /// An executable application that targets the console subsystem
        /// </summary>
        ConsoleApplication,

        /// <summary>
        /// An executable application that targets the graphical subsystem
        /// </summary>
        WindowsApplication
    }

    /// <summary>
    /// Compile error or warning.
    /// </summary>
    public class AddTypeCompilerError
    {
        /// <summary>
        /// FileName, if compiled from paths.
        /// </summary>
        public string FileName { get; internal set; }

        /// <summary>
        /// Line number.
        /// </summary>
        public int Line { get; internal set; }

        /// <summary>
        /// Column number.
        /// </summary>
        public int Column { get; internal set; }

        /// <summary>
        /// Error number code, i.e. CS0116
        /// </summary>
        public string ErrorNumber { get; internal set; }

        /// <summary>
        /// Error message text.
        /// </summary>
        public string ErrorText { get; internal set; }

        /// <summary>
        /// true if warning. false if error. 
        /// </summary>
        public bool IsWarning { get; internal set; }
    }

    /// <summary>
    /// Base class that contains logic for Add-Type cmdlet based on 
    /// - CodeDomProvider 
    /// - CodeAnalysis(Roslyn)
    /// </summary>
    public abstract class AddTypeCommandBase : PSCmdlet
    {
        /// <summary>
        /// The source code of this type
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "FromSource")]
        public String TypeDefinition
        {
            get
            {
                return sourceCode;
            }
            set
            {
                sourceCode = value;
            }
        }

        /// <summary>
        /// The name of the type used for auto-generated types
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "FromMember")]
        public String Name { get; set; }

        /// <summary>
        /// The source code of this method / member
        /// </summary>
        [Parameter(Mandatory = true, Position = 1, ParameterSetName = "FromMember")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public String[] MemberDefinition
        {
            get
            {
                return new string[] { sourceCode };
            }
            set
            {
                sourceCode = "";

                if (value != null)
                {
                    for (int counter = 0; counter < value.Length; counter++)
                    {
                        sourceCode += value[counter] + "\n";
                    }
                }
            }
        }

        internal String sourceCode;


        /// <summary>
        /// The namespaced used for the auto-generated type
        /// </summary>
        [Parameter(ParameterSetName = "FromMember")]
        [Alias("NS")]
        [AllowNull]
        public String Namespace
        {
            get
            {
                return typeNamespace;
            }
            set
            {
                typeNamespace = value;
                if (typeNamespace != null)
                {
                    typeNamespace = typeNamespace.Trim();
                }
            }
        }
        internal string typeNamespace = "Microsoft.PowerShell.Commands.AddType.AutoGeneratedTypes";

        /// <summary>
        /// Any using statements required by the auto-generated type
        /// </summary>
        [Parameter(ParameterSetName = "FromMember")]
        [Alias("Using")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public String[] UsingNamespace { get; set; } = Utils.EmptyArray<string>();


        /// <summary>
        /// The path to the source code or DLL to load
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "FromPath")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] Path
        {
            get
            {
                return paths;
            }
            set
            {
                if (value == null)
                {
                    paths = null;
                    return;
                }

                string[] pathValue = value;

                List<string> resolvedPaths = new List<string>();

                // Verify that the paths are resolved and valid
                foreach (string path in pathValue)
                {
                    // Try to resolve the path
                    ProviderInfo provider = null;
                    Collection<string> newPaths = SessionState.Path.GetResolvedProviderPathFromPSPath(path, out provider);

                    // If it didn't resolve, add the original back
                    // for a better error mesage.
                    if (newPaths.Count == 0)
                    {
                        resolvedPaths.Add(path);
                    }
                    else
                    {
                        resolvedPaths.AddRange(newPaths);
                    }
                }

                ProcessPaths(resolvedPaths);
            }
        }

        /// <summary>
        /// The literal path to the source code or DLL to load
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "FromLiteralPath")]
        [Alias("PSPath")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] LiteralPath
        {
            get
            {
                return paths;
            }
            set
            {
                if (value == null)
                {
                    paths = null;
                    return;
                }

                List<string> resolvedPaths = new List<string>();
                foreach (string path in value)
                {
                    string literalPath = SessionState.Path.GetUnresolvedProviderPathFromPSPath(path);
                    resolvedPaths.Add(literalPath);
                }

                ProcessPaths(resolvedPaths);
            }
        }

        private void ProcessPaths(List<string> resolvedPaths)
        {
            // Now, get the file type. At the same time, make sure
            // we aren't attempting to mix languages, as that is
            // not supported by the CodeDomProvider. While it
            // would be possible to partition the files into
            // languages, that would be much too complex to
            // describe.
            string activeExtension = null;
            foreach (string path in resolvedPaths)
            {
                string currentExtension = System.IO.Path.GetExtension(path).ToUpperInvariant();

                switch (currentExtension)
                {
                    case ".CS":
                        Language = Language.CSharp;
                        break;

                    case ".VB":
                        Language = Language.VisualBasic;
                        break;

                    case ".JS":
                        Language = Language.JScript;
                        break;

                    case ".DLL":
                        loadAssembly = true;
                        break;

                    // Throw an error if it is an unrecognized extension
                    default:
                        ErrorRecord errorRecord = new ErrorRecord(
                            new Exception(
                                StringUtil.Format(AddTypeStrings.FileExtensionNotSupported, currentExtension)),
                            "EXTENSION_NOT_SUPPORTED",
                            ErrorCategory.InvalidArgument,
                            currentExtension);

                        ThrowTerminatingError(errorRecord);

                        break;
                }

                if (activeExtension == null)
                {
                    activeExtension = currentExtension;
                }
                else if (!String.Equals(activeExtension, currentExtension, StringComparison.OrdinalIgnoreCase))
                {
                    // Throw an error if they are switching extensions
                    ErrorRecord errorRecord = new ErrorRecord(
                        new Exception(
                            StringUtil.Format(AddTypeStrings.MultipleExtensionsNotSupported)),
                        "MULTIPLE_EXTENSION_NOT_SUPPORTED",
                        ErrorCategory.InvalidArgument,
                        currentExtension);

                    ThrowTerminatingError(errorRecord);
                }

                paths = resolvedPaths.ToArray();
            }
        }

        internal string[] paths;

        /// <summary>
        /// The name of the assembly to load
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "FromAssemblyName")]
        [Alias("AN")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public String[] AssemblyName
        {
            get
            {
                return assemblyNames;
            }
            set
            {
                assemblyNames = value;
                loadAssembly = true;
            }
        }

        internal String[] assemblyNames;

        internal bool loadAssembly = false;


        /// <summary>
        /// The language used to generate source code
        /// </summary>
        [Parameter(ParameterSetName = "FromSource")]
        [Parameter(ParameterSetName = "FromMember")]
        public Language Language
        {
            get
            {
                return language;
            }
            set
            {
                language = value;
                languageSpecified = true;

                PostSetLanguage(language);
            }
        }

        /// <summary>
        /// Post-action for Language setter.
        /// </summary>
        /// <param name="language"></param>
        internal virtual void PostSetLanguage(Language language)
        {
        }


        internal bool languageSpecified = false;
        internal Language language = Language.CSharp;


        /// <summary>
        /// Any reference DLLs to use in the compilation
        /// </summary>
        [Parameter(ParameterSetName = "FromSource")]
        [Parameter(ParameterSetName = "FromMember")]
        [Parameter(ParameterSetName = "FromPath")]
        [Parameter(ParameterSetName = "FromLiteralPath")]
        [Alias("RA")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public String[] ReferencedAssemblies
        {
            get { return referencedAssemblies; }
            set
            {
                referencedAssemblies = value ?? Utils.EmptyArray<string>();
                referencedAssembliesSpecified = true;
            }
        }
        internal bool referencedAssembliesSpecified = false;
        internal string[] referencedAssemblies = Utils.EmptyArray<string>();

        /// <summary>
        /// The path to the output assembly
        /// </summary>
        [Parameter(ParameterSetName = "FromSource")]
        [Parameter(ParameterSetName = "FromMember")]
        [Parameter(ParameterSetName = "FromPath")]
        [Parameter(ParameterSetName = "FromLiteralPath")]
        [Alias("OA")]
        public string OutputAssembly
        {
            get
            {
                return outputAssembly;
            }
            set
            {
                outputAssembly = value;

                if (outputAssembly != null)
                {
                    outputAssembly = outputAssembly.Trim();

                    // Try to resolve the path
                    ProviderInfo provider = null;
                    Collection<string> newPaths = new Collection<string>();

                    try
                    {
                        newPaths = SessionState.Path.GetResolvedProviderPathFromPSPath(outputAssembly, out provider);
                    }
                    // Ignore the ItemNotFound -- we handle it.
                    catch (ItemNotFoundException) { }

                    ErrorRecord errorRecord = new ErrorRecord(
                        new Exception(
                            StringUtil.Format(AddTypeStrings.OutputAssemblyDidNotResolve, outputAssembly)),
                        "INVALID_OUTPUT_ASSEMBLY",
                        ErrorCategory.InvalidArgument,
                        outputAssembly);

                    // If it resolved to a non-standard provider,
                    // generate an error.
                    if (!String.Equals("FileSystem", provider.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        ThrowTerminatingError(errorRecord);
                        return;
                    }

                    // If it resolved to more than one path,
                    // generate an error.
                    if (newPaths.Count > 1)
                    {
                        ThrowTerminatingError(errorRecord);
                        return;
                    }
                    // It didn't resolve to any files. They may
                    // want to create the file.
                    else if (newPaths.Count == 0)
                    {
                        // We can't create one with wildcard characters
                        if (WildcardPattern.ContainsWildcardCharacters(outputAssembly))
                        {
                            ThrowTerminatingError(errorRecord);
                        }
                        // Create the file
                        else
                        {
                            outputAssembly = SessionState.Path.GetUnresolvedProviderPathFromPSPath(outputAssembly);
                        }
                    }
                    // It resolved to a single file
                    else
                    {
                        outputAssembly = newPaths[0];
                    }
                }
            }
        }
        internal string outputAssembly = null;

        /// <summary>
        /// The output type of the assembly
        /// </summary>
        [Parameter(ParameterSetName = "FromSource")]
        [Parameter(ParameterSetName = "FromMember")]
        [Parameter(ParameterSetName = "FromPath")]
        [Parameter(ParameterSetName = "FromLiteralPath")]
        [Alias("OT")]
        public OutputAssemblyType OutputType
        {
            get
            {
                return outputType;
            }
            set
            {
                outputTypeSpecified = true;
                outputType = value;
            }
        }
        internal OutputAssemblyType outputType = OutputAssemblyType.Library;
        internal bool outputTypeSpecified = false;


        /// <summary>
        /// Flag to pass the resulting types along
        /// </summary>
        [Parameter()]
        public SwitchParameter PassThru
        {
            get
            {
                return passThru;
            }
            set
            {
                passThru = value;
            }
        }
        internal SwitchParameter passThru;

        /// <summary>
        /// Flag to ignore warnings during compilation
        /// </summary>
        [Parameter()]
        public SwitchParameter IgnoreWarnings
        {
            get
            {
                return ignoreWarnings;
            }
            set
            {
                ignoreWarnings = value;
                ignoreWarningsSpecified = true;
            }
        }
        internal bool ignoreWarningsSpecified;
        internal SwitchParameter ignoreWarnings;

        internal string GenerateTypeSource(string typeNamespace, string name, string sourceCode, Language language)
        {
            string usingSource = String.Format(
                    CultureInfo.CurrentCulture,
                    GetUsingTemplate(language), GetUsingSet(language));

            string typeSource = String.Format(
                    CultureInfo.CurrentCulture,
                    GetMethodTemplate(language), Name, sourceCode);

            if (!String.IsNullOrEmpty(typeNamespace))
            {
                return usingSource + String.Format(
                    CultureInfo.CurrentCulture,
                    GetNamespaceTemplate(language), typeNamespace, typeSource);
            }
            else
            {
                return usingSource + typeSource;
            }
        }

        internal bool IsCSharp(Language language)
        {
            switch (language)
            {
                case Language.CSharp:
                case Language.CSharpVersion2:
                case Language.CSharpVersion3:
#if CORECLR
                case Language.CSharpVersion1:
                case Language.CSharpVersion4:
                case Language.CSharpVersion5:
                case Language.CSharpVersion6:
#endif                
                    return true;
                default:
                    return false;
            }
        }

        // Get the -FromMember template for a given language
        internal string GetMethodTemplate(Language language)
        {
            if (IsCSharp(language))
            {
                return
                    "    public class {0}\n" +
                    "    {{\n" +
                    "    {1}\n" +
                    "    }}\n";
            }

            switch (language)
            {
                case Language.VisualBasic:
                    return
                        "    public Class {0}\n" +
                        "    \n" +
                        "    {1}\n" +
                        "    \n" +
                        "    End Class\n";
                case Language.JScript:
                    return
                        "    public class {0}\n" +
                        "    {{\n" +
                        "    {1}\n" +
                        "    }}\n";
            }
            return null;
        }

        // Get the -FromMember namespace template for a given language
        internal string GetNamespaceTemplate(Language language)
        {
            if (IsCSharp(language))
            {
                return
                    "namespace {0}\n" +
                    "{{\n" +
                    "{1}\n" +
                    "}}\n";
            }

            switch (language)
            {
                case Language.VisualBasic:
                    return
                        "Namespace {0}\n" +
                        "\n" +
                        "{1}\n" +
                        "End Namespace\n";
                case Language.JScript:
                    return
                        "package {0}\n" +
                        "{{\n" +
                        "{1}\n" +
                        "}}\n";
            }
            return null;
        }

        // Get the -FromMember namespace template for a given language
        internal string GetUsingTemplate(Language language)
        {
            if (IsCSharp(language))
            {
                return
                    "using System;\n" +
                    "using System.Runtime.InteropServices;\n" +
                    "{0}" +
                    "\n";
            }

            switch (language)
            {
                case Language.VisualBasic:
                    return
                        "Imports System\n" +
                        "Imports System.Runtime.InteropServices\n" +
                        "{0}" +
                        "\n";
                case Language.JScript:
                    return
                        "import System;\n" +
                        "import System.Runtime.InteropServices;\n" +
                        "{0}" +
                        "\n";
            }
            return null;
        }

        // Generate the code for the using statements
        internal string GetUsingSet(Language language)
        {
            StringBuilder usingNamespaceSet = new StringBuilder();
            if (IsCSharp(language))
            {
                foreach (string namespaceValue in UsingNamespace)
                {
                    usingNamespaceSet.Append("using " + namespaceValue + ";\n");
                }
            }
            else
            {
                switch (language)
                {
                    case Language.VisualBasic:
                        foreach (string namespaceValue in UsingNamespace)
                        {
                            usingNamespaceSet.Append("Imports " + namespaceValue + "\n");
                        }
                        break;

                    case Language.JScript:
                        foreach (string namespaceValue in UsingNamespace)
                        {
                            usingNamespaceSet.Append("import " + namespaceValue + ";\n");
                        }
                        break;
                }
            }

            return usingNamespaceSet.ToString();
        }

        /// <summary>
        /// Perform common error checks.
        /// Populate source code.
        /// </summary>
        protected override void EndProcessing()
        {
            // Generate an error if they've specified an output
            // assembly type without an output assembly
            if (String.IsNullOrEmpty(outputAssembly) && outputTypeSpecified)
            {
                ErrorRecord errorRecord = new ErrorRecord(
                    new Exception(
                        String.Format(
                            CultureInfo.CurrentCulture,
                            AddTypeStrings.OutputTypeRequiresOutputAssembly)),
                    "OUTPUTTYPE_REQUIRES_ASSEMBLY",
                    ErrorCategory.InvalidArgument,
                    outputType);

                ThrowTerminatingError(errorRecord);
                return;
            }

            PopulateSource();
        }

        internal void PopulateSource()
        {
            // Prevent code compilation in ConstrainedLanguage mode
            if (SessionState.LanguageMode == PSLanguageMode.ConstrainedLanguage)
            {
                ThrowTerminatingError(
                    new ErrorRecord(
                        new PSNotSupportedException(AddTypeStrings.CannotDefineNewType), "CannotDefineNewType", ErrorCategory.PermissionDenied, null));
            }

            // Load the source if they want to load from a file
            if (String.Equals(ParameterSetName, "FromPath", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(ParameterSetName, "FromLiteralPath", StringComparison.OrdinalIgnoreCase)
                )
            {
                sourceCode = "";
                foreach (string file in paths)
                {
                    sourceCode += System.IO.File.ReadAllText(file) + "\n";
                }
            }

            if (String.Equals(ParameterSetName, "FromMember", StringComparison.OrdinalIgnoreCase))
            {
                sourceCode = GenerateTypeSource(typeNamespace, Name, sourceCode, language);
            }
        }

        internal void HandleCompilerErrors(AddTypeCompilerError[] compilerErrors)
        {
            // Get the source code that corresponds to their type in the case of errors
            string[] actualSource = Utils.EmptyArray<string>();

            // Get the source code that corresponds to the
            // error if we generated it
            if ((compilerErrors.Length > 0) &&
                (!String.Equals(ParameterSetName, "FromPath", StringComparison.OrdinalIgnoreCase)) &&
                (!String.Equals(ParameterSetName, "FromLiteralPath", StringComparison.OrdinalIgnoreCase))
                )
            {
                actualSource = sourceCode.Split(Utils.Separators.Newline);
            }

            // Write any errors to the pipeline
            foreach (var error in compilerErrors)
            {
                OutputError(error, actualSource);
            }

            if (compilerErrors.Any(e => !e.IsWarning))
            {
                ErrorRecord errorRecord = new ErrorRecord(
                    new InvalidOperationException(AddTypeStrings.CompilerErrors),
                        "COMPILER_ERRORS",
                        ErrorCategory.InvalidData,
                        null);
                ThrowTerminatingError(errorRecord);
            }
        }

        private void OutputError(AddTypeCompilerError error, string[] actualSource)
        {
            // Get the actual line of the file if they
            // used the -FromPath parameter set
            if (String.Equals(ParameterSetName, "FromPath", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(ParameterSetName, "FromLiteralPath", StringComparison.OrdinalIgnoreCase)
                )
            {
                if (!String.IsNullOrEmpty(error.FileName))
                {
                    actualSource = System.IO.File.ReadAllText(error.FileName).Split(Utils.Separators.Newline);
                }
            }

            string errorText = StringUtil.Format(AddTypeStrings.CompilationErrorFormat,
                        error.FileName, error.Line, error.ErrorText) + "\n";

            for (int lineNumber = error.Line - 1; lineNumber < error.Line + 2; lineNumber++)
            {
                if (lineNumber > 0)
                {
                    if (lineNumber > actualSource.Length)
                        break;

                    string lineText = "";

                    if (lineNumber == error.Line)
                    {
                        lineText += ">>> ";
                    }

                    lineText += actualSource[lineNumber - 1];

                    errorText += "\n" + StringUtil.Format(AddTypeStrings.CompilationErrorFormat,
                        error.FileName, lineNumber, lineText) + "\n";
                }
            }

            if (error.IsWarning)
            {
                WriteWarning(errorText);
            }
            else
            {
                ErrorRecord errorRecord = new ErrorRecord(
                    new Exception(errorText),
                        "SOURCE_CODE_ERROR",
                        ErrorCategory.InvalidData,
                        error);

                WriteError(errorRecord);
            }
        }
    }

#if CORECLR

    /// <summary>
    /// Adds a new type to the Application Domain. 
    /// This version is based on CodeAnalysis (Roslyn).
    /// </summary>
    [Cmdlet(VerbsCommon.Add, "Type", DefaultParameterSetName = "FromSource", HelpUri = "http://go.microsoft.com/fwlink/?LinkID=135195")]
    [OutputType(typeof(Type))]
    public sealed class AddTypeCommand : AddTypeCommandBase
    {
        private static Dictionary<string, int> s_sourceCache = new Dictionary<string, int>();

        /// <summary>
        /// Generate the type(s)
        /// </summary>
        protected override void EndProcessing()
        {
            base.EndProcessing();

            if (loadAssembly)
            {
                if (String.Equals(ParameterSetName, "FromPath", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(ParameterSetName, "FromLiteralPath", StringComparison.OrdinalIgnoreCase))
                {
                    LoadAssemblies(this.paths);
                }

                if (String.Equals(ParameterSetName, "FromAssemblyName", StringComparison.OrdinalIgnoreCase))
                {
                    LoadAssemblies(this.assemblyNames);
                }
            }
            else
            {
                // Load the source if they want to load from a file
                if (String.Equals(ParameterSetName, "FromPath", StringComparison.OrdinalIgnoreCase) ||
                    String.Equals(ParameterSetName, "FromLiteralPath", StringComparison.OrdinalIgnoreCase))
                {
                    this.sourceCode = "";
                    foreach (string file in paths)
                    {
                        this.sourceCode += System.IO.File.ReadAllText(file) + "\n";
                    }
                }

                CompileSourceToAssembly(this.sourceCode);
            }
        }

        private void LoadAssemblies(IEnumerable<string> assemblies)
        {
            foreach (string assemblyName in assemblies)
            {
                // CoreCLR doesn't allow re-load TPA assemblis with different API (i.e. we load them by name and now want to load by path).
                // LoadAssemblyHelper helps us avoid re-loading them, if they already loaded.
                Assembly assembly = LoadAssemblyHelper(assemblyName);
                if (assembly == null)
                {
                    assembly = LoadFrom(ResolveReferencedAssembly(assemblyName));
                }

                if (passThru)
                {
                    WriteTypes(assembly);
                }
            }
        }

        private OutputKind OutputAssemblyTypeToOutputKind(OutputAssemblyType outputType)
        {
            switch (outputType)
            {
                case OutputAssemblyType.Library:
                    return OutputKind.DynamicallyLinkedLibrary;
                case OutputAssemblyType.ConsoleApplication:
                    return OutputKind.ConsoleApplication;
                case OutputAssemblyType.WindowsApplication:
                    return OutputKind.WindowsApplication;
                default:
                    throw new ArgumentOutOfRangeException("outputType");
            }
        }

        private void CheckTypesForDuplicates(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (s_sourceCache.ContainsKey(type.FullName))
                {
                    if (s_sourceCache[type.FullName] != sourceCode.GetHashCode())
                    {
                        ErrorRecord errorRecord = new ErrorRecord(
                                new Exception(
                                    String.Format(AddTypeStrings.TypeAlreadyExists, type.FullName)),
                                "TYPE_ALREADY_EXISTS",
                                ErrorCategory.InvalidOperation,
                                type.FullName);

                        ThrowTerminatingError(errorRecord);
                        return;
                    }
                }
                else
                {
                    s_sourceCache[type.FullName] = sourceCode.GetHashCode();
                }
            }
        }

        private static string s_frameworkFolder = System.IO.Path.GetDirectoryName(typeof(object).GetTypeInfo().Assembly.Location);

        // there are two different assemblies: framework contract and framework implementation.
        // Version 1.1.1 of Microsoft.CodeAnalysis doesn't provide a good way to handle contract separetely from implementation.
        // To simplify user expirience we always add both of them to references.
        // 1) It's a legitimate scenario, when user provides a custom referenced assembly that was built against the contract assembly 
        // (i.e. System.Management.Automation), so we need the contract one.
        // 2) We have to provide implementation assembly explicitly, Roslyn doesn't have a way to figure out implementation by itself.
        // So we are adding both.
        private static PortableExecutableReference s_objectImplementationAssemblyReference =
            MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location);

        private static PortableExecutableReference s_mscorlibAssemblyReference =
           MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("mscorlib")).Location);

        // This assembly should be System.Runtime.dll
        private static PortableExecutableReference s_systemRuntimeAssemblyReference =
            MetadataReference.CreateFromFile(ClrFacade.GetAssemblies(typeof(object).FullName).First().Location);

        // SecureString is defined in a separate assembly.
        // This fact is an implementation detail and should not require the user to specify one more assembly, 
        // if they want to use SecureString in Add-Type -TypeDefinition.
        // So this assembly should be in the default assemblies list to provide the best experience.
        private static PortableExecutableReference s_secureStringAssemblyReference =
            MetadataReference.CreateFromFile(typeof(System.Security.SecureString).GetTypeInfo().Assembly.Location);


        // These assemlbies are always automatically added to ReferencedAssemblies.
        private static PortableExecutableReference[] s_autoReferencedAssemblies = new PortableExecutableReference[]
        {
            s_mscorlibAssemblyReference,
            s_systemRuntimeAssemblyReference,
            s_secureStringAssemblyReference,
            s_objectImplementationAssemblyReference
        };

        // These assemlbies are used, when ReferencedAssemblies parameter is not specified.
        private static PortableExecutableReference[] s_defaultAssemblies = new PortableExecutableReference[]
        {
            s_mscorlibAssemblyReference,
            s_systemRuntimeAssemblyReference,
            s_secureStringAssemblyReference,
            s_objectImplementationAssemblyReference,
            MetadataReference.CreateFromFile(typeof(PSObject).GetTypeInfo().Assembly.Location)
        };

        private bool InMemory { get { return String.IsNullOrEmpty(outputAssembly); } }

        private string ResolveReferencedAssembly(string referencedAssembly)
        {
            // if it's a path, resolve it
            if (referencedAssembly.Contains(System.IO.Path.DirectorySeparatorChar) || referencedAssembly.Contains(System.IO.Path.AltDirectorySeparatorChar))
            {
                if (System.IO.Path.IsPathRooted(referencedAssembly))
                {
                    return referencedAssembly;
                }
                else
                {
                    var paths = SessionState.Path.GetResolvedPSPathFromPSPath(referencedAssembly);
                    return paths[0].Path;
                }
            }

            if (!String.Equals(System.IO.Path.GetExtension(referencedAssembly), ".dll", StringComparison.OrdinalIgnoreCase))
            {
                // If we already load the assembly, we can reference it by name
                Assembly result = LoadAssemblyHelper(referencedAssembly);
                if (result != null)
                {
                    return result.Location;
                }
                else
                {
                    referencedAssembly += ".dll";
                }
            }

            // lookup in framework folders and the current folder
            string frameworkPossiblePath = System.IO.Path.Combine(s_frameworkFolder, referencedAssembly);
            if (File.Exists(frameworkPossiblePath))
            {
                return frameworkPossiblePath;
            }

            string currentFolderPath = SessionState.Path.GetResolvedPSPathFromPSPath(referencedAssembly)[0].Path;
            if (File.Exists(currentFolderPath))
            {
                return currentFolderPath;
            }

            ErrorRecord errorRecord = new ErrorRecord(
                                new Exception(
                                    String.Format(ParserStrings.ErrorLoadingAssembly, referencedAssembly)),
                                "ErrorLoadingAssembly",
                                ErrorCategory.InvalidOperation,
                                referencedAssembly);

            ThrowTerminatingError(errorRecord);
            return null;
        }

        // LoadWithPartialName is deprecated, so we have to write the closest approximation possible.
        // However, this does give us a massive usability improvement, as users can just say
        // Add-Type -AssemblyName Forms (instead of System.Windows.Forms)
        // This is  just long, not unmaintainable.
        [SuppressMessage("Microsoft.Maintainability", "CA1505:AvoidUnmaintainableCode")]
        private Assembly LoadAssemblyHelper(string assemblyName)
        {
            Assembly loadedAssembly = null;

            // First try by strong name
            try
            {
                loadedAssembly = Assembly.Load(new AssemblyName(assemblyName));
            }
            // Generates a FileNotFoundException if you can't load the strong type.
            // So we'll try from the short name.
            catch (System.IO.FileNotFoundException) { }
            // File load exception can happen, when we trying to load from the incorrect assembly name
            // or file corrupted.
            catch (System.IO.FileLoadException) { }

            if (loadedAssembly != null)
                return loadedAssembly;

            return null;
        }

        private void WriteTypes(Assembly assembly)
        {
            WriteObject(assembly.GetTypes(), true);
        }

        private void CompileSourceToAssembly(string source)
        {
            CSharpParseOptions parseOptions;
            if (IsCSharp(language))
            {
                switch (language)
                {
                    case Language.CSharpVersion1:
                        parseOptions = new CSharpParseOptions(LanguageVersion.CSharp1);
                        break;
                    case Language.CSharpVersion2:
                        parseOptions = new CSharpParseOptions(LanguageVersion.CSharp2);
                        break;
                    case Language.CSharpVersion3:
                        parseOptions = new CSharpParseOptions(LanguageVersion.CSharp3);
                        break;
                    case Language.CSharpVersion4:
                        parseOptions = new CSharpParseOptions(LanguageVersion.CSharp4);
                        break;
                    case Language.CSharpVersion5:
                        parseOptions = new CSharpParseOptions(LanguageVersion.CSharp5);
                        break;
                    case Language.CSharpVersion6:
                        parseOptions = new CSharpParseOptions(LanguageVersion.CSharp6);
                        break;
                    case Language.CSharp:
                        parseOptions = new CSharpParseOptions();
                        break;
                    default:
                        parseOptions = null;
                        break;
                }
            }
            else
            {
                ErrorRecord errorRecord = new ErrorRecord(
                    new Exception(String.Format(CultureInfo.CurrentCulture, AddTypeStrings.SpecialNetVersionRequired, language.ToString(), string.Empty)),
                    "LANGUAGE_NOT_SUPPORTED",
                    ErrorCategory.InvalidArgument,
                    language);

                ThrowTerminatingError(errorRecord);
                parseOptions = null;
            }

            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);
            var references = s_defaultAssemblies;
            if (referencedAssembliesSpecified)
            {
                var tempReferences = ReferencedAssemblies.Select(a => MetadataReference.CreateFromFile(ResolveReferencedAssembly(a))).ToList();
                tempReferences.AddRange(s_autoReferencedAssemblies);

                references = tempReferences.ToArray();
            }

            CSharpCompilation compilation = CSharpCompilation.Create(
                System.IO.Path.GetRandomFileName(),
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputAssemblyTypeToOutputKind(OutputType)));

            EmitResult emitResult;

            if (InMemory)
            {
                using (var ms = new MemoryStream())
                {
                    emitResult = compilation.Emit(ms);
                    if (emitResult.Success)
                    {
                        ms.Flush();
                        ms.Seek(0, SeekOrigin.Begin);
                        Assembly assembly = LoadFrom(ms);
                        CheckTypesForDuplicates(assembly);
                        if (passThru)
                        {
                            WriteTypes(assembly);
                        }
                    }
                }
            }
            else
            {
                emitResult = compilation.Emit(outputAssembly);
                if (emitResult.Success)
                {
                    if (passThru)
                    {
                        Assembly assembly = LoadFrom(outputAssembly);
                        CheckTypesForDuplicates(assembly);
                        WriteTypes(assembly);
                    }
                }
            }

            if (emitResult.Diagnostics.Length > 0)
            {
                HandleCompilerErrors(GetErrors(emitResult.Diagnostics));
            }
        }

        private AddTypeCompilerError[] GetErrors(ImmutableArray<Diagnostic> diagnostics)
        {
            return diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error || d.Severity == DiagnosticSeverity.Warning)
                .Select(d => new AddTypeCompilerError
                {
                    ErrorText = d.GetMessage(),
                    FileName = null,
                    Line = d.Location.GetMappedLineSpan().StartLinePosition.Line + 1, // Convert 0-based to 1-based
                    IsWarning = !d.IsWarningAsError && d.Severity == DiagnosticSeverity.Warning,
                    Column = d.Location.GetMappedLineSpan().StartLinePosition.Character + 1, // Convert 0-based to 1-based
                    ErrorNumber = d.Id
                }).ToArray();
        }

        private Assembly LoadFrom(Stream assembly)
        {
            return ClrFacade.LoadFrom(assembly);
        }

        private Assembly LoadFrom(string path)
        {
            return ClrFacade.LoadFrom(path);
        }
    }


#else // end of CORECLR, if !CORECLR

    /// <summary>
    /// Adds a new type to the Application Domain. 
    /// This version is based on CodeDomProvider.
    /// </summary>
    [Cmdlet(VerbsCommon.Add, "Type", DefaultParameterSetName = "FromSource", HelpUri = "http://go.microsoft.com/fwlink/?LinkID=135195")]
    [OutputType(typeof(Type))]
    public sealed class AddTypeCommand : AddTypeCommandBase
    {
        private static Dictionary<string, int> sourceCache = new Dictionary<string, int>();
        private static ConcurrentDictionary<int, Type[]> typeCache = new ConcurrentDictionary<int, Type[]>();
        private static readonly object _cachesLock = new object(); // this lock should be used to protect the 2 dictionaries above

        // Creates a JScript provider, but put in a helper method so that
        // the JIT doesn't return a FileNotFound exception on WOA where this
        // DLL isn't present. This will instead create the FileNotFoundException
        // only when the type is actually used.
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        private System.CodeDom.Compiler.CodeDomProvider SafeCreateJScriptProvider()
        {
            return new Microsoft.JScript.JScriptCodeProvider();
        }

        internal override void PostSetLanguage(Language language)
        {
            switch (language)
            {
                case Language.CSharp:
                    codeDomProvider = new CSharpCodeProvider();
                    break;

                case Language.CSharpVersion2:
                    codeDomProvider = SafeGetCSharpVersion2Compiler();
                    defaultAssemblies.Remove("System.Core.dll");
                    break;

                case Language.CSharpVersion3:
                    codeDomProvider = SafeGetCSharpVersion3Compiler();
                    break;

                case Language.VisualBasic:
                    codeDomProvider = new Microsoft.VisualBasic.VBCodeProvider();
                    break;

                case Language.JScript:
                    codeDomProvider = SafeCreateJScriptProvider();
                    break;
            }
        }

        private List<String> defaultAssemblies = new List<String>(
            new string[] { "System.dll", typeof(PSObject).Assembly.Location, "System.Core.dll" });

        // Internal mapping table from the shortcut name to the strong name
        private static readonly Lazy<ConcurrentDictionary<string, string>> StrongNames = new Lazy<ConcurrentDictionary<string, string>>(InitializeStrongNameDictionary);

        private string ResolveReferencedAssembly(string referencedAssembly)
        {
            if (!String.Equals(System.IO.Path.GetExtension(referencedAssembly), ".dll", StringComparison.OrdinalIgnoreCase))
            {
                // See if it represents an assembly
                // shortcut
                Assembly result = LoadAssemblyHelper(referencedAssembly);
                if (result != null)
                {
                    referencedAssembly = result.Location;
                }
                else
                {
                    referencedAssembly += ".dll";
                }
            }

            return referencedAssembly;
        }

        /// <summary>
        /// A specific CodeProvider to use
        /// </summary>
        [Parameter(ParameterSetName = "FromSource")]
        [Parameter(ParameterSetName = "FromMember")]
        [Alias("Provider")]
        // Disabling OACR warning 26506 and 26505, which complains that there could be null pointer.
        // Since we are using [ValidateNotNullOrEmpty], PowerShell will throw an error if CodeDomProvider is null or empty.
        [SuppressMessage("Microsoft.Usage", "#pw26506")]
        [SuppressMessage("Microsoft.Usage", "#pw26505")]
        [ValidateNotNullOrEmpty]
        public CodeDomProvider CodeDomProvider
        {
            get
            {
                return codeDomProvider;
            }
            set
            {
                codeDomProvider = value;
                providerSpecified = true;

                string typename = codeDomProvider.GetType().FullName;
                if (String.Equals(typename, "Microsoft.CSharp.CSharpCodeProvider", StringComparison.OrdinalIgnoreCase))
                {
                    language = Language.CSharp;
                }

                else if (String.Equals(typename, "Microsoft.VisualBasic.VBCodeProvider", StringComparison.OrdinalIgnoreCase))
                {
                    language = Language.VisualBasic;
                }

                else if (String.Equals(typename, "Microsoft.JScript.JScriptCodeProvider", StringComparison.OrdinalIgnoreCase))
                {
                    language = Language.JScript;
                }
                else if (String.Equals(ParameterSetName, "FromMember", StringComparison.OrdinalIgnoreCase))
                {
                    ErrorRecord errorRecord = new ErrorRecord(
                        new Exception(
                            String.Format(
                                Thread.CurrentThread.CurrentCulture,
                                AddTypeStrings.FromMemberNotSupported)),
                        "LANGUAGE_NOT_SUPPORTED",
                        ErrorCategory.InvalidArgument,
                        codeDomProvider);

                    ThrowTerminatingError(errorRecord);
                }
            }
        }
        private bool providerSpecified = false;
        private CodeDomProvider codeDomProvider = new CSharpCodeProvider();

        /// <summary>
        /// Specific compiler parameters to use
        /// </summary>
        [Parameter(ParameterSetName = "FromSource")]
        [Parameter(ParameterSetName = "FromMember")]
        [Parameter(ParameterSetName = "FromPath")]
        [Parameter(ParameterSetName = "FromLiteralPath")]
        [Alias("CP")]
        public CompilerParameters CompilerParameters { get; set; } = null;

        /// <summary>
        /// Generate the type(s)
        /// </summary>
        protected override void EndProcessing()
        {
            base.EndProcessing();

            // Generate an error if they've specified a language AND a CodeDomProvider
            if (providerSpecified && languageSpecified)
            {
                ErrorRecord errorRecord = new ErrorRecord(
                    new Exception(
                        String.Format(
                            Thread.CurrentThread.CurrentCulture,
                            AddTypeStrings.LanguageAndProviderSpecified)),
                    "LANGUAGE_AND_PROVIDER",
                    ErrorCategory.InvalidArgument,
                    codeDomProvider);

                ThrowTerminatingError(errorRecord);
                return;
            }

            // Generate if user has specified CompilerParmeters and
            // (referencedAssemblies | ignoreWarnings | outputAssembly | outputType)
            if (null != CompilerParameters)
            {
                if (referencedAssembliesSpecified)
                {
                    ErrorRecord errorRecord = new ErrorRecord(
                    new Exception(
                        String.Format(
                            System.Globalization.CultureInfo.InvariantCulture,
                            AddTypeStrings.WrongCompilerParameterCombination,
                            "CompilerParameters", "ReferencedAssemblies")),
                    "COMPILERPARAMETERS_AND_REFERENCEDASSEMBLIES",
                    ErrorCategory.InvalidArgument,
                    CompilerParameters);

                    ThrowTerminatingError(errorRecord);
                    return;
                }

                if (ignoreWarningsSpecified)
                {
                    ErrorRecord errorRecord = new ErrorRecord(
                    new Exception(
                        String.Format(
                            System.Globalization.CultureInfo.InvariantCulture,
                            AddTypeStrings.WrongCompilerParameterCombination,
                            "CompilerParameters", "IgnoreWarnings")),
                    "COMPILERPARAMETERS_AND_IGNOREWARNINGS",
                    ErrorCategory.InvalidArgument,
                    CompilerParameters);

                    ThrowTerminatingError(errorRecord);
                    return;
                }

                if (!string.IsNullOrEmpty(outputAssembly))
                {
                    ErrorRecord errorRecord = new ErrorRecord(
                    new Exception(
                        String.Format(
                            System.Globalization.CultureInfo.InvariantCulture,
                            AddTypeStrings.WrongCompilerParameterCombination,
                            "CompilerParameters", "OutputAssembly")),
                    "COMPILERPARAMETERS_AND_OUTPUTASSEMBLY",
                    ErrorCategory.InvalidArgument,
                    CompilerParameters);

                    ThrowTerminatingError(errorRecord);
                    return;
                }

                if ((outputTypeSpecified))
                {
                    ErrorRecord errorRecord = new ErrorRecord(
                    new Exception(
                        String.Format(
                            System.Globalization.CultureInfo.InvariantCulture,
                            AddTypeStrings.WrongCompilerParameterCombination,
                            "CompilerParameters", "OutputType")),
                    "COMPILERPARAMETERS_AND_OUTPUTTYPE",
                    ErrorCategory.InvalidArgument,
                    CompilerParameters);

                    ThrowTerminatingError(errorRecord);
                    return;
                }
            }

            List<Type> generatedTypes = new List<Type>();

            // We want to compile from a source representation
            if (!loadAssembly)
            {
                CompileAssemblyFromSource(generatedTypes);
            }
            // Otherwise, load from an assembly
            else
            {
                LoadAssemblyFromPathOrName(generatedTypes);
            }

            // Pass the type along if they supplied the PassThru parameter
            if (PassThru)
            {
                WriteObject(generatedTypes, true);
            }
        }

        private void CompileAssemblyFromSource(List<Type> generatedTypes)
        {
            // Prevent code compilation in ConstrainedLanguage mode
            if (SessionState.LanguageMode == PSLanguageMode.ConstrainedLanguage)
            {
                ThrowTerminatingError(
                    new ErrorRecord(
                        new PSNotSupportedException(AddTypeStrings.CannotDefineNewType), "CannotDefineNewType", ErrorCategory.PermissionDenied, null));
            }

            // Load the source if they want to load from a file
            if (String.Equals(ParameterSetName, "FromPath", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(ParameterSetName, "FromLiteralPath", StringComparison.OrdinalIgnoreCase)
                )
            {
                sourceCode = "";
                foreach (string file in paths)
                {
                    sourceCode += System.IO.File.ReadAllText(file) + "\n";
                }
            }

            int sourceHash = sourceCode.GetHashCode();
            string composedName = typeNamespace + "." + Name;

            lock (_cachesLock)
            {
                // See if we've processed this code before. We don't return quickly if they specify
                // the FromMember parameter set or OutputAssembly, though, because they may want to change the
                // type name or re-compile it.
                if (!String.Equals(ParameterSetName, "FromMember", StringComparison.OrdinalIgnoreCase))
                {
                    // Verify it's not OutputAssembly
                    if (String.IsNullOrEmpty(outputAssembly))
                    {
                        // If the hash is the same, just return
                        if (typeCache.ContainsKey(sourceHash))
                        {
                            generatedTypes.AddRange(typeCache[sourceHash]);
                        }
                    }
                }
                // See if we've seen this type before in the FromMember parameter set
                else
                {
                    if (sourceCache.ContainsKey(composedName))
                    {
                        int cachedSourceHash = sourceCache[composedName];

                        // If the hash is the same, just return
                        if (cachedSourceHash == sourceHash)
                        {
                            generatedTypes.AddRange(typeCache[cachedSourceHash]);
                        }
                        // If the hash is different, we can't replace the type
                        // and should generate an error
                        else
                        {
                            ErrorRecord errorRecord = new ErrorRecord(
                                new Exception(
                                    String.Format(
                                        Thread.CurrentThread.CurrentCulture,
                                        AddTypeStrings.TypeAlreadyExists,
                                        composedName)),
                                "TYPE_ALREADY_EXISTS",
                                ErrorCategory.InvalidOperation,
                                composedName);

                            ThrowTerminatingError(errorRecord);
                            return;
                        }
                    }
                }

                // We haven't seen this code before. Continue with the
                // compilation.
                if (generatedTypes.Count == 0)
                {
                    // Obtains an ICodeCompiler from a CodeDomProvider class. 
                    CodeDomProvider provider = codeDomProvider;

                    // Configure the compiler parameters 
                    if (CompilerParameters == null)
                    {
                        CompilerParameters = new CompilerParameters();

                        // Turn off debug information and turn on compiler optimizations by default.
                        CompilerParameters.IncludeDebugInformation = false;
                        if (Language == Language.CSharp || Language == Language.VisualBasic || Language == Language.CSharpVersion2 || Language == Language.CSharpVersion3)
                        {
                            CompilerParameters.CompilerOptions = "/optimize+";
                        }

                        CompilerParameters.ReferencedAssemblies.AddRange(defaultAssemblies.ToArray());

                        foreach (string referencedAssembly in referencedAssemblies)
                        {
                            string resolvedAssembly = ResolveReferencedAssembly(referencedAssembly.Trim());
                            if (!String.IsNullOrEmpty(resolvedAssembly))
                            {
                                bool add = true;
                                bool resolvedIsRooted = System.IO.Path.IsPathRooted(resolvedAssembly);
                                for (int i = 0; i < CompilerParameters.ReferencedAssemblies.Count; ++i)
                                {
                                    var existing = CompilerParameters.ReferencedAssemblies[i];

                                    // We don't want to add duplicates.  We have a duplicate if the resolved file is identical,
                                    // and we also have a duplicate if the specified file is not rooted (is a simple filename) and
                                    // we already have a rooted version of that file in our list.
                                    if (existing.Equals(resolvedAssembly, StringComparison.OrdinalIgnoreCase) ||
                                        (!resolvedIsRooted && System.IO.Path.GetFileName(existing).Equals(resolvedAssembly, StringComparison.OrdinalIgnoreCase)))
                                    {
                                        add = false;
                                        break;
                                    }

                                    // If we've added an unrooted file in the list, and we're trying to add a rooted file, replace
                                    // unrooted file, but don't add a new reference.
                                    if (!System.IO.Path.IsPathRooted(existing) &&
                                        System.IO.Path.GetFileName(resolvedAssembly).Equals(existing, StringComparison.OrdinalIgnoreCase))
                                    {
                                        // Replace the existing reference with one that's resolved.
                                        CompilerParameters.ReferencedAssemblies[i] = resolvedAssembly;
                                        add = false;
                                        break;
                                    }
                                }

                                if (add)
                                {
                                    CompilerParameters.ReferencedAssemblies.Add(resolvedAssembly);
                                }
                            }
                        }

                        CompilerParameters.TreatWarningsAsErrors = !((bool)ignoreWarnings);

                        if (String.IsNullOrEmpty(outputAssembly))
                        {
                            CompilerParameters.GenerateInMemory = true;
                        }
                        else
                        {
                            CompilerParameters.GenerateInMemory = false;
                            CompilerParameters.OutputAssembly = outputAssembly;

                            // If it's an executable, update the compiler
                            // parameters for them.
                            if (String.Equals(
                                System.IO.Path.GetExtension(outputAssembly),
                                ".exe",
                                StringComparison.OrdinalIgnoreCase))
                            {
                                CompilerParameters.GenerateExecutable = true;

                                // They might want to generate a windows executable
                                if (outputType == OutputAssemblyType.WindowsApplication)
                                {
                                    CompilerParameters.CompilerOptions = "/target:winexe";
                                }
                            }
                        }
                    }

                    // Invokes compilation.
                    WriteDebug("\n" + sourceCode);

                    CompilerResults compilerResults = null;

                    try
                    {
                        // Compile from a batch of files if they used the
                        // -FromPath parameter set
                        if (String.Equals(ParameterSetName, "FromPath", StringComparison.OrdinalIgnoreCase) ||
                            String.Equals(ParameterSetName, "FromLiteralPath", StringComparison.OrdinalIgnoreCase)
                            )
                        {
                            compilerResults = provider.CompileAssemblyFromFile(CompilerParameters, paths);
                        }
                        // Otherwise, this is code generated. Use the
                        // source code
                        else
                        {
                            compilerResults = provider.CompileAssemblyFromSource(CompilerParameters, sourceCode);
                        }
                    }
                    catch (InvalidOperationException ex)
                    {
                        if (ex.Message.Contains("csc.exe"))
                        {
                            string errorMessage = "";
                            switch (language)
                            {
                                case Language.CSharpVersion2:
                                    errorMessage = string.Format(CultureInfo.InvariantCulture,
                                                                 AddTypeStrings.CompilerErrorWithCSC, ex.Message, "2");
                                    break;

                                case Language.CSharpVersion3:
                                    errorMessage = string.Format(CultureInfo.InvariantCulture,
                                                                 AddTypeStrings.CompilerErrorWithCSC, ex.Message, "3.5");
                                    break;

                                default:
                                    throw;
                            }

                            throw new InvalidOperationException(errorMessage);
                        }
                        else
                        {
                            throw;
                        }
                    }

                    if (compilerResults.Errors.Count > 0)
                    {
                        HandleCompilerErrors(GetErrors(compilerResults.Errors));
                        return;
                    }

                    // Only return / load the types if they aren't
                    // storing it to disk. PassThru overrides this.
                    if (PassThru || String.IsNullOrEmpty(outputAssembly))
                    {
                        // Some CodeDom providers don't actually return
                        // anything through their CompiledAssembly.
                        if (compilerResults.CompiledAssembly != null)
                        {
                            generatedTypes.AddRange(compilerResults.CompiledAssembly.GetTypes());
                        }
                    }
                    else
                    {
                        // Types stored on disk don't get cached
                        return;
                    }

                    // Now, go through the generated types, as associate them with their
                    // source code. This lets us detect when somebody has compiled from source, changed
                    // the source code, but not the type name.

                    // Cache the generated types
                    if (String.Equals(ParameterSetName, "FromMember", StringComparison.OrdinalIgnoreCase))
                    {
                        sourceCache[composedName] = sourceHash;
                    }
                    else
                    {
                        // Check for type duplication for CSharp, VisualBasic, and JScript.
                        // We don't want to do it for other providers, as they may have type duplication
                        // issues like VB does (where it auto-generates types in the "My" namespace)
                        string typename = codeDomProvider.GetType().FullName;
                        if (String.Equals(typename, "Microsoft.CSharp.CSharpCodeProvider", StringComparison.OrdinalIgnoreCase) ||
                            String.Equals(typename, "Microsoft.VisualBasic.VBCodeProvider", StringComparison.OrdinalIgnoreCase) ||
                            String.Equals(typename, "Microsoft.JScript.JScriptCodeProvider", StringComparison.OrdinalIgnoreCase))
                        {
                            foreach (Type currentType in generatedTypes)
                            {
                                // Check if we already have the type in the cache. We have to exclude the VB "My"
                                // namespace, since it auto-generates duplicate types.
                                if (sourceCache.ContainsKey(currentType.FullName) &&
                                    (!String.Equals(currentType.Namespace, "My", StringComparison.OrdinalIgnoreCase)))
                                {
                                    ErrorRecord errorRecord = new ErrorRecord(
                                        new Exception(
                                            String.Format(
                                                Thread.CurrentThread.CurrentCulture,
                                                AddTypeStrings.TypeAlreadyExists,
                                                currentType.FullName)),
                                        "TYPE_ALREADY_EXISTS",
                                        ErrorCategory.InvalidOperation,
                                        currentType.FullName);

                                    ThrowTerminatingError(errorRecord);
                                    return;
                                }

                                // The type name hasn't been seen before. Cache it.
                                sourceCache[currentType.FullName] = sourceHash;
                            }
                        }
                    }

                    // And finally cache the actual types
                    typeCache[sourceHash] = generatedTypes.ToArray();


                    // Ensure they generated a usable assembly. This won't show up again, as we
                    // cache the results before this.

                    // Verify that at least one type is public
                    // Verify that at least one member for each type is public

                    bool foundPublicMember = false;
                    bool foundPublicType = false;
                    foreach (Type generatedType in generatedTypes)
                    {
                        if (generatedType.IsPublic) { foundPublicType = true; }

                        // Suppress the "defines no public members" for
                        // cmdlets as they are so common
                        if (typeof(System.Management.Automation.Cmdlet).IsAssignableFrom(generatedType))
                        {
                            foundPublicMember = true;
                            continue;
                        }

                        foreach (MemberInfo currentMember in generatedType.GetMembers())
                        {
                            // If the member is defined by this type, is public,
                            // and is a field, method, or property, this is a usable type.
                            if (
                                currentMember.Module.Assembly.Equals(generatedType.Assembly) &&
                                generatedType.IsPublic &&
                                (
                                    (currentMember.MemberType == MemberTypes.Field) ||
                                    (currentMember.MemberType == MemberTypes.Method) ||
                                    (currentMember.MemberType == MemberTypes.Property)
                                )
                                )
                            {
                                foundPublicMember = true;
                                break;
                            }
                        }
                    }

                    if (!foundPublicType)
                    {
                        WriteWarning(AddTypeStrings.TypeDefinitionNotPublic);
                    }
                    else if (!foundPublicMember)
                    {
                        WriteWarning(AddTypeStrings.MethodDefinitionNotPublic);
                    }
                }
            }
        }

        private AddTypeCompilerError[] GetErrors(CompilerErrorCollection errors)
        {
            var result = new List<AddTypeCompilerError>();
            for (int i = 0; i < errors.Count; i++)
            {
                var e = errors[i];
                result.Add(new AddTypeCompilerError
                {
                    ErrorText = e.ErrorText,
                    FileName = e.FileName,
                    Line = e.Line,
                    IsWarning = e.IsWarning,
                    Column = e.Column,
                    ErrorNumber = e.ErrorNumber
                });
            }

            return result.ToArray();
        }

        [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Reflection.Assembly.LoadFrom")]
        private void LoadAssemblyFromPathOrName(List<Type> generatedTypes)
        {
            // Load the source if they want to load from a file
            if (String.Equals(ParameterSetName, "FromPath", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(ParameterSetName, "FromLiteralPath", StringComparison.OrdinalIgnoreCase)
                )
            {
                foreach (string path in paths)
                {
                    generatedTypes.AddRange(ClrFacade.LoadFrom(path).GetTypes());
                }
            }
            // Load the assembly by name
            else if (String.Equals(ParameterSetName, "FromAssemblyName", StringComparison.OrdinalIgnoreCase))
            {
                bool caughtError = false;

                foreach (string assemblyName in assemblyNames)
                {
                    Assembly loadedAssembly = LoadAssemblyHelper(assemblyName);

                    // Generate an error if we could not load the type
                    if (loadedAssembly == null)
                    {
                        caughtError = true;
                        ErrorRecord errorRecord = new ErrorRecord(
                            new Exception(
                                String.Format(
                                    Thread.CurrentThread.CurrentCulture,
                                    AddTypeStrings.AssemblyNotFound,
                                    assemblyName)),
                            "ASSEMBLY_NOT_FOUND",
                            ErrorCategory.ObjectNotFound,
                            assemblyName);

                        WriteError(errorRecord);
                    }
                    else
                    {
                        // Since loading assemblies by name mean that they will usually be large,
                        // only iterate through them if the user specified the PassThru flag.
                        if (PassThru)
                        {
                            generatedTypes.AddRange(loadedAssembly.GetTypes());
                        }
                    }
                }

                if (caughtError)
                {
                    ErrorRecord errorRecord = new ErrorRecord(
                        new InvalidOperationException(AddTypeStrings.AssemblyLoadErrors),
                            "ASSEMBLY_LOAD_ERRORS",
                            ErrorCategory.InvalidData,
                            null);
                    ThrowTerminatingError(errorRecord);
                }
            }
        }

        private CodeDomProvider SafeGetCSharpVersion3Compiler()
        {
            CodeDomProvider csc = null;

            // .NET 3.5 adds a new constructor to the CSharpCodeProvider class that lets you
            // gain access to new features of the C# language. However, this constructor is
            // not available on V2 of the Framework, and in fact is a compile-time error to try
            // to use it. So here, we do it all through reflection.

            // Go through the constructors
            foreach (ConstructorInfo constructor in typeof(CSharpCodeProvider).GetConstructors())
            {
                // Look for the one that takes a single parameter
                ParameterInfo[] constructorParameters = constructor.GetParameters();
                if (constructorParameters.Length == 1)
                {
                    // Ensure this is the one that takes a dictionary
                    Dictionary<string, string> providerParameters = new Dictionary<string, string>();
                    providerParameters["CompilerVersion"] = "v3.5";

                    if (constructorParameters[0].ParameterType.IsAssignableFrom(providerParameters.GetType()))
                    {
                        // If so, create the compiler and break
                        csc = (CodeDomProvider)constructor.Invoke(new object[] { providerParameters });
                        break;
                    }
                }
            }

            if (csc == null)
            {
                throw new NotSupportedException(
                    string.Format(CultureInfo.InvariantCulture, AddTypeStrings.SpecialNetVersionRequired, Language.CSharpVersion3.ToString(), "3.5"));
            }

            return csc;
        }

        private CodeDomProvider SafeGetCSharpVersion2Compiler()
        {
            CodeDomProvider csc = null;

            Dictionary<string, string> providerParameters = new Dictionary<string, string>();
            providerParameters["CompilerVersion"] = "v2.0";
            csc = new CSharpCodeProvider(providerParameters);

            if (csc == null)
            {
                throw new NotSupportedException(
                    string.Format(CultureInfo.InvariantCulture, AddTypeStrings.SpecialNetVersionRequired, Language.CSharpVersion2.ToString(), "2"));
            }

            return csc;
        }

        // LoadWithPartialName is deprecated, so we have to write the closest approximation possible.
        // However, this does give us a massive usability improvement, as users can just say
        // Add-Type -AssemblyName Forms (instead of System.Windows.Forms)
        // This is  just long, not unmaintainable.
        [SuppressMessage("Microsoft.Maintainability", "CA1505:AvoidUnmaintainableCode")]
        private Assembly LoadAssemblyHelper(string assemblyName)
        {
            Assembly loadedAssembly = null;

            // First try by strong name
            try
            {
                loadedAssembly = Assembly.Load(assemblyName);
            }
            // Generates a FileNotFoundException if you can't load the strong type.
            // So we'll try from the short name.
            catch (System.IO.FileNotFoundException) { }

            if (loadedAssembly != null)
                return loadedAssembly;

            // Next, try an exact match
            if (StrongNames.Value.ContainsKey(assemblyName))
            {
                return Assembly.Load(StrongNames.Value[assemblyName]);
            }

            // If the assembly name doesn't contain wildcards, return null. The caller generates an error here.
            if (!WildcardPattern.ContainsWildcardCharacters(assemblyName))
            {
                return null;
            }

            // Now try by wildcard
            string matchedStrongName = null;

            WildcardPattern pattern = WildcardPattern.Get(assemblyName, WildcardOptions.IgnoreCase);
            foreach (string strongNameShortcut in StrongNames.Value.Keys)
            {
                if (pattern.IsMatch(strongNameShortcut))
                {
                    // If we've already found a matching type, throw an error.
                    if (matchedStrongName != null)
                    {
                        ErrorRecord errorRecord = new ErrorRecord(
                            new Exception(
                                String.Format(
                                    Thread.CurrentThread.CurrentCulture,
                                    AddTypeStrings.AmbiguousAssemblyName,
                                    assemblyName, matchedStrongName, StrongNames.Value[strongNameShortcut])),
                            "AMBIGUOUS_ASSEMBLY_NAME",
                            ErrorCategory.InvalidArgument,
                            assemblyName);

                        ThrowTerminatingError(errorRecord);
                        return null;
                    }

                    matchedStrongName = StrongNames.Value[strongNameShortcut];
                }
            }

            // Return NULL if we couldn't find one. The caller generates an error here.
            if (matchedStrongName == null)
                return null;

            // Otherwise, load the assembly.
            return Assembly.Load(matchedStrongName);
        }

        private static ConcurrentDictionary<string, string> InitializeStrongNameDictionary()
        {
            // Then by shortcut name. We don't load by PartialName, because that is non-deterministic.

            // Doing this as a dictionary is 4x as fast as a huge string array.
            // 10,000 iterations of this method takes ~800 ms for a string array, and ~ 200ms for the dictionary
            //
            // $dlls = ((dir c:\windows\Microsoft.NET\Framework\ -fi *.dll -rec) + (dir c:\windows\assembly -fi *.dll -rec)) + (dir C:\Windows\Microsoft.NET\assembly) | 
            //     % { [Reflection.Assembly]::LoadFrom($_.FullName) }
            // "var strongNames = new ConcurrentDictionary<string, string>(4, $($dlls.Count), StringComparer.OrdinalIgnoreCase);" > c:\temp\strongnames.txt
            // $dlls | Sort-Object -u { $_.GetName().Name} | % { 'strongNames["{0}"] = "{1}";' -f $_.FullName.Split(",", 2)[0], $_.FullName  >> c:\temp\strongnames.txt }

            // The default concurrent level is 4. We use the default level.
            var strongNames = new ConcurrentDictionary<string, string>(4, 744, StringComparer.OrdinalIgnoreCase);

            strongNames["Accessibility"] = "Accessibility, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Accessibility.4"] = "Accessibility, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["ADODB"] = "ADODB, Version=7.0.3300.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["AspNetMMCExt"] = "AspNetMMCExt, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["AspNetMMCExt.4"] = "AspNetMMCExt, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["AuditPolicyGPManagedStubs.Interop"] = "AuditPolicyGPManagedStubs.Interop, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["blbmmc"] = "blbmmc, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["blbmmc.resources"] = "blbmmc.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["blbproxy"] = "blbproxy, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["blbproxy.resources"] = "blbproxy.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["blbwizfx"] = "blbwizfx, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["blbwizfx.resources"] = "blbwizfx.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["CppCodeProvider"] = "CppCodeProvider, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["CRVsPackageLib"] = "CRVsPackageLib, Version=10.2.3600.0, Culture=neutral, PublicKeyToken=692fbea5521e1304";
            strongNames["CrystalDecisions.CrystalReports.Design"] = "CrystalDecisions.CrystalReports.Design, Version=10.2.3600.0, Culture=neutral, PublicKeyToken=692fbea5521e1304";
            strongNames["CrystalDecisions.CrystalReports.Engine"] = "CrystalDecisions.CrystalReports.Engine, Version=10.2.3600.0, Culture=neutral, PublicKeyToken=692fbea5521e1304";
            strongNames["CrystalDecisions.Data.AdoDotNetInterop"] = "CrystalDecisions.Data.AdoDotNetInterop, Version=10.2.3600.0, Culture=neutral, PublicKeyToken=692fbea5521e1304";
            strongNames["CrystalDecisions.Enterprise.Desktop.Report"] = "CrystalDecisions.Enterprise.Desktop.Report, Version=10.2.3600.0, Culture=neutral, PublicKeyToken=692fbea5521e1304";
            strongNames["CrystalDecisions.Enterprise.Framework"] = "CrystalDecisions.Enterprise.Framework, Version=10.2.3600.0, Culture=neutral, PublicKeyToken=692fbea5521e1304";
            strongNames["CrystalDecisions.Enterprise.InfoStore"] = "CrystalDecisions.Enterprise.InfoStore, Version=10.2.3600.0, Culture=neutral, PublicKeyToken=692fbea5521e1304";
            strongNames["CrystalDecisions.Enterprise.PluginManager"] = "CrystalDecisions.Enterprise.PluginManager, Version=10.2.3600.0, Culture=neutral, PublicKeyToken=692fbea5521e1304";
            strongNames["CrystalDecisions.Enterprise.Viewing.ReportSource"] = "CrystalDecisions.Enterprise.Viewing.ReportSource, Version=10.2.3600.0, Culture=neutral, PublicKeyToken=692fbea5521e1304";
            strongNames["CrystalDecisions.KeyCode"] = "CrystalDecisions.KeyCode, Version=10.2.3600.0, Culture=neutral, PublicKeyToken=692fbea5521e1304";
            strongNames["CrystalDecisions.ReportAppServer.ClientDoc"] = "CrystalDecisions.ReportAppServer.ClientDoc, Version=10.2.3600.0, Culture=neutral, PublicKeyToken=692fbea5521e1304";
            strongNames["CrystalDecisions.ReportAppServer.CommLayer"] = "CrystalDecisions.ReportAppServer.CommLayer, Version=10.2.3600.0, Culture=neutral, PublicKeyToken=692fbea5521e1304";
            strongNames["CrystalDecisions.ReportAppServer.CommonControls"] = "CrystalDecisions.ReportAppServer.CommonControls, Version=10.2.3600.0, Culture=neutral, PublicKeyToken=692fbea5521e1304";
            strongNames["CrystalDecisions.ReportAppServer.CommonObjectModel"] = "CrystalDecisions.ReportAppServer.CommonObjectModel, Version=10.2.3600.0, Culture=neutral, PublicKeyToken=692fbea5521e1304";
            strongNames["CrystalDecisions.ReportAppServer.Controllers"] = "CrystalDecisions.ReportAppServer.Controllers, Version=10.2.3600.0, Culture=neutral, PublicKeyToken=692fbea5521e1304";
            strongNames["CrystalDecisions.ReportAppServer.CubeDefModel"] = "CrystalDecisions.ReportAppServer.CubeDefModel, Version=10.2.3600.0, Culture=neutral, PublicKeyToken=692fbea5521e1304";
            strongNames["CrystalDecisions.ReportAppServer.DataDefModel"] = "CrystalDecisions.ReportAppServer.DataDefModel, Version=10.2.3600.0, Culture=neutral, PublicKeyToken=692fbea5521e1304";
            strongNames["CrystalDecisions.ReportAppServer.DataSetConversion"] = "CrystalDecisions.ReportAppServer.DataSetConversion, Version=10.2.3600.0, Culture=neutral, PublicKeyToken=692fbea5521e1304";
            strongNames["CrystalDecisions.ReportAppServer.ObjectFactory"] = "CrystalDecisions.ReportAppServer.ObjectFactory, Version=10.2.3600.0, Culture=neutral, PublicKeyToken=692fbea5521e1304";
            strongNames["CrystalDecisions.ReportAppServer.ReportDefModel"] = "CrystalDecisions.ReportAppServer.ReportDefModel, Version=10.2.3600.0, Culture=neutral, PublicKeyToken=692fbea5521e1304";
            strongNames["CrystalDecisions.ReportAppServer.XmlSerialize"] = "CrystalDecisions.ReportAppServer.XmlSerialize, Version=10.2.3600.0, Culture=neutral, PublicKeyToken=692fbea5521e1304";
            strongNames["CrystalDecisions.ReportSource"] = "CrystalDecisions.ReportSource, Version=10.2.3600.0, Culture=neutral, PublicKeyToken=692fbea5521e1304";
            strongNames["CrystalDecisions.Shared"] = "CrystalDecisions.Shared, Version=10.2.3600.0, Culture=neutral, PublicKeyToken=692fbea5521e1304";
            strongNames["CrystalDecisions.VSDesigner"] = "CrystalDecisions.VSDesigner, Version=10.2.3600.0, Culture=neutral, PublicKeyToken=692fbea5521e1304";
            strongNames["CrystalDecisions.Web"] = "CrystalDecisions.Web, Version=10.2.3600.0, Culture=neutral, PublicKeyToken=692fbea5521e1304";
            strongNames["CrystalDecisions.Windows.Forms"] = "CrystalDecisions.Windows.Forms, Version=10.2.3600.0, Culture=neutral, PublicKeyToken=692fbea5521e1304";
            strongNames["cscompmgd"] = "cscompmgd, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["CustomMarshalers"] = "CustomMarshalers, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["CustomMarshalers.4"] = "CustomMarshalers, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["dao"] = "dao, Version=10.0.4504.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["DfsMgmt"] = "DfsMgmt, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["DfsMgmt.resources"] = "DfsMgmt.resources, Version=1.0.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["DfsObjectModel"] = "DfsObjectModel, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["DfsObjectModel.resources"] = "DfsObjectModel.resources, Version=1.0.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["EnvDTE"] = "EnvDTE, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["EnvDTE80"] = "EnvDTE80, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["EnvDTE90"] = "EnvDTE90, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["EventViewer"] = "EventViewer, Version=6.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["EventViewer.resources"] = "EventViewer.resources, Version=6.0.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["EventViewer.6.2"] = "EventViewer, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["EventViewer.resources.6.2"] = "EventViewer.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Extensibility"] = "Extensibility, Version=7.0.3300.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["IACore"] = "IACore, Version=1.7.6223.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["IALoader"] = "IALoader, Version=1.7.6223.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["IEExecRemote"] = "IEExecRemote, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["IEHost"] = "IEHost, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["IIEHost"] = "IIEHost, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Interop.DFSRHelper"] = "Interop.DFSRHelper, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["ipdmctrl"] = "ipdmctrl, Version=11.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c";
            strongNames["ISymWrapper"] = "ISymWrapper, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["MFCMIFC80"] = "MFCMIFC80, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.Activities.Build"] = "Microsoft.Activities.Build, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.ADRoles.Aspects"] = "Microsoft.ADRoles.Aspects, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.ADRoles.ServerManager.Common"] = "Microsoft.ADRoles.ServerManager.Common, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.ADRoles.ServerManager.Common.resources"] = "Microsoft.ADRoles.ServerManager.Common.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.ADRoles.UI.Common"] = "Microsoft.ADRoles.UI.Common, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.AnalysisServices"] = "Microsoft.AnalysisServices, Version=9.0.242.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91";
            strongNames["Microsoft.AnalysisServices.AdomdClient"] = "Microsoft.AnalysisServices.AdomdClient, Version=9.0.242.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91";
            strongNames["Microsoft.AnalysisServices.DeploymentEngine"] = "Microsoft.AnalysisServices.DeploymentEngine, Version=9.0.242.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91";
            strongNames["Microsoft.ApplicationId.Framework"] = "Microsoft.ApplicationId.Framework, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.ApplicationId.Framework.resources"] = "Microsoft.ApplicationId.Framework.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.ApplicationId.RuleWizard"] = "Microsoft.ApplicationId.RuleWizard, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.ApplicationId.RuleWizard.resources"] = "Microsoft.ApplicationId.RuleWizard.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.BackgroundIntelligentTransfer.Management"] = "Microsoft.BackgroundIntelligentTransfer.Management, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.BackgroundIntelligentTransfer.Management.resources"] = "Microsoft.BackgroundIntelligentTransfer.Management.resources, Version=3.0.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.BestPractices"] = "Microsoft.BestPractices, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.BestPractices.resources"] = "Microsoft.BestPractices.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Build"] = "Microsoft.Build, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.Build.Conversion"] = "Microsoft.Build.Conversion, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.Build.Conversion.v3.5"] = "Microsoft.Build.Conversion.v3.5, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.Build.Conversion.v4.0"] = "Microsoft.Build.Conversion.v4.0, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.Build.Engine"] = "Microsoft.Build.Engine, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.Build.Engine.4"] = "Microsoft.Build.Engine, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.Build.Framework"] = "Microsoft.Build.Framework, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.Build.Framework.4"] = "Microsoft.Build.Framework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.Build.Tasks"] = "Microsoft.Build.Tasks, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.Build.Tasks.v3.5"] = "Microsoft.Build.Tasks.v3.5, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.Build.Tasks.v4.0"] = "Microsoft.Build.Tasks.v4.0, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.Build.Utilities"] = "Microsoft.Build.Utilities, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.Build.Utilities.v3.5"] = "Microsoft.Build.Utilities.v3.5, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.Build.Utilities.v4.0"] = "Microsoft.Build.Utilities.v4.0, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.Build.VisualJSharp"] = "Microsoft.Build.VisualJSharp, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.CertificateServices.Deployment.Common"] = "Microsoft.CertificateServices.Deployment.Common, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.CertificateServices.Deployment.Common.resources"] = "Microsoft.CertificateServices.Deployment.Common.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.CertificateServices.PKIClient.Cmdlets"] = "Microsoft.CertificateServices.PKIClient.Cmdlets, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.CertificateServices.PKIClient.Cmdlets.resources"] = "Microsoft.CertificateServices.PKIClient.Cmdlets.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.CertificateServices.ServerManager.DeploymentPlugIn"] = "Microsoft.CertificateServices.ServerManager.DeploymentPlugIn, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.CertificateServices.ServerManager.DeploymentPlugIn.resources"] = "Microsoft.CertificateServices.ServerManager.DeploymentPlugIn.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.CertificateServices.Setup.Interop"] = "Microsoft.CertificateServices.Setup.Interop, Version=6.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.CompactFramework.Build.Tasks"] = "Microsoft.CompactFramework.Build.Tasks, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.CompactFramework.Design"] = "Microsoft.CompactFramework.Design, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.CompactFramework.Design.Model"] = "Microsoft.CompactFramework.Design.Model, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.CompactFramework.Design.PocketPC"] = "Microsoft.CompactFramework.Design.PocketPC, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.CompactFramework.Design.PocketPC2004"] = "Microsoft.CompactFramework.Design.PocketPC2004, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.CompactFramework.Design.PocketPCV1"] = "Microsoft.CompactFramework.Design.PocketPCV1, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.CompactFramework.Design.SmartPhone"] = "Microsoft.CompactFramework.Design.SmartPhone, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.CompactFramework.Design.SmartPhone2004"] = "Microsoft.CompactFramework.Design.SmartPhone2004, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.CompactFramework.Design.WindowsCE"] = "Microsoft.CompactFramework.Design.WindowsCE, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.CSharp"] = "Microsoft.CSharp, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.Data.Entity.Build.Tasks"] = "Microsoft.Data.Entity.Build.Tasks, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.DataWarehouse.Interfaces"] = "Microsoft.DataWarehouse.Interfaces, Version=9.0.242.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91";
            strongNames["Microsoft.DirectoryServices.Deployment.Types"] = "Microsoft.DirectoryServices.Deployment.Types, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.DirectoryServices.Deployment.Types.resources"] = "Microsoft.DirectoryServices.Deployment.Types.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.DirectoryServices.ServerManager"] = "Microsoft.DirectoryServices.ServerManager, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.DirectoryServices.ServerManager.resources"] = "Microsoft.DirectoryServices.ServerManager.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Dtc.PowerShell"] = "Microsoft.Dtc.PowerShell, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Dtc.PowerShell.resources"] = "Microsoft.Dtc.PowerShell.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.ExceptionMessageBox"] = "Microsoft.ExceptionMessageBox, Version=9.0.242.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91";
            strongNames["Microsoft.FederationServices.ServerManager"] = "Microsoft.FederationServices.ServerManager, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.FederationServices.ServerManager.resources"] = "Microsoft.FederationServices.ServerManager.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.GroupPolicy.AdmTmplEditor"] = "Microsoft.GroupPolicy.AdmTmplEditor, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.GroupPolicy.AdmTmplEditor.resources"] = "Microsoft.GroupPolicy.AdmTmplEditor.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.GroupPolicy.GpmgmtLib"] = "Microsoft.GroupPolicy.GpmgmtLib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.GroupPolicy.GPOAdminGrid"] = "Microsoft.GroupPolicy.GPOAdminGrid, Version=2.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.GroupPolicy.Interop"] = "Microsoft.GroupPolicy.Interop, Version=2.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.GroupPolicy.Private.GpmgmtpLib"] = "Microsoft.GroupPolicy.Private.GpmgmtpLib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.GroupPolicy.Reporting"] = "Microsoft.GroupPolicy.Reporting, Version=2.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.GroupPolicy.Reporting.resources"] = "Microsoft.GroupPolicy.Reporting.resources, Version=2.0.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Ink"] = "Microsoft.Ink, Version=6.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Ink.resources"] = "Microsoft.Ink.resources, Version=6.0.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Internal.VisualStudio.Shell.Interop.9.0"] = "Microsoft.Internal.VisualStudio.Shell.Interop.9.0, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.Interop.Security.AzRoles"] = "Microsoft.Interop.Security.AzRoles, Version=2.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.JScript"] = "Microsoft.JScript, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.JScript.10"] = "Microsoft.JScript, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.KeyDistributionService.Cmdlets"] = "Microsoft.KeyDistributionService.Cmdlets, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.KeyDistributionService.Cmdlets.resources"] = "Microsoft.KeyDistributionService.Cmdlets.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.LightweightDirectoryServices.ServerManager"] = "Microsoft.LightweightDirectoryServices.ServerManager, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.LightweightDirectoryServices.ServerManager.resources"] = "Microsoft.LightweightDirectoryServices.ServerManager.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Management.Infrastructure.Native"] = "Microsoft.Management.Infrastructure.Native, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Management.UI"] = "Microsoft.Management.UI, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Management.UI.resources"] = "Microsoft.Management.UI.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.ManagementConsole"] = "Microsoft.ManagementConsole, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.ManagementConsole.resources"] = "Microsoft.ManagementConsole.resources, Version=3.0.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.mshtml"] = "Microsoft.mshtml, Version=7.0.3300.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.MSXML"] = "Microsoft.MSXML, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.NetEnterpriseServers.ExceptionMessageBox"] = "Microsoft.NetEnterpriseServers.ExceptionMessageBox, Version=9.0.242.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91";
            strongNames["Microsoft.Office.InfoPath"] = "Microsoft.Office.InfoPath, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c";
            strongNames["Microsoft.Office.InfoPath.Client.Internal.Host"] = "Microsoft.Office.InfoPath.Client.Internal.Host, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c";
            strongNames["Microsoft.Office.InfoPath.Client.Internal.Host.Interop"] = "Microsoft.Office.InfoPath.Client.Internal.Host.Interop, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c";
            strongNames["Microsoft.Office.InfoPath.FormControl"] = "Microsoft.Office.InfoPath.FormControl, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c";
            strongNames["Microsoft.Office.InfoPath.Permission"] = "Microsoft.Office.InfoPath.Permission, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c";
            strongNames["Microsoft.Office.InfoPath.Vsta"] = "Microsoft.Office.InfoPath.Vsta, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c";
            strongNames["Microsoft.Office.Interop.Access"] = "Microsoft.Office.Interop.Access, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c";
            strongNames["Microsoft.Office.Interop.Access.Dao"] = "Microsoft.Office.Interop.Access.Dao, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c";
            strongNames["Microsoft.Office.Interop.Excel"] = "Microsoft.Office.Interop.Excel, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c";
            strongNames["Microsoft.Office.Interop.Graph"] = "Microsoft.Office.Interop.Graph, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c";
            strongNames["Microsoft.Office.Interop.InfoPath"] = "Microsoft.Office.Interop.InfoPath, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c";
            strongNames["Microsoft.Office.Interop.InfoPath.SemiTrust"] = "Microsoft.Office.Interop.InfoPath.SemiTrust, Version=11.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c";
            strongNames["Microsoft.Office.Interop.InfoPath.Xml"] = "Microsoft.Office.Interop.InfoPath.Xml, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c";
            strongNames["Microsoft.Office.Interop.OneNote"] = "Microsoft.Office.Interop.OneNote, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c";
            strongNames["Microsoft.Office.Interop.Outlook"] = "Microsoft.Office.Interop.Outlook, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c";
            strongNames["Microsoft.Office.Interop.OutlookViewCtl"] = "Microsoft.Office.Interop.OutlookViewCtl, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c";
            strongNames["Microsoft.Office.Interop.PowerPoint"] = "Microsoft.Office.Interop.PowerPoint, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c";
            strongNames["Microsoft.Office.Interop.Publisher"] = "Microsoft.Office.Interop.Publisher, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c";
            strongNames["Microsoft.Office.Interop.SmartTag"] = "Microsoft.Office.Interop.SmartTag, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c";
            strongNames["Microsoft.Office.Interop.Word"] = "Microsoft.Office.Interop.Word, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c";
            strongNames["Microsoft.Office.Tools.Common"] = "Microsoft.Office.Tools.Common, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.Office.Tools.Common2007"] = "Microsoft.Office.Tools.Common2007, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.Office.Tools.Excel"] = "Microsoft.Office.Tools.Excel, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.Office.Tools.Outlook"] = "Microsoft.Office.Tools.Outlook, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.Office.Tools.Word"] = "Microsoft.Office.Tools.Word, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.PowerShell.Commands.Diagnostics"] = "Microsoft.PowerShell.Commands.Diagnostics, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.PowerShell.Commands.Management"] = "Microsoft.PowerShell.Commands.Management, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.PowerShell.Commands.Management.resources"] = "Microsoft.PowerShell.Commands.Management.resources, Version=3.0.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.PowerShell.Commands.Utility"] = "Microsoft.PowerShell.Commands.Utility, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.PowerShell.Commands.Utility.resources"] = "Microsoft.PowerShell.Commands.Utility.resources, Version=3.0.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.PowerShell.ConsoleHost"] = "Microsoft.PowerShell.ConsoleHost, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.PowerShell.ConsoleHost.resources"] = "Microsoft.PowerShell.ConsoleHost.resources, Version=3.0.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.PowerShell.Editor"] = "Microsoft.PowerShell.Editor, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.PowerShell.Editor.resources"] = "Microsoft.PowerShell.Editor.resources, Version=3.0.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.PowerShell.GPowerShell"] = "Microsoft.PowerShell.GPowerShell, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.PowerShell.GPowerShell.resources"] = "Microsoft.PowerShell.GPowerShell.resources, Version=3.0.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.PowerShell.GraphicalHost"] = "Microsoft.PowerShell.GraphicalHost, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.PowerShell.GraphicalHost.resources"] = "Microsoft.PowerShell.GraphicalHost.resources, Version=3.0.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.PowerShell.Security"] = "Microsoft.PowerShell.Security, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.PowerShell.Security.resources"] = "Microsoft.PowerShell.Security.resources, Version=3.0.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.RemoteDesktopServices.Management.Activities"] = "Microsoft.RemoteDesktopServices.Management.Activities, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.RemoteDesktopServices.Management.Activities.resources"] = "Microsoft.RemoteDesktopServices.Management.Activities.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.ReportViewer.Common"] = "Microsoft.ReportViewer.Common, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.ReportViewer.Design"] = "Microsoft.ReportViewer.Design, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.ReportViewer.ProcessingObjectModel"] = "Microsoft.ReportViewer.ProcessingObjectModel, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.ReportViewer.WebDesign"] = "Microsoft.ReportViewer.WebDesign, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.ReportViewer.WebForms"] = "Microsoft.ReportViewer.WebForms, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.ReportViewer.WinForms"] = "Microsoft.ReportViewer.WinForms, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.SecureBoot.Commands"] = "Microsoft.SecureBoot.Commands, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.SecureBoot.Commands.resources"] = "Microsoft.SecureBoot.Commands.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Security.ApplicationId.PolicyManagement.Cmdlets"] = "Microsoft.Security.ApplicationId.PolicyManagement.Cmdlets, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Security.ApplicationId.PolicyManagement.Cmdlets.resources"] = "Microsoft.Security.ApplicationId.PolicyManagement.Cmdlets.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Security.ApplicationId.PolicyManagement.PolicyEngineApi.Interop"] = "Microsoft.Security.ApplicationId.PolicyManagement.PolicyEngineApi.Interop, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Security.ApplicationId.PolicyManagement.PolicyManager"] = "Microsoft.Security.ApplicationId.PolicyManagement.PolicyManager, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Security.ApplicationId.PolicyManagement.PolicyManager.resources"] = "Microsoft.Security.ApplicationId.PolicyManagement.PolicyManager.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Security.ApplicationId.PolicyManagement.PolicyModel"] = "Microsoft.Security.ApplicationId.PolicyManagement.PolicyModel, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Security.ApplicationId.PolicyManagement.PolicyModel.resources"] = "Microsoft.Security.ApplicationId.PolicyManagement.PolicyModel.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Security.ApplicationId.PolicyManagement.XmlHelper"] = "Microsoft.Security.ApplicationId.PolicyManagement.XmlHelper, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Security.ApplicationId.Wizards.AutomaticRuleGenerationWizard"] = "Microsoft.Security.ApplicationId.Wizards.AutomaticRuleGenerationWizard, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Security.ApplicationId.Wizards.AutomaticRuleGenerationWizard.resources"] = "Microsoft.Security.ApplicationId.Wizards.AutomaticRuleGenerationWizard.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.SqlServer.BatchParser"] = "Microsoft.SqlServer.BatchParser, Version=9.0.242.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91";
            strongNames["Microsoft.SqlServer.ConnectionInfo"] = "Microsoft.SqlServer.ConnectionInfo, Version=9.0.242.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91";
            strongNames["Microsoft.SqlServer.CustomControls"] = "Microsoft.SqlServer.CustomControls, Version=9.0.242.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91";
            strongNames["Microsoft.SqlServer.GridControl"] = "Microsoft.SqlServer.GridControl, Version=9.0.242.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91";
            strongNames["Microsoft.SqlServer.Instapi"] = "Microsoft.SqlServer.Instapi, Version=9.0.242.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91";
            strongNames["Microsoft.SqlServer.MgdSqlDumper"] = "Microsoft.SqlServer.MgdSqlDumper, Version=9.0.242.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91";
            strongNames["Microsoft.SqlServer.RegSvrEnum"] = "Microsoft.SqlServer.RegSvrEnum, Version=9.0.242.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91";
            strongNames["Microsoft.SqlServer.Replication"] = "Microsoft.SqlServer.Replication, Version=9.0.242.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91";
            strongNames["Microsoft.SqlServer.Replication.BusinessLogicSupport"] = "Microsoft.SqlServer.Replication.BusinessLogicSupport, Version=9.0.242.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91";
            strongNames["Microsoft.SqlServer.Rmo"] = "Microsoft.SqlServer.Rmo, Version=9.0.242.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91";
            strongNames["Microsoft.SqlServer.ServiceBrokerEnum"] = "Microsoft.SqlServer.ServiceBrokerEnum, Version=9.0.242.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91";
            strongNames["Microsoft.SqlServer.Setup"] = "Microsoft.SqlServer.Setup, Version=9.0.242.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91";
            strongNames["Microsoft.SqlServer.Smo"] = "Microsoft.SqlServer.Smo, Version=9.0.242.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91";
            strongNames["Microsoft.SqlServer.SmoEnum"] = "Microsoft.SqlServer.SmoEnum, Version=9.0.242.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91";
            strongNames["Microsoft.SqlServer.SqlEnum"] = "Microsoft.SqlServer.SqlEnum, Version=9.0.242.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91";
            strongNames["Microsoft.SqlServer.SqlTDiagM"] = "Microsoft.SqlServer.SqlTDiagM, Version=9.0.242.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91";
            strongNames["Microsoft.SqlServer.SString"] = "Microsoft.SqlServer.SString, Version=9.0.242.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91";
            strongNames["Microsoft.SqlServer.WizardFrameworkLite"] = "Microsoft.SqlServer.WizardFrameworkLite, Version=9.0.242.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91";
            strongNames["Microsoft.SqlServer.WmiEnum"] = "Microsoft.SqlServer.WmiEnum, Version=9.0.242.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91";
            strongNames["Microsoft.StdFormat"] = "Microsoft.StdFormat, Version=7.0.3300.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.Storage.Vds"] = "Microsoft.Storage.Vds, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Storage.Vds.resources"] = "Microsoft.Storage.Vds.resources, Version=1.0.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Tpm"] = "Microsoft.Tpm, Version=6.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Tpm.6.2"] = "Microsoft.Tpm, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Tpm.Commands"] = "Microsoft.Tpm.Commands, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Tpm.Commands.resources"] = "Microsoft.Tpm.Commands.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Tpm.resources"] = "Microsoft.Tpm.resources, Version=6.0.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Tpm.resources.6.2"] = "Microsoft.Tpm.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Transactions.Bridge"] = "Microsoft.Transactions.Bridge, Version=3.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.Transactions.Bridge.4"] = "Microsoft.Transactions.Bridge, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.Transactions.Bridge.Dtc"] = "Microsoft.Transactions.Bridge.Dtc, Version=3.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.Transactions.Bridge.Dtc.4"] = "Microsoft.Transactions.Bridge.Dtc, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.Vbe.Interop"] = "Microsoft.Vbe.Interop, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c";
            strongNames["Microsoft.Vbe.Interop.Forms"] = "Microsoft.Vbe.Interop.Forms, Version=11.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c";
            strongNames["Microsoft.VisualBasic"] = "Microsoft.VisualBasic, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualBasic.10"] = "Microsoft.VisualBasic, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualBasic.Activities.Compiler"] = "Microsoft.VisualBasic.Activities.Compiler, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualBasic.Compatibility"] = "Microsoft.VisualBasic.Compatibility, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualBasic.Compatibility.10"] = "Microsoft.VisualBasic.Compatibility, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualBasic.Compatibility.Data"] = "Microsoft.VisualBasic.Compatibility.Data, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualBasic.Compatibility.Data.10"] = "Microsoft.VisualBasic.Compatibility.Data, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualBasic.Vsa"] = "Microsoft.VisualBasic.Vsa, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualC"] = "Microsoft.VisualC, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualC.10"] = "Microsoft.VisualC, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualC.ApplicationVerifier"] = "Microsoft.VisualC.ApplicationVerifier, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualC.STLCLR"] = "Microsoft.VisualC.STLCLR, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualC.STLCLR.2"] = "Microsoft.VisualC.STLCLR, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualC.VSCodeParser"] = "Microsoft.VisualC.VSCodeParser, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualC.VSCodeProvider"] = "Microsoft.VisualC.VSCodeProvider, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio"] = "Microsoft.VisualStudio, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.CommandBars"] = "Microsoft.VisualStudio.CommandBars, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.CommonIDE"] = "Microsoft.VisualStudio.CommonIDE, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Configuration"] = "Microsoft.VisualStudio.Configuration, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Debugger.Interop"] = "Microsoft.VisualStudio.Debugger.Interop, Version=8.0.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Debugger.InteropA"] = "Microsoft.VisualStudio.Debugger.InteropA, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.DebuggerVisualizers"] = "Microsoft.VisualStudio.DebuggerVisualizers, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Design"] = "Microsoft.VisualStudio.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Designer.Interfaces"] = "Microsoft.VisualStudio.Designer.Interfaces, Version=1.0.5000.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.DeviceConnectivity.Interop"] = "Microsoft.VisualStudio.DeviceConnectivity.Interop, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Diagnostics.ServiceModelSink"] = "Microsoft.VisualStudio.Diagnostics.ServiceModelSink, Version=3.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Editors"] = "Microsoft.VisualStudio.Editors, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.EnterpriseTools"] = "Microsoft.VisualStudio.EnterpriseTools, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.EnterpriseTools.ClassDesigner"] = "Microsoft.VisualStudio.EnterpriseTools.ClassDesigner, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.EnterpriseTools.Shell"] = "Microsoft.VisualStudio.EnterpriseTools.Shell, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.EnterpriseTools.TypeSystem"] = "Microsoft.VisualStudio.EnterpriseTools.TypeSystem, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.HostingProcess.Utilities"] = "Microsoft.VisualStudio.HostingProcess.Utilities, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.HostingProcess.Utilities.Sync"] = "Microsoft.VisualStudio.HostingProcess.Utilities.Sync, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.ManagedInterfaces"] = "Microsoft.VisualStudio.ManagedInterfaces, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Modeling"] = "Microsoft.VisualStudio.Modeling, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Modeling.ArtifactMapper"] = "Microsoft.VisualStudio.Modeling.ArtifactMapper, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Modeling.ArtifactMapper.VSHost"] = "Microsoft.VisualStudio.Modeling.ArtifactMapper.VSHost, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Modeling.Diagrams"] = "Microsoft.VisualStudio.Modeling.Diagrams, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Modeling.Diagrams.GraphObject"] = "Microsoft.VisualStudio.Modeling.Diagrams.GraphObject, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.OfficeTools.Build.Tasks"] = "Microsoft.VisualStudio.OfficeTools.Build.Tasks, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.OfficeTools.Controls.ContainerControl"] = "Microsoft.VisualStudio.OfficeTools.Controls.ContainerControl, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.OfficeTools.Controls.ManagedWrapper"] = "Microsoft.VisualStudio.OfficeTools.Controls.ManagedWrapper, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.OfficeTools.Designer"] = "Microsoft.VisualStudio.OfficeTools.Designer, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.OLE.Interop"] = "Microsoft.VisualStudio.OLE.Interop, Version=7.1.40304.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Package.LanguageService"] = "Microsoft.VisualStudio.Package.LanguageService, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.ProjectAggregator"] = "Microsoft.VisualStudio.ProjectAggregator, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Publish"] = "Microsoft.VisualStudio.Publish, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.QualityTools.Resource"] = "Microsoft.VisualStudio.QualityTools.Resource, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.QualityTools.UnitTestFramework"] = "Microsoft.VisualStudio.QualityTools.UnitTestFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Shell"] = "Microsoft.VisualStudio.Shell, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Shell.9.0"] = "Microsoft.VisualStudio.Shell.9.0, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Shell.Design"] = "Microsoft.VisualStudio.Shell.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Shell.Interop"] = "Microsoft.VisualStudio.Shell.Interop, Version=7.1.40304.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Shell.Interop.8.0"] = "Microsoft.VisualStudio.Shell.Interop.8.0, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Shell.Interop.9.0"] = "Microsoft.VisualStudio.Shell.Interop.9.0, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.TeamSystem.PerformanceWizard"] = "Microsoft.VisualStudio.TeamSystem.PerformanceWizard, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.TemplateWizardInterface"] = "Microsoft.VisualStudio.TemplateWizardInterface, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.TextManager.Interop"] = "Microsoft.VisualStudio.TextManager.Interop, Version=7.1.40304.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.TextManager.Interop.8.0"] = "Microsoft.VisualStudio.TextManager.Interop.8.0, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.TextManager.Interop.9.0"] = "Microsoft.VisualStudio.TextManager.Interop.9.0, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Tools.Applications.Adapter"] = "Microsoft.VisualStudio.Tools.Applications.Adapter, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Tools.Applications.Adapter.v9.0"] = "Microsoft.VisualStudio.Tools.Applications.Adapter.v9.0, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Tools.Applications.AddInAdapter"] = "Microsoft.VisualStudio.Tools.Applications.AddInAdapter, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Tools.Applications.AddInBase"] = "Microsoft.VisualStudio.Tools.Applications.AddInBase, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Tools.Applications.AddInManager"] = "Microsoft.VisualStudio.Tools.Applications.AddInManager, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Tools.Applications.Blueprints"] = "Microsoft.VisualStudio.Tools.Applications.Blueprints, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Tools.Applications.Common"] = "Microsoft.VisualStudio.Tools.Applications.Common, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Tools.Applications.ComRPCChannel"] = "Microsoft.VisualStudio.Tools.Applications.ComRPCChannel, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Tools.Applications.Contract"] = "Microsoft.VisualStudio.Tools.Applications.Contract, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Tools.Applications.Contract.v9.0"] = "Microsoft.VisualStudio.Tools.Applications.Contract.v9.0, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Tools.Applications.DesignTime"] = "Microsoft.VisualStudio.Tools.Applications.DesignTime, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Tools.Applications.HostAdapter"] = "Microsoft.VisualStudio.Tools.Applications.HostAdapter, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Tools.Applications.Hosting"] = "Microsoft.VisualStudio.Tools.Applications.Hosting, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Tools.Applications.Hosting.v9.0"] = "Microsoft.VisualStudio.Tools.Applications.Hosting.v9.0, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Tools.Applications.InteropAdapter"] = "Microsoft.VisualStudio.Tools.Applications.InteropAdapter, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Tools.Applications.Runtime"] = "Microsoft.VisualStudio.Tools.Applications.Runtime, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Tools.Applications.ServerDocument"] = "Microsoft.VisualStudio.Tools.Applications.ServerDocument, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Tools.Office"] = "Microsoft.VisualStudio.Tools.Office, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Tools.Office.AddInAdapter"] = "Microsoft.VisualStudio.Tools.Office.AddInAdapter, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Tools.Office.AddInHostAdapter"] = "Microsoft.VisualStudio.Tools.Office.AddInHostAdapter, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Tools.Office.AppInfoDocument"] = "Microsoft.VisualStudio.Tools.Office.AppInfoDocument, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Tools.Office.Common"] = "Microsoft.VisualStudio.Tools.Office.Common, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Tools.Office.Contract"] = "Microsoft.VisualStudio.Tools.Office.Contract, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Tools.Office.Controls.ContainerControl"] = "Microsoft.VisualStudio.Tools.Office.Controls.ContainerControl, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Tools.Office.Excel"] = "Microsoft.VisualStudio.Tools.Office.Excel, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Tools.Office.Excel.Adapter"] = "Microsoft.VisualStudio.Tools.Office.Excel.Adapter, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Tools.Office.Excel.AddInAdapter"] = "Microsoft.VisualStudio.Tools.Office.Excel.AddInAdapter, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Tools.Office.Excel.AddInProxy"] = "Microsoft.VisualStudio.Tools.Office.Excel.AddInProxy, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Tools.Office.Excel.Contract"] = "Microsoft.VisualStudio.Tools.Office.Excel.Contract, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Tools.Office.Outlook"] = "Microsoft.VisualStudio.Tools.Office.Outlook, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Tools.Office.Outlook.Adapter"] = "Microsoft.VisualStudio.Tools.Office.Outlook.Adapter, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Tools.Office.Runtime"] = "Microsoft.VisualStudio.Tools.Office.Runtime, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Tools.Office.Word"] = "Microsoft.VisualStudio.Tools.Office.Word, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Tools.Office.Word.Adapter"] = "Microsoft.VisualStudio.Tools.Office.Word.Adapter, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Tools.Office.Word.AddInAdapter"] = "Microsoft.VisualStudio.Tools.Office.Word.AddInAdapter, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Tools.Office.Word.AddInProxy"] = "Microsoft.VisualStudio.Tools.Office.Word.AddInProxy, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Tools.Office.Word.Contract"] = "Microsoft.VisualStudio.Tools.Office.Word.Contract, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.VCCodeModel"] = "Microsoft.VisualStudio.VCCodeModel, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.VCProject"] = "Microsoft.VisualStudio.VCProject, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.VCProjectEngine"] = "Microsoft.VisualStudio.VCProjectEngine, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.VirtualTreeGrid"] = "Microsoft.VisualStudio.VirtualTreeGrid, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.VSContentInstaller"] = "Microsoft.VisualStudio.VSContentInstaller, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.VSHelp"] = "Microsoft.VisualStudio.VSHelp, Version=7.0.3300.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.VSHelp80"] = "Microsoft.VisualStudio.VSHelp80, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Windows.Forms"] = "Microsoft.VisualStudio.Windows.Forms, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.WizardFramework"] = "Microsoft.VisualStudio.WizardFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VisualStudio.Zip"] = "Microsoft.VisualStudio.Zip, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.Vsa"] = "Microsoft.Vsa, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.Vsa.Vb.CodeDOMProcessor"] = "Microsoft.Vsa.Vb.CodeDOMProcessor, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.VSDesigner"] = "Microsoft.VSDesigner, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Microsoft.Windows.ApplicationServer.Applications"] = "Microsoft.Windows.ApplicationServer.Applications, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.ApplicationServer.ServerManager.Plugin"] = "Microsoft.Windows.ApplicationServer.ServerManager.Plugin, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.ApplicationServer.ServerManager.Plugin.resources"] = "Microsoft.Windows.ApplicationServer.ServerManager.Plugin.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.Appx.PackageManager.Commands"] = "Microsoft.Windows.Appx.PackageManager.Commands, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.Appx.PackageManager.Commands.resources"] = "Microsoft.Windows.Appx.PackageManager.Commands.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.DeploymentServices.ServerManager.Plugin"] = "Microsoft.Windows.DeploymentServices.ServerManager.Plugin, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.DeploymentServices.ServerManager.Plugin.resources"] = "Microsoft.Windows.DeploymentServices.ServerManager.Plugin.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.Diagnosis.Commands.GetDiagInput"] = "Microsoft.Windows.Diagnosis.Commands.GetDiagInput, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.Diagnosis.Commands.GetDiagInput.resources"] = "Microsoft.Windows.Diagnosis.Commands.GetDiagInput.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.Diagnosis.Commands.UpdateDiagReport"] = "Microsoft.Windows.Diagnosis.Commands.UpdateDiagReport, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.Diagnosis.Commands.UpdateDiagReport.resources"] = "Microsoft.Windows.Diagnosis.Commands.UpdateDiagReport.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.Diagnosis.Commands.UpdateDiagRootcause"] = "Microsoft.Windows.Diagnosis.Commands.UpdateDiagRootcause, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.Diagnosis.Commands.UpdateDiagRootcause.resources"] = "Microsoft.Windows.Diagnosis.Commands.UpdateDiagRootcause.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.Diagnosis.Commands.WriteDiagProgress"] = "Microsoft.Windows.Diagnosis.Commands.WriteDiagProgress, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.Diagnosis.Commands.WriteDiagProgress.resources"] = "Microsoft.Windows.Diagnosis.Commands.WriteDiagProgress.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.Diagnosis.SDCommon"] = "Microsoft.Windows.Diagnosis.SDCommon, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.Diagnosis.SDEngine"] = "Microsoft.Windows.Diagnosis.SDEngine, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.Diagnosis.SDHost"] = "Microsoft.Windows.Diagnosis.SDHost, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.Diagnosis.SDHost.resources"] = "Microsoft.Windows.Diagnosis.SDHost.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.Diagnosis.TroubleshootingPack"] = "Microsoft.Windows.Diagnosis.TroubleshootingPack, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.Diagnosis.TroubleshootingPack.resources"] = "Microsoft.Windows.Diagnosis.TroubleshootingPack.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.Dns"] = "Microsoft.Windows.Dns, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.Dns.resources"] = "Microsoft.Windows.Dns.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.FileServer.Management.Common"] = "Microsoft.Windows.FileServer.Management.Common, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.FileServer.Management.Common.resources"] = "Microsoft.Windows.FileServer.Management.Common.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.FileServer.Management.Plugin"] = "Microsoft.Windows.FileServer.Management.Plugin, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.FileServer.Management.Plugin.resources"] = "Microsoft.Windows.FileServer.Management.Plugin.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.FileServer.Management.Plugin.UI"] = "Microsoft.Windows.FileServer.Management.Plugin.UI, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.FileServer.Management.Plugin.UI.resources"] = "Microsoft.Windows.FileServer.Management.Plugin.UI.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.FileServer.Management.ServerManagerProxy"] = "Microsoft.Windows.FileServer.Management.ServerManagerProxy, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.Server"] = "Microsoft.Windows.Server, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.Server.resources"] = "Microsoft.Windows.Server.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.ServerManager.Activities"] = "Microsoft.Windows.ServerManager.Activities, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.ServerManager.Common"] = "Microsoft.Windows.ServerManager.Common, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.ServerManager.Common.resources"] = "Microsoft.Windows.ServerManager.Common.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.ServerManager.Deployment.Extension"] = "Microsoft.Windows.ServerManager.Deployment.Extension, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.ServerManager.DhcpServer.Plugin"] = "Microsoft.Windows.ServerManager.DhcpServer.Plugin, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.ServerManager.DhcpServer.Plugin.resources"] = "Microsoft.Windows.ServerManager.DhcpServer.Plugin.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.ServerManager.FaxServer.Plugin"] = "Microsoft.Windows.ServerManager.FaxServer.Plugin, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.ServerManager.FaxServer.Plugin.resources"] = "Microsoft.Windows.ServerManager.FaxServer.Plugin.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.ServerManager.Ipam.Plugin"] = "Microsoft.Windows.ServerManager.Ipam.Plugin, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.ServerManager.Ipam.Plugin.resources"] = "Microsoft.Windows.ServerManager.Ipam.Plugin.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.ServerManager.NPASRole.Plugin"] = "Microsoft.Windows.ServerManager.NPASRole.Plugin, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.ServerManager.NPASRole.Plugin.resources"] = "Microsoft.Windows.ServerManager.NPASRole.Plugin.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.ServerManager.PowerShell"] = "Microsoft.Windows.ServerManager.PowerShell, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.ServerManager.PowerShell.resources"] = "Microsoft.Windows.ServerManager.PowerShell.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.ServerManager.PrintingServer.Plugin"] = "Microsoft.Windows.ServerManager.PrintingServer.Plugin, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.ServerManager.PrintingServer.Plugin.resources"] = "Microsoft.Windows.ServerManager.PrintingServer.Plugin.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.ServerManager.RDSPlugin"] = "Microsoft.Windows.ServerManager.RDSPlugin, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.ServerManager.RDSPlugin.resources"] = "Microsoft.Windows.ServerManager.RDSPlugin.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.ServerManager.RemoteAccess.Plugin"] = "Microsoft.Windows.ServerManager.RemoteAccess.Plugin, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.ServerManager.RemoteAccess.Plugin.resources"] = "Microsoft.Windows.ServerManager.RemoteAccess.Plugin.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.ServerManager.ServerComponentDeploymentWizard"] = "Microsoft.Windows.ServerManager.ServerComponentDeploymentWizard, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.ServerManager.ServerComponentDeploymentWizard.resources"] = "Microsoft.Windows.ServerManager.ServerComponentDeploymentWizard.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.ServerManager.ServerComponentManager"] = "Microsoft.Windows.ServerManager.ServerComponentManager, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.ServerManager.WebServerRole.Plugin"] = "Microsoft.Windows.ServerManager.WebServerRole.Plugin, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.ServerManager.WebServerRole.Plugin.resources"] = "Microsoft.Windows.ServerManager.WebServerRole.Plugin.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.ServerManagerToolTask.Telemetry"] = "Microsoft.Windows.ServerManagerToolTask.Telemetry, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.ServerManagerToolTask.Telemetry.resources"] = "Microsoft.Windows.ServerManagerToolTask.Telemetry.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.Ual"] = "Microsoft.Windows.Ual, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.VolumeActivation.Plugin"] = "Microsoft.Windows.VolumeActivation.Plugin, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Windows.VolumeActivation.Plugin.resources"] = "Microsoft.Windows.VolumeActivation.Plugin.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.WSMan.Management"] = "Microsoft.WSMan.Management, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.WSMan.Runtime"] = "Microsoft.WSMan.Runtime, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Microsoft.Wtt.Log"] = "Microsoft.Wtt.Log, Version=2.0.0.0, Culture=neutral, PublicKeyToken=8a96d095ee9fe264";
            strongNames["Microsoft_VsaVb"] = "Microsoft_VsaVb, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["MIGUIControls"] = "MIGUIControls, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["MIGUIControls.resources"] = "MIGUIControls.resources, Version=1.0.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["MMCEx"] = "MMCEx, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["MMCEx.resources"] = "MMCEx.resources, Version=3.0.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["MMCFxCommon"] = "MMCFxCommon, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["MMCFxCommon.resources"] = "MMCFxCommon.resources, Version=3.0.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["MSClusterLib"] = "MSClusterLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91";
            strongNames["mscomctl"] = "mscomctl, Version=10.0.4504.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["mscorcfg"] = "mscorcfg, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["mscorlib"] = "mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["mscorlib.4"] = "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["MSDATASRC"] = "MSDATASRC, Version=7.0.3300.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["msddslmp"] = "msddslmp, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["msddsp"] = "msddsp, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["napcrypt"] = "napcrypt, Version=6.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["napcrypt.6.2"] = "napcrypt, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["naphlpr"] = "naphlpr, Version=6.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["naphlpr.6.2"] = "naphlpr, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["napinit"] = "napinit, Version=6.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["napinit.6.2"] = "napinit, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["napinit.resources"] = "napinit.resources, Version=6.0.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["napinit.resources.6.2"] = "napinit.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["napsnap"] = "napsnap, Version=6.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["napsnap.6.2"] = "napsnap, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["napsnap.resources"] = "napsnap.resources, Version=6.0.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["napsnap.resources.6.2"] = "napsnap.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["office"] = "office, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c";
            strongNames["Policy.1.0.Microsoft.Ink"] = "Policy.1.0.Microsoft.Ink, Version=6.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Policy.1.0.Microsoft.Interop.Security.AzRoles"] = "Policy.1.0.Microsoft.Interop.Security.AzRoles, Version=6.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["policy.1.0.System.Web.Extensions"] = "policy.1.0.System.Web.Extensions, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["policy.1.0.System.Web.Extensions.Design"] = "policy.1.0.System.Web.Extensions.Design, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Policy.1.2.Microsoft.Interop.Security.AzRoles"] = "Policy.1.2.Microsoft.Interop.Security.AzRoles, Version=6.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Policy.1.7.Microsoft.Ink"] = "Policy.1.7.Microsoft.Ink, Version=6.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Policy.11.0.Microsoft.Office.Interop.Access"] = "Policy.11.0.Microsoft.Office.Interop.Access, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c";
            strongNames["Policy.11.0.Microsoft.Office.Interop.Excel"] = "Policy.11.0.Microsoft.Office.Interop.Excel, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c";
            strongNames["Policy.11.0.Microsoft.Office.Interop.Graph"] = "Policy.11.0.Microsoft.Office.Interop.Graph, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c";
            strongNames["Policy.11.0.Microsoft.Office.Interop.InfoPath"] = "Policy.11.0.Microsoft.Office.Interop.InfoPath, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c";
            strongNames["Policy.11.0.Microsoft.Office.Interop.InfoPath.Xml"] = "Policy.11.0.Microsoft.Office.Interop.InfoPath.Xml, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c";
            strongNames["Policy.11.0.Microsoft.Office.Interop.Outlook"] = "Policy.11.0.Microsoft.Office.Interop.Outlook, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c";
            strongNames["Policy.11.0.Microsoft.Office.Interop.OutlookViewCtl"] = "Policy.11.0.Microsoft.Office.Interop.OutlookViewCtl, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c";
            strongNames["Policy.11.0.Microsoft.Office.Interop.PowerPoint"] = "Policy.11.0.Microsoft.Office.Interop.PowerPoint, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c";
            strongNames["Policy.11.0.Microsoft.Office.Interop.Publisher"] = "Policy.11.0.Microsoft.Office.Interop.Publisher, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c";
            strongNames["Policy.11.0.Microsoft.Office.Interop.SmartTag"] = "Policy.11.0.Microsoft.Office.Interop.SmartTag, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c";
            strongNames["Policy.11.0.Microsoft.Office.Interop.Word"] = "Policy.11.0.Microsoft.Office.Interop.Word, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c";
            strongNames["Policy.11.0.Microsoft.Vbe.Interop"] = "Policy.11.0.Microsoft.Vbe.Interop, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c";
            strongNames["Policy.11.0.office"] = "Policy.11.0.office, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c";
            strongNames["PresentationBuildTasks"] = "PresentationBuildTasks, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["PresentationBuildTasks.4"] = "PresentationBuildTasks, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["PresentationCFFRasterizer"] = "PresentationCFFRasterizer, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["PresentationCore"] = "PresentationCore, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["PresentationCore.4"] = "PresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["PresentationFramework"] = "PresentationFramework, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["PresentationFramework.4"] = "PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["PresentationFramework.Aero"] = "PresentationFramework.Aero, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["PresentationFramework.Aero.4"] = "PresentationFramework.Aero, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["PresentationFramework.Classic"] = "PresentationFramework.Classic, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["PresentationFramework.Classic.4"] = "PresentationFramework.Classic, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["PresentationFramework.Luna"] = "PresentationFramework.Luna, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["PresentationFramework.Luna.4"] = "PresentationFramework.Luna, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["PresentationFramework.Royale"] = "PresentationFramework.Royale, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["PresentationFramework.Royale.4"] = "PresentationFramework.Royale, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["PresentationFramework-SystemCore"] = "PresentationFramework-SystemCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["PresentationFramework-SystemData"] = "PresentationFramework-SystemData, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["PresentationFramework-SystemDrawing"] = "PresentationFramework-SystemDrawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["PresentationFramework-SystemXml"] = "PresentationFramework-SystemXml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["PresentationFramework-SystemXmlLinq"] = "PresentationFramework-SystemXmlLinq, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["PresentationUI"] = "PresentationUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["PresentationUI.4"] = "PresentationUI, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["ReachFramework"] = "ReachFramework, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["ReachFramework.4"] = "ReachFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Regcode"] = "Regcode, Version=1.0.5000.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["SecurityAuditPoliciesSnapIn"] = "SecurityAuditPoliciesSnapIn, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["SecurityAuditPoliciesSnapIn.resources"] = "SecurityAuditPoliciesSnapIn.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["SMDiagnostics"] = "SMDiagnostics, Version=3.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["SMDiagnostics.4"] = "SMDiagnostics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["soapsudscode"] = "soapsudscode, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["SrpUxSnapIn"] = "SrpUxSnapIn, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["SrpUxSnapIn.resources"] = "SrpUxSnapIn.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["stdole"] = "stdole, Version=7.0.3300.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["StorageMgmt"] = "StorageMgmt, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["StorageMgmt.resources"] = "StorageMgmt.resources, Version=1.0.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["sysglobl"] = "sysglobl, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["sysglobl.4"] = "sysglobl, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System"] = "System, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.4"] = "System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.Activities"] = "System.Activities, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["System.Activities.Core.Presentation"] = "System.Activities.Core.Presentation, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["System.Activities.DurableInstancing"] = "System.Activities.DurableInstancing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["System.Activities.Presentation"] = "System.Activities.Presentation, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["System.Activities.Statements"] = "System.Activities.Statements, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["System.AddIn"] = "System.AddIn, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.AddIn.4"] = "System.AddIn, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.AddIn.Contract"] = "System.AddIn.Contract, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.AddIn.Contract.4"] = "System.AddIn.Contract, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Collections"] = "System.Collections, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Collections.Concurrent"] = "System.Collections.Concurrent, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Collections.ObjectModel"] = "System.Collections.ObjectModel, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.ComponentModel"] = "System.ComponentModel, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.ComponentModel.Composition"] = "System.ComponentModel.Composition, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.ComponentModel.Composition.AttributedModel"] = "System.ComponentModel.Composition.AttributedModel, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.ComponentModel.Composition.Hosting"] = "System.ComponentModel.Composition.Hosting, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.ComponentModel.Composition.Primitives"] = "System.ComponentModel.Composition.Primitives, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.ComponentModel.Composition.Registration"] = "System.ComponentModel.Composition.Registration, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.ComponentModel.DataAnnotations"] = "System.ComponentModel.DataAnnotations, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["System.ComponentModel.EventBasedAsync"] = "System.ComponentModel.EventBasedAsync, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Configuration"] = "System.Configuration, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Configuration.4"] = "System.Configuration, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Configuration.Install"] = "System.Configuration.Install, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Configuration.Install.4"] = "System.Configuration.Install, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Core"] = "System.Core, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.Core.4"] = "System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.Data"] = "System.Data, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.Data.4"] = "System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.Data.DataSetExtensions"] = "System.Data.DataSetExtensions, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.Data.DataSetExtensions.4"] = "System.Data.DataSetExtensions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.Data.Entity"] = "System.Data.Entity, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.Data.Entity.Design"] = "System.Data.Entity.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.Data.Linq"] = "System.Data.Linq, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.Data.Linq.4"] = "System.Data.Linq, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.Data.OracleClient"] = "System.Data.OracleClient, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.Data.OracleClient.4"] = "System.Data.OracleClient, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.Data.Services"] = "System.Data.Services, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.Data.Services.Client"] = "System.Data.Services.Client, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.Data.Services.Design"] = "System.Data.Services.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.Data.SqlXml"] = "System.Data.SqlXml, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.Data.SqlXml.4"] = "System.Data.SqlXml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.Deployment"] = "System.Deployment, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Deployment.4"] = "System.Deployment, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Design"] = "System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Design.4"] = "System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Device"] = "System.Device, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.Diagnostics.Contracts"] = "System.Diagnostics.Contracts, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Diagnostics.Debug"] = "System.Diagnostics.Debug, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Diagnostics.Tools"] = "System.Diagnostics.Tools, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Diagnostics.Tracing"] = "System.Diagnostics.Tracing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.DirectoryServices"] = "System.DirectoryServices, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.DirectoryServices.4"] = "System.DirectoryServices, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.DirectoryServices.AccountManagement"] = "System.DirectoryServices.AccountManagement, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.DirectoryServices.AccountManagement.4"] = "System.DirectoryServices.AccountManagement, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.DirectoryServices.Protocols"] = "System.DirectoryServices.Protocols, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.DirectoryServices.Protocols.4"] = "System.DirectoryServices.Protocols, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Drawing"] = "System.Drawing, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Drawing.4"] = "System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Drawing.Design"] = "System.Drawing.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Drawing.Design.4"] = "System.Drawing.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Dynamic"] = "System.Dynamic, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Dynamic.Runtime"] = "System.Dynamic.Runtime, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.EnterpriseServices"] = "System.EnterpriseServices, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.EnterpriseServices.4"] = "System.EnterpriseServices, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Globalization"] = "System.Globalization, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.IdentityModel"] = "System.IdentityModel, Version=3.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.IdentityModel.4"] = "System.IdentityModel, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.IdentityModel.Selectors"] = "System.IdentityModel.Selectors, Version=3.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.IdentityModel.Selectors.4"] = "System.IdentityModel.Selectors, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.IdentityModel.Services"] = "System.IdentityModel.Services, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.IO"] = "System.IO, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.IO.Compression"] = "System.IO.Compression, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.IO.Compression.FileSystem"] = "System.IO.Compression.FileSystem, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.IO.Log"] = "System.IO.Log, Version=3.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.IO.Log.4"] = "System.IO.Log, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Linq"] = "System.Linq, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Linq.Expressions"] = "System.Linq.Expressions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Linq.Parallel"] = "System.Linq.Parallel, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Linq.Queryable"] = "System.Linq.Queryable, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Management"] = "System.Management, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Management.4"] = "System.Management, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Management.Automation"] = "System.Management.Automation, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["System.Management.Automation.resources"] = "System.Management.Automation.resources, Version=3.0.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["System.Management.Instrumentation"] = "System.Management.Instrumentation, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.Management.Instrumentation.4"] = "System.Management.Instrumentation, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.Messaging"] = "System.Messaging, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Messaging.4"] = "System.Messaging, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Net"] = "System.Net, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Net.4"] = "System.Net, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Net.Http"] = "System.Net.Http, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Net.Http.WebRequest"] = "System.Net.Http.WebRequest, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Net.NetworkInformation"] = "System.Net.NetworkInformation, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Net.Primitives"] = "System.Net.Primitives, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Net.Requests"] = "System.Net.Requests, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Numerics"] = "System.Numerics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.Printing"] = "System.Printing, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["System.Printing.4"] = "System.Printing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["System.Reflection"] = "System.Reflection, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Reflection.Context"] = "System.Reflection.Context, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.Reflection.Extensions"] = "System.Reflection.Extensions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Resources.ResourceManager"] = "System.Resources.ResourceManager, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Runtime"] = "System.Runtime, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Runtime.Caching"] = "System.Runtime.Caching, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Runtime.DurableInstancing"] = "System.Runtime.DurableInstancing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["System.Runtime.Extensions"] = "System.Runtime.Extensions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Runtime.InteropServices"] = "System.Runtime.InteropServices, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Runtime.InteropServices.WindowsRuntime"] = "System.Runtime.InteropServices.WindowsRuntime, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Runtime.Remoting"] = "System.Runtime.Remoting, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.Runtime.Remoting.4"] = "System.Runtime.Remoting, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.Runtime.Serialization"] = "System.Runtime.Serialization, Version=3.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.Runtime.Serialization.4"] = "System.Runtime.Serialization, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.Runtime.Serialization.Formatters.Soap"] = "System.Runtime.Serialization.Formatters.Soap, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Runtime.Serialization.Formatters.Soap.4"] = "System.Runtime.Serialization.Formatters.Soap, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Runtime.WindowsRuntime"] = "System.Runtime.WindowsRuntime, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.Runtime.WindowsRuntime.UI.Xaml"] = "System.Runtime.WindowsRuntime.UI.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Security"] = "System.Security, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Security.4"] = "System.Security, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Security.Principal"] = "System.Security.Principal, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Serialization.DataContract"] = "System.Serialization.DataContract, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Serialization.DataContract.JsonSerializer"] = "System.Serialization.DataContract.JsonSerializer, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Serialization.DataContract.Serializer"] = "System.Serialization.DataContract.Serializer, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Serialization.Xml"] = "System.Serialization.Xml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.ServiceModel"] = "System.ServiceModel, Version=3.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.ServiceModel.4"] = "System.ServiceModel, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.ServiceModel.Activation"] = "System.ServiceModel.Activation, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["System.ServiceModel.Activities"] = "System.ServiceModel.Activities, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["System.ServiceModel.Channels"] = "System.ServiceModel.Channels, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["System.ServiceModel.Discovery"] = "System.ServiceModel.Discovery, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["System.ServiceModel.Duplex"] = "System.ServiceModel.Duplex, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.ServiceModel.Http"] = "System.ServiceModel.Http, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.ServiceModel.Install"] = "System.ServiceModel.Install, Version=3.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.ServiceModel.Internals"] = "System.ServiceModel.Internals, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["System.ServiceModel.NetTcp"] = "System.ServiceModel.NetTcp, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.ServiceModel.Primitives"] = "System.ServiceModel.Primitives, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.ServiceModel.Routing"] = "System.ServiceModel.Routing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["System.ServiceModel.Security"] = "System.ServiceModel.Security, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.ServiceModel.ServiceMoniker40"] = "System.ServiceModel.ServiceMoniker40, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.ServiceModel.WasHosting"] = "System.ServiceModel.WasHosting, Version=3.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.ServiceModel.WasHosting.4"] = "System.ServiceModel.WasHosting, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.ServiceModel.Web"] = "System.ServiceModel.Web, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["System.ServiceModel.Web.4"] = "System.ServiceModel.Web, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["System.ServiceModel.XmlSerializer"] = "System.ServiceModel.XmlSerializer, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.ServiceProcess"] = "System.ServiceProcess, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.ServiceProcess.4"] = "System.ServiceProcess, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Speech"] = "System.Speech, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["System.Speech.4"] = "System.Speech, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["System.Text.Encoding"] = "System.Text.Encoding, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Text.RegularExpressions"] = "System.Text.RegularExpressions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Threading"] = "System.Threading, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Threading.Tasks"] = "System.Threading.Tasks, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Threading.Tasks.Dataflow"] = "System.Threading.Tasks.Dataflow, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.Threading.Tasks.Parallel"] = "System.Threading.Tasks.Parallel, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Transactions"] = "System.Transactions, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.Transactions.4"] = "System.Transactions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.Web"] = "System.Web, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Web.4"] = "System.Web, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Web.Abstractions"] = "System.Web.Abstractions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["System.Web.ApplicationServices"] = "System.Web.ApplicationServices, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["System.Web.DataVisualization"] = "System.Web.DataVisualization, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["System.Web.DataVisualization.Design"] = "System.Web.DataVisualization.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["System.Web.DynamicData"] = "System.Web.DynamicData, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["System.Web.DynamicData.Design"] = "System.Web.DynamicData.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["System.Web.Entity"] = "System.Web.Entity, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.Web.Entity.Design"] = "System.Web.Entity.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.Web.Extensions"] = "System.Web.Extensions, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["System.Web.Extensions.4"] = "System.Web.Extensions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["System.Web.Extensions.Design"] = "System.Web.Extensions.Design, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["System.Web.Extensions.Design.4"] = "System.Web.Extensions.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["System.Web.Mobile"] = "System.Web.Mobile, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Web.Mobile.4"] = "System.Web.Mobile, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Web.RegularExpressions"] = "System.Web.RegularExpressions, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Web.RegularExpressions.4"] = "System.Web.RegularExpressions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Web.Routing"] = "System.Web.Routing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["System.Web.Services"] = "System.Web.Services, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Web.Services.4"] = "System.Web.Services, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Windows.Controls.Ribbon"] = "System.Windows.Controls.Ribbon, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.Windows.Forms"] = "System.Windows.Forms, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.Windows.Forms.4"] = "System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.Windows.Forms.DataVisualization"] = "System.Windows.Forms.DataVisualization, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["System.Windows.Forms.DataVisualization.Design"] = "System.Windows.Forms.DataVisualization.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["System.Windows.Input.Manipulations"] = "System.Windows.Input.Manipulations, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.Windows.Presentation"] = "System.Windows.Presentation, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.Windows.Presentation.4"] = "System.Windows.Presentation, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.Workflow.Activities"] = "System.Workflow.Activities, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["System.Workflow.Activities.4"] = "System.Workflow.Activities, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["System.Workflow.ComponentModel"] = "System.Workflow.ComponentModel, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["System.Workflow.ComponentModel.4"] = "System.Workflow.ComponentModel, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["System.Workflow.Runtime"] = "System.Workflow.Runtime, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["System.Workflow.Runtime.4"] = "System.Workflow.Runtime, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["System.WorkflowServices"] = "System.WorkflowServices, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["System.WorkflowServices.4"] = "System.WorkflowServices, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["System.Xaml"] = "System.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.Xaml.Hosting"] = "System.Xaml.Hosting, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["System.Xml"] = "System.Xml, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.Xml.4"] = "System.Xml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.Xml.Linq"] = "System.Xml.Linq, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.Xml.Linq.4"] = "System.Xml.Linq, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["System.Xml.ReaderWriter"] = "System.Xml.ReaderWriter, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["System.Xml.Serialization"] = "System.Xml.Serialization, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            strongNames["TaskScheduler"] = "TaskScheduler, Version=6.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["TaskScheduler.6.2"] = "TaskScheduler, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["TaskScheduler.resources"] = "TaskScheduler.resources, Version=6.0.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["TaskScheduler.resources.6.2"] = "TaskScheduler.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["UIAutomationClient"] = "UIAutomationClient, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["UIAutomationClient.4"] = "UIAutomationClient, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["UIAutomationClientsideProviders"] = "UIAutomationClientsideProviders, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["UIAutomationClientsideProviders.4"] = "UIAutomationClientsideProviders, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["UIAutomationProvider"] = "UIAutomationProvider, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["UIAutomationProvider.4"] = "UIAutomationProvider, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["UIAutomationTypes"] = "UIAutomationTypes, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["UIAutomationTypes.4"] = "UIAutomationTypes, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["vjscor"] = "vjscor, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["VJSharpCodeProvider"] = "VJSharpCodeProvider, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["vjsjbc"] = "vjsjbc, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["vjslib"] = "vjslib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["vjslibcw"] = "vjslibcw, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["VJSSupUILib"] = "VJSSupUILib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["vjsvwaux"] = "vjsvwaux, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["vjswfc"] = "vjswfc, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["VjsWfcBrowserStubLib"] = "VjsWfcBrowserStubLib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["vjswfccw"] = "vjswfccw, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["vjswfchtml"] = "vjswfchtml, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["VSLangProj"] = "VSLangProj, Version=7.0.3300.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["VSLangProj2"] = "VSLangProj2, Version=7.0.5000.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["VSLangProj80"] = "VSLangProj80, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["VSTOPersist.Interop"] = "VSTOPersist.Interop, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["VSTOStorageWrapper.Interop"] = "VSTOStorageWrapper.Interop, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["VsWebSite.Interop"] = "VsWebSite.Interop, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["WebDev.WebHost"] = "WebDev.WebHost, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            strongNames["Windows.ServerManagerPlugin.CEIPForwarding.Deploy"] = "Windows.ServerManagerPlugin.CEIPForwarding.Deploy, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["Windows.ServerManagerPlugin.CEIPForwarding.Deploy.resources"] = "Windows.ServerManagerPlugin.CEIPForwarding.Deploy.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["WindowsBase"] = "WindowsBase, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["WindowsBase.4"] = "WindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["WindowsFormsIntegration"] = "WindowsFormsIntegration, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["WindowsFormsIntegration.4"] = "WindowsFormsIntegration, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["wsbmmc"] = "wsbmmc, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["wsbmmc.resources"] = "wsbmmc.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["wsbsnapincommon"] = "wsbsnapincommon, Version=6.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["wsbsnapincommon.resources"] = "wsbsnapincommon.resources, Version=6.2.0.0, Culture=en, PublicKeyToken=31bf3856ad364e35";
            strongNames["XamlBuildTask"] = "XamlBuildTask, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            strongNames["XsdBuildTask"] = "XsdBuildTask, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";

            return strongNames;
        }
    }
#endif // if !CORECLR
}

