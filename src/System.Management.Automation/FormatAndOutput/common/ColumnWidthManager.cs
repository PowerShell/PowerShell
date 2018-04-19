// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.PowerShell.Commands.Internal.Format
{
    /// <summary>
    /// class providing an algorithm for automatic resizing
    /// of table columns
    /// </summary>
    internal sealed class ColumnWidthManager
    {
        /// <summary>
        /// class providing an algorithm for automatic resizing
        /// </summary>
        /// <param name="tableWidth">overall width of the table in characters</param>
        /// <param name="minimumColumnWidth">minimum usable column width</param>
        /// <param name="separatorWidth">number of separator characters</param>
        internal ColumnWidthManager(int tableWidth, int minimumColumnWidth, int separatorWidth)
        {
            _tableWidth = tableWidth;
            _minimumColumnWidth = minimumColumnWidth;
            _separatorWidth = separatorWidth;
        }

        /// <summary>
        /// calculate the widths by applying some heuristics to get them to fit on the
        /// allotted table width. It first assigns widths to the columns that do not have a specified
        /// width, then it checks if the total width exceeds the screen widths. If so, it proceeds
        /// with column elimination, starting from the right most column
        /// </summary>
        /// <param name="columnWidths">array of column widths to appropriately size</param>
        internal void CalculateColumnWidths(Span<int> columnWidths)
        {
            if (AssignColumnWidths(columnWidths))
            {
                // we do not have any trimming to do, we are done
                return;
            }

            // total width exceeds screen width, go on with trimming
            TrimToFit(columnWidths);
        }

        /// <summary>
        /// do not remove columns, just assign widths to columns that have a zero width
        /// (meaning unassigned)
        /// </summary>
        /// <param name="columnWidths">columns to process</param>
        /// <returns>true if there was a fit, false if there is need for trimming</returns>
        private bool AssignColumnWidths(Span<int> columnWidths)
        {
            // run a quick check to see if all the columns have a specified width,
            // if so, we are done
            bool allSpecified = true;
            int maxInitialWidthSum = 0;

            for (int k = 0; k < columnWidths.Length; k++)
            {
                if (columnWidths[k] <= 0)
                {
                    allSpecified = false;
                    break;
                }

                maxInitialWidthSum += columnWidths[k];
            }

            if (allSpecified)
            {
                // compute the total table width (columns and separators)
                maxInitialWidthSum += _separatorWidth * (columnWidths.Length - 1);
                if (maxInitialWidthSum <= _tableWidth)
                {
                    // we fit with all the columns specified
                    return true;
                }
                // we do not fit, we will have to trim
                return false;
            }

            // we have columns with no width assigned
            // remember the columns we are trying to size
            // assign them the minimum column size
            bool[] fixedColumn = new bool[columnWidths.Length];
            for (int k = 0; k < columnWidths.Length; k++)
            {
                fixedColumn[k] = columnWidths[k] > 0;
                if (columnWidths[k] == 0)
                    columnWidths[k] = _minimumColumnWidth;
            }

            // see if we fit
            int currentTableWidth = CurrentTableWidth(columnWidths);
            int availableWidth = _tableWidth - currentTableWidth;

            if (availableWidth < 0)
            {
                // if the total width is too much, we will have to remove some columns
                return false;
            }
            else if (availableWidth == 0)
            {
                // we just fit
                return true;
            }

            // we still have room and we want to add more width

            while (availableWidth > 0)
            {
                for (int k = 0; k < columnWidths.Length; k++)
                {
                    if (fixedColumn[k])
                        continue;

                    columnWidths[k]++;
                    availableWidth--;
                    if (availableWidth == 0)
                        break;
                }
            } // while

            return true; // we fit
        }

        /// <summary>
        /// trim columns if the total column width is too much for the screen.
        /// </summary>
        /// <param name="columnWidths">column widths to trim</param>
        private void TrimToFit(Span<int> columnWidths)
        {
            while (true)
            {
                int currentTableWidth = CurrentTableWidth(columnWidths);
                int widthInExcess = currentTableWidth - _tableWidth;
                if (widthInExcess <= 0)
                {
                    return; // we are done, because we fit
                }

                // we need to remove or shrink the last visible column
                int lastVisibleColumn = GetLastVisibleColumn(columnWidths);

                if (lastVisibleColumn < 0)
                    return; // nothing left to hide, because all the columns are hidden

                // try to trim the last column to fit
                int newLastVisibleColumnWidth = columnWidths[lastVisibleColumn] - widthInExcess;

                if (newLastVisibleColumnWidth < _minimumColumnWidth)
                {
                    // cannot fit it in, just hide
                    columnWidths[lastVisibleColumn] = -1;
                    continue;
                }
                else
                {
                    // shrink the column to fit
                    columnWidths[lastVisibleColumn] = newLastVisibleColumnWidth;
                }
            }
        }

        /// <summary>
        /// computes the total table width from the column width array
        /// </summary>
        /// <param name="columnWidths">column widths array</param>
        /// <returns></returns>
        private int CurrentTableWidth(Span<int> columnWidths)
        {
            int sum = 0;
            int visibleColumns = 0;

            for (int k = 0; k < columnWidths.Length; k++)
            {
                if (columnWidths[k] > 0)
                {
                    sum += columnWidths[k];
                    visibleColumns++;
                }
            }

            return sum + _separatorWidth * (visibleColumns - 1);
        }

        /// <summary>
        /// get the last visible column (i.e. with a width >= 0)
        /// </summary>
        /// <param name="columnWidths">column widths array</param>
        /// <returns>index of the last visible column, -1 if none</returns>
        private static int GetLastVisibleColumn(Span<int> columnWidths)
        {
            for (int k = 0; k < columnWidths.Length; k++)
            {
                if (columnWidths[k] < 0)
                    return k - 1;
            }

            return columnWidths.Length - 1;
        }

        private int _tableWidth;
        private int _minimumColumnWidth;
        private int _separatorWidth;
    }
}
