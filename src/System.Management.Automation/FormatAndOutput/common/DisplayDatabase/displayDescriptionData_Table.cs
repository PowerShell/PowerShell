// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// this file contains the data structures for the in memory database
// containing display and formatting information

using System.Collections.Generic;
using Microsoft.PowerShell.Commands;
using Microsoft.PowerShell.Commands.Internal.Format;

namespace Microsoft.PowerShell.Commands.Internal.Format
{
    #region Table View Definitions

    /// <summary>
    /// Alignment values
    /// NOTE: we do not use an enum because this will have to be
    /// serialized and ERS/serialization do not support enumerations.
    /// </summary>
    internal static class TextAlignment
    {
        internal const int Undefined = 0;
        internal const int Left = 1;
        internal const int Center = 2;
        internal const int Right = 3;
    }

    /// <summary>
    /// Definition of a table control.
    /// </summary>
    internal sealed class TableControlBody : ControlBody
    {
        /// <summary>
        /// Optional, if not present, use data off the default table row definition.
        /// </summary>
        internal TableHeaderDefinition header = new TableHeaderDefinition();

        /// <summary>
        /// Default row definition
        /// It's mandatory.
        /// </summary>
        internal TableRowDefinition defaultDefinition;

        /// <summary>
        /// Optional list of row definition overrides. It can be empty if there are no overrides.
        /// </summary>
        internal List<TableRowDefinition> optionalDefinitionList = new List<TableRowDefinition>();

        internal override ControlBase Copy()
        {
            TableControlBody result = new TableControlBody
            {
                autosize = this.autosize,
                header = this.header.Copy()
            };
            if (defaultDefinition != null)
            {
                result.defaultDefinition = this.defaultDefinition.Copy();
            }

            foreach (TableRowDefinition trd in this.optionalDefinitionList)
            {
                result.optionalDefinitionList.Add(trd);
            }

            return result;
        }
    }

    /// <summary>
    /// Information about the table header
    /// NOTE: if an instance of this class is present, the list must not be empty.
    /// </summary>
    internal sealed class TableHeaderDefinition
    {
        /// <summary>
        /// If true, direct the outputter to suppress table header printing.
        /// </summary>
        internal bool hideHeader;

        /// <summary>
        /// Mandatory list of column header definitions.
        /// </summary>
        internal List<TableColumnHeaderDefinition> columnHeaderDefinitionList =
                            new List<TableColumnHeaderDefinition>();

        /// <summary>
        /// Returns a Shallow Copy of the current object.
        /// </summary>
        /// <returns></returns>
        internal TableHeaderDefinition Copy()
        {
            TableHeaderDefinition result = new TableHeaderDefinition { hideHeader = this.hideHeader };
            foreach (TableColumnHeaderDefinition tchd in this.columnHeaderDefinitionList)
            {
                result.columnHeaderDefinitionList.Add(tchd);
            }

            return result;
        }
    }

    internal sealed class TableColumnHeaderDefinition
    {
        /// <summary>
        /// Optional label
        /// If not present, use the name of the property from the matching
        /// mandatory row description.
        /// </summary>
        internal TextToken label = null;

        /// <summary>
        /// General alignment for the column
        /// If not present, either use the one from the row definition
        /// or the data driven heuristics.
        /// </summary>
        internal int alignment = TextAlignment.Undefined;

        /// <summary>
        /// Width of the column.
        /// </summary>
        internal int width = 0; // undefined
    }

    /// <summary>
    /// Definition of the data to be displayed in a table row.
    /// </summary>
    internal sealed class TableRowDefinition
    {
        /// <summary>
        /// Applicability clause
        /// Only valid if not the default definition.
        /// </summary>
        internal AppliesTo appliesTo;

        /// <summary>
        /// If true, the current table row should be allowed
        /// to wrap to multiple lines, else truncated.
        /// </summary>
        internal bool multiLine;

        /// <summary>
        /// Mandatory list of column items.
        /// It cannot be empty.
        /// </summary>
        internal List<TableRowItemDefinition> rowItemDefinitionList = new List<TableRowItemDefinition>();

        /// <summary>
        /// Returns a Shallow Copy of the current object.
        /// </summary>
        /// <returns></returns>
        internal TableRowDefinition Copy()
        {
            TableRowDefinition result = new TableRowDefinition
            {
                appliesTo = this.appliesTo,
                multiLine = this.multiLine
            };
            foreach (TableRowItemDefinition trid in this.rowItemDefinitionList)
            {
                result.rowItemDefinitionList.Add(trid);
            }

            return result;
        }
    }

