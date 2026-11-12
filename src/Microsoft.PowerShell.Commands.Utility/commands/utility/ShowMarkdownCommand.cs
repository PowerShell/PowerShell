// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Text;

using Microsoft.PowerShell.MarkdownRender;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Show the VT100EncodedString or Html property of on console or show.
    /// VT100EncodedString will be displayed on console.
    /// Html will be displayed in default browser.
    /// </summary>
    [Cmdlet(
        VerbsCommon.Show, "Markdown",
        DefaultParameterSetName = "Path",
        HelpUri = "https://go.microsoft.com/fwlink/?linkid=2102329")]
    [OutputType(typeof(string))]
    public class ShowMarkdownCommand : PSCmdlet
    {
        /// <summary>
        /// Gets or sets InputObject of type Microsoft.PowerShell.MarkdownRender.MarkdownInfo to display.
        /// </summary>
        [ValidateNotNullOrEmpty]
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "InputObject")]
        public PSObject InputObject { get; set; }

        /// <summary>
        /// Gets or sets path to markdown file(s) to display.
        /// </summary>
        [ValidateNotNullOrEmpty]
        [Parameter(Position = 0, Mandatory = true,
                   ValueFromPipelineByPropertyName = true, ParameterSetName = "Path")]
        public string[] Path { get; set; }

        /// <summary>
        /// Gets or sets the literal path parameter to markdown files(s) to display.
        /// </summary>
        [Parameter(ParameterSetName = "LiteralPath",
                   Mandatory = true, ValueFromPipeline = false, ValueFromPipelineByPropertyName = true)]
        [Alias("PSPath", "LP")]
        public string[] LiteralPath
        {
            get { return Path; }

            set { Path = value; }
        }

        /// <summary>
        /// Gets or sets the switch to view Html in default browser.
        /// </summary>
        [Parameter]
        public SwitchParameter UseBrowser { get; set; }

        private System.Management.Automation.PowerShell _powerShell;

        private readonly StringBuilder _inputObjectBuffer = new();

        /// <summary>
        /// Override BeginProcessing.
        /// </summary>
        protected override void BeginProcessing()
        {
            _powerShell = System.Management.Automation.PowerShell.Create(RunspaceMode.CurrentRunspace);
        }

        /// <summary>
        /// Override ProcessRecord.
        /// </summary>
        protected override void ProcessRecord()
        {
            switch (ParameterSetName)
            {
                case "InputObject":
                    if (InputObject.BaseObject is MarkdownInfo markdownInfo)
                    {
                        ProcessMarkdownInfo(markdownInfo);
                    }
                    else if (InputObject.BaseObject is string objectString)
                    {
                        _inputObjectBuffer.AppendLine(objectString);
                    }
                    else
                    {
                        ConvertFromMarkdown("InputObject", InputObject.BaseObject);
                    }

                    break;

                case "Path":
                case "LiteralPath":
                    ConvertFromMarkdown(ParameterSetName, Path);
                    break;

                default:
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, ConvertMarkdownStrings.InvalidParameterSet, ParameterSetName));
            }
        }

        /// <summary>
        /// Process markdown as path.
        /// </summary>
        /// <param name="parameter">Name of parameter to pass to `ConvertFrom-Markdown`.</param>
        /// <param name="input">Value of parameter.</param>
        private void ConvertFromMarkdown(string parameter, object input)
        {
            _powerShell.AddCommand("Microsoft.PowerShell.Utility\\ConvertFrom-Markdown").AddParameter(parameter, input);
            if (!UseBrowser)
            {
                _powerShell.AddParameter("AsVT100EncodedString");
            }

            Collection<MarkdownInfo> output = _powerShell.Invoke<MarkdownInfo>();

            if (_powerShell.HadErrors)
            {
                foreach (ErrorRecord errorRecord in _powerShell.Streams.Error)
                {
                    WriteError(errorRecord);
                }
            }

            foreach (MarkdownInfo result in output)
            {
                ProcessMarkdownInfo(result);
            }
        }

        /// <summary>
        /// Process markdown as input objects.
        /// </summary>
        /// <param name="markdownInfo">Markdown object to process.</param>
        private void ProcessMarkdownInfo(MarkdownInfo markdownInfo)
        {
            if (UseBrowser)
            {
                var html = markdownInfo.Html;

                if (!string.IsNullOrEmpty(html))
                {
                    string tmpFilePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString() + ".html");

                    try
                    {
                        using (var writer = new StreamWriter(new FileStream(tmpFilePath, FileMode.Create, FileAccess.Write, FileShare.Write)))
                        {
                            writer.Write(html);
                        }
                    }
                    catch (Exception e)
                    {
                        var errorRecord = new ErrorRecord(
                            e,
                            "ErrorWritingTempFile",
                            ErrorCategory.WriteError,
                            tmpFilePath);

                        WriteError(errorRecord);
                        return;
                    }

                    if (InternalTestHooks.ShowMarkdownOutputBypass)
                    {
                        WriteObject(html);
                        return;
                    }

                    try
                    {
                        ProcessStartInfo startInfo = new();
                        startInfo.FileName = tmpFilePath;
                        startInfo.UseShellExecute = true;
                        Process.Start(startInfo);
                    }
                    catch (Exception e)
                    {
                        var errorRecord = new ErrorRecord(
                            e,
                            "ErrorLaunchingDefaultApplication",
                            ErrorCategory.InvalidOperation,
                            targetObject: null);

                        WriteError(errorRecord);
                        return;
                    }
                }
                else
                {
                    string errorMessage = StringUtil.Format(ConvertMarkdownStrings.MarkdownInfoInvalid, "Html");
                    var errorRecord = new ErrorRecord(
                        new InvalidDataException(errorMessage),
                        "HtmlIsNullOrEmpty",
                        ErrorCategory.InvalidData,
                        html);

                    WriteError(errorRecord);
                }
            }
            else
            {
                var vt100String = markdownInfo.VT100EncodedString;

                if (!string.IsNullOrEmpty(vt100String))
                {
                    WriteObject(vt100String);
                }
                else
                {
                    string errorMessage = StringUtil.Format(ConvertMarkdownStrings.MarkdownInfoInvalid, "VT100EncodedString");
                    var errorRecord = new ErrorRecord(
                        new InvalidDataException(errorMessage),
                        "VT100EncodedStringIsNullOrEmpty",
                        ErrorCategory.InvalidData,
                        vt100String);

                    WriteError(errorRecord);
                }
            }
        }

        /// <summary>
        /// Override EndProcessing.
        /// </summary>
        protected override void EndProcessing()
        {
            if (_inputObjectBuffer.Length > 0)
            {
                ConvertFromMarkdown(ParameterSetName, _inputObjectBuffer.ToString());
            }

            _powerShell?.Dispose();
        }
    }
}
