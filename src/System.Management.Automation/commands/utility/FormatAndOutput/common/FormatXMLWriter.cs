/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Management.Automation;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Helper class for writing formatting directives to XML
    /// </summary>
    internal class FormatXmlWriter
    {
        private XmlWriter writer;
        private bool exportScriptBlock;

        private FormatXmlWriter() { }

        /// <summary>
        /// Writes a collection of format view definitions to XML file
        /// </summary>
        /// <param name="typeDefinitions">collection of PSTypeDefinition</param>
        /// <param name="filepath">path to XML file</param>
        /// <param name="cmdlet">cmdlet from which this si used</param>
        /// <param name="force">true - to force write the file</param>
        /// <param name="writeScriptBlock">true - to export scriptblocks</param>
        /// <param name="noclobber">true - do not overwrite the file</param>
        /// <param name="isLiteralPath">true - bypass wildcard expansion on the file name</param>
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
                using (XmlWriter xmlWriter = XmlWriter.Create(streamWriter))
                {
                    var writer = new FormatXmlWriter
                    {
                        writer = xmlWriter,
                        exportScriptBlock = writeScriptBlock
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
            var formatXmlWriter = new FormatXmlWriter {exportScriptBlock = writeScriptBlock, writer = writer};
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

            writer.WriteStartElement("Configuration");

            writer.WriteStartElement("ViewDefinitions");
            foreach(var pair in formatdefs)
            {
                Guid id = pair.Key;
                FormatViewDefinition formatdef = pair.Value;

                writer.WriteStartElement("View");
                writer.WriteElementString("Name", formatdef.Name);
                writer.WriteStartElement("ViewSelectedBy");
                foreach (ExtendedTypeDefinition definition in views[id])
                {
                    writer.WriteElementString("TypeName", definition.TypeName);
                }
                writer.WriteEndElement(/*</ViewSelectedBy>*/);

                var groupBy = formatdef.Control.GroupBy;
                if (groupBy != null)
                {
                    writer.WriteStartElement("GroupBy");
                    WriteDisplayEntry(groupBy.Expression);
                    if (!string.IsNullOrEmpty(groupBy.Label))
                    {
                        writer.WriteElementString("Label", groupBy.Label);
                    }
                    if (groupBy.CustomControl != null)
                    {
                        WriteCustomControl(groupBy.CustomControl);
                    }
                    writer.WriteEndElement(/*</GroupBy>*/);
                }
                if (formatdef.Control.OutOfBand)
                {
                    writer.WriteElementString("OutOfBand", "");
                }

                formatdef.Control.WriteToXml(this);
                writer.WriteEndElement(/*</View>*/);
            }
            writer.WriteEndElement(/*</ViewDefinitions>*/);

            writer.WriteEndElement(/*</Configuration>*/);
        }

        internal void WriteTableControl(TableControl tableControl)
        {
            writer.WriteStartElement("TableControl");
            if (tableControl.AutoSize)
            {
                writer.WriteElementString("AutoSize", "");
            }
            if (tableControl.HideTableHeaders)
            {
                writer.WriteElementString("HideTableHeaders", "");
            }

            writer.WriteStartElement("TableHeaders");
            foreach (TableControlColumnHeader columnheader in tableControl.Headers)
            {
                writer.WriteStartElement("TableColumnHeader");
                if (!string.IsNullOrEmpty(columnheader.Label))
                {
                    writer.WriteElementString("Label", columnheader.Label);
                }
                if (columnheader.Width > 0)
                {
                    writer.WriteElementString("Width", columnheader.Width.ToString(CultureInfo.InvariantCulture));
                }
                if (columnheader.Alignment != Alignment.Undefined)
                {
                    writer.WriteElementString("Alignment", columnheader.Alignment.ToString());
                }
                writer.WriteEndElement(/*</TableColumnHeader>*/);
            }
            writer.WriteEndElement(/*</TableHeaders>*/);

            writer.WriteStartElement("TableRowEntries");
            foreach (TableControlRow row in tableControl.Rows)
            {
                writer.WriteStartElement("TableRowEntry");
                if (row.Wrap)
                {
                    writer.WriteStartElement("Wrap");
                    writer.WriteEndElement(/*</Wrap>*/);
                }
                if (row.SelectedBy != null)
                {
                    WriteEntrySelectedBy(row.SelectedBy);
                }
                writer.WriteStartElement("TableColumnItems");
                foreach (TableControlColumn coldefn in row.Columns)
                {
                    writer.WriteStartElement("TableColumnItem");
                    if (coldefn.Alignment != Alignment.Undefined)
                    {
                        writer.WriteElementString("Alignment", coldefn.Alignment.ToString());
                    }
                    if (!string.IsNullOrEmpty(coldefn.FormatString))
                    {
                        writer.WriteElementString("FormatString", coldefn.FormatString);
                    }
                    WriteDisplayEntry(coldefn.DisplayEntry);
                    writer.WriteEndElement(/*</TableColumnItem>*/);
                }

                writer.WriteEndElement(/*<TableColumnItems>*/);
                writer.WriteEndElement(/*<TableRowEntry>*/);
            }
            writer.WriteEndElement(/*</TableRowEntries>*/);

            writer.WriteEndElement(/*</TableControl>*/);
        }

        internal void WriteListControl(ListControl listControl)
        {
            writer.WriteStartElement("ListControl");
            writer.WriteStartElement("ListEntries");

            // write the list entry's one by one
            foreach (ListControlEntry entry in listControl.Entries)
            {
                writer.WriteStartElement("ListEntry");

                // write entry selected by if available
                WriteEntrySelectedBy(entry.EntrySelectedBy);

                if (entry.Items.Count > 0)
                {
                    writer.WriteStartElement("ListItems");

                    // write the list items
                    foreach (ListControlEntryItem item in entry.Items)
                    {
                        writer.WriteStartElement("ListItem");

                        if (!string.IsNullOrEmpty(item.Label))
                        {
                            writer.WriteElementString("Label", item.Label);
                        }

                        if (!string.IsNullOrEmpty(item.FormatString))
                        {
                            writer.WriteElementString("FormatString", item.FormatString);
                        }

                        if (item.ItemSelectionCondition != null)
                        {
                            writer.WriteStartElement("ItemSelectionCondition");
                            WriteDisplayEntry(item.ItemSelectionCondition);
                            writer.WriteEndElement(/*</ItemSelectionCondition>*/);
                        }

                        // write the entry
                        WriteDisplayEntry(item.DisplayEntry);

                        writer.WriteEndElement(/*</ListItem>*/);
                    }

                    writer.WriteEndElement(/*</ListItems>*/);
                }

                writer.WriteEndElement(/*</ListEntry>*/);
            }

            writer.WriteEndElement(/*</ListEntries>*/);
            writer.WriteEndElement(/*</ListControl>*/);
        }

        private void WriteEntrySelectedBy(EntrySelectedBy entrySelectedBy)
        {
            if (entrySelectedBy != null &&
                ((entrySelectedBy.TypeNames != null && entrySelectedBy.TypeNames.Count > 0) ||
                 (entrySelectedBy.SelectionCondition != null && entrySelectedBy.SelectionCondition.Count > 0)))
            {
                writer.WriteStartElement("EntrySelectedBy");

                if (entrySelectedBy.TypeNames != null)
                {
                    foreach (string typename in entrySelectedBy.TypeNames)
                    {
                        writer.WriteElementString("TypeName", typename);
                    }
                }
                if (entrySelectedBy.SelectionCondition != null)
                {
                    foreach (var condition in entrySelectedBy.SelectionCondition)
                    {
                        writer.WriteStartElement("SelectionCondition");
                        WriteDisplayEntry(condition);
                        writer.WriteEndElement(/*</SelectionCondition>*/);
                    }
                }
                writer.WriteEndElement(/*</EntrySelectedBy>*/);
            }
        }

        internal void WriteWideControl(WideControl wideControl)
        {
            writer.WriteStartElement("WideControl");

            if (wideControl.Columns > 0)
            {
                writer.WriteElementString("ColumnNumber", wideControl.Columns.ToString(CultureInfo.InvariantCulture));
            }

            if (wideControl.AutoSize)
            {
                writer.WriteElementString("AutoSize", "");
            }

            writer.WriteStartElement("WideEntries");
            foreach (WideControlEntryItem entry in wideControl.Entries)
            {
                writer.WriteStartElement("WideEntry");

                WriteEntrySelectedBy(entry.EntrySelectedBy);

                writer.WriteStartElement("WideItem");
                WriteDisplayEntry(entry.DisplayEntry);
                if (!string.IsNullOrEmpty(entry.FormatString))
                {
                    writer.WriteElementString("FormatString", entry.FormatString);
                }
                writer.WriteEndElement(/*</WideItem>*/);
                
                writer.WriteEndElement(/*</WideEntry>*/);
            }
            writer.WriteEndElement(/*</WideEntries>*/);
            
            writer.WriteEndElement(/*</WideControl>*/);
        }

        internal void WriteDisplayEntry(DisplayEntry displayEntry)
        {
            if (displayEntry.ValueType == DisplayEntryValueType.Property)
            {
                writer.WriteElementString("PropertyName", displayEntry.Value);
            }
            else if (displayEntry.ValueType == DisplayEntryValueType.ScriptBlock)
            {
                writer.WriteStartElement("ScriptBlock");
                writer.WriteValue(exportScriptBlock ? displayEntry.Value : ";");
                writer.WriteEndElement(/*</ScriptBlock>*/);
            }
        }

        internal void WriteCustomControl(CustomControl customControl)
        {
            writer.WriteStartElement("CustomControl");

            writer.WriteStartElement("CustomEntries");
            foreach (var entry in customControl.Entries)
            {
                writer.WriteStartElement("CustomEntry");
                WriteEntrySelectedBy(entry.SelectedBy);
                writer.WriteStartElement("CustomItem");
                foreach (var item in entry.CustomItems)
                {
                    WriteCustomItem(item);
                }
                writer.WriteEndElement(/*</CustomItem>*/);
                writer.WriteEndElement(/*</CustomEntry>*/);
            }
            writer.WriteEndElement(/*</CustomEntries>*/);
            writer.WriteEndElement(/*</CustomControl>*/);
        }

        internal void WriteCustomItem(CustomItemBase item)
        {
            var newline = item as CustomItemNewline;
            if (newline != null)
            {
                for (int i = 0; i < newline.Count; i++)
                {
                    writer.WriteElementString("NewLine", "");
                }
                return;
            }

            var text = item as CustomItemText;
            if (text != null)
            {
                writer.WriteElementString("Text", text.Text);
                return;
            }

            var expr = item as CustomItemExpression;
            if (expr != null)
            {
                writer.WriteStartElement("ExpressionBinding");
                if (expr.EnumerateCollection)
                {
                    writer.WriteElementString("EnumerateCollection", "");
                }

                if (expr.ItemSelectionCondition != null)
                {
                    writer.WriteStartElement("ItemSelectionCondition");
                    WriteDisplayEntry(expr.ItemSelectionCondition);
                    writer.WriteEndElement(/*</ItemSelectionCondition>*/);
                }

                if (expr.Expression != null)
                {
                    WriteDisplayEntry(expr.Expression);
                }

                if (expr.CustomControl != null)
                {
                    WriteCustomControl(expr.CustomControl);
                }

                writer.WriteEndElement(/*</ExpressionBinding>*/);
                return;
            }

            var frame = (CustomItemFrame) item;
            writer.WriteStartElement("Frame");
            if (frame.LeftIndent != 0)
                writer.WriteElementString("LeftIndent", frame.LeftIndent.ToString(CultureInfo.InvariantCulture));
            if (frame.RightIndent != 0)
                writer.WriteElementString("RightIndent", frame.RightIndent.ToString(CultureInfo.InvariantCulture));
            if (frame.FirstLineHanging != 0)
                writer.WriteElementString("FirstLineHanging", frame.FirstLineHanging.ToString(CultureInfo.InvariantCulture));
            if (frame.FirstLineIndent != 0)
                writer.WriteElementString("FirstLineIndent", frame.FirstLineIndent.ToString(CultureInfo.InvariantCulture));

            writer.WriteStartElement("CustomItem");
            foreach (var frameItem in frame.CustomItems)
            {
                WriteCustomItem(frameItem);
            }
            writer.WriteEndElement(/*</CustomItem>*/);
            writer.WriteEndElement(/*</Frame>*/);
        }
    }
}
