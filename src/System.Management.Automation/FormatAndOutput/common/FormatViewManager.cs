// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Text;

using Microsoft.PowerShell.Commands;

namespace Microsoft.PowerShell.Commands.Internal.Format
{
    internal static class DefaultScalarTypes
    {
        internal static bool IsTypeInList(Collection<string> typeNames)
        {
            // NOTE: we do not use inheritance here, since we deal with
            // value types or with types where inheritance is not a factor for the selection
            string typeName = PSObjectHelper.PSObjectIsOfExactType(typeNames);

            if (string.IsNullOrEmpty(typeName))
                return false;

            string originalTypeName = Deserializer.MaskDeserializationPrefix(typeName);

            if (string.IsNullOrEmpty(originalTypeName))
                return false;

            // check if the type is derived from a System.Enum
            // e.g. in C#
            // enum Foo { Red, Black, Green}

            if (PSObjectHelper.PSObjectIsEnum(typeNames))
                return true;

            return s_defaultScalarTypesHash.Contains(originalTypeName);
        }

        static DefaultScalarTypes()
        {
            s_defaultScalarTypesHash.Add("System.String");
            s_defaultScalarTypesHash.Add("System.SByte");
            s_defaultScalarTypesHash.Add("System.Byte");
            s_defaultScalarTypesHash.Add("System.Int16");
            s_defaultScalarTypesHash.Add("System.UInt16");
            s_defaultScalarTypesHash.Add("System.Int32");
            s_defaultScalarTypesHash.Add("System.UInt32");
            s_defaultScalarTypesHash.Add("System.Int64");
            s_defaultScalarTypesHash.Add("System.UInt64");
            s_defaultScalarTypesHash.Add("System.Char");
            s_defaultScalarTypesHash.Add("System.Single");
            s_defaultScalarTypesHash.Add("System.Double");
            s_defaultScalarTypesHash.Add("System.Boolean");
            s_defaultScalarTypesHash.Add("System.Decimal");
            s_defaultScalarTypesHash.Add("System.IntPtr");
            s_defaultScalarTypesHash.Add("System.Security.SecureString");
            s_defaultScalarTypesHash.Add("System.Numerics.BigInteger");
        }

        private static readonly HashSet<string> s_defaultScalarTypesHash = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Class to manage the selection of a desired view type and
    /// manage state associated to the selected view.
    /// </summary>
    internal sealed class FormatViewManager
    {
        #region tracer
        [TraceSource("FormatViewBinding", "Format view binding")]
        private static readonly PSTraceSource s_formatViewBindingTracer = PSTraceSource.GetTracer("FormatViewBinding", "Format view binding", false);
        #endregion tracer

        private static string PSObjectTypeName(PSObject so)
        {
            // if so is not null, its TypeNames will not be null
            if (so != null)
            {
                var typeNames = so.InternalTypeNames;
                if (typeNames.Count > 0)
                {
                    return typeNames[0];
                }
            }

            return string.Empty;
        }

