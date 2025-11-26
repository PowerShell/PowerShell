// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

using Microsoft.PowerShell.Commands.Internal.Format;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Implementation for the Format-Custom command. It just calls the formatting engine on complex shape.
    /// </summary>
    [Cmdlet(VerbsCommon.Format, "Custom", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096929")]
    [OutputType(typeof(FormatStartData), typeof(FormatEntryData), typeof(FormatEndData), typeof(GroupStartData), typeof(GroupEndData))]
    public class FormatCustomCommand : OuterFormatShapeCommandBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FormatCustomCommand"/> class
        /// and sets the inner command.
        /// </summary>
        public FormatCustomCommand()
        {
            this.implementation = new InnerFormatShapeCommand(FormatShape.Complex);
        }

        #region Command Line Switches

        /// <summary>
        /// Positional parameter for properties, property sets and table sets.
        /// specified on the command line.
        /// The parameter is optional, since the defaults
        /// will be determined using property sets, etc.
        /// </summary>
        [Parameter(Position = 0)]
        public object[] Property
        {
            get { return _props; }

            set { _props = value; }
        }

        private object[] _props;

        /// <summary>
        /// Gets or sets the properties to exclude from formatting.
        /// </summary>
        [Parameter]
        public string[] ExcludeProperty { get; set; }

        /// <summary>
        /// </summary>
        /// <value></value>
        [ValidateRange(1, int.MaxValue)]
        [Parameter]
        public int Depth
        {
            get { return _depth; }

            set { _depth = value; }
        }

        private int _depth = ComplexSpecificParameters.maxDepthAllowable;

        #endregion

        internal override FormattingCommandLineParameters GetCommandLineParameters()
        {
            FormattingCommandLineParameters parameters = new();

            if (_props != null)
            {
                ParameterProcessor processor = new(new FormatObjectParameterDefinition());
                TerminatingErrorContext invocationContext = new(this);
                parameters.mshParameterList = processor.ProcessParameters(_props, invocationContext);
            }

            if (ExcludeProperty != null)
            {
                parameters.excludePropertyFilter = new PSPropertyExpressionFilter(ExcludeProperty);

                // ExcludeProperty implies -Property * for better UX
                if (_props == null || _props.Length == 0)
                {
                    ParameterProcessor processor = new(new FormatObjectParameterDefinition());
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

            parameters.groupByParameter = this.ProcessGroupByParameter();
            parameters.forceFormattingAlsoOnOutOfBand = this.Force;
            if (this.showErrorsAsMessages.HasValue)
                parameters.showErrorsAsMessages = this.showErrorsAsMessages;
            if (this.showErrorsInFormattedOutput.HasValue)
                parameters.showErrorsInFormattedOutput = this.showErrorsInFormattedOutput;

            parameters.expansion = ProcessExpandParameter();

            ComplexSpecificParameters csp = new();
            csp.maxDepth = _depth;
            parameters.shapeParameters = csp;

            return parameters;
        }
    }
}
