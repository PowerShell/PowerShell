// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Text;

using Microsoft.PowerShell.Commands.Internal.Format;

namespace Microsoft.PowerShell.Commands.Internal.Format
{
    internal class TableWriter
    {
        /// <summary>
        /// Information about each column boundaries.
        /// </summary>
        private sealed class ColumnInfo
        {
            internal int startCol = 0;
            internal int width = 0;
            internal int alignment = TextAlignment.Left;
            internal bool HeaderMatchesProperty = true;
        }
        /// <summary>
        /// Class containing information about the tabular layout.
        /// </summary>
        private sealed class ScreenInfo
        {
            internal int screenColumns = 0;
            internal int screenRows = 0;

            internal const int separatorCharacterCount = 1;

            internal const int minimumScreenColumns = 5;

            internal const int minimumColumnWidth = 1;

            internal ColumnInfo[] columnInfo = null;
        }

        private ScreenInfo _si;

        private List<string> _header;

        internal static int ComputeWideViewBestItemsPerRowFit(int stringLen, int screenColumns)
        {
            if (stringLen <= 0 || screenColumns < 1)
                return 1;

            if (stringLen >= screenColumns)
            {
                // we too wide anyways, we might have to trim even for a single column
                return 1;
            }

            // we try to fit more than one: start increasing until we do not fit anymore
            int columnNumber = 1;
            while (true)
            {
                // would we fit with one more column?
                int nextValue = columnNumber + 1;
                // compute the width if we added the extra column
                int width = stringLen * nextValue + (nextValue - 1) * ScreenInfo.separatorCharacterCount;

                if (width >= screenColumns)
                {
                    // we would not fit, so we are done
                    return columnNumber;
                }

                // try another round
                columnNumber++;
            }
        }

        /// <summary>
        /// Initialize the table specifying the width of each column.
        /// </summary>
        /// <param name="leftMarginIndent">Left margin indentation.</param>
        /// <param name="screenColumns">Number of character columns on the screen.</param>
        /// <param name="columnWidths">Array of specified column widths.</param>
        /// <param name="alignment">Array of alignment flags.</param>
        /// <param name="headerMatchesProperty">Array of flags where the header label matches a property name.</param>
        /// <param name="suppressHeader">If true, suppress header printing.</param>
        /// <param name="screenRows">Number of rows on the screen.</param>
        internal void Initialize(int leftMarginIndent, int screenColumns, Span<int> columnWidths, ReadOnlySpan<int> alignment, ReadOnlySpan<bool> headerMatchesProperty, bool suppressHeader, int screenRows = int.MaxValue)
        {
            if (leftMarginIndent < 0)
            {
                leftMarginIndent = 0;
            }

            if (screenColumns - leftMarginIndent < ScreenInfo.minimumScreenColumns)
            {
                _disabled = true;
                return;
            }

            _startColumn = leftMarginIndent;
            _hideHeader = suppressHeader;

            // make sure the column widths are correct; if not, take appropriate action
            ColumnWidthManager manager = new ColumnWidthManager(
                screenColumns - leftMarginIndent,
                ScreenInfo.minimumColumnWidth,
                ScreenInfo.separatorCharacterCount);

            manager.CalculateColumnWidths(columnWidths);

            // if all the columns are hidden, just disable
            bool oneValid = false;

            for (int k = 0; k < columnWidths.Length; k++)
            {
                if (columnWidths[k] >= ScreenInfo.minimumColumnWidth)
                {
                    oneValid = true;
                    break;
                }
            }

            if (!oneValid)
            {
                _disabled = true;
                return;
            }

            // now set the run time data structures
            _si = new ScreenInfo();
            _si.screenColumns = screenColumns;
            _si.screenRows = screenRows;
            _si.columnInfo = new ColumnInfo[columnWidths.Length];

            int startCol = _startColumn;
            for (int k = 0; k < columnWidths.Length; k++)
            {
                _si.columnInfo[k] = new ColumnInfo();
                _si.columnInfo[k].startCol = startCol;
                _si.columnInfo[k].width = columnWidths[k];
                _si.columnInfo[k].alignment = alignment[k];
                if (!headerMatchesProperty.IsEmpty)
                {
                    _si.columnInfo[k].HeaderMatchesProperty = headerMatchesProperty[k];
                }

                startCol += columnWidths[k] + ScreenInfo.separatorCharacterCount;
            }
        }