        internal void Initialize(TerminatingErrorContext errorContext,
                                    PSPropertyExpressionFactory expressionFactory,
                                    TypeInfoDataBase db,
                                    PSObject so,
                                    FormatShape shape,
                                    FormattingCommandLineParameters parameters)
        {
            ViewDefinition view = null;
            const string findViewType = "FINDING VIEW TYPE: {0}";
            const string findViewShapeType = "FINDING VIEW {0} TYPE: {1}";
            const string findViewNameType = "FINDING VIEW NAME: {0} TYPE: {1}";
            const string viewFound = "An applicable view has been found";
            const string viewNotFound = "No applicable view has been found";
            try
            {
                DisplayDataQuery.SetTracer(s_formatViewBindingTracer);

                // shape not specified: we need to select one
                var typeNames = so.InternalTypeNames;
                if (shape == FormatShape.Undefined)
                {
                    using (s_formatViewBindingTracer.TraceScope(findViewType, PSObjectTypeName(so)))
                    {
                        view = DisplayDataQuery.GetViewByShapeAndType(expressionFactory, db, shape, typeNames, null);
                    }

                    if (view != null)
                    {
                        // we got a matching view from the database
                        // use this and we are done
                        _viewGenerator = SelectViewGeneratorFromViewDefinition(
                                                errorContext,
                                                expressionFactory,
                                                db,
                                                view,
                                                parameters);
                        s_formatViewBindingTracer.WriteLine(viewFound);
                        PrepareViewForRemoteObjects(ViewGenerator, so);
                        return;
                    }

                    s_formatViewBindingTracer.WriteLine(viewNotFound);
                    // we did not get any default view (and shape), we need to force one
                    // we just select properties out of the object itself, since they were not
                    // specified on the command line
                    _viewGenerator = SelectViewGeneratorFromProperties(shape, so, errorContext, expressionFactory, db, null);
                    PrepareViewForRemoteObjects(ViewGenerator, so);

                    return;
                }

                // we have a predefined shape: did the user specify properties on the command line?
                if (parameters != null && parameters.mshParameterList.Count > 0)
                {
                    _viewGenerator = SelectViewGeneratorFromProperties(shape, so, errorContext, expressionFactory, db, parameters);
                    return;
                }

                // predefined shape: did the user specify the name of a view?
                if (parameters != null && !string.IsNullOrEmpty(parameters.viewName))
                {
                    using (s_formatViewBindingTracer.TraceScope(findViewNameType, parameters.viewName,
                        PSObjectTypeName(so)))
                    {
                        view = DisplayDataQuery.GetViewByShapeAndType(expressionFactory, db, shape, typeNames, parameters.viewName);
                    }

                    if (view != null)
                    {
                        _viewGenerator = SelectViewGeneratorFromViewDefinition(
                                                    errorContext,
                                                    expressionFactory,
                                                    db,
                                                    view,
                                                    parameters);
                        s_formatViewBindingTracer.WriteLine(viewFound);
                        return;
                    }

                    s_formatViewBindingTracer.WriteLine(viewNotFound);
                    // illegal input, we have to terminate
                    ProcessUnknownViewName(errorContext, parameters.viewName, so, db, shape);
                }

                // predefined shape: do we have a default view in format.ps1xml?
                using (s_formatViewBindingTracer.TraceScope(findViewShapeType, shape, PSObjectTypeName(so)))
                {
                    view = DisplayDataQuery.GetViewByShapeAndType(expressionFactory, db, shape, typeNames, null);
                }

                if (view != null)
                {
                    _viewGenerator = SelectViewGeneratorFromViewDefinition(
                                                errorContext,
                                                expressionFactory,
                                                db,
                                                view,
                                                parameters);
                    s_formatViewBindingTracer.WriteLine(viewFound);
                    PrepareViewForRemoteObjects(ViewGenerator, so);

                    return;
                }

                s_formatViewBindingTracer.WriteLine(viewNotFound);
                // we just select properties out of the object itself
                _viewGenerator = SelectViewGeneratorFromProperties(shape, so, errorContext, expressionFactory, db, parameters);
                PrepareViewForRemoteObjects(ViewGenerator, so);
            }
            finally
            {
                DisplayDataQuery.ResetTracer();
            }
        }

        /// <summary>
        /// Prepares a given view for remote object processing ie., lets the view
        /// display (or not) ComputerName property. This will query the object to
        /// check if ComputerName property is present. If present, this will prepare
        /// the view.
        /// </summary>
        /// <param name="viewGenerator"></param>
        /// <param name="so"></param>
        private static void PrepareViewForRemoteObjects(ViewGenerator viewGenerator, PSObject so)
        {
            if (PSObjectHelper.ShouldShowComputerNameProperty(so))
            {
                viewGenerator.PrepareForRemoteObjects(so);
            }
        }

