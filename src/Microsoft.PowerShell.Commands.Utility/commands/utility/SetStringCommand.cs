using System;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Text.RegularExpressions;

namespace Microsoft.PowerShell.Commands.Utility.commands.utility
{
    /// <summary>
    /// Implements the Set-String command
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "String", DefaultParameterSetName = "regex")]
    [OutputType(typeof(string))]
    [Alias("replace")]
    public class SetStringCommand : PSCmdlet
    {
        #region Parameters
        /// <summary>
        /// What to replace
        /// </summary>
        [Parameter(Position = 0, Mandatory = true)]
        public string What;
        
        /// <summary>
        /// What to replace with
        /// </summary>
        [Parameter(Position = 1)]
        public object With;

        /// <summary>
        /// Whether the simple string Replace() method should be used instead
        /// </summary>
        [Parameter(ParameterSetName = "simple")]
        public SwitchParameter Simple;

        /// <summary>
        /// The regex options to apply while replacing
        /// </summary>
        [Parameter(ParameterSetName = "regex")]
        public RegexOptions Options = RegexOptions.IgnoreCase;

        /// <summary>
        /// The strings to update
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        public string[] InputString;
        #endregion Parameters

        #region Fields
        /// <summary>
        /// Stores the string value of the With parameter, if applicable.
        /// </summary>
        private string StringValue;

        /// <summary>
        /// Stores the scriptblock value of the With parameter, if applicable.
        /// </summary>
        private ScriptBlock ScriptBlockValue;

        /// <summary>
        /// Evaluates the replace match.
        /// </summary>
        private MatchEvaluator ScriptBlockEvaluator;
        #endregion Fields

        #region Methods
        /// <summary>
        /// Handles the input conversion on the What parameter
        /// </summary>
        protected override void BeginProcessing()
        {
            if (With == null)
                StringValue = "";
            else if (!Simple.ToBool() && (With is ScriptBlock))
            {
                ScriptBlockValue = (ScriptBlock)With;
                ScriptBlockEvaluator = match => {
                    var result = ScriptBlockValue.DoInvokeReturnAsIs(
                        useLocalScope: false, /* Use current scope to be consistent with 'ForEach/Where-Object {}' and 'collection.ForEach{}/Where{}' */
                        errorHandlingBehavior: ScriptBlock.ErrorHandlingBehavior.WriteToCurrentErrorPipe,
                        dollarUnder: match,
                        input: AutomationNull.Value,
                        scriptThis: AutomationNull.Value,
                        args: Utils.EmptyArray<object>());

                    return PSObject.ToStringParser(Context, result); ;
                };
            }
            else
                StringValue = With.ToString();
        }

        /// <summary>
        /// Processes each input item
        /// </summary>
        protected override void ProcessRecord()
        {
            foreach (string item in InputString)
            {
                if (Simple.ToBool())
                    WriteObject(item.Replace(What, StringValue));
                else if (ScriptBlockValue != null)
                    WriteObject(Regex.Replace(item, What, ScriptBlockEvaluator, Options));
                else
                    WriteObject(Regex.Replace(item, What, StringValue, Options));
            }
        }
        #endregion Methods
    }
}
