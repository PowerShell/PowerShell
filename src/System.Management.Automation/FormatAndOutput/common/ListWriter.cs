// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Internal;

namespace Microsoft.PowerShell.Commands.Internal.Format
{
    /// <summary>
    /// Class to write object properties in list form by using
    /// the host screen interfaces.
    /// </summary>
    internal class ListWriter
    {
        /// <summary>
        /// Labels already padded with blanks, separator characters, etc.
        /// </summary>
        private string[] _propertyLabels;

        /// <summary>
        /// Display length of the property labels in the array (all the same length)
        /// </summary>
        private int _propertyLabelsDisplayLength = 0;

        /// <summary>
        /// Column width of the screen.
        /// </summary>
        private int _columnWidth = 0;

        /// <summary>
        /// </summary>
        /// <param name="propertyNames">Names of the properties to display.</param>
        /// <param name="screenColumnWidth">Column width of the screen.</param>
        /// <param name="dc">Instance of the DisplayCells helper object.</param>
        internal void Initialize(string[] propertyNames, int screenColumnWidth, DisplayCells dc)
        {
            _columnWidth = screenColumnWidth;
            if (propertyNames == null || propertyNames.Length == 0)
            {
                // there is nothing to show
                _disabled = true;
                return;
            }

            _disabled = false;

            Debug.Assert(propertyNames != null, "propertyNames is null");
            Debug.Assert(propertyNames.Length > 0, "propertyNames has zero length");

            // assess the useful widths
            if ((screenColumnWidth - Separator.Length - MinFieldWidth - MinLabelWidth) < 0)
            {
                // we do not have enough space for any meaningful display
                _disabled = true;
                return;
            }

            // check if we have to truncate the labels
            int maxAllowableLabelLength = screenColumnWidth - Separator.Length - MinFieldWidth;
            if (InternalTestHooks.ForceFormatListFixedLabelWidth)
            {
                maxAllowableLabelLength = 10;
            }

            // find out the max display length (cell count) of the property names
            _propertyLabelsDisplayLength = 0; // reset max

            // cache the cell lengths for each property
            Span<int> propertyNameCellCounts = propertyNames.Length <= OutCommandInner.StackAllocThreshold ? stackalloc int[propertyNames.Length] : new int[propertyNames.Length];
            for (int k = 0; k < propertyNames.Length; k++)
            {
                Debug.Assert(propertyNames[k] != null, "propertyNames[k] is null");
                propertyNameCellCounts[k] = dc.Length(propertyNames[k]);
                if (propertyNameCellCounts[k] > _propertyLabelsDisplayLength)
                    _propertyLabelsDisplayLength = propertyNameCellCounts[k];
            }

            if (_propertyLabelsDisplayLength > maxAllowableLabelLength)
            {
                // need to truncate
                _propertyLabelsDisplayLength = maxAllowableLabelLength;
            }

            _propertyLabels = new string[propertyNames.Length];

            for (int k = 0; k < propertyNames.Length; k++)
            {
                if (propertyNameCellCounts[k] < _propertyLabelsDisplayLength)
                {
                    // shorter than the max, add padding
                    _propertyLabels[k] = propertyNames[k] + StringUtil.Padding(_propertyLabelsDisplayLength - propertyNameCellCounts[k]);
                }
                else if (propertyNameCellCounts[k] > _propertyLabelsDisplayLength)
                {
                    // longer than the max, clip
                    _propertyLabels[k] = propertyNames[k].Substring(0, dc.GetHeadSplitLength(propertyNames[k], _propertyLabelsDisplayLength));
                }
                else
                {
                    _propertyLabels[k] = propertyNames[k];
                }

                _propertyLabels[k] += Separator;
            }

            _propertyLabelsDisplayLength += Separator.Length;
        }

