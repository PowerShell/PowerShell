// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Internal;
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
        HelpUri = "https://go.microsoft.com/fwlink/?linkid=2006266")]
    [OutputType(typeof(string))]
    public class ShowMarkdownCommand : PSCmdlet
    {
        /// <summary>
        /// Gets or sets InputObject of type Microsoft.PowerShell.MarkdownRender.MarkdownInfo to display.
        /// </summary>
        [ValidateNotNullOrEmpty]
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        public PSObject InputObject { get; set; }

        /// <summary>
        /// Gets or sets the switch to view Html in default browser.
        /// </summary>
        [Parameter]
        public SwitchParameter UseBrowser { get; set; }

        private SteppablePipeline stepPipe;

        /// <summary>
        /// Override BeginProcessing.
        /// </summary>
        protected override void BeginProcessing()
        {
            if (!UseBrowser.IsPresent)
            {
                // Since UseBrowser is not bound, we use proxy to Out-Default
                stepPipe = ScriptBlock.Create(@"Microsoft.PowerShell.Core\Out-Default @PSBoundParameters").GetSteppablePipeline(this.MyInvocation.CommandOrigin);
                stepPipe.Begin(this);
            }
        }

        /// <summary>
        /// Override ProcessRecord.
        /// </summary>
        protected override void ProcessRecord()
        {
            object inpObj = InputObject.BaseObject;

            if (inpObj is MarkdownInfo markdownInfo)
            {
                if (UseBrowser)
                {
                    var html = markdownInfo.Html;

                    if (!string.IsNullOrEmpty(html))
                    {
                        string tmpFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".html");

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
                            ProcessStartInfo startInfo = new ProcessStartInfo();
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
                                targetObject : null);

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
                        if (InternalTestHooks.ShowMarkdownOutputBypass)
                        {
                            WriteObject(vt100String);
                            return;
                        }

                        if (stepPipe != null)
                        {
                            stepPipe.Process(vt100String);
                        }
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
            else
            {
                string errorMessage = StringUtil.Format(ConvertMarkdownStrings.InvalidInputObjectType, inpObj.GetType());
                var errorRecord = new ErrorRecord(
                            new ArgumentException(errorMessage),
                            "InvalidInputObject",
                            ErrorCategory.InvalidArgument,
                            InputObject);

                WriteError(errorRecord);
            }
        }

        /// <summary>
        /// Override EndProcessing.
        /// </summary>
        protected override void EndProcessing()
        {
            if (stepPipe != null)
            {
                stepPipe.End();
            }
        }
    }
}
