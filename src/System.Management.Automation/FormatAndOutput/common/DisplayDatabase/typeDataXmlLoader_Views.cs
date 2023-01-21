// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation;
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
        private void LoadViewDefinitions(TypeInfoDataBase db, XmlNode viewDefinitionsNode)
        {
            using (this.StackFrame(viewDefinitionsNode))
            {
                int index = 0;
                foreach (XmlNode n in viewDefinitionsNode.ChildNodes)
                {
                    if (MatchNodeName(n, XmlTags.ViewNode))
                    {
                        ViewDefinition view = LoadView(n, index++);
                        if (view != null)
                        {
                            ReportTrace(string.Create(CultureInfo.InvariantCulture, $"{ControlBase.GetControlShapeName(view.mainControl)} view {view.name} is loaded from file {view.loadingInfo.filePath}"));
                            // we are fine, add the view to the list
                            db.viewDefinitionsSection.viewDefinitionList.Add(view);
                        }
                    }
                    else
                    {
                        ProcessUnknownNode(n);
                    }
                }
            }
        }

        private ViewDefinition LoadView(XmlNode viewNode, int index)
        {
            using (this.StackFrame(viewNode, index))
            {
                // load the common data
                ViewDefinition view = new ViewDefinition();
                List<XmlNode> unprocessedNodes = new List<XmlNode>();
                bool success = LoadCommonViewData(viewNode, view, unprocessedNodes);

                if (!success)
                {
                    // Error at XPath {0} in file {1}: View cannot be loaded.
                    this.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.ViewNotLoaded, ComputeCurrentXPath(), FilePath));
                    return null; // fatal error
                }

                // add the main control constituting the view
                // only one control can exist, and it can be
                // of the various types: Table, List, etc.

                string[] controlNodeTags = new string[]
                {
                    XmlTags.TableControlNode,
                    XmlTags.ListControlNode,
                    XmlTags.WideControlNode,
                    XmlTags.ComplexControlNode
                };

                List<XmlNode> secondPassUnprocessedNodes = new List<XmlNode>();

                bool mainControlFound = false; // cardinality 1
                foreach (XmlNode n in unprocessedNodes)
                {
                    if (MatchNodeName(n, XmlTags.TableControlNode))
                    {
                        if (mainControlFound)
                        {
                            ProcessDuplicateNode(n);
                            return null;
                        }

                        mainControlFound = true;
                        view.mainControl = LoadTableControl(n);
                    }
                    else if (MatchNodeName(n, XmlTags.ListControlNode))
                    {
                        if (mainControlFound)
                        {
                            ProcessDuplicateNode(n);
                            return null;
                        }

                        mainControlFound = true;
                        view.mainControl = LoadListControl(n);
                    }
                    else if (MatchNodeName(n, XmlTags.WideControlNode))
                    {
                        if (mainControlFound)
                        {
                            ProcessDuplicateNode(n);
                            return null;
                        }

                        mainControlFound = true;
                        view.mainControl = LoadWideControl(n);
                    }
                    else if (MatchNodeName(n, XmlTags.ComplexControlNode))
                    {
                        if (mainControlFound)
                        {
                            ProcessDuplicateNode(n);
                            return null;
                        }

                        mainControlFound = true;
                        view.mainControl = LoadComplexControl(n);
                    }
                    else
                    {
                        secondPassUnprocessedNodes.Add(n);
                    }
                }

                if (view.mainControl == null)
                {
                    this.ReportMissingNodes(controlNodeTags);
                    return null; // fatal
                }

                if (!LoadMainControlDependentData(secondPassUnprocessedNodes, view))
                {
                    return null; // fatal
                }

                if (view.outOfBand && (view.groupBy != null))
                {
                    // we cannot have grouping and out of band at the same time
                    // Error at XPath {0} in file {1}: An Out Of Band view cannot have GroupBy.
                    this.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.OutOfBandGroupByConflict, ComputeCurrentXPath(), FilePath));
                    return null; // fatal
                }

                return view;
            }
        }

        private bool LoadMainControlDependentData(List<XmlNode> unprocessedNodes, ViewDefinition view)
        {
            foreach (XmlNode n in unprocessedNodes)
            {
                bool outOfBandNodeFound = false; // cardinality 0..1
                bool controlDefinitionsFound = false; // cardinality 0..1

                if (MatchNodeName(n, XmlTags.OutOfBandNode))
                {
                    if (outOfBandNodeFound)
                    {
                        this.ProcessDuplicateNode(n);
                        return false;
                    }

                    outOfBandNodeFound = true;
                    if (!this.ReadBooleanNode(n, out view.outOfBand))
                    {
                        return false;
                    }

                    if (view.mainControl is not ComplexControlBody && view.mainControl is not ListControlBody)
                    {
                        // Error at XPath {0} in file {1}: Out Of Band views can only have CustomControl or ListControl.
                        ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.InvalidControlForOutOfBandView, ComputeCurrentXPath(), FilePath));
                        return false;
                    }
                }
                else if (MatchNodeName(n, XmlTags.ControlsNode))
                {
                    if (controlDefinitionsFound)
                    {
                        this.ProcessDuplicateNode(n);
                        return false;
                    }

                    controlDefinitionsFound = true;
                    LoadControlDefinitions(n, view.formatControlDefinitionHolder.controlDefinitionList);
                }
                else
                {
                    ProcessUnknownNode(n);
                }
            }

            return true;
        }

        private bool LoadCommonViewData(XmlNode viewNode, ViewDefinition view, List<XmlNode> unprocessedNodes)
        {
            if (viewNode == null)
                throw PSTraceSource.NewArgumentNullException(nameof(viewNode));

            if (view == null)
                throw PSTraceSource.NewArgumentNullException(nameof(view));

            // set loading information
            view.loadingInfo = this.LoadingInfo;
            view.loadingInfo.xPath = this.ComputeCurrentXPath();

            // start the loading process
            bool nameNodeFound = false;             // cardinality 1
            bool appliesToNodeFound = false;        // cardinality 1
            bool groupByFound = false;              // cardinality 0..1

            foreach (XmlNode n in viewNode.ChildNodes)
            {
                if (MatchNodeName(n, XmlTags.NameNode))
                {
                    if (nameNodeFound)
                    {
                        this.ProcessDuplicateNode(n);
                        return false;
                    }

                    nameNodeFound = true;
                    view.name = GetMandatoryInnerText(n);
                    if (view.name == null)
                    {
                        return false;
                    }
                }
                else if (MatchNodeName(n, XmlTags.ViewSelectedByNode))
                {
                    if (appliesToNodeFound)
                    {
                        this.ProcessDuplicateNode(n);
                        return false;
                    }

                    appliesToNodeFound = true;

                    // if null, we invalidate the view
                    view.appliesTo = LoadAppliesToSection(n, false);
                    if (view.appliesTo == null)
                    {
                        return false;
                    }
                }
                else if (MatchNodeName(n, XmlTags.GroupByNode))
                {
                    if (groupByFound)
                    {
                        this.ProcessDuplicateNode(n);
                        return false;
                    }

                    groupByFound = true;
                    view.groupBy = LoadGroupBySection(n);
                    if (view.groupBy == null)
                    {
                        return false;
                    }
                }
                else
                {
                    // save for further processing
                    unprocessedNodes.Add(n);
                }
            }

            if (!nameNodeFound)
            {
                this.ReportMissingNode(XmlTags.NameNode);
                return false;
            }

            if (!appliesToNodeFound)
            {
                this.ReportMissingNode(XmlTags.ViewSelectedByNode);
                return false;
            }

            return true;
        }

        private void LoadControlDefinitions(XmlNode definitionsNode, List<ControlDefinition> controlDefinitionList)
        {
            using (this.StackFrame(definitionsNode))
            {
                int controlDefinitionIndex = 0;
                foreach (XmlNode n in definitionsNode.ChildNodes)
                {
                    if (MatchNodeName(n, XmlTags.ControlNode))
                    {
                        ControlDefinition def = LoadControlDefinition(n, controlDefinitionIndex++);
                        if (def != null)
                        {
                            controlDefinitionList.Add(def);
                        }
                    }
                    else
                    {
                        ProcessUnknownNode(n);
                    }
                }
            }
        }

        private ControlDefinition LoadControlDefinition(XmlNode controlDefinitionNode, int index)
        {
            using (this.StackFrame(controlDefinitionNode, index))
            {
                bool nameNodeFound = false;         // cardinality 1
                bool controlNodeFound = false;         // cardinality 1

                ControlDefinition def = new ControlDefinition();

                foreach (XmlNode n in controlDefinitionNode.ChildNodes)
                {
                    if (MatchNodeName(n, XmlTags.NameNode))
                    {
                        if (nameNodeFound)
                        {
                            this.ProcessDuplicateNode(n);
                            continue;
                        }

                        nameNodeFound = true;
                        def.name = GetMandatoryInnerText(n);
                        if (def.name == null)
                        {
                            // Error at XPath {0} in file {1}: Control cannot have a null Name.
                            this.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.NullControlName, ComputeCurrentXPath(), FilePath));
                            return null; // fatal error
                        }
                    }
                    else if (MatchNodeName(n, XmlTags.ComplexControlNode))
                    {
                        // NOTE: for the time being we allow only complex control definitions to be loaded
                        if (controlNodeFound)
                        {
                            this.ProcessDuplicateNode(n);
                            return null; // fatal
                        }

                        controlNodeFound = true;
                        def.controlBody = LoadComplexControl(n);
                        if (def.controlBody == null)
                        {
                            return null; // fatal error
                        }
                    }
                    else
                    {
                        this.ProcessUnknownNode(n);
                    }
                }

                if (def.name == null)
                {
                    this.ReportMissingNode(XmlTags.NameNode);
                    return null; // fatal
                }

                if (def.controlBody == null)
                {
                    this.ReportMissingNode(XmlTags.ComplexControlNode);
                    return null; // fatal
                }

                return def;
            }
        }
    }
}
