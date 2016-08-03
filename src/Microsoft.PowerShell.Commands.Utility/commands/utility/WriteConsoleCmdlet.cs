/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Management.Automation;
using System.Collections;
using System.Text;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    ///
    /// Class comment
    ///
    /// </summary>

    [Cmdlet("Write", "Host", HelpUri = "http://go.microsoft.com/fwlink/?LinkID=113426", RemotingCapability = RemotingCapability.None)]
    public sealed class WriteHostCommand : ConsoleColorCmdlet
    {
        //
        // Parameters
        //



        /// <summary>
        ///
        /// Object to be output.
        ///
        /// </summary>

        [Parameter(Position = 0, ValueFromRemainingArguments = true, ValueFromPipeline = true)]
        public object Object { get; set; } = null;


        /// <summary>
        ///
        /// False to add a newline to the end of the output string, true if not.
        ///
        /// </summary>

        [Parameter]
        public SwitchParameter NoNewline
        {
            get
            {
                return _notAppendNewline;
            }
            set
            {
                _notAppendNewline = value;
            }
        }



        /// <summary>
        /// 
        /// The separator to print between objects
        /// 
        /// </summary>
        /// <value></value>

        [Parameter]
        public object Separator { get; set; } = " ";


        //
        // Cmdlet Overrides
        //

        private string ProcessObject(object o)
        {
            if (o != null)
            {
                string s = o as string;
                IEnumerable enumerable = null;
                if (s != null)
                {
                    // strings are IEnumerable, so we special case them
                    if (s.Length > 0)
                    {
                        return s;
                    }
                }
                else if ((enumerable = o as IEnumerable) != null)
                {
                    // unroll enumerables, including arrays.

                    bool printSeparator = false;
                    StringBuilder result = new StringBuilder();

                    foreach (object element in enumerable)
                    {
                        if (printSeparator == true && Separator != null)
                        {
                            result.Append(Separator.ToString());
                        }

                        result.Append(ProcessObject(element));
                        printSeparator = true;
                    }

                    return result.ToString();
                }
                else
                {
                    s = o.ToString();

                    if (s.Length > 0)
                    {
                        return s;
                    }
                }
            }

            return null;
        }



        /// <summary>
        ///
        /// Outputs the object to the host console, with optional newline
        ///
        /// </summary>
        protected override void ProcessRecord()
        {
            string result = ProcessObject(Object) ?? "";

            HostInformationMessage informationMessage = new HostInformationMessage();
            informationMessage.Message = result;
            informationMessage.NoNewLine = NoNewline.IsPresent;

            try
            {
                informationMessage.ForegroundColor = ForegroundColor;
                informationMessage.BackgroundColor = BackgroundColor;
            }
            catch (System.Management.Automation.Host.HostException)
            {
                // Expected if the host is not interactive, or doesn't have Foreground / Background
                // colours.
            }

            this.WriteInformation(informationMessage, new string[] { "PSHOST" });
            this.Host.UI.TranscribeResult(result);
        }

        private Boolean _notAppendNewline = false;
    }
}   // namespace Microsoft.PowerShell.Commands