// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The implementation of the "new-guid" cmdlet.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "Guid", DefaultParameterSetName = "Default", HelpUri = "https://go.microsoft.com/fwlink/?LinkId=2097130")]
    [OutputType(typeof(Guid))]
    public class NewGuidCommand : PSCmdlet
    {
        /// <summary>
        /// Gets or sets a value indicating that the cmdlet should return a Guid structure whose value is all zeros.
        /// </summary>
        [Parameter(ParameterSetName = "Empty")]
        public SwitchParameter Empty { get; set; }

        /// <summary>
        /// Gets or sets the value to be converted to a Guid.
        /// </summary>
        [Parameter(Position = 0, ValueFromPipeline = true, ParameterSetName = "InputObject")]
        [System.Diagnostics.CodeAnalysis.AllowNull]
        public string InputObject { get; set; }

        /// <summary>
        /// Returns a Guid.
        /// </summary>
        protected override void ProcessRecord()
        {
            Guid? guid = null;

            if (ParameterSetName is "InputObject")
            {
                try
                {
                    guid = new(InputObject);
                }
                catch (Exception ex)
                {
                    ErrorRecord error = new(ex, "StringNotRecognizedAsGuid", ErrorCategory.InvalidArgument, null);
                    WriteError(error);
                }
            }
            else
            {
                guid = Empty.ToBool() ? Guid.Empty : Guid.NewGuid();
            }

            WriteObject(guid);
        }
    }
}