    /// <summary>
    /// Cell definition inside a row.
    /// </summary>
    internal sealed class TableRowItemDefinition
    {
        /// <summary>
        /// Optional alignment to override the default one at the header level.
        /// </summary>
        internal int alignment = TextAlignment.Undefined;

        /// <summary>
        /// Format directive body telling how to format the cell
        /// RULE: the body can only contain
        ///     * TextToken
        ///     * PropertyToken
        ///     * NOTHING (provide an empty cell)
        /// </summary>
        internal List<FormatToken> formatTokenList = new List<FormatToken>();
    }

    #endregion
}

namespace System.Management.Automation
{
    /// <summary>
    /// Defines a table control.
    /// </summary>
    public sealed class TableControl : PSControl
    {
        /// <summary>Collection of column header definitions for this table control</summary>
        public List<TableControlColumnHeader> Headers { get; set; }

        /// <summary>Collection of row definitions for this table control</summary>
        public List<TableControlRow> Rows { get; set; }

        /// <summary>When true, column widths are calculated based on more than the first object.</summary>
        public bool AutoSize { get; set; }

        /// <summary>When true, table headers are not displayed</summary>
        public bool HideTableHeaders { get; set; }

        /// <summary>Create a default TableControl</summary>
        public static TableControlBuilder Create(bool outOfBand = false, bool autoSize = false, bool hideTableHeaders = false)
        {
            var table = new TableControl { OutOfBand = outOfBand, AutoSize = autoSize, HideTableHeaders = hideTableHeaders };
            return new TableControlBuilder(table);
        }

        /// <summary>Public default constructor for TableControl</summary>
        public TableControl()
        {
            Headers = new List<TableControlColumnHeader>();
            Rows = new List<TableControlRow>();
        }

        internal override void WriteToXml(FormatXmlWriter writer)
        {
            writer.WriteTableControl(this);
        }

        /// <summary>
        /// Determines if this object is safe to be written.
        /// </summary>
        /// <returns>True if safe, false otherwise.</returns>
        internal override bool SafeForExport()
        {
            if (!base.SafeForExport())
                return false;

            foreach (var row in Rows)
            {
                if (!row.SafeForExport())
                    return false;
            }

            return true;
        }

        internal override bool CompatibleWithOldPowerShell()
        {
            if (!base.CompatibleWithOldPowerShell())
                return false;

            foreach (var row in Rows)
            {
                if (!row.CompatibleWithOldPowerShell())
                    return false;
            }

            return true;
        }

        internal TableControl(TableControlBody tcb, ViewDefinition viewDefinition) : this()
        {
            this.OutOfBand = viewDefinition.outOfBand;
            this.GroupBy = PSControlGroupBy.Get(viewDefinition.groupBy);

            this.AutoSize = tcb.autosize.GetValueOrDefault();
            this.HideTableHeaders = tcb.header.hideHeader;

            TableControlRow row = new TableControlRow(tcb.defaultDefinition);

            Rows.Add(row);

            foreach (TableRowDefinition rd in tcb.optionalDefinitionList)
            {
                row = new TableControlRow(rd);

                Rows.Add(row);
            }

            foreach (TableColumnHeaderDefinition hd in tcb.header.columnHeaderDefinitionList)
            {
                TableControlColumnHeader header = new TableControlColumnHeader(hd);
                Headers.Add(header);
            }
        }

        /// <summary>
        /// Public constructor for TableControl that only takes 'tableControlRows'.
        /// </summary>
        /// <param name="tableControlRow"></param>
        public TableControl(TableControlRow tableControlRow) : this()
        {
            if (tableControlRow == null)
                throw PSTraceSource.NewArgumentNullException("tableControlRows");

            this.Rows.Add(tableControlRow);
        }

        /// <summary>
        /// Public constructor for TableControl that takes both 'tableControlRows' and 'tableControlColumnHeaders'.
        /// </summary>
        /// <param name="tableControlRow"></param>
        /// <param name="tableControlColumnHeaders"></param>
        public TableControl(TableControlRow tableControlRow, IEnumerable<TableControlColumnHeader> tableControlColumnHeaders) : this()
        {
            if (tableControlRow == null)
                throw PSTraceSource.NewArgumentNullException("tableControlRows");
            if (tableControlColumnHeaders == null)
                throw PSTraceSource.NewArgumentNullException(nameof(tableControlColumnHeaders));

            this.Rows.Add(tableControlRow);
            foreach (TableControlColumnHeader header in tableControlColumnHeaders)
            {
                this.Headers.Add(header);
            }
        }
    }

