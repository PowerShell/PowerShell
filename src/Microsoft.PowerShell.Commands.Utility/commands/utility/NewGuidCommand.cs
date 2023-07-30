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
    [Cmdlet(VerbsCommon.New, "Guid", HelpUri = "https://go.microsoft.com/fwlink/?LinkId=2097130")]
    [OutputType(typeof(Guid))]
    public class NewGuidCommand : Cmdlet
    {
        /// <summary>
        /// Generates an instance of the Guid structure whose value is all zeros.
        /// </summary>
        [Parameter(ParameterSetName = "Empty")]
        public SwitchParameter Empty { get; set; }

        /// <summary>
        /// Converts a string to a Guid.
        /// </summary>
        [Parameter(ParameterSetName = "FromString")]
        [ValidateNotNullOrWhiteSpace]
        public string? FromString { get; set; }

        /// <summary>
        /// Returns a Guid.
        /// </summary>
        protected override void EndProcessing()
        {
            Guid guid = Empty.IsPresent ? Guid.Empty : Guid.NewGuid();

            if (FromString is not null)
            {
                try
                {
                    guid = new(FromString);
                }
                catch (Exception ex)
                {
                    ErrorRecord error = new(ex, "StringNotRecognizedAsGuid", ErrorCategory.InvalidOperation, null);
                    ThrowTerminatingError(error);
                }  
            }

            WriteObject(guid);
        }
    }
}
