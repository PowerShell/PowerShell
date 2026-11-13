// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// this file contains the data structures for the in memory database
// containing display and formatting information

using System.Collections.Generic;
using Microsoft.PowerShell.Commands;
using Microsoft.PowerShell.Commands.Internal.Format;

namespace Microsoft.PowerShell.Commands.Internal.Format
{
    #region List View Definitions

    /// <summary>
    /// In line definition of a list control.
    /// </summary>
    internal sealed class ListControlBody : ControlBody
    {
        /// <summary>
        /// Default list entry definition
        /// It's mandatory.
        /// </summary>
        internal ListControlEntryDefinition defaultEntryDefinition = null;

        /// <summary>
        /// Optional list of list entry definition overrides. It can be empty if there are no overrides.
        /// </summary>
        internal List<ListControlEntryDefinition> optionalEntryList = new List<ListControlEntryDefinition>();

        internal override ControlBase Copy()
        {
            ListControlBody result = new ListControlBody();
            result.autosize = this.autosize;
            if (defaultEntryDefinition != null)
            {
                result.defaultEntryDefinition = this.defaultEntryDefinition.Copy();
            }

            foreach (ListControlEntryDefinition lced in this.optionalEntryList)
            {
                result.optionalEntryList.Add(lced);
            }

            return result;
        }
    }

    /// <summary>
    /// Definition of the data to be displayed in a list entry.
    /// </summary>
    internal sealed class ListControlEntryDefinition
    {
        /// <summary>
        /// Applicability clause
        /// Only valid if not the default definition.
        /// </summary>
        internal AppliesTo appliesTo = null;

        /// <summary>
        /// Mandatory list of list view items.
        /// It cannot be empty.
        /// </summary>
        internal List<ListControlItemDefinition> itemDefinitionList = new List<ListControlItemDefinition>();

        /// <summary>
        /// Returns a Shallow Copy of the current object.
        /// </summary>
        /// <returns></returns>
        internal ListControlEntryDefinition Copy()
        {
            ListControlEntryDefinition result = new ListControlEntryDefinition();
            result.appliesTo = this.appliesTo;
            foreach (ListControlItemDefinition lcid in this.itemDefinitionList)
            {
                result.itemDefinitionList.Add(lcid);
            }

            return result;
        }
    }

    /// <summary>
    /// Cell definition inside a row.
    /// </summary>
    internal sealed class ListControlItemDefinition
    {
        /// <summary>
        /// Optional expression for conditional binding.
        /// </summary>
        internal ExpressionToken conditionToken;

        /// <summary>
        /// Optional label
        /// If not present, use the name of the property from the matching
        /// mandatory item description.
        /// </summary>
        internal TextToken label = null;

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
    public sealed class ListControl : PSControl
    {
        /// <summary>Entries in this list control</summary>
        public List<ListControlEntry> Entries { get; internal set; }

        /// <summary></summary>
        public static ListControlBuilder Create(bool outOfBand = false)
        {
            var list = new ListControl { OutOfBand = false };
            return new ListControlBuilder(list);
        }

        internal override void WriteToXml(FormatXmlWriter writer)
        {
            writer.WriteListControl(this);
        }

        /// <summary>Indicates if this control does not have any script blocks and is safe to export</summary>
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

        /// <summary>Initiate an instance of ListControl</summary>
        public ListControl()
        {
            Entries = new List<ListControlEntry>();
        }

        /// <summary>To go from internal representation to external - for Get-FormatData</summary>
        internal ListControl(ListControlBody listcontrolbody, ViewDefinition viewDefinition)
            : this()
        {
            this.GroupBy = PSControlGroupBy.Get(viewDefinition.groupBy);
            this.OutOfBand = viewDefinition.outOfBand;

            Entries.Add(new ListControlEntry(listcontrolbody.defaultEntryDefinition));

            foreach (ListControlEntryDefinition lced in listcontrolbody.optionalEntryList)
            {
                Entries.Add(new ListControlEntry(lced));
            }
        }

        /// <summary>Public constructor for ListControl</summary>
        public ListControl(IEnumerable<ListControlEntry> entries)
            : this()
        {
            if (entries == null)
                throw PSTraceSource.NewArgumentNullException(nameof(entries));
            foreach (ListControlEntry entry in entries)
            {
                this.Entries.Add(entry);
            }
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
    }

    /// <summary>
    /// Defines one entry in a list control.
    /// </summary>
    public sealed class ListControlEntry
    {
        /// <summary>List of items in the entry</summary>
        public List<ListControlEntryItem> Items { get; internal set; }

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

        /// <summary>Initiate an instance of ListControlEntry</summary>
        public ListControlEntry()
        {
            Items = new List<ListControlEntryItem>();
        }

        internal ListControlEntry(ListControlEntryDefinition entrydefn)
            : this()
        {
            if (entrydefn.appliesTo != null)
            {
                EntrySelectedBy = EntrySelectedBy.Get(entrydefn.appliesTo.referenceList);
            }

            foreach (ListControlItemDefinition itemdefn in entrydefn.itemDefinitionList)
            {
                Items.Add(new ListControlEntryItem(itemdefn));
            }
        }

        /// <summary>Public constructor for ListControlEntry</summary>
        public ListControlEntry(IEnumerable<ListControlEntryItem> listItems)
            : this()
        {
            if (listItems == null)
                throw PSTraceSource.NewArgumentNullException(nameof(listItems));
            foreach (ListControlEntryItem item in listItems)
            {
                this.Items.Add(item);
            }
        }

