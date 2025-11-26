// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands.Internal.Format
{
    internal sealed class WideViewGenerator : ViewGenerator
    {
        internal override void Initialize(TerminatingErrorContext errorContext, PSPropertyExpressionFactory expressionFactory,
                                PSObject so, TypeInfoDataBase db, FormattingCommandLineParameters parameters)
        {
            base.Initialize(errorContext, expressionFactory, so, db, parameters);
        }

        internal override FormatStartData GenerateStartData(PSObject so)
        {
            FormatStartData startFormat = base.GenerateStartData(so);

            WideViewHeaderInfo wideViewHeaderInfo = new WideViewHeaderInfo();
            startFormat.shapeInfo = wideViewHeaderInfo;

            if (!this.AutoSize)
            {
                // autosize overrides columns
                wideViewHeaderInfo.columns = this.Columns;
            }
            else
            {
                wideViewHeaderInfo.columns = 0;
            }

            return startFormat;
        }

        private int Columns
        {
            get
            {
                // check command line first
                if (parameters != null && parameters.shapeParameters != null)
                {
                    WideSpecificParameters wp = (WideSpecificParameters)parameters.shapeParameters;
                    if (wp.columns.HasValue)
                        return wp.columns.Value;
                }

                // check if the view has info
                if (this.dataBaseInfo.view != null && this.dataBaseInfo.view.mainControl != null)
                {
                    WideControlBody wideControl = (WideControlBody)this.dataBaseInfo.view.mainControl;
                    return wideControl.columns;
                }
                // not specified
                return 0;
            }
        }

        internal override FormatEntryData GeneratePayload(PSObject so, int enumerationLimit)
        {
            FormatEntryData fed = new FormatEntryData();

            if (this.dataBaseInfo.view != null)
                fed.formatEntryInfo = GenerateWideViewEntryFromDataBaseInfo(so, enumerationLimit);
            else
                fed.formatEntryInfo = GenerateWideViewEntryFromProperties(so, enumerationLimit);

            return fed;
        }

        private WideViewEntry GenerateWideViewEntryFromDataBaseInfo(PSObject so, int enumerationLimit)
        {
            WideControlBody wideBody = (WideControlBody)this.dataBaseInfo.view.mainControl;

            WideControlEntryDefinition activeWideControlEntryDefinition =
                    GetActiveWideControlEntryDefinition(wideBody, so);

            WideViewEntry wve = new WideViewEntry();
            wve.formatPropertyField = GenerateFormatPropertyField(activeWideControlEntryDefinition.formatTokenList, so, enumerationLimit);

            // wve.alignment = activeWideViewEntryDefinition.alignment;

            return wve;
        }

        private WideControlEntryDefinition GetActiveWideControlEntryDefinition(WideControlBody wideBody, PSObject so)
        {
            // see if we have an override that matches
            var typeNames = so.InternalTypeNames;
            TypeMatch match = new TypeMatch(expressionFactory, this.dataBaseInfo.db, typeNames);
            foreach (WideControlEntryDefinition x in wideBody.optionalEntryList)
            {
                if (match.PerfectMatch(new TypeMatchItem(x, x.appliesTo)))
                {
                    return x;
                }
            }

            if (match.BestMatch != null)
            {
                return match.BestMatch as WideControlEntryDefinition;
            }
            else
            {
                Collection<string> typesWithoutPrefix = Deserializer.MaskDeserializationPrefix(typeNames);
                if (typesWithoutPrefix != null)
                {
                    match = new TypeMatch(expressionFactory, this.dataBaseInfo.db, typesWithoutPrefix);
                    foreach (WideControlEntryDefinition x in wideBody.optionalEntryList)
                    {
                        if (match.PerfectMatch(new TypeMatchItem(x, x.appliesTo)))
                        {
                            return x;
                        }
                    }

                    if (match.BestMatch != null)
                    {
                        return match.BestMatch as WideControlEntryDefinition;
                    }
                }

                // we do not have any override, use default
                return wideBody.defaultEntryDefinition;
            }
        }

        private WideViewEntry GenerateWideViewEntryFromProperties(PSObject so, int enumerationLimit)
        {
            // compute active properties every time
            if (this.activeAssociationList == null)
            {
                SetUpActiveProperty(so);
            }

            WideViewEntry wve = new WideViewEntry();
            FormatPropertyField fpf = new FormatPropertyField();

            wve.formatPropertyField = fpf;
            if (this.activeAssociationList.Count > 0)
            {
                // get the first one
                MshResolvedExpressionParameterAssociation a = this.activeAssociationList[0];
                FieldFormattingDirective directive = null;
                if (a.OriginatingParameter != null)
                {
                    directive = a.OriginatingParameter.GetEntry(FormatParameterDefinitionKeys.FormatStringEntryKey) as FieldFormattingDirective;
                }

                fpf.propertyValue = this.GetExpressionDisplayValue(so, enumerationLimit, a.ResolvedExpression, directive);
            }

            this.activeAssociationList = null;
            return wve;
        }

        private void SetUpActiveProperty(PSObject so)
        {
            List<MshParameter> rawMshParameterList = null;

            if (this.parameters != null)
                rawMshParameterList = this.parameters.mshParameterList;

            // check if we received properties from the command line
            if (rawMshParameterList != null && rawMshParameterList.Count > 0)
            {
                this.activeAssociationList = AssociationManager.ExpandParameters(rawMshParameterList, so);
                ApplyExcludePropertyFilter();
                return;
            }

            // we did not get any properties:
            // try to get the display property of the object
            PSPropertyExpression displayNameExpression = PSObjectHelper.GetDisplayNameExpression(so, this.expressionFactory);
            if (displayNameExpression != null)
            {
                this.activeAssociationList = new List<MshResolvedExpressionParameterAssociation>();
                this.activeAssociationList.Add(new MshResolvedExpressionParameterAssociation(null, displayNameExpression));
                ApplyExcludePropertyFilter();
                return;
            }

            // try to get the default property set (we will use the first property)
            this.activeAssociationList = AssociationManager.ExpandDefaultPropertySet(so, this.expressionFactory);
            if (this.activeAssociationList.Count > 0)
            {
                // we got a valid set of properties from the default property set
                ApplyExcludePropertyFilter();
                return;
            }

            // we failed to get anything from the default property set
            // just get all the properties
            this.activeAssociationList = AssociationManager.ExpandAll(so);
            ApplyExcludePropertyFilter();
        }
    }
}
