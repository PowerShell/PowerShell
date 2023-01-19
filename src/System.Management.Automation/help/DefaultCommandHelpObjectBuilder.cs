// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Text;

namespace System.Management.Automation.Help
{
    /// <summary>
    /// Positional parameter comparer.
    /// </summary>
    internal class PositionalParameterComparer : IComparer
    {
        /// <summary>
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public int Compare(object x, object y)
        {
            CommandParameterInfo a = x as CommandParameterInfo;
            CommandParameterInfo b = y as CommandParameterInfo;

            Debug.Assert(a != null && b != null);

            return (a.Position - b.Position);
        }
    }

    /// <summary>
    /// The help object builder class attempts to create a full HelpInfo object from
    /// a CmdletInfo object. This is used to generate the default UX when no help content
    /// is present in the box. This class mimics the exact same structure as that of a MAML
    /// node, so that the default UX does not introduce regressions.
    /// </summary>
    internal static class DefaultCommandHelpObjectBuilder
    {
        internal static readonly string TypeNameForDefaultHelp = "ExtendedCmdletHelpInfo";
        /// <summary>
        /// Generates a HelpInfo PSObject from a CmdletInfo object.
        /// </summary>
        /// <param name="input">Command info.</param>
        /// <returns>HelpInfo PSObject.</returns>
        internal static PSObject GetPSObjectFromCmdletInfo(CommandInfo input)
        {
            // Create a copy of commandInfo for GetCommandCommand so that we can generate parameter
            // sets based on Dynamic Parameters (+ optional arguments)
            CommandInfo commandInfo = input.CreateGetCommandCopy(null);

            PSObject obj = new PSObject();

            obj.TypeNames.Clear();
            obj.TypeNames.Add(string.Format(CultureInfo.InvariantCulture, "{0}#{1}#command", DefaultCommandHelpObjectBuilder.TypeNameForDefaultHelp, commandInfo.ModuleName));
            obj.TypeNames.Add(string.Format(CultureInfo.InvariantCulture, "{0}#{1}", DefaultCommandHelpObjectBuilder.TypeNameForDefaultHelp, commandInfo.ModuleName));
            obj.TypeNames.Add(DefaultCommandHelpObjectBuilder.TypeNameForDefaultHelp);
            obj.TypeNames.Add("CmdletHelpInfo");
            obj.TypeNames.Add("HelpInfo");

            if (commandInfo is CmdletInfo cmdletInfo)
            {
                bool common = false;
                if (cmdletInfo.Parameters != null)
                {
                    common = HasCommonParameters(cmdletInfo.Parameters);
                }

                obj.Properties.Add(new PSNoteProperty("CommonParameters", common));
                AddDetailsProperties(obj, cmdletInfo.Name, cmdletInfo.Noun, cmdletInfo.Verb, TypeNameForDefaultHelp);
                AddSyntaxProperties(obj, cmdletInfo.Name, cmdletInfo.ParameterSets, common, TypeNameForDefaultHelp);
                AddParametersProperties(obj, cmdletInfo.Parameters, common, TypeNameForDefaultHelp);
                AddInputTypesProperties(obj, cmdletInfo.Parameters);
                AddRelatedLinksProperties(obj, commandInfo.CommandMetadata.HelpUri);

                try
                {
                    AddOutputTypesProperties(obj, cmdletInfo.OutputType);
                }
                catch (PSInvalidOperationException)
                {
                    AddOutputTypesProperties(obj, new ReadOnlyCollection<PSTypeName>(new List<PSTypeName>()));
                }

                AddAliasesProperties(obj, cmdletInfo.Name, cmdletInfo.Context);

                if (HasHelpInfoUri(cmdletInfo.Module, cmdletInfo.ModuleName))
                {
                    AddRemarksProperties(obj, cmdletInfo.Name, cmdletInfo.CommandMetadata.HelpUri);
                }
                else
                {
                    obj.Properties.Add(new PSNoteProperty("remarks", HelpDisplayStrings.None));
                }

                obj.Properties.Add(new PSNoteProperty("PSSnapIn", cmdletInfo.PSSnapIn));
            }
            else if (commandInfo is FunctionInfo funcInfo)
            {
                bool common = HasCommonParameters(funcInfo.Parameters);

                obj.Properties.Add(new PSNoteProperty("CommonParameters", common));
                AddDetailsProperties(obj, funcInfo.Name, string.Empty, string.Empty, TypeNameForDefaultHelp);
                AddSyntaxProperties(obj, funcInfo.Name, funcInfo.ParameterSets, common, TypeNameForDefaultHelp);
                AddParametersProperties(obj, funcInfo.Parameters, common, TypeNameForDefaultHelp);
                AddInputTypesProperties(obj, funcInfo.Parameters);
                AddRelatedLinksProperties(obj, funcInfo.CommandMetadata.HelpUri);

                try
                {
                    AddOutputTypesProperties(obj, funcInfo.OutputType);
                }
                catch (PSInvalidOperationException)
                {
                    AddOutputTypesProperties(obj, new ReadOnlyCollection<PSTypeName>(new List<PSTypeName>()));
                }

                AddAliasesProperties(obj, funcInfo.Name, funcInfo.Context);

                if (HasHelpInfoUri(funcInfo.Module, funcInfo.ModuleName))
                {
                    AddRemarksProperties(obj, funcInfo.Name, funcInfo.CommandMetadata.HelpUri);
                }
                else
                {
                    obj.Properties.Add(new PSNoteProperty("remarks", HelpDisplayStrings.None));
                }
            }

            obj.Properties.Add(new PSNoteProperty("alertSet", null));
            obj.Properties.Add(new PSNoteProperty("description", null));
            obj.Properties.Add(new PSNoteProperty("examples", null));
            obj.Properties.Add(new PSNoteProperty("Synopsis", commandInfo.Syntax));
            obj.Properties.Add(new PSNoteProperty("ModuleName", commandInfo.ModuleName));
            obj.Properties.Add(new PSNoteProperty("nonTerminatingErrors", string.Empty));
            obj.Properties.Add(new PSNoteProperty("xmlns:command", "http://schemas.microsoft.com/maml/dev/command/2004/10"));
            obj.Properties.Add(new PSNoteProperty("xmlns:dev", "http://schemas.microsoft.com/maml/dev/2004/10"));
            obj.Properties.Add(new PSNoteProperty("xmlns:maml", "http://schemas.microsoft.com/maml/2004/10"));

            return obj;
        }