        /// <summary>Public constructor for ListControlEntry</summary>
        public ListControlEntry(IEnumerable<ListControlEntryItem> listItems, IEnumerable<string> selectedBy)
        {
            if (listItems == null)
                throw PSTraceSource.NewArgumentNullException(nameof(listItems));
            if (selectedBy == null)
                throw PSTraceSource.NewArgumentNullException(nameof(selectedBy));

            EntrySelectedBy = new EntrySelectedBy { TypeNames = new List<string>(selectedBy) };
            foreach (ListControlEntryItem item in listItems)
            {
                this.Items.Add(item);
            }
        }

        internal bool SafeForExport()
        {
            foreach (var item in Items)
            {
                if (!item.SafeForExport())
                    return false;
            }

            return EntrySelectedBy == null || EntrySelectedBy.SafeForExport();
        }

        internal bool CompatibleWithOldPowerShell()
        {
            foreach (var item in Items)
            {
                if (!item.CompatibleWithOldPowerShell())
                    return false;
            }

            return EntrySelectedBy == null || EntrySelectedBy.CompatibleWithOldPowerShell();
        }
    }

    /// <summary>
    /// Defines one row in a list control entry.
    /// </summary>
    public sealed class ListControlEntryItem
    {
        /// <summary>
        /// Gets the label for this List Control Entry Item
        /// If nothing is specified, then it uses the
        /// property name.
        /// </summary>
        public string Label { get; internal set; }

        /// <summary>Display entry</summary>
        public DisplayEntry DisplayEntry { get; internal set; }

        /// <summary/>
        public DisplayEntry ItemSelectionCondition { get; internal set; }

        /// <summary>Format string to apply</summary>
        public string FormatString { get; internal set; }

        internal ListControlEntryItem()
        {
        }

        internal ListControlEntryItem(ListControlItemDefinition definition)
        {
            if (definition.label != null)
            {
                Label = definition.label.text;
            }

            if (definition.formatTokenList[0] is FieldPropertyToken fpt)
            {
                if (fpt.fieldFormattingDirective.formatString != null)
                {
                    FormatString = fpt.fieldFormattingDirective.formatString;
                }

                DisplayEntry = new DisplayEntry(fpt.expression);
                if (definition.conditionToken != null)
                {
                    ItemSelectionCondition = new DisplayEntry(definition.conditionToken);
                }
            }
        }

        /// <summary>
        /// Public constructor for ListControlEntryItem
        /// Label and Entry could be null.
        /// </summary>
        /// <param name="label"></param>
        /// <param name="entry"></param>
        public ListControlEntryItem(string label, DisplayEntry entry)
        {
            this.Label = label;
            this.DisplayEntry = entry;
        }

        internal bool SafeForExport()
        {
            return DisplayEntry.SafeForExport() &&
                   (ItemSelectionCondition == null || ItemSelectionCondition.SafeForExport());
        }

        internal bool CompatibleWithOldPowerShell()
        {
            // Old versions of PowerShell know nothing about ItemSelectionCondition.
            return ItemSelectionCondition == null;
        }
    }

    /// <summary/>
    public class ListEntryBuilder
    {
        private readonly ListControlBuilder _listBuilder;
        internal ListControlEntry _listEntry;

        internal ListEntryBuilder(ListControlBuilder listBuilder, ListControlEntry listEntry)
        {
            _listBuilder = listBuilder;
            _listEntry = listEntry;
        }

        private ListEntryBuilder AddItem(string value, string label, DisplayEntryValueType kind, string format)
        {
            if (string.IsNullOrEmpty(value))
                throw PSTraceSource.NewArgumentNullException("property");

            _listEntry.Items.Add(new ListControlEntryItem
            {
                DisplayEntry = new DisplayEntry(value, kind),
                Label = label,
                FormatString = format
            });

            return this;
        }

        /// <summary></summary>
        public ListEntryBuilder AddItemScriptBlock(string scriptBlock, string label = null, string format = null)
        {
            return AddItem(scriptBlock, label, DisplayEntryValueType.ScriptBlock, format);
        }

        /// <summary></summary>
        public ListEntryBuilder AddItemProperty(string property, string label = null, string format = null)
        {
            return AddItem(property, label, DisplayEntryValueType.Property, format);
        }

        /// <summary></summary>
        public ListControlBuilder EndEntry()
        {
            return _listBuilder;
        }
    }

    /// <summary></summary>
    public class ListControlBuilder
    {
        internal ListControl _list;

        internal ListControlBuilder(ListControl list)
        {
            _list = list;
        }

        /// <summary>Group instances by the property name with an optional label.</summary>
        public ListControlBuilder GroupByProperty(string property, CustomControl customControl = null, string label = null)
        {
            _list.GroupBy = new PSControlGroupBy
            {
                Expression = new DisplayEntry(property, DisplayEntryValueType.Property),
                CustomControl = customControl,
                Label = label
            };
            return this;
        }

        /// <summary>Group instances by the script block expression with an optional label.</summary>
        public ListControlBuilder GroupByScriptBlock(string scriptBlock, CustomControl customControl = null, string label = null)
        {
            _list.GroupBy = new PSControlGroupBy
            {
                Expression = new DisplayEntry(scriptBlock, DisplayEntryValueType.ScriptBlock),
                CustomControl = customControl,
                Label = label
            };
            return this;
        }

        /// <summary></summary>
        public ListEntryBuilder StartEntry(IEnumerable<string> entrySelectedByType = null, IEnumerable<DisplayEntry> entrySelectedByCondition = null)
        {
            var listEntry = new ListControlEntry
            {
                EntrySelectedBy = EntrySelectedBy.Get(entrySelectedByType, entrySelectedByCondition)
            };
            _list.Entries.Add(listEntry);
            return new ListEntryBuilder(this, listEntry);
        }

        /// <summary></summary>
        public ListControl EndList()
        {
            return _list;
        }
    }
}
