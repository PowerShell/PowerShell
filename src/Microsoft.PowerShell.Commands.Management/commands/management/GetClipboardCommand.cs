// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Management.Automation;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Media;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Specialized;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Defines the different type supported by the clipboard.
    /// </summary>
    public enum ClipboardFormat
    {
        /// Text format as default.
        Text = 0,

        /// File format.
        FileDropList = 1,

        /// Image format.
        Image = 2,

        /// Audio format.
        Audio = 3,
    };

    /// <summary>
    /// Defines the implementation of the 'Get-Clipboard' cmdlet.
    /// This cmdlet get the content from system clipboard.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "Clipboard", HelpUri = "https://go.microsoft.com/fwlink/?LinkId=526219")]
    [Alias("gcb")]
    [OutputType(typeof(string), typeof(FileInfo), typeof(Image), typeof(Stream))]
    public class GetClipboardCommand : PSCmdlet
    {
        /// <summary>
        /// Property that sets clipboard type. This will return the required format from clipboard.
        /// </summary>
        [Parameter]
        public ClipboardFormat Format { get; set; }

        /// <summary>
        /// Property that sets format type when the return type is text.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        public TextDataFormat TextFormatType
        {
            get { return _textFormat; }

            set
            {
                _isTextFormatTypeSet = true;
                _textFormat = value;
            }
        }

        private TextDataFormat _textFormat = TextDataFormat.UnicodeText;
        private bool _isTextFormatTypeSet = false;

        /// <summary>
        /// Property that sets raw parameter. This will allow clipboard return text or file list as one string.
        /// </summary>
        [Parameter]
        public SwitchParameter Raw
        {
            get { return _raw; }

            set
            {
                _isRawSet = true;
                _raw = value;
            }
        }

        private bool _raw;
        private bool _isRawSet = false;

        /// <summary>
        /// This method implements the ProcessRecord method for Get-Clipboard command.
        /// </summary>
        protected override void BeginProcessing()
        {
            // TextFormatType should only combine with Text.
            if (Format != ClipboardFormat.Text && _isTextFormatTypeSet)
            {
                ThrowTerminatingError(new ErrorRecord(new InvalidOperationException(
                    string.Format(CultureInfo.InvariantCulture, ClipboardResources.InvalidTypeCombine)),
                    "FailedToGetClipboard", ErrorCategory.InvalidOperation, "Clipboard"));
            }

            // Raw should only combine with Text or FileDropList.
            if (Format != ClipboardFormat.Text && Format != ClipboardFormat.FileDropList && _isRawSet)
            {
                ThrowTerminatingError(new ErrorRecord(new InvalidOperationException(
                    string.Format(CultureInfo.InvariantCulture, ClipboardResources.InvalidRawCombine)),
                    "FailedToGetClipboard", ErrorCategory.InvalidOperation, "Clipboard"));
            }

            if (Format == ClipboardFormat.Text)
            {
                this.WriteObject(GetClipboardContentAsText(_textFormat), true);
            }
            else if (Format == ClipboardFormat.Image)
            {
                this.WriteObject(Clipboard.GetImage());
            }
            else if (Format == ClipboardFormat.FileDropList)
            {
                if (_raw)
                {
                    this.WriteObject(Clipboard.GetFileDropList(), true);
                }
                else
                {
                    this.WriteObject(GetClipboardContentAsFileList());
                }
            }
            else if (Format == ClipboardFormat.Audio)
            {
                this.WriteObject(Clipboard.GetAudioStream());
            }
        }

        /// <summary>
        /// Returns the clipboard content as text format.
        /// </summary>
        /// <param name="textFormat"></param>
        /// <returns></returns>
        private List<string> GetClipboardContentAsText(TextDataFormat textFormat)
        {
            if (!Clipboard.ContainsText(textFormat))
            {
                return null;
            }

            List<string> result = new List<string>();

            // TextFormat default value is Text, by default it is same as Clipboard.GetText()
            string textContent = Clipboard.GetText(textFormat);
            if (_raw)
            {
                result.Add(textContent);
            }
            else
            {
                string[] splitSymbol = { Environment.NewLine };
                result.AddRange(textContent.Split(splitSymbol, StringSplitOptions.None));
            }

            return result;
        }

        /// <summary>
        /// Returns the clipboard content as file info.
        /// </summary>
        /// <returns></returns>
        private List<PSObject> GetClipboardContentAsFileList()
        {
            if (!Clipboard.ContainsFileDropList())
            {
                return null;
            }

            List<PSObject> result = new List<PSObject>();
            foreach (string filePath in Clipboard.GetFileDropList())
            {
                FileInfo file = new FileInfo(filePath);
                result.Add(WrapOutputInPSObject(file, filePath));
            }

            return result;
        }

        /// <summary>
        /// Wraps the item in a PSObject and attaches some notes to the
        /// object that deal with path information.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        private PSObject WrapOutputInPSObject(
            FileInfo item,
            string path)
        {
            PSObject result = new PSObject(item);

            // Now get the parent path and child name
            if (path != null)
            {
                // Get the parent path
                string parentPath = Directory.GetParent(path).FullName;
                result.AddOrSetProperty("PSParentPath", parentPath);

                // Get the child name
                string childName = item.Name;
                result.AddOrSetProperty("PSChildName", childName);
            }

            return result;
        }
    }
}
