// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces;
using Microsoft.PowerShell.Commands;

namespace Microsoft.PowerShell.Commands.Internal.Format
{
    /// <summary>
    /// Base class defining the formatting context and the
    /// formatting context manager (stack based)
    /// </summary>
    internal class InnerFormatShapeCommandBase : ImplementationCommandBase
    {
        /// <summary>
        /// Constructor to set up the formatting context.
        /// </summary>
        internal InnerFormatShapeCommandBase()
        {
            contextManager.Push(FormattingContextState.none);
        }

        /// <summary>
        /// Enum listing the possible states the context is in.
        /// </summary>
        internal enum FormattingContextState { none, document, group }

        /// <summary>
        /// Context manager: stack to keep track in which context
        /// the formatter is.
        /// </summary>
        protected Stack<FormattingContextState> contextManager = new Stack<FormattingContextState>();
    }

    /// <summary>
    /// Core inner implementation for format/xxx commands.
    /// </summary>
    internal class InnerFormatShapeCommand : InnerFormatShapeCommandBase
    {
        /// <summary>
        /// Constructor to glue to the CRO.
        /// </summary>
        internal InnerFormatShapeCommand(FormatShape shape)
        {
            _shape = shape;
        }

        internal static int FormatEnumerationLimit()
        {
            object enumLimitVal = null;
            try
            {
                // Win8: 192504
                if (LocalPipeline.GetExecutionContextFromTLS() != null)
                {
                    enumLimitVal = LocalPipeline.GetExecutionContextFromTLS()
                        .SessionState.PSVariable
                        .GetValue("global:" + InitialSessionState.FormatEnumerationLimit);
                }
            }
            // Eat the following exceptions, enumerationLimit will use the default value
            catch (ProviderNotFoundException)
            {
            }
            catch (ProviderInvocationException)
            {
            }

            // if $global:FormatEnumerationLimit is an int, overwrite the default value
            return enumLimitVal is int ? (int)enumLimitVal : InitialSessionState.DefaultFormatEnumerationLimit;
        }

        internal override void BeginProcessing()
        {
            base.BeginProcessing();

            // Get the Format Enumeration Limit.
            _enumerationLimit = InnerFormatShapeCommand.FormatEnumerationLimit();

            _expressionFactory = new PSPropertyExpressionFactory();

            _formatObjectDeserializer = new FormatObjectDeserializer(this.TerminatingErrorContext);
        }

        /// <summary>
        /// Execution entry point.
        /// </summary>
        internal override void ProcessRecord()
        {
            _typeInfoDataBase = this.OuterCmdlet().Context.FormatDBManager.GetTypeInfoDataBase();

            PSObject so = this.ReadObject();
            if (so == null || so == AutomationNull.Value)
                return;

            IEnumerable e = PSObjectHelper.GetEnumerable(so);
            if (e == null)
            {
                ProcessObject(so);
                return;
            }

            // we have an IEnumerable, we have to decide if to expand, if at all
            EnumerableExpansion expansionState = this.GetExpansionState(so);

            switch (expansionState)
            {
                case EnumerableExpansion.EnumOnly:
                    {
                        foreach (object obj in e)
                        {
                            ProcessObject(PSObjectHelper.AsPSObject(obj));
                        }
                    }

                    break;
                case EnumerableExpansion.Both:
                    {
                        var objs = e.Cast<object>().ToArray();

                        ProcessCoreOutOfBand(so, objs.Length);
                        foreach (object obj in objs)
                        {
                            ProcessObject(PSObjectHelper.AsPSObject(obj));
                        }
                    }

                    break;
                default:
                    {
                        // do not enumerate at all (CoreOnly)
                        ProcessObject(so);
                    }

                    break;
            }
        }

        private EnumerableExpansion GetExpansionState(PSObject so)
        {
            // if the command line swtich has been specified, use this as an override
            if (_parameters != null && _parameters.expansion.HasValue)
            {
                return _parameters.expansion.Value;
            }

            // check if we have an expansion entry in format.mshxml
            var typeNames = so.InternalTypeNames;
            return DisplayDataQuery.GetEnumerableExpansionFromType(
                _expressionFactory, _typeInfoDataBase, typeNames);
        }

