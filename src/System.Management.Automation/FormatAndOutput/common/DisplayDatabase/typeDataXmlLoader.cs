// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;

using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Internal;
using System.Management.Automation.Security;
using System.Threading;

namespace Microsoft.PowerShell.Commands.Internal.Format
{
    internal sealed class XmlFileLoadInfo
    {
        internal XmlFileLoadInfo() { }

        internal XmlFileLoadInfo(string dir, string path, ConcurrentBag<string> errors, string psSnapinName)
        {
            fileDirectory = dir;
            filePath = path;
            this.errors = errors;
            this.psSnapinName = psSnapinName;
        }

        internal string fileDirectory = null;
        internal string filePath = null;
        internal ConcurrentBag<string> errors;
        internal string psSnapinName;
    }

    /// <summary>
    /// Class to load the XML document into data structures.
    /// It encapsulates the file format specific code.
    /// </summary>
    internal sealed partial class TypeInfoDataBaseLoader : XmlLoaderBase
    {
        private const string resBaseName = "TypeInfoDataBaseLoaderStrings";

        #region tracer
        [TraceSource("TypeInfoDataBaseLoader", "TypeInfoDataBaseLoader")]
        private static readonly PSTraceSource s_tracer = PSTraceSource.GetTracer("TypeInfoDataBaseLoader", "TypeInfoDataBaseLoader");
        #endregion tracer

        /// <summary>
        /// Table of XML node tags used in the file format.
        /// </summary>
        private static class XmlTags
        {
            // top level entries in the XML document
            internal const string DefaultSettingsNode = "DefaultSettings";
            internal const string ConfigurationNode = "Configuration";
            internal const string SelectionSetsNode = "SelectionSets";
            internal const string ViewDefinitionsNode = "ViewDefinitions";
            internal const string ControlsNode = "Controls";

            // default settings entries
            internal const string MultilineTablesNode = "WrapTables";
            internal const string PropertyCountForTableNode = "PropertyCountForTable";
            internal const string ShowErrorsAsMessagesNode = "ShowError";
            internal const string ShowErrorsInFormattedOutputNode = "DisplayError";
            internal const string EnumerableExpansionsNode = "EnumerableExpansions";
            internal const string EnumerableExpansionNode = "EnumerableExpansion";
            internal const string ExpandNode = "Expand";

            // entries identifying the various control types definitions
            internal const string ControlNode = "Control";
            internal const string ComplexControlNameNode = "CustomControlName";

            // selection sets (a.k.a. Type Groups)
            internal const string SelectionSetNode = "SelectionSet";
            internal const string SelectionSetNameNode = "SelectionSetName";
            internal const string SelectionConditionNode = "SelectionCondition";
            internal const string NameNode = "Name";
            internal const string TypesNode = "Types";
            internal const string TypeNameNode = "TypeName";

            internal const string ViewNode = "View";

            // entries identifying the various control types
            internal const string TableControlNode = "TableControl";
            internal const string ListControlNode = "ListControl";
            internal const string WideControlNode = "WideControl";
            internal const string ComplexControlNode = "CustomControl";
            internal const string FieldControlNode = "FieldControl";

            // view specific tags
            internal const string ViewSelectedByNode = "ViewSelectedBy";
            internal const string GroupByNode = "GroupBy";
            internal const string OutOfBandNode = "OutOfBand";

            // table specific tags
            internal const string HideTableHeadersNode = "HideTableHeaders";
            internal const string TableHeadersNode = "TableHeaders";
            internal const string TableColumnHeaderNode = "TableColumnHeader";

            internal const string TableRowEntriesNode = "TableRowEntries";
            internal const string TableRowEntryNode = "TableRowEntry";
            internal const string MultiLineNode = "Wrap";
            internal const string TableColumnItemsNode = "TableColumnItems";
            internal const string TableColumnItemNode = "TableColumnItem";
            internal const string WidthNode = "Width";

            // list specific tags
            internal const string ListEntriesNode = "ListEntries";
            internal const string ListEntryNode = "ListEntry";
            internal const string ListItemsNode = "ListItems";
            internal const string ListItemNode = "ListItem";

            // wide specific tags
            internal const string ColumnNumberNode = "ColumnNumber";
            internal const string WideEntriesNode = "WideEntries";
            internal const string WideEntryNode = "WideEntry";
            internal const string WideItemNode = "WideItem";

            // complex specific tags
            internal const string ComplexEntriesNode = "CustomEntries";
            internal const string ComplexEntryNode = "CustomEntry";
            internal const string ComplexItemNode = "CustomItem";

            internal const string ExpressionBindingNode = "ExpressionBinding";
            internal const string NewLineNode = "NewLine";
            internal const string TextNode = "Text";
            internal const string FrameNode = "Frame";
            internal const string LeftIndentNode = "LeftIndent";
            internal const string RightIndentNode = "RightIndent";
            internal const string FirstLineIndentNode = "FirstLineIndent";
            internal const string FirstLineHangingNode = "FirstLineHanging";

            internal const string EnumerateCollectionNode = "EnumerateCollection";

            // general purpose tags
            internal const string AutoSizeNode = "AutoSize"; // valid only for table and wide
            internal const string AlignmentNode = "Alignment";
            internal const string PropertyNameNode = "PropertyName";
            internal const string ScriptBlockNode = "ScriptBlock";
            internal const string FormatStringNode = "FormatString";
            internal const string LabelNode = "Label";

            internal const string EntrySelectedByNode = "EntrySelectedBy";
            internal const string ItemSelectionConditionNode = "ItemSelectionCondition";

            // attribute tags for resource strings
            internal const string AssemblyNameAttribute = "AssemblyName";
            internal const string BaseNameAttribute = "BaseName";
            internal const string ResourceIdAttribute = "ResourceId";
        }

        /// <summary>
        /// Table of miscellanea string constant values for XML nodes.
        /// </summary>
        private static class XMLStringValues
        {
            internal const string True = "TRUE";
            internal const string False = "FALSE";

            internal const string AlignmentLeft = "left";
            internal const string AlignmentCenter = "center";
            internal const string AlignmentRight = "right";
        }

        // Flag that determines whether validation should be suppressed while
        // processing pre-validated type / formatting information.
        private bool _suppressValidation = false;

        /// <summary>
        /// Entry point for the loader algorithm.
        /// </summary>
        /// <param name="info">Information needed to load the file.</param>
        /// <param name="db">Database instance to load the file into.</param>
        /// <param name="expressionFactory">Expression factory to validate script blocks.</param>
        /// <param name="authorizationManager">
        /// Authorization manager to perform signature checks before reading ps1xml files (or null of no checks are needed)
        /// </param>
        /// <param name="host">
        /// Host passed to <paramref name="authorizationManager"/>.  Can be null if no interactive questions should be asked.
        /// </param>
        /// <param name="preValidated">
        /// True if the format data has been pre-validated (build time, manual testing, etc) so that validation can be
        /// skipped at runtime.
        /// </param>
        /// <returns>True if successful.</returns>
        internal bool LoadXmlFile(
            XmlFileLoadInfo info,
            TypeInfoDataBase db,
            PSPropertyExpressionFactory expressionFactory,
            AuthorizationManager authorizationManager,
            PSHost host,
            bool preValidated)
        {
            if (info == null)
                throw PSTraceSource.NewArgumentNullException(nameof(info));

            if (info.filePath == null)
                throw PSTraceSource.NewArgumentNullException("info.filePath");

            if (db == null)
                throw PSTraceSource.NewArgumentNullException(nameof(db));

            if (expressionFactory == null)
                throw PSTraceSource.NewArgumentNullException(nameof(expressionFactory));

            if (SecuritySupport.IsProductBinary(info.filePath))
            {
                this.SetLoadingInfoIsProductCode(true);
            }

            this.displayResourceManagerCache = db.displayResourceManagerCache;

            this.expressionFactory = expressionFactory;
            this.SetDatabaseLoadingInfo(info);
            this.ReportTrace("loading file started");

            // load file into XML document
            XmlDocument newDocument = null;
            bool isFullyTrusted = false;

            newDocument = LoadXmlDocumentFromFileLoadingInfo(authorizationManager, host, out isFullyTrusted);

            // If we're not in a locked-down environment, types and formatting are allowed based just on the authorization
            // manager. If we are in a locked-down environment, additionally check the system policy.
            if (SystemPolicy.GetSystemLockdownPolicy() == SystemEnforcementMode.Enforce)
            {
                SetLoadingInfoIsFullyTrusted(isFullyTrusted);
            }

            if (newDocument == null)
            {
                return false;
            }

            // load the XML document into a copy of the
            // in memory database
            bool previousSuppressValidation = _suppressValidation;
            try
            {
                _suppressValidation = preValidated;

                try
                {
                    this.LoadData(newDocument, db);
                }
                catch (TooManyErrorsException)
                {
                    // already logged an error before throwing
                    return false;
                }
                catch (Exception e) // will rethrow
                {
                    // Error in file {0}: {1}

                    this.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.ErrorInFile, FilePath, e.Message));
                    throw;
                }