        /// <summary>
        /// Adds the details properties.
        /// </summary>
        /// <param name="obj">HelpInfo object.</param>
        /// <param name="name">Command name.</param>
        /// <param name="noun">Command noun.</param>
        /// <param name="verb">Command verb.</param>
        /// <param name="typeNameForHelp">Type name for help.</param>
        /// <param name="synopsis">Synopsis.</param>
        internal static void AddDetailsProperties(PSObject obj, string name, string noun, string verb, string typeNameForHelp,
            string synopsis = null)
        {
            PSObject mshObject = new PSObject();

            mshObject.TypeNames.Clear();
            mshObject.TypeNames.Add(string.Format(CultureInfo.InvariantCulture, $"{typeNameForHelp}#details"));

            mshObject.Properties.Add(new PSNoteProperty("name", name));
            mshObject.Properties.Add(new PSNoteProperty("noun", noun));
            mshObject.Properties.Add(new PSNoteProperty("verb", verb));

            // add synopsis
            if (!string.IsNullOrEmpty(synopsis))
            {
                PSObject descriptionObject = new PSObject();
                descriptionObject.TypeNames.Clear();
                descriptionObject.TypeNames.Add("MamlParaTextItem");
                descriptionObject.Properties.Add(new PSNoteProperty("Text", synopsis));
                mshObject.Properties.Add(new PSNoteProperty("Description", descriptionObject));
            }

            obj.Properties.Add(new PSNoteProperty("details", mshObject));
        }

        /// <summary>
        /// Adds the syntax properties.
        /// </summary>
        /// <param name="obj">HelpInfo object.</param>
        /// <param name="cmdletName">Command name.</param>
        /// <param name="parameterSets">Parameter sets.</param>
        /// <param name="common">Common parameters.</param>
        /// <param name="typeNameForHelp">Type name for help.</param>
        internal static void AddSyntaxProperties(PSObject obj, string cmdletName, ReadOnlyCollection<CommandParameterSetInfo> parameterSets, bool common, string typeNameForHelp)
        {
            PSObject mshObject = new PSObject();

            mshObject.TypeNames.Clear();
            mshObject.TypeNames.Add(string.Format(CultureInfo.InvariantCulture, $"{typeNameForHelp}#syntax"));

            AddSyntaxItemProperties(mshObject, cmdletName, parameterSets, common, typeNameForHelp);

            obj.Properties.Add(new PSNoteProperty("Syntax", mshObject));
        }

