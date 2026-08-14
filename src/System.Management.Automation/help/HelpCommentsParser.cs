// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation.Help;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace System.Management.Automation
{
    /// <summary>
    /// Parses help comments and turns them into HelpInfo objects.
    /// </summary>
    internal sealed class HelpCommentsParser
    {
        private HelpCommentsParser()
        {
        }

        private HelpCommentsParser(List<string> parameterDescriptions)
        {
            _parameterDescriptions = parameterDescriptions;
        }

        private HelpCommentsParser(CommandInfo commandInfo, List<string> parameterDescriptions)
        {
            FunctionInfo fi = commandInfo as FunctionInfo;
            if (fi != null)
            {
                _scriptBlock = fi.ScriptBlock;
                _commandName = fi.Name;
            }
            else
            {
                ExternalScriptInfo si = commandInfo as ExternalScriptInfo;
                if (si != null)
                {
                    _scriptBlock = si.ScriptBlock;
                    _commandName = si.Path;
                }
            }

            _commandMetadata = commandInfo.CommandMetadata;
            _parameterDescriptions = parameterDescriptions;
        }

        private readonly Language.CommentHelpInfo _sections = new Language.CommentHelpInfo();
        private readonly Dictionary<string, string> _parameters = new Dictionary<string, string>();
        private readonly List<string> _examples = new List<string>();
        private readonly List<string> _inputs = new List<string>();
        private readonly List<string> _outputs = new List<string>();
        private readonly List<string> _links = new List<string>();
        internal bool isExternalHelpSet = false;

        private readonly ScriptBlock _scriptBlock;
        private readonly CommandMetadata _commandMetadata;
        private readonly string _commandName;
        private readonly List<string> _parameterDescriptions;
        private XmlDocument _doc;
        internal static readonly string mshURI = "http://msh";
        internal static readonly string mamlURI = "http://schemas.microsoft.com/maml/2004/10";
        internal static readonly string commandURI = "http://schemas.microsoft.com/maml/dev/command/2004/10";
        internal static readonly string devURI = "http://schemas.microsoft.com/maml/dev/2004/10";

        private const string directive = @"^\s*\.(\w+)(\s+(\S.*))?\s*$";
        private const string blankline = @"^\s*$";
        // Although "http://msh" is the default namespace, it still must be explicitly qualified with non-empty prefix,
        // because XPath 1.0 will associate empty prefix with "null" namespace (not with "default") and query will fail.
        // See: http://www.w3.org/TR/1999/REC-xpath-19991116/#node-tests
        internal static readonly string ProviderHelpCommandXPath =
            "/msh:helpItems/msh:providerHelp/msh:CmdletHelpPaths/msh:CmdletHelpPath{0}/command:command[command:details/command:verb='{1}' and command:details/command:noun='{2}']";

        private void DetermineParameterDescriptions()
        {
            int i = 0;
            foreach (string parameterName in _commandMetadata.StaticCommandParameterMetadata.BindableParameters.Keys)
            {
                string description;
                if (!_parameters.TryGetValue(parameterName.ToUpperInvariant(), out description))
                {
                    if (i < _parameterDescriptions.Count)
                    {
                        _parameters.Add(parameterName.ToUpperInvariant(), _parameterDescriptions[i]);
                    }
                }

                ++i;
            }
        }

        private string GetParameterDescription(string parameterName)
        {
            Diagnostics.Assert(!string.IsNullOrEmpty(parameterName), "Parameter name must not be empty");

            string description;
            _parameters.TryGetValue(parameterName.ToUpperInvariant(), out description);
            return description;
        }

        private XmlElement BuildXmlForParameter(
            string parameterName,
            bool isMandatory,
            bool valueFromPipeline,
            bool valueFromPipelineByPropertyName,
            string position,
            Type type,
            string description,
            bool supportsWildcards,
            string defaultValue,
            bool forSyntax)
        {
            XmlElement command_parameter = _doc.CreateElement("command:parameter", commandURI);
            command_parameter.SetAttribute("required", isMandatory ? "true" : "false");
            // command_parameter.SetAttribute("variableLength", "unknown");
            command_parameter.SetAttribute("globbing", supportsWildcards ? "true" : "false");
            string fromPipeline;
            if (valueFromPipeline && valueFromPipelineByPropertyName)
            {
                fromPipeline = "true (ByValue, ByPropertyName)";
            }
            else if (valueFromPipeline)
            {
                fromPipeline = "true (ByValue)";
            }
            else if (valueFromPipelineByPropertyName)
            {
                fromPipeline = "true (ByPropertyName)";
            }
            else
            {
                fromPipeline = "false";
            }

            command_parameter.SetAttribute("pipelineInput", fromPipeline);
            command_parameter.SetAttribute("position", position);

            XmlElement name = _doc.CreateElement("maml:name", mamlURI);
            XmlText name_text = _doc.CreateTextNode(parameterName);
            command_parameter.AppendChild(name).AppendChild(name_text);
            if (!string.IsNullOrEmpty(description))
            {
                XmlElement maml_description = _doc.CreateElement("maml:description", mamlURI);
                XmlElement maml_para = _doc.CreateElement("maml:para", mamlURI);
                XmlText maml_para_text = _doc.CreateTextNode(description);
                command_parameter.AppendChild(maml_description).AppendChild(maml_para).AppendChild(maml_para_text);
            }

            if (type == null)
                type = typeof(object);

            var elementType = type.IsArray ? type.GetElementType() : type;

            if (elementType.IsEnum)
            {
                XmlElement parameterValueGroup = _doc.CreateElement("command:parameterValueGroup", commandURI);
                foreach (string valueName in Enum.GetNames(elementType))
                {
                    XmlElement parameterValue = _doc.CreateElement("command:parameterValue", commandURI);
                    parameterValue.SetAttribute("required", "false");
                    XmlText parameterValue_text = _doc.CreateTextNode(valueName);
                    parameterValueGroup.AppendChild(parameterValue).AppendChild(parameterValue_text);
                }

                command_parameter.AppendChild(parameterValueGroup);
            }
            else
            {
                bool isSwitchParameter = elementType == typeof(SwitchParameter);
                if (!forSyntax || !isSwitchParameter)
                {
                    XmlElement parameterValue = _doc.CreateElement("command:parameterValue", commandURI);
                    parameterValue.SetAttribute("required", isSwitchParameter ? "false" : "true");
                    // parameterValue.SetAttribute("variableLength", "unknown");
                    XmlText parameterValue_text = _doc.CreateTextNode(type.Name);
                    command_parameter.AppendChild(parameterValue).AppendChild(parameterValue_text);
                }
            }

            if (!forSyntax)
            {
                XmlElement devType = _doc.CreateElement("dev:type", devURI);
                XmlElement typeName = _doc.CreateElement("maml:name", mamlURI);
                XmlText typeName_text = _doc.CreateTextNode(type.Name);
                command_parameter.AppendChild(devType).AppendChild(typeName).AppendChild(typeName_text);

                XmlElement defaultValueElement = _doc.CreateElement("dev:defaultValue", devURI);
                XmlText defaultValue_text = _doc.CreateTextNode(defaultValue);
                command_parameter.AppendChild(defaultValueElement).AppendChild(defaultValue_text);
            }

            return command_parameter;
        }

        /// <summary>
        /// Create the maml xml after a successful analysis of the comments.
        /// </summary>
        /// <returns>The xml node for the command constructed.</returns>
        internal XmlDocument BuildXmlFromComments()
        {
            Diagnostics.Assert(!string.IsNullOrEmpty(_commandName), "Name can never be null");

            _doc = new XmlDocument();
            XmlElement command = _doc.CreateElement("command:command", commandURI);
            command.SetAttribute("xmlns:maml", mamlURI);
            command.SetAttribute("xmlns:command", commandURI);
            command.SetAttribute("xmlns:dev", devURI);
            _doc.AppendChild(command);

            XmlElement details = _doc.CreateElement("command:details", commandURI);
            command.AppendChild(details);

            XmlElement name = _doc.CreateElement("command:name", commandURI);
            XmlText name_text = _doc.CreateTextNode(_commandName);
            details.AppendChild(name).AppendChild(name_text);

            if (!string.IsNullOrEmpty(_sections.Synopsis))
            {
                XmlElement synopsis = _doc.CreateElement("maml:description", mamlURI);
                XmlElement synopsis_para = _doc.CreateElement("maml:para", mamlURI);
                XmlText synopsis_text = _doc.CreateTextNode(_sections.Synopsis);
                details.AppendChild(synopsis).AppendChild(synopsis_para).AppendChild(synopsis_text);
            }

            #region Syntax

            // The syntax is automatically generated from parameter metadata
            DetermineParameterDescriptions();

            XmlElement syntax = _doc.CreateElement("command:syntax", commandURI);
            MergedCommandParameterMetadata parameterMetadata = _commandMetadata.StaticCommandParameterMetadata;
            if (parameterMetadata.ParameterSetCount > 0)
            {
                for (int i = 0; i < parameterMetadata.ParameterSetCount; ++i)
                {
                    BuildSyntaxForParameterSet(command, syntax, parameterMetadata, i);
                }
            }
            else
            {
                BuildSyntaxForParameterSet(command, syntax, parameterMetadata, int.MaxValue);
            }

            #endregion Syntax

            #region Parameters

            XmlElement commandParameters = _doc.CreateElement("command:parameters", commandURI);
            foreach (KeyValuePair<string, MergedCompiledCommandParameter> pair in parameterMetadata.BindableParameters)
            {
                MergedCompiledCommandParameter mergedParameter = pair.Value;
                if (mergedParameter.BinderAssociation == ParameterBinderAssociation.CommonParameters)
                {
                    continue;
                }

                string parameterName = pair.Key;
                string description = GetParameterDescription(parameterName);

                ParameterSetSpecificMetadata parameterSetData;
                bool isMandatory = false;
                bool valueFromPipeline = false;
                bool valueFromPipelineByPropertyName = false;
                string position = "named";
                int i = 0;

                CompiledCommandParameter parameter = mergedParameter.Parameter;
                parameter.ParameterSetData.TryGetValue(ParameterAttribute.AllParameterSets, out parameterSetData);
                while (parameterSetData == null && i < 32)
                {
                    parameterSetData = parameter.GetParameterSetData(1u << i++);
                }

                if (parameterSetData != null)
                {
                    isMandatory = parameterSetData.IsMandatory;
                    valueFromPipeline = parameterSetData.ValueFromPipeline;
                    valueFromPipelineByPropertyName = parameterSetData.ValueFromPipelineByPropertyName;
                    position = parameterSetData.IsPositional ? (1 + parameterSetData.Position).ToString(CultureInfo.InvariantCulture) : "named";
                }

                var compiledAttributes = parameter.CompiledAttributes;
                bool supportsWildcards = compiledAttributes.OfType<SupportsWildcardsAttribute>().Any();

                string defaultValueStr = string.Empty;
                object defaultValue = null;
                var defaultValueAttribute = compiledAttributes.OfType<PSDefaultValueAttribute>().FirstOrDefault();
                if (defaultValueAttribute != null)
                {
                    defaultValueStr = defaultValueAttribute.Help;
                    if (string.IsNullOrEmpty(defaultValueStr))
                    {
                        defaultValue = defaultValueAttribute.Value;
                    }
                }

                if (string.IsNullOrEmpty(defaultValueStr))
                {
                    if (defaultValue == null)
                    {
                        RuntimeDefinedParameter rdp;
                        if (_scriptBlock.RuntimeDefinedParameters.TryGetValue(parameterName, out rdp))
                        {
                            defaultValue = rdp.Value;
                        }
                    }

                    var wrapper = defaultValue as Compiler.DefaultValueExpressionWrapper;
                    if (wrapper != null)
                    {
                        defaultValueStr = wrapper.Expression.Extent.Text;
                    }
                    else if (defaultValue != null)
                    {
                        defaultValueStr = PSObject.ToStringParser(null, defaultValue);
                    }
                }

                XmlElement parameterElement = BuildXmlForParameter(parameterName, isMandatory,
                    valueFromPipeline, valueFromPipelineByPropertyName, position,
                    parameter.Type, description, supportsWildcards, defaultValueStr, forSyntax: false);
                commandParameters.AppendChild(parameterElement);
            }

            command.AppendChild(commandParameters);

            #endregion Parameters

            if (!string.IsNullOrEmpty(_sections.Description))
            {
                XmlElement description = _doc.CreateElement("maml:description", mamlURI);
                XmlElement description_para = _doc.CreateElement("maml:para", mamlURI);
                XmlText description_text = _doc.CreateTextNode(_sections.Description);
                command.AppendChild(description).AppendChild(description_para).AppendChild(description_text);
            }

            if (!string.IsNullOrEmpty(_sections.Notes))
            {
                XmlElement alertSet = _doc.CreateElement("maml:alertSet", mamlURI);
                XmlElement alert = _doc.CreateElement("maml:alert", mamlURI);
                XmlElement alert_para = _doc.CreateElement("maml:para", mamlURI);
                XmlText alert_para_text = _doc.CreateTextNode(_sections.Notes);
                command.AppendChild(alertSet).AppendChild(alert).AppendChild(alert_para).AppendChild(alert_para_text);
            }

            if (_examples.Count > 0)
            {
                XmlElement examples = _doc.CreateElement("command:examples", commandURI);
                int count = 1;
                foreach (string example in _examples)
                {
                    XmlElement example_node = _doc.CreateElement("command:example", commandURI);

                    // The title is automatically generated
                    XmlElement title = _doc.CreateElement("maml:title", mamlURI);
                    string titleStr = string.Format(CultureInfo.InvariantCulture,
                        "\t\t\t\t-------------------------- {0} {1} --------------------------",
                        HelpDisplayStrings.ExampleUpperCase, count++);
                    XmlText title_text = _doc.CreateTextNode(titleStr);
                    example_node.AppendChild(title).AppendChild(title_text);

                    string prompt_str;
                    string code_str;
                    string remarks_str;
                    GetExampleSections(example, out prompt_str, out code_str, out remarks_str);

                    // Introduction (usually the prompt)
                    XmlElement introduction = _doc.CreateElement("maml:introduction", mamlURI);
                    XmlElement introduction_para = _doc.CreateElement("maml:para", mamlURI);
                    XmlText introduction_para_text = _doc.CreateTextNode(prompt_str);
                    example_node.AppendChild(introduction).AppendChild(introduction_para).AppendChild(introduction_para_text);

                    // Example code
                    XmlElement code = _doc.CreateElement("dev:code", devURI);
                    XmlText code_text = _doc.CreateTextNode(code_str);
                    example_node.AppendChild(code).AppendChild(code_text);

                    // Remarks are comments on the example
                    XmlElement remarks = _doc.CreateElement("dev:remarks", devURI);
                    XmlElement remarks_para = _doc.CreateElement("maml:para", mamlURI);
                    XmlText remarks_para_text = _doc.CreateTextNode(remarks_str);
                    example_node.AppendChild(remarks).AppendChild(remarks_para).AppendChild(remarks_para_text);
                    // The convention is to have 4 blank paras after the example for spacing
                    for (int i = 0; i < 4; i++)
                    {
                        remarks.AppendChild(_doc.CreateElement("maml:para", mamlURI));
                    }

                    examples.AppendChild(example_node);
                }

                command.AppendChild(examples);
            }

            if (_inputs.Count > 0)
            {
                XmlElement inputTypes = _doc.CreateElement("command:inputTypes", commandURI);
                foreach (string inputStr in _inputs)
                {
                    XmlElement inputType = _doc.CreateElement("command:inputType", commandURI);
                    XmlElement type = _doc.CreateElement("dev:type", devURI);
                    XmlElement maml_name = _doc.CreateElement("maml:name", mamlURI);
                    XmlText maml_name_text = _doc.CreateTextNode(inputStr);
                    inputTypes.AppendChild(inputType).AppendChild(type).AppendChild(maml_name).AppendChild(maml_name_text);
                }

                command.AppendChild(inputTypes);
            }
            // For outputs, we prefer what was specified in the comments, but if there are no comments
            // and the OutputType attribute was specified, we'll use those instead.
            IEnumerable outputs = null;
            if (_outputs.Count > 0)
            {
                outputs = _outputs;
            }
            else if (_scriptBlock.OutputType.Count > 0)
            {
                outputs = _scriptBlock.OutputType;
            }

            if (outputs != null)
            {
                XmlElement returnValues = _doc.CreateElement("command:returnValues", commandURI);
                foreach (object output in outputs)
                {
                    XmlElement returnValue = _doc.CreateElement("command:returnValue", commandURI);
                    XmlElement type = _doc.CreateElement("dev:type", devURI);
                    XmlElement maml_name = _doc.CreateElement("maml:name", mamlURI);
                    string returnValueStr = output as string ?? ((PSTypeName)output).Name;
                    XmlText maml_name_text = _doc.CreateTextNode(returnValueStr);
                    returnValues.AppendChild(returnValue).AppendChild(type).AppendChild(maml_name).AppendChild(maml_name_text);
                }

                command.AppendChild(returnValues);
            }

            if (_links.Count > 0)
            {
                XmlElement links = _doc.CreateElement("maml:relatedLinks", mamlURI);
                foreach (string link in _links)
                {
                    XmlElement navigationLink = _doc.CreateElement("maml:navigationLink", mamlURI);
                    bool isOnlineHelp = Uri.IsWellFormedUriString(link, UriKind.Absolute);
                    string nodeName = isOnlineHelp ? "maml:uri" : "maml:linkText";
                    XmlElement linkText = _doc.CreateElement(nodeName, mamlURI);
                    XmlText linkText_text = _doc.CreateTextNode(link);
                    links.AppendChild(navigationLink).AppendChild(linkText).AppendChild(linkText_text);
                }

                command.AppendChild(links);
            }

            return _doc;
        }

        private void BuildSyntaxForParameterSet(XmlElement command, XmlElement syntax, MergedCommandParameterMetadata parameterMetadata, int i)
        {
            XmlElement syntaxItem = _doc.CreateElement("command:syntaxItem", commandURI);
            XmlElement syntaxItemName = _doc.CreateElement("maml:name", mamlURI);
            XmlText syntaxItemName_text = _doc.CreateTextNode(_commandName);

            syntaxItem.AppendChild(syntaxItemName).AppendChild(syntaxItemName_text);

            Collection<MergedCompiledCommandParameter> compiledParameters =
                parameterMetadata.GetParametersInParameterSet(1u << i);

            foreach (MergedCompiledCommandParameter mergedParameter in compiledParameters)
            {
                if (mergedParameter.BinderAssociation == ParameterBinderAssociation.CommonParameters)
                {
                    continue;
                }

                CompiledCommandParameter parameter = mergedParameter.Parameter;
                ParameterSetSpecificMetadata parameterSetData = parameter.GetParameterSetData(1u << i);
                string description = GetParameterDescription(parameter.Name);
                bool supportsWildcards = parameter.CompiledAttributes.Any(static attribute => attribute is SupportsWildcardsAttribute);
                XmlElement parameterElement = BuildXmlForParameter(parameter.Name,
                    parameterSetData.IsMandatory, parameterSetData.ValueFromPipeline,
                    parameterSetData.ValueFromPipelineByPropertyName,
                    parameterSetData.IsPositional ? (1 + parameterSetData.Position).ToString(CultureInfo.InvariantCulture) : "named",
                    parameter.Type, description, supportsWildcards, defaultValue: string.Empty, forSyntax: true);
                syntaxItem.AppendChild(parameterElement);
            }

            command.AppendChild(syntax).AppendChild(syntaxItem);
        }

        private static void GetExampleSections(string content, out string prompt_str, out string code_str, out string remarks_str)
        {
            const string default_prompt_str = "PS > ";

            var promptMatch = Regex.Match(content, "^.*?>");
            prompt_str = promptMatch.Success ? promptMatch.Value : default_prompt_str;
            if (promptMatch.Success)
            {
                content = content.Substring(prompt_str.Length);
            }

            var codeAndRemarksMatch = Regex.Match(content, "^(?<code>.*?)\r?\n\r?\n(?<remarks>.*)$", RegexOptions.Singleline);
            if (codeAndRemarksMatch.Success)
            {
                code_str = codeAndRemarksMatch.Groups["code"].Value.Trim();
                remarks_str = codeAndRemarksMatch.Groups["remarks"].Value;
            }
            else
            {
                code_str = content.Trim();
                remarks_str = string.Empty;
            }
        }

        /// <summary>
        /// Split the text in the comment token into multiple lines, appending commentLines.
        /// </summary>
        /// <param name="comment">A single line or multiline comment token.</param>
        /// <param name="commentLines"></param>
        private static void CollectCommentText(Token comment, List<string> commentLines)
        {
            string text = comment.Text;
            CollectCommentText(text, commentLines);
        }

        private static void CollectCommentText(string text, List<string> commentLines)
        {
            int i = 0;
            if (text[0] == '<')
            {
                int start = 2;
                // The full text includes '<#', so start at index 2 to skip those characters,
                // and the full text also includes '#>' at the end, so skip those as well.
                for (i = 2; i < text.Length - 2; i++)
                {
                    if (text[i] == '\n')
                    {
                        commentLines.Add(text.Substring(start, i - start));
                        start = i + 1;
                    }
                    else if (text[i] == '\r')
                    {
                        commentLines.Add(text.Substring(start, i - start));

                        // No need to check length here, comment text has at least '#>' at the end.
                        if (text[i + 1] == '\n')
                        {
                            i++;
                        }

                        start = i + 1;
                    }
                }

                commentLines.Add(text.Substring(start, i - start));
            }
            else
            {
                for (; i < text.Length; i++)
                {
                    // Skip all leading '#' characters as it is a common convention
                    // to use more than one '#' character.
                    if (text[i] != '#')
                    {
                        break;
                    }
                }

                commentLines.Add(text.Substring(i));
            }
        }

        /// <summary>
        /// Collect the text of a section.  Stop collecting the section
        /// when a new directive is found (even if it is an unknown directive).
        /// </summary>
        /// <param name="commentLines">The comment block, as a list of lines.</param>
        /// <param name="i"></param>
        /// <returns>The text of the help section, with 'i' left on the last line collected.</returns>
        private static string GetSection(List<string> commentLines, ref int i)
        {
            bool capturing = false;
            int countLeadingWS = 0;
            StringBuilder sb = new StringBuilder();
            const char nbsp = (char)0xA0;

            for (i++; i < commentLines.Count; i++)
            {
                string line = commentLines[i];
                if (!capturing && Regex.IsMatch(line, blankline))
                {
                    // Skip blank lines before capturing anything in the section.
                    continue;
                }

                if (Regex.IsMatch(line, directive))
                {
                    // Break on any directive even if we haven't started capturing.
                    i--;
                    break;
                }

                // The first line of a section sets how much whitespace we'll ignore (and hence strip).
                if (!capturing)
                {
                    int j = 0;
                    while (j < line.Length && (line[j] == ' ' || line[j] == '\t' || line[j] == nbsp))
                    {
                        countLeadingWS++;
                        j++;
                    }
                }

                capturing = true;

                // Skip leading whitespace based on the first line in the section, skipping
                // only as much whitespace as the first line had, no more (and possibly less.)
                int start = 0;
                while (start < line.Length && start < countLeadingWS &&
                       (line[start] == ' ' || line[start] == '\t' || line[start] == nbsp))
                {
                    start++;
                }

                sb.Append(line.AsSpan(start));
                sb.Append('\n');
            }

            return sb.ToString();
        }

        internal string GetHelpFile(CommandInfo commandInfo)
        {
            if (_sections.MamlHelpFile == null)
            {
                return null;
            }

            string helpFileToLoad = _sections.MamlHelpFile;
            Collection<string> searchPaths = new Collection<string>();
            string scriptFile = ((IScriptCommandInfo)commandInfo).ScriptBlock.File;
            if (!string.IsNullOrEmpty(scriptFile))
            {
                helpFileToLoad = Path.Combine(Path.GetDirectoryName(scriptFile), _sections.MamlHelpFile);
            }
            else if (commandInfo.Module != null)
            {
                helpFileToLoad = Path.Combine(Path.GetDirectoryName(commandInfo.Module.Path), _sections.MamlHelpFile);
            }

            string location = MUIFileSearcher.LocateFile(helpFileToLoad, searchPaths);

            return location;
        }

        internal RemoteHelpInfo GetRemoteHelpInfo(ExecutionContext context, CommandInfo commandInfo)
        {
            if (string.IsNullOrEmpty(_sections.ForwardHelpTargetName) || string.IsNullOrEmpty(_sections.RemoteHelpRunspace))
            {
                return null;
            }

            // get the PSSession object from the variable specified in the comments
            IScriptCommandInfo scriptCommandInfo = (IScriptCommandInfo)commandInfo;
            SessionState sessionState = scriptCommandInfo.ScriptBlock.SessionState;
            object runspaceInfoAsObject = sessionState.PSVariable.GetValue(_sections.RemoteHelpRunspace);
            PSSession runspaceInfo;
            if (runspaceInfoAsObject == null ||
                !LanguagePrimitives.TryConvertTo(runspaceInfoAsObject, out runspaceInfo))
            {
                string errorMessage = HelpErrors.RemoteRunspaceNotAvailable;
                throw new InvalidOperationException(errorMessage);
            }

            return new RemoteHelpInfo(
                context,
                (RemoteRunspace)runspaceInfo.Runspace,
                commandInfo.Name,
                _sections.ForwardHelpTargetName,
                _sections.ForwardHelpCategory,
                commandInfo.HelpCategory);
        }

        /// <summary>
        /// Look for special comments indicating the comment block is meant
        /// to be used for help.
        /// </summary>
        /// <param name="comments">The list of comments to process.</param>
        /// <returns>True if any special comments are found, false otherwise.</returns>
        internal bool AnalyzeCommentBlock(List<Token> comments)
        {
            if (comments == null || comments.Count == 0)
            {
                return false;
            }

            List<string> commentLines = new List<string>();
            foreach (Token comment in comments)
            {
                CollectCommentText(comment, commentLines);
            }

            return AnalyzeCommentBlock(commentLines);
        }

        private bool AnalyzeCommentBlock(List<string> commentLines)
        {
            bool directiveFound = false;
            for (int i = 0; i < commentLines.Count; i++)
            {
                Match match = Regex.Match(commentLines[i], directive);
                if (match.Success)
                {
                    directiveFound = true;

                    if (match.Groups[3].Success)
                    {
                        switch (match.Groups[1].Value.ToUpperInvariant())
                        {
                            case "PARAMETER":
                                {
                                    string param = match.Groups[3].Value.ToUpperInvariant().Trim();
                                    string section = GetSection(commentLines, ref i);
                                    if (!_parameters.ContainsKey(param))
                                    {
                                        _parameters.Add(param, section);
                                    }

                                    break;
                                }
                            case "FORWARDHELPTARGETNAME":
                                _sections.ForwardHelpTargetName = match.Groups[3].Value.Trim();
                                break;
                            case "FORWARDHELPCATEGORY":
                                _sections.ForwardHelpCategory = match.Groups[3].Value.Trim();
                                break;
                            case "REMOTEHELPRUNSPACE":
                                _sections.RemoteHelpRunspace = match.Groups[3].Value.Trim();
                                break;
                            case "EXTERNALHELP":
                                _sections.MamlHelpFile = match.Groups[3].Value.Trim();
                                isExternalHelpSet = true;
                                break;
                            default:
                                return false;
                        }
                    }
                    else
                    {
                        switch (match.Groups[1].Value.ToUpperInvariant())
                        {
                            case "SYNOPSIS":
                                _sections.Synopsis = GetSection(commentLines, ref i);
                                break;
                            case "DESCRIPTION":
                                _sections.Description = GetSection(commentLines, ref i);
                                break;
                            case "NOTES":
                                _sections.Notes = GetSection(commentLines, ref i);
                                break;
                            case "LINK":
                                _links.Add(GetSection(commentLines, ref i).Trim());
                                break;
                            case "EXAMPLE":
                                _examples.Add(GetSection(commentLines, ref i));
                                break;
                            case "INPUTS":
                                _inputs.Add(GetSection(commentLines, ref i));
                                break;
                            case "OUTPUTS":
                                _outputs.Add(GetSection(commentLines, ref i));
                                break;
                            case "COMPONENT":
                                _sections.Component = GetSection(commentLines, ref i).Trim();
                                break;
                            case "ROLE":
                                _sections.Role = GetSection(commentLines, ref i).Trim();
                                break;
                            case "FUNCTIONALITY":
                                _sections.Functionality = GetSection(commentLines, ref i).Trim();
                                break;
                            default:
                                return false;
                        }
                    }
                }
                else if (!Regex.IsMatch(commentLines[i], blankline))
                {
                    return false;
                }
            }

            _sections.Examples = new ReadOnlyCollection<string>(_examples);
            _sections.Inputs = new ReadOnlyCollection<string>(_inputs);
            _sections.Outputs = new ReadOnlyCollection<string>(_outputs);
            _sections.Links = new ReadOnlyCollection<string>(_links);
            // TODO, Changing this to an IDictionary because ReadOnlyDictionary is available only in .NET 4.5
            // This is a temporary workaround and will be fixed later. Tracked by Win8: 354135
            _sections.Parameters = new Dictionary<string, string>(_parameters);

            return directiveFound;
        }

        /// <summary>
        /// The analysis of the comments finds the component, functionality, and role fields, but
        /// those fields aren't added to the xml because they aren't children of the command xml
        /// node, they are under a sibling of the command xml node and apply to all command nodes
        /// in a maml file.
        /// </summary>
        /// <param name="helpInfo">The helpInfo object to set the fields on.</param>
        internal void SetAdditionalData(MamlCommandHelpInfo helpInfo)
        {
            helpInfo.SetAdditionalDataFromHelpComment(
                _sections.Component,
                _sections.Functionality,
                _sections.Role);
        }

        internal static CommentHelpInfo GetHelpContents(List<Language.Token> comments, List<string> parameterDescriptions)
        {
            HelpCommentsParser helpCommentsParser = new HelpCommentsParser(parameterDescriptions);
            helpCommentsParser.AnalyzeCommentBlock(comments);
            return helpCommentsParser._sections;
        }

        internal static HelpInfo CreateFromComments(ExecutionContext context,
                                                    CommandInfo commandInfo,
                                                    List<Language.Token> comments,
                                                    List<string> parameterDescriptions,
                                                    bool dontSearchOnRemoteComputer,
                                                    out string helpFile, out string helpUriFromDotLink)
        {
            HelpCommentsParser helpCommentsParser = new HelpCommentsParser(commandInfo, parameterDescriptions);
            helpCommentsParser.AnalyzeCommentBlock(comments);

            if (helpCommentsParser._sections.Links != null && helpCommentsParser._sections.Links.Count != 0)
            {
                helpUriFromDotLink = helpCommentsParser._sections.Links[0];
            }
            else
            {
                helpUriFromDotLink = null;
            }

            helpFile = helpCommentsParser.GetHelpFile(commandInfo);

            // If only .ExternalHelp is defined and the help file is not found, then we
            // use the metadata driven help
            if (comments.Count == 1 && helpCommentsParser.isExternalHelpSet && helpFile == null)
            {
                return null;
            }

            return CreateFromComments(context, commandInfo, helpCommentsParser, dontSearchOnRemoteComputer);
        }

        internal static HelpInfo CreateFromComments(ExecutionContext context, CommandInfo commandInfo, HelpCommentsParser helpCommentsParser,
            bool dontSearchOnRemoteComputer)
        {
            if (!dontSearchOnRemoteComputer)
            {
                RemoteHelpInfo remoteHelpInfo = helpCommentsParser.GetRemoteHelpInfo(context, commandInfo);
                if (remoteHelpInfo != null)
                {
                    // Add HelpUri if necessary
                    if (remoteHelpInfo.GetUriForOnlineHelp() == null)
                    {
                        DefaultCommandHelpObjectBuilder.AddRelatedLinksProperties(remoteHelpInfo.FullHelp,
                                                                                  commandInfo.CommandMetadata.HelpUri);
                    }

                    return remoteHelpInfo;
                }
            }

            XmlDocument doc = helpCommentsParser.BuildXmlFromComments();
            HelpCategory helpCategory = commandInfo.HelpCategory;
            MamlCommandHelpInfo localHelpInfo = MamlCommandHelpInfo.Load(doc.DocumentElement, helpCategory);
            if (localHelpInfo != null)
            {
                helpCommentsParser.SetAdditionalData(localHelpInfo);

                if (!string.IsNullOrEmpty(helpCommentsParser._sections.ForwardHelpTargetName)
                    || !string.IsNullOrEmpty(helpCommentsParser._sections.ForwardHelpCategory))
                {
                    if (string.IsNullOrEmpty(helpCommentsParser._sections.ForwardHelpTargetName))
                    {
                        localHelpInfo.ForwardTarget = localHelpInfo.Name;
                    }
                    else
                    {
                        localHelpInfo.ForwardTarget = helpCommentsParser._sections.ForwardHelpTargetName;
                    }

                    if (!string.IsNullOrEmpty(helpCommentsParser._sections.ForwardHelpCategory))
                    {
                        try
                        {
                            localHelpInfo.ForwardHelpCategory = (HelpCategory)Enum.Parse(typeof(HelpCategory), helpCommentsParser._sections.ForwardHelpCategory, true);
                        }
                        catch (System.ArgumentException)
                        {
                            // Ignore conversion errors.
                        }
                    }
                    else
                    {
                        localHelpInfo.ForwardHelpCategory = (HelpCategory.Alias |
                                                             HelpCategory.Cmdlet |
                                                             HelpCategory.ExternalScript |
                                                             HelpCategory.Filter |
                                                             HelpCategory.Function |
                                                             HelpCategory.ScriptCommand);
                    }
                }

                // Add HelpUri if necessary
                if (localHelpInfo.GetUriForOnlineHelp() == null)
                {
                    DefaultCommandHelpObjectBuilder.AddRelatedLinksProperties(localHelpInfo.FullHelp, commandInfo.CommandMetadata.HelpUri);
                }
            }

            return localHelpInfo;
        }

        /// <summary>
        /// Analyze a block of comments to determine if it is a special help block.
        /// </summary>
        /// <param name="commentBlock">The block of comments to analyze.</param>
        /// <returns>True if the block is our special comment block for help, false otherwise.</returns>
        internal static bool IsCommentHelpText(List<Token> commentBlock)
        {
            if ((commentBlock == null) || (commentBlock.Count == 0))
                return false;

            HelpCommentsParser generator = new HelpCommentsParser();
            return generator.AnalyzeCommentBlock(commentBlock);
        }

        #region Collect comments from AST

        private static List<Language.Token> GetCommentBlock(Language.Token[] tokens, ref int startIndex)
        {
            var result = new List<Language.Token>();

            // Any whitespace between the token and the first comment is allowed.
            int nextMaxStartLine = int.MaxValue;

            for (int i = startIndex; i < tokens.Length; i++)
            {
                Language.Token current = tokens[i];

                // If the current token starts on a line beyond the current "chunk",
                // then we're done scanning.
                if (current.Extent.StartLineNumber > nextMaxStartLine)
                {
                    startIndex = i;
                    break;
                }

                if (current.Kind == TokenKind.Comment)
                {
                    result.Add(current);

                    // The next comment must be on either the same line as this comment ends, or
                    // the next line, but nowhere else, otherwise it's not in the same "chunk".
                    nextMaxStartLine = current.Extent.EndLineNumber + 1;
                }
                else if (current.Kind != TokenKind.NewLine)
                {
                    // A non-comment, non-position token means we are no longer collecting comments
                    startIndex = i;
                    break;
                }
            }

            return result;
        }

        private static List<Language.Token> GetPrecedingCommentBlock(Language.Token[] tokens, int tokenIndex, int proximity)
        {
            var result = new List<Language.Token>();
            int minEndLine = tokens[tokenIndex].Extent.StartLineNumber - proximity;

            for (int i = tokenIndex - 1; i >= 0; i--)
            {
                Language.Token current = tokens[i];

                if (current.Extent.EndLineNumber < minEndLine)
                    break;

                if (current.Kind == TokenKind.Comment)
                {
                    result.Add(current);
                    minEndLine = current.Extent.StartLineNumber - 1;
                }
                else if (current.Kind != TokenKind.NewLine)
                {
                    break;
                }
            }

            result.Reverse();
            return result;
        }

        private static int FirstTokenInExtent(Language.Token[] tokens, IScriptExtent extent, int startIndex = 0)
        {
            int index;
            for (index = startIndex; index < tokens.Length; ++index)
            {
                if (!tokens[index].Extent.IsBefore(extent))
                {
                    break;
                }
            }

            return index;
        }

        private static int LastTokenInExtent(Language.Token[] tokens, IScriptExtent extent, int startIndex)
        {
            int index;
            for (index = startIndex; index < tokens.Length; ++index)
            {
                if (tokens[index].Extent.IsAfter(extent))
                {
                    break;
                }
            }

            return index - 1;
        }

        internal const int CommentBlockProximity = 2;

        private static List<string> GetParameterComments(Language.Token[] tokens, IParameterMetadataProvider ipmp, int startIndex)
        {
            var result = new List<string>();
            var parameters = ipmp.Parameters;
            if (parameters == null || parameters.Count == 0)
            {
                return result;
            }

            foreach (var parameter in parameters)
            {
                var commentLines = new List<string>();

                var firstToken = FirstTokenInExtent(tokens, parameter.Extent, startIndex);
                var comments = GetPrecedingCommentBlock(tokens, firstToken, CommentBlockProximity);
                if (comments != null)
                {
                    foreach (var comment in comments)
                    {
                        CollectCommentText(comment, commentLines);
                    }
                }

                var lastToken = LastTokenInExtent(tokens, parameter.Extent, firstToken);
                for (int i = firstToken; i < lastToken; ++i)
                {
                    if (tokens[i].Kind == TokenKind.Comment)
                    {
                        CollectCommentText(tokens[i], commentLines);
                    }
                }

                lastToken += 1;
                comments = GetCommentBlock(tokens, ref lastToken);
                if (comments != null)
                {
                    foreach (var comment in comments)
                    {
                        CollectCommentText(comment, commentLines);
                    }
                }

                int n = -1;
                result.Add(GetSection(commentLines, ref n));
            }

            return result;
        }

        internal static Tuple<List<Language.Token>, List<string>> GetHelpCommentTokens(IParameterMetadataProvider ipmp,
            Dictionary<Ast, Token[]> scriptBlockTokenCache)
        {
            Diagnostics.Assert(scriptBlockTokenCache != null, "scriptBlockTokenCache cannot be null");
            var ast = (Ast)ipmp;

            var rootAst = ast;
            Ast configAst = null;
            while (rootAst.Parent != null)
            {
                rootAst = rootAst.Parent;
                if (rootAst is ConfigurationDefinitionAst)
                {
                    configAst = rootAst;
                }
            }

            //  tokens saved from reparsing the script.
            Language.Token[] tokens = null;
            scriptBlockTokenCache.TryGetValue(rootAst, out tokens);

            if (tokens == null)
            {
                ParseError[] errors;
                // storing all comment tokens
                Language.Parser.ParseInput(rootAst.Extent.Text, out tokens, out errors);
                scriptBlockTokenCache[rootAst] = tokens;
            }

            int savedStartIndex;
            int startTokenIndex;
            int lastTokenIndex;

            var funcDefnAst = ast as FunctionDefinitionAst;
            List<Language.Token> commentBlock;
            if (funcDefnAst != null || configAst != null)
            {
                // The first comment block preceding the function or configuration keyword is a candidate help comment block.
                var funcOrConfigTokenIndex =
                    savedStartIndex = FirstTokenInExtent(tokens, configAst == null ? ast.Extent : configAst.Extent);

                commentBlock = GetPrecedingCommentBlock(tokens, funcOrConfigTokenIndex, CommentBlockProximity);

                if (HelpCommentsParser.IsCommentHelpText(commentBlock))
                {
                    return Tuple.Create(commentBlock, GetParameterComments(tokens, ipmp, savedStartIndex));
                }

                // comment block is behind function definition
                // we don't support it for configuration declaration as this style is rarely used
                if (funcDefnAst != null)
                {
                    startTokenIndex =
                        FirstTokenInExtent(tokens, funcDefnAst.Body.Extent) + 1;
                    lastTokenIndex = LastTokenInExtent(tokens, ast.Extent, funcOrConfigTokenIndex);

                    Diagnostics.Assert(tokens[startTokenIndex - 1].Kind == TokenKind.LCurly,
                        "Unexpected first token in function");
                    Diagnostics.Assert(tokens[lastTokenIndex].Kind == TokenKind.RCurly,
                        "Unexpected last token in function");
                }
                else
                {
                    return null;
                }
            }
            else if (ast == rootAst)
            {
                startTokenIndex = savedStartIndex = 0;
                lastTokenIndex = tokens.Length - 1;
            }
            else
            {
                // This case should be rare (but common with implicit remoting).
                // We have a script block that was used to generate a function like:
                //     $sb = { }
                //     set-item function:foo $sb
                //     help foo
                startTokenIndex = savedStartIndex = FirstTokenInExtent(tokens, ast.Extent) + 1;
                lastTokenIndex = LastTokenInExtent(tokens, ast.Extent, startTokenIndex);

                Diagnostics.Assert(tokens[startTokenIndex - 1].Kind == TokenKind.LCurly,
                    "Unexpected first token in script block");
                Diagnostics.Assert(tokens[lastTokenIndex].Kind == TokenKind.RCurly,
                    "Unexpected last token in script block");
            }

            while (true)
            {
                commentBlock = GetCommentBlock(tokens, ref startTokenIndex);
                if (commentBlock.Count == 0)
                    break;

                if (!HelpCommentsParser.IsCommentHelpText(commentBlock))
                    continue;

                if (ast == rootAst)
                {
                    // One more check - make sure the comment doesn't belong to the first function in the script.
                    var endBlock = ((ScriptBlockAst)ast).EndBlock;
                    if (endBlock == null || !endBlock.Unnamed)
                    {
                        return Tuple.Create(commentBlock, GetParameterComments(tokens, ipmp, savedStartIndex));
                    }

                    var firstStatement = endBlock.Statements.FirstOrDefault();
                    if (firstStatement is FunctionDefinitionAst)
                    {
                        int linesBetween = firstStatement.Extent.StartLineNumber -
                                            commentBlock.Last().Extent.EndLineNumber;
                        if (linesBetween > CommentBlockProximity)
                        {
                            return Tuple.Create(commentBlock, GetParameterComments(tokens, ipmp, savedStartIndex));
                        }

                        break;
                    }
                }

                return Tuple.Create(commentBlock, GetParameterComments(tokens, ipmp, savedStartIndex));
            }

            commentBlock = GetPrecedingCommentBlock(tokens, lastTokenIndex, tokens[lastTokenIndex].Extent.StartLineNumber);
            if (HelpCommentsParser.IsCommentHelpText(commentBlock))
            {
                return Tuple.Create(commentBlock, GetParameterComments(tokens, ipmp, savedStartIndex));
            }

            return null;
        }

        #endregion Collect comments from AST
    }
}
