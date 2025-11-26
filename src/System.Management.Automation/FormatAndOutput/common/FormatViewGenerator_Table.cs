// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Internal;

namespace Microsoft.PowerShell.Commands.Internal.Format
{
    internal sealed class TableViewGenerator : ViewGenerator
    {
        // tableBody to use for this instance of the ViewGenerator;
        private TableControlBody _tableBody;

        internal override void Initialize(TerminatingErrorContext terminatingErrorContext, PSPropertyExpressionFactory mshExpressionFactory, TypeInfoDataBase db, ViewDefinition view, FormattingCommandLineParameters formatParameters)
        {
            base.Initialize(terminatingErrorContext, mshExpressionFactory, db, view, formatParameters);
            if ((this.dataBaseInfo != null) && (this.dataBaseInfo.view != null))
            {
                _tableBody = (TableControlBody)this.dataBaseInfo.view.mainControl;
            }
        }

        internal override void Initialize(TerminatingErrorContext errorContext, PSPropertyExpressionFactory expressionFactory,
                                        PSObject so, TypeInfoDataBase db,
            FormattingCommandLineParameters parameters)
        {
            base.Initialize(errorContext, expressionFactory, so, db, parameters);

            if ((this.dataBaseInfo != null) && (this.dataBaseInfo.view != null))
            {
                _tableBody = (TableControlBody)this.dataBaseInfo.view.mainControl;
            }

            List<MshParameter> rawMshParameterList = null;

            if (parameters != null)
                rawMshParameterList = parameters.mshParameterList;

            // check if we received properties from the command line
            if (rawMshParameterList != null && rawMshParameterList.Count > 0)
            {
                this.activeAssociationList = AssociationManager.ExpandTableParameters(rawMshParameterList, so);
                ApplyExcludePropertyFilter();
                return;
            }

            // we did not get any properties:
            // try to get properties from the default property set of the object
            this.activeAssociationList = AssociationManager.ExpandDefaultPropertySet(so, this.expressionFactory);
            if (this.activeAssociationList.Count > 0)
            {
                // we got a valid set of properties from the default property set..add computername for
                // remoteobjects (if available)
                if (PSObjectHelper.ShouldShowComputerNameProperty(so))
                {
                    activeAssociationList.Add(new MshResolvedExpressionParameterAssociation(null,
                        new PSPropertyExpression(RemotingConstants.ComputerNameNoteProperty)));
                }

                ApplyExcludePropertyFilter();
                return;
            }

            // we failed to get anything from the default property set
            this.activeAssociationList = AssociationManager.ExpandAll(so);
            if (this.activeAssociationList.Count > 0)
            {
                // Remove PSComputerName and PSShowComputerName from the display as needed.
                AssociationManager.HandleComputerNameProperties(so, activeAssociationList);
                FilterActiveAssociationList();
                ApplyExcludePropertyFilter();
                return;
            }

            // we were unable to retrieve any properties, so we leave an empty list
            this.activeAssociationList = new List<MshResolvedExpressionParameterAssociation>();
        }

        /// <summary>
        /// Let the view prepare itself for RemoteObjects. This will add "ComputerName" to the
        /// table columns.
        /// </summary>
        /// <param name="so"></param>
        internal override void PrepareForRemoteObjects(PSObject so)
        {
            Diagnostics.Assert(so != null, "so cannot be null");

            // make sure computername property exists.
            Diagnostics.Assert(so.Properties[RemotingConstants.ComputerNameNoteProperty] != null,
                "PrepareForRemoteObjects cannot be called when the object does not contain ComputerName property.");

            if ((dataBaseInfo != null) && (dataBaseInfo.view != null) && (dataBaseInfo.view.mainControl != null))
            {
                // dont change the original format definition in the database..just make a copy and work
                // with the copy
                _tableBody = (TableControlBody)this.dataBaseInfo.view.mainControl.Copy();

                TableRowItemDefinition cnRowDefinition = new TableRowItemDefinition();
                PropertyTokenBase propToken = new FieldPropertyToken();
                propToken.expression = new ExpressionToken(RemotingConstants.ComputerNameNoteProperty, false);
                cnRowDefinition.formatTokenList.Add(propToken);
                _tableBody.defaultDefinition.rowItemDefinitionList.Add(cnRowDefinition);

                // add header only if there are other header definitions
                if (_tableBody.header.columnHeaderDefinitionList.Count > 0)
                {
                    TableColumnHeaderDefinition cnHeaderDefinition = new TableColumnHeaderDefinition();
                    cnHeaderDefinition.label = new TextToken();
                    cnHeaderDefinition.label.text = RemotingConstants.ComputerNameNoteProperty;
                    _tableBody.header.columnHeaderDefinitionList.Add(cnHeaderDefinition);
                }
            }
        }

