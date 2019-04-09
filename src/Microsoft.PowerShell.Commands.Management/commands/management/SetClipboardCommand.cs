// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Management.Automation;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Collections.Specialized;
using System.Linq;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Defines the implementation of the 'Set-Clipboard' cmdlet.
    /// This cmdlet gets the content from system clipboard.
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "Clipboard", DefaultParameterSetName = "String", SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkId=526220")]
    [Alias("scb")]
    public class SetClipboardCommand : PSCmdlet
    {
        private List<string> _contentList = new List<string>();
        private const string ValueParameterSet = "Value";
        private const string PathParameterSet = "Path";
        private const string LiteralPathParameterSet = "LiteralPath";

        /// <summary>
        /// Property that sets clipboard content.
        /// </summary>
        [Parameter(ParameterSetName = ValueParameterSet, Position = 0, Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [AllowNull]
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
        /// Property that sets Path parameter. This will allow to set file formats to Clipboard.
        /// </summary>
        [Parameter(ParameterSetName = PathParameterSet, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] Path { get; set; }

        /// <summary>
        /// Property that sets LiteralPath parameter. This will allow to set file formats to Clipboard.
        /// </summary>
        [Parameter(ParameterSetName = LiteralPathParameterSet, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [Alias("PSPath")]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] LiteralPath { get; set; }

        /// <summary>
        /// Property that sets html parameter. This will allow html content rendered as html to clipboard.
        /// </summary>
        [Parameter]
        public SwitchParameter AsHtml
        {
            get { return _asHtml; }

            set
            {
                _isHtmlSet = true;
                _asHtml = value;
            }
        }

        private bool _asHtml;
        private bool _isHtmlSet = false;

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
            // Html should only combine with Text content.
            if (Value == null && _isHtmlSet)
            {
                ThrowTerminatingError(new ErrorRecord(new InvalidOperationException(
    string.Format(CultureInfo.InvariantCulture, ClipboardResources.InvalidHtmlCombine)),
    "FailedToSetClipboard", ErrorCategory.InvalidOperation, "Clipboard"));
            }

            if (Value != null)
            {
                _contentList.AddRange(Value);
            }
            else if (Path != null)
            {
                _contentList.AddRange(Path);
            }
            else if (LiteralPath != null)
            {
                _contentList.AddRange(LiteralPath);
            }
        }

        /// <summary>
        /// This method implements the EndProcessing method for Set-Clipboard command.
        /// </summary>
        protected override void EndProcessing()
        {
            if (LiteralPath != null)
            {
                CopyFilesToClipboard(_contentList, Append, true);
            }
            else if (Path != null)
            {
                CopyFilesToClipboard(_contentList, Append, false);
            }
            else
            {
                SetClipboardContent(_contentList, Append, _asHtml);
            }
        }

        /// <summary>
        /// Set the clipboard content.
        /// </summary>
        /// <param name="contentList"></param>
        /// <param name="append"></param>
        /// <param name="asHtml"></param>
        private void SetClipboardContent(List<string> contentList, bool append, bool asHtml)
        {
            string setClipboardShouldProcessTarget;

            if ((contentList == null || contentList.Count == 0) && !append)
            {
                setClipboardShouldProcessTarget = string.Format(CultureInfo.InvariantCulture, ClipboardResources.ClipboardCleared);
                if (ShouldProcess(setClipboardShouldProcessTarget, "Set-Clipboard"))
                {
                    Clipboard.Clear();
                }

                return;
            }

            StringBuilder content = new StringBuilder();
            if (append)
            {
                if (!Clipboard.ContainsText())
                {
                    WriteVerbose(string.Format(CultureInfo.InvariantCulture, ClipboardResources.NoAppendableClipboardContent));
                    append = false;
                }
                else
                {
                    content.AppendLine(Clipboard.GetText());
                }
            }

            if (contentList != null)
            {
                content.Append(string.Join(Environment.NewLine, contentList.ToArray(), 0, contentList.Count));
            }

            // Verbose output
            string verboseString = null;
            if (contentList != null)
            {
                verboseString = contentList[0];
                if (verboseString.Length >= 20)
                {
                    verboseString = verboseString.Substring(0, 20);
                    verboseString = verboseString + " ...";
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
                // Set the text data
                Clipboard.Clear();
                if (asHtml)
                    Clipboard.SetText(GetHtmlDataString(content.ToString()), TextDataFormat.Html);
                else
                    Clipboard.SetText(content.ToString());
            }
        }

        /// <summary>
        /// Copy the file format to clipboard.
        /// </summary>
        /// <param name="fileList"></param>
        /// <param name="append"></param>
        /// <param name="isLiteralPath"></param>
        private void CopyFilesToClipboard(List<string> fileList, bool append, bool isLiteralPath)
        {
            int clipBoardContentLength = 0;
            HashSet<string> dropFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Append the new file list after the file list exists in the clipboard.
            if (append)
            {
                if (!Clipboard.ContainsFileDropList())
                {
                    WriteVerbose(string.Format(CultureInfo.InvariantCulture, ClipboardResources.NoAppendableClipboardContent));
                    append = false;
                }
                else
                {
                    StringCollection clipBoardContent = Clipboard.GetFileDropList();
                    dropFiles = new HashSet<string>(clipBoardContent.Cast<string>().ToList(), StringComparer.OrdinalIgnoreCase);

                    // we need the count of original files so we can get the accurate files number that has been appended.
                    clipBoardContentLength = clipBoardContent.Count;
                }
            }

            ProviderInfo provider = null;
            for (int i = 0; i < fileList.Count; i++)
            {
                Collection<string> newPaths = new Collection<string>();

                try
                {
                    if (isLiteralPath)
                    {
                        newPaths.Add(Context.SessionState.Path.GetUnresolvedProviderPathFromPSPath(fileList[i]));
                    }
                    else
                    {
                        newPaths = Context.SessionState.Path.GetResolvedProviderPathFromPSPath(fileList[i], out provider);
                    }
                }
                catch (ItemNotFoundException exception)
                {
                    WriteError(new ErrorRecord(exception, "FailedToSetClipboard", ErrorCategory.InvalidOperation, "Clipboard"));
                }

                foreach (string fileName in newPaths)
                {
                    // Avoid adding duplicated files.
                    if (!dropFiles.Contains(fileName))
                    {
                        dropFiles.Add(fileName);
                    }
                }
            }

            if (dropFiles.Count == 0)
                return;

            // Verbose output
            string setClipboardShouldProcessTarget;
            if ((dropFiles.Count - clipBoardContentLength) == 1)
            {
                if (append)
                {
                    setClipboardShouldProcessTarget = string.Format(CultureInfo.InvariantCulture, ClipboardResources.AppendSingleFileToClipboard, dropFiles.ElementAt<string>(dropFiles.Count - 1));
                }
                else
                {
                    setClipboardShouldProcessTarget = string.Format(CultureInfo.InvariantCulture, ClipboardResources.SetSingleFileToClipboard, dropFiles.ElementAt<string>(0));
                }
            }
            else
            {
                if (append)
                {
                    setClipboardShouldProcessTarget = string.Format(CultureInfo.InvariantCulture, ClipboardResources.AppendMultipleFilesToClipboard, (dropFiles.Count - clipBoardContentLength));
                }
                else
                {
                    setClipboardShouldProcessTarget = string.Format(CultureInfo.InvariantCulture, ClipboardResources.SetMultipleFilesToClipboard, dropFiles.Count);
                }
            }

            if (ShouldProcess(setClipboardShouldProcessTarget, "Set-Clipboard"))
            {
                // Set file list formats to clipboard.
                Clipboard.Clear();
                StringCollection fileDropList = new StringCollection();
                fileDropList.AddRange(dropFiles.ToArray());
                Clipboard.SetFileDropList(fileDropList);
            }
        }

        /// <summary>
        /// Generate HTML fragment data string with header that is required for the clipboard.
        /// </summary>
        /// <param name="html">The html to generate for.</param>
        /// <returns>The resulted string.</returns>
        private static string GetHtmlDataString(string html)
        {
            // The string contains index references to other spots in the string, so we need placeholders so we can compute the offsets.
            // The  "<<<<<<<<1,<<<<<<<<2, etc" strings are just placeholders.  We'll back-patch them actual values within the header location afterwards.
            const string Header = @"Version:0.9
StartHTML:<<<<<<<<1
EndHTML:<<<<<<<<2
StartFragment:<<<<<<<<3
EndFragment:<<<<<<<<4
StartSelection:<<<<<<<<3
EndSelection:<<<<<<<<4";

            const string StartFragment = "<!--StartFragment-->";
            const string EndFragment = @"<!--EndFragment-->";

            var sb = new StringBuilder();
            sb.AppendLine(Header);
            sb.AppendLine(@"<!DOCTYPE HTML  PUBLIC ""-//W3C//DTD HTML 4.0  Transitional//EN"">");

            // if given html already provided the fragments we won't add them
            int fragmentStart, fragmentEnd;
            int fragmentStartIdx = html.IndexOf(StartFragment, StringComparison.OrdinalIgnoreCase);
            int fragmentEndIdx = html.LastIndexOf(EndFragment, StringComparison.OrdinalIgnoreCase);

            // if html tag is missing add it surrounding the given html
            // find the index of "<html", ignore white space and case
            int htmlOpenIdx = Regex.Match(html, @"<\s*h\s*t\s*m\s*l", RegexOptions.IgnoreCase).Index;
            int htmlOpenEndIdx = htmlOpenIdx > 0 ? html.IndexOf('>', htmlOpenIdx) + 1 : -1;
            // find the index of "</html", ignore white space and case
            int htmlCloseIdx = Regex.Match(html, @"<\s*/\s*h\s*t\s*m\s*l", RegexOptions.IgnoreCase).Index;

            if (fragmentStartIdx < 0 && fragmentEndIdx < 0)
            {
                // find the index of "<body", ignore white space and case
                int bodyOpenIdx = Regex.Match(html, @"<\s*b\s*o\s*d\s*y", RegexOptions.IgnoreCase).Index;
                int bodyOpenEndIdx = bodyOpenIdx > 0 ? html.IndexOf('>', bodyOpenIdx) + 1 : -1;

                if (htmlOpenEndIdx < 0 && bodyOpenEndIdx < 0)
                {
                    // the given html doesn't contain html or body tags so we need to add them and place start/end fragments around the given html only
                    sb.Append("<html><body>");
                    sb.Append(StartFragment);
                    fragmentStart = GetByteCount(sb);
                    sb.Append(html);
                    fragmentEnd = GetByteCount(sb);
                    sb.Append(EndFragment);
                    sb.Append("</body></html>");
                }
                else
                {
                    // insert start/end fragments in the proper place (related to html/body tags if exists) so the paste will work correctly
                    // find the index of "</body", ignore white space and case
                    int bodyCloseIdx = Regex.Match(html, @"<\s*/\s*b\s*o\s*d\s*y", RegexOptions.IgnoreCase).Index;

                    if (htmlOpenEndIdx < 0)
                        sb.Append("<html>");
                    else
                        sb.Append(html, 0, htmlOpenEndIdx);

                    if (bodyOpenEndIdx > -1)
                        sb.Append(html, htmlOpenEndIdx > -1 ? htmlOpenEndIdx : 0, bodyOpenEndIdx - (htmlOpenEndIdx > -1 ? htmlOpenEndIdx : 0));

                    sb.Append(StartFragment);
                    fragmentStart = GetByteCount(sb);

                    var innerHtmlStart = bodyOpenEndIdx > -1 ? bodyOpenEndIdx : (htmlOpenEndIdx > -1 ? htmlOpenEndIdx : 0);
                    var innerHtmlEnd = bodyCloseIdx > 0 ? bodyCloseIdx : (htmlCloseIdx > 0 ? htmlCloseIdx : html.Length);
                    sb.Append(html, innerHtmlStart, innerHtmlEnd - innerHtmlStart);

                    fragmentEnd = GetByteCount(sb);
                    sb.Append(EndFragment);

                    if (innerHtmlEnd < html.Length)
                        sb.Append(html, innerHtmlEnd, html.Length - innerHtmlEnd);

                    if (htmlCloseIdx <= 0)
                        sb.Append("</html>");
                }
            }
            else
            {
                // directly return the cf_html
                return html;
            }

            // Back-patch offsets, the replace text area is restricted to header only from index 0 to header.Length
            sb.Replace("<<<<<<<<1", Header.Length.ToString("D9", CultureInfo.CreateSpecificCulture("en-US")), 0, Header.Length);
            sb.Replace("<<<<<<<<2", GetByteCount(sb).ToString("D9", CultureInfo.CreateSpecificCulture("en-US")), 0, Header.Length);
            sb.Replace("<<<<<<<<3", fragmentStart.ToString("D9", CultureInfo.CreateSpecificCulture("en-US")), 0, Header.Length);
            sb.Replace("<<<<<<<<4", fragmentEnd.ToString("D9", CultureInfo.CreateSpecificCulture("en-US")), 0, Header.Length);
            return sb.ToString();
        }

        /// <summary>
        /// Calculates the number of bytes produced by encoding the string in the string builder in UTF-8 and not .NET default string encoding.
        /// </summary>
        /// <param name="sb">The string builder to count its string.</param>
        /// <param name="start">Optional: the start index to calculate from (default  - start of string).</param>
        /// <param name="end">Optional: the end index to calculate to (default - end of string).</param>
        /// <returns>The number of bytes required to encode the string in UTF-8.</returns>
        private static int GetByteCount(StringBuilder sb, int start = 0, int end = -1)
        {
            char[] _byteCount = new char[1];
            int count = 0;
            end = end > -1 ? end : sb.Length;
            for (int i = start; i < end; i++)
            {
                _byteCount[0] = sb[i];
                count += Encoding.UTF8.GetByteCount(_byteCount);
            }

            return count;
        }
    }
}
