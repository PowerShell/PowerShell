// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// A cmdlet that gets the TraceSource instances that are instantiated in the process.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "TraceSource", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096707")]
    [OutputType(typeof(PSTraceSource))]
    public class GetTraceSourceCommand : TraceCommandBase
    {
        #region Parameters

        /// <summary>
        /// Gets or sets the category parameter which determines which trace switch to get.
        /// </summary>
        /// <value></value>
        [Parameter(Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty()]
        public string[] Name
        {
            get
            {
                return _names;
            }

            set
            {
                if (value == null || value.Length == 0)
                {
                    value = new string[] { "*" };
                }

                _names = value;
            }
        }

        private string[] _names = new string[] { "*" };

        #endregion Parameters

        #region Cmdlet code

        /// <summary>
        /// Gets the PSTraceSource for the specified category.
        /// </summary>
        protected override void ProcessRecord()
        {
            var sources = GetMatchingTraceSource(_names, true);
            var result = sources.OrderBy(static source => source.Name);
            WriteObject(result, true);
        }

        #endregion Cmdlet code
    }
}