        internal override FormatStartData GenerateStartData(PSObject so)
        {
            FormatStartData startFormat = base.GenerateStartData(so);

            if (this.dataBaseInfo.view != null)
                startFormat.shapeInfo = GenerateTableHeaderInfoFromDataBaseInfo(so);
            else
                startFormat.shapeInfo = GenerateTableHeaderInfoFromProperties(so);
            return startFormat;
        }

        /// <summary>
        /// Method to filter resolved expressions as per table view needs.
        /// For v1.0, table view supports only 10 properties.
        ///
        /// This method filters and updates "activeAssociationList" instance property.
        /// </summary>
        /// <returns>None.</returns>
        /// <remarks>This method updates "activeAssociationList" instance property.</remarks>
        private void FilterActiveAssociationList()
        {
            // we got a valid set of properties from the default property set
            // make sure we do not have too many properties

            // NOTE: this is an arbitrary number, chosen to be a sensitive default
            const int nMax = 10;

            if (activeAssociationList.Count > nMax)
            {
                List<MshResolvedExpressionParameterAssociation> tmp = this.activeAssociationList;
                this.activeAssociationList = new List<MshResolvedExpressionParameterAssociation>();
                for (int k = 0; k < nMax; k++)
                    this.activeAssociationList.Add(tmp[k]);
            }

            return;
        }

        private TableHeaderInfo GenerateTableHeaderInfoFromDataBaseInfo(PSObject so)
        {
            TableHeaderInfo thi = new TableHeaderInfo();

            bool dummy;
            List<TableRowItemDefinition> activeRowItemDefinitionList = GetActiveTableRowDefinition(_tableBody, so, out dummy);
            thi.hideHeader = this.HideHeaders;
            thi.repeatHeader = this.RepeatHeader;

            int col = 0;
            foreach (TableRowItemDefinition rowItem in activeRowItemDefinitionList)
            {
                TableColumnInfo ci = new TableColumnInfo();
                TableColumnHeaderDefinition colHeader = null;
                if (_tableBody.header.columnHeaderDefinitionList.Count > 0)
                    colHeader = _tableBody.header.columnHeaderDefinitionList[col];

                if (colHeader != null)
                {
                    ci.width = colHeader.width;
                    ci.alignment = colHeader.alignment;
                    if (colHeader.label != null)
                    {
                        if (colHeader.label.text != string.Empty)
                        {
                            ci.HeaderMatchesProperty = so.Properties[colHeader.label.text] is not null;
                        }

                        ci.label = this.dataBaseInfo.db.displayResourceManagerCache.GetTextTokenString(colHeader.label);
                    }
                }

                if (ci.alignment == TextAlignment.Undefined)
                {
                    ci.alignment = rowItem.alignment;
                }

                if (ci.label == null)
                {
                    FormatToken token = null;
                    if (rowItem.formatTokenList.Count > 0)
                        token = rowItem.formatTokenList[0];
                    if (token != null)
                    {
                        if (token is FieldPropertyToken fpt)
                        {
                            ci.label = fpt.expression.expressionValue;
                        }
                        else if (token is TextToken tt)
                        {
                            ci.label = this.dataBaseInfo.db.displayResourceManagerCache.GetTextTokenString(tt);
                        }
                    }
                    else
                    {
                        ci.label = string.Empty;
                    }
                }

                thi.tableColumnInfoList.Add(ci);
                col++;
            }

            return thi;
        }

