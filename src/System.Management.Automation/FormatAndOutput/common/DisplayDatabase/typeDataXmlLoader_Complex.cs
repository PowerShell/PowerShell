// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
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
        private ComplexControlBody LoadComplexControl(XmlNode controlNode)
        {
            using (this.StackFrame(controlNode))
            {
                ComplexControlBody complexBody = new ComplexControlBody();

                bool entriesFound = false;

                foreach (XmlNode n in controlNode.ChildNodes)
                {
                    if (MatchNodeName(n, XmlTags.ComplexEntriesNode))
                    {
                        if (entriesFound)
                        {
                            this.ProcessDuplicateNode(n);
                            continue;
                        }

                        entriesFound = true;

                        // now read the columns section
                        LoadComplexControlEntries(n, complexBody);
                        if (complexBody.defaultEntry == null)
                        {
                            return null; // fatal error
                        }
                    }
                    else
                    {
                        this.ProcessUnknownNode(n);
                    }
                }

                if (!entriesFound)
                {
                    this.ReportMissingNode(XmlTags.ComplexEntriesNode);
                    return null; // fatal error
                }

                return complexBody;
            }
        }

        private void LoadComplexControlEntries(XmlNode complexControlEntriesNode, ComplexControlBody complexBody)
        {
            using (this.StackFrame(complexControlEntriesNode))
            {
                int entryIndex = 0;

                foreach (XmlNode n in complexControlEntriesNode.ChildNodes)
                {
                    if (MatchNodeName(n, XmlTags.ComplexEntryNode))
                    {
                        ComplexControlEntryDefinition cced = LoadComplexControlEntryDefinition(n, entryIndex++);
                        if (cced == null)
                        {
                            // Error at XPath {0} in file {1}: {2} failed to load.
                            this.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.LoadTagFailed, ComputeCurrentXPath(), FilePath, XmlTags.ComplexEntryNode));
                            complexBody.defaultEntry = null;
                            return; // fatal error
                        }
                        // determine if we have a default entry and if it's already set
                        if (cced.appliesTo == null)
                        {
                            if (complexBody.defaultEntry == null)
                            {
                                complexBody.defaultEntry = cced;
                            }
                            else
                            {
                                // Error at XPath {0} in file {1}: There cannot be more than one default {2}.
                                this.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.TooManyDefaultShapeEntry, ComputeCurrentXPath(), FilePath, XmlTags.ComplexEntryNode));
                                complexBody.defaultEntry = null;
                                return; // fatal error
                            }
                        }
                        else
                        {
                            complexBody.optionalEntryList.Add(cced);
                        }
                    }
                    else
                    {
                        this.ProcessUnknownNode(n);
                    }
                }

                if (complexBody.defaultEntry == null)
                {
                    // Error at XPath {0} in file {1}: There must be at least one default {2}.
                    this.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.NoDefaultShapeEntry, ComputeCurrentXPath(), FilePath, XmlTags.ComplexEntryNode));
                }
            }
        }

        private ComplexControlEntryDefinition LoadComplexControlEntryDefinition(XmlNode complexControlEntryNode, int index)
        {
            using (this.StackFrame(complexControlEntryNode, index))
            {
                bool appliesToNodeFound = false;    // cardinality 0..1
                bool bodyNodeFound = false;         // cardinality 1

                ComplexControlEntryDefinition cced = new ComplexControlEntryDefinition();

                foreach (XmlNode n in complexControlEntryNode.ChildNodes)
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
                        cced.appliesTo = LoadAppliesToSection(n, true);
                    }
                    else if (MatchNodeName(n, XmlTags.ComplexItemNode))
                    {
                        if (bodyNodeFound)
                        {
                            this.ProcessDuplicateNode(n);
                            return null; // fatal
                        }

                        bodyNodeFound = true;
                        cced.itemDefinition.formatTokenList = LoadComplexControlTokenListDefinitions(n);
                    }
                    else
                    {
                        this.ProcessUnknownNode(n);
                    }
                }

                if (cced.itemDefinition.formatTokenList == null)
                {
                    // MissingNode=Error at XPath {0} in file {1}: Missing Node {2}.
                    this.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.MissingNode, ComputeCurrentXPath(), FilePath, XmlTags.ComplexItemNode));
                    return null;
                }

                return cced;
            }
        }

        private List<FormatToken> LoadComplexControlTokenListDefinitions(XmlNode bodyNode)
        {
            using (this.StackFrame(bodyNode))
            {
                List<FormatToken> formatTokenList = new List<FormatToken>();

                int compoundPropertyIndex = 0;
                int newLineIndex = 0;
                int textIndex = 0;
                int frameIndex = 0;

                foreach (XmlNode n in bodyNode.ChildNodes)
                {
                    if (MatchNodeName(n, XmlTags.ExpressionBindingNode))
                    {
                        CompoundPropertyToken cpt = LoadCompoundProperty(n, compoundPropertyIndex++);

                        if (cpt == null)
                        {
                            // Error at XPath {0} in file {1}: {2} failed to load.
                            this.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.LoadTagFailed, ComputeCurrentXPath(), FilePath, XmlTags.ExpressionBindingNode));
                            return null;
                        }

                        formatTokenList.Add(cpt);
                    }
                    else if (MatchNodeName(n, XmlTags.NewLineNode))
                    {
                        NewLineToken nlt = LoadNewLine(n, newLineIndex++);

                        if (nlt == null)
                        {
                            // Error at XPath {0} in file {1}: {2} failed to load.
                            this.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.LoadTagFailed, ComputeCurrentXPath(), FilePath, XmlTags.NewLineNode));
                            return null;
                        }

                        formatTokenList.Add(nlt);
                    }
                    else if (MatchNodeNameWithAttributes(n, XmlTags.TextNode))
                    {
                        TextToken tt = LoadText(n, textIndex++);

                        if (tt == null)
                        {
                            // Error at XPath {0} in file {1}: {2} failed to load.
                            this.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.LoadTagFailed, ComputeCurrentXPath(), FilePath, XmlTags.TextNode));
                            return null;
                        }

                        formatTokenList.Add(tt);
                    }
                    else if (MatchNodeName(n, XmlTags.FrameNode))
                    {
                        FrameToken frame = LoadFrameDefinition(n, frameIndex++);

                        if (frame == null)
                        {
                            // Error at XPath {0} in file {1}: {2} failed to load.
                            this.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.LoadTagFailed, ComputeCurrentXPath(), FilePath, XmlTags.FrameNode));
                            return null;
                        }

                        formatTokenList.Add(frame);
                    }
                    else
                    {
                        this.ProcessUnknownNode(n);
                    }
                }

                if (formatTokenList.Count == 0)
                {
                    // Error at XPath {0} in file {1}: Empty custom control token list.
                    this.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.EmptyCustomControlList, ComputeCurrentXPath(), FilePath));
                    return null;
                }

                return formatTokenList;
            }
        }

        private bool LoadPropertyBaseHelper(XmlNode propertyBaseNode, PropertyTokenBase ptb, List<XmlNode> unprocessedNodes)
        {
            ExpressionNodeMatch expressionMatch = new ExpressionNodeMatch(this);

            bool expressionNodeFound = false;     // cardinality 0..1
            bool collectionNodeFound = false;       // cardinality 0..1
            bool itemSelectionConditionNodeFound = false; // cardinality 0..1

            ExpressionToken condition = null;

            foreach (XmlNode n in propertyBaseNode.ChildNodes)
            {
                if (expressionMatch.MatchNode(n))
                {
                    if (expressionNodeFound)
                    {
                        this.ProcessDuplicateNode(n);
                        return false; // fatal error
                    }

                    expressionNodeFound = true;
                    if (!expressionMatch.ProcessNode(n))
                        return false; // fatal error
                }
                else if (MatchNodeName(n, XmlTags.EnumerateCollectionNode))
                {
                    if (collectionNodeFound)
                    {
                        this.ProcessDuplicateNode(n);
                        return false;
                    }

                    collectionNodeFound = true;
                    if (!ReadBooleanNode(n, out ptb.enumerateCollection))
                        return false;
                }
                else if (MatchNodeName(n, XmlTags.ItemSelectionConditionNode))
                {
                    if (itemSelectionConditionNodeFound)
                    {
                        this.ProcessDuplicateNode(n);
                        return false;
                    }

                    itemSelectionConditionNodeFound = true;
                    condition = LoadItemSelectionCondition(n);
                    if (condition == null)
                    {
                        return false;
                    }
                }
                else
                {
                    if (!IsFilteredOutNode(n))
                        unprocessedNodes.Add(n);
                }
            }

            if (expressionNodeFound)
            {
                // we add only if we encountered one, since it's not mandatory
                ExpressionToken expression = expressionMatch.GenerateExpressionToken();
                if (expression == null)
                {
                    return false; // fatal error
                }

                ptb.expression = expression;
                ptb.conditionToken = condition;
            }

            return true;
        }
        // No used currently
