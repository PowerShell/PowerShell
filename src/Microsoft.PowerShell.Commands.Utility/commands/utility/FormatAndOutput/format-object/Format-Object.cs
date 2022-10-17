// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

using Microsoft.PowerShell.Commands.Internal.Format;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Implementation for the format-custom command. It just calls the formatting engine on complex shape.
    /// </summary>
    [Cmdlet(VerbsCommon.Format, "Custom", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096929")]
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
        public object[] Property { get; set; }

        /// <summary>
        /// Specifies the number of levels to recurse complex properties
        /// </summary>
        [ValidateRangeAttribute(1, int.MaxValue)]
        [Parameter]
        public int Depth { get; set; } = ComplexSpecificParameters.maxDepthAllowable;

        /// <inheritdoc cref="ComplexSpecificParameters.ScalarTypesToExpand" />
        [Parameter]
        [ValidateSet("System.DateTime", "System.DateTimeOffset")] // if we get more types here, consider moving to IValidateSetValuesGenerator
        public string[] ExpandType { get; set; }

        #endregion

        internal override FormattingCommandLineParameters GetCommandLineParameters()
        {
            FormattingCommandLineParameters parameters = new();

            if (Property != null)
            {
                ParameterProcessor processor = new(new FormatObjectParameterDefinition());
                TerminatingErrorContext invocationContext = new(this);
                parameters.mshParameterList = processor.ProcessParameters(Property, invocationContext);
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

            ComplexSpecificParameters csp = new();
            csp.maxDepth = Depth;
            csp.ScalarTypesToExpand = ExpandType;
            parameters.shapeParameters = csp;

            return parameters;
        }
    }
}