        internal int GenerateHeader(string[] values, LineOutput lo)
        {
            if (_disabled || _hideHeader)
            {
                return 0;
            }
            else if (_header != null)
            {
                string style = PSStyle.Instance.Formatting.TableHeader;
                string reset = PSStyle.Instance.Reset;

                foreach (string line in _header)
                {
                    lo.WriteLine(line);
                }

                return _header.Count;
            }

            _header = new List<string>();

            // generate the row with the header labels
            GenerateRow(values, lo, true, null, lo.DisplayCells, _header, isHeader: true);

            // generate an array of "--" as header markers below
            // the column header labels
            string[] breakLine = new string[values.Length];
            for (int k = 0; k < breakLine.Length; k++)
            {
                // the column can be hidden
                if (_si.columnInfo[k].width <= 0)
                {
                    breakLine[k] = string.Empty;
                    continue;
                }
                // the title can be larger than the width
                int count = _si.columnInfo[k].width;
                if (!string.IsNullOrEmpty(values[k]))
                {
                    int labelDisplayCells = lo.DisplayCells.Length(values[k]);
                    if (labelDisplayCells < count)
                        count = labelDisplayCells;
                }
                // NOTE: we can do this because "-" is a single cell character
                // on all devices. If changed to some other character, this assumption
                // would be invalidated
                breakLine[k] = StringUtil.DashPadding(count);
            }

            GenerateRow(breakLine, lo, false, null, lo.DisplayCells, _header, isHeader: true);
            return _header.Count;
        }

        internal void GenerateRow(string[] values, LineOutput lo, bool multiLine, ReadOnlySpan<int> alignment, DisplayCells dc, List<string> generatedRows, bool isHeader = false)
        {
            if (_disabled)
            {
                return;
            }

            // build the current row alignment settings
            int cols = _si.columnInfo.Length;
            Span<int> currentAlignment = cols <= OutCommandInner.StackAllocThreshold ? stackalloc int[cols] : new int[cols];

            if (alignment.IsEmpty)
            {
                for (int i = 0; i < currentAlignment.Length; i++)
                {
                    currentAlignment[i] = _si.columnInfo[i].alignment;
                }
            }
            else
            {
                for (int i = 0; i < currentAlignment.Length; i++)
                {
                    if (alignment[i] == TextAlignment.Undefined)
                    {
                        currentAlignment[i] = _si.columnInfo[i].alignment;
                    }
                    else
                    {
                        currentAlignment[i] = alignment[i];
                    }
                }
            }

            string style = PSStyle.Instance.Formatting.TableHeader;
            string reset = PSStyle.Instance.Reset;

            if (multiLine)
            {
                foreach (string line in GenerateTableRow(values, currentAlignment, lo.DisplayCells, isHeader))
                {
                    generatedRows?.Add(line);
                    lo.WriteLine(line);
                }
            }
            else
            {
                string line = GenerateRow(values, currentAlignment, dc, isHeader);
                generatedRows?.Add(line);
                lo.WriteLine(line);
            }
        }

