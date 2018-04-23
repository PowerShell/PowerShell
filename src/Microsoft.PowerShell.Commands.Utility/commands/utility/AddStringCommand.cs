using System;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands.Utility.commands.utility
{
    /// <summary>
    /// Implementation of the Add-String command
    /// </summary>
    [Cmdlet(VerbsCommon.Add, "String", DefaultParameterSetName = "wrap")]
    [OutputType(typeof(string))]
    [Alias("wrap")]
    public sealed class AddStringCommand : Cmdlet
    {
        #region Parameters
        /// <summary>
        /// The character to pad the input string with on the left side
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "padLeft")]
        public char PadLeft;

        /// <summary>
        /// The character to pad the input string with on the right side
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "padRight")]
        public char PadRight;

        /// <summary>
        /// To what total string width to pad
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "padLeft")]
        [Parameter(Mandatory = true, ParameterSetName = "padRight")]
        public int PadWidth;

        /// <summary>
        /// The string to add before the input
        /// </summary>
        [Parameter(Position = 0, ParameterSetName = "wrap")]
        public string Before;

        /// <summary>
        /// The string to add behind the input
        /// </summary>
        [Parameter(Position = 1, ParameterSetName = "wrap")]
        public string Behind;

        /// <summary>
        /// The string to add to
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        public string[] InputString;
        #endregion Parameters

        #region Methods
        /// <summary>
        /// Process each string as it is passed through.
        /// </summary>
        protected override void ProcessRecord()
        {
            foreach (string line in InputString)
                if (_ParameterSetName == "wrap")
                    WriteObject(String.Format("{0}{1}{2}", Before, line, Behind));
                else if (_ParameterSetName == "padRight")
                    WriteObject(line.PadRight(PadWidth, PadRight));
                else
                    WriteObject(line.PadLeft(PadWidth, PadLeft));
        }
        #endregion Methods
    }
}
