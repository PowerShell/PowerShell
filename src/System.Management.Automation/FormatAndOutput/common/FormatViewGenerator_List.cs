// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Internal;

namespace Microsoft.PowerShell.Commands.Internal.Format
{
    internal sealed class ListViewGenerator : ViewGenerator
    {
        // tableBody to use for this instance of the ViewGenerator;
        private ListControlBody _listBody;

        internal override void Initialize(TerminatingErrorContext terminatingErrorContext, PSPropertyExpressionFactory mshExpressionFactory, TypeInfoDataBase db, ViewDefinition view, FormattingCommandLineParameters formatParameters)
        {
            base.Initialize(terminatingErrorContext, mshExpressionFactory, db, view, formatParameters);
            if ((this.dataBaseInfo != null) && (this.dataBaseInfo.view != null))
            {
                _listBody = (ListControlBody)this.dataBaseInfo.view.mainControl;
            }
        }

        internal override void Initialize(TerminatingErrorContext errorContext, PSPropertyExpressionFactory expressionFactory,
                                    PSObject so, TypeInfoDataBase db, FormattingCommandLineParameters parameters)
        {
            base.Initialize(errorContext, expressionFactory, so, db, parameters);
            if ((this.dataBaseInfo != null) && (this.dataBaseInfo.view != null))
            {
                _listBody = (ListControlBody)this.dataBaseInfo.view.mainControl;
            }

            this.inputParameters = parameters;
            SetUpActiveProperties(so);
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
                _listBody = (ListControlBody)this.dataBaseInfo.view.mainControl.Copy();
                // build up the definition for computer name.
                ListControlItemDefinition cnListItemDefinition = new ListControlItemDefinition();
                cnListItemDefinition.label = new TextToken();
                cnListItemDefinition.label.text = RemotingConstants.ComputerNameNoteProperty;
                FieldPropertyToken fpt = new FieldPropertyToken();
                fpt.expression = new ExpressionToken(RemotingConstants.ComputerNameNoteProperty, false);
                cnListItemDefinition.formatTokenList.Add(fpt);

                _listBody.defaultEntryDefinition.itemDefinitionList.Add(cnListItemDefinition);
            }
        }

        internal override FormatStartData GenerateStartData(PSObject so)
        {
            FormatStartData startFormat = base.GenerateStartData(so);
            startFormat.shapeInfo = new ListViewHeaderInfo();
            return startFormat;
        }

        internal override FormatEntryData GeneratePayload(PSObject so, int enumerationLimit)
        {
            FormatEntryData fed = new FormatEntryData();

            if (this.dataBaseInfo.view != null)
                fed.formatEntryInfo = GenerateListViewEntryFromDataBaseInfo(so, enumerationLimit);
            else
                fed.formatEntryInfo = GenerateListViewEntryFromProperties(so, enumerationLimit);
            return fed;
        }

        private ListViewEntry GenerateListViewEntryFromDataBaseInfo(PSObject so, int enumerationLimit)
        {
            ListViewEntry lve = new ListViewEntry();

            ListControlEntryDefinition activeListControlEntryDefinition =
                GetActiveListControlEntryDefinition(_listBody, so);

            foreach (ListControlItemDefinition listItem in activeListControlEntryDefinition.itemDefinitionList)
            {
                if (!EvaluateDisplayCondition(so, listItem.conditionToken))
                    continue;

                ListViewField lvf = new ListViewField();
                PSPropertyExpressionResult result;
                lvf.formatPropertyField = GenerateFormatPropertyField(listItem.formatTokenList, so, enumerationLimit, out result);

                // we need now to provide a label
                if (listItem.label != null)
                {
                    // if the directive provides one, we use it
                    lvf.label = this.dataBaseInfo.db.displayResourceManagerCache.GetTextTokenString(listItem.label);
                }
                else if (result != null)
                {
                    // if we got a valid match from the Mshexpression, use it as a label
                    lvf.label = result.ResolvedExpression.ToString();
                }
                else
                {
                    // we did fail getting a result (i.e. property does not exist on the object)

                    // we try to fall back and see if we have an un-resolved PSPropertyExpression
                    FormatToken token = listItem.formatTokenList[0];
                    FieldPropertyToken fpt = token as FieldPropertyToken;
                    if (fpt != null)
                    {
                        PSPropertyExpression ex = this.expressionFactory.CreateFromExpressionToken(fpt.expression, this.dataBaseInfo.view.loadingInfo);

                        // use the un-resolved PSPropertyExpression string as a label
                        lvf.label = ex.ToString();
                    }
                    else
                    {
                        TextToken tt = token as TextToken;
                        if (tt != null)
                            // we had a text token, use it as a label (last resort...)
                            lvf.label = this.dataBaseInfo.db.displayResourceManagerCache.GetTextTokenString(tt);
                    }
                }

                lve.listViewFieldList.Add(lvf);
            }

            return lve;
        }