        /// <summary>
        /// Add the syntax item properties.
        /// </summary>
        /// <param name="obj">HelpInfo object.</param>
        /// <param name="cmdletName">Cmdlet name, you can't get this from parameterSets.</param>
        /// <param name="parameterSets">A collection of parameter sets.</param>
        /// <param name="common">Common parameters.</param>
        /// <param name="typeNameForHelp">Type name for help.</param>
        private static void AddSyntaxItemProperties(PSObject obj, string cmdletName, ReadOnlyCollection<CommandParameterSetInfo> parameterSets, bool common, string typeNameForHelp)
        {
            ArrayList mshObjects = new ArrayList();

            foreach (CommandParameterSetInfo parameterSet in parameterSets)
            {
                PSObject mshObject = new PSObject();

                mshObject.TypeNames.Clear();
                mshObject.TypeNames.Add(string.Format(CultureInfo.InvariantCulture, $"{typeNameForHelp}#syntaxItem"));

                mshObject.Properties.Add(new PSNoteProperty("name", cmdletName));
                mshObject.Properties.Add(new PSNoteProperty("CommonParameters", common));

                Collection<CommandParameterInfo> parameters = new Collection<CommandParameterInfo>();
                // GenerateParameters parameters in display order
                // ie., Positional followed by
                //      Named Mandatory (in alpha numeric) followed by
                //      Named (in alpha numeric)
                parameterSet.GenerateParametersInDisplayOrder(parameters.Add, delegate { });

                AddSyntaxParametersProperties(mshObject, parameters, common, parameterSet.Name);

                mshObjects.Add(mshObject);
            }

            obj.Properties.Add(new PSNoteProperty("syntaxItem", mshObjects.ToArray()));
        }

        /// <summary>
        /// Add the syntax parameters properties (these parameters are used to create the syntax section)
        /// </summary>
        /// <param name="obj">HelpInfo object.</param>
        /// <param name="parameters">
        /// a collection of parameters in display order
        /// ie., Positional followed by
        ///      Named Mandatory (in alpha numeric) followed by
        ///      Named (in alpha numeric)
        /// </param>
        /// <param name="common">Common parameters.</param>
        /// <param name="parameterSetName">Name of the parameter set for which the syntax is generated.</param>
        private static void AddSyntaxParametersProperties(PSObject obj, IEnumerable<CommandParameterInfo> parameters,
            bool common, string parameterSetName)
        {
            ArrayList mshObjects = new ArrayList();

            foreach (CommandParameterInfo parameter in parameters)
            {
                if (common && Cmdlet.CommonParameters.Contains(parameter.Name))
                {
                    continue;
                }

                PSObject mshObject = new PSObject();

                mshObject.TypeNames.Clear();
                mshObject.TypeNames.Add(string.Format(CultureInfo.InvariantCulture, $"{DefaultCommandHelpObjectBuilder.TypeNameForDefaultHelp}#parameter"));

                Collection<Attribute> attributes = new Collection<Attribute>(parameter.Attributes);

                AddParameterProperties(mshObject, parameter.Name, new Collection<string>(parameter.Aliases),
                    parameter.IsDynamic, parameter.ParameterType, attributes, parameterSetName);

                Collection<ValidateSetAttribute> validateSet = GetValidateSetAttribute(attributes);
                List<string> names = new List<string>();

                foreach (ValidateSetAttribute set in validateSet)
                {
                    foreach (string value in set.ValidValues)
                    {
                        names.Add(value);
                    }
                }

                if (names.Count != 0)
                {
                    AddParameterValueGroupProperties(mshObject, names.ToArray());
                }
                else
                {
                    if (parameter.ParameterType.IsEnum && (Enum.GetNames(parameter.ParameterType) != null))
                    {
                        AddParameterValueGroupProperties(mshObject, Enum.GetNames(parameter.ParameterType));
                    }
                    else if (parameter.ParameterType.IsArray)
                    {
                        if (parameter.ParameterType.GetElementType().IsEnum &&
                            Enum.GetNames(parameter.ParameterType.GetElementType()) != null)
                        {
                            AddParameterValueGroupProperties(mshObject, Enum.GetNames(parameter.ParameterType.GetElementType()));
                        }
                    }
                    else if (parameter.ParameterType.IsGenericType)
                    {
                        Type[] types = parameter.ParameterType.GetGenericArguments();

                        if (types.Length != 0)
                        {
                            Type type = types[0];

                            if (type.IsEnum && (Enum.GetNames(type) != null))
                            {
                                AddParameterValueGroupProperties(mshObject, Enum.GetNames(type));
                            }
                            else if (type.IsArray)
                            {
                                if (type.GetElementType().IsEnum &&
                                    Enum.GetNames(type.GetElementType()) != null)
                                {
                                    AddParameterValueGroupProperties(mshObject, Enum.GetNames(type.GetElementType()));
                                }
                            }
                        }
                    }
                }

                mshObjects.Add(mshObject);
            }

            obj.Properties.Add(new PSNoteProperty("parameter", mshObjects.ToArray()));
        }

