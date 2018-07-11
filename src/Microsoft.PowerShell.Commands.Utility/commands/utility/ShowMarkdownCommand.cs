// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;
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
        HelpUri = "TBD")]
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
            if (!this.MyInvocation.BoundParameters.ContainsKey("UseBrowser"))
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
            var markdownInfo = inpObj as MarkdownInfo;

            if (markdownInfo == null)
            {
                var errorRecord = new ErrorRecord(
                            new ArgumentException(),
                            "InvalidInputObject",
                            ErrorCategory.InvalidArgument,
                            InputObject);

                WriteError(errorRecord);
            }
            else
            {
                if (UseBrowser)
                {
                    var html = markdownInfo.Html;

                    if (!string.IsNullOrEmpty(html))
                    {
                        string tmpFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".html");
                        using (var writer = new StreamWriter(new FileStream(tmpFilePath, FileMode.Create, FileAccess.Write, FileShare.Write)))
                        {
                            writer.Write(html);
                        }

                        if (outputBypassTestHook)
                        {
                            WriteObject(html);
                            return;
                        }

                        ProcessStartInfo startInfo = new ProcessStartInfo();

#if UNIX
                        startInfo.FileName = Platform.IsLinux ? "xdg-open" : /* macOS */ "open";
                        startInfo.Arguments = tmpFilePath;
#else
                        startInfo.FileName = tmpFilePath;
                        startInfo.UseShellExecute = true;
#endif

                        Process.Start(startInfo);
                    }
                    else
                    {
                        var errorRecord = new ErrorRecord(
                            new InvalidDataException(),
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
                        if (outputBypassTestHook)
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
                        var errorRecord = new ErrorRecord(
                            new InvalidDataException(),
                            "VT100EncodedStringIsNullOrEmpty",
                            ErrorCategory.InvalidData,
                            vt100String);

                        WriteError(errorRecord);
                    }
                }
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

        private static bool outputBypassTestHook = false;

        /// <summary>
        /// Test hook to enable or disable launching of browser.
        /// When set, the converted output is returned.
        /// </summary>
        /// <param name="value">True to enable test hook, false to disable.</param>
        public static void SetOutputBypassTestHook(bool value)
        {
            outputBypassTestHook = value;
        }
    }
}
