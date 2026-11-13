// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// this file contains the data structures for the in memory database
// containing display and formatting information

using System.Collections.Generic;
using Microsoft.PowerShell.Commands;
using Microsoft.PowerShell.Commands.Internal.Format;

namespace Microsoft.PowerShell.Commands.Internal.Format
{
    #region Wide View Definitions

    /// <summary>
    /// In line definition of a wide control.
    /// </summary>
    internal sealed class WideControlBody : ControlBody
    {
        /// <summary>
        /// Number of columns to use for wide display.
        /// </summary>
        internal int columns = 0;

        /// <summary>
        /// Default wide entry definition
        /// It's mandatory.
        /// </summary>
        internal WideControlEntryDefinition defaultEntryDefinition = null;

        /// <summary>
        /// Optional list of list entry definition overrides. It can be empty if there are no overrides.
        /// </summary>
        internal List<WideControlEntryDefinition> optionalEntryList = new List<WideControlEntryDefinition>();
    }

    /// <summary>
    /// Definition of the data to be displayed in a list entry.
    /// </summary>
    internal sealed class WideControlEntryDefinition
    {
        /// <summary>
        /// Applicability clause
        /// Only valid if not the default definition.
        /// </summary>
        internal AppliesTo appliesTo = null;

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
    /// Defines a list control.
    /// </summary>
    public sealed class WideControl : PSControl
    {
        /// <summary>Entries in this wide control</summary>
        public List<WideControlEntryItem> Entries { get; internal set; }

        /// <summary>When true, widths are calculated based on more than the first object.</summary>
        public bool AutoSize { get; set; }

        /// <summary>Number of columns in the control</summary>
        public uint Columns { get; internal set; }

        /// <summary>Create a default WideControl</summary>
        public static WideControlBuilder Create(bool outOfBand = false, bool autoSize = false, uint columns = 0)
        {
            var control = new WideControl { OutOfBand = false, AutoSize = autoSize, Columns = columns };
            return new WideControlBuilder(control);
        }

        internal override void WriteToXml(FormatXmlWriter writer)
        {
            writer.WriteWideControl(this);
        }

        /// <summary>
        /// Indicates if this control does not have
        /// any script blocks and is safe to export.
        /// </summary>
        /// <returns>True if exportable, false otherwise.</returns>
        internal override bool SafeForExport()
        {
            if (!base.SafeForExport())
                return false;

            foreach (var entry in Entries)
            {
                if (!entry.SafeForExport())
                    return false;
            }

            return true;
        }

        internal override bool CompatibleWithOldPowerShell()
        {
            if (!base.CompatibleWithOldPowerShell())
                return false;

            foreach (var entry in Entries)
            {
                if (!entry.CompatibleWithOldPowerShell())
                    return false;
            }

            return true;
        }

        /// <summary>Default constructor for WideControl</summary>
        public WideControl()
        {
            Entries = new List<WideControlEntryItem>();
        }

        internal WideControl(WideControlBody widecontrolbody, ViewDefinition viewDefinition) : this()
        {
            OutOfBand = viewDefinition.outOfBand;
            GroupBy = PSControlGroupBy.Get(viewDefinition.groupBy);

            AutoSize = widecontrolbody.autosize.GetValueOrDefault();
            Columns = (uint)widecontrolbody.columns;

            Entries.Add(new WideControlEntryItem(widecontrolbody.defaultEntryDefinition));

            foreach (WideControlEntryDefinition definition in widecontrolbody.optionalEntryList)
            {
                Entries.Add(new WideControlEntryItem(definition));
            }
        }

        /// <summary>Public constructor for WideControl</summary>
        public WideControl(IEnumerable<WideControlEntryItem> wideEntries) : this()
        {
            if (wideEntries == null)
                throw PSTraceSource.NewArgumentNullException(nameof(wideEntries));

            foreach (WideControlEntryItem entryItem in wideEntries)
            {
                this.Entries.Add(entryItem);
            }
        }

        /// <summary>Public constructor for WideControl</summary>
        public WideControl(IEnumerable<WideControlEntryItem> wideEntries, uint columns) : this()
        {
            if (wideEntries == null)
                throw PSTraceSource.NewArgumentNullException(nameof(wideEntries));

            foreach (WideControlEntryItem entryItem in wideEntries)
            {
                this.Entries.Add(entryItem);
            }

            this.Columns = columns;
        }

        /// <summary>Construct an instance with columns</summary>
        public WideControl(uint columns) : this()
        {
            this.Columns = columns;
        }
    }

    /// <summary>
    /// Defines one item in a wide control entry.
    /// </summary>
    public sealed class WideControlEntryItem
    {
        /// <summary>Display entry</summary>
        public DisplayEntry DisplayEntry { get; internal set; }

