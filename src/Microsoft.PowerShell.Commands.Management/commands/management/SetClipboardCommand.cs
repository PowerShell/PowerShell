// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Management.Automation;
using System.Text;
using Microsoft.PowerShell.Commands.Internal;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Defines the implementation of the 'Set-Clipboard' cmdlet.
    /// This cmdlet gets the content from system clipboard.
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "Clipboard", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium, HelpUri = "https://go.microsoft.com/fwlink/?LinkId=2109826")]
    [Alias("scb")]
    [OutputType(typeof(string))]
    public class SetClipboardCommand : PSCmdlet
    {
        private readonly List<string> _contentList = new();

        /// <summary>
        /// Property that sets clipboard content.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [System.Management.Automation.AllowNull]
        [AllowEmptyCollection]
        [AllowEmptyString]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] Value { get; set; }

        /// <summary>
        /// Property that sets append parameter. This will allow to append clipboard without clear it.
        /// </summary>
        [Parameter]
        public SwitchParameter Append { get; set; }

        /// <summary>
        /// Gets or sets if the values sent down the pipeline.
        /// </summary>
        [Parameter]
        public SwitchParameter PassThru { get; set; }

        /// <summary>
        /// Gets or sets whether to use OSC52 escape sequence to set the clipboard of host instead of target.
        /// </summary>
        [Parameter]
        [Alias("ToLocalhost")]
        public SwitchParameter AsOSC52 { get; set; }

        /// <summary>
        /// This method implements the BeginProcessing method for Set-Clipboard command.
        /// </summary>
        protected override void BeginProcessing()
        {
            _contentList.Clear();
        }

        /// <summary>
        /// This method implements the ProcessRecord method for Set-Clipboard command.
        /// </summary>
        protected override void ProcessRecord()
        {
            if (Value != null)
            {
                _contentList.AddRange(Value);

                if (PassThru)
                {
                    WriteObject(Value);
                }
            }
        }

        /// <summary>
        /// This method implements the EndProcessing method for Set-Clipboard command.
        /// </summary>
        protected override void EndProcessing()
        {
            SetClipboardContent(_contentList, Append);
        }

        /// <summary>
        /// Set the clipboard content.
        /// </summary>
        /// <param name="contentList">The content to store into the clipboard.</param>
        /// <param name="append">If true, appends to clipboard instead of overwriting.</param>
        private void SetClipboardContent(List<string> contentList, bool append)
        {
            string setClipboardShouldProcessTarget;

            if ((contentList == null || contentList.Count == 0) && !append)
            {
                setClipboardShouldProcessTarget = string.Format(CultureInfo.InvariantCulture, ClipboardResources.ClipboardCleared);
                if (ShouldProcess(setClipboardShouldProcessTarget, "Set-Clipboard"))
                {
                    Clipboard.SetText(string.Empty);
                }

                return;
            }

            StringBuilder content = new();
            if (append)
            {
                content.AppendLine(Clipboard.GetText());
            }

            if (contentList != null)
            {
                content.Append(string.Join(Environment.NewLine, contentList.ToArray(), 0, contentList.Count));
            }

            string verboseString = null;
            if (contentList != null)
            {
                verboseString = contentList[0];
                if (verboseString.Length >= 20)
                {
                    verboseString = verboseString.Substring(0, 20);
                    verboseString += " ...";
                }
            }

            if (append)
            {
                setClipboardShouldProcessTarget = string.Format(CultureInfo.InvariantCulture, ClipboardResources.AppendClipboardContent, verboseString);
            }
            else
            {
                setClipboardShouldProcessTarget = string.Format(CultureInfo.InvariantCulture, ClipboardResources.SetClipboardContent, verboseString);
            }

            if (ShouldProcess(setClipboardShouldProcessTarget, "Set-Clipboard"))
            {
                SetClipboardContent(content.ToString());
            }
        }

        /// <summary>
        /// Set the clipboard content.
        /// </summary>
        /// <param name="content">The content to store into the clipboard.</param>
        private void SetClipboardContent(string content)
        {
            if (!AsOSC52)
            {
                Clipboard.SetText(content);
                return;
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(content);
            var encoded = System.Convert.ToBase64String(bytes);
            var osc = $"\u001B]52;;{encoded}\u0007";

            var message = new HostInformationMessage { Message = osc, NoNewLine = true };
            WriteInformation(message, new string[] { "PSHOST" });
        }
    }
}
