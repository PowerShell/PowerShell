// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
        [Parameter]
        public SwitchParameter Empty { get; set; }

        /// <summary>
        /// Converts a string to a Guid.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrWhiteSpace]
        public string FromString { get; set; }

        /// <summary>
        /// Returns a Guid.
        /// </summary>
        protected override void EndProcessing()
        {
            if (Empty.IsPresent && FromString is not null)
            {
                ValidationMetadataException ex = new("The cmdlet cannot run because the following conflicting parameters are specified: Empty and FromString.");
                ErrorRecord error = new(ex, "NewGuidParameterException", ErrorCategory.InvalidArgument, this);
                ThrowTerminatingError(error);
            }

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