        /// <summary>List of typenames which select this entry, deprecated, use EntrySelectedBy</summary>
        public List<string> SelectedBy
        {
            get
            {
                EntrySelectedBy ??= new EntrySelectedBy { TypeNames = new List<string>() };
                return EntrySelectedBy.TypeNames;
            }
        }

        /// <summary>List of typenames and/or a script block which select this entry.</summary>
        public EntrySelectedBy EntrySelectedBy { get; internal set; }

        /// <summary>Format string to apply</summary>
        public string FormatString { get; internal set; }

        internal WideControlEntryItem()
        {
        }

        internal WideControlEntryItem(WideControlEntryDefinition definition) : this()
        {
            if (definition.formatTokenList[0] is FieldPropertyToken fpt)
            {
                DisplayEntry = new DisplayEntry(fpt.expression);
                FormatString = fpt.fieldFormattingDirective.formatString;
            }

            if (definition.appliesTo != null)
            {
                EntrySelectedBy = EntrySelectedBy.Get(definition.appliesTo.referenceList);
            }
        }

        /// <summary>
        /// Public constructor for WideControlEntryItem.
        /// </summary>
        public WideControlEntryItem(DisplayEntry entry) : this()
        {
            if (entry == null)
                throw PSTraceSource.NewArgumentNullException(nameof(entry));
            this.DisplayEntry = entry;
        }

        /// <summary>
        /// Public constructor for WideControlEntryItem.
        /// </summary>
        public WideControlEntryItem(DisplayEntry entry, IEnumerable<string> selectedBy) : this()
        {
            if (entry == null)
                throw PSTraceSource.NewArgumentNullException(nameof(entry));
            if (selectedBy == null)
                throw PSTraceSource.NewArgumentNullException(nameof(selectedBy));

            this.DisplayEntry = entry;
            this.EntrySelectedBy = EntrySelectedBy.Get(selectedBy, null);
        }

        internal bool SafeForExport()
        {
            return DisplayEntry.SafeForExport() && (EntrySelectedBy == null || EntrySelectedBy.SafeForExport());
        }

        internal bool CompatibleWithOldPowerShell()
        {
            // Old versions of PowerShell don't know anything about FormatString or conditions in EntrySelectedBy.
            return FormatString == null &&
                   (EntrySelectedBy == null || EntrySelectedBy.CompatibleWithOldPowerShell());
        }
    }

    /// <summary/>
    public sealed class WideControlBuilder
    {
        private readonly WideControl _control;

        internal WideControlBuilder(WideControl control)
        {
            _control = control;
        }

        /// <summary>Group instances by the property name with an optional label.</summary>
        public WideControlBuilder GroupByProperty(string property, CustomControl customControl = null, string label = null)
        {
            _control.GroupBy = new PSControlGroupBy
            {
                Expression = new DisplayEntry(property, DisplayEntryValueType.Property),
                CustomControl = customControl,
                Label = label
            };
            return this;
        }

        /// <summary>Group instances by the script block expression with an optional label.</summary>
        public WideControlBuilder GroupByScriptBlock(string scriptBlock, CustomControl customControl = null, string label = null)
        {
            _control.GroupBy = new PSControlGroupBy
            {
                Expression = new DisplayEntry(scriptBlock, DisplayEntryValueType.ScriptBlock),
                CustomControl = customControl,
                Label = label
            };
            return this;
        }

        /// <summary/>
        public WideControlBuilder AddScriptBlockEntry(string scriptBlock, string format = null, IEnumerable<string> entrySelectedByType = null, IEnumerable<DisplayEntry> entrySelectedByCondition = null)
        {
            var entry = new WideControlEntryItem(new DisplayEntry(scriptBlock, DisplayEntryValueType.ScriptBlock))
            {
                EntrySelectedBy = EntrySelectedBy.Get(entrySelectedByType, entrySelectedByCondition)
            };
            _control.Entries.Add(entry);
            return this;
        }

        /// <summary/>
        public WideControlBuilder AddPropertyEntry(string propertyName, string format = null, IEnumerable<string> entrySelectedByType = null, IEnumerable<DisplayEntry> entrySelectedByCondition = null)
        {
            var entry = new WideControlEntryItem(new DisplayEntry(propertyName, DisplayEntryValueType.Property))
            {
                EntrySelectedBy = EntrySelectedBy.Get(entrySelectedByType, entrySelectedByCondition)
            };
            _control.Entries.Add(entry);
            return this;
        }

        /// <summary/>
        public WideControl EndWideControl()
        {
            return _control;
        }
    }
}