        private string[] GenerateTableRow(string[] values, ReadOnlySpan<int> alignment, DisplayCells ds, bool isHeader)
        {
            // select the active columns (skip hidden ones)
            Span<int> validColumnArray = _si.columnInfo.Length <= OutCommandInner.StackAllocThreshold ? stackalloc int[_si.columnInfo.Length] : new int[_si.columnInfo.Length];
            int validColumnCount = 0;
            for (int k = 0; k < _si.columnInfo.Length; k++)
            {
                if (_si.columnInfo[k].width > 0)
                {
                    validColumnArray[validColumnCount++] = k;
                }
            }

            if (validColumnCount == 0)
            {
                return null;
            }

            StringCollection[] scArray = new StringCollection[validColumnCount];
            bool addPadding = true;
            for (int k = 0; k < scArray.Length; k++)
            {
                // for the last column, don't pad it with trailing spaces
                if (k == scArray.Length - 1)
                {
                    addPadding = false;
                }

                // obtain a set of tokens for each field
                scArray[k] = GenerateMultiLineRowField(values[validColumnArray[k]], validColumnArray[k], alignment[validColumnArray[k]], ds, addPadding);

                // NOTE: the following padding operations assume that we
                // pad with a blank (or any character that ALWAYS maps to a single screen cell
                if (k > 0)
                {
                    // skipping the first ones, add a separator for concatenation
                    for (int j = 0; j < scArray[k].Count; j++)
                    {
                        scArray[k][j] = StringUtil.Padding(ScreenInfo.separatorCharacterCount) + scArray[k][j];
                    }
                }
                else
                {
                    // add indentation padding if needed
                    if (_startColumn > 0)
                    {
                        for (int j = 0; j < scArray[k].Count; j++)
                        {
                            scArray[k][j] = StringUtil.Padding(_startColumn) + scArray[k][j];
                        }
                    }
                }
            }

            // now we processed all the rows columns and we need to find the cell that occupies the most
            // rows
            int screenRows = 0;
            for (int k = 0; k < scArray.Length; k++)
            {
                if (scArray[k].Count > screenRows)
                {
                    screenRows = scArray[k].Count;
                }
            }

            // column headers can span multiple rows if the width of the column is shorter than the header text like:
            //
            // Long Header2 Head
            // Head         er3
            // er
            // ---- ------- ----
            // 1    2       3
            //
            // To ensure we don't add whitespace to the end, we need to determine the last column in each row with content
            System.Span<int> lastColWithContent = screenRows <= OutCommandInner.StackAllocThreshold ? stackalloc int[screenRows] : new int[screenRows];
            for (int row = 0; row < screenRows; row++)
            {
                for (int col = 0; col < scArray.Length; col++)
                {
                    if (scArray[col].Count > row)
                    {
                        lastColWithContent[row] = col;
                    }
                }
            }

            // add padding for the columns that are shorter
            for (int col = 0; col < scArray.Length; col++)
            {
                int paddingBlanks = 0;

                // don't pad if last column
                if (col < scArray.Length - 1)
                {
                    paddingBlanks = _si.columnInfo[validColumnArray[col]].width;
                    if (col > 0)
                    {
                        paddingBlanks += ScreenInfo.separatorCharacterCount;
                    }
                    else
                    {
                        paddingBlanks += _startColumn;
                    }
                }

                int paddingEntries = screenRows - scArray[col].Count;
                if (paddingEntries > 0)
                {
                    for (int row = screenRows - paddingEntries; row < screenRows; row++)
                    {
                        // if the column is beyond the last column with content, just use empty string
                        if (col > lastColWithContent[row])
                        {
                            scArray[col].Add(string.Empty);
                        }
                        else
                        {
                            scArray[col].Add(StringUtil.Padding(paddingBlanks));
                        }
                    }
                }
            }

            // finally, build an array of strings
            string[] rows = new string[screenRows];
            for (int row = 0; row < screenRows; row++)
            {
                StringBuilder sb = new StringBuilder();

                // for a given row, walk the columns
                for (int col = 0; col < scArray.Length; col++)
                {
                    string value = scArray[col][row];

                    // if the column is the last column with content, we need to trim trailing whitespace, unless there is only one row
                    if (col == lastColWithContent[row] && screenRows > 1)
                    {
                        value = value.TrimEnd();
                    }

                    if (isHeader)
                    {
                        if (_si.columnInfo[col].HeaderMatchesProperty)
                        {
                            sb.Append(PSStyle.Instance.Formatting.TableHeader);
                        }
                        else if (value.Length > 0)
                        {
                            // after the first column, each additional column starts with a whitespace for separation
                            value = value.Insert(col == 0 ? 0 : 1, PSStyle.Instance.Formatting.CustomTableHeaderLabel);
                        }
                    }

                    sb.Append(value);

                    if (isHeader)
                    {
                        sb.Append(PSStyle.Instance.Reset);
                    }
                }

                rows[row] = sb.ToString();
            }

            return rows;
        }

        private StringCollection GenerateMultiLineRowField(string val, int k, int alignment, DisplayCells dc, bool addPadding)
        {
            StringCollection sc = StringManipulationHelper.GenerateLines(dc, val,
                                        _si.columnInfo[k].width, _si.columnInfo[k].width);
            if (addPadding || alignment == TextAlignment.Right || alignment == TextAlignment.Center)
            {
                // if length is shorter, do some padding
                for (int col = 0; col < sc.Count; col++)
                {
                    if (dc.Length(sc[col]) < _si.columnInfo[k].width)
                        sc[col] = GenerateRowField(sc[col], _si.columnInfo[k].width, alignment, dc, addPadding);
                }
            }

            return sc;
        }

