// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Text;

namespace Microsoft.PowerShell.Commands.Internal.Format
{
    /// <summary>
    /// Writer class to handle Complex Object formatting.
    /// </summary>
    internal sealed class ComplexWriter
    {
        /// <summary>
        /// Initialization method to be called before any other operation.
        /// </summary>
        /// <param name="lineOutput">LineOutput interfaces to write to.</param>
        /// <param name="numberOfTextColumns">Number of columns used to write out.</param>
        internal void Initialize(LineOutput lineOutput, int numberOfTextColumns)
        {
            _lo = lineOutput;
            _textColumns = numberOfTextColumns;
        }

        /// <summary>
        /// Writes a string.
        /// </summary>
        /// <param name="s"></param>
        internal void WriteString(string s)
        {
            _indentationManager.Clear();

            AddToBuffer(s);

            WriteToScreen();
        }

        /// <summary>
        /// It interprets a list of format value tokens and outputs it.
        /// </summary>
        /// <param name="formatValueList">List of FormatValue tokens to interpret.</param>
        internal void WriteObject(List<FormatValue> formatValueList)
        {
            // we always start with no indentation
            _indentationManager.Clear();

            foreach (FormatEntry fe in formatValueList)
            {
                // operate on each directive inside the list,
                // carrying the indentation from invocation to invocation
                GenerateFormatEntryDisplay(fe, 0);
            }
            // make sure that, if we have pending text in the buffer it gets flushed
            WriteToScreen();
        }

        /// <summary>
        /// Operate on a single entry.
        /// </summary>
        /// <param name="fe">Entry to process.</param>
        /// <param name="currentDepth">Current depth of recursion.</param>
        private void GenerateFormatEntryDisplay(FormatEntry fe, int currentDepth)
        {
            foreach (object obj in fe.formatValueList)
            {
                if (obj is FormatEntry feChild)
                {
                    if (currentDepth < maxRecursionDepth)
                    {
                        if (feChild.frameInfo != null)
                        {
                            // if we have frame information, we need to push it on the
                            // indentation stack
                            using (_indentationManager.StackFrame(feChild.frameInfo))
                            {
                                GenerateFormatEntryDisplay(feChild, currentDepth + 1);
                            }
                        }
                        else
                        {
                            // no need here of activating an indentation stack frame
                            GenerateFormatEntryDisplay(feChild, currentDepth + 1);
                        }
                    }

                    continue;
                }

                if (obj is FormatNewLine)
                {
                    this.WriteToScreen();
                    continue;
                }

                if (obj is FormatTextField ftf)
                {
                    this.AddToBuffer(ftf.text);
                    continue;
                }

                if (obj is FormatPropertyField fpf)
                {
                    this.AddToBuffer(fpf.propertyValue);
                }
            }
        }

        /// <summary>
        /// Add a string to the current buffer, waiting for a FlushBuffer()
        /// </summary>
        /// <param name="s">String to add to buffer.</param>
        private void AddToBuffer(string s)
        {
            _stringBuffer.Append(s);
        }

        /// <summary>
        /// Write to the output interface.
        /// </summary>
        private void WriteToScreen()
        {
            int leftIndentation = _indentationManager.LeftIndentation;
            int rightIndentation = _indentationManager.RightIndentation;
            int firstLineIndentation = _indentationManager.FirstLineIndentation;

            // VALIDITY CHECKS:

            // check the useful ("active") width
            int usefulWidth = _textColumns - rightIndentation - leftIndentation;
            if (usefulWidth <= 0)
            {
                // fatal error, there is nothing to write to the device
                // just clear the buffer and return
                _stringBuffer = new StringBuilder();
            }

            // check indentation or hanging is not larger than the active width
            int indentationAbsoluteValue = (firstLineIndentation > 0) ? firstLineIndentation : -firstLineIndentation;
            if (indentationAbsoluteValue >= usefulWidth)
            {
                // value too big, we reset it to zero
                firstLineIndentation = 0;
            }

            // compute the first line indentation or hanging
            int firstLineWidth = _textColumns - rightIndentation - leftIndentation;
            int followingLinesWidth = firstLineWidth;

            if (firstLineIndentation >= 0)
            {
                // the first line has an indentation
                firstLineWidth -= firstLineIndentation;
            }
            else
            {
                // the first line is hanging
                followingLinesWidth += firstLineIndentation;
            }

            // error checking on invalid values

            // generate the lines using the computed widths
            StringCollection sc = StringManipulationHelper.GenerateLines(_lo.DisplayCells, _stringBuffer.ToString(),
                                        firstLineWidth, followingLinesWidth);

            // compute padding
            int firstLinePadding = leftIndentation;
            int followingLinesPadding = leftIndentation;
            if (firstLineIndentation >= 0)
            {
                // the first line has an indentation
                firstLinePadding += firstLineIndentation;
            }
            else
            {
                // the first line is hanging
                followingLinesPadding -= firstLineIndentation;
            }

            // now write the lines on the screen
            bool firstLine = true;
            foreach (string s in sc)
            {
                if (firstLine)
                {
                    firstLine = false;
                    _lo.WriteLine(StringManipulationHelper.PadLeft(s, firstLinePadding));
                }
                else
                {
                    _lo.WriteLine(StringManipulationHelper.PadLeft(s, followingLinesPadding));
                }
            }

            _stringBuffer = new StringBuilder();
        }

