
/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Globalization;

namespace Microsoft.PowerShell.Commands.Internal.Format
{
    /// <summary>
    /// base class for the various types of formatting shapes
    /// </summary>
    internal abstract class ViewGenerator
    {

        internal virtual void Initialize (TerminatingErrorContext terminatingErrorContext, 
                                        MshExpressionFactory mshExpressionFactory,
                                        TypeInfoDataBase db,
                                        ViewDefinition view,
                                        FormattingCommandLineParameters formatParameters)
        {
            Diagnostics.Assert (mshExpressionFactory != null, "mshExpressionFactory cannot be null");
            Diagnostics.Assert (db != null, "db cannot be null");
            Diagnostics.Assert (view != null, "view cannot be null");

            errorContext = terminatingErrorContext;
            expressionFactory = mshExpressionFactory;
            parameters = formatParameters;

            dataBaseInfo.db = db;
            dataBaseInfo.view = view;
            dataBaseInfo.applicableTypes = DisplayDataQuery.GetAllApplicableTypes (db, view.appliesTo);

            InitializeHelper();
        }

        internal virtual void Initialize(TerminatingErrorContext terminatingErrorContext,
                                            MshExpressionFactory mshExpressionFactory,
                                            PSObject so, 
                                            TypeInfoDataBase db,
                                            FormattingCommandLineParameters formatParameters)
        {
            errorContext = terminatingErrorContext;
            expressionFactory = mshExpressionFactory;
            parameters = formatParameters;
            dataBaseInfo.db = db;

            InitializeHelper();
        }

        /// <summary>
        /// Let the view prepare itself for RemoteObjects. Specific view generators can
        /// use this call to customize display for remote objects like showing/hiding
        /// computername property etc.
        /// </summary>
        /// <param name="so"></param>
        internal virtual void PrepareForRemoteObjects(PSObject so)
        {
        }

        private void InitializeHelper()
        {
            InitializeFormatErrorManager();
            InitializeGroupBy();
            InitializeAutoSize();
        }

        private void InitializeFormatErrorManager()
        {
            FormatErrorPolicy formatErrorPolicy = new FormatErrorPolicy();
            if (parameters != null && parameters.showErrorsAsMessages.HasValue)
            {
                formatErrorPolicy.ShowErrorsAsMessages = parameters.showErrorsAsMessages.Value;
            }
            else
            {
                formatErrorPolicy.ShowErrorsAsMessages = this.dataBaseInfo.db.defaultSettingsSection.formatErrorPolicy.ShowErrorsAsMessages;
            }
            if (parameters != null && parameters.showErrorsInFormattedOutput.HasValue)
            {
                formatErrorPolicy.ShowErrorsInFormattedOutput = parameters.showErrorsInFormattedOutput.Value;
            }
            else
            {
                formatErrorPolicy.ShowErrorsInFormattedOutput = this.dataBaseInfo.db.defaultSettingsSection.formatErrorPolicy.ShowErrorsInFormattedOutput;
            }

            this.errorManager = new FormatErrorManager(formatErrorPolicy);
        }

        private void InitializeGroupBy ()
        {
            // first check if there is an override from the command line
            if (parameters != null && parameters.groupByParameter != null)
            {
                // get the epxpression to use
                MshExpression groupingKeyExpression = parameters.groupByParameter.GetEntry (FormatParameterDefinitionKeys.ExpressionEntryKey) as MshExpression;

                // set the label
                string label = null;
                object labelKey = parameters.groupByParameter.GetEntry (FormatParameterDefinitionKeys.LabelEntryKey);
                if (labelKey != AutomationNull.Value)
                {
                    label = labelKey as string;
                }
                this.groupingManager = new GroupingInfoManager ();
                this.groupingManager.Initialize (groupingKeyExpression, label);
                return;
            }

            // check if we have a view to initialize from
            if (this.dataBaseInfo.view != null)
            {
                GroupBy gb = this.dataBaseInfo.view.groupBy;
                if (gb == null)
                {
                    return;
                }
                if (gb.startGroup == null || gb.startGroup.expression == null)
                {
                    return;
                }

                MshExpression ex = this.expressionFactory.CreateFromExpressionToken (gb.startGroup.expression, this.dataBaseInfo.view.loadingInfo);

                this.groupingManager = new GroupingInfoManager ();
                this.groupingManager.Initialize (ex, null);
            }
        }

        private void InitializeAutoSize ()
        {
            // check the autosize flag first
            if (parameters != null && parameters.autosize.HasValue)
            {
                this.autosize = parameters.autosize.Value;
                return;
            }
            // check if we have a view with autosize checked
            if (this.dataBaseInfo.view != null && this.dataBaseInfo.view.mainControl != null)
            {
                ControlBody controlBody = this.dataBaseInfo.view.mainControl as ControlBody;
                if (controlBody != null && controlBody.autosize.HasValue)
                {
                    this.autosize = controlBody.autosize.Value;
                }
            }
        }