        private void ProcessCoreOutOfBand(PSObject so, int count)
        {
            // emit some description header
            SendCommentOutOfBand(FormatAndOut_format_xxx.IEnum_Header);

            // emit the object as out of band
            ProcessOutOfBand(so, isProcessingError: false);

            string msg;
            // emit a comment to signal that the next N objects are from the IEnumerable
            switch (count)
            {
                case 0:
                    {
                        msg = FormatAndOut_format_xxx.IEnum_NoObjects;
                    }

                    break;
                case 1:
                    {
                        msg = FormatAndOut_format_xxx.IEnum_OneObject;
                    }

                    break;
                default:
                    {
                        msg = StringUtil.Format(FormatAndOut_format_xxx.IEnum_ManyObjects, count);
                    }

                    break;
            }

            SendCommentOutOfBand(msg);
        }

        private void SendCommentOutOfBand(string msg)
        {
            FormatEntryData fed = OutOfBandFormatViewManager.GenerateOutOfBandObjectAsToString(PSObjectHelper.AsPSObject(msg));
            if (fed != null)
            {
                this.WriteObject(fed);
            }
        }

        /// <summary>
        /// Execute formatting on a single object.
        /// </summary>
        /// <param name="so">Object to process.</param>
        private void ProcessObject(PSObject so)
        {
            // we do protect against reentrancy, assuming
            // no fancy multiplexing
            if (_formatObjectDeserializer.IsFormatInfoData(so))
            {
                // we are already formatted...
                this.WriteObject(so);
                return;
            }

            // if objects have to be treated as out of band, just
            // bail now
            // this is the case of objects coming before the
            // context manager is properly set
            if (ProcessOutOfBandObjectOutsideDocumentSequence(so))
            {
                return;
            }

            // if we haven't started yet, need to do so
            FormattingContextState ctx = contextManager.Peek();
            if (ctx == FormattingContextState.none)
            {
                // initialize the view manager
                _viewManager.Initialize(this.TerminatingErrorContext, _expressionFactory, _typeInfoDataBase, so, _shape, _parameters);

                // add the start message to output queue
                WriteFormatStartData(so);

                // enter the document context
                contextManager.Push(FormattingContextState.document);
            }

            // if we are here, we are either in the document document, or in a group

            // since we have a view now, we check if objects should be treated as out of band
            if (ProcessOutOfBandObjectInsideDocumentSequence(so))
            {
                return;
            }

            // check if we have to enter or exit a group
            GroupTransition transition = ComputeGroupTransition(so);
            if (transition == GroupTransition.enter)
            {
                // insert the group start marker
                PushGroup(so);
                this.WritePayloadObject(so);
            }
            else if (transition == GroupTransition.exit)
            {
                this.WritePayloadObject(so);
                // insert the group end marker
                PopGroup();
            }
            else if (transition == GroupTransition.startNew)
            {
                // Add newline before each group except first
                WriteNewLineObject();

                // double transition
                PopGroup(); // exit the current one
                PushGroup(so); // start a sibling group
                this.WritePayloadObject(so);
            }
            else // none, we did not have any transitions, just push out the data
            {
                this.WritePayloadObject(so);
            }
        }

        private void WriteNewLineObject()
        {
            FormatEntryData fed = new FormatEntryData();
            fed.outOfBand = true;

            ComplexViewEntry cve = new ComplexViewEntry();
            FormatEntry fe = new FormatEntry();
            cve.formatValueList.Add(fe);

            // Formating system writes newline before each object
            // so no need to add newline here like:
            //     fe.formatValueList.Add(new FormatNewLine());
            fed.formatEntryInfo = cve;

            this.WriteObject(fed);
        }

        private bool ShouldProcessOutOfBand
        {
            get
            {
                if (_shape == FormatShape.Undefined || _parameters == null)
                {
                    return true;
                }

                return !_parameters.forceFormattingAlsoOnOutOfBand;
            }
        }

        private bool ProcessOutOfBandObjectOutsideDocumentSequence(PSObject so)
        {
            if (!ShouldProcessOutOfBand)
            {
                return false;
            }

            if (so.InternalTypeNames.Count == 0)
            {
                return false;
            }

            List<ErrorRecord> errors;
            var fed = OutOfBandFormatViewManager.GenerateOutOfBandData(this.TerminatingErrorContext, _expressionFactory,
                _typeInfoDataBase, so, _enumerationLimit, false, out errors);
            WriteErrorRecords(errors);

            if (fed != null)
            {
                this.WriteObject(fed);
                return true;
            }

            return false;
        }