        /// <summary>
        /// Helper object to manage the frame-based indentation and margins.
        /// </summary>
        private readonly IndentationManager _indentationManager = new IndentationManager();

        /// <summary>
        /// Buffer to accumulate partially constructed text.
        /// </summary>
        private StringBuilder _stringBuffer = new StringBuilder();

        /// <summary>
        /// Interface to write to.
        /// </summary>
        private LineOutput _lo;

        /// <summary>
        /// Number of columns for the output device.
        /// </summary>
        private int _textColumns;

        private const int maxRecursionDepth = 50;
    }

    internal sealed class IndentationManager
    {
        private sealed class IndentationStackFrame : IDisposable
        {
            internal IndentationStackFrame(IndentationManager mgr)
            {
                _mgr = mgr;
            }

            public void Dispose()
            {
                _mgr?.RemoveStackFrame();
            }

            private readonly IndentationManager _mgr;
        }

        internal void Clear()
        {
            _frameInfoStack.Clear();
        }

        internal IDisposable StackFrame(FrameInfo frameInfo)
        {
            IndentationStackFrame frame = new IndentationStackFrame(this);
            _frameInfoStack.Push(frameInfo);
            return frame;
        }

        private void RemoveStackFrame()
        {
            _frameInfoStack.Pop();
        }

        internal int RightIndentation
        {
            get
            {
                return ComputeRightIndentation();
            }
        }

        internal int LeftIndentation
        {
            get
            {
                return ComputeLeftIndentation();
            }
        }

        internal int FirstLineIndentation
        {
            get
            {
                if (_frameInfoStack.Count == 0)
                    return 0;
                return _frameInfoStack.Peek().firstLine;
            }
        }

        private int ComputeRightIndentation()
        {
            int val = 0;
            foreach (FrameInfo fi in _frameInfoStack)
            {
                val += fi.rightIndentation;
            }

            return val;
        }

        private int ComputeLeftIndentation()
        {
            int val = 0;
            foreach (FrameInfo fi in _frameInfoStack)
            {
                val += fi.leftIndentation;
            }

            return val;
        }

        private readonly Stack<FrameInfo> _frameInfoStack = new Stack<FrameInfo>();
    }

    /// <summary>
    /// Result of GetWords.
    /// </summary>
    internal struct GetWordsResult
    {
        internal string Word;
        internal string Delim;
        internal bool VtResetAdded;
    }

    /// <summary>
    /// Collection of helper functions for string formatting.
    /// </summary>
    internal sealed class StringManipulationHelper
    {
        private const char SoftHyphen = '\u00AD';
        private const char HardHyphen = '\u2011';
        private const char NonBreakingSpace = '\u00A0';

        private static readonly Collection<string> s_cultureCollection = new Collection<string>();

        static StringManipulationHelper()
        {
            s_cultureCollection.Add("en");        // English
            s_cultureCollection.Add("fr");        // French
            s_cultureCollection.Add("de");        // German
            s_cultureCollection.Add("it");        // Italian
            s_cultureCollection.Add("pt");        // Portuguese
            s_cultureCollection.Add("es");        // Spanish
        }

