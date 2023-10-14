// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// This class implements Get-PSCallStack.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PSCallStack", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096705")]
    [OutputType(typeof(CallStackFrame))]
    public class GetPSCallStackCommand : PSCmdlet
    {
        /// <summary>
        /// Gets or sets the number of items to return.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName=true)]
        public uint First { get; set; } = 0;

        /// <summary>
        /// Gets or sets the number of items to skip.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName=true)]
        public uint Skip { get; set; } = 0;

        /// <summary>
        /// Get the call stack.
        /// </summary>
        protected override void ProcessRecord()
        {
            uint skipped = 0;
            uint count   = 0;            
            foreach (CallStackFrame frame in Context.Debugger.GetCallStack())
            {
                if (this.Skip > 0 && skipped < this.Skip)
                {
                    skipped++;
                    continue;
                }

                if (this.First > 0)
                {
                    if (count >= this.First)
                    {
                        break;
                    }
                    count++;
                    WriteObject(frame);    
                }
                else
                {
                    WriteObject(frame);
                }
            }
        }
    }
}
