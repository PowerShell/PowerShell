// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Internal;

using Microsoft.PowerShell.Commands.Internal.Format;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Implementation for the format-table command.
    /// </summary>
    [Cmdlet(VerbsCommon.Format, "Wide", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113304")]
    public class FormatWideCommand : OuterFormatShapeCommandBase
    {
        /// <summary>
        /// Constructor to se the inner command.
        /// </summary>
        public FormatWideCommand()
        {
            this.implementation = new InnerFormatShapeCommand(FormatShape.Wide);
        }

        #region Command Line Switches

        /// <summary>
        /// Positional parameter for properties, property sets and table sets specified on the command line.
        /// The parameter is optional, since the defaults will be determined using property sets, etc.
        /// </summary>
        [Parameter(Position = 0)]
        public object Property
        {
            get { return _prop; }

            set { _prop = value; }
        }

        private object _prop;

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

            set { _autosize = value; }
        }

        private bool? _autosize = null;

        /// <summary>
        /// Optional, non positional parameter.
        /// </summary>
        /// <value></value>
        [Parameter]
        [ValidateRangeAttribute(1, int.MaxValue)]
        public int Column
        {
            get
            {
                if (_column.HasValue)
                    return _column.Value;
                return -1;
            }

            set { _column = value; }
        }

        private int? _column = null;

        #endregion

        internal override FormattingCommandLineParameters GetCommandLineParameters()
        {
            FormattingCommandLineParameters parameters = new FormattingCommandLineParameters();

            if (_prop != null)
            {
                ParameterProcessor processor = new ParameterProcessor(new FormatWideParameterDefinition());
                TerminatingErrorContext invocationContext = new TerminatingErrorContext(this);
                parameters.mshParameterList = processor.ProcessParameters(new object[] { _prop }, invocationContext);
            }

            if (!string.IsNullOrEmpty(this.View))
            {
                // we have a view command line switch
                if (parameters.mshParameterList.Count != 0)
                {
                    ReportCannotSpecifyViewAndProperty();
                }

                parameters.viewName = this.View;
            }

            // we cannot specify -column and -autosize, they are mutually exclusive
            if (_autosize.HasValue && _column.HasValue)
            {
                if (_autosize.Value)
                {
                    // the user specified -autosize:true AND a column number
                    string msg = StringUtil.Format(FormatAndOut_format_xxx.CannotSpecifyAutosizeAndColumnsError);

                    ErrorRecord errorRecord = new ErrorRecord(
                        new InvalidDataException(),
                        "FormatCannotSpecifyAutosizeAndColumns",
                        ErrorCategory.InvalidArgument,
                        null);

                    errorRecord.ErrorDetails = new ErrorDetails(msg);
                    this.ThrowTerminatingError(errorRecord);
                }
            }

            parameters.groupByParameter = this.ProcessGroupByParameter();
            parameters.forceFormattingAlsoOnOutOfBand = this.Force;
            if (this.showErrorsAsMessages.HasValue)
                parameters.showErrorsAsMessages = this.showErrorsAsMessages;
            if (this.showErrorsInFormattedOutput.HasValue)
                parameters.showErrorsInFormattedOutput = this.showErrorsInFormattedOutput;

            parameters.expansion = ProcessExpandParameter();

            if (_autosize.HasValue)
                parameters.autosize = _autosize.Value;

            WideSpecificParameters wideSpecific = new WideSpecificParameters();
            parameters.shapeParameters = wideSpecific;
            if (_column.HasValue)
            {
                wideSpecific.columns = _column.Value;
            }

            return parameters;
        }
    }
}