        private ListControlEntryDefinition GetActiveListControlEntryDefinition(ListControlBody listBody, PSObject so)
        {
            // see if we have an override that matches
            var typeNames = so.InternalTypeNames;
            TypeMatch match = new TypeMatch(expressionFactory, this.dataBaseInfo.db, typeNames);
            foreach (ListControlEntryDefinition x in listBody.optionalEntryList)
            {
                if (match.PerfectMatch(new TypeMatchItem(x, x.appliesTo, so)))
                {
                    return x;
                }
            }

            if (match.BestMatch != null)
            {
                return match.BestMatch as ListControlEntryDefinition;
            }
            else
            {
                Collection<string> typesWithoutPrefix = Deserializer.MaskDeserializationPrefix(typeNames);
                if (typesWithoutPrefix != null)
                {
                    match = new TypeMatch(expressionFactory, this.dataBaseInfo.db, typesWithoutPrefix);
                    foreach (ListControlEntryDefinition x in listBody.optionalEntryList)
                    {
                        if (match.PerfectMatch(new TypeMatchItem(x, x.appliesTo)))
                        {
                            return x;
                        }
                    }

                    if (match.BestMatch != null)
                    {
                        return match.BestMatch as ListControlEntryDefinition;
                    }
                }

                // we do not have any override, use default
                return listBody.defaultEntryDefinition;
            }
        }

        private ListViewEntry GenerateListViewEntryFromProperties(PSObject so, int enumerationLimit)
        {
            // compute active properties every time
            if (this.activeAssociationList == null)
            {
                SetUpActiveProperties(so);
            }

            ListViewEntry lve = new ListViewEntry();

            for (int k = 0; k < this.activeAssociationList.Count; k++)
            {
                MshResolvedExpressionParameterAssociation a = this.activeAssociationList[k];
                ListViewField lvf = new ListViewField();

                if (a.OriginatingParameter != null)
                {
                    object key = a.OriginatingParameter.GetEntry(FormatParameterDefinitionKeys.LabelEntryKey);

                    if (key != AutomationNull.Value)
                    {
                        lvf.propertyName = (string)key;
                    }
                    else
                    {
                        lvf.propertyName = a.ResolvedExpression.ToString();
                    }
                }
                else
                {
                    lvf.propertyName = a.ResolvedExpression.ToString();
                }

                FieldFormattingDirective directive = null;
                if (a.OriginatingParameter != null)
                {
                    directive = a.OriginatingParameter.GetEntry(FormatParameterDefinitionKeys.FormatStringEntryKey) as FieldFormattingDirective;
                }

                lvf.formatPropertyField.propertyValue = this.GetExpressionDisplayValue(so, enumerationLimit, a.ResolvedExpression, directive);
                lve.listViewFieldList.Add(lvf);
            }

            this.activeAssociationList = null;
            return lve;
        }

        private void SetUpActiveProperties(PSObject so)
        {
            List<MshParameter> mshParameterList = null;

            if (this.inputParameters != null)
                mshParameterList = this.inputParameters.mshParameterList;

            this.activeAssociationList = AssociationManager.SetupActiveProperties(mshParameterList, so, this.expressionFactory);
        }
    }
}
