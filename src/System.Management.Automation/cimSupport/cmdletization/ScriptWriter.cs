// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Schema;

using Microsoft.PowerShell.Cmdletization.Xml;
using System.Management.Automation;
using System.Management.Automation.Language;
using Microsoft.PowerShell.Commands;
using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell.Cmdletization
{
    internal sealed class ScriptWriter
    {
        #region Static code reused for reading cmdletization xml

        private static readonly XmlReaderSettings s_xmlReaderSettings;

        static ScriptWriter()
        {
            //
            // XmlReaderSettings
            //
            ScriptWriter.s_xmlReaderSettings = new XmlReaderSettings();
            // general settings
            ScriptWriter.s_xmlReaderSettings.CheckCharacters = true;
            ScriptWriter.s_xmlReaderSettings.CloseInput = false;
            ScriptWriter.s_xmlReaderSettings.ConformanceLevel = ConformanceLevel.Document;
            ScriptWriter.s_xmlReaderSettings.IgnoreComments = true;
            ScriptWriter.s_xmlReaderSettings.IgnoreProcessingInstructions = true;
            ScriptWriter.s_xmlReaderSettings.IgnoreWhitespace = false;
            ScriptWriter.s_xmlReaderSettings.MaxCharactersFromEntities = 16384; // generous guess for the upper bound
            ScriptWriter.s_xmlReaderSettings.MaxCharactersInDocument = 128 * 1024 * 1024; // generous guess for the upper bound

#if CORECLR // The XML Schema file 'cmdlets-over-objects.xsd' is missing in Github, and it's likely the resource string
            // 'CmdletizationCoreResources.Xml_cmdletsOverObjectsXsd' needs to be reworked to work in .NET Core.
            ScriptWriter.s_xmlReaderSettings.DtdProcessing = DtdProcessing.Ignore;
#else
            ScriptWriter.s_xmlReaderSettings.DtdProcessing = DtdProcessing.Parse; // Allowing DTD parsing with limits of MaxCharactersFromEntities/MaxCharactersInDocument
            ScriptWriter.s_xmlReaderSettings.XmlResolver = null; // do not fetch external documents
            // xsd schema related settings
            ScriptWriter.s_xmlReaderSettings.ValidationFlags = XmlSchemaValidationFlags.ProcessIdentityConstraints |
                                                XmlSchemaValidationFlags.ReportValidationWarnings;
            ScriptWriter.s_xmlReaderSettings.ValidationType = ValidationType.Schema;
            string cmdletizationXsd = CmdletizationCoreResources.Xml_cmdletsOverObjectsXsd;
            XmlReader cmdletizationSchemaReader = XmlReader.Create(new StringReader(cmdletizationXsd), ScriptWriter.s_xmlReaderSettings);
            ScriptWriter.s_xmlReaderSettings.Schemas = new XmlSchemaSet();
            ScriptWriter.s_xmlReaderSettings.Schemas.Add(null, cmdletizationSchemaReader);
            ScriptWriter.s_xmlReaderSettings.Schemas.XmlResolver = null; // do not fetch external documents
#endif
        }

        #endregion Static code reused for reading cmdletization xml

        #region Constructors / setup code

        [Flags]
        internal enum GenerationOptions
        {
            TypesPs1Xml = 1,
            FormatPs1Xml = 2,
            HelpXml = 4,
        }

        private readonly PowerShellMetadata _cmdletizationMetadata;
        private readonly string _moduleName;
        private readonly Type _objectModelWrapper;
        private readonly Type _objectInstanceType;
        private readonly InvocationInfo _invocationInfo;
        private readonly GenerationOptions _generationOptions;

        internal ScriptWriter(
            TextReader cmdletizationXmlReader,
            string moduleName,
            string defaultObjectModelWrapper,
            InvocationInfo invocationInfo,
            GenerationOptions generationOptions)
        {
            Dbg.Assert(cmdletizationXmlReader != null, "Caller should verify that cmdletizationXmlReader != null");
            Dbg.Assert(!string.IsNullOrEmpty(moduleName), "Caller should verify that moduleName != null");
            Dbg.Assert(invocationInfo != null, "Caller should verify that invocationInfo != null");
            Dbg.Assert(!string.IsNullOrEmpty(defaultObjectModelWrapper), "Caller should verify that defaultObjectModelWrapper != null");

            XmlReader xmlReader = XmlReader.Create(cmdletizationXmlReader, ScriptWriter.s_xmlReaderSettings);
            try
            {
                var xmlSerializer = new PowerShellMetadataSerializer();
                _cmdletizationMetadata = (PowerShellMetadata)xmlSerializer.Deserialize(xmlReader);
            }
            catch (InvalidOperationException e)
            {
                XmlSchemaException schemaException = e.InnerException as XmlSchemaException;
                if (schemaException != null)
                {
                    throw new XmlException(schemaException.Message, schemaException, schemaException.LineNumber, schemaException.LinePosition);
                }

                XmlException xmlException = e.InnerException as XmlException;
                if (xmlException != null)
                {
                    throw xmlException;
                }

                if (e.InnerException != null)
                {
                    string message = string.Format(
                        CultureInfo.CurrentCulture,
                        CmdletizationCoreResources.ScriptWriter_ConcatenationOfDeserializationExceptions,
                        e.Message,
                        e.InnerException.Message);

                    throw new InvalidOperationException(message, e.InnerException);
                }

                throw;
            }

            string objectModelWrapperName = _cmdletizationMetadata.Class.CmdletAdapter ?? defaultObjectModelWrapper;
            _objectModelWrapper = (Type)LanguagePrimitives.ConvertTo(objectModelWrapperName, typeof(Type), CultureInfo.InvariantCulture);
            if (_objectModelWrapper.IsGenericType)
            {
                string message = string.Format(
                    CultureInfo.CurrentCulture,
                    CmdletizationCoreResources.ScriptWriter_ObjectModelWrapperIsStillGeneric,
                    objectModelWrapperName);
                throw new XmlException(message);
            }

            Type baseType = _objectModelWrapper;
            while ((!baseType.IsGenericType) || baseType.GetGenericTypeDefinition() != typeof(CmdletAdapter<>))
            {
                baseType = baseType.BaseType;
                if (baseType == typeof(object))
                {
                    string message = string.Format(
                        CultureInfo.CurrentCulture,
                        CmdletizationCoreResources.ScriptWriter_ObjectModelWrapperNotDerivedFromObjectModelWrapper,
                        objectModelWrapperName,
                        typeof(CmdletAdapter<>).FullName);
                    throw new XmlException(message);
                }
            }

            _objectInstanceType = baseType.GetGenericArguments()[0];

            _moduleName = moduleName;
            _invocationInfo = invocationInfo;
            _generationOptions = generationOptions;
        }

        #endregion Constructors / setup code

        #region psm1

        private const string HeaderTemplate = @"
#requires -version 3.0

try {{ Microsoft.PowerShell.Core\Set-StrictMode -Off }} catch {{ }}

$script:MyModule = $MyInvocation.MyCommand.ScriptBlock.Module

$script:ClassName = '{0}'
$script:ClassVersion = '{1}'
$script:ModuleVersion = '{2}'
$script:ObjectModelWrapper = [{3}]

$script:PrivateData = [System.Collections.Generic.Dictionary[string,string]]::new()

Microsoft.PowerShell.Core\Export-ModuleMember -Function @()
        ";

        private void WriteModulePreamble(TextWriter output)
        {
            output.WriteLine(
                ScriptWriter.HeaderTemplate,
                CodeGeneration.EscapeSingleQuotedStringContent(_cmdletizationMetadata.Class.ClassName),
                CodeGeneration.EscapeSingleQuotedStringContent(_cmdletizationMetadata.Class.ClassVersion ?? string.Empty),
                CodeGeneration.EscapeSingleQuotedStringContent(new Version(_cmdletizationMetadata.Class.Version).ToString()),
                CodeGeneration.EscapeSingleQuotedStringContent(_objectModelWrapper.FullName));

            if (_cmdletizationMetadata.Class.CmdletAdapterPrivateData != null)
            {
                foreach (ClassMetadataData data in _cmdletizationMetadata.Class.CmdletAdapterPrivateData)
                {
                    output.WriteLine(
                        "$script:PrivateData.Add('{0}', '{1}')",
                        CodeGeneration.EscapeSingleQuotedStringContent(data.Name),
                        CodeGeneration.EscapeSingleQuotedStringContent(data.Value));
                }
            }
        }

        private void WriteBindCommonParametersFunction(TextWriter output)
        {
            output.WriteLine(@"
function __cmdletization_BindCommonParameters
{
    param(
        $__cmdletization_objectModelWrapper,
        $myPSBoundParameters
    )
                ");

            foreach (ParameterMetadata commonParameter in this.GetCommonParameters().Values)
            {
                output.WriteLine(@"
        if ($myPSBoundParameters.ContainsKey('{0}')) {{
            $__cmdletization_objectModelWrapper.PSObject.Properties['{0}'].Value = $myPSBoundParameters['{0}']
        }}
                    ",
                     CodeGeneration.EscapeSingleQuotedStringContent(commonParameter.Name));
            }

            output.WriteLine(@"
}
                ");
        }

        private string GetCmdletName(CommonCmdletMetadata cmdletMetadata)
        {
            string noun = cmdletMetadata.Noun ?? _cmdletizationMetadata.Class.DefaultNoun;
            string verb = cmdletMetadata.Verb;
            return verb + "-" + noun;
        }

        private static string GetCmdletAttributes(CommonCmdletMetadata cmdletMetadata)
        {
            // Generate the script for the Alias and Obsolete Attribute if any is declared in CDXML
            StringBuilder attributes = new(150);
            if (cmdletMetadata.Aliases != null)
            {
                attributes.Append("[Alias('" + string.Join("','", cmdletMetadata.Aliases.Select(static alias => CodeGeneration.EscapeSingleQuotedStringContent(alias))) + "')]");
            }

            if (cmdletMetadata.Obsolete != null)
            {
                string obsoleteMsg = (cmdletMetadata.Obsolete.Message != null)
                    ? ("'" + CodeGeneration.EscapeSingleQuotedStringContent(cmdletMetadata.Obsolete.Message) + "'")
                    : string.Empty;
                string newline = (attributes.Length > 0) ? Environment.NewLine : string.Empty;
                attributes.Append(CultureInfo.InvariantCulture, $"{newline}[Obsolete({obsoleteMsg})]");
            }

            return attributes.ToString();
        }

        private Dictionary<string, ParameterMetadata> GetCommonParameters()
        {
            Dictionary<string, ParameterMetadata> commonParameters = new(StringComparer.OrdinalIgnoreCase);

            InternalParameterMetadata internalParameterMetadata = new(_objectModelWrapper, false);
            foreach (CompiledCommandParameter compiledCommandParameter in internalParameterMetadata.BindableParameters.Values)
            {
                ParameterMetadata parameterMetadata = new(compiledCommandParameter);
                foreach (ParameterSetMetadata psetMetadata in parameterMetadata.ParameterSets.Values)
                {
                    if (psetMetadata.ValueFromPipeline)
                    {
                        string message = string.Format(
                            CultureInfo.InvariantCulture,
                            CmdletizationCoreResources.ScriptWriter_ObjectModelWrapperUsesIgnoredParameterMetadata,
                            _objectModelWrapper.FullName,
                            parameterMetadata.Name,
                            "ValueFromPipeline");
                        throw new XmlException(message);
                    }

                    if (psetMetadata.ValueFromPipelineByPropertyName)
                    {
                        string message = string.Format(
                            CultureInfo.InvariantCulture,
                            CmdletizationCoreResources.ScriptWriter_ObjectModelWrapperUsesIgnoredParameterMetadata,
                            _objectModelWrapper.FullName,
                            parameterMetadata.Name,
                            "ValueFromPipelineByPropertyName");
                        throw new XmlException(message);
                    }

                    if (psetMetadata.ValueFromRemainingArguments)
                    {
                        string message = string.Format(
                            CultureInfo.InvariantCulture,
                            CmdletizationCoreResources.ScriptWriter_ObjectModelWrapperUsesIgnoredParameterMetadata,
                            _objectModelWrapper.FullName,
                            parameterMetadata.Name,
                            "ValueFromRemainingArguments");
                        throw new XmlException(message);
                    }

                    psetMetadata.ValueFromPipeline = false;
                    psetMetadata.ValueFromPipelineByPropertyName = false;
                    psetMetadata.ValueFromRemainingArguments = false;
                }

                commonParameters.Add(parameterMetadata.Name, parameterMetadata);
            }

            List<string> commonParameterSets = GetCommonParameterSets(commonParameters);
            if (commonParameterSets.Count > 1)
            {
                string message = string.Format(
                    CultureInfo.InvariantCulture,
                    CmdletizationCoreResources.ScriptWriter_ObjectModelWrapperDefinesMultipleParameterSets,
                    _objectModelWrapper.FullName);
                throw new XmlException(message);
            }

            foreach (ParameterMetadata parameter in commonParameters.Values)
            {
                if ((parameter.ParameterSets.Count == 1) && (parameter.ParameterSets.ContainsKey(ParameterAttribute.AllParameterSets)))
                {
                    ParameterSetMetadata oldParameterSetMetadata = parameter.ParameterSets[ParameterAttribute.AllParameterSets];

                    parameter.ParameterSets.Clear();
                    foreach (string parameterSetName in commonParameterSets)
                    {
                        parameter.ParameterSets.Add(parameterSetName, oldParameterSetMetadata);
                    }
                }
            }

            return commonParameters;
        }

        private static List<string> GetCommonParameterSets(Dictionary<string, ParameterMetadata> commonParameters)
        {
            Dictionary<string, object> parameterSetNames = new(StringComparer.OrdinalIgnoreCase);
            foreach (ParameterMetadata parameter in commonParameters.Values)
            {
                foreach (string parameterSetName in parameter.ParameterSets.Keys)
                {
                    if (!parameterSetName.Equals(ParameterAttribute.AllParameterSets))
                    {
                        parameterSetNames[parameterSetName] = null;
                    }
                }
            }

            if (parameterSetNames.Count == 0)
            {
                parameterSetNames.Add(ParameterAttribute.AllParameterSets, null);
            }

            List<string> result = new(parameterSetNames.Keys);
            result.Sort(StringComparer.Ordinal); // to have a deterministic order of parameter sets (also means that Ordinal instead of OrdinalIgnoreCase is ok)
            return result;
        }

        private string GetMethodParameterSet(StaticMethodMetadata staticMethod)
        {
            Dbg.Assert(staticMethod != null, "Caller should verify that staticMethod != null");
            return staticMethod.CmdletParameterSet ?? GetMethodParameterSet((CommonMethodMetadata)staticMethod);
        }

        private List<string> GetMethodParameterSets(StaticCmdletMetadata staticCmdlet)
        {
            Dictionary<string, object> parameterSetNames = new(StringComparer.OrdinalIgnoreCase);

            foreach (StaticMethodMetadata method in staticCmdlet.Method)
            {
                string parameterSetName = GetMethodParameterSet(method);
                if (parameterSetNames.ContainsKey(parameterSetName))
                {
                    string message = string.Format(
                        CultureInfo.InvariantCulture,
                        CmdletizationCoreResources.ScriptWriter_DuplicateParameterSetInStaticCmdlet,
                        this.GetCmdletName(staticCmdlet.CmdletMetadata),
                        parameterSetName);
                    throw new XmlException(message);
                }

                parameterSetNames.Add(parameterSetName, null);
            }

            return new List<string>(parameterSetNames.Keys);
        }

        private readonly Dictionary<CommonMethodMetadata, int> _staticMethodMetadataToUniqueId = new();

        private string GetMethodParameterSet(CommonMethodMetadata methodMetadata)
        {
            Dbg.Assert(methodMetadata != null, "Caller should verify that instanceMethod != null");

            int uniqueId;
            if (!_staticMethodMetadataToUniqueId.TryGetValue(methodMetadata, out uniqueId))
            {
                uniqueId = _staticMethodMetadataToUniqueId.Count;
                _staticMethodMetadataToUniqueId.Add(methodMetadata, uniqueId);
            }

            return methodMetadata.MethodName + uniqueId;
        }

        private List<string> GetMethodParameterSets(InstanceCmdletMetadata instanceCmdlet)
        {
            Dictionary<string, object> parameterSetNames = new(StringComparer.OrdinalIgnoreCase);

            InstanceMethodMetadata method = instanceCmdlet.Method;
            string parameterSetName = GetMethodParameterSet(method);
            parameterSetNames.Add(parameterSetName, null);

            return new List<string>(parameterSetNames.Keys);
        }

        private GetCmdletParameters GetGetCmdletParameters(InstanceCmdletMetadata instanceCmdlet)
        {
            if (instanceCmdlet == null)
            {
                if ((_cmdletizationMetadata.Class.InstanceCmdlets.GetCmdlet != null) &&
                    (_cmdletizationMetadata.Class.InstanceCmdlets.GetCmdlet.GetCmdletParameters != null))
                {
                    return _cmdletizationMetadata.Class.InstanceCmdlets.GetCmdlet.GetCmdletParameters;
                }
            }
            else
            {
                if (instanceCmdlet.GetCmdletParameters != null)
                {
                    return instanceCmdlet.GetCmdletParameters;
                }
            }

            return _cmdletizationMetadata.Class.InstanceCmdlets.GetCmdletParameters;
        }

        private List<string> GetQueryParameterSets(InstanceCmdletMetadata instanceCmdlet)
        {
            Dictionary<string, object> parameterSetNames = new(StringComparer.OrdinalIgnoreCase);

            var parameters = new List<CmdletParameterMetadataForGetCmdletParameter>();
            bool anyQueryParameters = false;

            GetCmdletParameters getCmdletParameters = GetGetCmdletParameters(instanceCmdlet);
            if (getCmdletParameters.QueryableProperties != null)
            {
                foreach (PropertyMetadata property in getCmdletParameters.QueryableProperties)
                {
                    if (property.Items != null)
                    {
                        foreach (PropertyQuery query in property.Items)
                        {
                            anyQueryParameters = true;
                            if (query.CmdletParameterMetadata != null)
                            {
                                parameters.Add(query.CmdletParameterMetadata);
                            }
                        }
                    }
                }
            }

            if (getCmdletParameters.QueryableAssociations != null)
            {
                foreach (Association association in getCmdletParameters.QueryableAssociations)
                {
                    if (association.AssociatedInstance != null)
                    {
                        anyQueryParameters = true;
                        if (association.AssociatedInstance.CmdletParameterMetadata != null)
                        {
                            parameters.Add(association.AssociatedInstance.CmdletParameterMetadata);
                        }
                    }
                }
            }

            if (getCmdletParameters.QueryOptions != null)
            {
                foreach (QueryOption option in getCmdletParameters.QueryOptions)
                {
                    anyQueryParameters = true;
                    if (option.CmdletParameterMetadata != null)
                    {
                        parameters.Add(option.CmdletParameterMetadata);
                    }
                }
            }

            foreach (CmdletParameterMetadataForGetCmdletParameter parameter in parameters)
            {
                if (parameter.CmdletParameterSets != null)
                {
                    foreach (string parameterSetName in parameter.CmdletParameterSets)
                    {
                        parameterSetNames[parameterSetName] = null;
                    }
                }
            }

            if (anyQueryParameters && (parameterSetNames.Count == 0))
            {
                parameterSetNames.Add(ScriptWriter.SingleQueryParameterSetName, null);
                getCmdletParameters.DefaultCmdletParameterSet = ScriptWriter.SingleQueryParameterSetName;
            }

            if (instanceCmdlet != null)
            {
                parameterSetNames.Add(ScriptWriter.InputObjectQueryParameterSetName, null);
            }

            return new List<string>(parameterSetNames.Keys);
        }

        private Type GetDotNetType(TypeMetadata typeMetadata)
        {
            Dbg.Assert(typeMetadata != null, "Caller should verify typeMetadata != null");

            string psTypeText;
            EnumMetadataEnum matchingEnum = null;

            if (_cmdletizationMetadata.Enums is not null)
            {
                string psType = typeMetadata.PSType;
                foreach (EnumMetadataEnum e in _cmdletizationMetadata.Enums)
                {
                    int index = psType.IndexOf(e.EnumName, StringComparison.Ordinal);
                    if (index == -1)
                    {
                        // Fast return if 'PSType' doesn't contain the enum name at all.
                        continue;
                    }

                    bool matchFound = false;
                    if (index == 0)
                    {
                        // Handle 2 common cases here (cover over 99% of how enum name is used in 'PSType'):
                        //  - 'PSType' is exactly the enum name.
                        //  - 'PSType' is the array format of the enum.
                        ReadOnlySpan<char> remains = psType.AsSpan(e.EnumName.Length);
                        matchFound = remains.Length is 0 || remains.Equals("[]", StringComparison.Ordinal);
                    }

                    if (!matchFound)
                    {
                        // Now we have to fall back to the expensive regular expression matching, because 'PSType'
                        // could be a composite type like 'Nullable<enum_name>' or 'Dictionary<enum_name, object>',
                        // but we don't want the case where the enum name is part of another type's name.
                        matchFound = Regex.IsMatch(psType, $@"\b{Regex.Escape(e.EnumName)}\b");
                    }

                    if (matchFound)
                    {
                        if (matchingEnum is null)
                        {
                            matchingEnum = e;
                            continue;
                        }

                        // If more than one matching enum names were found, we treat it as no match found.
                        matchingEnum = null;
                        break;
                    }
                }
            }

            if (matchingEnum != null)
            {
                psTypeText = typeMetadata.PSType.Replace(matchingEnum.EnumName, EnumWriter.GetEnumFullName(matchingEnum));
            }
            else
            {
                psTypeText = typeMetadata.PSType;
            }

            Type dotNetType = (Type)LanguagePrimitives.ConvertTo(psTypeText, typeof(Type), CultureInfo.InvariantCulture);
            return dotNetType;
        }

        private ParameterMetadata GetParameter(
            string parameterSetName,
            string objectModelParameterName,
            TypeMetadata parameterTypeMetadata,
            CmdletParameterMetadata parameterCmdletization,
            bool isValueFromPipeline,
            bool isValueFromPipelineByPropertyName)
        {
            string parameterName;
            if ((parameterCmdletization != null) && (!string.IsNullOrEmpty(parameterCmdletization.PSName)))
            {
                parameterName = parameterCmdletization.PSName;
            }
            else
            {
                parameterName = objectModelParameterName;
            }

            ParameterMetadata parameterMetadata = new(parameterName);
            parameterMetadata.ParameterType = GetDotNetType(parameterTypeMetadata);
            if (typeof(PSCredential).Equals(parameterMetadata.ParameterType))
            {
                parameterMetadata.Attributes.Add(new CredentialAttribute());
            }

            if (parameterTypeMetadata.ETSType != null)
            {
                parameterMetadata.Attributes.Add(new PSTypeNameAttribute(parameterTypeMetadata.ETSType));
            }

            if (parameterCmdletization != null)
            {
                if (parameterCmdletization.Aliases != null)
                {
                    foreach (string alias in parameterCmdletization.Aliases)
                    {
                        if (!string.IsNullOrEmpty(alias))
                        {
                            parameterMetadata.Aliases.Add(alias);
                        }
                    }
                }

                if (parameterCmdletization.AllowEmptyCollection != null)
                {
                    parameterMetadata.Attributes.Add(new AllowEmptyCollectionAttribute());
                }

                if (parameterCmdletization.AllowEmptyString != null)
                {
                    parameterMetadata.Attributes.Add(new AllowEmptyStringAttribute());
                }

                if (parameterCmdletization.AllowNull != null)
                {
                    parameterMetadata.Attributes.Add(new AllowNullAttribute());
                }

                if (parameterCmdletization.ValidateCount != null)
                {
                    int min = (int)LanguagePrimitives.ConvertTo(parameterCmdletization.ValidateCount.Min, typeof(int), CultureInfo.InvariantCulture);
                    int max = (int)LanguagePrimitives.ConvertTo(parameterCmdletization.ValidateCount.Max, typeof(int), CultureInfo.InvariantCulture);
                    parameterMetadata.Attributes.Add(new ValidateCountAttribute(min, max));
                }

                if (parameterCmdletization.ValidateLength != null)
                {
                    int min = (int)LanguagePrimitives.ConvertTo(parameterCmdletization.ValidateLength.Min, typeof(int), CultureInfo.InvariantCulture);
                    int max = (int)LanguagePrimitives.ConvertTo(parameterCmdletization.ValidateLength.Max, typeof(int), CultureInfo.InvariantCulture);
                    parameterMetadata.Attributes.Add(new ValidateLengthAttribute(min, max));
                }

                if (parameterCmdletization.Obsolete != null)
                {
                    string obsoleteMessage = parameterCmdletization.Obsolete.Message;
                    parameterMetadata.Attributes.Add(obsoleteMessage != null ? new ObsoleteAttribute(obsoleteMessage) : new ObsoleteAttribute());
                }

                if (parameterCmdletization.ValidateNotNull != null)
                {
                    parameterMetadata.Attributes.Add(new ValidateNotNullAttribute());
                }

                if (parameterCmdletization.ValidateNotNullOrEmpty != null)
                {
                    parameterMetadata.Attributes.Add(new ValidateNotNullOrEmptyAttribute());
                }

                if (parameterCmdletization.ValidateRange != null)
                {
                    Type parameterType = parameterMetadata.ParameterType;
                    Type elementType;
                    if (parameterType == null)
                    {
                        elementType = typeof(string);
                    }
                    else
                    {
                        elementType = parameterType.HasElementType ? parameterType.GetElementType() : parameterType;
                    }

                    object min = LanguagePrimitives.ConvertTo(parameterCmdletization.ValidateRange.Min, elementType, CultureInfo.InvariantCulture);
                    object max = LanguagePrimitives.ConvertTo(parameterCmdletization.ValidateRange.Max, elementType, CultureInfo.InvariantCulture);
                    parameterMetadata.Attributes.Add(new ValidateRangeAttribute(min, max));
                }

                if (parameterCmdletization.ValidateSet != null)
                {
                    List<string> allowedValues = new();
                    foreach (string allowedValue in parameterCmdletization.ValidateSet)
                    {
                        allowedValues.Add(allowedValue);
                    }

                    parameterMetadata.Attributes.Add(new ValidateSetAttribute(allowedValues.ToArray()));
                }
            }

            int position = int.MinValue;
            ParameterSetMetadata.ParameterFlags parameterFlags = 0;
            if (parameterCmdletization != null)
            {
                if (!string.IsNullOrEmpty(parameterCmdletization.Position))
                {
                    position = (int)LanguagePrimitives.ConvertTo(parameterCmdletization.Position, typeof(int), CultureInfo.InvariantCulture);
                }

                if (parameterCmdletization.IsMandatorySpecified && parameterCmdletization.IsMandatory)
                {
                    parameterFlags |= ParameterSetMetadata.ParameterFlags.Mandatory;
                }
            }

            if (isValueFromPipeline)
            {
                parameterFlags |= ParameterSetMetadata.ParameterFlags.ValueFromPipeline;
            }

            if (isValueFromPipelineByPropertyName)
            {
                parameterFlags |= ParameterSetMetadata.ParameterFlags.ValueFromPipelineByPropertyName;
            }

            parameterMetadata.ParameterSets.Add(parameterSetName, new ParameterSetMetadata(position, parameterFlags, null));

            return parameterMetadata;
        }

        private ParameterMetadata GetParameter(
            string parameterSetName,
            string objectModelParameterName,
            TypeMetadata parameterType,
            CmdletParameterMetadataForInstanceMethodParameter parameterCmdletization)
        {
            return GetParameter(
                parameterSetName,
                objectModelParameterName,
                parameterType,
                parameterCmdletization,
                false, /* isValueFromPipeline */
                parameterCmdletization != null && parameterCmdletization.ValueFromPipelineByPropertyNameSpecified && parameterCmdletization.ValueFromPipelineByPropertyName);
        }

        private ParameterMetadata GetParameter(
            IEnumerable<string> queryParameterSets,
            string objectModelParameterName,
            TypeMetadata parameterType,
            CmdletParameterMetadataForGetCmdletParameter parameterCmdletization)
        {
            ParameterMetadata result = GetParameter(
                ParameterAttribute.AllParameterSets,
                objectModelParameterName,
                parameterType,
                parameterCmdletization,
                parameterCmdletization != null && parameterCmdletization.ValueFromPipelineSpecified && parameterCmdletization.ValueFromPipeline,
                parameterCmdletization != null && parameterCmdletization.ValueFromPipelineByPropertyNameSpecified && parameterCmdletization.ValueFromPipelineByPropertyName);

            ParameterSetMetadata parameterSetMetadata = result.ParameterSets[ParameterAttribute.AllParameterSets];
            result.ParameterSets.Clear();
            if (parameterCmdletization != null && parameterCmdletization.CmdletParameterSets != null && parameterCmdletization.CmdletParameterSets.Length > 0)
            {
                queryParameterSets = parameterCmdletization.CmdletParameterSets;
            }

            foreach (string parameterSetName in queryParameterSets)
            {
                if (parameterSetName.Equals(ScriptWriter.InputObjectQueryParameterSetName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                result.ParameterSets.Add(parameterSetName, parameterSetMetadata);
            }

            return result;
        }

        private ParameterMetadata GetParameter(
            string parameterSetName,
            string objectModelParameterName,
            TypeMetadata parameterType,
            CmdletParameterMetadataForStaticMethodParameter parameterCmdletization)
        {
            return GetParameter(
                parameterSetName,
                objectModelParameterName,
                parameterType,
                parameterCmdletization,
                parameterCmdletization != null && parameterCmdletization.ValueFromPipelineSpecified && parameterCmdletization.ValueFromPipeline,
                parameterCmdletization != null && parameterCmdletization.ValueFromPipelineByPropertyNameSpecified && parameterCmdletization.ValueFromPipelineByPropertyName);
        }

        private void SetParameters(CommandMetadata commandMetadata, params Dictionary<string, ParameterMetadata>[] allParameters)
        {
            commandMetadata.Parameters.Clear();
            foreach (Dictionary<string, ParameterMetadata> parameters in allParameters)
            {
                foreach (KeyValuePair<string, ParameterMetadata> parameter in parameters)
                {
                    if (commandMetadata.Parameters.ContainsKey(parameter.Key))
                    {
                        if (this.GetCommonParameters().ContainsKey(parameter.Key))
                        {
                            string message = string.Format(
                                CultureInfo.InvariantCulture, // parameter name
                                CmdletizationCoreResources.ScriptWriter_ParameterNameConflictsWithCommonParameters,
                                parameter.Key,
                                commandMetadata.Name,
                                _objectModelWrapper.FullName);
                            throw new XmlException(message);
                        }
                        else
                        {
                            string message = string.Format(
                                CultureInfo.InvariantCulture, // parameter name
                                CmdletizationCoreResources.ScriptWriter_ParameterNameConflictsWithQueryParameters,
                                parameter.Key,
                                commandMetadata.Name,
                                "<GetCmdletParameters>");
                            throw new XmlException(message);
                        }
                    }

                    commandMetadata.Parameters.Add(parameter.Key, parameter.Value);
                }
            }
        }

        private CommandMetadata GetCommandMetadata(CommonCmdletMetadata cmdletMetadata)
        {
            string defaultParameterSetName = null;
            StaticCmdletMetadataCmdletMetadata staticCmdletMetadata = cmdletMetadata as StaticCmdletMetadataCmdletMetadata;
            if (staticCmdletMetadata != null)
            {
                if (!string.IsNullOrEmpty(staticCmdletMetadata.DefaultCmdletParameterSet))
                {
                    defaultParameterSetName = staticCmdletMetadata.DefaultCmdletParameterSet;
                }
            }

            var confirmImpact = System.Management.Automation.ConfirmImpact.None;
            if (cmdletMetadata.ConfirmImpactSpecified)
            {
                confirmImpact = (System.Management.Automation.ConfirmImpact)(int)cmdletMetadata.ConfirmImpact;
            }

            Dictionary<string, ParameterMetadata> parameters = new(StringComparer.OrdinalIgnoreCase);

            CommandMetadata commandMetadata = new(
                                   name: this.GetCmdletName(cmdletMetadata),
                            commandType: CommandTypes.Cmdlet,
                       isProxyForCmdlet: true,
                defaultParameterSetName: defaultParameterSetName, // this can only be figured out for static cmdlets - instance cmdlets have to set that separately
                  supportsShouldProcess: confirmImpact != System.Management.Automation.ConfirmImpact.None,
                          confirmImpact: confirmImpact,
                         supportsPaging: false,
                   supportsTransactions: false,
                      positionalBinding: false,
                             parameters: parameters);

            if (!string.IsNullOrEmpty(cmdletMetadata.HelpUri))
            {
                commandMetadata.HelpUri = cmdletMetadata.HelpUri;
            }

            return commandMetadata;
        }

        private static string EscapeModuleNameForHelpComment(string name)
        {
            Dbg.Assert(name != null, "Caller should verify name != null");

            StringBuilder result = new(name.Length);
            foreach (char c in name)
            {
                if (!"\"'`$#".Contains(c)
                    && !char.IsControl(c)
                    && !char.IsWhiteSpace(c))
                {
                    result.Append(c);
                }
            }

            return result.ToString();
        }

        private static List<List<string>> GetCombinations(params IEnumerable<string>[] x)
        {
            Dbg.Assert(x != null, "Caller to verify that x != null");
            Dbg.Assert(x.Length > 0, "Caller to verify that x.Length > 0");

            if (x.Length == 1)
            {
                List<List<string>> result = new();
                foreach (string s in x[0])
                {
                    List<string> subresult = new();
                    subresult.Add(s);
                    result.Add(subresult);
                }

                return result;
            }
            else
            {
                IEnumerable<string>[] smallX = new IEnumerable<string>[x.Length - 1];
                Array.Copy(x, 0, smallX, 0, smallX.Length);
                List<List<string>> smallResult = GetCombinations(smallX);

                List<List<string>> result = new();
                foreach (List<string> smallSubresult in smallResult)
                {
                    foreach (string s in x[x.Length - 1])
                    {
                        List<string> newsubresult = new(smallSubresult);
                        newsubresult.Add(s);
                        result.Add(newsubresult);
                    }
                }

                return result;
            }
        }

        private static void EnsureOrderOfPositionalParameters(
            Dictionary<string, ParameterMetadata> beforeParameters,
            Dictionary<string, ParameterMetadata> afterParameters)
        {
            int maxBeforePosition = int.MinValue;
            foreach (ParameterMetadata beforeParameter in beforeParameters.Values)
            {
                foreach (ParameterSetMetadata beforeParameterSet in beforeParameter.ParameterSets.Values)
                {
                    maxBeforePosition = Math.Max(beforeParameterSet.Position, maxBeforePosition);
                }
            }

            int minAfterPosition = int.MaxValue;
            foreach (ParameterMetadata afterParameter in afterParameters.Values)
            {
                foreach (ParameterSetMetadata afterParameterSet in afterParameter.ParameterSets.Values)
                {
                    if (afterParameterSet.Position != int.MinValue)
                    {
                        minAfterPosition = Math.Min(afterParameterSet.Position, minAfterPosition);
                    }
                }
            }

            if ((maxBeforePosition >= 0) && (minAfterPosition <= maxBeforePosition))
            {
                int delta = (1001 - minAfterPosition % 1000);
                foreach (ParameterMetadata afterParameter in afterParameters.Values)
                {
                    foreach (ParameterSetMetadata afterParameterSet in afterParameter.ParameterSets.Values)
                    {
                        if (afterParameterSet.Position != int.MinValue)
                        {
                            checked { afterParameterSet.Position += delta; }
                        }
                    }
                }
            }
        }

        private const string StaticCommonParameterSetTemplate = "{1}"; // "{0}::{1}";
        private const string StaticMethodParameterSetTemplate = "{0}"; // "{1}::{0}";

        private const string InstanceCommonParameterSetTemplate = "{1}"; // "{0}::{1}::{2}";
        private const string InstanceQueryParameterSetTemplate = "{0}"; // "{1}::{0}::{2}";
        private const string InstanceMethodParameterSetTemplate = "{2}"; // "{1}::{2}::{0}";

        private const string InputObjectQueryParameterSetName = "InputObject (cdxml)";
        private const string SingleQueryParameterSetName = "Query (cdxml)";

        private static void MultiplyParameterSets(
            Dictionary<string, ParameterMetadata> parameters,
            string parameterSetNameTemplate, // {0} is the original parameter set, other ones are taken from the otherParameterSets array
            params IEnumerable<string>[] otherParameterSets)
        {
            List<List<string>> combinations = GetCombinations(otherParameterSets);

            foreach (ParameterMetadata parameter in parameters.Values)
            {
                List<KeyValuePair<string, ParameterSetMetadata>> oldParameterSets = new(parameter.ParameterSets);

                parameter.ParameterSets.Clear();
                foreach (KeyValuePair<string, ParameterSetMetadata> oldParameterSet in oldParameterSets)
                {
                    foreach (List<string> combination in combinations)
                    {
                        string[] formattingArray = new string[otherParameterSets.Length + 1];
                        formattingArray[0] = oldParameterSet.Key;
                        combination.CopyTo(formattingArray, 1);
                        string newParameterSetName = string.Format(CultureInfo.InvariantCulture, parameterSetNameTemplate, formattingArray);

                        parameter.ParameterSets.Add(newParameterSetName, oldParameterSet.Value);
                    }
                }
            }
        }

        private static IEnumerable<string> MultiplyParameterSets(
            string mainParameterSet,
            string parameterSetNameTemplate, // {0} is the original parameter set, other ones are taken from the otherParameterSets array
            params IEnumerable<string>[] otherParameterSets)
        {
            List<string> result = new();

            List<List<string>> combinations = GetCombinations(otherParameterSets);
            foreach (List<string> combination in combinations)
            {
                string[] formattingArray = new string[otherParameterSets.Length + 1];
                formattingArray[0] = mainParameterSet;
                combination.CopyTo(formattingArray, 1);
                string newParameterSetName = string.Format(CultureInfo.InvariantCulture, parameterSetNameTemplate, formattingArray);
                result.Add(newParameterSetName);
            }

            return result;
        }

        private static MethodParameterBindings GetMethodParameterKind(InstanceMethodParameterMetadata methodParameter)
        {
            Dbg.Assert(methodParameter != null, "Caller should verify methodParameter != null");

            MethodParameterBindings bindings = 0;
            if (methodParameter.CmdletParameterMetadata != null)
            {
                bindings |= MethodParameterBindings.In;
            }

            if (methodParameter.CmdletOutputMetadata != null)
            {
                if (methodParameter.CmdletOutputMetadata.ErrorCode == null)
                {
                    bindings |= MethodParameterBindings.Out;
                }
                else
                {
                    bindings |= MethodParameterBindings.Error;
                }
            }

            return bindings;
        }

        private static MethodParameterBindings GetMethodParameterKind(StaticMethodParameterMetadata methodParameter)
        {
            Dbg.Assert(methodParameter != null, "Caller should verify methodParameter != null");

            MethodParameterBindings bindings = 0;
            if (methodParameter.CmdletParameterMetadata != null)
            {
                bindings |= MethodParameterBindings.In;
            }

            if (methodParameter.CmdletOutputMetadata != null)
            {
                if (methodParameter.CmdletOutputMetadata.ErrorCode == null)
                {
                    bindings |= MethodParameterBindings.Out;
                }
                else
                {
                    bindings |= MethodParameterBindings.Error;
                }
            }

            return bindings;
        }

        private static MethodParameterBindings GetMethodParameterKind(CommonMethodMetadataReturnValue returnValue)
        {
            Dbg.Assert(returnValue != null, "Caller should verify returnValue != null");

            MethodParameterBindings bindings = 0;
            if (returnValue.CmdletOutputMetadata != null)
            {
                if (returnValue.CmdletOutputMetadata.ErrorCode == null)
                {
                    bindings |= MethodParameterBindings.Out;
                }
                else
                {
                    bindings |= MethodParameterBindings.Error;
                }
            }

            return bindings;
        }

        private static void GenerateSingleMethodParameterProcessing(
            TextWriter output,
            string prefix,
            string cmdletParameterName,
            Type cmdletParameterType,
            string etsParameterTypeName,
            string cmdletParameterDefaultValue,
            string methodParameterName,
            MethodParameterBindings methodParameterBindings)
        {
            Dbg.Assert(output != null, "Called should verify output != null");
            Dbg.Assert(prefix != null, "Called should verify output != null");
            // cmdletParameterName can be null for 'out' parameters
            string cmdletParameterTypeName = (cmdletParameterType ?? typeof(object)).FullName;
            Dbg.Assert(methodParameterName != null, "Caller should verify methodParameterName != null");

            if (cmdletParameterDefaultValue != null)
            {
                output.WriteLine(
                    "{0}[object]$__cmdletization_defaultValue = [System.Management.Automation.LanguagePrimitives]::ConvertTo('{1}', '{2}')",
                    prefix,
                    CodeGeneration.EscapeSingleQuotedStringContent(cmdletParameterDefaultValue),
                    CodeGeneration.EscapeSingleQuotedStringContent(cmdletParameterTypeName));
                output.WriteLine(
                    "{0}[object]$__cmdletization_defaultValueIsPresent = $true",
                    prefix);
            }
            else
            {
                output.WriteLine(
                    "{0}[object]$__cmdletization_defaultValue = $null",
                    prefix);
                output.WriteLine(
                    "{0}[object]$__cmdletization_defaultValueIsPresent = $false",
                    prefix);
            }

            if ((methodParameterBindings & MethodParameterBindings.In) == MethodParameterBindings.In)
            {
                Dbg.Assert(cmdletParameterName != null, "Called should verify cmdletParameterName!=null for 'in' parameters");

                output.WriteLine(
                    "{0}if ($PSBoundParameters.ContainsKey('{1}')) {{",
                    prefix,
                    CodeGeneration.EscapeSingleQuotedStringContent(cmdletParameterName));
                output.WriteLine(
                    "{0}  [object]$__cmdletization_value = ${{{1}}}",
                    prefix,
                    CodeGeneration.EscapeVariableName(cmdletParameterName));
                output.WriteLine(
                    "{0}  $__cmdletization_methodParameter = [Microsoft.PowerShell.Cmdletization.MethodParameter]@{{Name = '{1}'; ParameterType = '{2}'; Bindings = '{3}'; Value = $__cmdletization_value; IsValuePresent = $true}}",
                    prefix,
                    CodeGeneration.EscapeSingleQuotedStringContent(methodParameterName),
                    CodeGeneration.EscapeSingleQuotedStringContent(cmdletParameterTypeName),
                    CodeGeneration.EscapeSingleQuotedStringContent(methodParameterBindings.ToString()));
                output.WriteLine("{0}}} else {{", prefix);
            }

            output.WriteLine(
                "{0}  $__cmdletization_methodParameter = [Microsoft.PowerShell.Cmdletization.MethodParameter]@{{Name = '{1}'; ParameterType = '{2}'; Bindings = '{3}'; Value = $__cmdletization_defaultValue; IsValuePresent = $__cmdletization_defaultValueIsPresent}}",
                prefix,
                CodeGeneration.EscapeSingleQuotedStringContent(methodParameterName),
                CodeGeneration.EscapeSingleQuotedStringContent(cmdletParameterTypeName),
                CodeGeneration.EscapeSingleQuotedStringContent(methodParameterBindings.ToString()));

            if ((methodParameterBindings & MethodParameterBindings.In) == MethodParameterBindings.In)
            {
                output.WriteLine("{0}}}", prefix);
            }

            if (!string.IsNullOrEmpty(etsParameterTypeName))
            {
                output.WriteLine(
                    "{0}$__cmdletization_methodParameter.ParameterTypeName = '{1}'",
                    prefix,
                    CodeGeneration.EscapeSingleQuotedStringContent(etsParameterTypeName));
            }

            output.WriteLine("{0}$__cmdletization_methodParameters.Add($__cmdletization_methodParameter)", prefix);
            output.WriteLine();
        }

        private void GenerateMethodParametersProcessing(
            StaticCmdletMetadata staticCmdlet,
            IEnumerable<string> commonParameterSets,
            out string scriptCode,
            out Dictionary<string, ParameterMetadata> methodParameters,
            out string outputTypeAttributeDeclaration)
        {
            methodParameters = new Dictionary<string, ParameterMetadata>(StringComparer.OrdinalIgnoreCase);
            StringBuilder outputTypeAttributeDeclarationBuilder = new();
            StringWriter output = new(CultureInfo.InvariantCulture);

            output.WriteLine("      $__cmdletization_methodParameters = [System.Collections.Generic.List[Microsoft.PowerShell.Cmdletization.MethodParameter]]::new()");
            output.WriteLine();

            bool multipleMethods = staticCmdlet.Method.Length > 1;
            if (multipleMethods)
            {
                output.WriteLine("      switch -exact ($PSCmdlet.ParameterSetName) { ");
            }

            foreach (StaticMethodMetadata method in staticCmdlet.Method)
            {
                if (multipleMethods)
                {
                    output.Write("        { @(");
                    bool firstParameterSet = true;
                    foreach (
                        string parameterSetName in
                            MultiplyParameterSets(
                                GetMethodParameterSet(method), StaticMethodParameterSetTemplate, commonParameterSets))
                    {
                        if (!firstParameterSet) output.Write(", ");
                        firstParameterSet = false;
                        output.Write("'{0}'", CodeGeneration.EscapeSingleQuotedStringContent(parameterSetName));
                    }

                    output.WriteLine(") -contains $_ } {");
                }

                List<Type> typesOfOutParameters = new();
                List<string> etsTypesOfOutParameters = new();
                if (method.Parameters != null)
                {
                    foreach (StaticMethodParameterMetadata methodParameter in method.Parameters)
                    {
                        string cmdletParameterName = null;
                        if (methodParameter.CmdletParameterMetadata != null)
                        {
                            string parameterSetName = GetMethodParameterSet(method);
                            ParameterMetadata parameterMetadata = GetParameter(
                                parameterSetName,
                                methodParameter.ParameterName,
                                methodParameter.Type,
                                methodParameter.CmdletParameterMetadata);
                            cmdletParameterName = parameterMetadata.Name;

                            ParameterMetadata oldParameterMetadata;
                            if (methodParameters.TryGetValue(parameterMetadata.Name, out oldParameterMetadata))
                            {
                                try
                                {
                                    oldParameterMetadata.ParameterSets.Add(parameterSetName, parameterMetadata.ParameterSets[parameterSetName]);
                                }
                                catch (ArgumentException e)
                                {
                                    string message = string.Format(
                                        CultureInfo.InvariantCulture, // xml element names and parameter names are culture-agnostic
                                        CmdletizationCoreResources.ScriptWriter_DuplicateQueryParameterName,
                                        "<StaticCmdlets>...<Cmdlet>...<Method>",
                                        parameterMetadata.Name);
                                    throw new XmlException(message, e);
                                }
                            }
                            else
                            {
                                methodParameters.Add(parameterMetadata.Name, parameterMetadata);
                            }
                        }

                        MethodParameterBindings methodParameterBindings = GetMethodParameterKind(methodParameter);
                        Type dotNetTypeOfParameter = GetDotNetType(methodParameter.Type);
                        GenerateSingleMethodParameterProcessing(
                            output,
                            "        ",
                            cmdletParameterName,
                            dotNetTypeOfParameter,
                            methodParameter.Type.ETSType,
                            methodParameter.DefaultValue,
                            methodParameter.ParameterName,
                            methodParameterBindings);

                        if ((methodParameterBindings & MethodParameterBindings.Out) == MethodParameterBindings.Out)
                        {
                            typesOfOutParameters.Add(dotNetTypeOfParameter);
                            etsTypesOfOutParameters.Add(methodParameter.Type.ETSType);
                        }
                    }
                }

                if (method.ReturnValue != null)
                {
                    MethodParameterBindings methodParameterBindings = GetMethodParameterKind(method.ReturnValue);
                    Type dotNetTypeOfParameter = GetDotNetType(method.ReturnValue.Type);
                    output.WriteLine(
                        "      $__cmdletization_returnValue = [Microsoft.PowerShell.Cmdletization.MethodParameter]@{{ Name = 'ReturnValue'; ParameterType = '{0}'; Bindings = '{1}'; Value = $null; IsValuePresent = $false }}",
                        CodeGeneration.EscapeSingleQuotedStringContent(dotNetTypeOfParameter.FullName),
                        CodeGeneration.EscapeSingleQuotedStringContent(methodParameterBindings.ToString()));

                    if (!string.IsNullOrEmpty(method.ReturnValue.Type.ETSType))
                    {
                        output.WriteLine(
                            "      $__cmdletization_methodParameter.ParameterTypeName = '{0}'",
                            CodeGeneration.EscapeSingleQuotedStringContent(method.ReturnValue.Type.ETSType));
                    }

                    if ((methodParameterBindings & MethodParameterBindings.Out) == MethodParameterBindings.Out)
                    {
                        typesOfOutParameters.Add(dotNetTypeOfParameter);
                        etsTypesOfOutParameters.Add(method.ReturnValue.Type.ETSType);
                    }
                }
                else
                {
                    output.WriteLine("      $__cmdletization_returnValue = $null");
                }

                output.WriteLine(
                    "      $__cmdletization_methodInvocationInfo = [Microsoft.PowerShell.Cmdletization.MethodInvocationInfo]::new('{0}', $__cmdletization_methodParameters, $__cmdletization_returnValue)",
                    CodeGeneration.EscapeSingleQuotedStringContent(method.MethodName));
                output.WriteLine("      $__cmdletization_objectModelWrapper.ProcessRecord($__cmdletization_methodInvocationInfo)");

                if (multipleMethods)
                {
                    output.WriteLine("        }");
                }

                if (typesOfOutParameters.Count == 1)
                {
                    outputTypeAttributeDeclarationBuilder.AppendFormat(
                        CultureInfo.InvariantCulture,
                        "[OutputType([{0}])]",
                        typesOfOutParameters[0].FullName);

                    if ((etsTypesOfOutParameters.Count == 1) && (!string.IsNullOrEmpty(etsTypesOfOutParameters[0])))
                    {
                        outputTypeAttributeDeclarationBuilder.AppendFormat(
                            CultureInfo.InvariantCulture,
                            "[OutputType('{0}')]",
                            CodeGeneration.EscapeSingleQuotedStringContent(etsTypesOfOutParameters[0]));
                    }
                }
            }

            if (multipleMethods)
            {
                output.WriteLine("    }");
            }

            scriptCode = output.ToString();
            outputTypeAttributeDeclaration = outputTypeAttributeDeclarationBuilder.ToString();
        }

        private void GenerateMethodParametersProcessing(
            InstanceCmdletMetadata instanceCmdlet,
            IEnumerable<string> commonParameterSets,
            IEnumerable<string> queryParameterSets,
            out string scriptCode,
            out Dictionary<string, ParameterMetadata> methodParameters,
            out string outputTypeAttributeDeclaration)
        {
            methodParameters = new Dictionary<string, ParameterMetadata>(StringComparer.OrdinalIgnoreCase);
            outputTypeAttributeDeclaration = string.Empty;
            StringWriter output = new(CultureInfo.InvariantCulture);

            output.WriteLine("    $__cmdletization_methodParameters = [System.Collections.Generic.List[Microsoft.PowerShell.Cmdletization.MethodParameter]]::new()");
            output.WriteLine("    switch -exact ($PSCmdlet.ParameterSetName) { ");

            InstanceMethodMetadata method = instanceCmdlet.Method;
            output.Write("        { @(");
            bool firstParameterSet = true;
            foreach (string parameterSetName in MultiplyParameterSets(GetMethodParameterSet(method), InstanceMethodParameterSetTemplate, commonParameterSets, queryParameterSets))
            {
                if (!firstParameterSet) output.Write(", ");
                firstParameterSet = false;
                output.Write("'{0}'", CodeGeneration.EscapeSingleQuotedStringContent(parameterSetName));
            }

            output.WriteLine(") -contains $_ } {");

            List<Type> typesOfOutParameters = new();
            List<string> etsTypesOfOutParameters = new();
            if (method.Parameters != null)
            {
                foreach (InstanceMethodParameterMetadata methodParameter in method.Parameters)
                {
                    string cmdletParameterName = null;
                    if (methodParameter.CmdletParameterMetadata != null)
                    {
                        ParameterMetadata parameterMetadata = GetParameter(
                            GetMethodParameterSet(method),
                            methodParameter.ParameterName,
                            methodParameter.Type,
                            methodParameter.CmdletParameterMetadata);
                        cmdletParameterName = parameterMetadata.Name;

                        try
                        {
                            methodParameters.Add(parameterMetadata.Name, parameterMetadata);
                        }
                        catch (ArgumentException e)
                        {
                            string message = string.Format(
                                CultureInfo.InvariantCulture, // xml element names and parameter names are culture-agnostic
                                CmdletizationCoreResources.ScriptWriter_DuplicateQueryParameterName,
                                "<InstanceCmdlets>...<Cmdlet>",
                                parameterMetadata.Name);
                            throw new XmlException(message, e);
                        }
                    }

                    MethodParameterBindings methodParameterBindings = GetMethodParameterKind(methodParameter);
                    Type dotNetTypeOfParameter = GetDotNetType(methodParameter.Type);
                    GenerateSingleMethodParameterProcessing(
                        output,
                        "          ",
                        cmdletParameterName,
                        dotNetTypeOfParameter,
                        methodParameter.Type.ETSType,
                        methodParameter.DefaultValue,
                        methodParameter.ParameterName,
                        methodParameterBindings);

                    if ((methodParameterBindings & MethodParameterBindings.Out) == MethodParameterBindings.Out)
                    {
                        typesOfOutParameters.Add(dotNetTypeOfParameter);
                        etsTypesOfOutParameters.Add(methodParameter.Type.ETSType);
                    }
                }
            }

            if (method.ReturnValue != null)
            {
                MethodParameterBindings methodParameterBindings = GetMethodParameterKind(method.ReturnValue);
                Type dotNetTypeOfParameter = GetDotNetType(method.ReturnValue.Type);
                output.WriteLine(
                    "      $__cmdletization_returnValue = [Microsoft.PowerShell.Cmdletization.MethodParameter]@{{ Name = 'ReturnValue'; ParameterType = '{0}'; Bindings = '{1}'; Value = $null; IsValuePresent = $false }}",
                    CodeGeneration.EscapeSingleQuotedStringContent(dotNetTypeOfParameter.FullName),
                    CodeGeneration.EscapeSingleQuotedStringContent(methodParameterBindings.ToString()));

                if (!string.IsNullOrEmpty(method.ReturnValue.Type.ETSType))
                {
                    output.WriteLine(
                        "      $__cmdletization_methodParameter.ParameterTypeName = '{0}'",
                        CodeGeneration.EscapeSingleQuotedStringContent(method.ReturnValue.Type.ETSType));
                }

                if ((methodParameterBindings & MethodParameterBindings.Out) == MethodParameterBindings.Out)
                {
                    typesOfOutParameters.Add(dotNetTypeOfParameter);
                    etsTypesOfOutParameters.Add(method.ReturnValue.Type.ETSType);
                }
            }
            else
            {
                output.WriteLine("      $__cmdletization_returnValue = $null");
            }

            output.WriteLine(
                "      $__cmdletization_methodInvocationInfo = [Microsoft.PowerShell.Cmdletization.MethodInvocationInfo]::new('{0}', $__cmdletization_methodParameters, $__cmdletization_returnValue)",
                CodeGeneration.EscapeSingleQuotedStringContent(method.MethodName));

            if (typesOfOutParameters.Count == 0)
            {
                output.WriteLine(
                    "      $__cmdletization_passThru = $PSBoundParameters.ContainsKey('PassThru') -and $PassThru");
            }
            else
            {
                output.WriteLine(
                    "      $__cmdletization_passThru = $false");
            }

            output.WriteLine("            if ($PSBoundParameters.ContainsKey('InputObject')) {");
            output.WriteLine("                foreach ($x in $InputObject) { $__cmdletization_objectModelWrapper.ProcessRecord($x, $__cmdletization_methodInvocationInfo, $__cmdletization_PassThru) }");
            output.WriteLine("            } else {");
            output.WriteLine("                $__cmdletization_objectModelWrapper.ProcessRecord($__cmdletization_queryBuilder, $__cmdletization_methodInvocationInfo, $__cmdletization_PassThru)");
            output.WriteLine("            }");
            output.WriteLine("        }");
            output.WriteLine("    }");

            scriptCode = output.ToString();

            if (typesOfOutParameters.Count == 0)
            {
                // -PassThru case
                outputTypeAttributeDeclaration = GetOutputAttributeForGetCmdlet();
            }
            else if (typesOfOutParameters.Count == 1)
            {
                outputTypeAttributeDeclaration = string.Format(
                    CultureInfo.InvariantCulture,
                    "[OutputType([{0}])]",
                    typesOfOutParameters[0].FullName);
                if ((etsTypesOfOutParameters.Count == 1) && (!string.IsNullOrEmpty(etsTypesOfOutParameters[0])))
                {
                    outputTypeAttributeDeclaration += string.Format(
                        CultureInfo.InvariantCulture,
                        "[OutputType('{0}')]",
                        CodeGeneration.EscapeSingleQuotedStringContent(etsTypesOfOutParameters[0]));
                }
            }
        }

        private static void GenerateIfBoundParameter(
            IEnumerable<string> commonParameterSets,
            IEnumerable<string> methodParameterSets,
            ParameterMetadata cmdletParameterMetadata,
            TextWriter output)
        {
            output.Write("    if ($PSBoundParameters.ContainsKey('{0}') -and (@(", CodeGeneration.EscapeSingleQuotedStringContent(cmdletParameterMetadata.Name));
            bool firstParameterSet = true;
            foreach (string queryParameterSetName in cmdletParameterMetadata.ParameterSets.Keys)
                foreach (string parameterSetName in MultiplyParameterSets(queryParameterSetName, InstanceQueryParameterSetTemplate, commonParameterSets, methodParameterSets))
                {
                    if (!firstParameterSet) output.Write(", ");
                    firstParameterSet = false;
                    output.Write("'{0}'", CodeGeneration.EscapeSingleQuotedStringContent(parameterSetName));
                }

            output.WriteLine(") -contains $PSCmdlet.ParameterSetName )) {");
        }

        private ParameterMetadata GenerateQueryClause(
            IEnumerable<string> commonParameterSets,
            IEnumerable<string> queryParameterSets,
            IEnumerable<string> methodParameterSets,
            string queryBuilderMethodName,
            PropertyMetadata property,
            PropertyQuery query,
            TextWriter output)
        {
            ParameterMetadata cmdletParameterMetadata = GetParameter(
                queryParameterSets,
                property.PropertyName,
                property.Type,
                query.CmdletParameterMetadata);

            WildcardablePropertyQuery wildcardablePropertyQuery = query as WildcardablePropertyQuery;
            if ((wildcardablePropertyQuery != null) && (!cmdletParameterMetadata.SwitchParameter))
            {
                if (cmdletParameterMetadata.ParameterType == null)
                {
                    cmdletParameterMetadata.ParameterType = typeof(object);
                }

                cmdletParameterMetadata.ParameterType = cmdletParameterMetadata.ParameterType.MakeArrayType();
            }

            GenerateIfBoundParameter(commonParameterSets, methodParameterSets, cmdletParameterMetadata, output);

            string localVariableName = wildcardablePropertyQuery == null ? "__cmdletization_value" : "__cmdletization_values";
            if (wildcardablePropertyQuery == null)
            {
                output.WriteLine(
                    "        [object]${0} = ${{{1}}}",
                    localVariableName,
                    CodeGeneration.EscapeVariableName(cmdletParameterMetadata.Name));
            }
            else
            {
                output.WriteLine(
                    "        ${0} = @(${{{1}}})",
                    localVariableName,
                    CodeGeneration.EscapeVariableName(cmdletParameterMetadata.Name));
            }

            output.Write(
                "        $__cmdletization_queryBuilder.{0}('{1}', ${2}",
                queryBuilderMethodName,
                CodeGeneration.EscapeSingleQuotedStringContent(property.PropertyName),
                localVariableName);
            if (wildcardablePropertyQuery == null)
            {
                output.WriteLine(
                    ", '{0}')",
                    GetBehaviorWhenNoMatchesFound(query.CmdletParameterMetadata));
            }
            else
            {
                bool allowGlobbing =
                    (!wildcardablePropertyQuery.AllowGlobbingSpecified && cmdletParameterMetadata.ParameterType.Equals(typeof(string[]))) ||
                    (wildcardablePropertyQuery.AllowGlobbingSpecified && wildcardablePropertyQuery.AllowGlobbing);
                output.WriteLine(
                    ", {0}, '{1}')",
                    allowGlobbing ? "$true" : "$false",
                    GetBehaviorWhenNoMatchesFound(query.CmdletParameterMetadata));
            }

            output.WriteLine("    }");

            return cmdletParameterMetadata;
        }

        private static BehaviorOnNoMatch GetBehaviorWhenNoMatchesFound(CmdletParameterMetadataForGetCmdletFilteringParameter cmdletParameterMetadata)
        {
            if ((cmdletParameterMetadata == null) || (!cmdletParameterMetadata.ErrorOnNoMatchSpecified))
            {
                return BehaviorOnNoMatch.Default;
            }

            if (cmdletParameterMetadata.ErrorOnNoMatch)
            {
                return BehaviorOnNoMatch.ReportErrors;
            }
            else
            {
                return BehaviorOnNoMatch.SilentlyContinue;
            }
        }

        private ParameterMetadata GenerateAssociationClause(
            IEnumerable<string> commonParameterSets,
            IEnumerable<string> queryParameterSets,
            IEnumerable<string> methodParameterSets,
            Association associationMetadata,
            AssociationAssociatedInstance associatedInstanceMetadata,
            TextWriter output)
        {
            ParameterMetadata cmdletParameterMetadata = GetParameter(
                queryParameterSets,
                associationMetadata.SourceRole,
                associatedInstanceMetadata.Type,
                associatedInstanceMetadata.CmdletParameterMetadata);
            cmdletParameterMetadata.Attributes.Add(new ValidateNotNullAttribute());

            GenerateIfBoundParameter(commonParameterSets, methodParameterSets, cmdletParameterMetadata, output);

            output.WriteLine(
                "    $__cmdletization_queryBuilder.FilterByAssociatedInstance(${{{0}}}, '{1}', '{2}', '{3}', '{4}')",
                CodeGeneration.EscapeVariableName(cmdletParameterMetadata.Name),
                CodeGeneration.EscapeSingleQuotedStringContent(associationMetadata.Association1),
                CodeGeneration.EscapeSingleQuotedStringContent(associationMetadata.SourceRole),
                CodeGeneration.EscapeSingleQuotedStringContent(associationMetadata.ResultRole),
                GetBehaviorWhenNoMatchesFound(associatedInstanceMetadata.CmdletParameterMetadata));

            output.WriteLine("    }");

            return cmdletParameterMetadata;
        }

        private ParameterMetadata GenerateOptionClause(
            IEnumerable<string> commonParameterSets,
            IEnumerable<string> queryParameterSets,
            IEnumerable<string> methodParameterSets,
            QueryOption queryOptionMetadata,
            TextWriter output)
        {
            ParameterMetadata cmdletParameterMetadata = GetParameter(
                queryParameterSets,
                queryOptionMetadata.OptionName,
                queryOptionMetadata.Type,
                queryOptionMetadata.CmdletParameterMetadata);

            GenerateIfBoundParameter(commonParameterSets, methodParameterSets, cmdletParameterMetadata, output);

            output.WriteLine(
                "    $__cmdletization_queryBuilder.AddQueryOption('{0}', ${{{1}}})",
                CodeGeneration.EscapeSingleQuotedStringContent(queryOptionMetadata.OptionName),
                CodeGeneration.EscapeVariableName(cmdletParameterMetadata.Name));

            output.WriteLine("    }");

            return cmdletParameterMetadata;
        }

        private void GenerateQueryParametersProcessing(
            InstanceCmdletMetadata instanceCmdlet,
            IEnumerable<string> commonParameterSets,
            IEnumerable<string> queryParameterSets,
            IEnumerable<string> methodParameterSets,
            out string scriptCode,
            out Dictionary<string, ParameterMetadata> queryParameters)
        {
            queryParameters = new Dictionary<string, ParameterMetadata>(StringComparer.OrdinalIgnoreCase);
            StringWriter output = new(CultureInfo.InvariantCulture);

            output.WriteLine("    $__cmdletization_queryBuilder = $__cmdletization_objectModelWrapper.GetQueryBuilder()");

            GetCmdletParameters getCmdletParameters = GetGetCmdletParameters(instanceCmdlet);
            if (getCmdletParameters.QueryableProperties != null)
            {
                foreach (PropertyMetadata property in getCmdletParameters.QueryableProperties.Where(static p => p.Items != null))
                {
                    for (int i = 0; i < property.Items.Length; i++)
                    {
                        string methodName;
                        switch (property.ItemsElementName[i])
                        {
                            case ItemsChoiceType.RegularQuery:
                                methodName = "FilterByProperty";
                                break;
                            case ItemsChoiceType.ExcludeQuery:
                                methodName = "ExcludeByProperty";
                                break;
                            case ItemsChoiceType.MinValueQuery:
                                methodName = "FilterByMinPropertyValue";
                                break;
                            case ItemsChoiceType.MaxValueQuery:
                                methodName = "FilterByMaxPropertyValue";
                                break;
                            default:
                                Dbg.Assert(false, "Unrecognized query xml element");
                                methodName = "NotAValidMethod";
                                break;
                        }

                        ParameterMetadata parameterMetadata = GenerateQueryClause(
                            commonParameterSets, queryParameterSets, methodParameterSets, methodName, property, property.Items[i], output);

                        switch (property.ItemsElementName[i])
                        {
                            case ItemsChoiceType.RegularQuery:
                            case ItemsChoiceType.ExcludeQuery:
                                parameterMetadata.Attributes.Add(new ValidateNotNullAttribute());
                                break;

                            default:
                                // do nothing
                                break;
                        }

                        try
                        {
                            queryParameters.Add(parameterMetadata.Name, parameterMetadata);
                        }
                        catch (ArgumentException e)
                        {
                            string message = string.Format(
                                CultureInfo.InvariantCulture, // xml element names and parameter names are culture-agnostic
                                CmdletizationCoreResources.ScriptWriter_DuplicateQueryParameterName,
                                "<GetCmdletParameters>",
                                parameterMetadata.Name);
                            throw new XmlException(message, e);
                        }
                    }
                }
            }

            if (getCmdletParameters.QueryableAssociations != null)
            {
                foreach (Association association in getCmdletParameters.QueryableAssociations.Where(static a => a.AssociatedInstance != null))
                {
                    ParameterMetadata parameterMetadata = GenerateAssociationClause(
                        commonParameterSets, queryParameterSets, methodParameterSets, association, association.AssociatedInstance, output);
                    try
                    {
                        queryParameters.Add(parameterMetadata.Name, parameterMetadata);
                    }
                    catch (ArgumentException e)
                    {
                        string message = string.Format(
                            CultureInfo.InvariantCulture, // xml element names and parameter names are culture-agnostic
                            CmdletizationCoreResources.ScriptWriter_DuplicateQueryParameterName,
                            "<GetCmdletParameters>",
                            parameterMetadata.Name);
                        throw new XmlException(message, e);
                    }
                }
            }

            if (getCmdletParameters.QueryOptions != null)
            {
                foreach (QueryOption queryOption in getCmdletParameters.QueryOptions)
                {
                    ParameterMetadata parameterMetadata = GenerateOptionClause(
                        commonParameterSets, queryParameterSets, methodParameterSets, queryOption, output);
                    try
                    {
                        queryParameters.Add(parameterMetadata.Name, parameterMetadata);
                    }
                    catch (ArgumentException e)
                    {
                        string message = string.Format(
                            CultureInfo.InvariantCulture, // xml element names and parameter names are culture-agnostic
                            CmdletizationCoreResources.ScriptWriter_DuplicateQueryParameterName,
                            "<GetCmdletParameters>",
                            parameterMetadata.Name);
                        throw new XmlException(message, e);
                    }
                }
            }

            if (instanceCmdlet != null)
            {
                ParameterMetadata inputObjectParameter = new("InputObject", _objectInstanceType.MakeArrayType());

                ParameterSetMetadata.ParameterFlags inputObjectFlags = ParameterSetMetadata.ParameterFlags.ValueFromPipeline;
                if (queryParameters.Count > 0)
                {
                    inputObjectFlags |= ParameterSetMetadata.ParameterFlags.Mandatory;
                }

                string psTypeNameOfInputObjectElements;
                if (_objectModelWrapper.FullName.Equals("Microsoft.PowerShell.Cmdletization.Cim.CimCmdletAdapter"))
                {
                    int indexOfLastBackslash = _cmdletizationMetadata.Class.ClassName.LastIndexOf('\\');
                    int indexOfLastForwardSlash = _cmdletizationMetadata.Class.ClassName.LastIndexOf('/');
                    int indexOfLastSeparator = Math.Max(indexOfLastBackslash, indexOfLastForwardSlash);
                    string cimClassName = _cmdletizationMetadata.Class.ClassName.Substring(
                        indexOfLastSeparator + 1,
                        _cmdletizationMetadata.Class.ClassName.Length - indexOfLastSeparator - 1);
                    psTypeNameOfInputObjectElements = string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}#{1}",
                        _objectInstanceType.FullName,
                        cimClassName);
                }
                else
                {
                    psTypeNameOfInputObjectElements = string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}#{1}",
                        _objectInstanceType.FullName,
                        _cmdletizationMetadata.Class.ClassName);
                }

                inputObjectParameter.Attributes.Add(new PSTypeNameAttribute(psTypeNameOfInputObjectElements));

                inputObjectParameter.Attributes.Add(new ValidateNotNullAttribute());
                inputObjectParameter.ParameterSets.Clear();
                ParameterSetMetadata inputObjectPSet = new(
                    int.MinValue, // non-positional
                    inputObjectFlags,
                    helpMessage: null);
                inputObjectParameter.ParameterSets.Add(ScriptWriter.InputObjectQueryParameterSetName, inputObjectPSet);
                queryParameters.Add(inputObjectParameter.Name, inputObjectParameter);
            }

            output.WriteLine();
            scriptCode = output.ToString();
        }

        private const string CmdletBeginBlockTemplate = @"
function {0}
{{
    {1}
    {2}
    {3}

    param(
    {4})

    DynamicParam {{
        try
        {{
            if (-not $__cmdletization_exceptionHasBeenThrown)
            {{
                $__cmdletization_objectModelWrapper = $script:ObjectModelWrapper::new()
                $__cmdletization_objectModelWrapper.Initialize($PSCmdlet, $script:ClassName, $script:ClassVersion, $script:ModuleVersion, $script:PrivateData)

                if ($__cmdletization_objectModelWrapper -is [System.Management.Automation.IDynamicParameters])
                {{
                    ([System.Management.Automation.IDynamicParameters]$__cmdletization_objectModelWrapper).GetDynamicParameters()
                }}
            }}
        }}
        catch
        {{
            $__cmdletization_exceptionHasBeenThrown = $true
            throw
        }}
    }}

    Begin {{
        $__cmdletization_exceptionHasBeenThrown = $false
        try
        {{
            __cmdletization_BindCommonParameters $__cmdletization_objectModelWrapper $PSBoundParameters
            $__cmdletization_objectModelWrapper.BeginProcessing()
        }}
        catch
        {{
            $__cmdletization_exceptionHasBeenThrown = $true
            throw
        }}
    }}
        ";

        private const string CmdletProcessBlockTemplate = @"
    Process {{
        try
        {{
            if (-not $__cmdletization_exceptionHasBeenThrown)
            {{
{0}
            }}
        }}
        catch
        {{
            $__cmdletization_exceptionHasBeenThrown = $true
            throw
        }}
    }}
        ";

        // 0 - help file (in a help comment directive)
        private const string CmdletEndBlockTemplate = @"
    End {{
        try
        {{
            if (-not $__cmdletization_exceptionHasBeenThrown)
            {{
                $__cmdletization_objectModelWrapper.EndProcessing()
            }}
        }}
        catch
        {{
            throw
        }}
    }}

    {0}
}}
Microsoft.PowerShell.Core\Export-ModuleMember -Function '{1}' -Alias '*'
        ";

        private string GetHelpDirectiveForExternalHelp()
        {
            StringBuilder output = new();

            if ((_generationOptions & GenerationOptions.HelpXml) == GenerationOptions.HelpXml)
            {
                output.AppendFormat(
                    CultureInfo.InvariantCulture,
                    "# .EXTERNALHELP {0}.cdxml-Help.xml",
                    ScriptWriter.EscapeModuleNameForHelpComment(_moduleName));
            }

            return output.ToString();
        }

        private void WriteCmdlet(TextWriter output, StaticCmdletMetadata staticCmdlet)
        {
            string attributeString = GetCmdletAttributes(staticCmdlet.CmdletMetadata);

            Dictionary<string, ParameterMetadata> commonParameters = this.GetCommonParameters();
            List<string> commonParameterSets = GetCommonParameterSets(commonParameters);
            Dbg.Assert(commonParameterSets != null && (commonParameterSets.Count > 0),
                       "Verifying stuff returned by GetCommonParameterSets");

            Dictionary<string, ParameterMetadata> methodParameters;
            string methodProcessingScript;
            string outputTypeAttributeDeclaration;
            GenerateMethodParametersProcessing(staticCmdlet, commonParameterSets, out methodProcessingScript, out methodParameters, out outputTypeAttributeDeclaration);
            List<string> methodParameterSets = GetMethodParameterSets(staticCmdlet);

            CommandMetadata commandMetadata = this.GetCommandMetadata(staticCmdlet.CmdletMetadata);
            if (!string.IsNullOrEmpty(commandMetadata.DefaultParameterSetName))
            {
                commandMetadata.DefaultParameterSetName = string.Format(CultureInfo.InvariantCulture, StaticMethodParameterSetTemplate, commandMetadata.DefaultParameterSetName, commonParameterSets[0]);
            }

            MultiplyParameterSets(commonParameters, StaticCommonParameterSetTemplate, methodParameterSets);
            MultiplyParameterSets(methodParameters, StaticMethodParameterSetTemplate, commonParameterSets);
            EnsureOrderOfPositionalParameters(commonParameters, methodParameters);
            SetParameters(commandMetadata, methodParameters, commonParameters);

            Dbg.Assert(
                Regex.IsMatch(commandMetadata.Name, @"[\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Lm}]{1,100}-[\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Lm}\p{Nd}]{1,100}"),
                "Command name doesn't need escaping - validated via xsd");
            output.WriteLine(
                CmdletBeginBlockTemplate,
                /* 0 */ commandMetadata.Name,
                /* 1 */ ProxyCommand.GetCmdletBindingAttribute(commandMetadata),
                /* 2 */ attributeString,
                /* 3 */ outputTypeAttributeDeclaration,
                /* 4 */ ProxyCommand.GetParamBlock(commandMetadata));

            output.WriteLine(
                CmdletProcessBlockTemplate,
                methodProcessingScript);

            output.WriteLine(
                CmdletEndBlockTemplate,
                /* 0 */ this.GetHelpDirectiveForExternalHelp(),
                /* 1 */ CodeGeneration.EscapeSingleQuotedStringContent(commandMetadata.Name));
        }

        private static void AddPassThruParameter(IDictionary<string, ParameterMetadata> commonParameters, InstanceCmdletMetadata instanceCmdletMetadata)
        {
            Dbg.Assert(commonParameters != null, "Caller should verify commonParameters != null");
            Dbg.Assert(instanceCmdletMetadata != null, "Caller should verify instanceCmdletMetadata != null");

            bool outParametersArePresent = false;
            if (instanceCmdletMetadata.Method.Parameters != null)
            {
                foreach (InstanceMethodParameterMetadata parameter in instanceCmdletMetadata.Method.Parameters)
                {
                    if ((parameter.CmdletOutputMetadata != null) && (parameter.CmdletOutputMetadata.ErrorCode == null))
                    {
                        outParametersArePresent = true;
                        break;
                    }
                }
            }

            if (instanceCmdletMetadata.Method.ReturnValue != null)
            {
                if ((instanceCmdletMetadata.Method.ReturnValue.CmdletOutputMetadata != null) &&
                    (instanceCmdletMetadata.Method.ReturnValue.CmdletOutputMetadata.ErrorCode == null))
                {
                    outParametersArePresent = true;
                }
            }

            if (!outParametersArePresent)
            {
                ParameterMetadata passThruParameter = new("PassThru", typeof(SwitchParameter));
                passThruParameter.ParameterSets.Clear();
                ParameterSetMetadata passThruPSet = new(int.MinValue, 0, null);
                passThruParameter.ParameterSets.Add(ParameterAttribute.AllParameterSets, passThruPSet);

                commonParameters.Add(passThruParameter.Name, passThruParameter);
            }
        }

        private void WriteCmdlet(TextWriter output, InstanceCmdletMetadata instanceCmdlet)
        {
            string attributeString = GetCmdletAttributes(instanceCmdlet.CmdletMetadata);

            Dictionary<string, ParameterMetadata> commonParameters = this.GetCommonParameters();
            List<string> commonParameterSets = GetCommonParameterSets(commonParameters);
            List<string> methodParameterSets = GetMethodParameterSets(instanceCmdlet);
            List<string> queryParameterSets = GetQueryParameterSets(instanceCmdlet);

            Dictionary<string, ParameterMetadata> queryParameters;
            string queryProcessingScript;
            GenerateQueryParametersProcessing(instanceCmdlet, commonParameterSets, queryParameterSets, methodParameterSets, out queryProcessingScript, out queryParameters);

            Dictionary<string, ParameterMetadata> methodParameters;
            string methodProcessingScript;
            string outputTypeAttributeDeclaration;
            GenerateMethodParametersProcessing(instanceCmdlet, commonParameterSets, queryParameterSets, out methodProcessingScript, out methodParameters, out outputTypeAttributeDeclaration);

            CommandMetadata commandMetadata = this.GetCommandMetadata(instanceCmdlet.CmdletMetadata);
            GetCmdletParameters getCmdletParameters = this.GetGetCmdletParameters(instanceCmdlet);
            if (!string.IsNullOrEmpty(getCmdletParameters.DefaultCmdletParameterSet))
            {
                commandMetadata.DefaultParameterSetName = getCmdletParameters.DefaultCmdletParameterSet;
            }
            else if (queryParameterSets.Count == 1)
            {
                commandMetadata.DefaultParameterSetName = queryParameterSets[0];
            }

            AddPassThruParameter(commonParameters, instanceCmdlet);
            MultiplyParameterSets(commonParameters, InstanceCommonParameterSetTemplate, queryParameterSets, methodParameterSets);
            MultiplyParameterSets(queryParameters, InstanceQueryParameterSetTemplate, commonParameterSets, methodParameterSets);
            MultiplyParameterSets(methodParameters, InstanceMethodParameterSetTemplate, commonParameterSets, queryParameterSets);
            EnsureOrderOfPositionalParameters(commonParameters, queryParameters);
            EnsureOrderOfPositionalParameters(queryParameters, methodParameters);
            SetParameters(commandMetadata, queryParameters, methodParameters, commonParameters);

            Dbg.Assert(
                Regex.IsMatch(commandMetadata.Name, @"[\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Lm}]{1,100}-[\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Lm}\p{Nd}]{1,100}"),
                "Command name doesn't need escaping - validated via xsd");
            output.WriteLine(
                CmdletBeginBlockTemplate,
                /* 0 */ commandMetadata.Name,
                /* 1 */ ProxyCommand.GetCmdletBindingAttribute(commandMetadata),
                /* 2 */ attributeString,
                /* 3 */ outputTypeAttributeDeclaration,
                /* 4 */ ProxyCommand.GetParamBlock(commandMetadata));

            output.WriteLine(
                CmdletProcessBlockTemplate,
                queryProcessingScript + "\r\n" + methodProcessingScript);

            output.WriteLine(
                CmdletEndBlockTemplate,
                /* 0 */ this.GetHelpDirectiveForExternalHelp(),
                /* 1 */ CodeGeneration.EscapeSingleQuotedStringContent(commandMetadata.Name));
        }

        private string GetOutputAttributeForGetCmdlet()
        {
            StringBuilder result = new();
            result.AppendFormat(
                CultureInfo.InvariantCulture,
                "[OutputType([{0}])]",
                CodeGeneration.EscapeSingleQuotedStringContent(_objectInstanceType.FullName));
            result.AppendLine();
            result.AppendFormat(
                CultureInfo.InvariantCulture,
                "[OutputType('{0}#{1}')]",
                CodeGeneration.EscapeSingleQuotedStringContent(_objectInstanceType.FullName),
                CodeGeneration.EscapeSingleQuotedStringContent(_cmdletizationMetadata.Class.ClassName));
            result.AppendLine();
            return result.ToString();
        }

        private CommonCmdletMetadata GetGetCmdletMetadata()
        {
            Dbg.Assert(_cmdletizationMetadata.Class.InstanceCmdlets != null, "Caller should verify presence of instance cmdlets");
            CommonCmdletMetadata cmdletMetadata;
            if (_cmdletizationMetadata.Class.InstanceCmdlets.GetCmdlet != null)
            {
                cmdletMetadata = _cmdletizationMetadata.Class.InstanceCmdlets.GetCmdlet.CmdletMetadata;
            }
            else
            {
                cmdletMetadata = new CommonCmdletMetadata();
                cmdletMetadata.Noun = _cmdletizationMetadata.Class.DefaultNoun;
                cmdletMetadata.Verb = VerbsCommon.Get;
            }

            Dbg.Assert(cmdletMetadata != null, "xsd should ensure that cmdlet metadata element is always present");
            return cmdletMetadata;
        }

        private void WriteGetCmdlet(TextWriter output)
        {
            Dictionary<string, ParameterMetadata> commonParameters = this.GetCommonParameters();
            List<string> commonParameterSets = GetCommonParameterSets(commonParameters);
            List<string> methodParameterSets = new();
            methodParameterSets.Add(string.Empty);
            List<string> queryParameterSets = GetQueryParameterSets(null);

            Dictionary<string, ParameterMetadata> queryParameters;
            string queryProcessingScript;
            GenerateQueryParametersProcessing(null, commonParameterSets, queryParameterSets, methodParameterSets, out queryProcessingScript, out queryParameters);

            CommonCmdletMetadata cmdletMetadata = this.GetGetCmdletMetadata();
            Dbg.Assert(cmdletMetadata != null, "xsd should ensure that cmdlet metadata element is always present");
            CommandMetadata commandMetadata = this.GetCommandMetadata(cmdletMetadata);
            string attributeString = GetCmdletAttributes(cmdletMetadata);

            GetCmdletParameters getCmdletParameters = this.GetGetCmdletParameters(null);
            if (!string.IsNullOrEmpty(getCmdletParameters.DefaultCmdletParameterSet))
            {
                commandMetadata.DefaultParameterSetName = getCmdletParameters.DefaultCmdletParameterSet;
            }

            MultiplyParameterSets(commonParameters, InstanceCommonParameterSetTemplate, queryParameterSets, methodParameterSets);
            MultiplyParameterSets(queryParameters, InstanceQueryParameterSetTemplate, commonParameterSets, methodParameterSets);
            EnsureOrderOfPositionalParameters(commonParameters, queryParameters);
            SetParameters(commandMetadata, queryParameters, commonParameters);

            Dbg.Assert(
                Regex.IsMatch(commandMetadata.Name, @"[\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Lm}]{1,100}-[\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Lm}\p{Nd}]{1,100}"),
                "Command name doesn't need escaping - validated via xsd");
            output.WriteLine(
                CmdletBeginBlockTemplate,
                /* 0 */ commandMetadata.Name,
                /* 1 */ ProxyCommand.GetCmdletBindingAttribute(commandMetadata),
                /* 2 */ attributeString,
                /* 3 */ this.GetOutputAttributeForGetCmdlet(),
                /* 4 */ ProxyCommand.GetParamBlock(commandMetadata));

            output.WriteLine(
                CmdletProcessBlockTemplate,
                queryProcessingScript + "\r\n" + "    $__cmdletization_objectModelWrapper.ProcessRecord($__cmdletization_queryBuilder)");

            output.WriteLine(
                CmdletEndBlockTemplate,
                /* 0 */ this.GetHelpDirectiveForExternalHelp(),
                /* 1 */ CodeGeneration.EscapeSingleQuotedStringContent(commandMetadata.Name));
        }

        private static readonly object s_enumCompilationLock = new();

        private static void CompileEnum(EnumMetadataEnum enumMetadata)
        {
            try
            {
                string enumFullName = EnumWriter.GetEnumFullName(enumMetadata);

                lock (s_enumCompilationLock)
                {
                    Type alreadyExistingType;
                    if (!LanguagePrimitives.TryConvertTo(enumFullName, CultureInfo.InvariantCulture, out alreadyExistingType))
                    {
                        EnumWriter.Compile(enumMetadata);
                    }
                }
            }
            catch (Exception e)
            {
                var errorMessage = string.Format(
                    CultureInfo.InvariantCulture,
                    CmdletizationCoreResources.ScriptWriter_InvalidEnum,
                    enumMetadata.EnumName,
                    e.Message);
                throw new XmlException(errorMessage, e);
            }
        }

        internal void WriteScriptModule(TextWriter output)
        {
            this.WriteModulePreamble(output);
            this.WriteBindCommonParametersFunction(output);

            if (_cmdletizationMetadata.Enums != null)
            {
                foreach (EnumMetadataEnum enumMetadata in _cmdletizationMetadata.Enums)
                {
                    CompileEnum(enumMetadata);
                }
            }

            if (_cmdletizationMetadata.Class.StaticCmdlets != null)
            {
                foreach (StaticCmdletMetadata staticCmdlet in _cmdletizationMetadata.Class.StaticCmdlets)
                {
                    this.WriteCmdlet(output, staticCmdlet);
                }
            }

            if (_cmdletizationMetadata.Class.InstanceCmdlets != null)
            {
                this.WriteGetCmdlet(output);
                if (_cmdletizationMetadata.Class.InstanceCmdlets.Cmdlet != null)
                {
                    foreach (InstanceCmdletMetadata instanceCmdlet in _cmdletizationMetadata.Class.InstanceCmdlets.Cmdlet)
                    {
                        this.WriteCmdlet(output, instanceCmdlet);
                    }
                }
            }
        }

        #endregion psm1

        #region PSModuleInfo

        internal const string PrivateDataKey_CmdletsOverObjects = "CmdletsOverObjects";
        internal const string PrivateDataKey_ClassName = "ClassName";
        internal const string PrivateDataKey_ObjectModelWrapper = "CmdletAdapter";
        internal const string PrivateDataKey_DefaultSession = "DefaultSession";

        internal void PopulatePSModuleInfo(PSModuleInfo moduleInfo)
        {
            moduleInfo.SetModuleType(ModuleType.Cim);
            moduleInfo.SetVersion(new Version(_cmdletizationMetadata.Class.Version));

            Hashtable cmdletizationData = new(StringComparer.OrdinalIgnoreCase);
            cmdletizationData.Add(PrivateDataKey_ClassName, _cmdletizationMetadata.Class.ClassName);
            cmdletizationData.Add(PrivateDataKey_ObjectModelWrapper, _objectModelWrapper);

            Hashtable privateData = new(StringComparer.OrdinalIgnoreCase);
            privateData.Add(PrivateDataKey_CmdletsOverObjects, cmdletizationData);
            moduleInfo.PrivateData = privateData;
        }

        internal void ReportExportedCommands(PSModuleInfo moduleInfo, string prefix)
        {
            if (moduleInfo.ExportedCommands.Count != 0)
            {
                return;
            }

            moduleInfo.DeclaredAliasExports = new Collection<string>();
            moduleInfo.DeclaredFunctionExports = new Collection<string>();

            IEnumerable<CommonCmdletMetadata> cmdletMetadatas = Enumerable.Empty<CommonCmdletMetadata>();
            if (_cmdletizationMetadata.Class.InstanceCmdlets != null)
            {
                cmdletMetadatas = cmdletMetadatas.Append(this.GetGetCmdletMetadata());
                if (_cmdletizationMetadata.Class.InstanceCmdlets.Cmdlet != null)
                {
                    cmdletMetadatas =
                        cmdletMetadatas.Concat(
                            _cmdletizationMetadata.Class.InstanceCmdlets.Cmdlet.Select(static c => c.CmdletMetadata));
                }
            }

            if (_cmdletizationMetadata.Class.StaticCmdlets != null)
            {
                cmdletMetadatas =
                    cmdletMetadatas.Concat(
                        _cmdletizationMetadata.Class.StaticCmdlets.Select(static c => c.CmdletMetadata));
            }

            foreach (CommonCmdletMetadata cmdletMetadata in cmdletMetadatas)
            {
                if (cmdletMetadata.Aliases != null)
                {
                    foreach (string alias in cmdletMetadata.Aliases)
                    {
                        moduleInfo.DeclaredAliasExports.Add(ModuleCmdletBase.AddPrefixToCommandName(alias, prefix));
                    }
                }

                CommandMetadata commandMetadata = this.GetCommandMetadata(cmdletMetadata);
                moduleInfo.DeclaredFunctionExports.Add(ModuleCmdletBase.AddPrefixToCommandName(commandMetadata.Name, prefix));
            }
        }

        #endregion PSModuleInfo
    }
}