        /// <summary>
        /// Helper method to process Unknown error message.
        /// It helps is creating appropriate error message to
        /// be displayed to the user.
        /// </summary>
        /// <param name="errorContext">Error context.</param>
        /// <param name="viewName">Uses supplied view name.</param>
        /// <param name="so">Source object.</param>
        /// <param name="db">Types info database.</param>
        /// <param name="formatShape">Requested format shape.</param>
        private static void ProcessUnknownViewName(TerminatingErrorContext errorContext, string viewName, PSObject so, TypeInfoDataBase db, FormatShape formatShape)
        {
            string msg = null;
            bool foundValidViews = false;
            string formatTypeName = null;
            const string separator = ", ";
            StringBuilder validViewFormats = new StringBuilder();

            if (so != null && so.BaseObject != null &&
                db != null && db.viewDefinitionsSection != null &&
                db.viewDefinitionsSection.viewDefinitionList != null &&
                db.viewDefinitionsSection.viewDefinitionList.Count > 0)
            {
                StringBuilder validViews = new StringBuilder();
                string currentObjectTypeName = so.BaseObject.GetType().ToString();

                Type formatType = null;
                if (formatShape == FormatShape.Table)
                {
                    formatType = typeof(TableControlBody);
                    formatTypeName = "Table";
                }
                else if (formatShape == FormatShape.List)
                {
                    formatType = typeof(ListControlBody);
                    formatTypeName = "List";
                }
                else if (formatShape == FormatShape.Wide)
                {
                    formatType = typeof(WideControlBody);
                    formatTypeName = "Wide";
                }
                else if (formatShape == FormatShape.Complex)
                {
                    formatType = typeof(ComplexControlBody);
                    formatTypeName = "Custom";
                }

                if (formatType != null)
                {
                    foreach (ViewDefinition currentViewDefinition in db.viewDefinitionsSection.viewDefinitionList)
                    {
                        if (currentViewDefinition.mainControl != null)
                        {
                            foreach (TypeOrGroupReference currentTypeOrGroupReference in currentViewDefinition.appliesTo.referenceList)
                            {
                                if (!string.IsNullOrEmpty(currentTypeOrGroupReference.name) &&
                                    string.Equals(currentObjectTypeName, currentTypeOrGroupReference.name, StringComparison.OrdinalIgnoreCase))
                                {
                                    if (currentViewDefinition.mainControl.GetType() == formatType)
                                    {
                                        validViews.Append(currentViewDefinition.name);
                                        validViews.Append(separator);
                                    }
                                    else if (string.Equals(viewName, currentViewDefinition.name, StringComparison.OrdinalIgnoreCase))
                                    {
                                        string cmdletFormatName = null;
                                        if (currentViewDefinition.mainControl is TableControlBody)
                                        {
                                            cmdletFormatName = "Format-Table";
                                        }
                                        else if (currentViewDefinition.mainControl is ListControlBody)
                                        {
                                            cmdletFormatName = "Format-List";
                                        }
                                        else if (currentViewDefinition.mainControl is WideControlBody)
                                        {
                                            cmdletFormatName = "Format-Wide";
                                        }
                                        else if (currentViewDefinition.mainControl is ComplexControlBody)
                                        {
                                            cmdletFormatName = "Format-Custom";
                                        }

                                        if (validViewFormats.Length == 0)
                                        {
                                            string suggestValidViewNamePrefix = StringUtil.Format(FormatAndOut_format_xxx.SuggestValidViewNamePrefix);
                                            validViewFormats.Append(suggestValidViewNamePrefix);
                                        }
                                        else
                                        {
                                            validViewFormats.Append(", ");
                                        }

                                        validViewFormats.Append(cmdletFormatName);
                                    }
                                }
                            }
                        }
                    }
                }

                if (validViews.Length > 0)
                {
                    validViews.Remove(validViews.Length - separator.Length, separator.Length);
                    msg = StringUtil.Format(FormatAndOut_format_xxx.InvalidViewNameError, viewName, formatTypeName, validViews.ToString());
                    foundValidViews = true;
                }
            }

            if (!foundValidViews)
            {
                StringBuilder unKnowViewFormatStringBuilder = new StringBuilder();
                if (validViewFormats.Length > 0)
                {
                    // unKnowViewFormatStringBuilder.Append(StringUtil.Format(FormatAndOut_format_xxx.UnknownViewNameError, viewName));
                    unKnowViewFormatStringBuilder.Append(StringUtil.Format(FormatAndOut_format_xxx.UnknownViewNameErrorSuffix, viewName, formatTypeName));
                    unKnowViewFormatStringBuilder.Append(validViewFormats);
                }
                else
                {
                    unKnowViewFormatStringBuilder.Append(StringUtil.Format(FormatAndOut_format_xxx.UnknownViewNameError, viewName));
                    unKnowViewFormatStringBuilder.Append(StringUtil.Format(FormatAndOut_format_xxx.NonExistingViewNameError, formatTypeName, so.BaseObject.GetType()));
                }

                msg = unKnowViewFormatStringBuilder.ToString();
            }

            ErrorRecord errorRecord = new ErrorRecord(
                                            new PipelineStoppedException(),
                                            "FormatViewNotFound",
                                            ErrorCategory.ObjectNotFound,
                                            viewName);

            errorRecord.ErrorDetails = new ErrorDetails(msg);
            errorContext.ThrowTerminatingError(errorRecord);
        }

