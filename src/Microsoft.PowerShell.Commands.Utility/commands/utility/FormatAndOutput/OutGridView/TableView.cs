// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Internal;

using Microsoft.PowerShell.Commands.Internal.Format;

namespace Microsoft.PowerShell.Commands
{
    internal class TableView
    {
        private PSPropertyExpressionFactory _expressionFactory;
        private TypeInfoDataBase _typeInfoDatabase;
        private FormatErrorManager _errorManager;

        internal void Initialize(PSPropertyExpressionFactory expressionFactory,
                                 TypeInfoDataBase db)
        {
            _expressionFactory = expressionFactory;
            _typeInfoDatabase = db;

            // Initialize Format Error Manager.
            FormatErrorPolicy formatErrorPolicy = new();

            formatErrorPolicy.ShowErrorsAsMessages = _typeInfoDatabase.defaultSettingsSection.formatErrorPolicy.ShowErrorsAsMessages;
            formatErrorPolicy.ShowErrorsInFormattedOutput = _typeInfoDatabase.defaultSettingsSection.formatErrorPolicy.ShowErrorsInFormattedOutput;

            _errorManager = new FormatErrorManager(formatErrorPolicy);
        }

        internal HeaderInfo GenerateHeaderInfo(PSObject input, TableControlBody tableBody, OutGridViewCommand parentCmdlet)
        {
            HeaderInfo headerInfo = new();

            // This verification is needed because the database returns "LastWriteTime" value for file system objects
            // as strings and it is used to detect this situation and use the actual field value.
            bool fileSystemObject = typeof(FileSystemInfo).IsInstanceOfType(input.BaseObject);

            if (tableBody != null) // If the tableBody is null, the TableControlBody info was not put into the database.
            {
                // Generate HeaderInfo from the type information database.
                List<TableRowItemDefinition> activeRowItemDefinitionList = GetActiveTableRowDefinition(tableBody, input);

                int col = 0;
                foreach (TableRowItemDefinition rowItem in activeRowItemDefinitionList)
                {
                    ColumnInfo columnInfo = null;
                    string displayName = null;
                    TableColumnHeaderDefinition colHeader = null;
                    // Retrieve a matching TableColumnHeaderDefinition
                    if (col < tableBody.header.columnHeaderDefinitionList.Count)
                        colHeader = tableBody.header.columnHeaderDefinitionList[col];

                    if (colHeader != null && colHeader.label != null)
                    {
                        displayName = _typeInfoDatabase.displayResourceManagerCache.GetTextTokenString(colHeader.label);
                    }

                    FormatToken token = null;
                    if (rowItem.formatTokenList.Count > 0)
                    {
                        token = rowItem.formatTokenList[0];
                    }

                    if (token != null)
                    {
                        FieldPropertyToken fpt = token as FieldPropertyToken;
                        if (fpt != null)
                        {
                            // If Database does not provide a label(DisplayName) for the current property, use the expression value instead.
                            displayName ??= fpt.expression.expressionValue;

                            if (fpt.expression.isScriptBlock)
                            {
                                PSPropertyExpression ex = _expressionFactory.CreateFromExpressionToken(fpt.expression);
                                // Using the displayName as a propertyName for a stale PSObject.
                                const string LastWriteTimePropertyName = "LastWriteTime";

                                // For FileSystem objects "LastWriteTime" property value should be used although the database indicates that a script should be executed to get the value.
                                if (fileSystemObject && displayName.Equals(LastWriteTimePropertyName, StringComparison.OrdinalIgnoreCase))
                                {
                                    columnInfo = new OriginalColumnInfo(displayName, displayName, LastWriteTimePropertyName, parentCmdlet);
                                }
                                else
                                {
                                    columnInfo = new ExpressionColumnInfo(displayName, displayName, ex);
                                }
                            }
                            else
                            {
                                columnInfo = new OriginalColumnInfo(fpt.expression.expressionValue, displayName, fpt.expression.expressionValue, parentCmdlet);
                            }
                        }
                        else
                        {
                            TextToken tt = token as TextToken;
                            if (tt != null)
                            {
                                displayName = _typeInfoDatabase.displayResourceManagerCache.GetTextTokenString(tt);
                                columnInfo = new OriginalColumnInfo(tt.text, displayName, tt.text, parentCmdlet);
                            }
                        }
                    }

                    if (columnInfo != null)
                    {
                        headerInfo.AddColumn(columnInfo);
                    }

                    col++;
                }
            }

            return headerInfo;
        }

