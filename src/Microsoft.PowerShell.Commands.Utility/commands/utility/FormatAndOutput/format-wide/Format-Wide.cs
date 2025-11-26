// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Management.Automation;
using System.Management.Automation.Internal;

using Microsoft.PowerShell.Commands.Internal.Format;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Implementation for the Format-Wide command.
    /// </summary>
    [Cmdlet(VerbsCommon.Format, "Wide", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096930")]
    [OutputType(typeof(FormatStartData), typeof(FormatEntryData), typeof(FormatEndData), typeof(GroupStartData), typeof(GroupEndData))]
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
        /// Gets or sets the properties to exclude from formatting.
        /// </summary>
        [Parameter]
        public string[] ExcludeProperty { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to autosize the output.
        /// </summary>
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
        [ValidateRange(1, int.MaxValue)]
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

            if (ExcludeProperty != null)
            {
                parameters.excludePropertyFilter = new PSPropertyExpressionFilter(ExcludeProperty);

                // ExcludeProperty implies -Property * for better UX
                if (_prop == null)
                {
                    ParameterProcessor processor = new(new FormatWideParameterDefinition());
                    TerminatingErrorContext invocationContext = new(this);
                    parameters.mshParameterList = processor.ProcessParameters(new object[] { "*" }, invocationContext);
                }
            }

            if (!string.IsNullOrEmpty(this.View))
            {
                // we have a view command line switch
                // View cannot be used with Property or ExcludeProperty
                if (parameters.mshParameterList.Count != 0 || ExcludeProperty != null)
                {
                    ReportCannotSpecifyViewAndProperty();
                }

                parameters.viewName = this.View;
            }

            // we cannot specify -column and -autosize, they are mutually exclusive
            if (AutoSize && _column.HasValue)
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
            if (this.showErrorsAsMessages.HasValue)
                parameters.showErrorsAsMessages = this.showErrorsAsMessages;
            if (this.showErrorsInFormattedOutput.HasValue)
                parameters.showErrorsInFormattedOutput = this.showErrorsInFormattedOutput;

            parameters.expansion = ProcessExpandParameter();

            if (_autosize.HasValue)
                parameters.autosize = _autosize.Value;

            WideSpecificParameters wideSpecific = new();
            parameters.shapeParameters = wideSpecific;
            if (_column.HasValue)
            {
                wideSpecific.columns = _column.Value;
            }

            return parameters;
        }
    }
}