        internal ViewGenerator ViewGenerator
        {
            get
            {
                Diagnostics.Assert(_viewGenerator != null, "this.viewGenerator cannot be null");
                return _viewGenerator;
            }
        }

        private static ViewGenerator SelectViewGeneratorFromViewDefinition(
                                        TerminatingErrorContext errorContext,
                                        PSPropertyExpressionFactory expressionFactory,
                                        TypeInfoDataBase db,
                                        ViewDefinition view,
                                        FormattingCommandLineParameters parameters)
        {
            ViewGenerator viewGenerator = null;
            if (view.mainControl is TableControlBody)
            {
                viewGenerator = new TableViewGenerator();
            }
            else if (view.mainControl is ListControlBody)
            {
                viewGenerator = new ListViewGenerator();
            }
            else if (view.mainControl is WideControlBody)
            {
                viewGenerator = new WideViewGenerator();
            }
            else if (view.mainControl is ComplexControlBody)
            {
                viewGenerator = new ComplexViewGenerator();
            }

            Diagnostics.Assert(viewGenerator != null, "viewGenerator != null");
            viewGenerator.Initialize(errorContext, expressionFactory, db, view, parameters);
            return viewGenerator;
        }

        private static ViewGenerator SelectViewGeneratorFromProperties(FormatShape shape, PSObject so,
                                    TerminatingErrorContext errorContext,
                                    PSPropertyExpressionFactory expressionFactory,
                                    TypeInfoDataBase db,
                                    FormattingCommandLineParameters parameters)
        {
            // use some heuristics to determine the shape if none is specified
            if (shape == FormatShape.Undefined && parameters == null)
            {
                // check first if we have a known shape for a type
                var typeNames = so.InternalTypeNames;
                shape = DisplayDataQuery.GetShapeFromType(expressionFactory, db, typeNames);

                if (shape == FormatShape.Undefined)
                {
                    // check if we can have a table:
                    // we want to get the # of properties we are going to display
                    List<PSPropertyExpression> expressionList = PSObjectHelper.GetDefaultPropertySet(so);
                    if (expressionList.Count == 0)
                    {
                        // we failed to get anything from a property set
                        // we just get the first properties out of the first object
                        foreach (MshResolvedExpressionParameterAssociation mrepa in AssociationManager.ExpandAll(so))
                        {
                            expressionList.Add(mrepa.ResolvedExpression);
                        }
                    }

                    // decide what shape we want for the given number of properties
                    shape = DisplayDataQuery.GetShapeFromPropertyCount(db, expressionList.Count);
                }
            }

            ViewGenerator viewGenerator = null;
            if (shape == FormatShape.Table)
            {
                viewGenerator = new TableViewGenerator();
            }
            else if (shape == FormatShape.List)
            {
                viewGenerator = new ListViewGenerator();
            }
            else if (shape == FormatShape.Wide)
            {
                viewGenerator = new WideViewGenerator();
            }
            else if (shape == FormatShape.Complex)
            {
                viewGenerator = new ComplexViewGenerator();
            }

            Diagnostics.Assert(viewGenerator != null, "viewGenerator != null");

            viewGenerator.Initialize(errorContext, expressionFactory, so, db, parameters);
            return viewGenerator;
        }