        internal HeaderInfo GenerateHeaderInfo(PSObject input, OutGridViewCommand parentCmdlet)
        {
            HeaderInfo headerInfo = new();
            List<MshResolvedExpressionParameterAssociation> activeAssociationList;

            // Get properties from the default property set of the object
            activeAssociationList = AssociationManager.ExpandDefaultPropertySet(input, _expressionFactory);
            if (activeAssociationList.Count > 0)
            {
                // we got a valid set of properties from the default property set..add computername for
                // remoteobjects (if available)
                if (PSObjectHelper.ShouldShowComputerNameProperty(input))
                {
                    activeAssociationList.Add(new MshResolvedExpressionParameterAssociation(null,
                        new PSPropertyExpression(RemotingConstants.ComputerNameNoteProperty)));
                }
            }
            else
            {
                // We failed to get anything from the default property set
                activeAssociationList = AssociationManager.ExpandAll(input);
                if (activeAssociationList.Count > 0)
                {
                    // Remove PSComputerName and PSShowComputerName from the display as needed.
                    AssociationManager.HandleComputerNameProperties(input, activeAssociationList);
                    FilterActiveAssociationList(activeAssociationList);
                }
                else
                {
                    // We were unable to retrieve any properties, so we leave an empty list
                    activeAssociationList = new List<MshResolvedExpressionParameterAssociation>();
                }
            }

            for (int k = 0; k < activeAssociationList.Count; k++)
            {
                string propertyName = null;

                MshResolvedExpressionParameterAssociation association = activeAssociationList[k];

                // set the label of the column
                if (association.OriginatingParameter != null)
                {
                    object key = association.OriginatingParameter.GetEntry(FormatParameterDefinitionKeys.LabelEntryKey);
                    if (key != AutomationNull.Value)
                        propertyName = (string)key;
                }

                propertyName ??= association.ResolvedExpression.ToString();

                ColumnInfo columnInfo = new OriginalColumnInfo(propertyName, propertyName, propertyName, parentCmdlet);

                headerInfo.AddColumn(columnInfo);
            }

            return headerInfo;
        }

        /// <summary>
        /// Method to filter resolved expressions as per table view needs.
        /// For v1.0, table view supports only 10 properties.
        ///
        /// This method filters and updates "activeAssociationList" instance property.
        /// </summary>
        /// <returns>None.</returns>
        /// <remarks>This method updates "activeAssociationList" instance property.</remarks>
        private static void FilterActiveAssociationList(List<MshResolvedExpressionParameterAssociation> activeAssociationList)
        {
            // we got a valid set of properties from the default property set
            // make sure we do not have too many properties

            // NOTE: this is an arbitrary number, chosen to be a sensitive default
            const int nMax = 256;

            if (activeAssociationList.Count > nMax)
            {
                List<MshResolvedExpressionParameterAssociation> tmp = new(activeAssociationList);
                activeAssociationList.Clear();
                for (int k = 0; k < nMax; k++)
                    activeAssociationList.Add(tmp[k]);
            }

            return;
        }

        private List<TableRowItemDefinition> GetActiveTableRowDefinition(TableControlBody tableBody, PSObject so)
        {
            if (tableBody.optionalDefinitionList.Count == 0)
            {
                // we do not have any override, use default
                return tableBody.defaultDefinition.rowItemDefinitionList;
            }

            // see if we have an override that matches
            TableRowDefinition matchingRowDefinition = null;

            var typeNames = so.InternalTypeNames;
            TypeMatch match = new(_expressionFactory, _typeInfoDatabase, typeNames);

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
                    match = new TypeMatch(_expressionFactory, _typeInfoDatabase, typesWithoutPrefix);

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

            // we have an override, we need to compute the merge of the active cells
            List<TableRowItemDefinition> activeRowItemDefinitionList = new();
            int col = 0;
            foreach (TableRowItemDefinition rowItem in matchingRowDefinition.rowItemDefinitionList)
            {
                // Check if the row is an override or not
                if (rowItem.formatTokenList.Count == 0)
                {
                    // It's a place holder, use the default
                    activeRowItemDefinitionList.Add(tableBody.defaultDefinition.rowItemDefinitionList[col]);
                }
                else
                {
                    // Use the override
                    activeRowItemDefinitionList.Add(rowItem);
                }

                col++;
            }

            return activeRowItemDefinitionList;
        }
    }
}