        /// <summary>
        /// Adds a parameter value group (for enums)
        /// </summary>
        /// <param name="obj">Object.</param>
        /// <param name="values">Parameter group values.</param>
        private static void AddParameterValueGroupProperties(PSObject obj, string[] values)
        {
            PSObject paramValueGroup = new PSObject();

            paramValueGroup.TypeNames.Clear();
            paramValueGroup.TypeNames.Add(string.Format(CultureInfo.InvariantCulture, $"{DefaultCommandHelpObjectBuilder.TypeNameForDefaultHelp}#parameterValueGroup"));

            ArrayList paramValue = new ArrayList(values);

            paramValueGroup.Properties.Add(new PSNoteProperty("parameterValue", paramValue.ToArray()));
            obj.Properties.Add(new PSNoteProperty("parameterValueGroup", paramValueGroup));
        }

        /// <summary>
        /// Add the parameters properties (these parameters are used to create the parameters section)
        /// </summary>
        /// <param name="obj">HelpInfo object.</param>
        /// <param name="parameters">Parameters.</param>
        /// <param name="common">Common parameters.</param>
        /// <param name="typeNameForHelp">Type name for help.</param>
        internal static void AddParametersProperties(PSObject obj, Dictionary<string, ParameterMetadata> parameters, bool common, string typeNameForHelp)
        {
            PSObject paramsObject = new PSObject();

            paramsObject.TypeNames.Clear();
            paramsObject.TypeNames.Add(string.Format(CultureInfo.InvariantCulture, $"{typeNameForHelp}#parameters"));

            ArrayList paramObjects = new ArrayList();

            ArrayList sortedParameters = new ArrayList();

            if (parameters != null)
            {
                foreach (KeyValuePair<string, ParameterMetadata> parameter in parameters)
                {
                    sortedParameters.Add(parameter.Key);
                }
            }

            sortedParameters.Sort(StringComparer.Ordinal);

            foreach (string parameter in sortedParameters)
            {
                if (common && Cmdlet.CommonParameters.Contains(parameter))
                {
                    continue;
                }

                PSObject paramObject = new PSObject();

                paramObject.TypeNames.Clear();
                paramObject.TypeNames.Add(string.Format(CultureInfo.InvariantCulture, $"{DefaultCommandHelpObjectBuilder.TypeNameForDefaultHelp}#parameter"));

                AddParameterProperties(paramObject, parameter, parameters[parameter].Aliases,
                    parameters[parameter].IsDynamic, parameters[parameter].ParameterType, parameters[parameter].Attributes);

                paramObjects.Add(paramObject);
            }

            paramsObject.Properties.Add(new PSNoteProperty("parameter", paramObjects.ToArray()));
            obj.Properties.Add(new PSNoteProperty("parameters", paramsObject));
        }

