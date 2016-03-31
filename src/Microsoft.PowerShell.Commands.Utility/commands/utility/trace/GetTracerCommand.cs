#if !CORECLR
/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.Linq;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// A cmdlet that gets the TraceSource instances that are instantiated in the process
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "TraceSource", HelpUri = "http://go.microsoft.com/fwlink/?LinkID=113333")]
    [OutputType(typeof(PSTraceSource))]
    public class GetTraceSourceCommand : TraceCommandBase
    {
        #region Parameters

        /// <summary>
        /// Gets or sets the category parameter which determines
        /// which trace switch to get.
        /// </summary>
        /// <value></value>
        [Parameter (Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public string[] Name
        {
            get 
            { 
                return names; 
            }

            set
            {
                if (value == null || value.Length == 0)
                {
                    value = new string[] { "*" };
                }

                names = value;
            }
        } // TraceSource
        private string[] names = new string[] { "*" };

        #endregion Parameters

        #region Cmdlet code

        /// <summary>
        /// Gets the PSTraceSource for the specified category
        /// </summary>
        protected override void ProcessRecord ()
        {
            var sources = GetMatchingTraceSource(names, true);
            var result = sources.OrderBy(source => source.Name);
            WriteObject (result, true);
        } // ProcessRecord

        #endregion Cmdlet code
    }
}


#endif