#if false
        private FieldPropertyToken LoadFieldProperty (XmlNode fieldPropertyNode, int index)
        {
            using (this.StackFrame (fieldPropertyNode, index))
            {
                FieldPropertyToken fpt = new FieldPropertyToken ();
                List<XmlNode> unprocessedNodes = new List<XmlNode> ();
                bool success = LoadPropertyBaseHelper (fieldPropertyNode, fpt, unprocessedNodes);

                foreach (XmlNode n in unprocessedNodes)
                {
                    this.ProcessUnknownNode (n);
                }

                if (success && unprocessedNodes.Count == 0)
                {
                    return fpt; // success
                }

                return null; // failure
            }
        }
#endif

        private CompoundPropertyToken LoadCompoundProperty(XmlNode compoundPropertyNode, int index)
        {
            using (this.StackFrame(compoundPropertyNode, index))
            {
                CompoundPropertyToken cpt = new CompoundPropertyToken();
                List<XmlNode> unprocessedNodes = new List<XmlNode>();
                bool success = LoadPropertyBaseHelper(compoundPropertyNode, cpt, unprocessedNodes);

                if (!success)
                {
                    return null;
                }

                cpt.control = null;

                // mutually exclusive
                bool complexControlFound = false;  // cardinality 0..1
                bool fieldControlFound = false;  // cardinality 0..1

                ComplexControlMatch controlMatch = new ComplexControlMatch(this);
                FieldControlBody fieldControlBody = null;

                foreach (XmlNode n in unprocessedNodes)
                {
                    if (controlMatch.MatchNode(n))
                    {
                        if (complexControlFound)
                        {
                            this.ProcessDuplicateAlternateNode(n, XmlTags.ComplexControlNode, XmlTags.ComplexControlNameNode);
                            return null;
                        }

                        complexControlFound = true;
                        if (!controlMatch.ProcessNode(n))
                            return null; // fatal error
                    }
                    else if (MatchNodeName(n, XmlTags.FieldControlNode))
                    {
                        if (fieldControlFound)
                        {
                            this.ProcessDuplicateAlternateNode(n, XmlTags.ComplexControlNode, XmlTags.ComplexControlNameNode);
                            return null; // fatal error
                        }

                        fieldControlFound = true;
                        fieldControlBody = new FieldControlBody();
                        fieldControlBody.fieldFormattingDirective.formatString = GetMandatoryInnerText(n);
                        if (fieldControlBody.fieldFormattingDirective.formatString == null)
                        {
                            return null; // fatal error
                        }
                    }
                    else
                    {
                        ProcessUnknownNode(n);
                    }
                }

                if (fieldControlFound && complexControlFound)
                {
                    this.ProcessDuplicateAlternateNode(XmlTags.ComplexControlNode, XmlTags.ComplexControlNameNode);
                    return null; // fatal error
                }

                if (fieldControlFound)
                {
                    cpt.control = fieldControlBody;
                }
                else
                {
                    cpt.control = controlMatch.Control;
                }
                /*
                if (cpt.control == null)
                {
                    this.ReportMissingNodes (
                            new string[] { XmlTags.FieldControlNode, XmlTags.ComplexControlNode, XmlTags.ComplexControlNameNode });
                    return null;
                }
                */
                return cpt;
            }
        }

        private NewLineToken LoadNewLine(XmlNode newLineNode, int index)
        {
            using (this.StackFrame(newLineNode, index))
            {
                if (!VerifyNodeHasNoChildren(newLineNode))
                {
                    return null;
                }

                NewLineToken nlt = new NewLineToken();

                return nlt;
            }
        }

        private TextToken LoadText(XmlNode textNode, int index)
        {
            using (this.StackFrame(textNode, index))
            {
                return LoadTextToken(textNode);
            }
        }

        internal TextToken LoadText(XmlNode textNode)
        {
            using (this.StackFrame(textNode))
            {
                return LoadTextToken(textNode);
            }
        }

        private int LoadIntegerValue(XmlNode node, out bool success)
        {
            using (this.StackFrame(node))
            {
                success = false;
                int retVal = 0;
                if (!VerifyNodeHasNoChildren(node))
                {
                    return retVal;
                }

                string val = this.GetMandatoryInnerText(node);

                if (val == null)
                {
                    // Error at XPath {0} in file {1}: Missing inner text value.
                    this.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.MissingInnerText, ComputeCurrentXPath(), FilePath));
                    return retVal;
                }

                if (!int.TryParse(val, out retVal))
                {
                    // Error at XPath {0} in file {1}: An integer is expected.
                    this.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.ExpectInteger, ComputeCurrentXPath(), FilePath));
                    return retVal;
                }

                success = true;
                return retVal;
            }
        }

        private int LoadPositiveOrZeroIntegerValue(XmlNode node, out bool success)
        {
            int val = LoadIntegerValue(node, out success);
            if (!success)
                return val;
            using (this.StackFrame(node))
            {
                if (val < 0)
                {
                    // Error at XPath {0} in file {1}: A non-negative integer is expected.
                    this.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.ExpectNaturalNumber, ComputeCurrentXPath(), FilePath));
                    success = false;
                }

                return val;
            }
        }

        private FrameToken LoadFrameDefinition(XmlNode frameNode, int index)
        {
            using (this.StackFrame(frameNode, index))
            {
                bool itemNodeFound = false; // cardinality 1
                bool leftIndentFound = false;   // cardinality 0..1
                bool rightIndentFound = false;  // cardinality 0..1

                // mutually exclusive
                bool firstLineIndentFound = false;  // cardinality 0..1
                bool firstLineHangingFound = false;  // cardinality 0..1

                bool success; // scratch variable

                FrameToken frame = new FrameToken();
                foreach (XmlNode n in frameNode.ChildNodes)
                {
                    if (MatchNodeName(n, XmlTags.LeftIndentNode))
                    {
                        if (leftIndentFound)
                        {
                            this.ProcessDuplicateNode(n);
                            return null; // fatal
                        }

                        leftIndentFound = true;

                        // optional section
                        frame.frameInfoDefinition.leftIndentation = LoadPositiveOrZeroIntegerValue(n, out success);
                        if (!success)
                            return null;
                    }
                    else if (MatchNodeName(n, XmlTags.RightIndentNode))
                    {
                        if (rightIndentFound)
                        {
                            this.ProcessDuplicateNode(n);
                            return null; // fatal
                        }

                        rightIndentFound = true;

                        // optional section
                        frame.frameInfoDefinition.rightIndentation = LoadPositiveOrZeroIntegerValue(n, out success);
                        if (!success)
                            return null;
                    }
                    else if (MatchNodeName(n, XmlTags.FirstLineIndentNode))
                    {
                        if (firstLineIndentFound)
                        {
                            this.ProcessDuplicateAlternateNode(n, XmlTags.FirstLineIndentNode, XmlTags.FirstLineHangingNode);
                            return null;
                        }

                        firstLineIndentFound = true;

                        frame.frameInfoDefinition.firstLine = LoadPositiveOrZeroIntegerValue(n, out success);
                        if (!success)
                            return null;
                    }
                    else if (MatchNodeName(n, XmlTags.FirstLineHangingNode))
                    {
                        if (firstLineHangingFound)
                        {
                            this.ProcessDuplicateAlternateNode(n, XmlTags.FirstLineIndentNode, XmlTags.FirstLineHangingNode);
                            return null;
                        }

                        firstLineHangingFound = true;

                        frame.frameInfoDefinition.firstLine = LoadPositiveOrZeroIntegerValue(n, out success);
                        if (!success)
                            return null;
                        // hanging is codified as negative
                        frame.frameInfoDefinition.firstLine = -frame.frameInfoDefinition.firstLine;
                    }
                    else if (MatchNodeName(n, XmlTags.ComplexItemNode))
                    {
                        if (itemNodeFound)
                        {
                            this.ProcessDuplicateNode(n);
                            return null; // fatal
                        }

                        itemNodeFound = true;
                        frame.itemDefinition.formatTokenList = LoadComplexControlTokenListDefinitions(n);
                    }
                    else
                    {
                        this.ProcessUnknownNode(n);
                    }
                }

                if (firstLineHangingFound && firstLineIndentFound)
                {
                    this.ProcessDuplicateAlternateNode(XmlTags.FirstLineIndentNode, XmlTags.FirstLineHangingNode);
                    return null; // fatal error
                }

                if (frame.itemDefinition.formatTokenList == null)
                {
                    // MissingNode=Error at XPath {0} in file {1}: Missing Node {2}.
                    this.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.MissingNode, ComputeCurrentXPath(), FilePath, XmlTags.ComplexItemNode));
                    return null;
                }

                return frame;
            }
        }

        private bool ReadBooleanNode(XmlNode collectionElement, out bool val)
        {
            val = false;

            if (!VerifyNodeHasNoChildren(collectionElement))
            {
                return false;
            }

            string s = collectionElement.InnerText;

            if (string.IsNullOrEmpty(s))
            {
                val = true;
                return true;
            }

            if (string.Equals(s, XMLStringValues.False, StringComparison.OrdinalIgnoreCase))
            {
                val = false;
                return true;
            }
            else if (string.Equals(s, XMLStringValues.True, StringComparison.OrdinalIgnoreCase))
            {
                val = true;
                return true;
            }
            // Error at XPath {0} in file {1}: A Boolean value is expected.
            this.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.ExpectBoolean, ComputeCurrentXPath(), FilePath));

            return false;
        }
    }
}
