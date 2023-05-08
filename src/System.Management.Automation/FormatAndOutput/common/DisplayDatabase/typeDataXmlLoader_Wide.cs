// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Management.Automation.Internal;
using System.Xml;

namespace Microsoft.PowerShell.Commands.Internal.Format
{
    /// <summary>
    /// Class to load the XML document into data structures.
    /// It encapsulates the file format specific code.
    /// </summary>
    internal sealed partial class TypeInfoDataBaseLoader : XmlLoaderBase
    {
        private WideControlBody LoadWideControl(XmlNode controlNode)
        {
            using (this.StackFrame(controlNode))
            {
                WideControlBody wideBody = new WideControlBody();

                bool wideViewEntriesFound = false;

                // mutually exclusive
                bool autosizeNodeFound = false;   // cardinality 0..1
                bool columnsNodeFound = false; // cardinality 0..1

                foreach (XmlNode n in controlNode.ChildNodes)
                {
                    if (MatchNodeName(n, XmlTags.AutoSizeNode))
                    {
                        if (autosizeNodeFound)
                        {
                            this.ProcessDuplicateAlternateNode(n, XmlTags.AutoSizeNode, XmlTags.ColumnNumberNode);
                            return null; // fatal error
                        }

                        autosizeNodeFound = true;
                        bool tempVal;
                        if (!this.ReadBooleanNode(n, out tempVal))
                        {
                            return null; // fatal error
                        }

                        wideBody.autosize = tempVal;
                    }
                    else if (MatchNodeName(n, XmlTags.ColumnNumberNode))
                    {
                        if (columnsNodeFound)
                        {
                            this.ProcessDuplicateAlternateNode(n, XmlTags.AutoSizeNode, XmlTags.ColumnNumberNode);
                            return null; // fatal error
                        }

                        columnsNodeFound = true;

                        if (!ReadPositiveIntegerValue(n, out wideBody.columns))
                        {
                            return null; // fatal error
                        }
                    }
                    else if (MatchNodeName(n, XmlTags.WideEntriesNode))
                    {
                        if (wideViewEntriesFound)
                        {
                            this.ProcessDuplicateNode(n);
                            continue;
                        }

                        wideViewEntriesFound = true;

                        // now read the entries section
                        LoadWideControlEntries(n, wideBody);
                        if (wideBody.defaultEntryDefinition == null)
                        {
                            // if we have an default entry, it means there was a failure
                            return null; // fatal error
                        }
                    }
                    else
                    {
                        this.ProcessUnknownNode(n);
                    }
                }

                if (autosizeNodeFound && columnsNodeFound)
                {
                    this.ProcessDuplicateAlternateNode(XmlTags.AutoSizeNode, XmlTags.ColumnNumberNode);
                    return null; // fatal error
                }

                if (!wideViewEntriesFound)
                {
                    this.ReportMissingNode(XmlTags.WideEntriesNode);
                    return null; // fatal error
                }

                return wideBody;
            }
        }

        private void LoadWideControlEntries(XmlNode wideControlEntriesNode, WideControlBody wideBody)
        {
            using (this.StackFrame(wideControlEntriesNode))
            {
                int entryIndex = 0;

                foreach (XmlNode n in wideControlEntriesNode.ChildNodes)
                {
                    if (MatchNodeName(n, XmlTags.WideEntryNode))
                    {
                        WideControlEntryDefinition wved = LoadWideControlEntry(n, entryIndex++);
                        if (wved == null)
                        {
                            // Error at XPath {0} in file {1}: Invalid {2}.
                            this.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.InvalidNode, ComputeCurrentXPath(), FilePath, XmlTags.WideEntryNode));
                            return;
                        }
                        // determine if we have a default entry and if it's already set
                        if (wved.appliesTo == null)
                        {
                            if (wideBody.defaultEntryDefinition == null)
                            {
                                wideBody.defaultEntryDefinition = wved;
                            }
                            else
                            {
                                // Error at XPath {0} in file {1}: There cannot be more than one default {2}.
                                this.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.TooManyDefaultShapeEntry, ComputeCurrentXPath(), FilePath, XmlTags.WideEntryNode));
                                wideBody.defaultEntryDefinition = null;
                                return; // fatal error
                            }
                        }
                        else
                        {
                            wideBody.optionalEntryList.Add(wved);
                        }
                    }
                    else
                    {
                        this.ProcessUnknownNode(n);
                    }
                }

                if (wideBody.defaultEntryDefinition == null)
                {
                    // Error at XPath {0} in file {1}: There must be at least one default {2}.
                    this.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.NoDefaultShapeEntry, ComputeCurrentXPath(), FilePath, XmlTags.WideEntryNode));
                }
            }
        }

        private WideControlEntryDefinition LoadWideControlEntry(XmlNode wideControlEntryNode, int index)
        {
            using (this.StackFrame(wideControlEntryNode, index))
            {
                bool appliesToNodeFound = false;     // cardinality 0..1
                bool propertyEntryNodeFound = false; // cardinality 1

                WideControlEntryDefinition wved = new WideControlEntryDefinition();

                foreach (XmlNode n in wideControlEntryNode.ChildNodes)
                {
                    if (MatchNodeName(n, XmlTags.EntrySelectedByNode))
                    {
                        if (appliesToNodeFound)
                        {
                            this.ProcessDuplicateNode(n);
                            return null; // fatal
                        }

                        appliesToNodeFound = true;
                        wved.appliesTo = LoadAppliesToSection(n, true);
                    }
                    else if (MatchNodeName(n, XmlTags.WideItemNode))
                    {
                        if (propertyEntryNodeFound)
                        {
                            this.ProcessDuplicateNode(n);
                            return null; // fatal
                        }

                        propertyEntryNodeFound = true;
                        wved.formatTokenList = LoadPropertyEntry(n);
                        if (wved.formatTokenList == null)
                        {
                            // Error at XPath {0} in file {1}: Invalid {2}.
                            this.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.InvalidNode, ComputeCurrentXPath(), FilePath, XmlTags.WideItemNode));
                            return null; // fatal
                        }
                    }
                    else
                    {
                        ProcessUnknownNode(n);
                    }
                }

                if (wved.formatTokenList.Count == 0)
                {
                    // Error at XPath {0} in file {1}: Missing WideItem.
                    this.ReportMissingNode(XmlTags.WideItemNode);
                    return null; // fatal error
                }

                return wved;
            }
        }

        private List<FormatToken> LoadPropertyEntry(XmlNode propertyEntryNode)
        {
            using (this.StackFrame(propertyEntryNode))
            {
                // process Mshexpression, format string and text token
                ViewEntryNodeMatch match = new ViewEntryNodeMatch(this);
                List<XmlNode> unprocessedNodes = new List<XmlNode>();
                if (!match.ProcessExpressionDirectives(propertyEntryNode, unprocessedNodes))
                {
                    return null; // fatal error
                }

                // process the remaining nodes

                foreach (XmlNode n in unprocessedNodes)
                {
                    this.ProcessUnknownNode(n);
                }

                // finally build the item to return
                List<FormatToken> formatTokenList = new List<FormatToken>();

                // add either the text token or the PSPropertyExpression with optional format string
                if (match.TextToken != null)
                {
                    formatTokenList.Add(match.TextToken);
                }
                else
                {
                    FieldPropertyToken fpt = new FieldPropertyToken();
                    fpt.expression = match.Expression;
                    fpt.fieldFormattingDirective.formatString = match.FormatString;
                    formatTokenList.Add(fpt);
                }

                return formatTokenList;
            }
        }
    }
}