        private bool ProcessOutOfBandObjectInsideDocumentSequence(PSObject so)
        {
            if (!ShouldProcessOutOfBand)
            {
                return false;
            }

            var typeNames = so.InternalTypeNames;
            if (_viewManager.ViewGenerator.IsObjectApplicable(typeNames))
            {
                return false;
            }

            return ProcessOutOfBand(so, isProcessingError: false);
        }

        private bool ProcessOutOfBand(PSObject so, bool isProcessingError)
        {
            List<ErrorRecord> errors;
            FormatEntryData fed = OutOfBandFormatViewManager.GenerateOutOfBandData(this.TerminatingErrorContext, _expressionFactory,
                                    _typeInfoDataBase, so, _enumerationLimit, true, out errors);
            if (!isProcessingError)
                WriteErrorRecords(errors);

            if (fed != null)
            {
                this.WriteObject(fed);
                return true;
            }

            return false;
        }

        protected void WriteInternalErrorMessage(string message)
        {
            FormatEntryData fed = new FormatEntryData();
            fed.outOfBand = true;

            ComplexViewEntry cve = new ComplexViewEntry();
            FormatEntry fe = new FormatEntry();
            cve.formatValueList.Add(fe);

            fe.formatValueList.Add(new FormatNewLine());

            // get a field for the message
            FormatTextField ftf = new FormatTextField();
            ftf.text = message;
            fe.formatValueList.Add(ftf);

            fe.formatValueList.Add(new FormatNewLine());

            fed.formatEntryInfo = cve;

            this.WriteObject(fed);
        }

        private void WriteErrorRecords(List<ErrorRecord> errorRecordList)
        {
            if (errorRecordList == null)
                return;

            // NOTE: for the time being we directly process error records.
            // This is should change if we hook up error pipelines; for the
            // time being, this achieves partial results.
            //
            // see NTRAID#Windows OS Bug-932722-2004/10/21-kevinloo ("Output: SS: Swallowing exceptions")
            foreach (ErrorRecord errorRecord in errorRecordList)
            {
                // we are recursing on formatting errors: isProcessingError == true
                ProcessOutOfBand(PSObjectHelper.AsPSObject(errorRecord), true);
            }
        }

        internal override void EndProcessing()
        {
            // need to pop all the contexts, in case the transmission sequence
            // was interrupted
            while (true)
            {
                FormattingContextState ctx = contextManager.Peek();

                if (ctx == FormattingContextState.none)
                {
                    break; // we emerged and we are done
                }
                else if (ctx == FormattingContextState.group)
                {
                    PopGroup();
                }
                else if (ctx == FormattingContextState.document)
                {
                    // inject the end format information
                    FormatEndData endFormat = new FormatEndData();
                    this.WriteObject(endFormat);
                    contextManager.Pop();
                }
            }
        }

        internal void SetCommandLineParameters(FormattingCommandLineParameters commandLineParameters)
        {
            Diagnostics.Assert(commandLineParameters != null, "the caller has to pass a valid instance");
            _parameters = commandLineParameters;
        }

        /// <summary>
        /// Group transitions:
        /// none: stay in the same group
        /// enter: start a new group
        /// exit: exit from the current group.
        /// </summary>
        private enum GroupTransition { none, enter, exit, startNew }

        /// <summary>
        /// Compute the group transition, given an input object.
        /// </summary>
        /// <param name="so">Object received from the input pipeline.</param>
        /// <returns>GroupTransition enumeration.</returns>
        private GroupTransition ComputeGroupTransition(PSObject so)
        {
            // check if we have to start a group
            FormattingContextState ctx = contextManager.Peek();
            if (ctx == FormattingContextState.document)
            {
                // prime the grouping algorithm
                _viewManager.ViewGenerator.UpdateGroupingKeyValue(so);

                // need to start a group, but we are not in one
                return GroupTransition.enter;
            }

            // check if we need to start another group and keep track
            // of the current value for the grouping property
            return _viewManager.ViewGenerator.UpdateGroupingKeyValue(so) ? GroupTransition.startNew : GroupTransition.none;
        }