        /// <summary>
        /// Write the values of the properties of an object.
        /// </summary>
        /// <param name="values">Array with the values in form of formatted strings.</param>
        /// <param name="lo">LineOutput interface to write to.</param>
        internal void WriteProperties(string[] values, LineOutput lo)
        {
            if (_disabled)
                return;

            string[] valuesToPrint = null;
            if (values == null)
            {
                // we have nothing, but we have to create an empty array
                valuesToPrint = new string[_propertyLabels.Length];
                for (int k = 0; k < _propertyLabels.Length; k++)
                    valuesToPrint[k] = string.Empty;
            }
            else if (values.Length < _propertyLabels.Length)
            {
                // need to pad to the end of the array
                valuesToPrint = new string[_propertyLabels.Length];
                for (int k = 0; k < _propertyLabels.Length; k++)
                {
                    if (k < values.Length)
                        valuesToPrint[k] = values[k];
                    else
                        valuesToPrint[k] = string.Empty;
                }
            }
            else if (values.Length > _propertyLabels.Length)
            {
                // need to trim
                valuesToPrint = new string[_propertyLabels.Length];
                for (int k = 0; k < _propertyLabels.Length; k++)
                    valuesToPrint[k] = values[k];
            }
            else
            {
                // perfect match
                valuesToPrint = values;
            }

            Debug.Assert(lo != null, "LineOutput is null");

            for (int k = 0; k < _propertyLabels.Length; k++)
            {
                WriteProperty(k, valuesToPrint[k], lo);
            }
        }

        /// <summary>
        /// Helper, writing a single property to the screen.
        /// It wraps the value of the property if it is tool long to fit.
        /// </summary>
        /// <param name="k">Index of property to write.</param>
        /// <param name="propertyValue">String value of the property to write.</param>
        /// <param name="lo">LineOutput interface to write to.</param>
        private void WriteProperty(int k, string propertyValue, LineOutput lo)
        {
            if (propertyValue == null)
                propertyValue = string.Empty;

            // make sure we honor embedded newlines
            string[] lines = StringManipulationHelper.SplitLines(propertyValue);

            // padding to use in the lines after the first
            string padding = null;

            for (int i = 0; i < lines.Length; i++)
            {
                string prependString = null;

                if (i == 0)
                    prependString = _propertyLabels[k];
                else
                {
                    if (padding == null)
                        padding = StringUtil.Padding(_propertyLabelsDisplayLength);

                    prependString = padding;
                }

                WriteSingleLineHelper(prependString, lines[i], lo);
            }
        }

        /// <summary>
        /// Internal helper to split a line that is too long to fit and pad it to the left
        /// with a given string.
        /// </summary>
        /// <param name="prependString">String to add to the left.</param>
        /// <param name="line">Line to print.</param>
        /// <param name="lo">LineOuput to write to.</param>
        private void WriteSingleLineHelper(string prependString, string line, LineOutput lo)
        {
            if (line == null)
                line = string.Empty;

            // compute the width of the field for the value string (in screen cells)
            int fieldCellCount = _columnWidth - _propertyLabelsDisplayLength;

            // split the lines
            StringCollection sc = StringManipulationHelper.GenerateLines(lo.DisplayCells, line, fieldCellCount, fieldCellCount);

            // padding to use in the lines after the first
            string padding = StringUtil.Padding(_propertyLabelsDisplayLength);

            // display the string collection
            for (int k = 0; k < sc.Count; k++)
            {
                if (k == 0)
                {
                    if (ExperimentalFeature.IsEnabled("PSAnsiRendering"))
                    {
                        lo.WriteLine(PSStyle.Instance.Formatting.FormatAccent + prependString + PSStyle.Instance.Reset + sc[k]);
                    }
                    else
                    {
                        lo.WriteLine(prependString + sc[k]);
                    }
                }
                else
                {
                    if (ExperimentalFeature.IsEnabled("PSAnsiRendering"))
                    {
                        lo.WriteLine(padding + PSStyle.Instance.Formatting.FormatAccent + PSStyle.Instance.Reset + sc[k]);
                    }
                    else
                    {
                        lo.WriteLine(padding + sc[k]);
                    }
                }
            }
        }

        /// <summary>
        /// Set to true when the width of the screen is too small to do anything useful.
        /// </summary>
        private bool _disabled = false;

        private const string Separator = " : ";

        /// <summary>
        /// Minimum width for the property label field.
        /// </summary>
        private const int MinLabelWidth = 1;

        /// <summary>
        /// Minimum width for the property value field.
        /// </summary>
        private const int MinFieldWidth = 1;
    }
}
