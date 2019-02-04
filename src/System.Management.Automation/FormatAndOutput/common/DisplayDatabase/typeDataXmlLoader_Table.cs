// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Xml;
using System.Management.Automation.Internal;

namespace Microsoft.PowerShell.Commands.Internal.Format
{
    /// <summary>
    /// Class to load the XML document into data structures.
    /// It encapsulates the file format specific code.
    /// </summary>
    internal sealed partial class TypeInfoDataBaseLoader : XmlLoaderBase
    {
        private ControlBase LoadTableControl(XmlNode controlNode)
        {
            using (this.StackFrame(controlNode))
            {
                TableControlBody tableBody = new TableControlBody();
                bool headersNodeFound = false;      // cardinality 0..1
                bool rowEntriesNodeFound = false;   // cardinality 1
                bool hideHeadersNodeFound = false;   // cardinality 0..1
                bool autosizeNodeFound = false;   // cardinality 0..1

                foreach (XmlNode n in controlNode.ChildNodes)
                {
                    if (MatchNodeName(n, XmlTags.HideTableHeadersNode))
                    {
                        if (hideHeadersNodeFound)
                        {
                            this.ProcessDuplicateNode(n);
                            return null; // fatal error
                        }

                        hideHeadersNodeFound = true;
                        if (!this.ReadBooleanNode(n, out tableBody.header.hideHeader))
                        {
                            return null; // fatal error
                        }
                    }
                    else if (MatchNodeName(n, XmlTags.AutoSizeNode))
                    {
                        if (autosizeNodeFound)
                        {
                            this.ProcessDuplicateNode(n);
                            return null; // fatal error
                        }

                        autosizeNodeFound = true;
                        bool tempVal;
                        if (!this.ReadBooleanNode(n, out tempVal))
                        {
                            return null; // fatal error
                        }

                        tableBody.autosize = tempVal;
                    }
                    else if (MatchNodeName(n, XmlTags.TableHeadersNode))
                    {
                        if (headersNodeFound)
                        {
                            this.ProcessDuplicateNode(n);
                            return null; // fatal error
                        }

                        headersNodeFound = true;

                        // now read the columns header section
                        LoadHeadersSection(tableBody, n);
                        if (tableBody.header.columnHeaderDefinitionList == null)
                        {
                            // if we have an empty list, it means there was a failure
                            return null; // fatal error
                        }
                    }
                    else if (MatchNodeName(n, XmlTags.TableRowEntriesNode))
                    {
                        if (rowEntriesNodeFound)
                        {
                            this.ProcessDuplicateNode(n);
                            return null; // fatal error
                        }

                        rowEntriesNodeFound = true;

                        // now read the columns section
                        LoadRowEntriesSection(tableBody, n);
                        if (tableBody.defaultDefinition == null)
                        {
                            return null; // fatal error
                        }
                    }
                    else
                    {
                        this.ProcessUnknownNode(n);
                    }
                }

                if (!rowEntriesNodeFound)
                {
                    this.ReportMissingNode(XmlTags.TableRowEntriesNode);
                    return null; // fatal error
                }

                // CHECK: verify consistency of headers and row entries
                if (tableBody.header.columnHeaderDefinitionList.Count != 0)
                {
                    // CHECK: if there are headers in the list, their number has to match
                    // the default row definition item count
                    if (tableBody.header.columnHeaderDefinitionList.Count !=
                        tableBody.defaultDefinition.rowItemDefinitionList.Count)
                    {
                        // Error at XPath {0} in file {1}: Header item count = {2} does not match default row item count = {3}.
                        this.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.IncorrectHeaderItemCount, ComputeCurrentXPath(), FilePath,
                            tableBody.header.columnHeaderDefinitionList.Count,
                            tableBody.defaultDefinition.rowItemDefinitionList.Count));

                        return null; // fatal error
                    }
                }

                // CHECK: if there are alternative row definitions, they should have the same # of items
                if (tableBody.optionalDefinitionList.Count != 0)
                {
                    int k = 0;
                    foreach (TableRowDefinition trd in tableBody.optionalDefinitionList)
                    {
                        if (trd.rowItemDefinitionList.Count !=
                            tableBody.defaultDefinition.rowItemDefinitionList.Count)
                        {
                            // Error at XPath {0} in file {1}: Row item count = {2} on alternative set #{3} does not match default row item count = {4}.
                            this.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.IncorrectRowItemCount, ComputeCurrentXPath(), FilePath,
                                trd.rowItemDefinitionList.Count,
                                tableBody.defaultDefinition.rowItemDefinitionList.Count, k + 1));

                            return null; // fatal error
                        }

