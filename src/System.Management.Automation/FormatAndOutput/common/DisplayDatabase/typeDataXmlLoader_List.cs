// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
        private ListControlBody LoadListControl(XmlNode controlNode)
        {
            using (this.StackFrame(controlNode))
            {
                ListControlBody listBody = new ListControlBody();

                bool listViewEntriesFound = false;

                foreach (XmlNode n in controlNode.ChildNodes)
                {
                    if (MatchNodeName(n, XmlTags.ListEntriesNode))
                    {
                        if (listViewEntriesFound)
                        {
                            this.ProcessDuplicateNode(n);
                            continue;
                        }

                        listViewEntriesFound = true;

                        // now read the columns section
                        LoadListControlEntries(n, listBody);
                        if (listBody.defaultEntryDefinition == null)
                        {
                            return null; // fatal error
                        }
                    }
                    else
                    {
                        this.ProcessUnknownNode(n);
                    }
                }

                if (!listViewEntriesFound)
                {
                    this.ReportMissingNode(XmlTags.ListEntriesNode);
                    return null; // fatal error
                }

                return listBody;
            }
        }

        private void LoadListControlEntries(XmlNode listViewEntriesNode, ListControlBody listBody)
        {
            using (this.StackFrame(listViewEntriesNode))
            {
                int entryIndex = 0;

                foreach (XmlNode n in listViewEntriesNode.ChildNodes)
                {
                    if (MatchNodeName(n, XmlTags.ListEntryNode))
                    {
                        ListControlEntryDefinition lved = LoadListControlEntryDefinition(n, entryIndex++);
                        if (lved == null)
                        {
                            // Error at XPath {0} in file {1}: {2} failed to load.
                            this.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.LoadTagFailed, ComputeCurrentXPath(), FilePath, XmlTags.ListEntryNode));
                            listBody.defaultEntryDefinition = null;
                            return; // fatal error
                        }
                        // determine if we have a default entry and if it's already set
                        if (lved.appliesTo == null)
                        {
                            if (listBody.defaultEntryDefinition == null)
                            {
                                listBody.defaultEntryDefinition = lved;
                            }
                            else
                            {
                                // Error at XPath {0} in file {1}: There cannot be more than one default {2}.
                                this.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.TooManyDefaultShapeEntry, ComputeCurrentXPath(), FilePath, XmlTags.ListEntryNode));
                                listBody.defaultEntryDefinition = null;
                                return; // fatal error
                            }
                        }
                        else
                        {
                            listBody.optionalEntryList.Add(lved);
                        }
                    }
                    else
                    {
                        this.ProcessUnknownNode(n);
                    }
                }

                if (listBody.optionalEntryList == null)
                {
                    // Error at XPath {0} in file {1}: There must be at least one default {2}.
                    this.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.NoDefaultShapeEntry, ComputeCurrentXPath(), FilePath, XmlTags.ListEntryNode));
                }
            }
        }

        private ListControlEntryDefinition LoadListControlEntryDefinition(XmlNode listViewEntryNode, int index)
        {
            using (this.StackFrame(listViewEntryNode, index))
            {
                bool appliesToNodeFound = false;    // cardinality 0..1
                bool bodyNodeFound = false;         // cardinality 1

                ListControlEntryDefinition lved = new ListControlEntryDefinition();

                foreach (XmlNode n in listViewEntryNode.ChildNodes)
                {
                    if (MatchNodeName(n, XmlTags.EntrySelectedByNode))
                    {
                        if (appliesToNodeFound)
                        {
                            this.ProcessDuplicateNode(n);
                            return null; // fatal
                        }

                        appliesToNodeFound = true;

                        // optional section
                        lved.appliesTo = LoadAppliesToSection(n, true);
                    }
                    else if (MatchNodeName(n, XmlTags.ListItemsNode))
                    {
                        if (bodyNodeFound)
                        {
                            this.ProcessDuplicateNode(n);
                            return null; // fatal
                        }

                        bodyNodeFound = true;
                        LoadListControlItemDefinitions(lved, n);
                    }
                    else
                    {
                        this.ProcessUnknownNode(n);
                    }
                }

                if (lved.itemDefinitionList == null)
                {
                    // Error at XPath {0} in file {1}: Missing definition list.
                    this.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.NoDefinitionList, ComputeCurrentXPath(), FilePath));
                    return null;
                }

                return lved;
            }
        }

        private void LoadListControlItemDefinitions(ListControlEntryDefinition lved, XmlNode bodyNode)
        {
            using (this.StackFrame(bodyNode))
            {
                int index = 0;

                foreach (XmlNode n in bodyNode.ChildNodes)
                {
                    if (MatchNodeName(n, XmlTags.ListItemNode))
                    {
                        index++;
                        ListControlItemDefinition lvid = LoadListControlItemDefinition(n);
                        if (lvid == null)
                        {
                            // Error at XPath {0} in file {1}: Invalid property entry.
                            this.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.InvalidPropertyEntry, ComputeCurrentXPath(), FilePath));
                            lved.itemDefinitionList = null;
                            return; // fatal
                        }

                        lved.itemDefinitionList.Add(lvid);
                    }
                    else
                    {
                        this.ProcessUnknownNode(n);
                    }
                }

                // we must have at least a definition in th elist
                if (lved.itemDefinitionList.Count == 0)
                {
                    // Error at XPath {0} in file {1}: At least one list view item must be specified.
                    this.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.NoListViewItem, ComputeCurrentXPath(), FilePath));
                    lved.itemDefinitionList = null;
                    return; // fatal
                }
            }
        }

        private ListControlItemDefinition LoadListControlItemDefinition(XmlNode propertyEntryNode)
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
                TextToken labelToken = null;
                ExpressionToken condition = null;
                bool labelNodeFound = false; // cardinality 0..1
                bool itemSelectionConditionNodeFound = false; // cardinality 0..1

                foreach (XmlNode n in unprocessedNodes)
                {
                    if (MatchNodeName(n, XmlTags.ItemSelectionConditionNode))
                    {
                        if (itemSelectionConditionNodeFound)
                        {
                            this.ProcessDuplicateNode(n);
                            return null; // fatal error
                        }

                        itemSelectionConditionNodeFound = true;
                        condition = LoadItemSelectionCondition(n);
                        if (condition == null)
                        {
                            return null; // fatal error
                        }
                    }
                    else if (MatchNodeNameWithAttributes(n, XmlTags.LabelNode))
                    {
                        if (labelNodeFound)
                        {
                            this.ProcessDuplicateNode(n);
                            return null; // fatal error
                        }

                        labelNodeFound = true;
                        labelToken = LoadLabel(n);
                        if (labelToken == null)
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
                ListControlItemDefinition lvid = new ListControlItemDefinition();

                // add the label
                lvid.label = labelToken;

                // add condition
                lvid.conditionToken = condition;

                // add either the text token or the PSPropertyExpression with optional format string
                if (match.TextToken != null)
                {
                    lvid.formatTokenList.Add(match.TextToken);
                }
                else
                {
                    FieldPropertyToken fpt = new FieldPropertyToken();
                    fpt.expression = match.Expression;
                    fpt.fieldFormattingDirective.formatString = match.FormatString;
                    lvid.formatTokenList.Add(fpt);
                }

                return lvid;
            }
        }

        private ExpressionToken LoadItemSelectionCondition(XmlNode itemNode)
        {
            using (this.StackFrame(itemNode))
            {
                bool expressionNodeFound = false;     // cardinality 1

                ExpressionNodeMatch expressionMatch = new ExpressionNodeMatch(this);
                foreach (XmlNode n in itemNode.ChildNodes)
                {
                    if (expressionMatch.MatchNode(n))
                    {
                        if (expressionNodeFound)
                        {
                            this.ProcessDuplicateNode(n);
                            return null; // fatal error
                        }

                        expressionNodeFound = true;
                        if (!expressionMatch.ProcessNode(n))
                            return null;
                    }
                    else
                    {
                        this.ProcessUnknownNode(n);
                    }
                }

                return expressionMatch.GenerateExpressionToken();
            }
        }
    }
}