        /// <summary>
        /// Adds the parameter properties.
        /// </summary>
        /// <param name="obj">HelpInfo object.</param>
        /// <param name="name">Parameter name.</param>
        /// <param name="aliases">Parameter aliases.</param>
        /// <param name="dynamic">Is dynamic parameter?</param>
        /// <param name="type">Parameter type.</param>
        /// <param name="attributes">Parameter attributes.</param>
        /// <param name="parameterSetName">Name of the parameter set for which the syntax is generated.</param>
        private static void AddParameterProperties(PSObject obj, string name, Collection<string> aliases, bool dynamic,
            Type type, Collection<Attribute> attributes, string parameterSetName = null)
        {
            Collection<ParameterAttribute> attribs = GetParameterAttribute(attributes);

            obj.Properties.Add(new PSNoteProperty("name", name));

            if (attribs.Count == 0)
            {
                obj.Properties.Add(new PSNoteProperty("required", string.Empty));
                obj.Properties.Add(new PSNoteProperty("pipelineInput", string.Empty));
                obj.Properties.Add(new PSNoteProperty("isDynamic", string.Empty));
                obj.Properties.Add(new PSNoteProperty("parameterSetName", string.Empty));
                obj.Properties.Add(new PSNoteProperty("description", string.Empty));
                obj.Properties.Add(new PSNoteProperty("position", string.Empty));
                obj.Properties.Add(new PSNoteProperty("aliases", string.Empty));
                obj.Properties.Add(new PSNoteProperty("globbing", string.Empty));
            }
            else
            {
                ParameterAttribute paramAttribute = attribs[0];
                if (!string.IsNullOrEmpty(parameterSetName))
                {
                    foreach (var attrib in attribs)
                    {
                        if (string.Equals(attrib.ParameterSetName, parameterSetName, StringComparison.OrdinalIgnoreCase))
                        {
                            paramAttribute = attrib;
                            break;
                        }
                    }
                }

                obj.Properties.Add(new PSNoteProperty("required", CultureInfo.CurrentCulture.TextInfo.ToLower(paramAttribute.Mandatory.ToString())));
                obj.Properties.Add(new PSNoteProperty("pipelineInput", GetPipelineInputString(paramAttribute)));
                obj.Properties.Add(new PSNoteProperty("isDynamic", CultureInfo.CurrentCulture.TextInfo.ToLower(dynamic.ToString())));
                AddParameterGlobbingProperties(obj, attributes);

                if (paramAttribute.ParameterSetName.Equals(ParameterAttribute.AllParameterSets, StringComparison.OrdinalIgnoreCase))
                {
                    obj.Properties.Add(new PSNoteProperty("parameterSetName", StringUtil.Format(HelpDisplayStrings.AllParameterSetsName)));
                }
                else
                {
                    StringBuilder sb = new StringBuilder();

                    for (int i = 0; i < attribs.Count; i++)
                    {
                        sb.Append(attribs[i].ParameterSetName);

                        if (i != (attribs.Count - 1))
                        {
                            sb.Append(", ");
                        }
                    }

                    obj.Properties.Add(new PSNoteProperty("parameterSetName", sb.ToString()));
                }

                if (paramAttribute.HelpMessage != null)
                {
                    StringBuilder sb = new StringBuilder();

                    sb.AppendLine(paramAttribute.HelpMessage);

                    obj.Properties.Add(new PSNoteProperty("description", sb.ToString()));
                }

                // We do not show switch parameters in the syntax section
                // (i.e. [-Syntax] not [-Syntax <SwitchParameter>]
                if (type != typeof(SwitchParameter))
                {
                    AddParameterValueProperties(obj, type, attributes);
                }

                AddParameterTypeProperties(obj, type, attributes);

                if (paramAttribute.Position == int.MinValue)
                {
                    obj.Properties.Add(new PSNoteProperty("position",
                        StringUtil.Format(HelpDisplayStrings.NamedParameter)));
                }
                else
                {
                    obj.Properties.Add(new PSNoteProperty("position",
                        paramAttribute.Position.ToString(CultureInfo.InvariantCulture)));
                }

                if (aliases.Count == 0)
                {
                    obj.Properties.Add(new PSNoteProperty("aliases", StringUtil.Format(
                        HelpDisplayStrings.None)));
                }
                else
                {
                    StringBuilder sb = new StringBuilder();

                    for (int i = 0; i < aliases.Count; i++)
                    {
                        sb.Append(aliases[i]);

                        if (i != (aliases.Count - 1))
                        {
                            sb.Append(", ");
                        }
                    }

                    obj.Properties.Add(new PSNoteProperty("aliases", sb.ToString()));
                }
            }
        }

        /// <summary>
        /// Adds the globbing properties.
        /// </summary>
        /// <param name="obj">HelpInfo object.</param>
        /// <param name="attributes">The attributes of the parameter (needed to look for PSTypeName).</param>
        private static void AddParameterGlobbingProperties(PSObject obj, IEnumerable<Attribute> attributes)
        {
            bool globbing = false;

            foreach (var attrib in attributes)
            {
                if (attrib is SupportsWildcardsAttribute)
                {
                    globbing = true;
                    break;
                }
            }

            obj.Properties.Add(new PSNoteProperty("globbing", CultureInfo.CurrentCulture.TextInfo.ToLower(globbing.ToString())));
        }

        /// <summary>
        /// Adds the parameterType properties.
        /// </summary>
        /// <param name="obj">HelpInfo object.</param>
        /// <param name="parameterType">The type of a parameter.</param>
        /// <param name="attributes">The attributes of the parameter (needed to look for PSTypeName).</param>
        private static void AddParameterTypeProperties(PSObject obj, Type parameterType, IEnumerable<Attribute> attributes)
        {
            PSObject mshObject = new PSObject();

            mshObject.TypeNames.Clear();
            mshObject.TypeNames.Add(string.Format(CultureInfo.InvariantCulture, $"{DefaultCommandHelpObjectBuilder.TypeNameForDefaultHelp}#type"));

            var parameterTypeString = CommandParameterSetInfo.GetParameterTypeString(parameterType, attributes);
            mshObject.Properties.Add(new PSNoteProperty("name", parameterTypeString));

            obj.Properties.Add(new PSNoteProperty("type", mshObject));
        }