        /// <summary>
        /// Breaks a string into a collection of words
        /// TODO: we might be able to improve this function in the future
        /// so that we do not break paths etc.
        /// </summary>
        /// <param name="s">Input string.</param>
        /// <returns>A collection of words.</returns>
        private static IEnumerable<GetWordsResult> GetWords(string s)
        {
            StringBuilder sb = new StringBuilder();
            StringBuilder vtSeqs = null;
            Dictionary<int, int> vtRanges = null;

            var valueStrDec = new ValueStringDecorated(s);
            if (valueStrDec.IsDecorated)
            {
                vtSeqs = new StringBuilder();
                vtRanges = valueStrDec.EscapeSequenceRanges;
            }

            bool wordHasVtSeqs = false;
            for (int i = 0; i < s.Length; i++)
            {
                if (vtRanges?.TryGetValue(i, out int len) == true)
                {
                    var vtSpan = s.AsSpan(i, len);
                    sb.Append(vtSpan);

                    if (vtSpan.SequenceEqual(PSStyle.Instance.Reset))
                    {
                        // The Reset sequence will void all previous VT sequences.
                        vtSeqs.Clear();
                        wordHasVtSeqs = false;
                    }
                    else
                    {
                        vtSeqs.Append(vtSpan);
                        wordHasVtSeqs = true;
                    }

                    i += len - 1;
                    continue;
                }

                string delimiter = null;
                if (s[i] is ' ' or '\t' or SoftHyphen)
                {
                    // Soft hyphen = \u00AD - Should break, and add a hyphen if needed.
                    // If not needed for a break, hyphen should be absent.
                    delimiter = new string(s[i], 1);
                }
                else if (s[i] is HardHyphen or NonBreakingSpace)
                {
                    // Non-breaking space = \u00A0 - ideally shouldn't wrap.
                    // Hard hyphen = \u2011 - Should not break.
                    delimiter = string.Empty;
                }

                if (delimiter is not null)
                {
                    bool vtResetAdded = false;
                    if (wordHasVtSeqs && !sb.EndsWith(PSStyle.Instance.Reset))
                    {
                        vtResetAdded = true;
                        sb.Append(PSStyle.Instance.Reset);
                    }

                    var result = new GetWordsResult()
                    {
                        Word = sb.ToString(),
                        Delim = delimiter,
                        VtResetAdded = vtResetAdded
                    };

                    sb.Clear().Append(vtSeqs);
                    yield return result;
                }
                else
                {
                    sb.Append(s[i]);
                }
            }

            if (wordHasVtSeqs)
            {
                if (sb.Length == vtSeqs.Length)
                {
                    // This indicates 'sb' only contains all VT sequences, which may happen when the string ends with a word delimiter.
                    // For a word that contains VT sequence only, it's the same as an empty string to the formatting system,
                    // because nothing will actually be rendered.
                    // So, we use an empty string in this case to avoid unneeded string allocations.
                    sb.Clear();
                }
                else if (!sb.EndsWith(PSStyle.Instance.Reset))
                {
                    sb.Append(PSStyle.Instance.Reset);
                }
            }

            yield return new GetWordsResult() { Word = sb.ToString(), Delim = string.Empty };
        }

        internal static StringCollection GenerateLines(DisplayCells displayCells, string val, int firstLineLen, int followingLinesLen)
        {
            if (s_cultureCollection.Contains(CultureInfo.CurrentCulture.TwoLetterISOLanguageName))
            {
                return GenerateLinesWithWordWrap(displayCells, val, firstLineLen, followingLinesLen);
            }
            else
            {
                return GenerateLinesWithoutWordWrap(displayCells, val, firstLineLen, followingLinesLen);
            }
        }

        private static StringCollection GenerateLinesWithoutWordWrap(DisplayCells displayCells, string val, int firstLineLen, int followingLinesLen)
        {
            StringCollection retVal = new StringCollection();

            if (string.IsNullOrEmpty(val))
            {
                // if null or empty, just add and we are done
                retVal.Add(val);
                return retVal;
            }

            // break string on newlines and process each line separately
            List<string> lines = SplitLines(val);

            for (int k = 0; k < lines.Count; k++)
            {
                string currentLine = lines[k];

                if (currentLine == null || displayCells.Length(currentLine) <= firstLineLen)
                {
                    // we do not need to split further, just add
                    retVal.Add(currentLine);
                    continue;
                }

                // the string does not fit, so we have to wrap around on multiple lines
                // for each of these lines in the string, the first line will have
                // a (potentially) different length (indentation or hanging)

                // for each line, start a new state
                SplitLinesAccumulator accumulator = new SplitLinesAccumulator(retVal, firstLineLen, followingLinesLen);

                int offset = 0; // offset into the line we are splitting

                while (offset < currentLine.Length)
                {
                    // acquire the current active display line length (it can very from call to call)
                    int currentDisplayLen = accumulator.ActiveLen;

                    // determine if the current tail would fit or not

                    // for the remaining part of the string, determine its display cell count
                    int currentCellsToFit = displayCells.Length(currentLine, offset);

                    // determine if we fit into the line
                    int excessCells = currentCellsToFit - currentDisplayLen;

                    if (excessCells > 0)
                    {
                        // we are not at the end of the string, select a sub string
                        // that would fit in the remaining display length
                        int charactersToAdd = displayCells.TruncateTail(currentLine, offset, currentDisplayLen);

                        if (charactersToAdd <= 0)
                        {
                            // corner case: we have a two cell character and the current
                            // display length is one.
                            // add a single cell arbitrary character instead of the original
                            // one and keep going
                            charactersToAdd = 1;
                            accumulator.AddLine("?");
                        }
                        else
                        {
                            // of the given length, add it to the accumulator
                            accumulator.AddLine(currentLine.VtSubstring(offset, charactersToAdd));
                        }

                        // increase the offset by the # of characters added
                        offset += charactersToAdd;
                    }
                    else
                    {
                        // we reached the last (partial) line, we add it all
                        accumulator.AddLine(currentLine.VtSubstring(offset));
                        break;
                    }
                }
            }

            return retVal;
        }

