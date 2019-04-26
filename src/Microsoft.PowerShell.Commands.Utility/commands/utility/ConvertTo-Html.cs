// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Net;
using System.Text;

using Microsoft.PowerShell.Commands.Internal.Format;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Class comment.
    /// </summary>

    [Cmdlet(VerbsData.ConvertTo, "Html", DefaultParameterSetName = "Page",
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113290", RemotingCapability = RemotingCapability.None)]
    public sealed
    class ConvertToHtmlCommand : PSCmdlet
    {
        /// <summary>The incoming object</summary>
        /// <value></value>
        [Parameter(ValueFromPipeline = true)]
        public PSObject InputObject
        {
            get
            {
                return _inputObject;
            }

            set
            {
                _inputObject = value;
            }
        }

        private PSObject _inputObject;

        /// <summary>
        /// The list of properties to display.
        /// These take the form of a PSPropertyExpression.
        /// </summary>
        /// <value></value>
        [Parameter(Position = 0)]
        public object[] Property
        {
            get
            {
                return _property;
            }

            set
            {
                _property = value;
            }
        }

        private object[] _property;

        /// <summary>
        /// Text to go after the opening body tag and before the table.
        /// </summary>
        /// <value></value>
        [Parameter(ParameterSetName = "Page", Position = 3)]
        public string[] Body
        {
            get
            {
                return _body;
            }

            set
            {
                _body = value;
            }
        }

        private string[] _body;

        /// <summary>
        /// Text to go into the head section of the html doc.
        /// </summary>
        /// <value></value>
        [Parameter(ParameterSetName = "Page", Position = 1)]
        public string[] Head
        {
            get
            {
                return _head;
            }

            set
            {
                _head = value;
            }
        }

        private string[] _head;

        /// <summary>
        /// The string for the title tag
        /// The title is also placed in the body of the document
        /// before the table between h3 tags
        /// If the -Head parameter is used, this parameter has no
        /// effect.
        /// </summary>
        /// <value></value>
        [Parameter(ParameterSetName = "Page", Position = 2)]
        [ValidateNotNullOrEmpty]
        public string Title
        {
            get
            {
                return _title;
            }

            set
            {
                _title = value;
            }
        }

        private string _title = "HTML TABLE";

        /// <summary>
        /// This specifies whether the objects should
        /// be rendered as an HTML TABLE or
        /// HTML LIST.
        /// </summary>
        /// <value></value>
        [Parameter]
        [ValidateNotNullOrEmpty]
        [ValidateSet("Table", "List")]
        public string As
        {
            get
            {
                return _as;
            }

            set
            {
                _as = value;
            }
        }

        private string _as = "Table";

        /// <summary>
        /// This specifies a full or partial URI
        /// for the CSS information.
        /// The HTML should reference the CSS file specified.
        /// </summary>
        [Parameter(ParameterSetName = "Page")]
        [Alias("cu", "uri")]
        [ValidateNotNullOrEmpty]
        public Uri CssUri
        {
            get
            {
                return _cssuri;
            }

            set
            {
                _cssuri = value;
                _cssuriSpecified = true;
            }
        }

        private Uri _cssuri;
        private bool _cssuriSpecified;

        /// <summary>
        /// When this switch is specified generate only the
        /// HTML representation of the incoming object
        /// without the HTML,HEAD,TITLE,BODY,etc tags.
        /// </summary>
        [Parameter(ParameterSetName = "Fragment")]
        [ValidateNotNullOrEmpty]
        public SwitchParameter Fragment
        {
            get
            {
                return _fragment;
            }

            set
            {
                _fragment = value;
            }
        }

        private SwitchParameter _fragment;

        /// <summary>
        /// Specifies the text to include prior the closing body tag of the HTML output.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] PostContent
        {
            get
            {
                return _postContent;
            }

            set
            {
                _postContent = value;
            }
        }

        private string[] _postContent;

        /// <summary>
        /// Specifies the text to include after the body tag of the HTML output.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] PreContent
        {
            get
            {
                return _preContent;
            }

            set
            {
                _preContent = value;
            }
        }

        private string[] _preContent;

        /// <summary>
        /// Sets and Gets the meta property of the HTML head.
        /// </summary>
        /// <returns></returns>
        [Parameter(ParameterSetName = "Page")]
        [ValidateNotNullOrEmpty]
        public Hashtable Meta
        {
            get
            {
                return _meta;
            }

            set
            {
                _meta = value;
                _metaSpecified = true;
            }
        }

        private Hashtable _meta;
        private bool _metaSpecified = false;

        /// <summary>
        /// Specifies the charset encoding for the HTML document.
        /// </summary>
        [Parameter(ParameterSetName = "Page")]
        [ValidateNotNullOrEmpty]
        [ValidatePattern("^[A-Za-z0-9]\\w+\\S+[A-Za-z0-9]$")]
        public string Charset
        {
            get
            {
                return _charset;
            }

            set
            {
                _charset = value;
                _charsetSpecified = true;
            }
        }

        private string _charset;
        private bool _charsetSpecified = false;

        /// <summary>
        /// When this switch statement is specified,
        /// it will change the DOCTYPE to XHTML Transitional DTD.
        /// </summary>
        /// <returns></returns>
        [Parameter(ParameterSetName = "Page")]
        [ValidateNotNullOrEmpty]
        public SwitchParameter Transitional
        {
            get
            {
                return _transitional;
            }

            set
            {
                _transitional = true;
            }
        }

        private bool _transitional = false;

        /// <summary>
        /// Definitions for hash table keys.
        /// </summary>
        internal static class ConvertHTMLParameterDefinitionKeys
        {
            internal const string LabelEntryKey = "label";
            internal const string AlignmentEntryKey = "alignment";
            internal const string WidthEntryKey = "width";
        }

        /// <summary>
        /// This allows for @{e='foo';label='bar';alignment='center';width='20'}.
        /// </summary>
        internal class ConvertHTMLExpressionParameterDefinition : CommandParameterDefinition
        {
            protected override void SetEntries()
            {
                this.hashEntries.Add(new ExpressionEntryDefinition());
                this.hashEntries.Add(new LabelEntryDefinition());
                this.hashEntries.Add(new HashtableEntryDefinition(ConvertHTMLParameterDefinitionKeys.AlignmentEntryKey, new[] { typeof(string) }));

                // Note: We accept "width" as either string or int.
                this.hashEntries.Add(new HashtableEntryDefinition(ConvertHTMLParameterDefinitionKeys.WidthEntryKey, new[] { typeof(string), typeof(int) }));
            }
        }

        /// <summary>
        /// Create a list of MshParameter from properties.
        /// </summary>
        /// <param name="properties">Can be a string, ScriptBlock, or Hashtable.</param>
        /// <returns></returns>
        private List<MshParameter> ProcessParameter(object[] properties)
        {
            TerminatingErrorContext invocationContext = new TerminatingErrorContext(this);
            ParameterProcessor processor =
                new ParameterProcessor(new ConvertHTMLExpressionParameterDefinition());
            if (properties == null)
            {
                properties = new object[] { "*" };
            }

            return processor.ProcessParameters(properties, invocationContext);
        }

        /// <summary>
        /// Resolve all wildcards in user input Property into resolvedNameMshParameters.
        /// </summary>
        private void InitializeResolvedNameMshParameters()
        {
            // temp list of properties with wildcards resolved
            ArrayList resolvedNameProperty = new ArrayList();

            foreach (MshParameter p in _propertyMshParameterList)
            {
                string label = p.GetEntry(ConvertHTMLParameterDefinitionKeys.LabelEntryKey) as string;
                string alignment = p.GetEntry(ConvertHTMLParameterDefinitionKeys.AlignmentEntryKey) as string;

                // Accept the width both as a string and as an int.
                string width;
                int? widthNum = p.GetEntry(ConvertHTMLParameterDefinitionKeys.WidthEntryKey) as int?;
                width = widthNum != null ? widthNum.Value.ToString() : p.GetEntry(ConvertHTMLParameterDefinitionKeys.WidthEntryKey) as string;
                PSPropertyExpression ex = p.GetEntry(FormatParameterDefinitionKeys.ExpressionEntryKey) as PSPropertyExpression;
                List<PSPropertyExpression> resolvedNames = ex.ResolveNames(_inputObject);
                foreach (PSPropertyExpression resolvedName in resolvedNames)
                {
                    Hashtable ht = CreateAuxPropertyHT(label, alignment, width);
                    if (resolvedName.Script != null)
                    {
                        // The argument is a calculated property whose value is calculated by a script block.
                        ht.Add(FormatParameterDefinitionKeys.ExpressionEntryKey, resolvedName.Script);
                    }
                    else
                    {
                        ht.Add(FormatParameterDefinitionKeys.ExpressionEntryKey, resolvedName.ToString());
                    }
                    resolvedNameProperty.Add(ht);
                }
            }

            _resolvedNameMshParameters = ProcessParameter(resolvedNameProperty.ToArray());
        }

        private static Hashtable CreateAuxPropertyHT(
            string label,
            string alignment,
            string width)
        {
            Hashtable ht = new Hashtable();
            if (label != null)
            {
                ht.Add(ConvertHTMLParameterDefinitionKeys.LabelEntryKey, label);
            }

            if (alignment != null)
            {
                ht.Add(ConvertHTMLParameterDefinitionKeys.AlignmentEntryKey, alignment);
            }

            if (width != null)
            {
                ht.Add(ConvertHTMLParameterDefinitionKeys.WidthEntryKey, width);
            }

            return ht;
        }

        /// <summary>
        /// Calls ToString. If an exception occurs, eats it and return string.Empty.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private static string SafeToString(object obj)
        {
            if (obj == null)
            {
                return string.Empty;
            }

            try
            {
                return obj.ToString();
            }
            catch (Exception)
            {
                // eats exception if safe
            }

            return string.Empty;
        }

        /// <summary>
        /// </summary>
        protected override void BeginProcessing()
        {
            // ValidateNotNullOrEmpty attribute is not working for System.Uri datatype, so handling it here
            if ((_cssuriSpecified) && (string.IsNullOrEmpty(_cssuri.OriginalString.Trim())))
            {
                ArgumentException ex = new ArgumentException(StringUtil.Format(UtilityCommonStrings.EmptyCSSUri, "CSSUri"));
                ErrorRecord er = new ErrorRecord(ex, "ArgumentException", ErrorCategory.InvalidArgument, "CSSUri");
                ThrowTerminatingError(er);
            }

            _propertyMshParameterList = ProcessParameter(_property);

            if (!string.IsNullOrEmpty(_title))
            {
                WebUtility.HtmlEncode(_title);
            }

            // This first line ensures w3c validation will succeed. However we are not specifying
            // an encoding in the HTML because we don't know where the text will be written and
            // if a particular encoding will be used.

            if (!_fragment)
            {
                if (!_transitional)
                {
                    WriteObject("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Strict//EN\"  \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd\">");
                }
                else
                {
                    WriteObject("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\"  \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">");
                }

                WriteObject("<html xmlns=\"http://www.w3.org/1999/xhtml\">");
                WriteObject("<head>");
                if (_charsetSpecified)
                {
                    WriteObject("<meta charset=\"" + _charset + "\">");
                }

                if (_metaSpecified)
                {
                    List<string> useditems = new List<string>();
                    foreach (string s in _meta.Keys)
                    {
                        if (!useditems.Contains(s))
                        {
                            switch (s.ToLower())
                            {
                                case "content-type":
                                case "default-style":
                                case "x-ua-compatible":
                                    WriteObject("<meta http-equiv=\"" + s + "\" content=\"" + _meta[s] + "\">");
                                    break;
                                case "application-name":
                                case "author":
                                case "description":
                                case "generator":
                                case "keywords":
                                case "viewport":
                                    WriteObject("<meta name=\"" + s + "\" content=\"" + _meta[s] + "\">");
                                    break;
                                default:
                                    MshCommandRuntime mshCommandRuntime = this.CommandRuntime as MshCommandRuntime;
                                    string Message = StringUtil.Format(ConvertHTMLStrings.MetaPropertyNotFound, s, _meta[s]);
                                    WarningRecord record = new WarningRecord(Message);
                                    InvocationInfo invocationInfo = GetVariableValue(SpecialVariables.MyInvocation) as InvocationInfo;

                                    if (invocationInfo != null)
                                    {
                                        record.SetInvocationInfo(invocationInfo);
                                    }

                                    mshCommandRuntime.WriteWarning(record);
                                    WriteObject("<meta name=\"" + s + "\" content=\"" + _meta[s] + "\">");
                                    break;
                            }

                            useditems.Add(s);
                        }
                    }
                }

                WriteObject(_head ?? new string[] { "<title>" + _title + "</title>" }, true);
                if (_cssuriSpecified)
                {
                    WriteObject("<link rel=\"stylesheet\" type=\"text/css\" href=\"" + _cssuri + "\" />");
                }

                WriteObject("</head><body>");
                if (_body != null)
                {
                    WriteObject(_body, true);
                }
            }

            if (_preContent != null)
            {
                WriteObject(_preContent, true);
            }

            WriteObject("<table>");
            _isTHWritten = false;
        }

        /// <summary>
        /// Reads Width and Alignment from Property and write Col tags.
        /// </summary>
        /// <param name="mshParams"></param>
        private void WriteColumns(List<MshParameter> mshParams)
        {
            StringBuilder COLTag = new StringBuilder();

            COLTag.Append("<colgroup>");
            foreach (MshParameter p in mshParams)
            {
                COLTag.Append("<col");
                string width = p.GetEntry(ConvertHTMLParameterDefinitionKeys.WidthEntryKey) as string;
                if (width != null)
                {
                    COLTag.Append(" width = \"");
                    COLTag.Append(width);
                    COLTag.Append("\"");
                }

                string alignment = p.GetEntry(ConvertHTMLParameterDefinitionKeys.AlignmentEntryKey) as string;
                if (alignment != null)
                {
                    COLTag.Append(" align = \"");
                    COLTag.Append(alignment);
                    COLTag.Append("\"");
                }

                COLTag.Append("/>");
            }

            COLTag.Append("</colgroup>");

            // The columngroup and col nodes will be printed in a single line.
            WriteObject(COLTag.ToString());
        }

        /// <summary>
        /// Writes the list entries when the As parameter has value List.
        /// </summary>
        private void WriteListEntry()
        {
            foreach (MshParameter p in _resolvedNameMshParameters)
            {
                StringBuilder Listtag = new StringBuilder();
                Listtag.Append("<tr><td>");

                // for writing the property name
                WritePropertyName(Listtag, p);
                Listtag.Append(":");
                Listtag.Append("</td>");

                // for writing the property value
                Listtag.Append("<td>");
                WritePropertyValue(Listtag, p);
                Listtag.Append("</td></tr>");

                WriteObject(Listtag.ToString());
            }
        }

        /// <summary>
        /// To write the Property name.
        /// </summary>
        private void WritePropertyName(StringBuilder Listtag, MshParameter p)
        {
            // for writing the property name
            string label = p.GetEntry(ConvertHTMLParameterDefinitionKeys.LabelEntryKey) as string;
            if (label != null)
            {
                Listtag.Append(label);
            }
            else
            {
                PSPropertyExpression ex = p.GetEntry(FormatParameterDefinitionKeys.ExpressionEntryKey) as PSPropertyExpression;
                Listtag.Append(ex.ToString());
            }
        }

        /// <summary>
        /// To write the Property value.
        /// </summary>
        private void WritePropertyValue(StringBuilder Listtag, MshParameter p)
        {
            PSPropertyExpression exValue = p.GetEntry(FormatParameterDefinitionKeys.ExpressionEntryKey) as PSPropertyExpression;

            // get the value of the property
            List<PSPropertyExpressionResult> resultList = exValue.GetValues(_inputObject);
            foreach (PSPropertyExpressionResult result in resultList)
            {
                // create comma sep list for multiple results
                if (result.Result != null)
                {
                    string htmlEncodedResult = WebUtility.HtmlEncode(SafeToString(result.Result));
                    Listtag.Append(htmlEncodedResult);
                }

                Listtag.Append(", ");
            }

            if (Listtag.ToString().EndsWith(", ", StringComparison.Ordinal))
            {
                Listtag.Remove(Listtag.Length - 2, 2);
            }
        }

        /// <summary>
        /// To write the Table header for the object property names.
        /// </summary>
        private void WriteTableHeader(StringBuilder THtag, List<MshParameter> resolvedNameMshParameters)
        {
            // write the property names
            foreach (MshParameter p in resolvedNameMshParameters)
            {
                THtag.Append("<th>");
                WritePropertyName(THtag, p);
                THtag.Append("</th>");
            }
        }

        /// <summary>
        /// To write the Table row for the object property values.
        /// </summary>
        private void WriteTableRow(StringBuilder TRtag, List<MshParameter> resolvedNameMshParameters)
        {
            // write the property values
            foreach (MshParameter p in resolvedNameMshParameters)
            {
                TRtag.Append("<td>");
                WritePropertyValue(TRtag, p);
                TRtag.Append("</td>");
            }
        }

        // count of the objects
        private int _numberObjects = 0;

        /// <summary>
        /// </summary>
        protected override void ProcessRecord()
        {
            // writes the table headers
            // it is not in BeginProcessing because the first inputObject is needed for
            // the number of columns and column name
            if (_inputObject == null || _inputObject == AutomationNull.Value)
            {
                return;
            }

            _numberObjects++;
            if (!_isTHWritten)
            {
                InitializeResolvedNameMshParameters();
                if (_resolvedNameMshParameters == null || _resolvedNameMshParameters.Count == 0)
                {
                    return;
                }

                // if the As parameter is given as List
                if (_as.Equals("List", StringComparison.OrdinalIgnoreCase))
                {
                    // if more than one object,write the horizontal rule to put visual separator
                    if (_numberObjects > 1)
                        WriteObject("<tr><td><hr></td></tr>");
                    WriteListEntry();
                }
                else // if the As parameter is Table, first we have to write the property names
                {
                    WriteColumns(_resolvedNameMshParameters);

                    StringBuilder THtag = new StringBuilder("<tr>");

                    // write the table header
                    WriteTableHeader(THtag, _resolvedNameMshParameters);

                    THtag.Append("</tr>");
                    WriteObject(THtag.ToString());
                    _isTHWritten = true;
                }
            }
            // if the As parameter is Table, write the property values
            if (_as.Equals("Table", StringComparison.OrdinalIgnoreCase))
            {
                StringBuilder TRtag = new StringBuilder("<tr>");

                // write the table row
                WriteTableRow(TRtag, _resolvedNameMshParameters);

                TRtag.Append("</tr>");
                WriteObject(TRtag.ToString());
            }
        }

        /// <summary>
        /// </summary>
        protected override void EndProcessing()
        {
            // if fragment,end with table
            WriteObject("</table>");
            if (_postContent != null)
                WriteObject(_postContent, true);

            // if not fragment end with body and html also
            if (!_fragment)
            {
                WriteObject("</body></html>");
            }
        }

        #region private

        /// <summary>
        /// List of incoming objects to compare.
        /// </summary>
        private bool _isTHWritten;
        private List<MshParameter> _propertyMshParameterList;
        private List<MshParameter> _resolvedNameMshParameters;
        // private string ResourcesBaseName = "ConvertHTMLStrings";

        #endregion private
    }
}
