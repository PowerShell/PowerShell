// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    #region WriteOutputCommand
    /// <summary>
    /// This class implements Write-Output command.
    /// </summary>
    [Cmdlet(VerbsCommunications.Write, "Output", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097117", RemotingCapability = RemotingCapability.None)]
    public sealed class WriteOutputCommand : PSCmdlet
    {
        /// <summary>
        /// Holds the list of objects to be written.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ValueFromRemainingArguments = true)]
        [AllowNull]
        [AllowEmptyCollection]
        public PSObject InputObject { get; set; }

        /// <summary>
        /// Prevents Write-Output from unravelling collections passed to the InputObject parameter.
        /// </summary>
        [Parameter]
        public SwitchParameter NoEnumerate { get; set; }

        /// <summary>
        /// This method implements the ProcessRecord method for Write-output command.
        /// </summary>
        protected override void ProcessRecord()
        {
            if (InputObject == null)
            {
                WriteObject(InputObject);
                return;
            }

            WriteObject(InputObject, !NoEnumerate.IsPresent);
        }
    }
    #endregion
}
