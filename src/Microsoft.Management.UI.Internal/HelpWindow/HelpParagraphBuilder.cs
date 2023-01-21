// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Management.Automation;
using System.Text;
using System.Windows.Documents;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// Builds a help paragraph for a cmdlet.
    /// </summary>
    internal class HelpParagraphBuilder : ParagraphBuilder
    {
        /// <summary>
        /// Indentation size.
        /// </summary>
        internal const int IndentSize = 4;

        /// <summary>
        /// new line separators.
        /// </summary>
        private static readonly string[] Separators = new[] { "\r\n", "\n" };

        /// <summary>
        /// Object with the cmdelt.
        /// </summary>
        private readonly PSObject psObj;

        /// <summary>
        /// Initializes a new instance of the HelpParagraphBuilder class.
        /// </summary>
        /// <param name="paragraph">Paragraph being built.</param>
        /// <param name="psObj">Object with help information.</param>
        internal HelpParagraphBuilder(Paragraph paragraph, PSObject psObj)
            : base(paragraph)
        {
            this.psObj = psObj;
            this.AddTextToParagraphBuilder();
        }

        /// <summary>
        /// Enum for category of Help.
        /// </summary>
        private enum HelpCategory
        {
            Default,
            DscResource,
            Class
        }

        /// <summary>
        /// Gets the string value of a property or null if it could not be retrieved.
        /// </summary>
        /// <param name="psObj">Object with the property.</param>
        /// <param name="propertyName">Property name.</param>
        /// <returns>The string value of a property or null if it could not be retrieved.</returns>
        internal static string GetPropertyString(PSObject psObj, string propertyName)
        {
            Debug.Assert(psObj != null, "ensured by caller");
            object value = GetPropertyObject(psObj, propertyName);

            if (value == null)
            {
                return null;
            }

            return value.ToString();
        }

        /// <summary>
        /// Adds the help text to the paragraph.
        /// </summary>
        internal void AddTextToParagraphBuilder()
        {
            this.ResetAllText();

            string strCategory = HelpParagraphBuilder.GetProperty(this.psObj, "Category").Value.ToString();

            HelpCategory category = HelpCategory.Default;

            if (string.Equals(strCategory, "DscResource", StringComparison.OrdinalIgnoreCase))
            {
                category = HelpCategory.DscResource;
            }
            else if (string.Equals(strCategory, "Class", StringComparison.OrdinalIgnoreCase))
            {
                category = HelpCategory.Class;
            }

            if (HelpParagraphBuilder.GetProperty(this.psObj, "Syntax") == null)
            {
                if (category == HelpCategory.Default)
                {
                    // if there is no syntax, this is not the standard help
                    // it might be an about page
                    this.AddText(this.psObj.ToString(), false);
                    return;
                }
            }

            switch (category)
            {
                case HelpCategory.Class:
                    this.AddDescription(HelpWindowSettings.Default.HelpSynopsysDisplayed, HelpWindowResources.SynopsisTitle, "Introduction");
                    this.AddMembers(HelpWindowSettings.Default.HelpParametersDisplayed, HelpWindowResources.PropertiesTitle);
                    this.AddMembers(HelpWindowSettings.Default.HelpParametersDisplayed, HelpWindowResources.MethodsTitle);
                    break;
                case HelpCategory.DscResource:
                    this.AddStringSection(HelpWindowSettings.Default.HelpSynopsysDisplayed, "Synopsis", HelpWindowResources.SynopsisTitle);
                    this.AddDescription(HelpWindowSettings.Default.HelpDescriptionDisplayed, HelpWindowResources.DescriptionTitle, "Description");
                    this.AddParameters(HelpWindowSettings.Default.HelpParametersDisplayed, HelpWindowResources.PropertiesTitle, "Properties", HelpCategory.DscResource);
                    break;
                default:
                    this.AddStringSection(HelpWindowSettings.Default.HelpSynopsysDisplayed, "Synopsis", HelpWindowResources.SynopsisTitle);
                    this.AddDescription(HelpWindowSettings.Default.HelpDescriptionDisplayed, HelpWindowResources.DescriptionTitle, "Description");
                    this.AddParameters(HelpWindowSettings.Default.HelpParametersDisplayed, HelpWindowResources.ParametersTitle, "Parameters", HelpCategory.Default);
                    this.AddSyntax(HelpWindowSettings.Default.HelpSyntaxDisplayed, HelpWindowResources.SyntaxTitle);
                    break;
            }

            this.AddInputOrOutputEntries(HelpWindowSettings.Default.HelpInputsDisplayed, HelpWindowResources.InputsTitle, "inputTypes", "inputType");
            this.AddInputOrOutputEntries(HelpWindowSettings.Default.HelpOutputsDisplayed, HelpWindowResources.OutputsTitle, "returnValues", "returnValue");
            this.AddNotes(HelpWindowSettings.Default.HelpNotesDisplayed, HelpWindowResources.NotesTitle);
            this.AddExamples(HelpWindowSettings.Default.HelpExamplesDisplayed, HelpWindowResources.ExamplesTitle);
            this.AddNavigationLink(HelpWindowSettings.Default.HelpRelatedLinksDisplayed, HelpWindowResources.RelatedLinksTitle);
            this.AddStringSection(HelpWindowSettings.Default.HelpRemarksDisplayed, "Remarks", HelpWindowResources.RemarksTitle);
        }

        /// <summary>
        /// Gets the object property or null if it could not be retrieved.
        /// </summary>
        /// <param name="psObj">Object with the property.</param>
        /// <param name="propertyName">Property name.</param>
        /// <returns>The object property or null if it could not be retrieved.</returns>
        private static PSPropertyInfo GetProperty(PSObject psObj, string propertyName)
        {
            Debug.Assert(psObj != null, "ensured by caller");
            return psObj.Properties[propertyName];
        }

        /// <summary>
        /// Gets a PSObject and then a value from it or null if the value could not be retrieved.
        /// </summary>
        /// <param name="psObj">PSObject that contains another PSObject as a property.</param>
        /// <param name="psObjectName">Property name that contains the PSObject.</param>
        /// <param name="propertyName">Property name in thye inner PSObject.</param>
        /// <returns>The string from the inner psObject property or null if it could not be retrieved.</returns>
        private static string GetInnerPSObjectPropertyString(PSObject psObj, string psObjectName, string propertyName)
        {
            Debug.Assert(psObj != null, "ensured by caller");
            PSObject innerPsObj = GetPropertyObject(psObj, psObjectName) as PSObject;

            if (innerPsObj == null)
            {
                return null;
            }

            object value = GetPropertyObject(innerPsObj, propertyName);

            if (value == null)
            {
                return null;
            }

            return value.ToString();
        }

        /// <summary>
        /// Gets the value of a property or null if the value could not be retrieved.
        /// </summary>
        /// <param name="psObj">Object with the property.</param>
        /// <param name="propertyName">Property name.</param>
        /// <returns>The value of a property or null if the value could not be retrieved.</returns>
        private static object GetPropertyObject(PSObject psObj, string propertyName)
        {
            Debug.Assert(psObj != null, "ensured by caller");
            PSPropertyInfo property = HelpParagraphBuilder.GetProperty(psObj, propertyName);
            if (property == null)
            {
                return null;
            }

            object value = null;
            try
            {
                value = property.Value;
            }
            catch (ExtendedTypeSystemException)
            {
                // ignore this exception
            }

            return value;
        }

        /// <summary>
        /// Gets the text from a property of type PSObject[] where the first object has a text property.
        /// </summary>
        /// <param name="psObj">Objhect to get text from.</param>
        /// <param name="propertyText">Property with PSObject[] containing text.</param>
        /// <returns>The text from a property of type PSObject[] where the first object has a text property.</returns>
        private static string GetTextFromArray(PSObject psObj, string propertyText)
        {
            PSObject[] introductionObjects = HelpParagraphBuilder.GetPropertyObject(psObj, propertyText) as PSObject[];
            if (introductionObjects != null && introductionObjects.Length > 0)
            {
                return GetPropertyString(introductionObjects[0], "text");
            }

            return null;
        }

        /// <summary>
        /// Returns the largest size of a group of strings.
        /// </summary>
        /// <param name="strs">Strings to evaluate the largest size from.</param>
        /// <returns>The largest size of a group of strings.</returns>
        private static int LargestSize(params string[] strs)
        {
            int returnValue = 0;

            foreach (string str in strs)
            {
                if (str != null && str.Length > returnValue)
                {
                    returnValue = str.Length;
                }
            }

            return returnValue;
        }

        /// <summary>
        /// Splits the string adding indentation before each line.
        /// </summary>
        /// <param name="str">String to add indentation to.</param>
        /// <returns>The string indented.</returns>
        private static string AddIndent(string str)
        {
            return HelpParagraphBuilder.AddIndent(str, 1);
        }

        /// <summary>
        /// Splits the string adding indentation before each line.
        /// </summary>
        /// <param name="str">String to add indentation to.</param>
        /// <param name="numberOfIdents">Number of indentations.</param>
        /// <returns>The string indented.</returns>
        private static string AddIndent(string str, int numberOfIdents)
        {
            StringBuilder indent = new StringBuilder();
            indent.Append(' ', numberOfIdents * HelpParagraphBuilder.IndentSize);
            return HelpParagraphBuilder.AddIndent(str, indent.ToString());
        }

        /// <summary>
        /// Splits the string adding indentation before each line.
        /// </summary>
        /// <param name="str">String to add indentation to.</param>
        /// <param name="indentString">Indentation string.</param>
        /// <returns>The string indented.</returns>
        private static string AddIndent(string str, string indentString)
        {
            if (str == null)
            {
                return string.Empty;
            }

            string[] lines = str.Split(Separators, StringSplitOptions.None);

            StringBuilder returnValue = new StringBuilder();
            foreach (string line in lines)
            {
                // Indentation is not localized
                returnValue.Append($"{indentString}{line}\r\n");
            }

            if (returnValue.Length > 2)
            {
                // remove the last \r\n
                returnValue.Remove(returnValue.Length - 2, 2);
            }

            return returnValue.ToString();
        }

        /// <summary>
        /// Get the object array value of a property.
        /// </summary>
        /// <param name="obj">Object containing the property.</param>
        /// <param name="propertyName">Property with the array value.</param>
        /// <returns>The object array value of a property.</returns>
        private static object[] GetPropertyObjectArray(PSObject obj, string propertyName)
        {
            object innerObject;
            if ((innerObject = HelpParagraphBuilder.GetPropertyObject(obj, propertyName)) == null)
            {
                return null;
            }

            if (innerObject is PSObject)
            {
                return new[] { innerObject };
            }

            object[] innerObjectArray = innerObject as object[];
            return innerObjectArray;
        }

        /// <summary>
        /// Adds a section that contains only a string.
        /// </summary>
        /// <param name="setting">True if it should add the segment.</param>
        /// <param name="sectionName">Name of the section to add.</param>
        /// <param name="sectionTitle">Title of the section.</param>
        private void AddStringSection(bool setting, string sectionName, string sectionTitle)
        {
            string propertyValue;
            if (!setting || (propertyValue = HelpParagraphBuilder.GetPropertyString(this.psObj, sectionName)) == null)
            {
                return;
            }

            this.AddText(sectionTitle, true);
            this.AddText("\r\n", false);
            this.AddText(HelpParagraphBuilder.AddIndent(propertyValue), false);
            this.AddText("\r\n\r\n", false);
        }

        /// <summary>
        /// Adds the help syntax segment.
        /// </summary>
        /// <param name="setting">True if it should add the segment.</param>
        /// <param name="sectionTitle">Title of the section.</param>
        private void AddSyntax(bool setting, string sectionTitle)
        {
            PSObject syntaxObject;
            if (!setting || (syntaxObject = HelpParagraphBuilder.GetPropertyObject(this.psObj, "Syntax") as PSObject) == null)
            {
                return;
            }

            object[] syntaxItemsObj = HelpParagraphBuilder.GetPropertyObjectArray(syntaxObject, "syntaxItem");
            if (syntaxItemsObj == null || syntaxItemsObj.Length == 0)
            {
                return;
            }

            this.AddText(sectionTitle, true);
            this.AddText("\r\n", false);

            foreach (object syntaxItemObj in syntaxItemsObj)
            {
                PSObject syntaxItem = syntaxItemObj as PSObject;
                if (syntaxItem == null)
                {
                    continue;
                }

                string commandName = GetPropertyString(syntaxItem, "name");

                object[] parameterObjs = HelpParagraphBuilder.GetPropertyObjectArray(syntaxItem, "parameter");
                if (commandName == null || parameterObjs == null || parameterObjs.Length == 0)
                {
                    continue;
                }

                string commandStart = string.Create(CultureInfo.CurrentCulture, $"{commandName} ");
                this.AddText(HelpParagraphBuilder.AddIndent(commandStart), false);

                foreach (object parameterObj in parameterObjs)
                {
                    PSObject parameter = parameterObj as PSObject;
                    if (parameter == null)
                    {
                        continue;
                    }

                    string parameterValue = GetPropertyString(parameter, "parameterValue");
                    string position = GetPropertyString(parameter, "position");
                    string required = GetPropertyString(parameter, "required");
                    string parameterName = GetPropertyString(parameter, "name");
                    if (position == null || required == null || parameterName == null)
                    {
                        continue;
                    }

                    string parameterType = parameterValue == null ? string.Empty : string.Create(CultureInfo.CurrentCulture, $"<{parameterValue}>");

                    string parameterOptionalOpenBrace, parameterOptionalCloseBrace;

                    if (string.Equals(required, "true", StringComparison.OrdinalIgnoreCase))
                    {
                        parameterOptionalOpenBrace = string.Empty;
                        parameterOptionalCloseBrace = string.Empty;
                    }
                    else
                    {
                        parameterOptionalOpenBrace = "[";
                        parameterOptionalCloseBrace = "]";
                    }

                    string parameterNameOptionalOpenBrace, parameterNameOptionalCloseBrace;

                    if (string.Equals(position, "named", StringComparison.OrdinalIgnoreCase))
                    {
                        parameterNameOptionalOpenBrace = parameterNameOptionalCloseBrace = string.Empty;
                    }
                    else
                    {
                        parameterNameOptionalOpenBrace = "[";
                        parameterNameOptionalCloseBrace = "]";
                    }

                    string paramterPrefix = string.Format(
                        CultureInfo.CurrentCulture,
                        "{0}{1}-",
                        parameterOptionalOpenBrace,
                        parameterNameOptionalOpenBrace);

                    this.AddText(paramterPrefix, false);
                    this.AddText(parameterName, true);

                    string paramterSuffix = string.Format(
                        CultureInfo.CurrentCulture,
                        "{0} {1}{2} ",
                        parameterNameOptionalCloseBrace,
                        parameterType,
                        parameterOptionalCloseBrace);
                    this.AddText(paramterSuffix, false);
                }

                string commonParametersText = string.Format(
                    CultureInfo.CurrentCulture,
                    "[<{0}>]\r\n\r\n",
                    HelpWindowResources.CommonParameters);

                this.AddText(commonParametersText, false);
            }

            this.AddText("\r\n", false);
        }

        /// <summary>
        /// Adds the help description segment.
        /// </summary>
        /// <param name="setting">True if it should add the segment.</param>
        /// <param name="sectionTitle">Title of the section.</param>
        /// <param name="propertyName">PropertyName that has description.</param>
        private void AddDescription(bool setting, string sectionTitle, string propertyName)
        {
            PSObject[] descriptionObjects;
            if (!setting ||
                 (descriptionObjects = HelpParagraphBuilder.GetPropertyObject(this.psObj, propertyName) as PSObject[]) == null ||
                 descriptionObjects.Length == 0)
            {
                return;
            }

            this.AddText(sectionTitle, true);
            this.AddText("\r\n", false);

            foreach (PSObject description in descriptionObjects)
            {
                string descriptionText = GetPropertyString(description, "text");
                this.AddText(HelpParagraphBuilder.AddIndent(descriptionText), false);
                this.AddText("\r\n", false);
            }

            this.AddText("\r\n\r\n", false);
        }

        /// <summary>
        /// Adds the help examples segment.
        /// </summary>
        /// <param name="setting">True if it should add the segment.</param>
        /// <param name="sectionTitle">Title of the section.</param>
        private void AddExamples(bool setting, string sectionTitle)
        {
            if (!setting)
            {
                return;
            }

            PSObject exampleRootObject = HelpParagraphBuilder.GetPropertyObject(this.psObj, "Examples") as PSObject;
            if (exampleRootObject == null)
            {
                return;
            }

            object[] exampleObjects = HelpParagraphBuilder.GetPropertyObjectArray(exampleRootObject, "example");
            if (exampleObjects == null || exampleObjects.Length == 0)
            {
                return;
            }

            this.AddText(sectionTitle, true);
            this.AddText("\r\n", false);

            foreach (object exampleObj in exampleObjects)
            {
                PSObject example = exampleObj as PSObject;
                if (example == null)
                {
                    continue;
                }

                string introductionText = null;
                introductionText = GetTextFromArray(example, "introduction");

                string codeText = GetPropertyString(example, "code");
                string title = GetPropertyString(example, "title");

                if (codeText == null)
                {
                    continue;
                }

                if (title != null)
                {
                    this.AddText(HelpParagraphBuilder.AddIndent(title), false);
                    this.AddText("\r\n", false);
                }

                string codeLine = string.Format(
                    CultureInfo.CurrentCulture,
                    "{0}{1}\r\n\r\n",
                    introductionText,
                    codeText);

                this.AddText(HelpParagraphBuilder.AddIndent(codeLine), false);

                PSObject[] remarks = HelpParagraphBuilder.GetPropertyObject(example, "remarks") as PSObject[];
                if (remarks == null)
                {
                    continue;
                }

                foreach (PSObject remark in remarks)
                {
                    string remarkText = GetPropertyString(remark, "text");
                    if (remarkText == null)
                    {
                        continue;
                    }

                    this.AddText(remarkText, false);
                    this.AddText("\r\n", false);
                }
            }

            this.AddText("\r\n\r\n", false);
        }

        private void AddMembers(bool setting, string sectionTitle)
        {
            if (!setting || string.IsNullOrEmpty(sectionTitle))
            {
                return;
            }

            PSObject memberRootObject = HelpParagraphBuilder.GetPropertyObject(this.psObj, "Members") as PSObject;
            if (memberRootObject == null)
            {
                return;
            }

            object[] memberObjects = HelpParagraphBuilder.GetPropertyObjectArray(memberRootObject, "member");

            if (memberObjects == null)
            {
                return;
            }

            this.AddText(sectionTitle, true);
            this.AddText("\r\n", false);

            foreach (object memberObj in memberObjects)
            {
                string description = null;
                string memberText = null;

                PSObject member = memberObj as PSObject;
                if (member == null)
                {
                    continue;
                }

                string name = GetPropertyString(member, "title");
                string type = GetPropertyString(member, "type");
                string propertyType = null;

                if (string.Equals("field", type, StringComparison.OrdinalIgnoreCase))
                {
                    PSObject fieldData = HelpParagraphBuilder.GetPropertyObject(member, "fieldData") as PSObject;

                    if (fieldData != null)
                    {
                        PSObject propertyTypeObject = HelpParagraphBuilder.GetPropertyObject(fieldData, "type") as PSObject;
                        if (propertyTypeObject != null)
                        {
                            propertyType = GetPropertyString(propertyTypeObject, "name");
                            description = GetPropertyString(propertyTypeObject, "description");
                        }

                        memberText = string.Create(CultureInfo.CurrentCulture, $" [{propertyType}] {name}\r\n");
                    }
                }
                else if (string.Equals("method", type, StringComparison.OrdinalIgnoreCase))
                {
                    FormatMethodData(member, name, out memberText, out description);
                }

                if (!string.IsNullOrEmpty(memberText))
                {
                    this.AddText(HelpParagraphBuilder.AddIndent(string.Empty), false);
                    this.AddText(memberText, true);

                    if (description != null)
                    {
                        this.AddText(HelpParagraphBuilder.AddIndent(description, 2), false);
                        this.AddText("\r\n", false);
                    }

                    this.AddText("\r\n", false);
                }
            }
        }

        private static void FormatMethodData(PSObject member, string name, out string memberText, out string description)
        {
            memberText = null;
            description = null;

            if (member == null || string.IsNullOrEmpty(name))
            {
                return;
            }

            string returnType = null;
            StringBuilder parameterText = new StringBuilder();

            // Get method return type
            PSObject returnTypeObject = HelpParagraphBuilder.GetPropertyObject(member, "returnValue") as PSObject;
            if (returnTypeObject != null)
            {
                PSObject returnTypeData = HelpParagraphBuilder.GetPropertyObject(returnTypeObject, "type") as PSObject;
                if (returnTypeData != null)
                {
                    returnType = GetPropertyString(returnTypeData, "name");
                }
            }

            // Get method description.
            PSObject[] methodDescriptions = HelpParagraphBuilder.GetPropertyObject(member, "introduction") as PSObject[];
            if (methodDescriptions != null)
            {
                foreach (var methodDescription in methodDescriptions)
                {
                    description = GetPropertyString(methodDescription, "Text");

                    // If we get an text we do not need to iterate more.
                    if (!string.IsNullOrEmpty(description))
                    {
                        break;
                    }
                }
            }

            // Get method parameters.
            PSObject parametersObject = HelpParagraphBuilder.GetPropertyObject(member, "parameters") as PSObject;
            if (parametersObject != null)
            {
                PSObject[] paramObject = HelpParagraphBuilder.GetPropertyObject(parametersObject, "parameter") as PSObject[];

                if (paramObject != null)
                {
                    foreach (var param in paramObject)
                    {
                        string parameterName = GetPropertyString(param, "name");
                        string parameterType = null;

                        PSObject parameterTypeData = HelpParagraphBuilder.GetPropertyObject(param, "type") as PSObject;

                        if (parameterTypeData != null)
                        {
                            parameterType = GetPropertyString(parameterTypeData, "name");

                            // If there is no type for the parameter, we expect it is System.Object
                            if (string.IsNullOrEmpty(parameterType))
                            {
                                parameterType = "object";
                            }
                        }

                        string paramString = string.Create(CultureInfo.CurrentCulture, $"[{parameterType}] ${parameterName},");

                        parameterText.Append(paramString);
                    }

                    if (string.Equals(parameterText[parameterText.Length - 1].ToString(), ",", StringComparison.OrdinalIgnoreCase))
                    {
                        parameterText = parameterText.Remove(parameterText.Length - 1, 1);
                    }
                }
            }

            memberText = string.Create(CultureInfo.CurrentCulture, $" [{returnType}] {name}({parameterText})\r\n");
        }

        /// <summary>
        /// Adds the help parameters segment.
        /// </summary>
        /// <param name="setting">True if it should add the segment.</param>
        /// <param name="sectionTitle">Title of the section.</param>
        /// <param name="paramPropertyName">Name of the property which has properties.</param>
        /// <param name="helpCategory">Category of help.</param>
        private void AddParameters(bool setting, string sectionTitle, string paramPropertyName, HelpCategory helpCategory)
        {
            if (!setting)
            {
                return;
            }

            PSObject parameterRootObject = HelpParagraphBuilder.GetPropertyObject(this.psObj, paramPropertyName) as PSObject;
            if (parameterRootObject == null)
            {
                return;
            }

            object[] parameterObjects = null;

            // Root object for Class has members not parameters.
            if (helpCategory != HelpCategory.Class)
            {
                parameterObjects = HelpParagraphBuilder.GetPropertyObjectArray(parameterRootObject, "parameter");
            }

            if (parameterObjects == null || parameterObjects.Length == 0)
            {
                return;
            }

            this.AddText(sectionTitle, true);
            this.AddText("\r\n", false);

            foreach (object parameterObj in parameterObjects)
            {
                PSObject parameter = parameterObj as PSObject;
                if (parameter == null)
                {
                    continue;
                }

                string parameterValue = GetPropertyString(parameter, "parameterValue");
                string name = GetPropertyString(parameter, "name");
                string description = GetTextFromArray(parameter, "description");
                string required = GetPropertyString(parameter, "required");
                string position = GetPropertyString(parameter, "position");
                string pipelineinput = GetPropertyString(parameter, "pipelineInput");
                string defaultValue = GetPropertyString(parameter, "defaultValue");
                string acceptWildcard = GetPropertyString(parameter, "globbing");

                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                if (helpCategory == HelpCategory.DscResource)
                {
                    this.AddText(HelpParagraphBuilder.AddIndent(string.Empty), false);
                }
                else
                {
                    this.AddText(HelpParagraphBuilder.AddIndent("-"), false);
                }

                this.AddText(name, true);
                string parameterText = string.Format(
                    CultureInfo.CurrentCulture,
                    " <{0}>\r\n",
                    parameterValue);

                this.AddText(parameterText, false);

                if (description != null)
                {
                    this.AddText(HelpParagraphBuilder.AddIndent(description, 2), false);
                    this.AddText("\r\n", false);
                }

                this.AddText("\r\n", false);

                int largestSize = HelpParagraphBuilder.LargestSize(
                    HelpWindowResources.ParameterRequired,
                    HelpWindowResources.ParameterPosition,
                    HelpWindowResources.ParameterDefaultValue,
                    HelpWindowResources.ParameterPipelineInput,
                    HelpWindowResources.ParameterAcceptWildcard);

                // justification of parameter values is not localized
                string formatString = string.Format(
                    CultureInfo.CurrentCulture,
                    "{{0,-{0}}}{{1}}",
                    largestSize + 2);

                string tableLine;

                tableLine = string.Format(
                    CultureInfo.CurrentCulture,
                    formatString,
                    HelpWindowResources.ParameterRequired,
                    required);
                this.AddText(HelpParagraphBuilder.AddIndent(tableLine, 2), false);
                this.AddText("\r\n", false);

                // these are not applicable for Dsc Resource help
                if (helpCategory != HelpCategory.DscResource)
                {
                    tableLine = string.Format(
                        CultureInfo.CurrentCulture,
                        formatString,
                        HelpWindowResources.ParameterPosition,
                        position);
                    this.AddText(HelpParagraphBuilder.AddIndent(tableLine, 2), false);
                    this.AddText("\r\n", false);

                    tableLine = string.Format(
                        CultureInfo.CurrentCulture,
                        formatString,
                        HelpWindowResources.ParameterDefaultValue,
                        defaultValue);
                    this.AddText(HelpParagraphBuilder.AddIndent(tableLine, 2), false);
                    this.AddText("\r\n", false);

                    tableLine = string.Format(
                        CultureInfo.CurrentCulture,
                        formatString,
                        HelpWindowResources.ParameterPipelineInput,
                        pipelineinput);
                    this.AddText(HelpParagraphBuilder.AddIndent(tableLine, 2), false);
                    this.AddText("\r\n", false);

                    tableLine = string.Format(
                        CultureInfo.CurrentCulture,
                        formatString,
                        HelpWindowResources.ParameterAcceptWildcard,
                        acceptWildcard);
                    this.AddText(HelpParagraphBuilder.AddIndent(tableLine, 2), false);
                }

                this.AddText("\r\n\r\n", false);
            }

            this.AddText("\r\n\r\n", false);
        }

        /// <summary>
        /// Adds the help navigation links segment.
        /// </summary>
        /// <param name="setting">True if it should add the segment.</param>
        /// <param name="sectionTitle">Title of the section.</param>
        private void AddNavigationLink(bool setting, string sectionTitle)
        {
            if (!setting)
            {
                return;
            }

            PSObject linkRootObject = HelpParagraphBuilder.GetPropertyObject(this.psObj, "RelatedLinks") as PSObject;
            if (linkRootObject == null)
            {
                return;
            }

            PSObject[] linkObjects;

            if ((linkObjects = HelpParagraphBuilder.GetPropertyObject(linkRootObject, "navigationLink") as PSObject[]) == null ||
                linkObjects.Length == 0)
            {
                return;
            }

            this.AddText(sectionTitle, true);
            this.AddText("\r\n", false);

            foreach (PSObject linkObject in linkObjects)
            {
                string text = GetPropertyString(linkObject, "linkText");
                string uri = GetPropertyString(linkObject, "uri");

                string linkLine = string.IsNullOrEmpty(uri) ? text : string.Format(
                    CultureInfo.CurrentCulture,
                    HelpWindowResources.LinkTextFormat,
                    text,
                    uri);

                this.AddText(HelpParagraphBuilder.AddIndent(linkLine), false);
                this.AddText("\r\n", false);
            }

            this.AddText("\r\n\r\n", false);
        }

        /// <summary>
        /// Adds the help input or output segment.
        /// </summary>
        /// <param name="setting">True if it should add the segment.</param>
        /// <param name="sectionTitle">Title of the section.</param>
        /// <param name="inputOrOutputProperty">Property with the outter object.</param>
        /// <param name="inputOrOutputInnerProperty">Property with the inner object.</param>
        private void AddInputOrOutputEntries(bool setting, string sectionTitle, string inputOrOutputProperty, string inputOrOutputInnerProperty)
        {
            if (!setting)
            {
                return;
            }

            PSObject rootObject = HelpParagraphBuilder.GetPropertyObject(this.psObj, inputOrOutputProperty) as PSObject;
            if (rootObject == null)
            {
                return;
            }

            object[] inputOrOutputObjs;
            inputOrOutputObjs = HelpParagraphBuilder.GetPropertyObjectArray(rootObject, inputOrOutputInnerProperty);

            if (inputOrOutputObjs == null || inputOrOutputObjs.Length == 0)
            {
                return;
            }

            this.AddText(sectionTitle, true);
            this.AddText("\r\n", false);

            foreach (object inputOrOutputObj in inputOrOutputObjs)
            {
                PSObject inputOrOutput = inputOrOutputObj as PSObject;
                if (inputOrOutput == null)
                {
                    continue;
                }

                string type = HelpParagraphBuilder.GetInnerPSObjectPropertyString(inputOrOutput, "type", "name");
                string description = GetTextFromArray(inputOrOutput, "description");

                this.AddText(HelpParagraphBuilder.AddIndent(type), false);
                this.AddText("\r\n", false);
                if (description != null)
                {
                    this.AddText(HelpParagraphBuilder.AddIndent(description), false);
                    this.AddText("\r\n", false);
                }
            }

            this.AddText("\r\n", false);
        }

        /// <summary>
        /// Adds the help notes segment.
        /// </summary>
        /// <param name="setting">True if it should add the segment.</param>
        /// <param name="sectionTitle">Title of the section.</param>
        private void AddNotes(bool setting, string sectionTitle)
        {
            if (!setting)
            {
                return;
            }

            PSObject rootObject = HelpParagraphBuilder.GetPropertyObject(this.psObj, "alertSet") as PSObject;
            if (rootObject == null)
            {
                return;
            }

            string note = GetTextFromArray(rootObject, "alert");

            if (note == null)
            {
                return;
            }

            this.AddText(sectionTitle, true);
            this.AddText("\r\n", false);
            this.AddText(HelpParagraphBuilder.AddIndent(note), false);
            this.AddText("\r\n\r\n", false);
        }
    }
}
