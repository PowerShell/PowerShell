/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.Collections.Specialized;
using System.Text;

namespace Microsoft.PowerShell.Commands.Internal.Format
{
    internal class TableWriter
    {
        /// <summary>
        /// Information about each column boundaries
        /// </summary>
        private class ColumnInfo
        {
            internal int startCol = 0;
            internal int width = 0;
            internal int alignment = TextAlignment.Left;
        }
        /// <summary>
        /// Class containing information about the tabular layout
        /// </summary>
        private class ScreenInfo
        {
            internal int screenColumns = 0;

            internal const int separatorCharacterCount = 1;

            internal const int minimumScreenColumns = 5;

            internal const int minimumColumnWidth = 1;

            internal ColumnInfo[] columnInfo = null;
        }

        private ScreenInfo _si;

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
        /// Initialize the table specifying the width of each column
        /// </summary>
        /// <param name="leftMarginIndent">left margin indentation</param>
        /// <param name="screenColumns">number of character columns on the screen</param>
        /// <param name="columnWidths">array of specified column widths</param>
        /// <param name="alignment">array of alignment flags</param>
        /// <param name="suppressHeader">if true, suppress header printing</param>
        internal void Initialize(int leftMarginIndent, int screenColumns, int[] columnWidths, int[] alignment, bool suppressHeader)
        {
            //Console.WriteLine("         1         2         3         4         5         6         7");
            //Console.WriteLine("01234567890123456789012345678901234567890123456789012345678901234567890123456789");

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
            ColumnWidthManager manager = new ColumnWidthManager(screenColumns - leftMarginIndent,
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
            _si.columnInfo = new ColumnInfo[columnWidths.Length];

            int startCol = _startColumn;
            for (int k = 0; k < columnWidths.Length; k++)
            {
                _si.columnInfo[k] = new ColumnInfo();
                _si.columnInfo[k].startCol = startCol;
                _si.columnInfo[k].width = columnWidths[k];
                _si.columnInfo[k].alignment = alignment[k];
                startCol += columnWidths[k] + ScreenInfo.separatorCharacterCount;
                //Console.WriteLine("start = {0} width = {1}", si.columnInfo[k].startCol, si.columnInfo[k].width);
            }
        }

        internal void GenerateHeader(string[] values, LineOutput lo)
        {
            if (_disabled)
                return;

            if (_hideHeader)
                return;

            // generate the row with the header labels
            GenerateRow(values, lo, true, null, lo.DisplayCells);

            // generate an array of "--" as header markers below
            // the column header labels
            string[] breakLine = new string[values.Length];
            for (int k = 0; k < _si.columnInfo.Length; k++)
            {
                // the column can be hidden
                if (_si.columnInfo[k].width <= 0)
                {
                    breakLine[k] = "";
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
                breakLine[k] = new string('-', count);
            }
            GenerateRow(breakLine, lo, false, null, lo.DisplayCells);
        }

        internal void GenerateRow(string[] values, LineOutput lo, bool multiLine, int[] alignment, DisplayCells dc)
        {
            if (_disabled)
                return;

            // build the current row alignment settings
            int cols = _si.columnInfo.Length;
            int[] currentAlignment = new int[cols];

            if (alignment == null)
            {
                for (int i = 0; i < cols; i++)
                {
                    currentAlignment[i] = _si.columnInfo[i].alignment;
                }
            }
            else
            {
                for (int i = 0; i < cols; i++)
                {
                    if (alignment[i] == TextAlignment.Undefined)
                        currentAlignment[i] = _si.columnInfo[i].alignment;
                    else
                        currentAlignment[i] = alignment[i];
                }
            }


            if (multiLine)
            {
                string[] lines = GenerateTableRow(values, currentAlignment, lo.DisplayCells);

                for (int k = 0; k < lines.Length; k++)
                {
                    lo.WriteLine(lines[k]);
                }
            }
            else
            {
                lo.WriteLine(GenerateRow(values, currentAlignment, dc));
            }
        }

        private string[] GenerateTableRow(string[] values, int[] alignment, DisplayCells ds)
        {
            // select the active columns (skip hidden ones)
            int[] validColumnArray = new int[_si.columnInfo.Length];
            int validColumnCount = 0;
            for (int k = 0; k < _si.columnInfo.Length; k++)
            {
                if (_si.columnInfo[k].width > 0)
                {
                    validColumnArray[validColumnCount++] = k;
                }
            }

            if (validColumnCount == 0)
                return null;


            StringCollection[] scArray = new StringCollection[validColumnCount];
            for (int k = 0; k < scArray.Length; k++)
            {
                // obtain a set of tokens for each field
                scArray[k] = GenerateMultiLineRowField(values[validColumnArray[k]], validColumnArray[k],
                                                                        alignment[validColumnArray[k]], ds);

                // NOTE: the following padding operations assume that we 
                // pad with a blank (or any character that ALWAYS maps to a single screen cell
                if (k > 0)
                {
                    // skipping the first ones, add a separator for catenation
                    for (int j = 0; j < scArray[k].Count; j++)
                    {
                        scArray[k][j] = new string(' ', ScreenInfo.separatorCharacterCount) + scArray[k][j];
                    }
                }
                else
                {
                    // add indentation padding if needed
                    if (_startColumn > 0)
                    {
                        for (int j = 0; j < scArray[k].Count; j++)
                        {
                            scArray[k][j] = new string(' ', _startColumn) + scArray[k][j];
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
                    screenRows = scArray[k].Count;
            }

            // add padding for the columns that are shorter
            for (int col = 0; col < scArray.Length; col++)
            {
                int paddingBlanks = _si.columnInfo[validColumnArray[col]].width;
                if (col > 0)
                    paddingBlanks += ScreenInfo.separatorCharacterCount;
                else
                {
                    paddingBlanks += _startColumn;
                }
                int paddingEntries = screenRows - scArray[col].Count;
                if (paddingEntries > 0)
                {
                    for (int j = 0; j < paddingEntries; j++)
                    {
                        scArray[col].Add(new string(' ', paddingBlanks));
                    }
                }
            }

            // finally, build an array of strings
            string[] rows = new string[screenRows];
            for (int row = 0; row < rows.Length; row++)
            {
                StringBuilder sb = new StringBuilder();
                // for a give row, walk the columns
                for (int col = 0; col < scArray.Length; col++)
                {
                    sb.Append(scArray[col][row]);
                }
                rows[row] = sb.ToString();
            }
            return rows;
        }

        private StringCollection GenerateMultiLineRowField(string val, int k, int aligment, DisplayCells dc)
        {
            StringCollection sc = StringManipulationHelper.GenerateLines(dc, val,
                                        _si.columnInfo[k].width, _si.columnInfo[k].width);
            // if length is shorter, do some padding
            for (int col = 0; col < sc.Count; col++)
            {
                if (dc.Length(sc[col]) < _si.columnInfo[k].width)
                    sc[col] = GenerateRowField(sc[col], _si.columnInfo[k].width, aligment, dc);
            }
            return sc;
        }


        private string GenerateRow(string[] values, int[] alignment, DisplayCells dc)
        {
            StringBuilder sb = new StringBuilder();

            for (int k = 0; k < _si.columnInfo.Length; k++)
            {
                if (_si.columnInfo[k].width <= 0)
                {
                    // skip columns that are not at least a single character wide
                    continue;
                }
                int newRowIndex = sb.Length;

                // NOTE: the following padding operations assume that we 
                // pad with a blank (or any character that ALWAYS maps to a single screen cell
                if (k > 0)
                {
                    sb.Append(new string(' ', ScreenInfo.separatorCharacterCount));
                }
                else
                {
                    // add indentation padding if needed
                    if (_startColumn > 0)
                    {
                        sb.Append(new string(' ', _startColumn));
                    }
                }
                sb.Append(GenerateRowField(values[k], _si.columnInfo[k].width, alignment[k], dc));
            }
            return sb.ToString();
        }


        private static string GenerateRowField(string val, int width, int alignment, DisplayCells dc)
        {
            // make sure the string does not have any embedded <CR> in it
            string s = StringManipulationHelper.TruncateAtNewLine(val) ?? "";

            string currentValue = s;
            int currentValueDisplayLength = dc.Length(currentValue);

            if (currentValueDisplayLength < width)
            {
                // the string is shorter than the width of the column
                // need to pad with with blanks to reach the desired width
                int padCount = width - currentValueDisplayLength;
                switch (alignment)
                {
                    case TextAlignment.Right:
                        {
                            s = new string(' ', padCount) + s;
                        }
                        break;

                    case TextAlignment.Center:
                        {
                            // add a bit at both ends of the string
                            int padLeft = padCount / 2;
                            int padRight = padCount - padLeft;

                            s = new string(' ', padLeft) + s + new string(' ', padRight);
                        }
                        break;

                    default:
                        {
                            // left align is the default
                            s += new string(' ', padCount);
                        }
                        break;
                }
            }
            else if (currentValueDisplayLength > width)
            {
                // the string is longer than the width of the column
                // truncate and add ellipsis if it's too long
                int truncationDisplayLength = width - ellipsis.Length;

                if (truncationDisplayLength > 0)
                {
                    // we have space for the ellipsis, add it
                    switch (alignment)
                    {
                        case TextAlignment.Right:
                            {
                                // get from "abcdef" to "...f"    
                                int tailCount = dc.GetTailSplitLength(s, truncationDisplayLength);
                                s = s.Substring(s.Length - tailCount);
                                s = ellipsis + s;
                            }
                            break;

                        case TextAlignment.Center:
                            {
                                // get from "abcdef" to "a..."
                                s = s.Substring(0, dc.GetHeadSplitLength(s, truncationDisplayLength));
                                s += ellipsis;
                            }
                            break;

                        default:
                            {
                                // left align is the default
                                // get from "abcdef" to "a..."
                                s = s.Substring(0, dc.GetHeadSplitLength(s, truncationDisplayLength));
                                s += ellipsis;
                            }
                            break;
                    }
                }
                else
                {
                    // not enough space for the ellipsis, just truncate at the width
                    int len = width;

                    switch (alignment)
                    {
                        case TextAlignment.Right:
                            {
                                // get from "abcdef" to "f"
                                int tailCount = dc.GetTailSplitLength(s, len);
                                s = s.Substring(s.Length - tailCount, tailCount);
                            }
                            break;

                        case TextAlignment.Center:
                            {
                                // get from "abcdef" to "a"
                                s = s.Substring(0, dc.GetHeadSplitLength(s, len));
                            }
                            break;

                        default:
                            {
                                // left align is the default
                                // get from "abcdef" to "a"
                                s = s.Substring(0, dc.GetHeadSplitLength(s, len));
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
            // we have to pad
            System.Diagnostics.Debug.Assert(finalValueDisplayLength == width - 1, "padding is not correct");
            switch (alignment)
            {
                case TextAlignment.Right:
                    {
                        s = " " + s;
                    }
                    break;

                case TextAlignment.Center:
                    {
                        s += " ";
                    }
                    break;

                default:
                    {
                        // left align is the default
                        s += " ";
                    }
                    break;
            }

            return s;
        }

        private const string ellipsis = "...";

        private bool _disabled = false;

        private bool _hideHeader = false;

        private int _startColumn = 0;
    }
}
