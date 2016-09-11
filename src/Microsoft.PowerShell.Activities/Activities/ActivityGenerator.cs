//
//    Copyright (C) Microsoft.  All rights reserved.
//
﻿using System;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Reflection;
using Microsoft.CSharp;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.PowerShell.Cmdletization;
using Microsoft.PowerShell.Cmdletization.Xml;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Schema;
using System.CodeDom.Compiler;
using System.CodeDom;

namespace Microsoft.PowerShell.Activities
{
    /// <summary>
    /// Generates an activity that corresponds to a PowerShell command
    /// </summary>
    public static class ActivityGenerator
    {
        private static readonly Lazy<XmlSerializer> xmlSerializer = new Lazy<XmlSerializer>(ConstructXmlSerializer);
        private static readonly Lazy<XmlReaderSettings> xmlReaderSettings = new Lazy<XmlReaderSettings>(ConstructXmlReaderSettings);

        static XmlSerializer ConstructXmlSerializer()
        {
            XmlSerializer xmlSerializer = new Microsoft.PowerShell.Cmdletization.Xml.PowerShellMetadataSerializer();
            return xmlSerializer;
        }

        static XmlReaderSettings ConstructXmlReaderSettings()
        {
            //
            // XmlReaderSettings
            //
            XmlReaderSettings result = new XmlReaderSettings();
            // general settings
            result.CheckCharacters = true;
            result.CloseInput = false;
            result.ConformanceLevel = ConformanceLevel.Document;
            result.IgnoreComments = true;
            result.IgnoreProcessingInstructions = true;
            result.IgnoreWhitespace = false;
            result.MaxCharactersFromEntities = 16384; // generous guess for the upper bound
            result.MaxCharactersInDocument = 128 * 1024 * 1024; // generous guess for the upper bound
            result.DtdProcessing = DtdProcessing.Parse; // Allowing DTD parsing with limits of MaxCharactersFromEntities/MaxCharactersInDocument
            result.XmlResolver = null; // do not fetch external documents
            // xsd schema related settings
            result.ValidationFlags = XmlSchemaValidationFlags.ProcessIdentityConstraints |
                                                XmlSchemaValidationFlags.ReportValidationWarnings;
            result.ValidationType = ValidationType.Schema;
            string cmdletizationXsd = ActivityResources.Xml_cmdletsOverObjectsXsd;
            XmlReader cmdletizationSchemaReader = XmlReader.Create(new StringReader(cmdletizationXsd), result);
            result.Schemas = new XmlSchemaSet();
            result.Schemas.Add(null, cmdletizationSchemaReader);
            result.Schemas.XmlResolver = null; // do not fetch external documents

            return result;
        }

        static string templateCommand = @"
using Microsoft.PowerShell.Activities;
using System.Management.Automation;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;


namespace {0}
{{
    /// <summary>
    /// Activity to invoke the {1} command in a Workflow.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode(""Microsoft.PowerShell.Activities.ActivityGenerator.GenerateFromName"", ""3.0"")]
    public sealed class {2} : {6}
    {{
        /// <summary>
        /// Gets the display name of the command invoked by this activity.
        /// </summary>
        public {2}()
        {{
            this.DisplayName = ""{8}"";
        }}

        /// <summary>
        /// Gets the fully qualified name of the command invoked by this activity.
        /// </summary>
        public override string PSCommandName {{ get {{ return ""{4}""; }} }}
        
        // Arguments
        {3}

        // Module defining this command
        {7}

        // Optional custom code for this activity
        {9}

        /// <summary>
        /// Returns a configured instance of System.Management.Automation.PowerShell, pre-populated with the command to run.
        /// </summary>
        /// <param name=""context"">The NativeActivityContext for the currently running activity.</param>
        /// <returns>A populated instance of System.Management.Automation.PowerShell</returns>
        /// <remarks>The infrastructure takes responsibility for closing and disposing the PowerShell instance returned.</remarks>
        protected override ActivityImplementationContext GetPowerShell(NativeActivityContext context)
        {{
            System.Management.Automation.PowerShell invoker = global::System.Management.Automation.PowerShell.Create();
            System.Management.Automation.PowerShell targetCommand = invoker.AddCommand(PSCommandName);

            // Initialize the arguments
            {5}

            return new ActivityImplementationContext() {{ PowerShellInstance = invoker }};
        }}
    }}
}}";