                        k++;
                    }
                }

                return tableBody;
            }
        }

        private void LoadHeadersSection(TableControlBody tableBody, XmlNode headersNode)
        {
            using (this.StackFrame(headersNode))
            {
                int columnIndex = 0;
                foreach (XmlNode n in headersNode.ChildNodes)
                {
                    if (MatchNodeName(n, XmlTags.TableColumnHeaderNode))
                    {
                        TableColumnHeaderDefinition chd = LoadColumnHeaderDefinition(n, columnIndex++);

                        if (chd != null)
                            tableBody.header.columnHeaderDefinitionList.Add(chd);
                        else
                        {
                            // Error at XPath {0} in file {1}: Column header definition is invalid; all headers are discarded.
                            this.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.InvalidColumnHeader, ComputeCurrentXPath(), FilePath));
                            tableBody.header.columnHeaderDefinitionList = null;
                            return; // fatal error
                        }
                    }
                    else
                    {
                        this.ProcessUnknownNode(n);
                    }
                }
                // NOTICE: the list can be empty if no entries were found
            }
        }

        private TableColumnHeaderDefinition LoadColumnHeaderDefinition(XmlNode columnHeaderNode, int index)
        {
            using (this.StackFrame(columnHeaderNode, index))
            {
                TableColumnHeaderDefinition chd = new TableColumnHeaderDefinition();

                bool labelNodeFound = false; // cardinality 0..1
                bool widthNodeFound = false; // cardinality 0..1
                bool alignmentNodeFound = false; // cardinality 0..1

                foreach (XmlNode n in columnHeaderNode.ChildNodes)
                {
                    if (MatchNodeNameWithAttributes(n, XmlTags.LabelNode))
                    {
                        if (labelNodeFound)
                        {
                            this.ProcessDuplicateNode(n);
                            return null; // fatal error
                        }

                        labelNodeFound = true;
                        chd.label = LoadLabel(n);
                        if (chd.label == null)
                        {
                            return null; // fatal error
                        }
                    }
                    else if (MatchNodeName(n, XmlTags.WidthNode))
                    {
                        if (widthNodeFound)
                        {
                            this.ProcessDuplicateNode(n);
                            return null; // fatal error
                        }

                        widthNodeFound = true;
                        int wVal;
                        if (ReadPositiveIntegerValue(n, out wVal))
                        {
                            chd.width = wVal;
                        }
                        else
                        {
                            // Error at XPath {0} in file {1}: Invalid {2} value.
                            this.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.InvalidNodeValue, ComputeCurrentXPath(), FilePath, XmlTags.WidthNode));
                            return null; // fatal error
                        }
                    }
                    else if (MatchNodeName(n, XmlTags.AlignmentNode))
                    {
                        if (alignmentNodeFound)
                        {
                            this.ProcessDuplicateNode(n);
                            return null; // fatal error
                        }

                        alignmentNodeFound = true;
                        if (!LoadAlignmentValue(n, out chd.alignment))
                        {
                            return null; // fatal error
                        }
                    }
                    else
                    {
                        this.ProcessUnknownNode(n);
                    }
                }

                return chd;
            }
        }

        private bool ReadPositiveIntegerValue(XmlNode n, out int val)
        {
            val = -1;
            string text = GetMandatoryInnerText(n);
            if (text == null)
                return false;
            bool isInteger = int.TryParse(text, out val);
            if (!isInteger || val <= 0)
            {
                // Error at XPath {0} in file {1}: A positive integer is expected.
                this.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.ExpectPositiveInteger, ComputeCurrentXPath(), FilePath));
                return false;
            }

            return true;
        }

        private bool LoadAlignmentValue(XmlNode n, out int alignmentValue)
        {
            alignmentValue = TextAlignment.Undefined;
            string alignmentString = GetMandatoryInnerText(n);
            if (alignmentString == null)
            {
                return false; // fatal error
            }

            if (string.Equals(n.InnerText, XMLStringValues.AlignmentLeft, StringComparison.OrdinalIgnoreCase))
            {
                alignmentValue = TextAlignment.Left;
            }
            else if (string.Equals(n.InnerText, XMLStringValues.AlignmentRight, StringComparison.OrdinalIgnoreCase))
            {
                alignmentValue = TextAlignment.Right;
            }
            else if (string.Equals(n.InnerText, XMLStringValues.AlignmentCenter, StringComparison.OrdinalIgnoreCase))
            {
                alignmentValue = TextAlignment.Center;
            }
            else
            {
                // Error at XPath {0} in file {1}: "{2}" is not an valid alignment value.
                this.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.InvalidAlignmentValue, ComputeCurrentXPath(), FilePath, alignmentString));
                return false; // fatal error
            }

            return true;
        }

        private void LoadRowEntriesSection(TableControlBody tableBody, XmlNode rowEntriesNode)
        {
            using (this.StackFrame(rowEntriesNode))
            {
                int rowEntryIndex = 0;
                foreach (XmlNode n in rowEntriesNode.ChildNodes)
                {
                    if (MatchNodeName(n, XmlTags.TableRowEntryNode))
                    {
                        TableRowDefinition trd = LoadRowEntryDefinition(n, rowEntryIndex++);
                        if (trd == null)
                        {
                            // Error at XPath {0} in file {1}: {2} failed to load.
                            this.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.LoadTagFailed, ComputeCurrentXPath(), FilePath, XmlTags.TableRowEntryNode));
                            tableBody.defaultDefinition = null;
                            return; // fatal error
                        }

                        // determine if we have a default entry and if it's already set
                        if (trd.appliesTo == null)
                        {
                            if (tableBody.defaultDefinition == null)
                            {
                                tableBody.defaultDefinition = trd;
                            }
                            else
                            {
                                // Error at XPath {0} in file {1}: There cannot be more than one default {2}.
                                this.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.TooManyDefaultShapeEntry, ComputeCurrentXPath(), FilePath, XmlTags.TableRowEntryNode));
                                tableBody.defaultDefinition = null;
                                return; // fatal error
                            }
                        }
                        else
                        {
                            tableBody.optionalDefinitionList.Add(trd);
                        }
                    }
                    else
                    {
                        this.ProcessUnknownNode(n);
                    }
                }

                if (tableBody.defaultDefinition == null)
                {
                    // Error at XPath {0} in file {1}: There must be at least one default {2}.
                    this.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.NoDefaultShapeEntry, ComputeCurrentXPath(), FilePath, XmlTags.TableRowEntryNode));
                }
            }
        }

        private TableRowDefinition LoadRowEntryDefinition(XmlNode rowEntryNode, int index)
        {
            using (this.StackFrame(rowEntryNode, index))
            {
                bool appliesToNodeFound = false;    // cardinality 0..1
                bool columnEntriesNodeFound = false;         // cardinality 1
                bool multiLineFound = false;    // cardinality 0..1

                TableRowDefinition trd = new TableRowDefinition();
                foreach (XmlNode n in rowEntryNode.ChildNodes)
                {
                    if (MatchNodeName(n, XmlTags.EntrySelectedByNode))
                    {
                        if (appliesToNodeFound)
                        {
                            this.ProcessDuplicateNode(n);
                            return null; // fatal error
                        }

                        appliesToNodeFound = true;

                        // optional section
                        trd.appliesTo = LoadAppliesToSection(n, true);
                    }
                    else if (MatchNodeName(n, XmlTags.TableColumnItemsNode))
                    {
                        if (columnEntriesNodeFound)
                        {
                            this.ProcessDuplicateNode(n);
                            return null; // fatal
                        }

                        LoadColumnEntries(n, trd);
                        if (trd.rowItemDefinitionList == null)
                        {
                            return null; // fatal error
                        }
                    }
                    else if (MatchNodeName(n, XmlTags.MultiLineNode))
                    {
                        if (multiLineFound)
                        {
                            this.ProcessDuplicateNode(n);
                            return null; // fatal
                        }

                        multiLineFound = true;
                        if (!this.ReadBooleanNode(n, out trd.multiLine))
                        {
                            return null; // fatal error
                        }
                    }
                    else
                    {
                        this.ProcessUnknownNode(n);
                    }
                }

                return trd;
            }
        }

        private void LoadColumnEntries(XmlNode columnEntriesNode, TableRowDefinition trd)
        {
            using (this.StackFrame(columnEntriesNode))
            {
                int columnEntryIndex = 0;
                foreach (XmlNode n in columnEntriesNode.ChildNodes)
                {
                    if (MatchNodeName(n, XmlTags.TableColumnItemNode))
                    {
                        TableRowItemDefinition rid = LoadColumnEntry(n, columnEntryIndex++);
                        if (rid != null)
                        {
                            trd.rowItemDefinitionList.Add(rid);
                        }
                        else
                        {
                            // we failed one entry: fatal error to percolate up
                            // remove all the entries
                            trd.rowItemDefinitionList = null;
                            return; // fatal error
                        }
                    }
                    else
                    {
                        this.ProcessUnknownNode(n);
                    }
                }
            }
        }

        private TableRowItemDefinition LoadColumnEntry(XmlNode columnEntryNode, int index)
        {
            using (this.StackFrame(columnEntryNode, index))
            {
                // process Mshexpression, format string and text token
                ViewEntryNodeMatch match = new ViewEntryNodeMatch(this);
                List<XmlNode> unprocessedNodes = new List<XmlNode>();
                if (!match.ProcessExpressionDirectives(columnEntryNode, unprocessedNodes))
                {
                    return null; // fatal error
                }

                TableRowItemDefinition rid = new TableRowItemDefinition();

                // process the remaining nodes
                bool alignmentNodeFound = false; // cardinality 0..1
                foreach (XmlNode n in unprocessedNodes)
                {
                    if (MatchNodeName(n, XmlTags.AlignmentNode))
                    {
                        if (alignmentNodeFound)
                        {
                            this.ProcessDuplicateNode(n);
                            return null; // fatal error
                        }

                        alignmentNodeFound = true;
                        if (!LoadAlignmentValue(n, out rid.alignment))
                        {
                            return null; // fatal error
                        }
                    }
                    else
                    {
                        this.ProcessUnknownNode(n);
                    }
                }

                // finally build the item to return
                // add either the text token or the PSPropertyExpression with optional format string
                if (match.TextToken != null)
                {
                    rid.formatTokenList.Add(match.TextToken);
                }
                else if (match.Expression != null)
                {
                    FieldPropertyToken fpt = new FieldPropertyToken();
                    fpt.expression = match.Expression;
                    fpt.fieldFormattingDirective.formatString = match.FormatString;
                    rid.formatTokenList.Add(fpt);
                }

                return rid;
            }
        }
    }
}
