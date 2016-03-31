/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Collections.Specialized;
using System.Diagnostics;

namespace Microsoft.PowerShell.Commands.Internal.Format
{
    /// <summary>
    /// class to write object properties in list form by using
    /// the host screen interfaces
    /// </summary>
    internal class ListWriter
    {

        /// <summary>
        /// labels already padded with blanks, separator characters, etc.
        /// </summary>
         private string[] propertyLabels;
        
        /// <summary>
        /// display length of the property labels in the array (all the same length)
        /// </summary>
        private int propertyLabelsDisplayLength = 0;

        /// <summary>
        /// column width of the screen
        /// </summary>
        private int columnWidth = 0;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyNames">names of the properties to display</param>
        /// <param name="screenColumnWidth">column width of the screen</param>
        /// <param name="dc">instance of the DisplayCells helper object</param>
        internal void Initialize (string[] propertyNames, int screenColumnWidth, DisplayCells dc)
        {
            columnWidth = screenColumnWidth;
            if (propertyNames == null || propertyNames.Length == 0)
            {
                // there is nothing to show
                this.disabled = true;
                return;
            }

            this.disabled = false;

            Debug.Assert(propertyNames != null, "propertyNames is null");
            Debug.Assert(propertyNames.Length > 0, "propertyNames has zero length");

            // assess the useful widths
            if ((screenColumnWidth - Separator.Length - MinFieldWidth - MinLabelWidth) < 0)
            {
                // we do not have enough space for any meaningful display
                this.disabled = true;
                return;
            }

            // check if we have to truncate the labels
            int maxAllowableLabelLength = screenColumnWidth - Separator.Length - MinFieldWidth;

            // find out the max display length (cell count) of the property names
            this.propertyLabelsDisplayLength = 0; // reset max

            // cache the cell lengths for each property
            int[] propertyNameCellCounts = new int[propertyNames.Length];
            for (int k=0; k<propertyNames.Length; k++)
            {
                Debug.Assert(propertyNames[k] != null, "propertyNames[k] is null");
                propertyNameCellCounts[k] = dc.Length(propertyNames[k]);
                if (propertyNameCellCounts[k] > this.propertyLabelsDisplayLength)
                    this.propertyLabelsDisplayLength = propertyNameCellCounts[k];
            }

            if (this.propertyLabelsDisplayLength > maxAllowableLabelLength)
            {
                // need to truncate
                this.propertyLabelsDisplayLength = maxAllowableLabelLength;
            }

            this.propertyLabels = new string[propertyNames.Length];

            for (int k=0; k<propertyNames.Length; k++)
            {
                if (propertyNameCellCounts[k] < this.propertyLabelsDisplayLength)
                {
                    // shorter than the max, add padding
                    propertyLabels[k] = propertyNames[k] + new string(' ', this.propertyLabelsDisplayLength - propertyNameCellCounts[k]);
                }
                else if (propertyNameCellCounts[k] > this.propertyLabelsDisplayLength)
                {
                    // longer than the max, clip
                    propertyLabels[k] = propertyNames[k].Substring(0, dc.GetHeadSplitLength(propertyNames[k], this.propertyLabelsDisplayLength));
                }
                else
                {
                    propertyLabels[k] = propertyNames[k];
                }

                propertyLabels[k] += Separator;
            }
            this.propertyLabelsDisplayLength += Separator.Length;
        }

        /// <summary>
        /// write the values of the properties of an object
        /// </summary>
        /// <param name="values">array with the values in form of formatted strings</param>
        /// <param name="lo">LineOutput interface to write to</param>
        internal void WriteProperties (string[] values, LineOutput lo)
        {
            if (this.disabled)
                return;

            string[] valuesToPrint = null;
            if (values == null)
            {
                // we have nothing, but we have to create an empty array
                valuesToPrint = new string[this.propertyLabels.Length];
                for (int k = 0; k < this.propertyLabels.Length; k++)
                    valuesToPrint[k] = "";
            }
            else if (values.Length < this.propertyLabels.Length)
            {
                // need to pad to the end of the array
                valuesToPrint = new string[this.propertyLabels.Length];
                for (int k = 0; k < this.propertyLabels.Length; k++)
                {
                    if (k < values.Length)
                        valuesToPrint[k] = values[k];
                    else
                        valuesToPrint[k] = "";
                }
            }
            else if (values.Length > this.propertyLabels.Length)
            {
                // need to trim
                valuesToPrint = new string[this.propertyLabels.Length];
                for (int k = 0; k < this.propertyLabels.Length; k++)
                    valuesToPrint[k] = values[k];
            }
            else
            {
                // perfect match
                valuesToPrint = values;
            }

            Debug.Assert(lo != null, "LineOutput is null");

            for (int k=0; k<propertyLabels.Length; k++)
            {
                WriteProperty (k, valuesToPrint[k], lo);
            }
        }

        /// <summary>
        /// helper, writing a single property to the screen.
        /// It wraps the value of the property if it is tool long to fit
        /// </summary>
        /// <param name="k">index of property to write</param>
        /// <param name="propertyValue">string value of the property to write</param>
        /// <param name="lo">LineOutput interface to write to</param>
        private void WriteProperty (int k, string propertyValue, LineOutput lo)
        {
            if (propertyValue == null)
                propertyValue = "";

            // make sure we honor embedded newlines
            string[] lines = StringManipulationHelper.SplitLines(propertyValue);

            // padding to use in the lines after the first
            string padding = null;

            for (int i = 0; i < lines.Length; i++)
            {
                string prependString = null;

                if (i == 0)
                    prependString = propertyLabels[k];
                else
                {
                    if (padding == null)
                        padding = prependString = new string(' ', propertyLabelsDisplayLength);

                    prependString = padding;
                }

                WriteSingleLineHelper (prependString, lines[i], lo);
            }

        }

        /// <summary>
        /// internal helper to split a line that is too long to fit and pad it to the left
        /// with a given string
        /// </summary>
        /// <param name="prependString">string to add to the left</param>
        /// <param name="line">line to print</param>
        /// <param name="lo">LineOuput to write to</param>
        private void WriteSingleLineHelper (string prependString, string line, LineOutput lo)
        {
            if (line == null)
                line = "";

            // compute the width of the field for the value string (in screen cells)
            int fieldCellCount = this.columnWidth - propertyLabelsDisplayLength;

            // split the lines 
            StringCollection sc = StringManipulationHelper.GenerateLines(lo.DisplayCells, line, fieldCellCount, fieldCellCount);

            // padding to use in the lines after the first
            string padding = new string(' ', propertyLabelsDisplayLength);

            // display the string collection
            for (int k = 0; k < sc.Count; k++)
            {
                if (k == 0)
                {
                    lo.WriteLine(prependString + sc[k]);
                }
                else
                {
                    lo.WriteLine(padding + sc[k]);
                }
            }
        }

        /// <summary>
        /// set to true when the width of the screen is too small to do anything useful
        /// </summary>
        private bool disabled = false;

        private const string Separator = " : ";

        /// <summary>
        /// minimum width for the property label field
        /// </summary>
        private const int MinLabelWidth = 1;

        /// <summary>
        /// minimum width for the property value field
        /// </summary>
        private const int MinFieldWidth = 1;
    }
}