        private TableHeaderInfo GenerateTableHeaderInfoFromProperties(PSObject so)
        {
            TableHeaderInfo thi = new TableHeaderInfo();

            thi.hideHeader = this.HideHeaders;
            thi.repeatHeader = this.RepeatHeader;

            for (int k = 0; k < this.activeAssociationList.Count; k++)
            {
                MshResolvedExpressionParameterAssociation a = this.activeAssociationList[k];
                TableColumnInfo ci = new TableColumnInfo();

                // set the label of the column
                if (a.OriginatingParameter != null)
                {
                    object key = a.OriginatingParameter.GetEntry(FormatParameterDefinitionKeys.LabelEntryKey);
                    if (key != AutomationNull.Value)
                        ci.propertyName = (string)key;
                }

                ci.propertyName ??= this.activeAssociationList[k].ResolvedExpression.ToString();

                // set the width of the table
                if (a.OriginatingParameter != null)
                {
                    object key = a.OriginatingParameter.GetEntry(FormatParameterDefinitionKeys.WidthEntryKey);

                    if (key != AutomationNull.Value)
                        ci.width = (int)key;
                    else
                    {
                        ci.width = 0; // let Column Width Manager decide the width
                    }
                }
                else
                {
                    ci.width = 0; // let Column Width Manager decide the width
                }

                // set the alignment
                if (a.OriginatingParameter != null)
                {
                    object key = a.OriginatingParameter.GetEntry(FormatParameterDefinitionKeys.AlignmentEntryKey);

                    if (key != AutomationNull.Value)
                        ci.alignment = (int)key;
                    else
                        ci.alignment = ComputeDefaultAlignment(so, a.ResolvedExpression);
                }
                else
                {
                    ci.alignment = ComputeDefaultAlignment(so, a.ResolvedExpression);
                }

                thi.tableColumnInfoList.Add(ci);
            }

            return thi;
        }

        private bool HideHeaders
        {
            get
            {
                // first check command line, it takes the precedence
                if (this.parameters != null && this.parameters.shapeParameters != null)
                {
                    TableSpecificParameters tableSpecific = (TableSpecificParameters)this.parameters.shapeParameters;
                    if (tableSpecific != null && tableSpecific.hideHeaders.HasValue)
                    {
                        return tableSpecific.hideHeaders.Value;
                    }
                }
                // if we have a view, get the value out of it
                if (this.dataBaseInfo.view != null)
                {
                    return _tableBody.header.hideHeader;
                }

                return false;
            }
        }

        private bool RepeatHeaders
        {
            get
            {
                if (this.parameters != null)
                {
                    return this.parameters.repeatHeader;
                }

                return false;
            }
        }

        private static int ComputeDefaultAlignment(PSObject so, PSPropertyExpression ex)
        {
            List<PSPropertyExpressionResult> rList = ex.GetValues(so);

            if ((rList.Count == 0) || (rList[0].Exception != null))
                return TextAlignment.Left;

            object val = rList[0].Result;
            if (val == null)
                return TextAlignment.Left;

            PSObject soVal = PSObject.AsPSObject(val);
            var typeNames = soVal.InternalTypeNames;
            if (string.Equals(PSObjectHelper.PSObjectIsOfExactType(typeNames),
                                "System.String", StringComparison.OrdinalIgnoreCase))
                return TextAlignment.Left;

            if (DefaultScalarTypes.IsTypeInList(typeNames))
                return TextAlignment.Right;

            return TextAlignment.Left;
        }

        internal override FormatEntryData GeneratePayload(PSObject so, int enumerationLimit)
        {
            FormatEntryData fed = new FormatEntryData();

            TableRowEntry tre;
            if (this.dataBaseInfo.view != null)
            {
                tre = GenerateTableRowEntryFromDataBaseInfo(so, enumerationLimit);
            }
            else
            {
                tre = GenerateTableRowEntryFromFromProperties(so, enumerationLimit);
                // get the global setting for multiline
                tre.multiLine = this.dataBaseInfo.db.defaultSettingsSection.MultilineTables;
            }

            fed.formatEntryInfo = tre;

            // override from command line, if there
            if (this.parameters != null && this.parameters.shapeParameters != null)
            {
                TableSpecificParameters tableSpecific = (TableSpecificParameters)this.parameters.shapeParameters;
                if (tableSpecific != null && tableSpecific.multiLine.HasValue)
                {
                    tre.multiLine = tableSpecific.multiLine.Value;
                }
            }

            return fed;
        }

