// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Management.Automation;
using System.Management.Automation.Internal;

using Microsoft.PowerShell.Commands.Internal.Format;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Implementation for the format-table command.
    /// </summary>
    [Cmdlet(VerbsCommon.Format, "Wide", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096930")]
    public class FormatWideCommand : OuterFormatShapeCommandBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FormatWideCommand"/> class
        /// and sets the inner command.
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
            get => _autosize.GetValueOrDefault();
            set => _autosize = value;
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
            get => _column.GetValueOrDefault(-1);
            set => _column = value;
        }

        private int? _column = null;

        #endregion

        internal override FormattingCommandLineParameters GetCommandLineParameters()
        {
            FormattingCommandLineParameters parameters = new();

            if (_prop != null)
            {
                ParameterProcessor processor = new(new FormatWideParameterDefinition());
                TerminatingErrorContext invocationContext = new(this);
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
            if (_column.HasValue && _autosize.GetValueOrDefault())
            {
                // the user specified -autosize:true AND a column number
                string msg = StringUtil.Format(FormatAndOut_format_xxx.CannotSpecifyAutosizeAndColumnsError);

                ErrorRecord errorRecord = new(
                    new InvalidDataException(),
                    "FormatCannotSpecifyAutosizeAndColumns",
                    ErrorCategory.InvalidArgument,
                    null);

                errorRecord.ErrorDetails = new ErrorDetails(msg);
                this.ThrowTerminatingError(errorRecord);
            }

            parameters.groupByParameter = this.ProcessGroupByParameter();
            parameters.forceFormattingAlsoOnOutOfBand = this.Force;
            parameters.showErrorsAsMessages = this.showErrorsAsMessages.GetValueOrDefault();
            parameters.showErrorsInFormattedOutput = this.showErrorsInFormattedOutput.GetValueOrDefault();

            parameters.expansion = ProcessExpandParameter();

            parameters.autosize = _autosize.GetValueOrDefault();

            parameters.shapeParameters = new WideSpecificParameters()
            {
                columns = _column.GetValueOrDefault()
            };

            return parameters;
        }
    }
}