    /// <summary>
    /// Defines the header for a particular column in a table control.
    /// </summary>
    public sealed class TableControlColumnHeader
    {
        /// <summary>Label for the column</summary>
        public string Label { get; set; }

        /// <summary>Alignment of the string within the column</summary>
        public Alignment Alignment { get; set; }

        /// <summary>Width of the column - in number of display cells</summary>
        public int Width { get; set; }

        internal TableControlColumnHeader(TableColumnHeaderDefinition colheaderdefinition)
        {
            if (colheaderdefinition.label != null)
            {
                Label = colheaderdefinition.label.text;
            }

            Alignment = (Alignment)colheaderdefinition.alignment;
            Width = colheaderdefinition.width;
        }

        /// <summary>Default constructor</summary>
        public TableControlColumnHeader()
        {
        }

        /// <summary>
        /// Public constructor for TableControlColumnHeader.
        /// </summary>
        /// <param name="label">Could be null if no label to specify.</param>
        /// <param name="width">The Value should be non-negative.</param>
        /// <param name="alignment">The default value is Alignment.Undefined.</param>
        public TableControlColumnHeader(string label, int width, Alignment alignment)
        {
            if (width < 0)
                throw PSTraceSource.NewArgumentOutOfRangeException(nameof(width), width);

            this.Label = label;
            this.Width = width;
            this.Alignment = alignment;
        }
    }

    /// <summary>
    /// Defines a particular column within a row
    /// in a table control.
    /// </summary>
    public sealed class TableControlColumn
    {
        /// <summary>Alignment of the particular column</summary>
        public Alignment Alignment { get; set; }

        /// <summary>Display Entry</summary>
        public DisplayEntry DisplayEntry { get; set; }

        /// <summary>Format string to apply</summary>
        public string FormatString { get; internal set; }

        /// <summary>
        /// Returns the value of the entry.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return DisplayEntry.Value;
        }

        /// <summary>Default constructor</summary>
        public TableControlColumn()
        {
        }

        internal TableControlColumn(string text, int alignment, bool isscriptblock, string formatString)
        {
            Alignment = (Alignment)alignment;
            DisplayEntry = new DisplayEntry(text, isscriptblock ? DisplayEntryValueType.ScriptBlock : DisplayEntryValueType.Property);
            FormatString = formatString;
        }

        /// <summary>
        /// Public constructor for TableControlColumn.
        /// </summary>
        /// <param name="alignment"></param>
        /// <param name="entry"></param>
        public TableControlColumn(Alignment alignment, DisplayEntry entry)
        {
            this.Alignment = alignment;
            this.DisplayEntry = entry;
        }

