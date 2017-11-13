/********************************************************************++
Copyright (c) Microsoft Corporation. All rights reserved.
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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.IO;
using Microsoft.CodeAnalysis.Emit;
using System.Collections.Immutable;
using System.Security;
using PathType = System.IO.Path;

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

        /// <summary>
        /// The C# programming language v7
        /// </summary>
        CSharpVersion7,

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

        /// <summary>
        /// The C# programming language v3 (for Linq, etc)
        /// </summary>
        CSharpVersion3,

        /// <summary>
        /// The C# programming language v2
        /// </summary>
        CSharpVersion2,

        /// <summary>
        /// The C# programming language v1
        /// </summary>
        CSharpVersion1,

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
                    // for a better error message.
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
                string currentExtension = PathType.GetExtension(path).ToUpperInvariant();

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
                if (value != null) { referencedAssemblies = value; }
            }
        }
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
                case Language.CSharpVersion1:
                case Language.CSharpVersion4:
                case Language.CSharpVersion5:
                case Language.CSharpVersion6:
                case Language.CSharpVersion7:
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
        /// We only keep the code for backward compatibility.
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

        // We only keep the code for backward compatibility.
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
                    actualSource = System.IO.File.ReadAllLines(error.FileName);
                }
            }

            string errorText = StringUtil.Format(AddTypeStrings.CompilationErrorFormat,
                        error.FileName, error.Line, error.ErrorText) + Environment.NewLine;

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

                    errorText += Environment.NewLine + StringUtil.Format(AddTypeStrings.CompilationErrorFormat,
                        error.FileName, lineNumber, lineText) + Environment.NewLine;
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

    /// <summary>
    /// Adds a new type to the Application Domain.
    /// This version is based on CodeAnalysis (Roslyn).
    /// </summary>
    [Cmdlet(VerbsCommon.Add, "Type", DefaultParameterSetName = "FromSource", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=135195")]
    [OutputType(typeof(Type))]
    public sealed class AddTypeCommand : AddTypeCommandBase
    {
        private static Dictionary<string, int> s_sourceCache = new Dictionary<string, int>();

        /// <summary>
        /// Generate the type(s)
        /// </summary>
        protected override void EndProcessing()
        {
            // Prevent code compilation in ConstrainedLanguage mode
            if (SessionState.LanguageMode == PSLanguageMode.ConstrainedLanguage)
            {
                ThrowTerminatingError(
                    new ErrorRecord(
                        new PSNotSupportedException(AddTypeStrings.CannotDefineNewType), "CannotDefineNewType", ErrorCategory.PermissionDenied, null));
            }

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
                    String.Equals(ParameterSetName, "FromLiteralPath", StringComparison.OrdinalIgnoreCase)
                   )
                {
                    if (paths.Length == 1)
                    {
                        sourceCode = File.ReadAllText(paths[0]);
                    }
                    else
                    {

                        // We replace 'ReadAllText' with 'StringBuilder' and 'ReadAllLines'
                        // to avoide temporary LOH allocations.

                        StringBuilder sb = new StringBuilder(8192);

                        foreach (string file in paths)
                        {
                            foreach (string line in File.ReadAllLines(file))
                            {
                                sb.AppendLine(line);
                            }
                        }

                        sourceCode = sb.ToString();
                    }
                }
                else if (String.Equals(ParameterSetName, "FromMember", StringComparison.OrdinalIgnoreCase))
                {
                    sourceCode = GenerateTypeSource(typeNamespace, Name, sourceCode, language);
                }

                CompileSourceToAssembly(this.sourceCode);
            }
        }

        private void LoadAssemblies(IEnumerable<string> assemblies)
        {
            foreach (string assemblyName in assemblies)
            {
                // CoreCLR doesn't allow re-load TPA assemblies with different API (i.e. we load them by name and now want to load by path).
                // LoadAssemblyHelper helps us avoid re-loading them, if they already loaded.
                Assembly assembly = LoadAssemblyHelper(assemblyName);
                if (assembly == null)
                {
                    assembly = Assembly.LoadFrom(ResolveAssemblyName(assemblyName, false));
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

        // We now ship the NetCoreApp2.0 reference assemblies with PowerShell Core, so that Add-Type can work
        // in a predictable way and won't be broken when we move to newer version of .NET Core.
        // The NetCoreApp2.0 reference assemblies are located at '$PSHOME\ref'.
        private static string s_netcoreAppRefFolder = PathType.Combine(PathType.GetDirectoryName(typeof(PSObject).Assembly.Location), "ref");
        private static string s_frameworkFolder = PathType.GetDirectoryName(typeof(object).Assembly.Location);

        // These assemblies are always automatically added to ReferencedAssemblies.
        private static Lazy<PortableExecutableReference[]> s_autoReferencedAssemblies = new Lazy<PortableExecutableReference[]>(InitAutoIncludedRefAssemblies);

        // A HashSet of assembly names to be ignored if they are specified in '-ReferencedAssemblies'
        private static Lazy<HashSet<string>> s_refAssemblyNamesToIgnore = new Lazy<HashSet<string>>(InitRefAssemblyNamesToIgnore);

        // These assemblies are used, when ReferencedAssemblies parameter is not specified.
        private static Lazy<PortableExecutableReference[]> s_defaultAssemblies = new Lazy<PortableExecutableReference[]>(InitDefaultRefAssemblies);

        private bool InMemory { get { return String.IsNullOrEmpty(outputAssembly); } }

        /// <summary>
        /// Initialize the list of reference assemblies that will be used when '-ReferencedAssemblies' is not specified.
        /// </summary>
        private static PortableExecutableReference[] InitDefaultRefAssemblies()
        {
            // netcoreapp2.0 currently comes with 137 reference assemblies (maybe more in future), so we use a capacity of '150'.
            var defaultRefAssemblies = new List<PortableExecutableReference>(150);
            foreach (string file in Directory.EnumerateFiles(s_netcoreAppRefFolder, "*.dll", SearchOption.TopDirectoryOnly))
            {
                defaultRefAssemblies.Add(MetadataReference.CreateFromFile(file));
            }
            defaultRefAssemblies.Add(MetadataReference.CreateFromFile(typeof(PSObject).Assembly.Location));
            return defaultRefAssemblies.ToArray();
        }

        /// <summary>
        /// Initialize the set of assembly names that should be ignored when they are specified in '-ReferencedAssemblies'.
        ///   - System.Private.CoreLib.ni.dll - the runtim dll that contains most core/primitive types
        ///   - System.Private.Uri.dll - the runtime dll that contains 'System.Uri' and related types
        /// Referencing these runtime dlls may cause ambiguous type identity or other issues.
        ///   - System.Runtime.dll - the corresponding reference dll will be automatically included
        ///   - System.Runtime.InteropServices.dll - the corresponding reference dll will be automatically included
        /// </summary>
        private static HashSet<string> InitRefAssemblyNamesToIgnore()
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
                PathType.GetFileName(typeof(object).Assembly.Location),
                PathType.GetFileName(typeof(Uri).Assembly.Location),
                PathType.GetFileName(GetReferenceAssemblyPathBasedOnType(typeof(object))),
                PathType.GetFileName(GetReferenceAssemblyPathBasedOnType(typeof(SecureString)))
            };
        }

        /// <summary>
        /// Initialize the list of reference assemblies that will be automatically added when '-ReferencedAssemblies' is specified.
        /// </summary>
        private static PortableExecutableReference[] InitAutoIncludedRefAssemblies()
        {
            return new PortableExecutableReference[] {
                MetadataReference.CreateFromFile(GetReferenceAssemblyPathBasedOnType(typeof(object))),
                MetadataReference.CreateFromFile(GetReferenceAssemblyPathBasedOnType(typeof(SecureString)))
            };
        }

        /// <summary>
        /// Get the path of reference assembly where the type is declared.
        /// </summary>
        private static string GetReferenceAssemblyPathBasedOnType(Type type)
        {
            string refAsmFileName = PathType.GetFileName(ClrFacade.GetAssemblies(type.FullName).First().Location);
            return PathType.Combine(s_netcoreAppRefFolder, refAsmFileName);
        }

        private string ResolveAssemblyName(string assembly, bool isForReferenceAssembly)
        {
            // if it's a path, resolve it
            if (assembly.Contains(PathType.DirectorySeparatorChar) || assembly.Contains(PathType.AltDirectorySeparatorChar))
            {
                if (PathType.IsPathRooted(assembly))
                {
                    return assembly;
                }
                else
                {
                    var paths = SessionState.Path.GetResolvedPSPathFromPSPath(assembly);
                    return paths[0].Path;
                }
            }

            string refAssemblyDll = assembly;
            if (!assembly.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                // It could be a short assembly name or a full assembly name, but we
                // alwasy want the short name to find the corresponding assembly file.
                var assemblyName = new AssemblyName(assembly);
                refAssemblyDll = assemblyName.Name + ".dll";
            }

            // We look up in reference/framework only when it's for resolving reference assemblies.
            // In case of 'Add-Type -AssemblyName' scenario, we don't attempt to resolve against framework assemblies because
            //   1. Explicitly loading a framework assembly usually is not necessary in PowerShell Core.
            //   2. A user should use assembly name instead of path if they want to explicitly load a framework assembly.
            if (isForReferenceAssembly)
            {
                // If it's for resolving a reference assembly, then we look in NetCoreApp ref assemblies first
                string netcoreAppRefPath = PathType.Combine(s_netcoreAppRefFolder, refAssemblyDll);
                if (File.Exists(netcoreAppRefPath))
                {
                    return netcoreAppRefPath;
                }

                // Look up the assembly in the framework folder. This may happen when assembly is not part of
                // NetCoreApp, but comes from an additional package, such as 'Json.Net'.
                string frameworkPossiblePath = PathType.Combine(s_frameworkFolder, refAssemblyDll);
                if (File.Exists(frameworkPossiblePath))
                {
                    return frameworkPossiblePath;
                }

                // The assembly name may point to a third-party assembly that is already loaded at run time.
                if (!assembly.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    Assembly result = LoadAssemblyHelper(assembly);
                    if (result != null)
                    {
                        return result.Location;
                    }
                }
            }

            // Look up the assembly in the current folder
            string currentFolderPath = SessionState.Path.GetResolvedPSPathFromPSPath(refAssemblyDll)[0].Path;
            if (File.Exists(currentFolderPath))
            {
                return currentFolderPath;
            }

            ErrorRecord errorRecord = new ErrorRecord(
                                new Exception(
                                    String.Format(ParserStrings.ErrorLoadingAssembly, assembly)),
                                "ErrorLoadingAssembly",
                                ErrorCategory.InvalidOperation,
                                assembly);

            ThrowTerminatingError(errorRecord);
            return null;
        }

        // LoadWithPartialName is deprecated, so we have to write the closest approximation possible.
        // However, this does give us a massive usability improvement, as users can just say
        // Add-Type -AssemblyName Forms (instead of System.Windows.Forms)
        // This is just long, not unmaintainable.
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
                    case Language.CSharpVersion7:
                        parseOptions = new CSharpParseOptions(LanguageVersion.CSharp7);
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
            var references = s_defaultAssemblies.Value;
            if (ReferencedAssemblies.Length > 0)
            {
                var tempReferences = new List<PortableExecutableReference>(s_autoReferencedAssemblies.Value);
                foreach (string assembly in ReferencedAssemblies)
                {
                    if (string.IsNullOrWhiteSpace(assembly)) { continue; }
                    string resolvedAssemblyPath = ResolveAssemblyName(assembly, true);

                    // Ignore some specified reference assemblies
                    string fileName = PathType.GetFileName(resolvedAssemblyPath);
                    if (s_refAssemblyNamesToIgnore.Value.Contains(fileName))
                    {
                        WriteVerbose(StringUtil.Format(AddTypeStrings.ReferenceAssemblyIgnored, resolvedAssemblyPath));
                        continue;
                    }
                    tempReferences.Add(MetadataReference.CreateFromFile(resolvedAssemblyPath));
                }
                references = tempReferences.ToArray();
            }

            CSharpCompilation compilation = CSharpCompilation.Create(
                PathType.GetRandomFileName(),
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
                        Assembly assembly = Assembly.Load(ms.ToArray());
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
                        Assembly assembly = Assembly.LoadFrom(outputAssembly);
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
    }
}