        private sealed class SplitLinesAccumulator
        {
            internal SplitLinesAccumulator(StringCollection retVal, int firstLineLen, int followingLinesLen)
            {
                _retVal = retVal;
                _firstLineLen = firstLineLen;
                _followingLinesLen = followingLinesLen;
            }

            internal void AddLine(string s)
            {
                if (!_addedFirstLine)
                {
                    _addedFirstLine = true;
                }

                _retVal.Add(s);
            }

            internal int ActiveLen
            {
                get
                {
                    if (_addedFirstLine)
                        return _followingLinesLen;
                    return _firstLineLen;
                }
            }

            private readonly StringCollection _retVal;
            private bool _addedFirstLine;
            private readonly int _firstLineLen;
            private readonly int _followingLinesLen;
        }

        private static StringCollection GenerateLinesWithWordWrap(DisplayCells displayCells, string val, int firstLineLen, int followingLinesLen)
        {
            StringCollection retVal = new StringCollection();

            if (string.IsNullOrEmpty(val))
            {
                // if null or empty, just add and we are done
                retVal.Add(val);
                return retVal;
            }

            // break string on newlines and process each line separately
            List<string> lines = SplitLines(val);

            for (int k = 0; k < lines.Count; k++)
            {
                if (lines[k] == null || displayCells.Length(lines[k]) <= firstLineLen)
                {
                    // we do not need to split further, just add
                    retVal.Add(lines[k]);
                    continue;
                }

                int spacesLeft = firstLineLen;
                int lineWidth = firstLineLen;
                bool firstLine = true;
                StringBuilder singleLine = new StringBuilder();
                string resetStr = PSStyle.Instance.Reset;

                foreach (GetWordsResult word in GetWords(lines[k]))
                {
                    string wordToAdd = word.Word;
                    string suffix = null;

                    // Handle soft hyphen
                    if (word.Delim.Length == 1 && word.Delim[0] is SoftHyphen)
                    {
                        int wordWidthWithHyphen = displayCells.Length(wordToAdd) + displayCells.Length(SoftHyphen);

                        // Add hyphen only if necessary
                        if (wordWidthWithHyphen == spacesLeft)
                        {
                            suffix = "-";
                        }
                    }
                    else if (!string.IsNullOrEmpty(word.Delim))
                    {
                        suffix = word.Delim;
                    }

                    if (suffix is not null)
                    {
                        wordToAdd = word.VtResetAdded
                            ? wordToAdd.Insert(wordToAdd.Length - resetStr.Length, suffix)
                            : wordToAdd + suffix;
                    }

                    int wordWidth = displayCells.Length(wordToAdd);

                    // Handle zero width
                    if (lineWidth == 0)
                    {
                        if (firstLine)
                        {
                            firstLine = false;
                            lineWidth = followingLinesLen;
                        }

                        if (lineWidth == 0)
                        {
                            break;
                        }

                        spacesLeft = lineWidth;
                    }

                    // Word is wider than a single line
                    if (wordWidth > lineWidth)
                    {
                        Dictionary<int, int> vtRanges = null;
                        StringBuilder vtSeqs = null;

                        var valueStrDec = new ValueStringDecorated(wordToAdd);
                        if (valueStrDec.IsDecorated)
                        {
                            vtSeqs = new StringBuilder();
                            vtRanges = valueStrDec.EscapeSequenceRanges;
                        }

                        bool hasEscSeqs = false;
                        for (int i = 0; i < wordToAdd.Length; i++)
                        {
                            if (vtRanges?.TryGetValue(i, out int len) == true)
                            {
                                var vtSpan = wordToAdd.AsSpan(i, len);
                                singleLine.Append(vtSpan);
                                vtSeqs.Append(vtSpan);

                                hasEscSeqs = true;
                                i += len - 1;
                                continue;
                            }

                            char charToAdd = wordToAdd[i];
                            int charWidth = displayCells.Length(charToAdd);

                            // Corner case: we have a two cell character and the current display length is one.
                            // Add a single cell arbitrary character instead of the original one and keep going.
                            if (charWidth > lineWidth)
                            {
                                charToAdd = '?';
                                charWidth = 1;
                            }

                            if (charWidth > spacesLeft)
                            {
                                if (hasEscSeqs && !singleLine.EndsWith(resetStr))
                                {
                                    singleLine.Append(resetStr);
                                }

                                retVal.Add(singleLine.ToString());
                                singleLine.Clear().Append(vtSeqs).Append(charToAdd);

                                if (firstLine)
                                {
                                    firstLine = false;
                                    lineWidth = followingLinesLen;
                                }

                                spacesLeft = lineWidth - charWidth;
                            }
                            else
                            {
                                singleLine.Append(charToAdd);
                                spacesLeft -= charWidth;
                            }
                        }
                    }
                    else
                    {
                        if (wordWidth > spacesLeft)
                        {
                            retVal.Add(singleLine.ToString());
                            singleLine.Clear().Append(wordToAdd);

                            if (firstLine)
                            {
                                firstLine = false;
                                lineWidth = followingLinesLen;
                            }

                            spacesLeft = lineWidth - wordWidth;
                        }
                        else
                        {
                            singleLine.Append(wordToAdd);
                            spacesLeft -= wordWidth;
                        }
                    }
                }

                retVal.Add(singleLine.ToString());
            }

            return retVal;
        }