        const string templateParameter = @"
        /// <summary>
        /// Provides access to the {1} parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<{0}> {1} {{ get; set; }}";

        const string templateParameterSetter = @"
            if({0}.Expression != null)
            {{
                targetCommand.AddParameter(""{1}"", {0}.Get(context));
            }}";

        const string customRemotingMapping = @"
            if(GetIsComputerNameSpecified(context) && (PSRemotingBehavior.Get(context) == RemotingBehavior.Custom))
            {
                targetCommand.AddParameter(""ComputerName"", PSComputerName.Get(context));
            }";

        const string supportsCustomRemoting = @"
        /// <summary>
        /// Declares that this activity supports its own remoting.
        /// </summary>        
        protected override bool SupportsCustomRemoting { get { return true; } }";

        /// <summary>
        /// Generate an activity for the named command.
        /// </summary>
        /// <param name="command">The command name to generate.</param>
        /// <param name="activityNamespace">The namespace that will contain the command - for example,
        /// Microsoft.PowerShell.Activities.
        /// </param>
        /// <returns>A string representing the C# source code of the generated activity.</returns>
        public static string GenerateFromName(string command, string activityNamespace)
        {
            return GenerateFromName(command, activityNamespace, false);
        }

        /// <summary>
        /// Generate an activity for the named command.
        /// </summary>
        /// <param name="command">The command name to generate.</param>
        /// <param name="activityNamespace">The namespace that will contain the command - for example,
        /// Microsoft.PowerShell.Activities.
        /// </param>
        /// <param name="shouldRunLocally">True if remoting-related parameters should be suppressed. This
        /// should only be specified for commands that offer no value when run on a remote computer.
        /// </param>
        /// <returns>A string representing the C# source code of the generated activity.</returns>
        public static string GenerateFromName(string command, string activityNamespace, bool shouldRunLocally)
        {
            StringBuilder output = new StringBuilder();

            // Get the command from the runspace
            using (System.Management.Automation.PowerShell invoker = System.Management.Automation.PowerShell.Create())
            {
                invoker.AddCommand("Get-Command").AddParameter("Name", command);
                Collection<CommandInfo> result = invoker.Invoke<CommandInfo>();

                if (result.Count == 0)
                {
                    string message = String.Format(CultureInfo.InvariantCulture, ActivityResources.ActivityNameNotFound, command);
                    throw new ArgumentException(message, "command");
                }

                foreach (CommandInfo commandToGenerate in result)
                {
                    output.AppendLine(GenerateFromCommandInfo(commandToGenerate, activityNamespace, shouldRunLocally));
                }
            }

            return output.ToString().Trim();
        }


        /// <summary>
        /// By default, the activity wrapper uses the remoting command base.
        /// </summary>
        /// <param name="command">The command name to generate.</param>
        /// <param name="activityNamespace">The namespace that will contain the command - for example,
        /// Microsoft.PowerShell.Activities.
        /// </param>
        /// <returns></returns>
        public static string GenerateFromCommandInfo(CommandInfo command, string activityNamespace)
        {
            return GenerateFromCommandInfo(command, activityNamespace, false);
        }

        /// <summary>
        /// By default, the activity wrapper uses the remoting command base.
        /// </summary>
        /// <param name="command">The command name to generate.</param>
        /// <param name="activityNamespace">The namespace that will contain the command - for example,
        /// Microsoft.PowerShell.Activities.
        /// </param>
        /// <param name="shouldRunLocally">True if remoting-related parameters should be suppressed. This
        /// should only be specified for commands that offer no value when run on a remote computer.
        /// </param>
        /// <returns></returns>
        public static string GenerateFromCommandInfo(CommandInfo command, string activityNamespace, bool shouldRunLocally)
        {
            string activityBaseClass = "PSRemotingActivity";
            if (shouldRunLocally || (command.RemotingCapability == RemotingCapability.None))
            {
                activityBaseClass = "PSActivity";
            }

            return GenerateFromCommandInfo(command, activityNamespace, activityBaseClass, null, null, String.Empty);
        }

