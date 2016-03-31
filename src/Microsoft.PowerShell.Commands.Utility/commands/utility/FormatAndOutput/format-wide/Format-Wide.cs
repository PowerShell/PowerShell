/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Internal;

using Microsoft.PowerShell.Commands.Internal.Format;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// implementation for the format-table command
    /// </summary>
    [Cmdlet("Format", "Wide", HelpUri = "http://go.microsoft.com/fwlink/?LinkID=113304")]
    public class FormatWideCommand : OuterFormatShapeCommandBase
    {
        /// <summary>
        /// constructor to se the inner command
        /// </summary>
        public FormatWideCommand()
        {
            this.implementation = new InnerFormatShapeCommand(FormatShape.Wide);
        }


        #region Command Line Switches

        /// <summary>
        /// Positional parameter for properties, property sets and table sets
        /// specified on the command line.
        /// The paramater is optional, since the defaults
        /// will be determined using property sets, etc.
        /// </summary>
        [Parameter(Position=0)]
        public object Property
        {
            get { return prop; }
            set { prop = value; }
        }

        private object prop;

        /// <summary>
        /// optional, non positional parameter
        /// </summary>
        /// <value></value>
        [Parameter]
        public SwitchParameter AutoSize
        {
            get
            {
                if (autosize.HasValue)
                    return autosize.Value;
                return false;
            }
            set { autosize = value; }
        }
        private Nullable<bool> autosize = null;


        /// <summary>
        /// optional, non positional parameter
        /// </summary>
        /// <value></value>
        [Parameter]
        [ValidateRangeAttribute (1, int.MaxValue)]
        public int Column
        {
            get
            {
                if (column.HasValue)
                    return column.Value;
                return -1;
            }
            set { column = value; }
        }
        private Nullable<int> column = null;

        #endregion

        internal override FormattingCommandLineParameters GetCommandLineParameters ()
        {
            FormattingCommandLineParameters parameters = new FormattingCommandLineParameters ();

            if (this.prop != null)
            {
                ParameterProcessor processor = new ParameterProcessor (new FormatWideParameterDefinition());
                TerminatingErrorContext invocationContext = new TerminatingErrorContext (this);
                parameters.mshParameterList = processor.ProcessParameters (new object[] { prop }, invocationContext);
            }

            if (!string.IsNullOrEmpty (this.View))
            {
                // we have a view command line switch
                if (parameters.mshParameterList.Count != 0)
                {
                    ReportCannotSpecifyViewAndProperty ();
                }
                parameters.viewName = this.View;
            }

            // we cannot specify -column and -autosize, they are mutually exclusive
            if (this.autosize.HasValue && this.column.HasValue)
            {
                if (this.autosize.Value)
                {
                    // the user specified -autosize:true AND a column number
                    string msg = StringUtil.Format(FormatAndOut_format_xxx.CannotSpecifyAutosizeAndColumnsError);

                    
                    ErrorRecord errorRecord = new ErrorRecord (
                        new InvalidDataException (),
                        "FormatCannotSpecifyAutosizeAndColumns",
                        ErrorCategory.InvalidArgument,
                        null);

                    errorRecord.ErrorDetails = new ErrorDetails (msg);
                    this.ThrowTerminatingError (errorRecord);
                }
            }

            parameters.groupByParameter = this.ProcessGroupByParameter ();
            parameters.forceFormattingAlsoOnOutOfBand = this.Force;
            if (this.showErrorsAsMessages.HasValue)
                parameters.showErrorsAsMessages = this.showErrorsAsMessages;
            if (this.showErrorsInFormattedOutput.HasValue)
                parameters.showErrorsInFormattedOutput = this.showErrorsInFormattedOutput;

            parameters.expansion = ProcessExpandParameter ();

            if (this.autosize.HasValue)
                parameters.autosize = this.autosize.Value;

            WideSpecificParameters wideSpecific = new WideSpecificParameters ();
            parameters.shapeParameters = wideSpecific;
            if (this.column.HasValue)
            {
                wideSpecific.columns = this.column.Value;
            }
            return parameters;
        }
    }
}