                if (this.HasErrors)
                {
                    return false;
                }
            }
            finally
            {
                _suppressValidation = previousSuppressValidation;
            }

            this.ReportTrace("file loaded with no errors");
            return true;
        }

        /// <summary>
        /// Entry point for the loader algorithm to load formatting data from ExtendedTypeDefinition.
        /// </summary>
        /// <param name="typeDefinition">The ExtendedTypeDefinition instance to load formatting data from.</param>
        /// <param name="db">Database instance to load the formatting data into.</param>
        /// <param name="expressionFactory">Expression factory to validate the script block.</param>
        /// <param name="isBuiltInFormatData">Do we implicitly trust the script blocks (so they should run in full langauge mode)?</param>
        /// <param name="isForHelp">True when the view is for help output.</param>
        /// <returns></returns>
        internal bool LoadFormattingData(
            ExtendedTypeDefinition typeDefinition,
            TypeInfoDataBase db,
            PSPropertyExpressionFactory expressionFactory,
            bool isBuiltInFormatData,
            bool isForHelp)
        {
            if (typeDefinition == null)
                throw PSTraceSource.NewArgumentNullException(nameof(typeDefinition));
            if (typeDefinition.TypeName == null)
                throw PSTraceSource.NewArgumentNullException("typeDefinition.TypeName");
            if (db == null)
                throw PSTraceSource.NewArgumentNullException(nameof(db));
            if (expressionFactory == null)
                throw PSTraceSource.NewArgumentNullException(nameof(expressionFactory));

            this.expressionFactory = expressionFactory;
            this.ReportTrace("loading ExtendedTypeDefinition started");

            try
            {
                this.SetLoadingInfoIsFullyTrusted(isBuiltInFormatData);
                this.SetLoadingInfoIsProductCode(isBuiltInFormatData);
                this.LoadData(typeDefinition, db, isForHelp);
            }
            catch (TooManyErrorsException)
            {
                // already logged an error before throwing
                return false;
            }
            catch (Exception e) // will rethrow
            {
                // Error in formatting data "{0}": {1}

                this.ReportErrorForLoadingFromObjectModel(
                    StringUtil.Format(FormatAndOutXmlLoadingStrings.ErrorInFormattingData, typeDefinition.TypeName, e.Message), typeDefinition.TypeName);
                throw;
            }

            if (this.HasErrors)
            {
                return false;
            }

            this.ReportTrace("ExtendedTypeDefinition loaded with no errors");
            return true;
        }

        /// <summary>
        /// Load the content of the XML document into the data instance.
        /// It assumes that the XML document has been successfully loaded.
        /// </summary>
        /// <param name="doc">XML document to load from, cannot be null.</param>
        /// <param name="db">Instance of the databaseto load into.</param>
        private void LoadData(XmlDocument doc, TypeInfoDataBase db)
        {
            if (doc == null)
                throw PSTraceSource.NewArgumentNullException(nameof(doc));

            if (db == null)
                throw PSTraceSource.NewArgumentNullException(nameof(db));

            // create a new instance of the database to be loaded
            XmlElement documentElement = doc.DocumentElement;

            bool defaultSettingsNodeFound = false;
            bool typeGroupsFound = false;
            bool viewDefinitionsFound = false;
            bool controlDefinitionsFound = false;

            if (MatchNodeNameWithAttributes(documentElement, XmlTags.ConfigurationNode))
            {
                // load the various sections
                using (this.StackFrame(documentElement))
                {
                    foreach (XmlNode n in documentElement.ChildNodes)
                    {
                        if (MatchNodeName(n, XmlTags.DefaultSettingsNode))
                        {
                            if (defaultSettingsNodeFound)
                            {
                                ProcessDuplicateNode(n);
                            }
                            defaultSettingsNodeFound = true;
                            LoadDefaultSettings(db, n);
                        }
                        else if (MatchNodeName(n, XmlTags.SelectionSetsNode))
                        {
                            if (typeGroupsFound)
                            {
                                ProcessDuplicateNode(n);
                            }

                            typeGroupsFound = true;
                            LoadTypeGroups(db, n);
                        }
                        else if (MatchNodeName(n, XmlTags.ViewDefinitionsNode))
                        {
                            if (viewDefinitionsFound)
                            {
                                ProcessDuplicateNode(n);
                            }

                            viewDefinitionsFound = true;
                            LoadViewDefinitions(db, n);
                        }
                        else if (MatchNodeName(n, XmlTags.ControlsNode))
                        {
                            if (controlDefinitionsFound)
                            {
                                ProcessDuplicateNode(n);
                            }

                            controlDefinitionsFound = true;
                            LoadControlDefinitions(n, db.formatControlDefinitionHolder.controlDefinitionList);
                        }
                        else
                        {
                            ProcessUnknownNode(n);
                        }
                    }
                }
            }
            else
            {
                ProcessUnknownNode(documentElement);
            }
        }

        #region load formatting data from FormatViewDefinition

        /// <summary>
        /// Load the content of the ExtendedTypeDefinition instance into the db.
        /// Only support following view controls:
        ///     TableControl
        ///     ListControl
        ///     WideControl
        ///     CustomControl.
        /// </summary>
        /// <param name="typeDefinition">ExtendedTypeDefinition instances to load from, cannot be null.</param>
        /// <param name="db">Instance of the database to load into.</param>
        /// <param name="isForHelpOutput">True if the formatter is used for formatting help objects.</param>
        private void LoadData(ExtendedTypeDefinition typeDefinition, TypeInfoDataBase db, bool isForHelpOutput)
        {
            if (typeDefinition == null)
                throw PSTraceSource.NewArgumentNullException("viewDefinition");

            if (db == null)
                throw PSTraceSource.NewArgumentNullException(nameof(db));

            int viewIndex = 0;
            foreach (FormatViewDefinition formatView in typeDefinition.FormatViewDefinition)
            {
                ViewDefinition view = LoadViewFromObjectModel(typeDefinition.TypeNames, formatView, viewIndex++);
                if (view != null)
                {
                    ReportTrace(string.Create(CultureInfo.InvariantCulture, $"{ControlBase.GetControlShapeName(view.mainControl)} view {view.name} is loaded from the 'FormatViewDefinition' at index {viewIndex - 1} in 'ExtendedTypeDefinition' with type name {typeDefinition.TypeName}"));

                    // we are fine, add the view to the list
                    db.viewDefinitionsSection.viewDefinitionList.Add(view);

                    view.loadingInfo = this.LoadingInfo;
                    view.isHelpFormatter = isForHelpOutput;
                }
            }
        }

        /// <summary>
        /// Load the view into a ViewDefinition.
        /// </summary>
        /// <param name="typeNames">The TypeName tag under SelectedBy tag.</param>
        /// <param name="formatView"></param>
        /// <param name="viewIndex"></param>
        /// <returns></returns>
        private ViewDefinition LoadViewFromObjectModel(List<string> typeNames, FormatViewDefinition formatView, int viewIndex)
        {
            // Get AppliesTo information
            AppliesTo appliesTo = new AppliesTo();
            foreach (var typename in typeNames)
            {
                TypeReference tr = new TypeReference { name = typename };
                appliesTo.referenceList.Add(tr);
            }

            // Set AppliesTo and Name in the view
            ViewDefinition view = new ViewDefinition();
            view.appliesTo = appliesTo;
            view.name = formatView.Name;

            var firstTypeName = typeNames[0];
            PSControl control = formatView.Control;
            if (control is TableControl)
            {
                var tableControl = control as TableControl;
                view.mainControl = LoadTableControlFromObjectModel(tableControl, viewIndex, firstTypeName);
            }
            else if (control is ListControl)
            {
                var listControl = control as ListControl;
                view.mainControl = LoadListControlFromObjectModel(listControl, viewIndex, firstTypeName);
            }
            else if (control is WideControl)
            {
                var wideControl = control as WideControl;
                view.mainControl = LoadWideControlFromObjectModel(wideControl, viewIndex, firstTypeName);
            }
            else
            {
                view.mainControl = LoadCustomControlFromObjectModel((CustomControl)control, viewIndex, firstTypeName);
            }

            // Check if the PSControl is successfully loaded
            if (view.mainControl == null)
            {
                return null;
            }

            view.outOfBand = control.OutOfBand;

            if (control.GroupBy != null)
            {
                view.groupBy = new GroupBy
                {
                    startGroup = new StartGroup
                    {
                        expression = LoadExpressionFromObjectModel(control.GroupBy.Expression, viewIndex, firstTypeName)
                    }
                };
                if (control.GroupBy.Label != null)
                {
                    view.groupBy.startGroup.labelTextToken = new TextToken { text = control.GroupBy.Label };
                }

                if (control.GroupBy.CustomControl != null)
                {
                    view.groupBy.startGroup.control = LoadCustomControlFromObjectModel(control.GroupBy.CustomControl, viewIndex, firstTypeName);
                }
            }

            return view;
        }

        #region Load TableControl

        /// <summary>
        /// Load the TableControl to ControlBase.
        /// </summary>
        /// <param name="table"></param>
        /// <param name="viewIndex"></param>
        /// <param name="typeName"></param>
        /// <returns></returns>
        private ControlBase LoadTableControlFromObjectModel(TableControl table, int viewIndex, string typeName)
        {
            TableControlBody tableBody = new TableControlBody { autosize = table.AutoSize };

            LoadHeadersSectionFromObjectModel(tableBody, table.Headers);

            // No 'SelectedBy' data supplied, so the rowEntry will only be set to
            // tableBody.defaultDefinition. There cannot be more than one 'defaultDefinition'
            // defined for the tableBody.
            if (table.Rows.Count > 1)
            {
                this.ReportErrorForLoadingFromObjectModel(
                    StringUtil.Format(FormatAndOutXmlLoadingStrings.MultipleRowEntriesFoundInFormattingData, typeName, viewIndex, XmlTags.TableRowEntryNode), typeName);
                return null;
            }

            LoadRowEntriesSectionFromObjectModel(tableBody, table.Rows, viewIndex, typeName);
            // When error occurs while loading rowEntry, the tableBody.defaultDefinition would be null
            if (tableBody.defaultDefinition == null)
            {
                return null;
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
                    this.ReportErrorForLoadingFromObjectModel(
                        StringUtil.Format(FormatAndOutXmlLoadingStrings.IncorrectHeaderItemCountInFormattingData, typeName, viewIndex,
                        tableBody.header.columnHeaderDefinitionList.Count,
                        tableBody.defaultDefinition.rowItemDefinitionList.Count), typeName);

                    return null; // fatal error
                }
            }
            // CHECK: if there are alternative row definitions. There should be no alternative row definitions here.
            Diagnostics.Assert(tableBody.optionalDefinitionList.Count == 0,
                "there should be no alternative row definitions because no SelectedBy is defined for TableControlRow");

            return tableBody;
        }

        /// <summary>
        /// Load the headers defined for columns.
        /// </summary>
        /// <param name="tableBody"></param>
        /// <param name="headers"></param>
        private static void LoadHeadersSectionFromObjectModel(TableControlBody tableBody, List<TableControlColumnHeader> headers)
        {
            foreach (TableControlColumnHeader header in headers)
            {
                TableColumnHeaderDefinition chd = new TableColumnHeaderDefinition();

                // Contains:
                //   Label     --- Label     cardinality 0..1
                //   Width     --- Width     cardinality 0..1
                //   Alignment --- Alignment cardinality 0..1
                if (!string.IsNullOrEmpty(header.Label))
                {
                    TextToken tt = new TextToken();
                    tt.text = header.Label;
                    chd.label = tt;
                }

                chd.width = header.Width;
                chd.alignment = (int)header.Alignment;
                tableBody.header.columnHeaderDefinitionList.Add(chd);
            }
        }

        /// <summary>
        /// Load row enties, set the defaultDefinition of the TableControlBody.
        /// </summary>
        /// <param name="tableBody"></param>
        /// <param name="rowEntries"></param>
        /// <param name="viewIndex"></param>
        /// <param name="typeName"></param>
        private void LoadRowEntriesSectionFromObjectModel(TableControlBody tableBody, List<TableControlRow> rowEntries, int viewIndex, string typeName)
        {
            foreach (TableControlRow row in rowEntries)
            {
                TableRowDefinition trd = new TableRowDefinition { multiLine = row.Wrap };

                // Contains:
                //   Columns --- TableColumnItems cardinality: 0..1
                // No SelectedBy is supplied in the TableControlRow
                if (row.Columns.Count > 0)
                {
                    LoadColumnEntriesFromObjectModel(trd, row.Columns, viewIndex, typeName);
                    // trd.rowItemDefinitionList is null, it means there was a failure
                    if (trd.rowItemDefinitionList == null)
                    {
                        tableBody.defaultDefinition = null;
                        return;
                    }
                }

                if (row.SelectedBy != null)
                {
                    trd.appliesTo = LoadAppliesToSectionFromObjectModel(row.SelectedBy.TypeNames, row.SelectedBy.SelectionCondition);
                    tableBody.optionalDefinitionList.Add(trd);
                }
                else
                {
                    tableBody.defaultDefinition = trd;
                }
            }

            // rowEntries must not be empty
            if (tableBody.defaultDefinition == null)
            {
                this.ReportErrorForLoadingFromObjectModel(
                    StringUtil.Format(FormatAndOutXmlLoadingStrings.NoDefaultShapeEntryInFormattingData, typeName, viewIndex, XmlTags.TableRowEntryNode), typeName);
                return;
            }
        }

        /// <summary>
        /// Load the column items into the TableRowDefinition.
        /// </summary>
        /// <param name="trd"></param>
        /// <param name="columns"></param>
        /// <param name="viewIndex"></param>
        /// <param name="typeName"></param>
        private void LoadColumnEntriesFromObjectModel(TableRowDefinition trd, List<TableControlColumn> columns, int viewIndex, string typeName)
        {
            foreach (TableControlColumn column in columns)
            {
                TableRowItemDefinition rid = new TableRowItemDefinition();

                // Contain:
                //   DisplayEntry --- Expression cardinality: 0..1
                //   Alignment    --- Alignment  cardinality: 0..1
                if (column.DisplayEntry != null)
                {
                    ExpressionToken expression = LoadExpressionFromObjectModel(column.DisplayEntry, viewIndex, typeName);
                    if (expression == null)
                    {
                        trd.rowItemDefinitionList = null;
                        return;
                    }

                    FieldPropertyToken fpt = new FieldPropertyToken();
                    fpt.expression = expression;
                    fpt.fieldFormattingDirective.formatString = column.FormatString;
                    rid.formatTokenList.Add(fpt);
                }

                rid.alignment = (int)column.Alignment;
                trd.rowItemDefinitionList.Add(rid);
            }
        }

        #endregion Load TableControl

        /// <summary>
        /// Load the expression information from DisplayEntry.
        /// </summary>
        /// <param name="displayEntry"></param>
        /// <param name="viewIndex"></param>
        /// <param name="typeName"></param>
        /// <returns></returns>
        private ExpressionToken LoadExpressionFromObjectModel(DisplayEntry displayEntry, int viewIndex, string typeName)
        {
            ExpressionToken token = new ExpressionToken();
            if (displayEntry.ValueType == DisplayEntryValueType.Property)
            {
                token.expressionValue = displayEntry.Value;
                return token;
            }
            else if (displayEntry.ValueType == DisplayEntryValueType.ScriptBlock)
            {
                token.isScriptBlock = true;
                token.expressionValue = displayEntry.Value;

                try
                {
                    // For faster startup, we don't validate any of the built-in formatting script blocks, where isFullyTrusted == built-in.
                    if (!LoadingInfo.isFullyTrusted)
                    {
                        this.expressionFactory.VerifyScriptBlockText(token.expressionValue);
                    }
                }
                catch (ParseException e)
                {
                    // Error at
                    this.ReportErrorForLoadingFromObjectModel(
                        StringUtil.Format(FormatAndOutXmlLoadingStrings.InvalidScriptBlockInFormattingData, typeName, viewIndex, e.Message), typeName);
                    return null;
                }
                catch (Exception e) // will rethrow
                {
                    Diagnostics.Assert(false, "TypeInfoBaseLoader.VerifyScriptBlock unexpected exception " + e.GetType().FullName);
                    throw;
                }

                return token;
            }
            // this should never happen if the API is used correctly
            PSTraceSource.NewInvalidOperationException();
            return null;
        }

        /// <summary>
        /// Load EntrySelectedBy (TypeName) into AppliesTo.
        /// </summary>
        /// <returns></returns>
        private static AppliesTo LoadAppliesToSectionFromObjectModel(List<string> selectedBy, List<DisplayEntry> condition)
        {
            AppliesTo appliesTo = new AppliesTo();

            if (selectedBy != null)
            {
                foreach (string type in selectedBy)
                {
                    if (string.IsNullOrEmpty(type))
                        return null;
                    TypeReference tr = new TypeReference { name = type };
                    appliesTo.referenceList.Add(tr);
                }
            }

            if (condition != null)
            {
                foreach (var cond in condition)
                {
                    // TODO
                }
            }

            return appliesTo;
        }

        #region Load ListControl

        /// <summary>
        /// Load LoisControl into the ListControlBody.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="viewIndex"></param>
        /// <param name="typeName"></param>
        /// <returns></returns>
        private ListControlBody LoadListControlFromObjectModel(ListControl list, int viewIndex, string typeName)
        {
            ListControlBody listBody = new ListControlBody();

            // load the list entries section
            LoadListControlEntriesFromObjectModel(listBody, list.Entries, viewIndex, typeName);
            if (listBody.defaultEntryDefinition == null)
            {
                return null; // fatal error
            }

            return listBody;
        }

        private void LoadListControlEntriesFromObjectModel(ListControlBody listBody, List<ListControlEntry> entries, int viewIndex, string typeName)
        {
            // Contains:
            //   Entries --- ListEntries cardinality 1
            foreach (ListControlEntry listEntry in entries)
            {
                ListControlEntryDefinition lved = LoadListControlEntryDefinitionFromObjectModel(listEntry, viewIndex, typeName);
                if (lved == null)
                {
                    this.ReportErrorForLoadingFromObjectModel(
                        StringUtil.Format(FormatAndOutXmlLoadingStrings.LoadTagFailedInFormattingData, typeName, viewIndex, XmlTags.ListEntryNode), typeName);
                    listBody.defaultEntryDefinition = null;
                    return;
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
                        this.ReportErrorForLoadingFromObjectModel(
                            StringUtil.Format(FormatAndOutXmlLoadingStrings.TooManyDefaultShapeEntryInFormattingData, typeName, viewIndex, XmlTags.ListEntryNode), typeName);
                        listBody.defaultEntryDefinition = null;
                        return; // fatal error
                    }
                }
                else
                {
                    listBody.optionalEntryList.Add(lved);
                }
            }
            // list entries is empty
            if (listBody.defaultEntryDefinition == null)
            {
                // Error: there must be at least one default
                this.ReportErrorForLoadingFromObjectModel(
                    StringUtil.Format(FormatAndOutXmlLoadingStrings.NoDefaultShapeEntryInFormattingData, typeName, viewIndex, XmlTags.ListEntryNode), typeName);
            }
        }

        /// <summary>
        /// Load ListEntry into ListControlEntryDefinition.
        /// </summary>
        /// <param name="listEntry"></param>
        /// <param name="viewIndex"></param>
        /// <param name="typeName"></param>
        /// <returns></returns>
        private ListControlEntryDefinition LoadListControlEntryDefinitionFromObjectModel(ListControlEntry listEntry, int viewIndex, string typeName)
        {
            ListControlEntryDefinition lved = new ListControlEntryDefinition();

            // Contains:
            //   SelectedBy ---  EntrySelectedBy(TypeName)  cardinality 0..1
            //   Items      ---  ListItems                  cardinality 1
            if (listEntry.EntrySelectedBy != null)
            {
                lved.appliesTo = LoadAppliesToSectionFromObjectModel(listEntry.EntrySelectedBy.TypeNames, listEntry.EntrySelectedBy.SelectionCondition);
            }

            LoadListControlItemDefinitionsFromObjectModel(lved, listEntry.Items, viewIndex, typeName);
            if (lved.itemDefinitionList == null)
            {
                return null;
            }

            return lved;
        }

        /// <summary>
        /// Load ListItems into ListControlItemDefinition.
        /// </summary>
        /// <param name="lved"></param>
        /// <param name="listItems"></param>
        /// <param name="viewIndex"></param>
        /// <param name="typeName"></param>
        private void LoadListControlItemDefinitionsFromObjectModel(ListControlEntryDefinition lved, List<ListControlEntryItem> listItems, int viewIndex, string typeName)
        {
            foreach (ListControlEntryItem listItem in listItems)
            {
                ListControlItemDefinition lvid = new ListControlItemDefinition();

                // Contains:
                //   DisplayEntry --- Expression  cardinality 0..1
                //   Label        --- Label       cardinality 0..1
                if (listItem.DisplayEntry != null)
                {
                    ExpressionToken expression = LoadExpressionFromObjectModel(listItem.DisplayEntry, viewIndex, typeName);
                    if (expression == null)
                    {
                        lved.itemDefinitionList = null;
                        return; // fatal
                    }

                    FieldPropertyToken fpt = new FieldPropertyToken();
                    fpt.expression = expression;
                    fpt.fieldFormattingDirective.formatString = listItem.FormatString;
                    lvid.formatTokenList.Add(fpt);
                }

                if (!string.IsNullOrEmpty(listItem.Label))
                {
                    TextToken tt = new TextToken();
                    tt.text = listItem.Label;
                    lvid.label = tt;
                }

                lved.itemDefinitionList.Add(lvid);
            }

            // we must have at least a definition in th elist
            if (lved.itemDefinitionList.Count == 0)
            {
                // Error: At least one list view item must be specified.
                this.ReportErrorForLoadingFromObjectModel(
                    StringUtil.Format(FormatAndOutXmlLoadingStrings.NoListViewItemInFormattingData, typeName, viewIndex), typeName);
                lved.itemDefinitionList = null;
                return; // fatal
            }
        }

        #endregion Load ListControl

        #region Load WideControl

        /// <summary>
        /// Load the WideControl into the WideControlBody.
        /// </summary>
        /// <param name="wide"></param>
        /// <param name="viewIndex"></param>
        /// <param name="typeName"></param>
        /// <returns></returns>
        private WideControlBody LoadWideControlFromObjectModel(WideControl wide, int viewIndex, string typeName)
        {
            WideControlBody wideBody = new WideControlBody();

            // Contains:
            //   Columns --- ColumnNumbers  cardinality 0..1
            //   Entries --- WideEntries    cardinality 1
            wideBody.columns = (int)wide.Columns;

            if (wide.AutoSize)
                wideBody.autosize = true;

            LoadWideControlEntriesFromObjectModel(wideBody, wide.Entries, viewIndex, typeName);
            if (wideBody.defaultEntryDefinition == null)
            {
                // if we have no default entry definition, it means there was a failure
                return null;
            }

            return wideBody;
        }

        /// <summary>
        /// Load WideEntries.
        /// </summary>
        /// <param name="wideBody"></param>
        /// <param name="wideEntries"></param>
        /// <param name="viewIndex"></param>
        /// <param name="typeName"></param>
        private void LoadWideControlEntriesFromObjectModel(WideControlBody wideBody, List<WideControlEntryItem> wideEntries, int viewIndex, string typeName)
        {
            foreach (WideControlEntryItem wideItem in wideEntries)
            {
                WideControlEntryDefinition wved = LoadWideControlEntryFromObjectModel(wideItem, viewIndex, typeName);
                if (wved == null)
                {
                    this.ReportErrorForLoadingFromObjectModel(
                        StringUtil.Format(FormatAndOutXmlLoadingStrings.InvalidFormattingData, typeName, viewIndex, XmlTags.WideEntryNode), typeName);
                    wideBody.defaultEntryDefinition = null;
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
                        this.ReportErrorForLoadingFromObjectModel(
                            StringUtil.Format(FormatAndOutXmlLoadingStrings.TooManyDefaultShapeEntryInFormattingData, typeName, viewIndex, XmlTags.WideEntryNode), typeName);
                        wideBody.defaultEntryDefinition = null;
                        return; // fatal error
                    }
                }
                else
                {
                    wideBody.optionalEntryList.Add(wved);
                }
            }

            if (wideBody.defaultEntryDefinition == null)
            {
                this.ReportErrorForLoadingFromObjectModel(
                    StringUtil.Format(FormatAndOutXmlLoadingStrings.NoDefaultShapeEntryInFormattingData, typeName, viewIndex, XmlTags.WideEntryNode), typeName);
            }
        }

        /// <summary>
        /// Load WideEntry into WieControlEntryDefinition.
        /// </summary>
        /// <param name="wideItem"></param>
        /// <param name="viewIndex"></param>
        /// <param name="typeName"></param>
        /// <returns></returns>
        private WideControlEntryDefinition LoadWideControlEntryFromObjectModel(WideControlEntryItem wideItem, int viewIndex, string typeName)
        {
            WideControlEntryDefinition wved = new WideControlEntryDefinition();

            // Contains:
            //   SelectedBy   --- EntrySelectedBy (TypeName)  cardinality 0..1
            //   DisplayEntry --- WideItem (Expression)       cardinality 1
            // process selectedBy property
            if (wideItem.EntrySelectedBy != null)
            {
                wved.appliesTo = LoadAppliesToSectionFromObjectModel(wideItem.EntrySelectedBy.TypeNames, wideItem.EntrySelectedBy.SelectionCondition);
            }

            // process displayEntry property
            ExpressionToken expression = LoadExpressionFromObjectModel(wideItem.DisplayEntry, viewIndex, typeName);
            if (expression == null)
            {
                return null; // fatal
            }

            FieldPropertyToken fpt = new FieldPropertyToken();
            fpt.expression = expression;
            fpt.fieldFormattingDirective.formatString = wideItem.FormatString;
            wved.formatTokenList.Add(fpt);

            return wved;
        }

        #endregion Load WideControl

        #region Load CustomControl

        private ComplexControlBody LoadCustomControlFromObjectModel(CustomControl custom, int viewIndex, string typeName)
        {
            if (custom._cachedBody != null)
                return custom._cachedBody;

            var ccb = new ComplexControlBody();

            foreach (var entry in custom.Entries)
            {
                var cced = LoadComplexControlEntryDefinitionFromObjectModel(entry, viewIndex, typeName);
                if (cced.appliesTo == null)
                {
                    ccb.defaultEntry = cced;
                }
                else
                {
                    ccb.optionalEntryList.Add(cced);
                }
            }

            Interlocked.CompareExchange(ref custom._cachedBody, ccb, null);
            return ccb;
        }

        private ComplexControlEntryDefinition LoadComplexControlEntryDefinitionFromObjectModel(CustomControlEntry entry, int viewIndex, string typeName)
        {
            var cced = new ComplexControlEntryDefinition();
            if (entry.SelectedBy != null)
            {
                cced.appliesTo = LoadAppliesToSectionFromObjectModel(entry.SelectedBy.TypeNames, entry.SelectedBy.SelectionCondition);
            }

            foreach (var item in entry.CustomItems)
            {
                cced.itemDefinition.formatTokenList.Add(LoadFormatTokenFromObjectModel(item, viewIndex, typeName));
            }

            return cced;
        }

        private FormatToken LoadFormatTokenFromObjectModel(CustomItemBase item, int viewIndex, string typeName)
        {
            var newline = item as CustomItemNewline;
            if (newline != null)
            {
                return new NewLineToken { count = newline.Count };
            }

            var text = item as CustomItemText;
            if (text != null)
            {
                return new TextToken { text = text.Text };
            }

            var expr = item as CustomItemExpression;
            if (expr != null)
            {
                var cpt = new CompoundPropertyToken { enumerateCollection = expr.EnumerateCollection };

                if (expr.ItemSelectionCondition != null)
                {
                    cpt.conditionToken = LoadExpressionFromObjectModel(expr.ItemSelectionCondition, viewIndex, typeName);
                }

                if (expr.Expression != null)
                {
                    cpt.expression = LoadExpressionFromObjectModel(expr.Expression, viewIndex, typeName);
                }

                if (expr.CustomControl != null)
                {
                    cpt.control = LoadCustomControlFromObjectModel(expr.CustomControl, viewIndex, typeName);
                }

                return cpt;
            }

            var frame = (CustomItemFrame)item;
            var frameToken = new FrameToken
            {
                frameInfoDefinition =
                {
                    leftIndentation = (int)frame.LeftIndent,
                    rightIndentation = (int)frame.RightIndent,
                    firstLine = frame.FirstLineHanging != 0 ? -(int)frame.FirstLineHanging : (int)frame.FirstLineIndent
                }
            };

            foreach (var i in frame.CustomItems)
            {
                frameToken.itemDefinition.formatTokenList.Add(LoadFormatTokenFromObjectModel(i, viewIndex, typeName));
            }

            return frameToken;
        }

        #endregion Load Custom Control

        #endregion load formatting data from FormatViewDefinition

        #region Default Settings Loading

        private void LoadDefaultSettings(TypeInfoDataBase db, XmlNode defaultSettingsNode)
        {
            // all these nodes are of [0..1] cardinality
            bool propertyCountForTableFound = false;
            bool showErrorsAsMessagesFound = false;
            bool showErrorsInFormattedOutputFound = false;
            bool enumerableExpansionsFound = false;
            bool multilineTablesFound = false;

            bool tempVal;

            using (this.StackFrame(defaultSettingsNode))
            {
                foreach (XmlNode n in defaultSettingsNode.ChildNodes)
                {
                    if (MatchNodeName(n, XmlTags.ShowErrorsAsMessagesNode))
                    {
                        if (showErrorsAsMessagesFound)
                        {
                            ProcessDuplicateNode(n);
                        }

                        showErrorsAsMessagesFound = true;
                        if (ReadBooleanNode(n, out tempVal))
                            db.defaultSettingsSection.formatErrorPolicy.ShowErrorsAsMessages = tempVal;
                    }
                    else if (MatchNodeName(n, XmlTags.ShowErrorsInFormattedOutputNode))
                    {
                        if (showErrorsInFormattedOutputFound)
                        {
                            ProcessDuplicateNode(n);
                        }

                        showErrorsInFormattedOutputFound = true;
                        if (ReadBooleanNode(n, out tempVal))
                            db.defaultSettingsSection.formatErrorPolicy.ShowErrorsInFormattedOutput = tempVal;
                    }
                    else if (MatchNodeName(n, XmlTags.PropertyCountForTableNode))
                    {
                        if (propertyCountForTableFound)
                        {
                            ProcessDuplicateNode(n);
                        }

                        propertyCountForTableFound = true;
                        int val;
                        if (ReadPositiveIntegerValue(n, out val))
                        {
                            db.defaultSettingsSection.shapeSelectionDirectives.PropertyCountForTable = val;
                        }
                        else
                        {
                            // Error at XPath {0} in file {1}: Invalid {2} value.
                            this.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.InvalidNodeValue, ComputeCurrentXPath(), FilePath, XmlTags.PropertyCountForTableNode));
                        }
                    }
                    else if (MatchNodeName(n, XmlTags.MultilineTablesNode))
                    {
                        if (multilineTablesFound)
                        {
                            ProcessDuplicateNode(n);
                        }

                        multilineTablesFound = true;
                        if (ReadBooleanNode(n, out tempVal))
                        {
                            db.defaultSettingsSection.MultilineTables = tempVal;
                        }
                        else
                        {
                            // Error at XPath {0} in file {1}: Invalid {2} value.
                            this.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.InvalidNodeValue, ComputeCurrentXPath(), FilePath, XmlTags.MultilineTablesNode));
                        }
                    }
                    else if (MatchNodeName(n, XmlTags.EnumerableExpansionsNode))
                    {
                        if (enumerableExpansionsFound)
                        {
                            ProcessDuplicateNode(n);
                        }

                        enumerableExpansionsFound = true;
                        db.defaultSettingsSection.enumerableExpansionDirectiveList =
                            LoadEnumerableExpansionDirectiveList(n);
                    }
                    else
                    {
                        this.ProcessUnknownNode(n);
                    }
                }
            }
        }

        private List<EnumerableExpansionDirective> LoadEnumerableExpansionDirectiveList(XmlNode expansionListNode)
        {
            List<EnumerableExpansionDirective> retVal =
                                new List<EnumerableExpansionDirective>();
            using (this.StackFrame(expansionListNode))
            {
                int k = 0;
                foreach (XmlNode n in expansionListNode.ChildNodes)
                {
                    if (MatchNodeName(n, XmlTags.EnumerableExpansionNode))
                    {
                        EnumerableExpansionDirective eed = LoadEnumerableExpansionDirective(n, k++);
                        if (eed == null)
                        {
                            // Error at XPath {0} in file {1}: {2} failed to load.
                            this.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.LoadTagFailed, ComputeCurrentXPath(), FilePath, XmlTags.EnumerableExpansionNode));
                            return null; // fatal error
                        }

                        retVal.Add(eed);
                    }
                    else
                    {
                        this.ProcessUnknownNode(n);
                    }
                }
            }

            return retVal;
        }

        private EnumerableExpansionDirective LoadEnumerableExpansionDirective(XmlNode directive, int index)
        {
            using (this.StackFrame(directive, index))
            {
                EnumerableExpansionDirective eed = new EnumerableExpansionDirective();

                bool appliesToNodeFound = false;    // cardinality 1
                bool expandNodeFound = false;    // cardinality 1

                foreach (XmlNode n in directive.ChildNodes)
                {
                    if (MatchNodeName(n, XmlTags.EntrySelectedByNode))
                    {
                        if (appliesToNodeFound)
                        {
                            this.ProcessDuplicateNode(n);
                            return null; // fatal
                        }

                        appliesToNodeFound = true;
                        eed.appliesTo = LoadAppliesToSection(n, true);
                    }
                    else if (MatchNodeName(n, XmlTags.ExpandNode))
                    {
                        if (expandNodeFound)
                        {
                            this.ProcessDuplicateNode(n);
                            return null; // fatal
                        }

                        expandNodeFound = true;
                        string s = GetMandatoryInnerText(n);
                        if (s == null)
                        {
                            return null; // fatal
                        }

                        bool success = EnumerableExpansionConversion.Convert(s, out eed.enumerableExpansion);
                        if (!success)
                        {
                            // Error at XPath {0} in file {1}: Invalid {2} value.
                            this.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.InvalidNodeValue, ComputeCurrentXPath(), FilePath, XmlTags.ExpandNode));
                            return null; // fatal
                        }
                    }
                    else
                    {
                        this.ProcessUnknownNode(n);
                    }
                }

                return eed;
            }
        }

        #endregion

        #region Type Groups Loading

        private void LoadTypeGroups(TypeInfoDataBase db, XmlNode typeGroupsNode)
        {
            using (this.StackFrame(typeGroupsNode))
            {
                int typeGroupCount = 0;

                foreach (XmlNode n in typeGroupsNode.ChildNodes)
                {
                    if (MatchNodeName(n, XmlTags.SelectionSetNode))
                    {
                        LoadTypeGroup(db, n, typeGroupCount++);
                    }
                    else
                    {
                        ProcessUnknownNode(n);
                    }
                }
            }
        }

        private void LoadTypeGroup(TypeInfoDataBase db, XmlNode typeGroupNode, int index)
        {
            using (this.StackFrame(typeGroupNode, index))
            {
                // create data structure
                TypeGroupDefinition typeGroupDefinition = new TypeGroupDefinition();
                bool nameNodeFound = false;

                foreach (XmlNode n in typeGroupNode.ChildNodes)
                {
                    if (MatchNodeName(n, XmlTags.NameNode))
                    {
                        if (nameNodeFound)
                        {
                            this.ProcessDuplicateNode(n);
                            continue;
                        }

                        nameNodeFound = true;
                        typeGroupDefinition.name = GetMandatoryInnerText(n);
                    }
                    else if (MatchNodeName(n, XmlTags.TypesNode))
                    {
                        LoadTypeGroupTypeRefs(n, typeGroupDefinition);
                    }
                    else
                    {
                        this.ProcessUnknownNode(n);
                    }
                }

                if (!nameNodeFound)
                {
                    this.ReportMissingNode(XmlTags.NameNode);
                }

                // finally add to the list
                db.typeGroupSection.typeGroupDefinitionList.Add(typeGroupDefinition);
            }
        }

        private void LoadTypeGroupTypeRefs(XmlNode typesNode, TypeGroupDefinition typeGroupDefinition)
        {
            using (this.StackFrame(typesNode))
            {
                int typeRefCount = 0;

                foreach (XmlNode n in typesNode.ChildNodes)
                {
                    if (MatchNodeName(n, XmlTags.TypeNameNode))
                    {
                        using (this.StackFrame(n, typeRefCount++))
                        {
                            TypeReference tr = new TypeReference();

                            tr.name = GetMandatoryInnerText(n);
                            typeGroupDefinition.typeReferenceList.Add(tr);
                        }
                    }
                    else
                    {
                        ProcessUnknownNode(n);
                    }
                }
            }
        }

        #endregion

        #region AppliesTo Loading

        private AppliesTo LoadAppliesToSection(XmlNode appliesToNode, bool allowSelectionCondition)
        {
            using (this.StackFrame(appliesToNode))
            {
                AppliesTo appliesTo = new AppliesTo();

                // expect: type ref, group ref, or nothing
                foreach (XmlNode n in appliesToNode.ChildNodes)
                {
                    using (this.StackFrame(n))
                    {
                        if (MatchNodeName(n, XmlTags.SelectionSetNameNode))
                        {
                            TypeGroupReference tgr = LoadTypeGroupReference(n);
                            if (tgr != null)
                            {
                                appliesTo.referenceList.Add(tgr);
                            }
                            else
                            {
                                return null;
                            }
                        }
                        else if (MatchNodeName(n, XmlTags.TypeNameNode))
                        {
                            TypeReference tr = LoadTypeReference(n);
                            if (tr != null)
                            {
                                appliesTo.referenceList.Add(tr);
                            }
                            else
                            {
                                return null;
                            }
                        }
                        else if (allowSelectionCondition && MatchNodeName(n, XmlTags.SelectionConditionNode))
                        {
                            TypeOrGroupReference tgr = LoadSelectionConditionNode(n);
                            if (tgr != null)
                            {
                                appliesTo.referenceList.Add(tgr);
                            }
                            else
                            {
                                return null;
                            }
                        }
                        else
                        {
                            this.ProcessUnknownNode(n);
                        }
                    }
                }

                if (appliesTo.referenceList.Count == 0)
                {
                    // we do not accept an empty list
                    // Error at XPath {0} in file {1}: No type or condition is specified for applying the view.
                    this.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.EmptyAppliesTo, ComputeCurrentXPath(), FilePath));
                    return null;
                }

                return appliesTo;
            }
        }

        private TypeReference LoadTypeReference(XmlNode n)
        {
            string val = GetMandatoryInnerText(n);

            if (val != null)
            {
                TypeReference tr = new TypeReference();
                tr.name = val;
                return tr;
            }

            return null;
        }

        private TypeGroupReference LoadTypeGroupReference(XmlNode n)
        {
            string val = GetMandatoryInnerText(n);

            if (val != null)
            {
                TypeGroupReference tgr = new TypeGroupReference();
                tgr.name = val;
                return tgr;
            }

            return null;
        }

        private TypeOrGroupReference LoadSelectionConditionNode(XmlNode selectionConditionNode)
        {
            using (this.StackFrame(selectionConditionNode))
            {
                TypeOrGroupReference retVal = null;

                bool expressionNodeFound = false;       // cardinality 1

                // these two nodes are mutually exclusive
                bool typeFound = false;              // cardinality 0..1
                bool typeGroupFound = false;              // cardinality 0..1

                ExpressionNodeMatch expressionMatch = new ExpressionNodeMatch(this);

                foreach (XmlNode n in selectionConditionNode.ChildNodes)
                {
                    if (MatchNodeName(n, XmlTags.SelectionSetNameNode))
                    {
                        if (typeGroupFound)
                        {
                            this.ProcessDuplicateAlternateNode(n, XmlTags.SelectionSetNameNode, XmlTags.TypeNameNode);
                            return null;
                        }

                        typeGroupFound = true;
                        TypeGroupReference tgr = LoadTypeGroupReference(n);
                        if (tgr != null)
                        {
                            retVal = tgr;
                        }
                        else
                        {
                            return null;
                        }
                    }
                    else if (MatchNodeName(n, XmlTags.TypeNameNode))
                    {
                        if (typeFound)
                        {
                            this.ProcessDuplicateAlternateNode(n, XmlTags.SelectionSetNameNode, XmlTags.TypeNameNode);
                            return null;
                        }

                        typeFound = true;
                        TypeReference tr = LoadTypeReference(n);
                        if (tr != null)
                        {
                            retVal = tr;
                        }
                        else
                        {
                            return null;
                        }
                    }
                    else if (expressionMatch.MatchNode(n))
                    {
                        if (expressionNodeFound)
                        {
                            this.ProcessDuplicateNode(n);
                            return null; // fatal error
                        }

                        expressionNodeFound = true;
                        if (!expressionMatch.ProcessNode(n))
                            return null; // fatal error
                    }
                    else
                    {
                        this.ProcessUnknownNode(n);
                    }
                }

                if (typeFound && typeGroupFound)
                {
                    // Error at XPath {0} in file {1}: Cannot have SelectionSetName and TypeName at the same time.
                    this.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.SelectionSetNameAndTypeName, ComputeCurrentXPath(), FilePath));
                    return null; // fatal error
                }

                if (retVal == null)
                {
                    // missing mandatory node
                    this.ReportMissingNodes(new string[] { XmlTags.SelectionSetNameNode, XmlTags.TypeNameNode });
                    return null;
                }

                if (expressionNodeFound)
                {
                    // mandatory node
                    retVal.conditionToken = expressionMatch.GenerateExpressionToken();
                    if (retVal.conditionToken == null)
                    {
                        return null; // fatal error
                    }

                    return retVal;
                }
                // failure: expression is mandatory
                // Error at XPath {0} in file {1}: An expression is expected.
                this.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.ExpectExpression, ComputeCurrentXPath(), FilePath));
                return null;
            }
        }
        #endregion

        #region GroupBy Loading

        private GroupBy LoadGroupBySection(XmlNode groupByNode)
        {
            using (this.StackFrame(groupByNode))
            {
                ExpressionNodeMatch expressionMatch = new ExpressionNodeMatch(this);
                ComplexControlMatch controlMatch = new ComplexControlMatch(this);

                bool expressionNodeFound = false;       // cardinality 0..1

                // these two nodes are mutually exclusive
                bool controlFound = false;              // cardinality 0..1
                bool labelFound = false;              // cardinality 0..1

                GroupBy groupBy = new GroupBy();
                TextToken labelTextToken = null;

                foreach (XmlNode n in groupByNode)
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
                            return null; // fatal error
                    }
                    else if (controlMatch.MatchNode(n))
                    {
                        if (controlFound)
                        {
                            this.ProcessDuplicateAlternateNode(n, XmlTags.ComplexControlNode, XmlTags.ComplexControlNameNode);
                            return null;
                        }

                        controlFound = true;
                        if (!controlMatch.ProcessNode(n))
                            return null; // fatal error
                    }
                    else if (MatchNodeNameWithAttributes(n, XmlTags.LabelNode))
                    {
                        if (labelFound)
                        {
                            this.ProcessDuplicateAlternateNode(n, XmlTags.ComplexControlNode, XmlTags.ComplexControlNameNode);
                            return null;
                        }

                        labelFound = true;

                        labelTextToken = LoadLabel(n);
                        if (labelTextToken == null)
                        {
                            return null; // fatal error
                        }
                    }
                    else
                    {
                        this.ProcessUnknownNode(n);
                    }
                }

                if (controlFound && labelFound)
                {
                    // Error at XPath {0} in file {1}: Cannot have control and label at the same time.
                    this.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.ControlAndLabel, ComputeCurrentXPath(), FilePath));
                    return null; // fatal error
                }

                if (controlFound || labelFound)
                {
                    if (!expressionNodeFound)
                    {
                        // Error at XPath {0} in file {1}: Cannot have control or label without an expression.
                        this.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.ControlLabelWithoutExpression, ComputeCurrentXPath(), FilePath));
                        return null; // fatal error
                    }

                    if (controlFound)
                    {
                        groupBy.startGroup.control = controlMatch.Control;
                    }
                    else if (labelFound)
                    {
                        groupBy.startGroup.labelTextToken = labelTextToken;
                    }
                }

                if (expressionNodeFound)
                {
                    // we add only if we encountered one, since it's not mandatory
                    ExpressionToken expression = expressionMatch.GenerateExpressionToken();
                    if (expression == null)
                    {
                        return null; // fatal error
                    }

                    groupBy.startGroup.expression = expression;
                    return groupBy;
                }

                // failure: expression is mandatory
                // Error at XPath {0} in file {1}: An expression is expected.
                this.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.ExpectExpression, ComputeCurrentXPath(), FilePath));
                return null;
            }
        }

        private TextToken LoadLabel(XmlNode textNode)
        {
            using (this.StackFrame(textNode))
            {
                return LoadTextToken(textNode);
            }
        }

        private TextToken LoadTextToken(XmlNode n)
        {
            TextToken tt = new TextToken();

            if (!LoadStringResourceReference(n, out tt.resource))
            {
                return null;
            }

            if (tt.resource != null)
            {
                // inner text is optional
                tt.text = n.InnerText;
                return tt;
            }

            // inner text is mandatory
            tt.text = this.GetMandatoryInnerText(n);

            if (tt.text == null)
                return null;

            return tt;
        }

        private bool LoadStringResourceReference(XmlNode n, out StringResourceReference resource)
        {
            resource = null;
            XmlElement e = n as XmlElement;

            if (e == null)
            {
                // Error at XPath {0} in file {1}: Node should be an XmlElement.
                this.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.NonXmlElementNode, ComputeCurrentXPath(), FilePath));
                return false;
            }

            if (e.Attributes.Count <= 0)
            {
                // no resources to load
                return true;
            }

            // need to find mandatory attributes
            resource = LoadResourceAttributes(e.Attributes);

            // we committed to having resources, if not obtained, it's an error
            return resource != null;
        }

        private StringResourceReference LoadResourceAttributes(XmlAttributeCollection attributes)
        {
            StringResourceReference resource = new StringResourceReference();
            foreach (XmlAttribute a in attributes)
            {
                if (MatchAttributeName(a, XmlTags.AssemblyNameAttribute))
                {
                    resource.assemblyName = GetMandatoryAttributeValue(a);
                    if (resource.assemblyName == null)
                        return null;
                }
                else if (MatchAttributeName(a, XmlTags.BaseNameAttribute))
                {
                    resource.baseName = GetMandatoryAttributeValue(a);
                    if (resource.baseName == null)
                        return null;
                }
                else if (MatchAttributeName(a, XmlTags.ResourceIdAttribute))
                {
                    resource.resourceId = GetMandatoryAttributeValue(a);
                    if (resource.resourceId == null)
                        return null;
                }
                else
                {
                    ProcessUnknownAttribute(a);
                    return null;
                }
            }

            // make sure we got all the attributes, since allof them are mandatory
            if (resource.assemblyName == null)
            {
                ReportMissingAttribute(XmlTags.AssemblyNameAttribute);
                return null;
            }

            if (resource.baseName == null)
            {
                ReportMissingAttribute(XmlTags.BaseNameAttribute);
                return null;
            }

            if (resource.resourceId == null)
            {
                ReportMissingAttribute(XmlTags.ResourceIdAttribute);
                return null;
            }

            // success in loading
            resource.loadingInfo = this.LoadingInfo;

            // optional pre-load and binding verification
            if (this.VerifyStringResources)
            {
                DisplayResourceManagerCache.LoadingResult result;
                DisplayResourceManagerCache.AssemblyBindingStatus bindingStatus;
                this.displayResourceManagerCache.VerifyResource(resource, out result, out bindingStatus);
                if (result != DisplayResourceManagerCache.LoadingResult.NoError)
                {
                    ReportStringResourceFailure(resource, result, bindingStatus);
                    return null;
                }
            }

            return resource;
        }

        private void ReportStringResourceFailure(StringResourceReference resource,
                                                    DisplayResourceManagerCache.LoadingResult result,
                                                    DisplayResourceManagerCache.AssemblyBindingStatus bindingStatus)
        {
            string assemblyDisplayName;
            switch (bindingStatus)
            {
                case DisplayResourceManagerCache.AssemblyBindingStatus.FoundInPath:
                    {
                        assemblyDisplayName = resource.assemblyLocation;
                    }

                    break;
                case DisplayResourceManagerCache.AssemblyBindingStatus.FoundInGac:
                    {
                        // "(Global Assembly Cache) {0}"
                        assemblyDisplayName = StringUtil.Format(FormatAndOutXmlLoadingStrings.AssemblyInGAC, resource.assemblyName);
                    }

                    break;
                default:
                    {
                        assemblyDisplayName = resource.assemblyName;
                    }

                    break;
            }

            string msg = null;
            switch (result)
            {
                case DisplayResourceManagerCache.LoadingResult.AssemblyNotFound:
                    {
                        // Error at XPath {0} in file {1}: Assembly {2} is not found.
                        msg = StringUtil.Format(FormatAndOutXmlLoadingStrings.AssemblyNotFound, ComputeCurrentXPath(), FilePath, assemblyDisplayName);
                    }

                    break;
                case DisplayResourceManagerCache.LoadingResult.ResourceNotFound:
                    {
                        // Error at XPath {0} in file {1}: Resource {2} in assembly {3} is not found.
                        msg = StringUtil.Format(FormatAndOutXmlLoadingStrings.ResourceNotFound, ComputeCurrentXPath(), FilePath, resource.baseName, assemblyDisplayName);
                    }

                    break;
                case DisplayResourceManagerCache.LoadingResult.StringNotFound:
                    {
                        // Error at XPath {0} in file {1}: String {2} from resource {3} in assembly {4} is not found.
                        msg = StringUtil.Format(FormatAndOutXmlLoadingStrings.StringResourceNotFound, ComputeCurrentXPath(), FilePath,
                            resource.resourceId, resource.baseName, assemblyDisplayName);
                    }

                    break;
            }

            this.ReportError(msg);
        }

        #endregion

        #region Expression Loading
        /// <summary>
        /// Helper to verify the text of a string block and
        /// log an error if an exception is thrown.
        /// </summary>
        /// <param name="scriptBlockText">Script block string to verify.</param>
        /// <returns>True if parsed correctly, false if failed.</returns>
        internal bool VerifyScriptBlock(string scriptBlockText)
        {
            try
            {
                this.expressionFactory.VerifyScriptBlockText(scriptBlockText);
            }
            catch (ParseException e)
            {
                // Error at XPath {0} in file {1}: Invalid script block "{2}".
                this.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.InvalidScriptBlock, ComputeCurrentXPath(), FilePath, e.Message));
                return false;
            }
            catch (Exception e) // will rethrow
            {
                Diagnostics.Assert(false, "TypeInfoBaseLoader.VerifyScriptBlock unexpected exception " + e.GetType().FullName);
                throw;
            }

            return true;
        }

        /// <summary>
        /// Helper class to wrap the loading of a script block/property name alternative tag.
        /// </summary>
        private sealed class ExpressionNodeMatch
        {
            internal ExpressionNodeMatch(TypeInfoDataBaseLoader loader)
            {
                _loader = loader;
            }

            internal bool MatchNode(XmlNode n)
            {
                return _loader.MatchNodeName(n, XmlTags.PropertyNameNode) || _loader.MatchNodeName(n, XmlTags.ScriptBlockNode);
            }

            internal bool ProcessNode(XmlNode n)
            {
                if (_loader.MatchNodeName(n, XmlTags.PropertyNameNode))
                {
                    if (_token != null)
                    {
                        if (_token.isScriptBlock)
                            _loader.ProcessDuplicateAlternateNode(n, XmlTags.PropertyNameNode, XmlTags.ScriptBlockNode);
                        else
                            _loader.ProcessDuplicateNode(n);
                        return false; // fatal error
                    }

                    _token = new ExpressionToken();
                    _token.expressionValue = _loader.GetMandatoryInnerText(n);
                    if (_token.expressionValue == null)
                    {
                        // Error at XPath {0} in file {1}: Missing property.
                        _loader.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.NoProperty, _loader.ComputeCurrentXPath(), _loader.FilePath));
                        _fatalError = true;
                        return false; // fatal error
                    }

                    return true;
                }
                else if (_loader.MatchNodeName(n, XmlTags.ScriptBlockNode))
                {
                    if (_token != null)
                    {
                        if (!_token.isScriptBlock)
                            _loader.ProcessDuplicateAlternateNode(n, XmlTags.PropertyNameNode, XmlTags.ScriptBlockNode);
                        else
                            _loader.ProcessDuplicateNode(n);
                        return false; // fatal error
                    }

                    _token = new ExpressionToken();
                    _token.isScriptBlock = true;
                    _token.expressionValue = _loader.GetMandatoryInnerText(n);
                    if (_token.expressionValue == null)
                    {
                        // Error at XPath {0} in file {1}: Missing script block text.
                        _loader.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.NoScriptBlockText, _loader.ComputeCurrentXPath(), _loader.FilePath));
                        _fatalError = true;
                        return false; // fatal error
                    }

                    if ((!_loader._suppressValidation) && (!_loader.VerifyScriptBlock(_token.expressionValue)))
                    {
                        _fatalError = true;
                        return false; // fatal error
                    }

                    return true;
                }
                // this should never happen if the API is used correctly
                PSTraceSource.NewInvalidOperationException();
                return false;
            }

            internal ExpressionToken GenerateExpressionToken()
            {
                if (_fatalError)
                {
                    // we failed the loading already, just return
                    return null;
                }

                if (_token == null)
                {
                    // we do not have a token: we never got one
                    // the user should have specified either a property or a script block
                    _loader.ReportMissingNodes(new string[] { XmlTags.PropertyNameNode, XmlTags.ScriptBlockNode });
                    return null;
                }

                return _token;
            }

            private readonly TypeInfoDataBaseLoader _loader;
            private ExpressionToken _token;
            private bool _fatalError = false;
        }

        /// <summary>
        /// Helper class to wrap the loading of an expression (using ExpressionNodeMatch)
        /// plus the formatting string and an alternative text node.
        /// </summary>
        private sealed class ViewEntryNodeMatch
        {
            internal ViewEntryNodeMatch(TypeInfoDataBaseLoader loader)
            {
                _loader = loader;
            }

            internal bool ProcessExpressionDirectives(XmlNode containerNode, List<XmlNode> unprocessedNodes)
            {
                if (containerNode == null)
                    throw PSTraceSource.NewArgumentNullException(nameof(containerNode));

                string formatString = null;
                TextToken textToken = null;
                ExpressionNodeMatch expressionMatch = new ExpressionNodeMatch(_loader);

                bool formatStringNodeFound = false; // cardinality 0..1
                bool expressionNodeFound = false;   // cardinality 0..1
                bool textNodeFound = false;         // cardinality 0..1

                foreach (XmlNode n in containerNode.ChildNodes)
                {
                    if (expressionMatch.MatchNode(n))
                    {
                        if (expressionNodeFound)
                        {
                            _loader.ProcessDuplicateNode(n);
                            return false; // fatal error
                        }

                        expressionNodeFound = true;
                        if (!expressionMatch.ProcessNode(n))
                            return false; // fatal error
                    }
                    else if (_loader.MatchNodeName(n, XmlTags.FormatStringNode))
                    {
                        if (formatStringNodeFound)
                        {
                            _loader.ProcessDuplicateNode(n);
                            return false; // fatal error
                        }

                        formatStringNodeFound = true;
                        formatString = _loader.GetMandatoryInnerText(n);
                        if (formatString == null)
                        {
                            // Error at XPath {0} in file {1}: Missing a format string.
                            _loader.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.NoFormatString, _loader.ComputeCurrentXPath(), _loader.FilePath));
                            return false; // fatal error
                        }
                    }
                    else if (_loader.MatchNodeNameWithAttributes(n, XmlTags.TextNode))
                    {
                        if (textNodeFound)
                        {
                            _loader.ProcessDuplicateNode(n);
                            return false; // fatal error
                        }

                        textNodeFound = true;
                        textToken = _loader.LoadText(n);
                        if (textToken == null)
                        {
                            // Error at XPath {0} in file {1}: Invalid {2}.
                            _loader.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.InvalidNode, _loader.ComputeCurrentXPath(), _loader.FilePath, XmlTags.TextNode));
                            return false; // fatal error
                        }
                    }
                    else
                    {
                        // for further processing by calling context
                        unprocessedNodes.Add(n);
                    }
                }

                if (expressionNodeFound)
                {
                    // RULE: cannot have a text node and an expression at the same time
                    if (textNodeFound)
                    {
                        // Error at XPath {0} in file {1}: {2} cannot be specified with an expression.
                        _loader.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.NodeWithExpression, _loader.ComputeCurrentXPath(),
                            _loader.FilePath, XmlTags.TextNode));
                        return false; // fatal error
                    }

                    ExpressionToken expression = expressionMatch.GenerateExpressionToken();
                    if (expression == null)
                    {
                        return false; // fatal error
                    }

                    // set the output data
                    if (!string.IsNullOrEmpty(formatString))
                    {
                        _formatString = formatString;
                    }

                    _expression = expression;
                }
                else
                {
                    // RULE: we cannot have a format string without an expression node
                    if (formatStringNodeFound)
                    {
                        // Error at XPath {0} in file {1}: {2} cannot be specified without an expression.
                        _loader.ReportError(StringUtil.Format(FormatAndOutXmlLoadingStrings.NodeWithoutExpression, _loader.ComputeCurrentXPath(),
                            _loader.FilePath, XmlTags.FormatStringNode));
                        return false; // fatal error
                    }

                    // we might have a text node
                    if (textNodeFound)
                    {
                        _textToken = textToken;
                    }
                }

                return true;
            }

            internal string FormatString { get { return _formatString; } }

            internal TextToken TextToken { get { return _textToken; } }

            internal ExpressionToken Expression { get { return _expression; } }

            private string _formatString;
            private TextToken _textToken;
            private ExpressionToken _expression;

            private readonly TypeInfoDataBaseLoader _loader;
        }

        #endregion

        #region Complex Control Loading

        private sealed class ComplexControlMatch
        {
            internal ComplexControlMatch(TypeInfoDataBaseLoader loader)
            {
                _loader = loader;
            }

            internal bool MatchNode(XmlNode n)
            {
                return _loader.MatchNodeName(n, XmlTags.ComplexControlNode) ||
                        _loader.MatchNodeName(n, XmlTags.ComplexControlNameNode);
            }

            internal bool ProcessNode(XmlNode n)
            {
                if (_loader.MatchNodeName(n, XmlTags.ComplexControlNode))
                {
                    // load an embedded complex control
                    _control = _loader.LoadComplexControl(n);
                    return true;
                }
                else if (_loader.MatchNodeName(n, XmlTags.ComplexControlNameNode))
                {
                    string name = _loader.GetMandatoryInnerText(n);
                    if (name == null)
                    {
                        return false;
                    }

                    ControlReference controlRef = new ControlReference();
                    controlRef.name = name;
                    controlRef.controlType = typeof(ComplexControlBody);
                    _control = controlRef;
                    return true;
                }
                // this should never happen if the API is used correctly
                PSTraceSource.NewInvalidOperationException();
                return false;
            }

            internal ControlBase Control
            {
                get { return _control; }
            }

            private ControlBase _control;
            private readonly TypeInfoDataBaseLoader _loader;
        }

        #endregion
    }
}