        /// <summary>
        /// Adds the parameterValue properties.
        /// </summary>
        /// <param name="obj">HelpInfo object.</param>
        /// <param name="parameterType">The type of a parameter.</param>
        /// <param name="attributes">The attributes of the parameter (needed to look for PSTypeName).</param>
        private static void AddParameterValueProperties(PSObject obj, Type parameterType, IEnumerable<Attribute> attributes)
        {
            PSObject mshObject;

            if (parameterType != null)
            {
                Type type = Nullable.GetUnderlyingType(parameterType) ?? parameterType;
                var parameterTypeString = CommandParameterSetInfo.GetParameterTypeString(parameterType, attributes);
                mshObject = new PSObject(parameterTypeString);
                mshObject.Properties.Add(new PSNoteProperty("variableLength", parameterType.IsArray));
            }
            else
            {
                mshObject = new PSObject("System.Object");
                mshObject.Properties.Add(new PSNoteProperty("variableLength",
                    StringUtil.Format(HelpDisplayStrings.FalseShort)));
            }

            mshObject.Properties.Add(new PSNoteProperty("required", "true"));

            obj.Properties.Add(new PSNoteProperty("parameterValue", mshObject));
        }

        /// <summary>
        /// Adds the InputTypes properties.
        /// </summary>
        /// <param name="obj">HelpInfo object.</param>
        /// <param name="parameters">Command parameters.</param>
        internal static void AddInputTypesProperties(PSObject obj, Dictionary<string, ParameterMetadata> parameters)
        {
            Collection<string> inputs = new Collection<string>();

            if (parameters != null)
            {
                foreach (KeyValuePair<string, ParameterMetadata> parameter in parameters)
                {
                    Collection<ParameterAttribute> attribs = GetParameterAttribute(parameter.Value.Attributes);

                    foreach (ParameterAttribute attrib in attribs)
                    {
                        if (attrib.ValueFromPipeline ||
                            attrib.ValueFromPipelineByPropertyName ||
                            attrib.ValueFromRemainingArguments)
                        {
                            if (!inputs.Contains(parameter.Value.ParameterType.FullName))
                            {
                                inputs.Add(parameter.Value.ParameterType.FullName);
                            }
                        }
                    }
                }
            }

            if (inputs.Count == 0)
            {
                inputs.Add(StringUtil.Format(HelpDisplayStrings.None));
            }

            StringBuilder sb = new StringBuilder();

            foreach (string input in inputs)
            {
                sb.AppendLine(input);
            }

            PSObject inputTypesObj = new PSObject();

            inputTypesObj.TypeNames.Clear();
            inputTypesObj.TypeNames.Add(string.Format(CultureInfo.InvariantCulture, $"{DefaultCommandHelpObjectBuilder.TypeNameForDefaultHelp}#inputTypes"));

            PSObject inputTypeObj = new PSObject();

            inputTypeObj.TypeNames.Clear();
            inputTypeObj.TypeNames.Add(string.Format(CultureInfo.InvariantCulture, $"{DefaultCommandHelpObjectBuilder.TypeNameForDefaultHelp}#inputType"));

            PSObject typeObj = new PSObject();

            typeObj.TypeNames.Clear();
            typeObj.TypeNames.Add(string.Format(CultureInfo.InvariantCulture, $"{DefaultCommandHelpObjectBuilder.TypeNameForDefaultHelp}#type"));

            typeObj.Properties.Add(new PSNoteProperty("name", sb.ToString()));
            inputTypeObj.Properties.Add(new PSNoteProperty("type", typeObj));
            inputTypesObj.Properties.Add(new PSNoteProperty("inputType", inputTypeObj));
            obj.Properties.Add(new PSNoteProperty("inputTypes", inputTypesObj));
        }