        /// <summary>
        /// The view generator that produced data for a selected shape.
        /// </summary>
        private ViewGenerator _viewGenerator = null;
    }

    /// <summary>
    /// Class to manage the selection of a desired view type
    /// for out of band objects.
    /// </summary>
    internal static class OutOfBandFormatViewManager
    {
        private static bool IsNotRemotingProperty(string name)
        {
            var isRemotingPropertyName = name.Equals(RemotingConstants.ComputerNameNoteProperty, StringComparison.OrdinalIgnoreCase)
                   || name.Equals(RemotingConstants.ShowComputerNameNoteProperty, StringComparison.OrdinalIgnoreCase)
                   || name.Equals(RemotingConstants.RunspaceIdNoteProperty, StringComparison.OrdinalIgnoreCase)
                   || name.Equals(RemotingConstants.SourceJobInstanceId, StringComparison.OrdinalIgnoreCase);
            return !isRemotingPropertyName;
        }

        private static readonly MemberNamePredicate NameIsNotRemotingProperty = IsNotRemotingProperty;

        internal static bool HasNonRemotingProperties(PSObject so) => so.GetFirstPropertyOrDefault(NameIsNotRemotingProperty) != null;

        internal static FormatEntryData GenerateOutOfBandData(TerminatingErrorContext errorContext, PSPropertyExpressionFactory expressionFactory,
                    TypeInfoDataBase db, PSObject so, int enumerationLimit, bool useToStringFallback, out List<ErrorRecord> errors)
        {
            errors = null;

            var typeNames = so.InternalTypeNames;
            ViewDefinition view = DisplayDataQuery.GetOutOfBandView(expressionFactory, db, typeNames);

            ViewGenerator outOfBandViewGenerator;
            if (view != null)
            {
                // process an out of band view retrieved from the display database
                if (view.mainControl is ComplexControlBody)
                {
                    outOfBandViewGenerator = new ComplexViewGenerator();
                }
                else
                {
                    outOfBandViewGenerator = new ListViewGenerator();
                }

                outOfBandViewGenerator.Initialize(errorContext, expressionFactory, db, view, null);
            }
            else
            {
                if (DefaultScalarTypes.IsTypeInList(typeNames)
                    || !HasNonRemotingProperties(so))
                {
                    // we force a ToString() on well known types
                    return GenerateOutOfBandObjectAsToString(so);
                }

                if (!useToStringFallback)
                {
                    return null;
                }

                // we must check we have enough properties for a list view
                if (new PSPropertyExpression("*").ResolveNames(so).Count == 0)
                {
                    return null;
                }

                // we do not have a view, we default to list view
                // process an out of band view as a default
                outOfBandViewGenerator = new ListViewGenerator();
                outOfBandViewGenerator.Initialize(errorContext, expressionFactory, so, db, null);
            }

            FormatEntryData fed = outOfBandViewGenerator.GeneratePayload(so, enumerationLimit);
            fed.outOfBand = true;
            fed.writeStream = so.WriteStream;

            errors = outOfBandViewGenerator.ErrorManager.DrainFailedResultList();

            return fed;
        }

        internal static FormatEntryData GenerateOutOfBandObjectAsToString(PSObject so)
        {
            FormatEntryData fed = new FormatEntryData();
            fed.outOfBand = true;

            RawTextFormatEntry rawTextEntry = new RawTextFormatEntry();
            rawTextEntry.text = so.ToString();
            fed.formatEntryInfo = rawTextEntry;

            return fed;
        }
    }

    /// <summary>
    /// Helper class to manage the logging of errors resulting from
    /// evaluations of PSPropertyExpression instances
    ///
    /// Depending on settings, it queues the failing PSPropertyExpressionResult
    /// instances and generates a list of out-of-band FormatEntryData
    /// objects to be sent to the output pipeline.
    /// </summary>
    internal sealed class FormatErrorManager
    {
        internal FormatErrorManager(FormatErrorPolicy formatErrorPolicy)
        {
            _formatErrorPolicy = formatErrorPolicy;
        }

