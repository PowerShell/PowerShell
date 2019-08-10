// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Media;
using System.Runtime.InteropServices;
#if !UNIX
using System.Windows.Forms;
#endif

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

#if UNIX
    /// <summary>
    /// Defines the different text formats supported by clipboard.
    /// </summary>
    public enum TextDataFormat
    {
        /// Text format as default.
        Text = 0,

        /// Unicode text.
        UnicodeText = 1,

        /// Rich Text Format.
        Rtf = 2,

        /// Hyper-Text Markup Language.
        Html = 3,

        /// Comma Separated Value (CSV).
        CommaSeparatedValue = 4
    };
#endif

    /// <summary>
    /// Defines the implementation of the 'Get-Clipboard' cmdlet.
    /// This cmdlet get the content from system clipboard.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "Clipboard", HelpUri = "https://go.microsoft.com/fwlink/?LinkId=526219")]
    [Alias("gcb")]
#if UNIX
    [OutputType(typeof(string))]
#else
    [OutputType(typeof(string), typeof(FileInfo), typeof(Image), typeof(Stream))]
#endif
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
#if UNIX
            else
            {
                ThrowTerminatingError(new ErrorRecord(new InvalidOperationException(ClipboardResources.UnsupportedFormat),
                    "FailedToGetClipboard", ErrorCategory.InvalidOperation, "Clipboard"));
            }
#else
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
#endif
        }

        /// <summary>
        /// Returns the clipboard content as text format.
        /// </summary>
        /// <param name="textFormat"></param>
        /// <returns></returns>
        private List<string> GetClipboardContentAsText(TextDataFormat textFormat)
        {
            List<string> result = new List<string>();
            string textContent = null;
#if UNIX
            if (textFormat != TextDataFormat.UnicodeText)
            {
                ThrowTerminatingError(new ErrorRecord(new InvalidOperationException(ClipboardResources.TextFormatUnsupported),
                    "FailedToGetClipboard", ErrorCategory.InvalidOperation, "Clipboard"));
            }

            try
            {
                textContent = ClipboardHelper.GetText();
            }
            catch (PlatformNotSupportedException)
            {
                ThrowTerminatingError(new ErrorRecord(new InvalidOperationException(ClipboardResources.UnsupportedPlatform),
                    "FailedToGetClipboard", ErrorCategory.InvalidOperation, "Clipboard"));
            }
#else
            if (!Clipboard.ContainsText(textFormat))
            {
                return null;
            }

            // TextFormat default value is Text, by default it is same as Clipboard.GetText()
            textContent = Clipboard.GetText(textFormat);
#endif
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

#if !UNIX
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
#endif

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

#if UNIX
    internal class ClipboardHelper
    {
        private static bool? _clipboardSupported;

        private static string StartProcess(
            string tool,
            string args,
            string stdin = ""
        )
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.FileName = tool;
            startInfo.Arguments = args;
            string stdout = null;

            using (Process process = new Process())
            {
                process.StartInfo = startInfo;
                try
                {
                    process.Start();
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    _clipboardSupported = false;
                    throw new PlatformNotSupportedException();
                }

                if (stdin != string.Empty)
                {
                    process.StandardInput.Write(stdin);
                    process.StandardInput.Close();
                }
                stdout = process.StandardOutput.ReadToEnd();
                process.WaitForExit(250);

                _clipboardSupported = process.ExitCode == 0;
            }

            return stdout;
        }

        public static string GetText()
        {
            if (_clipboardSupported == false)
            {
                throw new PlatformNotSupportedException();
            }

            string tool = "";
            string args = "";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                tool = "xclip";
                args = "-selection clipboard -out";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                tool = "pbpaste";
            }
            else
            {
                _clipboardSupported = false;
                throw new PlatformNotSupportedException();
            }

            return StartProcess(tool, args);
        }

        public static void SetText(string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            if (_clipboardSupported == false)
            {
                throw new PlatformNotSupportedException();
            }

            string tool = "";
            string args = "";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                tool = "xclip";
                args = "-selection clipboard -in";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                tool = "pbcopy";
            }
            else
            {
                _clipboardSupported = false;
                throw new PlatformNotSupportedException();
            }

            StartProcess(tool, args, text);
        }
    }
#endif
}