        /// <summary>
        /// Adds the OutputTypes properties.
        /// </summary>
        /// <param name="obj">HelpInfo object.</param>
        /// <param name="outputTypes">Output types.</param>
        private static void AddOutputTypesProperties(PSObject obj, ReadOnlyCollection<PSTypeName> outputTypes)
        {
            PSObject returnValuesObj = new PSObject();

            returnValuesObj.TypeNames.Clear();
            returnValuesObj.TypeNames.Add(string.Format(CultureInfo.InvariantCulture, $"{DefaultCommandHelpObjectBuilder.TypeNameForDefaultHelp}#returnValues"));

            PSObject returnValueObj = new PSObject();

            returnValueObj.TypeNames.Clear();
            returnValueObj.TypeNames.Add(string.Format(CultureInfo.InvariantCulture, $"{DefaultCommandHelpObjectBuilder.TypeNameForDefaultHelp}#returnValue"));

            PSObject typeObj = new PSObject();

            typeObj.TypeNames.Clear();
            typeObj.TypeNames.Add(string.Format(CultureInfo.InvariantCulture, $"{DefaultCommandHelpObjectBuilder.TypeNameForDefaultHelp}#type"));

            if (outputTypes.Count == 0)
            {
                typeObj.Properties.Add(new PSNoteProperty("name", "System.Object"));
            }
            else
            {
                StringBuilder sb = new StringBuilder();

                foreach (PSTypeName outputType in outputTypes)
                {
                    sb.AppendLine(outputType.Name);
                }

                typeObj.Properties.Add(new PSNoteProperty("name", sb.ToString()));
            }

            returnValueObj.Properties.Add(new PSNoteProperty("type", typeObj));
            returnValuesObj.Properties.Add(new PSNoteProperty("returnValue", returnValueObj));
            obj.Properties.Add(new PSNoteProperty("returnValues", returnValuesObj));
        }

        /// <summary>
        /// Adds the aliases properties.
        /// </summary>
        /// <param name="obj">HelpInfo object.</param>
        /// <param name="name">Command name.</param>
        /// <param name="context">Execution context.</param>
        private static void AddAliasesProperties(PSObject obj, string name, ExecutionContext context)
        {
            StringBuilder sb = new StringBuilder();

            bool found = false;

            if (context != null)
            {
                foreach (string alias in context.SessionState.Internal.GetAliasesByCommandName(name))
                {
                    found = true;
                    sb.AppendLine(alias);
                }
            }

            if (!found)
            {
                sb.AppendLine(StringUtil.Format(HelpDisplayStrings.None));
            }

            obj.Properties.Add(new PSNoteProperty("aliases", sb.ToString()));
        }

        /// <summary>
        /// Adds the remarks properties.
        /// </summary>
        /// <param name="obj">HelpInfo object.</param>
        /// <param name="cmdletName"></param>
        /// <param name="helpUri"></param>
        private static void AddRemarksProperties(PSObject obj, string cmdletName, string helpUri)
        {
            if (string.IsNullOrEmpty(helpUri))
            {
                obj.Properties.Add(new PSNoteProperty("remarks", StringUtil.Format(HelpDisplayStrings.GetLatestHelpContentWithoutHelpUri, cmdletName)));
            }
            else
            {
                obj.Properties.Add(new PSNoteProperty("remarks", StringUtil.Format(HelpDisplayStrings.GetLatestHelpContent, cmdletName, helpUri)));
            }
        }

        /// <summary>
        /// Adds the related links properties.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="relatedLink"></param>
        internal static void AddRelatedLinksProperties(PSObject obj, string relatedLink)
        {
            if (!string.IsNullOrEmpty(relatedLink))
            {
                PSObject navigationLinkObj = new PSObject();

                navigationLinkObj.TypeNames.Clear();
                navigationLinkObj.TypeNames.Add(string.Format(CultureInfo.InvariantCulture, "{0}#navigationLinks", DefaultCommandHelpObjectBuilder.TypeNameForDefaultHelp));

                navigationLinkObj.Properties.Add(new PSNoteProperty("uri", relatedLink));

                List<PSObject> navigationLinkValues = new List<PSObject> { navigationLinkObj };

                // check if obj already has relatedLinks property
                PSNoteProperty relatedLinksPO = obj.Properties["relatedLinks"] as PSNoteProperty;
                if ((relatedLinksPO != null) && (relatedLinksPO.Value != null))
                {
                    PSObject relatedLinksValue = PSObject.AsPSObject(relatedLinksPO.Value);
                    PSNoteProperty navigationLinkPO = relatedLinksValue.Properties["navigationLink"] as PSNoteProperty;
                    if ((navigationLinkPO != null) && (navigationLinkPO.Value != null))
                    {
                        PSObject navigationLinkValue = navigationLinkPO.Value as PSObject;
                        if (navigationLinkValue != null)
                        {
                            navigationLinkValues.Add(navigationLinkValue);
                        }
                        else
                        {
                            PSObject[] navigationLinkValueArray = navigationLinkPO.Value as PSObject[];
                            if (navigationLinkValueArray != null)
                            {
                                foreach (var psObject in navigationLinkValueArray)
                                {
                                    navigationLinkValues.Add(psObject);
                                }
                            }
                        }
                    }
                }

                PSObject relatedLinksObj = new PSObject();

                relatedLinksObj.TypeNames.Clear();
                relatedLinksObj.TypeNames.Add(string.Format(CultureInfo.InvariantCulture, "{0}#relatedLinks", DefaultCommandHelpObjectBuilder.TypeNameForDefaultHelp));
                relatedLinksObj.Properties.Add(new PSNoteProperty("navigationLink", navigationLinkValues.ToArray()));

                obj.Properties.Add(new PSNoteProperty("relatedLinks", relatedLinksObj));
            }
        }

