using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text.RegularExpressions;

namespace Microsoft.PowerShell.Commands.Utility.commands.utility
{
    /// <summary>
    /// Implements the Split-String command
    /// </summary>
    [Cmdlet(VerbsCommon.Split, "String", DefaultParameterSetName = "regex")]
    [OutputType(typeof(string))]
    [Alias("split")]
    public class SplitStringCommand : Cmdlet
    {
        #region Parameters
        /// <summary>
        /// What to split by
        /// </summary>
        [Parameter(Position = 1)]
        [Alias("with")]
        public string Separator;

        /// <summary>
        /// Whether the simple string Replace() method should be used instead
        /// </summary>
        [Parameter(ParameterSetName = "simple")]
        [Alias("simple")]
        public SwitchParameter DoNotUseRegex;

        /// <summary>
        /// The regex options to apply while replacing
        /// </summary>
        [Parameter(ParameterSetName = "regex")]
        public RegexOptions Options = RegexOptions.IgnoreCase;

        /// <summary>
        /// The maximum number of items to split into.
        /// </summary>
        [Parameter()]
        public int Count;

        /// <summary>
        /// The strings to update
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        public string[] InputString;
        #endregion Parameters

        #region Methods
        /// <summary>
        /// Processes the items to split
        /// </summary>
        protected override void ProcessRecord()
        {
            foreach (string item in InputString)
            {
                if (DoNotUseRegex.ToBool())
                    if (Count > 0)
                        WriteObject(item.Split(Separator, Count), true);
                    else
                        WriteObject(item.Split(Separator), true);
                else
                {
                    if (Count < 1)
                        WriteObject(Regex.Split(item, Separator, Options), true);
                    else
                    {
                        Regex regex = new Regex(Separator, Options);
                        WriteObject(regex.Split(item, Count), true);
                    }
                }
            }
        }
        #endregion Methods
    }
}
