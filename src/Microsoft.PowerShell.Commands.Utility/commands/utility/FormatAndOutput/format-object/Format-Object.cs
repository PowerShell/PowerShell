// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Management.Automation;

using Microsoft.PowerShell.Commands.Internal.Format;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Implementation for the format-custom command. It just calls the formatting engine on complex shape.
    /// </summary>
    [Cmdlet(VerbsCommon.Format, "Custom", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113301")]
    public class FormatCustomCommand : OuterFormatShapeCommandBase
    {
        /// <summary>
        /// Constructor to se the inner command.
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
        /// </summary>
        /// <value></value>
        [ValidateRangeAttribute(1, int.MaxValue)]
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
            FormattingCommandLineParameters parameters = new FormattingCommandLineParameters();

            if (_props != null)
            {
                ParameterProcessor processor = new ParameterProcessor(new FormatObjectParameterDefinition());
                TerminatingErrorContext invocationContext = new TerminatingErrorContext(this);
                parameters.mshParameterList = processor.ProcessParameters(_props, invocationContext);
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

            parameters.groupByParameter = this.ProcessGroupByParameter();
            parameters.forceFormattingAlsoOnOutOfBand = this.Force;
            if (this.showErrorsAsMessages.HasValue)
                parameters.showErrorsAsMessages = this.showErrorsAsMessages;
            if (this.showErrorsInFormattedOutput.HasValue)
                parameters.showErrorsInFormattedOutput = this.showErrorsInFormattedOutput;

            parameters.expansion = ProcessExpandParameter();

            ComplexSpecificParameters csp = new ComplexSpecificParameters();
            csp.maxDepth = _depth;
            parameters.shapeParameters = csp;

            return parameters;
        }
    }
}