        /// <summary>
        /// Gets the parameter attribute from parameter metadata.
        /// </summary>
        /// <param name="attributes">Parameter attributes.</param>
        /// <returns>Collection of parameter attributes.</returns>
        private static Collection<ParameterAttribute> GetParameterAttribute(Collection<Attribute> attributes)
        {
            Collection<ParameterAttribute> paramAttributes = new Collection<ParameterAttribute>();

            foreach (Attribute attribute in attributes)
            {
                ParameterAttribute paramAttribute = (object)attribute as ParameterAttribute;

                if (paramAttribute != null)
                {
                    paramAttributes.Add(paramAttribute);
                }
            }

            return paramAttributes;
        }

        /// <summary>
        /// Gets the validate set attribute from parameter metadata.
        /// </summary>
        /// <param name="attributes">Parameter attributes.</param>
        /// <returns>Collection of parameter attributes.</returns>
        private static Collection<ValidateSetAttribute> GetValidateSetAttribute(Collection<Attribute> attributes)
        {
            Collection<ValidateSetAttribute> validateSetAttributes = new Collection<ValidateSetAttribute>();

            foreach (Attribute attribute in attributes)
            {
                ValidateSetAttribute validateSetAttribute = (object)attribute as ValidateSetAttribute;

                if (validateSetAttribute != null)
                {
                    validateSetAttributes.Add(validateSetAttribute);
                }
            }

            return validateSetAttributes;
        }

        /// <summary>
        /// Gets the pipeline input type.
        /// </summary>
        /// <param name="paramAttrib">Parameter attribute.</param>
        /// <returns>Pipeline input type.</returns>
        private static string GetPipelineInputString(ParameterAttribute paramAttrib)
        {
            Debug.Assert(paramAttrib != null);

            ArrayList values = new ArrayList();

            if (paramAttrib.ValueFromPipeline)
            {
                values.Add(StringUtil.Format(HelpDisplayStrings.PipelineByValue));
            }

            if (paramAttrib.ValueFromPipelineByPropertyName)
            {
                values.Add(StringUtil.Format(HelpDisplayStrings.PipelineByPropertyName));
            }

            if (paramAttrib.ValueFromRemainingArguments)
            {
                values.Add(StringUtil.Format(HelpDisplayStrings.PipelineFromRemainingArguments));
            }

            if (values.Count == 0)
            {
                return StringUtil.Format(HelpDisplayStrings.FalseShort);
            }

            StringBuilder sb = new StringBuilder();

            sb.Append(StringUtil.Format(HelpDisplayStrings.TrueShort));
            sb.Append(" (");

            for (int i = 0; i < values.Count; i++)
            {
                sb.Append((string)values[i]);

                if (i != (values.Count - 1))
                {
                    sb.Append(", ");
                }
            }

            sb.Append(')');

            return sb.ToString();
        }

        /// <summary>
        /// Checks if a set of parameters contains any of the common parameters.
        /// </summary>
        /// <param name="parameters">Parameters to check.</param>
        /// <returns>True if it contains common parameters, false otherwise.</returns>
        internal static bool HasCommonParameters(Dictionary<string, ParameterMetadata> parameters)
        {
            Collection<string> commonParams = new Collection<string>();

            foreach (KeyValuePair<string, ParameterMetadata> parameter in parameters)
            {
                if (Cmdlet.CommonParameters.Contains(parameter.Value.Name))
                {
                    commonParams.Add(parameter.Value.Name);
                }
            }

            return (commonParams.Count == Cmdlet.CommonParameters.Count);
        }

        /// <summary>
        /// Checks if the module contains HelpInfoUri.
        /// </summary>
        /// <param name="module"></param>
        /// <param name="moduleName"></param>
        /// <returns></returns>
        private static bool HasHelpInfoUri(PSModuleInfo module, string moduleName)
        {
            // The core module is really a SnapIn, so module will be null
            if (!string.IsNullOrEmpty(moduleName) && moduleName.Equals(InitialSessionState.CoreModule, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (module == null)
            {
                return false;
            }

            return !string.IsNullOrEmpty(module.HelpInfoUri);
        }
    }
}