        private void WriteFormatStartData(PSObject so)
        {
            FormatStartData startFormat = _viewManager.ViewGenerator.GenerateStartData(so);
            this.WriteObject(startFormat);
        }

        /// <summary>
        /// Write a payplad object by properly wrapping it into
        /// a FormatEntry object.
        /// </summary>
        /// <param name="so">Object to process.</param>
        private void WritePayloadObject(PSObject so)
        {
            Diagnostics.Assert(so != null, "object so cannot be null");
            FormatEntryData fed = _viewManager.ViewGenerator.GeneratePayload(so, _enumerationLimit);
            fed.writeStream = so.WriteStream;
            this.WriteObject(fed);

            List<ErrorRecord> errors = _viewManager.ViewGenerator.ErrorManager.DrainFailedResultList();
            WriteErrorRecords(errors);
        }

        /// <summary>
        /// Inject the start group information
        /// and push group context on stack.
        /// </summary>
        /// <param name="firstObjectInGroup">current pipeline object
        /// that is starting the group</param>
        private void PushGroup(PSObject firstObjectInGroup)
        {
            GroupStartData startGroup = _viewManager.ViewGenerator.GenerateGroupStartData(firstObjectInGroup, _enumerationLimit);
            this.WriteObject(startGroup);
            contextManager.Push(FormattingContextState.group);
        }

        /// <summary>
        /// Inject the end group information
        /// and pop group context out of stack.
        /// </summary>
        private void PopGroup()
        {
            GroupEndData endGroup = _viewManager.ViewGenerator.GenerateGroupEndData();
            this.WriteObject(endGroup);
            contextManager.Pop();
        }

        /// <summary>
        /// The formatting shape this formatter emits.
        /// </summary>
        private readonly FormatShape _shape;

        #region expression factory

        /// <exception cref="ParseException"></exception>
        internal ScriptBlock CreateScriptBlock(string scriptText)
        {
            var scriptBlock = this.OuterCmdlet().InvokeCommand.NewScriptBlock(scriptText);
            scriptBlock.DebuggerStepThrough = true;
            return scriptBlock;
        }

        private PSPropertyExpressionFactory _expressionFactory;
        #endregion

        private FormatObjectDeserializer _formatObjectDeserializer;

        private TypeInfoDataBase _typeInfoDataBase = null;

        private FormattingCommandLineParameters _parameters = null;
        private readonly FormatViewManager _viewManager = new FormatViewManager();

        private int _enumerationLimit = InitialSessionState.DefaultFormatEnumerationLimit;
    }

    /// <summary>
    /// </summary>
    public class OuterFormatShapeCommandBase : FrontEndCommandBase
    {
        #region Command Line Switches

        /// <summary>
        /// Optional, non positional parameter to specify the
        /// group by property.
        /// </summary>
        [Parameter]
        public object GroupBy { get; set; } = null;

        /// <summary>
        /// Optional, non positional parameter.
        /// </summary>
        /// <value></value>
        [Parameter]
        public string View { get; set; } = null;

        /// <summary>
        /// Optional, non positional parameter.
        /// </summary>
        /// <value></value>
        [Parameter]
        public SwitchParameter ShowError
        {
            get
            {
                if (showErrorsAsMessages.HasValue)
                    return showErrorsAsMessages.Value;
                return false;
            }

            set
            {
                showErrorsAsMessages = value;
            }
        }

        internal bool? showErrorsAsMessages = null;

        /// <summary>
        /// Optional, non positional parameter.
        /// </summary>
        /// <value></value>
        [Parameter]
        public SwitchParameter DisplayError
        {
            get
            {
                if (showErrorsInFormattedOutput.HasValue)
                    return showErrorsInFormattedOutput.Value;
                return false;
            }

            set
            {
                showErrorsInFormattedOutput = value;
            }
        }

        internal bool? showErrorsInFormattedOutput = null;

        /// <summary>
        /// Optional, non positional parameter.
        /// </summary>
        /// <value></value>
        [Parameter]
        public SwitchParameter Force
        {
            get { return _forceFormattingAlsoOnOutOfBand; }

            set { _forceFormattingAlsoOnOutOfBand = value; }
        }

        private bool _forceFormattingAlsoOnOutOfBand;

