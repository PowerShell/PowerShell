// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Management.Automation;
using System.Xml;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Helper class for writing formatting directives to XML.
    /// </summary>
    internal sealed class FormatXmlWriter
    {
        private XmlWriter _writer;
        private bool _exportScriptBlock;

        private FormatXmlWriter() { }

        /// <summary>
        /// Writes a collection of format view definitions to XML file.
        /// </summary>
        /// <param name="typeDefinitions">Collection of PSTypeDefinition.</param>
        /// <param name="filepath">Path to XML file.</param>
        /// <param name="cmdlet">Cmdlet from which this si used.</param>
        /// <param name="force">True - to force write the file.</param>
        /// <param name="writeScriptBlock">True - to export scriptblocks.</param>
        /// <param name="noclobber">True - do not overwrite the file.</param>
        /// <param name="isLiteralPath">True - bypass wildcard expansion on the file name.</param>
        internal static void WriteToPs1Xml(PSCmdlet cmdlet, List<ExtendedTypeDefinition> typeDefinitions,
            string filepath, bool force, bool noclobber, bool writeScriptBlock, bool isLiteralPath)
        {
            StreamWriter streamWriter;
            FileStream fileStream;
            FileInfo fileInfo;
            PathUtils.MasterStreamOpen(cmdlet, filepath, "ascii", true, false, force, noclobber,
                out fileStream, out streamWriter, out fileInfo, isLiteralPath);

            try
            {
                var settings = new XmlWriterSettings();
                settings.Indent = true;
                settings.IndentChars = "  ";
                settings.NewLineOnAttributes = true;

                using (XmlWriter xmlWriter = XmlWriter.Create(streamWriter, settings))
                {
                    var writer = new FormatXmlWriter
                    {
                        _writer = xmlWriter,
                        _exportScriptBlock = writeScriptBlock
                    };
                    writer.WriteToXml(typeDefinitions);
                }
            }
            finally
            {
                streamWriter.Dispose();
                fileStream.Dispose();
            }
        }

        internal static void WriteToXml(XmlWriter writer, IEnumerable<ExtendedTypeDefinition> typeDefinitions, bool writeScriptBlock)
        {
            var formatXmlWriter = new FormatXmlWriter { _exportScriptBlock = writeScriptBlock, _writer = writer };
            formatXmlWriter.WriteToXml(typeDefinitions);
        }

        internal void WriteToXml(IEnumerable<ExtendedTypeDefinition> typeDefinitions)
        {
            var views = new Dictionary<Guid, List<ExtendedTypeDefinition>>();
            var formatdefs = new Dictionary<Guid, FormatViewDefinition>();

            foreach (ExtendedTypeDefinition typedefinition in typeDefinitions)
            {
                foreach (FormatViewDefinition viewdefinition in typedefinition.FormatViewDefinition)
                {
                    List<ExtendedTypeDefinition> viewList;
                    if (!views.TryGetValue(viewdefinition.InstanceId, out viewList))
                    {
                        viewList = new List<ExtendedTypeDefinition>();
                        views.Add(viewdefinition.InstanceId, viewList);
                    }

                    if (!formatdefs.ContainsKey(viewdefinition.InstanceId))
                    {
                        formatdefs.Add(viewdefinition.InstanceId, viewdefinition);
                    }

                    viewList.Add(typedefinition);
                }
            }

            _writer.WriteStartElement("Configuration");

            _writer.WriteStartElement("ViewDefinitions");
            foreach (var pair in formatdefs)
            {
                Guid id = pair.Key;
                FormatViewDefinition formatdef = pair.Value;

                _writer.WriteStartElement("View");
                _writer.WriteElementString("Name", formatdef.Name);
                _writer.WriteStartElement("ViewSelectedBy");
                foreach (ExtendedTypeDefinition definition in views[id])
                {
                    _writer.WriteElementString("TypeName", definition.TypeName);
                }

                _writer.WriteEndElement(/*</ViewSelectedBy>*/);

                var groupBy = formatdef.Control.GroupBy;
                if (groupBy != null)
                {
                    _writer.WriteStartElement("GroupBy");
                    WriteDisplayEntry(groupBy.Expression);
                    if (!string.IsNullOrEmpty(groupBy.Label))
                    {
                        _writer.WriteElementString("Label", groupBy.Label);
                    }

                    if (groupBy.CustomControl != null)
                    {
                        WriteCustomControl(groupBy.CustomControl);
                    }

                    _writer.WriteEndElement(/*</GroupBy>*/);
                }

                if (formatdef.Control.OutOfBand)
                {
                    _writer.WriteElementString("OutOfBand", string.Empty);
                }

                formatdef.Control.WriteToXml(this);
                _writer.WriteEndElement(/*</View>*/);
            }

            _writer.WriteEndElement(/*</ViewDefinitions>*/);

            _writer.WriteEndElement(/*</Configuration>*/);
        }

        internal void WriteTableControl(TableControl tableControl)
        {
            _writer.WriteStartElement("TableControl");
            if (tableControl.AutoSize)
            {
                _writer.WriteElementString("AutoSize", string.Empty);
            }

            if (tableControl.HideTableHeaders)
            {
                _writer.WriteElementString("HideTableHeaders", string.Empty);
            }

            _writer.WriteStartElement("TableHeaders");
            foreach (TableControlColumnHeader columnheader in tableControl.Headers)
            {
                _writer.WriteStartElement("TableColumnHeader");
                if (!string.IsNullOrEmpty(columnheader.Label))
                {
                    _writer.WriteElementString("Label", columnheader.Label);
                }

                if (columnheader.Width > 0)
                {
                    _writer.WriteElementString("Width", columnheader.Width.ToString(CultureInfo.InvariantCulture));
                }

                if (columnheader.Alignment != Alignment.Undefined)
                {
                    _writer.WriteElementString("Alignment", columnheader.Alignment.ToString());
                }

                _writer.WriteEndElement(/*</TableColumnHeader>*/);
            }

            _writer.WriteEndElement(/*</TableHeaders>*/);

            _writer.WriteStartElement("TableRowEntries");
            foreach (TableControlRow row in tableControl.Rows)
            {
                _writer.WriteStartElement("TableRowEntry");
                if (row.Wrap)
                {
                    _writer.WriteStartElement("Wrap");
                    _writer.WriteEndElement(/*</Wrap>*/);
                }

                if (row.SelectedBy != null)
                {
                    WriteEntrySelectedBy(row.SelectedBy);
                }

                _writer.WriteStartElement("TableColumnItems");
                foreach (TableControlColumn coldefn in row.Columns)
                {
                    _writer.WriteStartElement("TableColumnItem");
                    if (coldefn.Alignment != Alignment.Undefined)
                    {
                        _writer.WriteElementString("Alignment", coldefn.Alignment.ToString());
                    }

                    if (!string.IsNullOrEmpty(coldefn.FormatString))
                    {
                        _writer.WriteElementString("FormatString", coldefn.FormatString);
                    }

                    WriteDisplayEntry(coldefn.DisplayEntry);
                    _writer.WriteEndElement(/*</TableColumnItem>*/);
                }

                _writer.WriteEndElement(/*<TableColumnItems>*/);
                _writer.WriteEndElement(/*<TableRowEntry>*/);
            }

            _writer.WriteEndElement(/*</TableRowEntries>*/);

            _writer.WriteEndElement(/*</TableControl>*/);
        }

        internal void WriteListControl(ListControl listControl)
        {
            _writer.WriteStartElement("ListControl");
            _writer.WriteStartElement("ListEntries");

            // write the list entry's one by one
            foreach (ListControlEntry entry in listControl.Entries)
            {
                _writer.WriteStartElement("ListEntry");

                // write entry selected by if available
                WriteEntrySelectedBy(entry.EntrySelectedBy);

                if (entry.Items.Count > 0)
                {
                    _writer.WriteStartElement("ListItems");

                    // write the list items
                    foreach (ListControlEntryItem item in entry.Items)
                    {
                        _writer.WriteStartElement("ListItem");

                        if (!string.IsNullOrEmpty(item.Label))
                        {
                            _writer.WriteElementString("Label", item.Label);
                        }

                        if (!string.IsNullOrEmpty(item.FormatString))
                        {
                            _writer.WriteElementString("FormatString", item.FormatString);
                        }

                        if (item.ItemSelectionCondition != null)
                        {
                            _writer.WriteStartElement("ItemSelectionCondition");
                            WriteDisplayEntry(item.ItemSelectionCondition);
                            _writer.WriteEndElement(/*</ItemSelectionCondition>*/);
                        }

                        // write the entry
                        WriteDisplayEntry(item.DisplayEntry);

                        _writer.WriteEndElement(/*</ListItem>*/);
                    }

                    _writer.WriteEndElement(/*</ListItems>*/);
                }

                _writer.WriteEndElement(/*</ListEntry>*/);
            }

            _writer.WriteEndElement(/*</ListEntries>*/);
            _writer.WriteEndElement(/*</ListControl>*/);
        }

        private void WriteEntrySelectedBy(EntrySelectedBy entrySelectedBy)
        {
            if (entrySelectedBy != null &&
                ((entrySelectedBy.TypeNames != null && entrySelectedBy.TypeNames.Count > 0) ||
                 (entrySelectedBy.SelectionCondition != null && entrySelectedBy.SelectionCondition.Count > 0)))
            {
                _writer.WriteStartElement("EntrySelectedBy");

                if (entrySelectedBy.TypeNames != null)
                {
                    foreach (string typename in entrySelectedBy.TypeNames)
                    {
                        _writer.WriteElementString("TypeName", typename);
                    }
                }

                if (entrySelectedBy.SelectionCondition != null)
                {
                    foreach (var condition in entrySelectedBy.SelectionCondition)
                    {
                        _writer.WriteStartElement("SelectionCondition");
                        WriteDisplayEntry(condition);
                        _writer.WriteEndElement(/*</SelectionCondition>*/);
                    }
                }

                _writer.WriteEndElement(/*</EntrySelectedBy>*/);
            }
        }

        internal void WriteWideControl(WideControl wideControl)
        {
            _writer.WriteStartElement("WideControl");

            if (wideControl.Columns > 0)
            {
                _writer.WriteElementString("ColumnNumber", wideControl.Columns.ToString(CultureInfo.InvariantCulture));
            }

            if (wideControl.AutoSize)
            {
                _writer.WriteElementString("AutoSize", string.Empty);
            }

            _writer.WriteStartElement("WideEntries");
            foreach (WideControlEntryItem entry in wideControl.Entries)
            {
                _writer.WriteStartElement("WideEntry");

                WriteEntrySelectedBy(entry.EntrySelectedBy);

                _writer.WriteStartElement("WideItem");
                WriteDisplayEntry(entry.DisplayEntry);
                if (!string.IsNullOrEmpty(entry.FormatString))
                {
                    _writer.WriteElementString("FormatString", entry.FormatString);
                }

                _writer.WriteEndElement(/*</WideItem>*/);

                _writer.WriteEndElement(/*</WideEntry>*/);
            }

            _writer.WriteEndElement(/*</WideEntries>*/);

            _writer.WriteEndElement(/*</WideControl>*/);
        }

        internal void WriteDisplayEntry(DisplayEntry displayEntry)
        {
            if (displayEntry.ValueType == DisplayEntryValueType.Property)
            {
                _writer.WriteElementString("PropertyName", displayEntry.Value);
            }
            else if (displayEntry.ValueType == DisplayEntryValueType.ScriptBlock)
            {
                _writer.WriteStartElement("ScriptBlock");
                _writer.WriteValue(_exportScriptBlock ? displayEntry.Value : ";");
                _writer.WriteEndElement(/*</ScriptBlock>*/);
            }
        }

        internal void WriteCustomControl(CustomControl customControl)
        {
            _writer.WriteStartElement("CustomControl");

            _writer.WriteStartElement("CustomEntries");
            foreach (var entry in customControl.Entries)
            {
                _writer.WriteStartElement("CustomEntry");
                WriteEntrySelectedBy(entry.SelectedBy);
                _writer.WriteStartElement("CustomItem");
                foreach (var item in entry.CustomItems)
                {
                    WriteCustomItem(item);
                }

                _writer.WriteEndElement(/*</CustomItem>*/);
                _writer.WriteEndElement(/*</CustomEntry>*/);
            }

            _writer.WriteEndElement(/*</CustomEntries>*/);
            _writer.WriteEndElement(/*</CustomControl>*/);
        }

        internal void WriteCustomItem(CustomItemBase item)
        {
            if (item is CustomItemNewline newline)
            {
                for (int i = 0; i < newline.Count; i++)
                {
                    _writer.WriteElementString("NewLine", string.Empty);
                }

                return;
            }

            if (item is CustomItemText text)
            {
                _writer.WriteElementString("Text", text.Text);
                return;
            }

            if (item is CustomItemExpression expr)
            {
                _writer.WriteStartElement("ExpressionBinding");
                if (expr.EnumerateCollection)
                {
                    _writer.WriteElementString("EnumerateCollection", string.Empty);
                }

                if (expr.ItemSelectionCondition != null)
                {
                    _writer.WriteStartElement("ItemSelectionCondition");
                    WriteDisplayEntry(expr.ItemSelectionCondition);
                    _writer.WriteEndElement(/*</ItemSelectionCondition>*/);
                }

                if (expr.Expression != null)
                {
                    WriteDisplayEntry(expr.Expression);
                }

                if (expr.CustomControl != null)
                {
                    WriteCustomControl(expr.CustomControl);
                }

                _writer.WriteEndElement(/*</ExpressionBinding>*/);
                return;
            }

            var frame = (CustomItemFrame)item;
            _writer.WriteStartElement("Frame");
            if (frame.LeftIndent != 0)
                _writer.WriteElementString("LeftIndent", frame.LeftIndent.ToString(CultureInfo.InvariantCulture));
            if (frame.RightIndent != 0)
                _writer.WriteElementString("RightIndent", frame.RightIndent.ToString(CultureInfo.InvariantCulture));
            if (frame.FirstLineHanging != 0)
                _writer.WriteElementString("FirstLineHanging", frame.FirstLineHanging.ToString(CultureInfo.InvariantCulture));
            if (frame.FirstLineIndent != 0)
                _writer.WriteElementString("FirstLineIndent", frame.FirstLineIndent.ToString(CultureInfo.InvariantCulture));

            _writer.WriteStartElement("CustomItem");
            foreach (var frameItem in frame.CustomItems)
            {
                WriteCustomItem(frameItem);
            }

            _writer.WriteEndElement(/*</CustomItem>*/);
            _writer.WriteEndElement(/*</Frame>*/);
        }
    }
}
