// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.DirectoryServices.AccountManagement;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The Remove-User cmdlet creates a new local user account.
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "User",
            SupportsShouldProcess = true,
            HelpUri = "")]
    public class RemoveUserCommand : PSCmdlet
    {
        /// <summary>
        /// Gets or sets an user identity to remove.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        public UserPrincipal Identity
        {
            get;
            set;
        }

        /// <summary>
        /// ProcessRecord method.
        /// </summary>
        protected override void ProcessRecord()
        {
            try
            {
                if (ShouldProcess(Identity.ToString(), NewUserStrings.ActionNewUser))
                {
                    Identity.Delete();
                }
            }
            catch (UnauthorizedAccessException exc)
            {
                WriteError(new ErrorRecord(exc, "RemoveUserFailure", ErrorCategory.PermissionDenied, Identity));
            }
            catch (InvalidOperationException exc)
            {
                WriteError(new ErrorRecord(exc, "UserAlreadyRemoved", ErrorCategory.InvalidOperation, Identity));
            }
        }
    }
}