        /// <summary>
        /// Optional, non positional parameter.
        /// </summary>
        /// <value></value>
        [Parameter]
        [ValidateSet(EnumerableExpansionConversion.CoreOnlyString,
                        EnumerableExpansionConversion.EnumOnlyString,
                        EnumerableExpansionConversion.BothString, IgnoreCase = true)]
        public string Expand { get; set; } = null;

        internal EnumerableExpansion? expansion = null;

        internal EnumerableExpansion? ProcessExpandParameter()
        {
            EnumerableExpansion? retVal = null;
            if (string.IsNullOrEmpty(Expand))
            {
                return null;
            }

            EnumerableExpansion temp;
            bool success = EnumerableExpansionConversion.Convert(Expand, out temp);
            if (!success)
            {
                // this should never happen, since we use the [ValidateSet] attribute
                // NOTE: this is an exception that should never be triggered
                throw PSTraceSource.NewArgumentException("Expand", FormatAndOut_MshParameter.IllegalEnumerableExpansionValue);
            }

            retVal = temp;
            return retVal;
        }

        internal MshParameter ProcessGroupByParameter()
        {
            if (GroupBy != null)
            {
                TerminatingErrorContext invocationContext =
                        new TerminatingErrorContext(this);
                ParameterProcessor processor = new ParameterProcessor(new FormatGroupByParameterDefinition());
                List<MshParameter> groupParameterList =
                    processor.ProcessParameters(new object[] { GroupBy },
                                                invocationContext);

                if (groupParameterList.Count != 0)
                    return groupParameterList[0];
            }

            return null;
        }

        #endregion

        /// <summary>
        /// </summary>
        protected override void BeginProcessing()
        {
            InnerFormatShapeCommand innerFormatCommand =
                            (InnerFormatShapeCommand)this.implementation;

            // read command line switches and pass them to the inner command
            FormattingCommandLineParameters parameters = GetCommandLineParameters();
            innerFormatCommand.SetCommandLineParameters(parameters);

            // must call base class for further processing
            base.BeginProcessing();
        }

        /// <summary>
        /// It reads the command line switches and collects them into a
        /// FormattingCommandLineParameters instance, ready to pass to the
        /// inner format command.
        /// </summary>
        /// <returns>Parameters collected in unified manner.</returns>
        internal virtual FormattingCommandLineParameters GetCommandLineParameters()
        {
            return null;
        }

        internal void ReportCannotSpecifyViewAndProperty()
        {
            string msg = StringUtil.Format(FormatAndOut_format_xxx.CannotSpecifyViewAndPropertyError);

            ErrorRecord errorRecord = new ErrorRecord(
                new InvalidDataException(),
                "FormatCannotSpecifyViewAndProperty",
                ErrorCategory.InvalidArgument,
                null);

            errorRecord.ErrorDetails = new ErrorDetails(msg);
            this.ThrowTerminatingError(errorRecord);
        }
    }

    /// <summary>
    /// </summary>
    public class OuterFormatTableAndListBase : OuterFormatShapeCommandBase
    {
        #region Command Line Switches

        /// <summary>
        /// Positional parameter for properties, property sets and table sets
        /// specified on the command line.
        /// The parameter is optional, since the defaults
        /// will be determined using property sets, etc.
        /// </summary>
        [Parameter(Position = 0)]
        public object[] Property { get; set; }

        /// <summary>
        /// Optional parameter for excluding properties from formatting.
        /// </summary>
        [Parameter]
        public string[] ExcludeProperty { get; set; }

        #endregion

        internal override FormattingCommandLineParameters GetCommandLineParameters()
        {
            FormattingCommandLineParameters parameters = new FormattingCommandLineParameters();

            GetCommandLineProperties(parameters, false);
            parameters.groupByParameter = this.ProcessGroupByParameter();

            parameters.forceFormattingAlsoOnOutOfBand = this.Force;
            if (this.showErrorsAsMessages.HasValue)
                parameters.showErrorsAsMessages = this.showErrorsAsMessages;
            if (this.showErrorsInFormattedOutput.HasValue)
                parameters.showErrorsInFormattedOutput = this.showErrorsInFormattedOutput;

            parameters.expansion = ProcessExpandParameter();

            return parameters;
        }