        private List<TableRowItemDefinition> GetActiveTableRowDefinition(TableControlBody tableBody, PSObject so,
                                                out bool multiLine)
        {
            multiLine = tableBody.defaultDefinition.multiLine;
            if (tableBody.optionalDefinitionList.Count == 0)
            {
                // we do not have any override, use default
                return tableBody.defaultDefinition.rowItemDefinitionList;
            }

            // see if we have an override that matches
            TableRowDefinition matchingRowDefinition = null;

            var typeNames = so.InternalTypeNames;
            TypeMatch match = new TypeMatch(expressionFactory, this.dataBaseInfo.db, typeNames);

            foreach (TableRowDefinition x in tableBody.optionalDefinitionList)
            {
                if (match.PerfectMatch(new TypeMatchItem(x, x.appliesTo)))
                {
                    matchingRowDefinition = x;
                    break;
                }
            }

            matchingRowDefinition ??= match.BestMatch as TableRowDefinition;

            if (matchingRowDefinition == null)
            {
                Collection<string> typesWithoutPrefix = Deserializer.MaskDeserializationPrefix(typeNames);
                if (typesWithoutPrefix != null)
                {
                    match = new TypeMatch(expressionFactory, this.dataBaseInfo.db, typesWithoutPrefix);

                    foreach (TableRowDefinition x in tableBody.optionalDefinitionList)
                    {
                        if (match.PerfectMatch(new TypeMatchItem(x, x.appliesTo)))
                        {
                            matchingRowDefinition = x;
                            break;
                        }
                    }

                    matchingRowDefinition ??= match.BestMatch as TableRowDefinition;
                }
            }

            if (matchingRowDefinition == null)
            {
                // no matching override, use default
                return tableBody.defaultDefinition.rowItemDefinitionList;
            }

            // the overriding row definition takes the precedence
            if (matchingRowDefinition.multiLine)
                multiLine = matchingRowDefinition.multiLine;

            // we have an override, we need to compute the merge of the active cells
            List<TableRowItemDefinition> activeRowItemDefinitionList = new List<TableRowItemDefinition>();
            int col = 0;
            foreach (TableRowItemDefinition rowItem in matchingRowDefinition.rowItemDefinitionList)
            {
                // check if the row is an override or not
                if (rowItem.formatTokenList.Count == 0)
                {
                    // it's a place holder, use the default
                    activeRowItemDefinitionList.Add(tableBody.defaultDefinition.rowItemDefinitionList[col]);
                }
                else
                {
                    // use the override
                    activeRowItemDefinitionList.Add(rowItem);
                }

                col++;
            }

            return activeRowItemDefinitionList;
        }

        private TableRowEntry GenerateTableRowEntryFromDataBaseInfo(PSObject so, int enumerationLimit)
        {
            TableRowEntry tre = new TableRowEntry();

            List<TableRowItemDefinition> activeRowItemDefinitionList = GetActiveTableRowDefinition(_tableBody, so, out tre.multiLine);
            foreach (TableRowItemDefinition rowItem in activeRowItemDefinitionList)
            {
                FormatPropertyField fpf = GenerateFormatPropertyField(rowItem.formatTokenList, so, enumerationLimit);

                // get the alignment from the row entry
                // NOTE: if it's not set, the alignment sent with the header will prevail
                fpf.alignment = rowItem.alignment;

                tre.formatPropertyFieldList.Add(fpf);
            }

            return tre;
        }

        private TableRowEntry GenerateTableRowEntryFromFromProperties(PSObject so, int enumerationLimit)
        {
            TableRowEntry tre = new TableRowEntry();
            for (int k = 0; k < this.activeAssociationList.Count; k++)
            {
                FormatPropertyField fpf = new FormatPropertyField();
                FieldFormattingDirective directive = null;
                if (activeAssociationList[k].OriginatingParameter != null)
                {
                    directive = activeAssociationList[k].OriginatingParameter.GetEntry(FormatParameterDefinitionKeys.FormatStringEntryKey) as FieldFormattingDirective;
                }

                if (directive is null)
                {
                    directive = new FieldFormattingDirective();
                    directive.isTable = true;
                }

                fpf.propertyValue = this.GetExpressionDisplayValue(so, enumerationLimit, this.activeAssociationList[k].ResolvedExpression, directive);
                tre.formatPropertyFieldList.Add(fpf);
            }

            return tre;
        }
    }
}