        private string GenerateRow(string[] values, ReadOnlySpan<int> alignment, DisplayCells dc, bool isHeader)
        {
            StringBuilder sb = new StringBuilder();

            bool addPadding = true;
            for (int k = 0; k < _si.columnInfo.Length; k++)
            {
                // don't pad the last column
                if (k == _si.columnInfo.Length - 1)
                {
                    addPadding = false;
                }

                if (_si.columnInfo[k].width <= 0)
                {
                    // skip columns that are not at least a single character wide
                    continue;
                }

                // NOTE: the following padding operations assume that we
                // pad with a blank (or any character that ALWAYS maps to a single screen cell
                if (k > 0)
                {
                    sb.Append(StringUtil.Padding(ScreenInfo.separatorCharacterCount));
                }
                else
                {
                    // add indentation padding if needed
                    if (_startColumn > 0)
                    {
                        sb.Append(StringUtil.Padding(_startColumn));
                    }
                }

                string rowField = GenerateRowField(values[k], _si.columnInfo[k].width, alignment[k], dc, addPadding);
                if (isHeader)
                {
                    sb.Append(PSStyle.Instance.Formatting.TableHeader);
                }

                sb.Append(rowField);

                if (isHeader || (rowField is not null && rowField.Contains(ValueStringDecorated.ESC) && !rowField.AsSpan().TrimEnd().EndsWith(PSStyle.Instance.Reset)))
                {
                    // Reset the console output if the content of this column contains ESC
                    sb.Append(PSStyle.Instance.Reset);
                }
            }

            return sb.ToString();
        }

        private static string GenerateRowField(string val, int width, int alignment, DisplayCells dc, bool addPadding)
        {
            // make sure the string does not have any embedded <CR> in it
            string s = StringManipulationHelper.TruncateAtNewLine(val);
            int currentValueDisplayLength = dc.Length(s);

            if (currentValueDisplayLength < width)
            {
                // the string is shorter than the width of the column
                // need to pad with blanks to reach the desired width
                int padCount = width - currentValueDisplayLength;
                switch (alignment)
                {
                    case TextAlignment.Right:
                        {
                            s = StringUtil.Padding(padCount) + s;
                        }

                        break;

                    case TextAlignment.Center:
                        {
                            // add a bit at both ends of the string
                            int padLeft = padCount / 2;
                            int padRight = padCount - padLeft;

                            s = StringUtil.Padding(padLeft) + s;
                            if (addPadding)
                            {
                                s += StringUtil.Padding(padRight);
                            }
                        }

                        break;

                    default:
                        {
                            if (addPadding)
                            {
                                // left align is the default
                                s += StringUtil.Padding(padCount);
                            }
                        }

                        break;
                }
            }
            else if (currentValueDisplayLength > width)
            {
                // the string is longer than the width of the column
                // truncate and add ellipsis if it's too long
                int truncationDisplayLength = width - EllipsisSize;

                if (truncationDisplayLength > 0)
                {
                    // we have space for the ellipsis, add it
                    switch (alignment)
                    {
                        case TextAlignment.Right:
                            {
                                // get from "abcdef" to "...f"
                                s = s.VtSubstring(
                                    startOffset: dc.TruncateHead(s, truncationDisplayLength),
                                    prependStr: PSObjectHelper.EllipsisStr,
                                    appendStr: null);
                            }

                            break;

                        default:
                            {
                                // left align is the default
                                // get from "abcdef" to "a..."
                                s = s.VtSubstring(
                                    startOffset: 0,
                                    length: dc.TruncateTail(s, truncationDisplayLength),
                                    prependStr: null,
                                    appendStr: PSObjectHelper.EllipsisStr);
                            }

                            break;
                    }
                }
                else
                {
                    // not enough space for the ellipsis, just truncate at the width
                    switch (alignment)
                    {
                        case TextAlignment.Right:
                            {
                                // get from "abcdef" to "f"
                                s = s.VtSubstring(startOffset: dc.TruncateHead(s, width));
                            }

                            break;

                        default:
                            {
                                // left align is the default
                                // get from "abcdef" to "a"
                                s = s.VtSubstring(startOffset: 0, length: dc.TruncateTail(s, width));
                            }

                            break;
                    }
                }
            }

            // we need to take into consideration that truncation left the string one
            // display cell short if a double cell character got truncated
            // in this case, we need to pad with a blank
            int finalValueDisplayLength = dc.Length(s);
            if (finalValueDisplayLength == width)
            {
                return s;
            }

            switch (alignment)
            {
                case TextAlignment.Right:
                    {
                        s = " " + s;
                    }

                    break;

                case TextAlignment.Center:
                    {
                        if (addPadding)
                        {
                            s += " ";
                        }
                    }

                    break;

                default:
                    {
                        // left align is the default
                        if (addPadding)
                        {
                            s += " ";
                        }
                    }

                    break;
            }

            return s;
        }

        private const int EllipsisSize = 1;

        private bool _disabled = false;

        private bool _hideHeader = false;

        private int _startColumn = 0;
    }
}