        internal void GetCommandLineProperties(FormattingCommandLineParameters parameters, bool isTable)
        {
            // Check View conflicts first (before any auto-expansion)
            if (!string.IsNullOrEmpty(this.View))
            {
                // View cannot be used with Property or ExcludeProperty
                if ((Property is not null && Property.Length != 0) || (ExcludeProperty is not null && ExcludeProperty.Length != 0))
                {
                    ReportCannotSpecifyViewAndProperty();
                }

                parameters.viewName = this.View;
            }

            if (Property != null)
            {
                CommandParameterDefinition def;

                if (isTable)
                    def = new FormatTableParameterDefinition();
                else
                    def = new FormatListParameterDefinition();
                ParameterProcessor processor = new ParameterProcessor(def);
                TerminatingErrorContext invocationContext = new TerminatingErrorContext(this);

                parameters.mshParameterList = processor.ProcessParameters(Property, invocationContext);
            }

            if (ExcludeProperty is not null)
            {
                parameters.excludePropertyFilter = new PSPropertyExpressionFilter(ExcludeProperty);

                // ExcludeProperty implies -Property * for better UX
                if (Property is null || Property.Length == 0)
                {
                    CommandParameterDefinition def = isTable
                        ? new FormatTableParameterDefinition()
                        : new FormatListParameterDefinition();
                    ParameterProcessor processor = new ParameterProcessor(def);
                    TerminatingErrorContext invocationContext = new TerminatingErrorContext(this);

                    parameters.mshParameterList = processor.ProcessParameters(new object[] { "*" }, invocationContext);
                }
            }
        }
    }

    /// <summary>
    /// </summary>
    public class OuterFormatTableBase : OuterFormatTableAndListBase
    {
        #region Command Line Switches

        /// <summary>
        /// Optional, non positional parameter.
        /// </summary>
        /// <value></value>
        [Parameter]
        public SwitchParameter AutoSize
        {
            get
            {
                if (_autosize.HasValue)
                    return _autosize.Value;
                return false;
            }

            set
            {
                _autosize = value;
            }
        }

        private bool? _autosize = null;

        /// <summary>
        /// Gets or sets if header is repeated per screen.
        /// </summary>
        [Parameter]
        public SwitchParameter RepeatHeader { get; set; }

        /// <summary>
        /// Optional, non positional parameter.
        /// </summary>
        /// <value></value>
        [Parameter]
        public SwitchParameter HideTableHeaders
        {
            get
            {
                if (_hideHeaders.HasValue)
                    return _hideHeaders.Value;
                return false;
            }

            set
            {
                _hideHeaders = value;
            }
        }

        private bool? _hideHeaders = null;

        /// <summary>
        /// Optional, non positional parameter.
        /// </summary>
        /// <value></value>
        [Parameter]
        public SwitchParameter Wrap
        {
            get
            {
                if (_multiLine.HasValue)
                    return _multiLine.Value;
                return false;
            }

            set
            {
                _multiLine = value;
            }
        }

        private bool? _multiLine = null;

        #endregion
        internal override FormattingCommandLineParameters GetCommandLineParameters()
        {
            FormattingCommandLineParameters parameters = new FormattingCommandLineParameters();

            GetCommandLineProperties(parameters, true);
            parameters.forceFormattingAlsoOnOutOfBand = this.Force;
            if (this.showErrorsAsMessages.HasValue)
                parameters.showErrorsAsMessages = this.showErrorsAsMessages;
            if (this.showErrorsInFormattedOutput.HasValue)
                parameters.showErrorsInFormattedOutput = this.showErrorsInFormattedOutput;

            parameters.expansion = ProcessExpandParameter();

            if (_autosize.HasValue)
                parameters.autosize = _autosize.Value;

            if (RepeatHeader)
            {
                parameters.repeatHeader = true;
            }

            parameters.groupByParameter = this.ProcessGroupByParameter();

            TableSpecificParameters tableParameters = new TableSpecificParameters();
            parameters.shapeParameters = tableParameters;

            if (_hideHeaders.HasValue)
            {
                tableParameters.hideHeaders = _hideHeaders.Value;
            }

            if (_multiLine.HasValue)
            {
                tableParameters.multiLine = _multiLine.Value;
            }

            return parameters;
        }
    }
}