        /// <summary>
        /// Log a failed evaluation of an PSPropertyExpression.
        /// </summary>
        /// <param name="result">PSPropertyExpressionResult containing the failed evaluation data.</param>
        /// <param name="sourceObject">Object used to evaluate the PSPropertyExpression.</param>
        internal void LogPSPropertyExpressionFailedResult(PSPropertyExpressionResult result, object sourceObject)
        {
            if (!_formatErrorPolicy.ShowErrorsAsMessages)
                return;
            PSPropertyExpressionError error = new PSPropertyExpressionError();
            error.result = result;
            error.sourceObject = sourceObject;
            _formattingErrorList.Add(error);
        }

        /// <summary>
        /// Log a failed formatting operation.
        /// </summary>
        /// <param name="error">String format error object.</param>
        internal void LogStringFormatError(StringFormatError error)
        {
            if (!_formatErrorPolicy.ShowErrorsAsMessages)
                return;
            _formattingErrorList.Add(error);
        }

        internal bool DisplayErrorStrings
        {
            get { return _formatErrorPolicy.ShowErrorsInFormattedOutput; }
        }

        internal bool DisplayFormatErrorString
        {
            get
            {
                // NOTE: we key off the same flag
                return this.DisplayErrorStrings;
            }
        }

        internal string ErrorString
        {
            get { return _formatErrorPolicy.errorStringInFormattedOutput; }
        }

        internal string FormatErrorString
        {
            get { return _formatErrorPolicy.formatErrorStringInFormattedOutput; }
        }

        /// <summary>
        /// Provide a list of ErrorRecord entries
        /// to be written to the error pipeline and clear the list of pending
        /// errors.
        /// </summary>
        /// <returns>List of ErrorRecord objects.</returns>
        internal List<ErrorRecord> DrainFailedResultList()
        {
            if (!_formatErrorPolicy.ShowErrorsAsMessages)
                return null;

            List<ErrorRecord> retVal = new List<ErrorRecord>();
            foreach (FormattingError error in _formattingErrorList)
            {
                ErrorRecord errorRecord = GenerateErrorRecord(error);
                if (errorRecord != null)
                    retVal.Add(errorRecord);
            }

            _formattingErrorList.Clear();
            return retVal;
        }

        /// <summary>
        /// Conversion between an error internal representation and ErrorRecord.
        /// </summary>
        /// <param name="error">Internal error object.</param>
        /// <returns>Corresponding ErrorRecord instance.</returns>
        private static ErrorRecord GenerateErrorRecord(FormattingError error)
        {
            ErrorRecord errorRecord = null;
            string msg = null;
            if (error is PSPropertyExpressionError psPropertyExpressionError)
            {
                errorRecord = new ErrorRecord(
                                psPropertyExpressionError.result.Exception,
                                "PSPropertyExpressionError",
                                ErrorCategory.InvalidArgument,
                                psPropertyExpressionError.sourceObject);

                msg = StringUtil.Format(FormatAndOut_format_xxx.PSPropertyExpressionError,
                    psPropertyExpressionError.result.ResolvedExpression.ToString());
                errorRecord.ErrorDetails = new ErrorDetails(msg);
            }

            if (error is StringFormatError formattingError)
            {
                errorRecord = new ErrorRecord(
                                formattingError.exception,
                                "formattingError",
                                ErrorCategory.InvalidArgument,
                                formattingError.sourceObject);

                msg = StringUtil.Format(FormatAndOut_format_xxx.FormattingError,
                    formattingError.formatString);
                errorRecord.ErrorDetails = new ErrorDetails(msg);
            }

            return errorRecord;
        }

        private readonly FormatErrorPolicy _formatErrorPolicy;

        /// <summary>
        /// Current list of failed PSPropertyExpression evaluations.
        /// </summary>
        private readonly List<FormattingError> _formattingErrorList = new List<FormattingError>();
    }
}
