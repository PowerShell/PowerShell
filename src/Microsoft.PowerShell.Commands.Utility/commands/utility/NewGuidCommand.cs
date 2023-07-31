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
        /// Generates an instance of the Guid structure whose value is all zeros.
        /// </summary>
        [Parameter(ParameterSetName = "Empty")]
        public SwitchParameter Empty { get; set; }

        /// <summary>
        /// Converts a string to a Guid.
        /// </summary>
        [Parameter(Position = 0, ValueFromPipeline = true, ParameterSetName = "FromString")]
        [System.Diagnostics.CodeAnalysis.AllowNull]
        public string FromString { get; set; }

        /// <summary>
        /// Returns a Guid.
        /// </summary>
        protected override void EndProcessing()
        {
            Guid guid;

            if (ParameterSetName is "FromString")
            {
                try
                {
                    guid = new(FromString);
                }
                catch (Exception ex)
                {
                    ErrorRecord error = new(ex, "StringNotRecognizedAsGuid", ErrorCategory.InvalidArgument, null);
                    ThrowTerminatingError(error);
                }  
            }

            guid = ParameterSetName is "Empty" ? Guid.Empty : Guid.NewGuid();

            WriteObject(guid);
        }
    }
}