        /// <summary>
        /// Generate an activity for the given command.
        /// </summary>
        /// <param name="command">The command to use as the basis of the generated activity.</param>
        /// <param name="activityNamespace">The namespace that will contain the command - for example,
        /// Microsoft.PowerShell.Activities.
        /// </param>
        /// <param name="activityBaseClass">The class to use as the base class for this activity</param>
        /// <param name="parametersToExclude">
        /// A list of parameters on the command being wrapped that should not
        /// be copied to the activity.
        /// </param>
        /// <param name="moduleToLoad"> The module that contains the wrapped command</param>
        /// <param name="moduleDefinitionText">Addition text to inset in the class definition</param>
        /// <returns>A string representing the C# source code of the generated activity.</returns>
        public static string GenerateFromCommandInfo(
            CommandInfo command,
            string activityNamespace,
            string activityBaseClass,
            string[] parametersToExclude,
            string moduleToLoad,
            string moduleDefinitionText
        )
        {
            if (command == null)
            {
                throw new ArgumentNullException("command");
            }

            if (String.IsNullOrEmpty(activityNamespace))
            {
                throw new ArgumentNullException("activityNamespace");
            }


            if (String.IsNullOrEmpty(activityBaseClass))
            {
                throw new ArgumentNullException("activityBaseClass");
            }

            StringBuilder parameterBlock = new StringBuilder();
            StringBuilder parameterInitialization = new StringBuilder();
            string commandName = command.Name;
            commandName = commandName.Substring(0, 1).ToUpper(CultureInfo.CurrentCulture) + commandName.Substring(1);
            string activityName = command.Name.Replace("-", "");
            activityName = activityName.Substring(0, 1).ToUpper(CultureInfo.CurrentCulture) + activityName.Substring(1);

            String displayName = commandName;

            // Verify that the activity name doesn't conflict with anything in the inheritance hierarchy
            Type testType = typeof(PSRemotingActivity);
            while (testType != null)
            {
                if (String.Equals(testType.Name, activityName, StringComparison.OrdinalIgnoreCase))
                {
                    string message = String.Format(CultureInfo.InvariantCulture, ActivityResources.ActivityNameConflict, activityName);
                    throw new ArgumentException(message, "command");
                }

                testType = testType.BaseType;
            }

            // The default list of parameters that need to be ignored.
            List<string> ignoredParameters = new List<string>(Cmdlet.CommonParameters.Concat<string>(Cmdlet.OptionalCommonParameters));

            // Add in any additional parameters the caller requested to ignore
            if (parametersToExclude != null && parametersToExclude.Length > 0)
            {
                ignoredParameters.AddRange(parametersToExclude);
            }

            // If this activity supports its own remoting, ignore the ComputerName
            // parameter (we will add special handling for that later)
            if (command.RemotingCapability == RemotingCapability.SupportedByCommand)
            {
                ignoredParameters.Add("ComputerName");
            }

            // Avoid properties in parent classes.
            List<string> parentProperties = new List<string>();
            testType = typeof(PSRemotingActivity);
            while (testType != typeof(PSActivity).BaseType)
            {
                parentProperties.AddRange(
                    from property in testType.GetProperties() select property.Name
                    );

                testType = testType.BaseType;
            }
            
            foreach(KeyValuePair<string,ParameterMetadata> parameter in command.Parameters)
            {
                // Get the name (with capitalized first letter)
                string name = parameter.Key;
                name = name.Substring(0, 1).ToUpper(CultureInfo.CurrentCulture) + name.Substring(1);

                // Ignore the common parameters
                if(ignoredParameters.Contains(name, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Avoid parameters used by the parent activity, currently "Id" and
                // "DisplayName". If the command has a noun, we name it:
                // NounId and NounDisplayName - for example, ProcessId, and
                // ServiceDisplayName.
                string originalName = name;
                if (parentProperties.Contains(name, StringComparer.OrdinalIgnoreCase))
                {
                    if (commandName.Contains('-'))
                    {
                        string[] commandParts = commandName.Split('-');
                        string noun = commandParts[1];
                        noun = noun.Substring(0, 1).ToUpper(CultureInfo.CurrentCulture) + noun.Substring(1);
                        name = noun + name;
                    }
                    else
                    {
                        name = commandName + name;
                    }
                }

                // If the parameter name is the same as the command name, add "Activity"
                // to the command name. Otherwise, we run afoul of the error:
                // "Member names cannot be the same as their enclosing type".
                if (String.Equals(name, activityName, StringComparison.OrdinalIgnoreCase))
                {
                    activityName += "Activity";
                }

                // And the type
                string type = parameter.Value.ParameterType.ToString();

                // Fix generic types
                if(type.Contains('`'))
                {
                    type = System.Text.RegularExpressions.Regex.Replace(type, "`[\\d]+", "");
                    type = System.Text.RegularExpressions.Regex.Replace(type, "\\[", "<");
                    type = System.Text.RegularExpressions.Regex.Replace(type, "\\]", ">");
                }

                // Fix nested classes...
                if (type.Contains('+'))
                {
                    type = System.Text.RegularExpressions.Regex.Replace(type, "\\+", ".");
                }

                // Append the parameter ( InArgument<type> Name { get; set } ... )
                parameterBlock.AppendLine(
                    String.Format(CultureInfo.InvariantCulture,
                    templateParameter,
                    type,
                    name));

                // Append the parameter initializer (... Parameters.Add(...) )
                // This may have to be mapped from name to originalName when the
                // parameter has a conflict with the parent activity.
                parameterInitialization.AppendLine(
                    String.Format(CultureInfo.InvariantCulture,
                    templateParameterSetter,
                    name,
                    originalName));
            }

            // Append the remoting support to parameter initialization
            if (command.RemotingCapability == RemotingCapability.SupportedByCommand)
            {
                parameterBlock.AppendLine(supportsCustomRemoting);
                parameterInitialization.AppendLine(customRemotingMapping);
            }


            // If no module definition string has been included then add the defining module
            // to the list of modules and make use a module-qualified name
            string psDefiningModule = "";
            if (string.IsNullOrEmpty(moduleDefinitionText))
            {
                // Prefer the module to load that was passed in over the module that
                // eventually defined the cmdlet...
                if (!string.IsNullOrEmpty(moduleToLoad))
                {
                    commandName = moduleToLoad + "\\" + commandName;
                }
                else if (!String.IsNullOrEmpty(command.ModuleName))
                {
                    commandName = command.ModuleName + "\\" + commandName;
                }

                if (!String.IsNullOrEmpty(moduleToLoad))
                {
                    psDefiningModule = "        /// <summary>\n/// Script module contents for this activity`n/// </summary>\n" + 
                        @"protected override string PSDefiningModule { get { return """ + moduleToLoad + @"""; } }";
                }
            }

            return String.Format(CultureInfo.InvariantCulture,
                templateCommand,
                activityNamespace,
                commandName,
                activityName,
                parameterBlock.ToString(),
                commandName.Replace("\\", "\\\\"),
                parameterInitialization.ToString(),
                activityBaseClass,
                psDefiningModule,
                displayName,
                moduleDefinitionText);
        }

        /// <summary>
        /// Generates a complete activity source file from a module.
        /// </summary>
        /// <param name="moduleToProcess"></param>
        /// <param name="activityNamespace">The namespace to use for the target classes</param>
        /// <returns>An array of code elements to compile into an assembly</returns>
        static public string[] GenerateFromModuleInfo(PSModuleInfo moduleToProcess, string activityNamespace)
        {
            if (moduleToProcess == null)
                throw new ArgumentNullException("moduleToProcess");

            List<string> codeToCompile = new List<string>();

            // Cmdlets and function need to exist in separate namespaces...
            if (moduleToProcess.ExportedCmdlets != null)
            {
                string namespaceToUse = ! string.IsNullOrEmpty(activityNamespace) ? activityNamespace : moduleToProcess.Name + "_Cmdlet_Activities";
                foreach (CmdletInfo ci in moduleToProcess.ExportedCmdlets.Values)
                {
                    string code = Microsoft.PowerShell.Activities.ActivityGenerator.GenerateFromCommandInfo(ci, namespaceToUse, "PSRemotingActivity", null, null, "");
                    codeToCompile.Add(code);
                }
            }

            Dictionary<string, string> modules = new Dictionary<string, string>();
            PSModuleInfo cimModule = moduleToProcess;

            if (moduleToProcess.ExportedFunctions != null)
            {
                string namespaceToUse = !string.IsNullOrEmpty(activityNamespace) ? activityNamespace : moduleToProcess.Name + "_Function_Activities";
                foreach (FunctionInfo fi in moduleToProcess.ExportedFunctions.Values)
                {
                    string moduleName = null;
                    string moduleDefinition = null;

                    // Save the module defining this function - we may need to extract
                    // embedded types further on
                    if (fi.ScriptBlock.Module != null && !string.IsNullOrEmpty(fi.ScriptBlock.Module.Definition))
                    {
                        moduleName = fi.ScriptBlock.Module.Name;
                        moduleDefinition = fi.ScriptBlock.Module.Definition;
                    }

                    string code;
                    if (fi.ScriptBlock.Module.ModuleType == ModuleType.Cim)
                    {
                        // Special-case CIM activities
                        string embeddedDefinition = "";

                        // Embed the module definition in the activity...
                        if (moduleDefinition != null)
                        {
                            // Remove all of the calls to Export-ModuleMember and getcommand
                            string editedDefinition = System.Text.RegularExpressions.Regex.Replace(moduleDefinition, @"Microsoft.PowerShell.Core\\Export-ModuleMember[^\n]*\n", "");
                            editedDefinition = System.Text.RegularExpressions.Regex.Replace(editedDefinition,
                                @"if \(\$\(Microsoft.PowerShell.Core\\Get-Command Set-StrictMode[^\n]*\n", ""); 

                            embeddedDefinition = "protected override string ModuleDefinition { get { return _moduleDefinition; } }\r\n        const string _moduleDefinition = @\""
                                + editedDefinition.Replace("\"", "\"\"") + "\";";
                        }
                        code = Microsoft.PowerShell.Activities.ActivityGenerator.GenerateFromCommandInfo(
                            fi, namespaceToUse, "PSGeneratedCIMActivity", new string[] { "Computer", "AsJob", "CimSession" }, null, embeddedDefinition);

                        cimModule = fi.ScriptBlock.Module;
                    }
                    else
                    {
                        code = Microsoft.PowerShell.Activities.ActivityGenerator.GenerateFromCommandInfo(
                            fi, namespaceToUse, "PSRemotingActivity", new string[] { "Computer", "AsJob" }, moduleToProcess.Name, "");
                    }
                    codeToCompile.Add(code);

                    if (moduleName != null && !modules.ContainsKey(fi.ScriptBlock.Module.Name))
                    {
                        modules.Add(moduleName, moduleDefinition);
                    }

                    
                }
            }

            string fileName = cimModule.Path;

            // See if there are any embedded types to extract
            if (Path.GetExtension(fileName).Equals(".cdxml", StringComparison.OrdinalIgnoreCase))
            {
                // generate cmdletization proxies
                using (FileStream file = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                {
                    XmlReader xmlReader = XmlReader.Create(file, xmlReaderSettings.Value);

                    PowerShellMetadata cmdletizationMetadata = (PowerShellMetadata)xmlSerializer.Value.Deserialize(xmlReader);

                    if (cmdletizationMetadata != null && cmdletizationMetadata.Enums != null)
                    {
                        foreach (EnumMetadataEnum enumMetadata in cmdletizationMetadata.Enums)
                        {
                            codeToCompile.Add(GetCSharpCode(enumMetadata));
                        }
                    }
                }
            }

            return codeToCompile.ToArray<string>();
        }

        internal static string GetCSharpCode(EnumMetadataEnum enumMetadata)
        {
            var codeCompileUnit = CreateCodeCompileUnit(enumMetadata);

            var stringWriter = new StringWriter(CultureInfo.InvariantCulture);
            CodeDomProvider.CreateProvider("C#").GenerateCodeFromCompileUnit(
                codeCompileUnit,
                stringWriter,
                new CodeGeneratorOptions());
            return stringWriter.ToString();
        }

        private const string namespacePrefix = "Microsoft.PowerShell.Cmdletization.GeneratedTypes";

        private static CodeCompileUnit CreateCodeCompileUnit(EnumMetadataEnum enumMetadata)
        {
            var codeDomProvider = CodeDomProvider.CreateProvider("C#");

            string subnamespaceText = string.Empty;
            string enumNameText;
            int indexOfLastDot = enumMetadata.EnumName.LastIndexOf('.');
            if (indexOfLastDot < 0)
            {
                enumNameText = enumMetadata.EnumName;
            }
            else
            {
                subnamespaceText = "." + enumMetadata.EnumName.Substring(0, indexOfLastDot);
                enumNameText = enumMetadata.EnumName.Substring(
                    indexOfLastDot + 1, enumMetadata.EnumName.Length - indexOfLastDot - 1);
            }

            // defense in depth (in case xsd is allowing some invalid identifiers)
            // + xsd allows reserved keywords (i.e. "namespace" passes the regex test, but is not a valid identifier)
            if (!codeDomProvider.IsValidIdentifier(enumNameText))
            {
                var errorMessage = string.Format(
                    CultureInfo.InvariantCulture,
                    ActivityResources.EnumWriter_InvalidEnumName,
                    enumMetadata.EnumName);
                throw new XmlException(errorMessage);
            }
            var newEnum = new CodeTypeDeclaration(codeDomProvider.CreateValidIdentifier(enumNameText)) { IsEnum = true, Attributes = MemberAttributes.Public };

            if (enumMetadata.BitwiseFlagsSpecified && enumMetadata.BitwiseFlags)
            {
                newEnum.CustomAttributes.Add(
                    new CodeAttributeDeclaration(new CodeTypeReference(typeof(FlagsAttribute))));
            }

            Type underlyingType = null;
            if (enumMetadata.UnderlyingType != null)
            {
                underlyingType = Type.GetType(enumMetadata.UnderlyingType, false, true);

                if (underlyingType != null)
                {
                    newEnum.BaseTypes.Add(underlyingType);
                }
                else
                {
                    underlyingType = typeof(Int32);
                }
            }
            else
            {
                underlyingType = typeof(Int32);
            }

            foreach (var value in enumMetadata.Value)
            {
                // defense in depth (in case xsd is allowing some invalid identifiers)
                // + xsd allows reserved keywords (i.e. "namespace" passes the regex test, but is not a valid identifier)
                if (!codeDomProvider.IsValidIdentifier(value.Name)) // defense in depth (in case xsd is allowing some invalid identifiers)
                {
                    var errorMessage = string.Format(
                        CultureInfo.InvariantCulture,
                        ActivityResources.EnumWriter_InvalidValueName,
                        value.Name);
                    throw new XmlException(errorMessage);
                }

                var nameValuePair = new CodeMemberField(underlyingType, codeDomProvider.CreateValidIdentifier(value.Name));

                object integerValue = LanguagePrimitives.ConvertTo(
                    value.Value, underlyingType, CultureInfo.InvariantCulture);
                nameValuePair.InitExpression = new CodePrimitiveExpression(integerValue);

                newEnum.Members.Add(nameValuePair);
            }

            var topLevelNamespace = new CodeNamespace(namespacePrefix + subnamespaceText);
            topLevelNamespace.Types.Add(newEnum);

            var codeCompileUnit = new CodeCompileUnit();
            codeCompileUnit.Namespaces.Add(topLevelNamespace);
            codeCompileUnit.ReferencedAssemblies.Add("System.dll");

            return codeCompileUnit;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="moduleToProcess"></param>
        /// <param name="activityNamespace"></param>
        /// <param name="outputAssemblyPath"></param>
        /// <param name="referenceAssemblies"></param>
        /// <param name="errors"></param>
        /// <returns></returns>
        public static Assembly GenerateAssemblyFromModuleInfo(
            PSModuleInfo moduleToProcess,
            string activityNamespace,
            string outputAssemblyPath,
            string[] referenceAssemblies,
            out string errors
        )
        {
            string[] src = GenerateFromModuleInfo(moduleToProcess, activityNamespace);

            bool toAssembly = ! string.IsNullOrEmpty(outputAssemblyPath);

            return CompileStrings(src, referenceAssemblies, toAssembly, outputAssemblyPath, out errors);
        }

        private static Assembly CompileStrings(
            string[] src,
            string[] referenceAssemblies,
            bool toAssembly,
            string outputAssemblyPath,
            out string errors
            )
        {
            var cpar = new System.CodeDom.Compiler.CompilerParameters()
            {
                GenerateInMemory = ! toAssembly,
                OutputAssembly = outputAssemblyPath,
            };

            // Add default references...
            cpar.ReferencedAssemblies.Add(typeof(System.Activities.Activity).Assembly.Location);
            cpar.ReferencedAssemblies.Add(typeof(System.CodeDom.Compiler.CodeCompiler).Assembly.Location);
            cpar.ReferencedAssemblies.Add(typeof(PSObject).Assembly.Location);
            cpar.ReferencedAssemblies.Add(typeof(Microsoft.PowerShell.Activities.PSActivity).Assembly.Location);
            cpar.ReferencedAssemblies.Add(ResolveReferencedAssembly("Microsoft.Management.Infrastructure, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35"));
            cpar.ReferencedAssemblies.Add(ResolveReferencedAssembly("Microsoft.PowerShell.Commands.Management, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35"));

            // Add user supplied references...
            if (referenceAssemblies != null)
            {
                foreach (string asm in referenceAssemblies)
                {
                    cpar.ReferencedAssemblies.Add(ResolveReferencedAssembly(asm));
                }
            }

            var compiler = new Microsoft.CSharp.CSharpCodeProvider();
            var cr = compiler.CompileAssemblyFromSource(cpar, src);
            if (cr.Errors == null || cr.Errors.Count == 0)
            {
                errors = string.Empty;
            }
            else
            {
                StringBuilder errorBuilder = new StringBuilder();
                foreach (var err in cr.Errors)
                {
                    errorBuilder.Append(err.ToString());
                    errorBuilder.Append('\n');
                }

                errors = errorBuilder.ToString();
            }

            if (errors.Length > 0)
            {
                return null;
            }

            // If the assembly was written to disk, return null
            // since we don't want to load the assembly we've just created.
            if (toAssembly)
            {
                return null;
            }
            else
            {
                return cr.CompiledAssembly;
            }
        }

        [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Reflection.Assembly.LoadWithPartialName")]
        [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Reflection.Assembly.LoadFrom")]
        private static string ResolveReferencedAssembly(string assembly)
        {
            Assembly asm = null;

            if (assembly == null)
            {
                throw new ArgumentNullException("assembly");
            }

            if (System.IO.Path.IsPathRooted(assembly))
            {
                return assembly;
            }

            if (assembly.Contains(','))
            {
                try
                {
                    asm = Assembly.Load(assembly);
                    return asm.Location;
                }
                catch (Exception)
                {
                    ;
                }
            }


            if (asm == null)
            {
                try
                {
#pragma warning disable 0618
                    asm = Assembly.LoadWithPartialName(assembly);
                    return asm.Location;
                }
                catch (Exception)
                {
                    ;
                }
            }

            if (asm == null)
            {
                throw new InvalidOperationException(assembly);
            }
            return null;
        }
    }
}