        internal bool SafeForExport()
        {
            return DisplayEntry.SafeForExport();
        }
    }

    /// <summary>
    /// Defines a single row in a table control.
    /// </summary>
    public sealed class TableControlRow
    {
        /// <summary>Collection of column definitions for this row</summary>
        public List<TableControlColumn> Columns { get; set; }

        /// <summary>List of typenames which select this entry</summary>
        public EntrySelectedBy SelectedBy { get; internal set; }

        /// <summary>When true, instead of truncating to the column width, use multiple lines.</summary>
        public bool Wrap { get; set; }

        /// <summary>Public constructor for TableControlRow</summary>
        public TableControlRow()
        {
            Columns = new List<TableControlColumn>();
        }

        internal TableControlRow(TableRowDefinition rowdefinition) : this()
        {
            Wrap = rowdefinition.multiLine;
            if (rowdefinition.appliesTo != null)
            {
                SelectedBy = EntrySelectedBy.Get(rowdefinition.appliesTo.referenceList);
            }

            foreach (TableRowItemDefinition itemdef in rowdefinition.rowItemDefinitionList)
            {
                FieldPropertyToken fpt = itemdef.formatTokenList[0] as FieldPropertyToken;
                TableControlColumn column;

                if (fpt != null)
                {
                    column = new TableControlColumn(fpt.expression.expressionValue, itemdef.alignment,
                                    fpt.expression.isScriptBlock, fpt.fieldFormattingDirective.formatString);
                }
                else
                {
                    column = new TableControlColumn();
                }

                Columns.Add(column);
            }
        }

        /// <summary>Public constructor for TableControlRow.</summary>
        public TableControlRow(IEnumerable<TableControlColumn> columns) : this()
        {
            if (columns == null)
                throw PSTraceSource.NewArgumentNullException(nameof(columns));
            foreach (TableControlColumn column in columns)
            {
                Columns.Add(column);
            }
        }

        internal bool SafeForExport()
        {
            foreach (var column in Columns)
            {
                if (!column.SafeForExport())
                    return false;
            }

            return SelectedBy != null && SelectedBy.SafeForExport();
        }

        internal bool CompatibleWithOldPowerShell()
        {
            // Old versions of PowerShell don't support multiple row definitions.
            return SelectedBy == null;
        }
    }

    /// <summary>A helper class for defining table controls</summary>
    public sealed class TableRowDefinitionBuilder
    {
        internal readonly TableControlBuilder _tcb;
        internal readonly TableControlRow _tcr;

        internal TableRowDefinitionBuilder(TableControlBuilder tcb, TableControlRow tcr)
        {
            _tcb = tcb;
            _tcr = tcr;
        }

        private TableRowDefinitionBuilder AddItem(string value, DisplayEntryValueType entryType, Alignment alignment, string format)
        {
            if (string.IsNullOrEmpty(value))
                throw PSTraceSource.NewArgumentException(nameof(value));

            var tableControlColumn = new TableControlColumn(alignment, new DisplayEntry(value, entryType))
            {
                FormatString = format
            };
            _tcr.Columns.Add(tableControlColumn);

            return this;
        }

        /// <summary>
        /// Add a column to the current row definition that calls a script block.
        /// </summary>
        public TableRowDefinitionBuilder AddScriptBlockColumn(string scriptBlock, Alignment alignment = Alignment.Undefined, string format = null)
        {
            return AddItem(scriptBlock, DisplayEntryValueType.ScriptBlock, alignment, format);
        }

        /// <summary>
        /// Add a column to the current row definition that references a property.
        /// </summary>
        public TableRowDefinitionBuilder AddPropertyColumn(string propertyName, Alignment alignment = Alignment.Undefined, string format = null)
        {
            return AddItem(propertyName, DisplayEntryValueType.Property, alignment, format);
        }

        /// <summary>
        /// Complete a row definition.
        /// </summary>
        public TableControlBuilder EndRowDefinition()
        {
            return _tcb;
        }
    }

    /// <summary>A helper class for defining table controls</summary>
    public sealed class TableControlBuilder
    {
        internal readonly TableControl _table;

        internal TableControlBuilder(TableControl table)
        {
            _table = table;
        }

        /// <summary>Group instances by the property name with an optional label.</summary>
        public TableControlBuilder GroupByProperty(string property, CustomControl customControl = null, string label = null)
        {
            _table.GroupBy = new PSControlGroupBy
            {
                Expression = new DisplayEntry(property, DisplayEntryValueType.Property),
                CustomControl = customControl,
                Label = label
            };
            return this;
        }

        /// <summary>Group instances by the script block expression with an optional label.</summary>
        public TableControlBuilder GroupByScriptBlock(string scriptBlock, CustomControl customControl = null, string label = null)
        {
            _table.GroupBy = new PSControlGroupBy
            {
                Expression = new DisplayEntry(scriptBlock, DisplayEntryValueType.ScriptBlock),
                CustomControl = customControl,
                Label = label
            };
            return this;
        }

        /// <summary>Add a header</summary>
        public TableControlBuilder AddHeader(Alignment alignment = Alignment.Undefined, int width = 0, string label = null)
        {
            _table.Headers.Add(new TableControlColumnHeader(label, width, alignment));
            return this;
        }

        /// <summary>Add a header</summary>
        public TableRowDefinitionBuilder StartRowDefinition(bool wrap = false, IEnumerable<string> entrySelectedByType = null, IEnumerable<DisplayEntry> entrySelectedByCondition = null)
        {
            var row = new TableControlRow { Wrap = wrap };
            if (entrySelectedByType != null || entrySelectedByCondition != null)
            {
                row.SelectedBy = new EntrySelectedBy();
                if (entrySelectedByType != null)
                {
                    row.SelectedBy.TypeNames = new List<string>(entrySelectedByType);
                }

                if (entrySelectedByCondition != null)
                {
                    row.SelectedBy.SelectionCondition = new List<DisplayEntry>(entrySelectedByCondition);
                }
            }

            _table.Rows.Add(row);
            return new TableRowDefinitionBuilder(this, row);
        }

        /// <summary>Complete a table definition</summary>
        public TableControl EndTable()
        {
            return _table;
        }
    }
}
