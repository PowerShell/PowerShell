// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    #region WriteOutputCommand
    /// <summary>
    /// This class implements Write-Output command.
    /// </summary>
    [Cmdlet(VerbsCommunications.Write, "Output", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113427", RemotingCapability = RemotingCapability.None)]
    public sealed class WriteOutputCommand : PSCmdlet
    {
        private PSObject[] _inputObjects = null;

        /// <summary>
        /// Holds the list of objects to be written.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ValueFromRemainingArguments = true)]
        [AllowNull]
        [AllowEmptyCollection]
        public PSObject[] InputObject
        {
            get { return _inputObjects; }
            set { _inputObjects = value; }
        }

        /// <summary>
        /// Prevents Write-Output from unravelling collections passed to the InputObject parameter.
        /// </summary>
        [Parameter()]
        public SwitchParameter NoEnumerate
        {
            get;
            set;
        }

        /// <summary>
        /// This method implements the ProcessRecord method for Write-output command.
        /// </summary>
        protected override void ProcessRecord()
        {
            if (_inputObjects == null)
            {
                WriteObject(_inputObjects);
                return;
            }

            bool enumerate = true;
            if (NoEnumerate.IsPresent)
            {
                enumerate = false;
            }

            WriteObject(_inputObjects, enumerate);
        }
    }
    #endregion
}