        /// <summary>
        /// Split a multiline string into an array of strings
        /// by honoring both \n and \r\n.
        /// </summary>
        /// <param name="s">String to split.</param>
        /// <returns>String array with the values.</returns>
        internal static List<string> SplitLines(string s)
        {
            if (string.IsNullOrEmpty(s) || !s.Contains('\n'))
            {
                return new List<string>(capacity: 1) { s?.Replace("\r", string.Empty) };
            }

            StringBuilder sb = new StringBuilder();
            List<string> list = new List<string>();

            StringBuilder vtSeqs = null;
            Dictionary<int, int> vtRanges = null;

            var valueStrDec = new ValueStringDecorated(s);
            if (valueStrDec.IsDecorated)
            {
                vtSeqs = new StringBuilder();
                vtRanges = valueStrDec.EscapeSequenceRanges;
            }

            bool hasVtSeqs = false;
            for (int i = 0; i < s.Length; i++)
            {
                if (vtRanges?.TryGetValue(i, out int len) == true)
                {
                    var vtSpan = s.AsSpan(i, len);
                    sb.Append(vtSpan);

                    if (vtSpan.SequenceEqual(PSStyle.Instance.Reset))
                    {
                        // The Reset sequence will void all previous VT sequences.
                        vtSeqs.Clear();
                        hasVtSeqs = false;
                    }
                    else
                    {
                        vtSeqs.Append(vtSpan);
                        hasVtSeqs = true;
                    }

                    i += len - 1;
                    continue;
                }

                char c = s[i];
                if (c == '\n')
                {
                    if (hasVtSeqs && !sb.EndsWith(PSStyle.Instance.Reset))
                    {
                        sb.Append(PSStyle.Instance.Reset);
                    }

                    list.Add(sb.ToString());
                    sb.Clear().Append(vtSeqs);
                }
                else if (c != '\r')
                {
                    sb.Append(c);
                }
            }

            if (hasVtSeqs)
            {
                if (sb.Length == vtSeqs.Length)
                {
                    // This indicates 'sb' only contains all VT sequences, which may happen when the string ends with '\n'.
                    // For a sub-string that contains VT sequence only, it's the same as an empty string to the formatting
                    // system, because nothing will actually be rendered.
                    // So, we use an empty string in this case to avoid unneeded string allocations.
                    sb.Clear();
                }
                else if (!sb.EndsWith(PSStyle.Instance.Reset))
                {
                    sb.Append(PSStyle.Instance.Reset);
                }
            }

            list.Add(sb.ToString());
            return list;
        }

        internal static string TruncateAtNewLine(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }

            int lineBreak = s.AsSpan().IndexOfAny('\n', '\r');

            if (lineBreak < 0)
            {
                return s;
            }

            return s.Substring(0, lineBreak) + PSObjectHelper.Ellipsis;
        }

        internal static string PadLeft(string val, int count)
        {
            return StringUtil.Padding(count) + val;
        }
    }
}