        internal virtual FormatStartData GenerateStartData (PSObject so)
        {
            FormatStartData startFormat = new FormatStartData ();

            if (this.autosize)
            {
                startFormat.autosizeInfo = new AutosizeInfo ();
            }
            return startFormat;
        }

        abstract internal FormatEntryData GeneratePayload (PSObject so, int enumerationLimit);

        internal GroupStartData GenerateGroupStartData (PSObject firstObjectInGroup, int enumerationLimit)
        {
            GroupStartData startGroup = new GroupStartData ();
            if (this.groupingManager == null)
                return startGroup;

            object currentGroupingValue = this.groupingManager.CurrentGroupingKeyPropertyValue;

            if (currentGroupingValue == AutomationNull.Value)
                return startGroup;

            PSObject so = PSObjectHelper.AsPSObject (currentGroupingValue);

            // we need to determine how to display the group header
            ControlBase control = null;
            TextToken labelTextToken = null;
            if (this.dataBaseInfo.view != null && this.dataBaseInfo.view.groupBy != null)
            {
                if (this.dataBaseInfo.view.groupBy.startGroup != null)
                {
                    // NOTE: from the database constraints, only one of the
                    // two will be non null
                    control = this.dataBaseInfo.view.groupBy.startGroup.control;
                    labelTextToken = this.dataBaseInfo.view.groupBy.startGroup.labelTextToken;
                }
            }

            startGroup.groupingEntry = new GroupingEntry ();

            if (control == null)
            {
                // we do not have a control, we auto generate a
                // snippet of complex display using a label

                StringFormatError formatErrorObject = null;
                if (this.errorManager.DisplayFormatErrorString)
                {
                    // we send a format error object down to the formatting calls
                    // only if we want to show the formatting error strings
                    formatErrorObject = new StringFormatError();
                }

                string currentGroupingValueDisplay = PSObjectHelper.SmartToString (so, this.expressionFactory, enumerationLimit, formatErrorObject);

                if (formatErrorObject != null && formatErrorObject.exception != null)
                {
                    // if we did no thave any errors in the expression evaluation
                    // we might have errors in the formatting, if present
                    this.errorManager.LogStringFormatError(formatErrorObject);
                    if (this.errorManager.DisplayFormatErrorString)
                    {
                        currentGroupingValueDisplay = this.errorManager.FormatErrorString;
                    }
                }


                FormatEntry fe = new FormatEntry ();
                startGroup.groupingEntry.formatValueList.Add (fe);

                FormatTextField ftf = new FormatTextField ();

                // determine what the label should be. If we have a label from the
                // database, let's use it, else fall back to the string provided
                // by the grouping manager
                string label;
                if (labelTextToken != null)
                    label = this.dataBaseInfo.db.displayResourceManagerCache.GetTextTokenString (labelTextToken);
                else
                    label = this.groupingManager.GroupingKeyDisplayName;

                ftf.text = StringUtil.Format(FormatAndOut_format_xxx.GroupStartDataIndentedAutoGeneratedLabel, label);

                fe.formatValueList.Add (ftf);

                FormatPropertyField fpf = new FormatPropertyField ();

                fpf.propertyValue = currentGroupingValueDisplay;
                fe.formatValueList.Add (fpf);
            }
            else
            {
                // NOTE: we set a max depth to protect ourselves from infinite loops
                const int maxTreeDepth = 50;
                ComplexControlGenerator controlGenerator =
                                new ComplexControlGenerator (this.dataBaseInfo.db,
                                        this.dataBaseInfo.view.loadingInfo,
                                        this.expressionFactory,
                                        this.dataBaseInfo.view.formatControlDefinitionHolder.controlDefinitionList,
                                        this.ErrorManager,
                                        enumerationLimit,
                                        this.errorContext);

                controlGenerator.GenerateFormatEntries (maxTreeDepth,
                    control, firstObjectInGroup, startGroup.groupingEntry.formatValueList);

            }
            return startGroup;
        }

        /// <summary>
        /// update the current value of the grouping key
        /// </summary>
        /// <param name="so">object to use for the update</param>
        /// <returns>true if the value of the key changed</returns>
        internal bool UpdateGroupingKeyValue (PSObject so)
        {
            if (this.groupingManager == null)
                return false;
            return this.groupingManager.UpdateGroupingKeyValue (so);
        }


        internal GroupEndData GenerateGroupEndData ()
        {
            return new GroupEndData ();
        }

        internal bool IsObjectApplicable (Collection<string> typeNames)
        {
            if (dataBaseInfo.view == null)
                return true;

            if (typeNames.Count == 0)
                return false;

            TypeMatch match = new TypeMatch (expressionFactory, dataBaseInfo.db, typeNames);
            if (match.PerfectMatch (new TypeMatchItem (this, dataBaseInfo.applicableTypes)))
            {
                return true;
            }

            bool result = match.BestMatch != null;

            // we were unable to find a best match so far..try
            // to get rid of Deserialization prefix and see if a
            // match can be found.
            if (false == result)
            {
                Collection<string> typesWithoutPrefix = Deserializer.MaskDeserializationPrefix(typeNames);
                if (null != typesWithoutPrefix)
                {
                    result = IsObjectApplicable(typesWithoutPrefix);
                }
            }
            return result;
        }


        private GroupingInfoManager groupingManager = null;

        protected bool AutoSize
        {
            get { return this.autosize; }
        }
        private bool autosize = false;

        protected class DataBaseInfo
        {
            internal TypeInfoDataBase db = null;
            internal ViewDefinition view = null;
            internal AppliesTo applicableTypes = null;
        }

        protected TerminatingErrorContext errorContext;

        protected FormattingCommandLineParameters parameters;

        protected MshExpressionFactory expressionFactory;

        protected DataBaseInfo dataBaseInfo = new DataBaseInfo();

        protected List<MshResolvedExpressionParameterAssociation> activeAssociationList = null;
        protected FormattingCommandLineParameters inputParameters = null;

        protected string GetExpressionDisplayValue (PSObject so, int enumerationLimit, MshExpression ex,
                    FieldFormattingDirective directive)
        {
            MshExpressionResult resolvedExpression;
            return GetExpressionDisplayValue (so, enumerationLimit, ex, directive, out resolvedExpression);
        }

        protected string GetExpressionDisplayValue (PSObject so, int enumerationLimit, MshExpression ex,
                    FieldFormattingDirective directive, out MshExpressionResult expressionResult)
        {
            StringFormatError formatErrorObject = null;
            if (this.errorManager.DisplayFormatErrorString)
            {
                // we send a format error object down to the formatting calls
                // only if we want to show the formatting error strings
                formatErrorObject = new StringFormatError ();
            }

            string retVal = PSObjectHelper.GetExpressionDisplayValue (so, enumerationLimit, ex,
                                directive, formatErrorObject, expressionFactory, out expressionResult);

            if (expressionResult != null)
            {
                // we obtained a result, check if there is an error
                if (expressionResult.Exception != null)
                {
                    this.errorManager.LogMshExpressionFailedResult (expressionResult, so);
                    if (this.errorManager.DisplayErrorStrings)
                    {
                        retVal = this.errorManager.ErrorString;
                    }
                }
                else if (formatErrorObject != null && formatErrorObject.exception != null)
                {
                    // if we did no thave any errors in the expression evaluation
                    // we might have errors in the formatting, if present
                    this.errorManager.LogStringFormatError (formatErrorObject);
                    if (this.errorManager.DisplayErrorStrings)
                    {
                        retVal = this.errorManager.FormatErrorString;
                    }
                }
            }
            return retVal;
        }

        protected bool EvaluateDisplayCondition (PSObject so, ExpressionToken conditionToken)
        {
            if (conditionToken == null)
                return true;

            MshExpression ex = this.expressionFactory.CreateFromExpressionToken (conditionToken, this.dataBaseInfo.view.loadingInfo);
            MshExpressionResult expressionResult;
            bool retVal = DisplayCondition.Evaluate (so, ex, out expressionResult);

            if (expressionResult != null && expressionResult.Exception != null)
            {
                this.errorManager.LogMshExpressionFailedResult (expressionResult, so);
            }
            return retVal;
        }


        internal FormatErrorManager ErrorManager
        {
            get { return this.errorManager; }
        }

        private FormatErrorManager errorManager;


        #region helpers

        protected FormatPropertyField GenerateFormatPropertyField (List<FormatToken> formatTokenList, PSObject so, int enumerationLimit)
        {
            MshExpressionResult result;
            return GenerateFormatPropertyField (formatTokenList, so, enumerationLimit, out result);
        }

        protected FormatPropertyField GenerateFormatPropertyField (List<FormatToken> formatTokenList, PSObject so, int enumerationLimit, out MshExpressionResult result)
        {
            result = null;
            FormatPropertyField fpf = new FormatPropertyField ();
            if (formatTokenList.Count != 0)
            {
                FormatToken token = formatTokenList[0];
                FieldPropertyToken fpt = token as FieldPropertyToken;
                if (fpt != null)
                {
                    MshExpression ex = this.expressionFactory.CreateFromExpressionToken(fpt.expression, this.dataBaseInfo.view.loadingInfo);
                    fpf.propertyValue = this.GetExpressionDisplayValue (so, enumerationLimit, ex, fpt.fieldFormattingDirective, out result);
                }
                else
                {
                    TextToken tt = token as TextToken;
                    if (tt != null)
                    fpf.propertyValue = this.dataBaseInfo.db.displayResourceManagerCache.GetTextTokenString (tt);
                }
            }
            else
            {
                fpf.propertyValue = "";
            }
            return fpf;
        }

        #endregion

    }

}
