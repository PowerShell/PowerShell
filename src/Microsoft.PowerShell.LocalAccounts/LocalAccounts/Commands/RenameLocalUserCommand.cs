// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives
using System;
using System.Management.Automation;

using System.Management.Automation.SecurityAccountsManager;
using System.Management.Automation.SecurityAccountsManager.Extensions;

using Microsoft.PowerShell.LocalAccounts;
#endregion

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The Rename-LocalUser cmdlet renames a local user account in the Security
    /// Accounts Manager.
    /// </summary>
    [Cmdlet(VerbsCommon.Rename, "LocalUser",
            SupportsShouldProcess = true,
            HelpUri = "https://go.microsoft.com/fwlink/?LinkID=717983")]
    [Alias("rnlu")]
    public class RenameLocalUserCommand : Cmdlet
    {
        #region Instance Data
        private Sam sam = null;
        #endregion Instance Data

        #region Parameter Properties
        /// <summary>
        /// The following is the definition of the input parameter "InputObject".
        /// Specifies the of the local user account to rename in the local Security
        /// Accounts Manager.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = "InputObject")]
        [ValidateNotNull]
        public Microsoft.PowerShell.Commands.LocalUser InputObject
        {
            get { return this.inputobject; }

            set { this.inputobject = value; }
        }

        private Microsoft.PowerShell.Commands.LocalUser inputobject;

        /// <summary>
        /// The following is the definition of the input parameter "Name".
        /// Specifies the local user account to be renamed in the local Security
        /// Accounts Manager.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = "Default")]
        [ValidateNotNullOrEmpty]
        public string Name
        {
            get { return this.name; }

            set { this.name = value; }
        }

        private string name;

        /// <summary>
        /// The following is the definition of the input parameter "NewName".
        /// Specifies the new name for the local user account in the Security Accounts
        /// Manager.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 1)]
        [ValidateNotNullOrEmpty]
        public string NewName
        {
            get { return this.newname; }

            set { this.newname = value; }
        }

        private string newname;

        /// <summary>
        /// The following is the definition of the input parameter "SID".
        /// Specifies the local user to rename.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = "SecurityIdentifier")]
        [ValidateNotNull]
        public System.Security.Principal.SecurityIdentifier SID
        {
            get { return this.sid; }

            set { this.sid = value; }
        }

        private System.Security.Principal.SecurityIdentifier sid;
        #endregion Parameter Properties

        #region Cmdlet Overrides
        /// <summary>
        /// BeginProcessing method.
        /// </summary>
        protected override void BeginProcessing()
        {
            sam = new Sam();
        }

        /// <summary>
        /// ProcessRecord method.
        /// </summary>
        protected override void ProcessRecord()
        {
            try
            {
                ProcessUser();
                ProcessName();
                ProcessSid();
            }
            catch (Exception ex)
            {
                WriteError(ex.MakeErrorRecord());
            }
        }

        /// <summary>
        /// EndProcessing method.
        /// </summary>
        protected override void EndProcessing()
        {
            if (sam != null)
            {
                sam.Dispose();
                sam = null;
            }
        }
        #endregion Cmdlet Overrides

        #region Private Methods
        /// <summary>
        /// Process user requested by -Name.
        /// </summary>
        /// <remarks>
        /// Arguments to -Name will be treated as names,
        /// even if a name looks like a SID.
        /// </remarks>
        private void ProcessName()
        {
            if (Name != null)
            {
                try
                {
                    if (CheckShouldProcess(Name, NewName))
                        sam.RenameLocalUser(sam.GetLocalUser(Name), NewName);
                }
                catch (Exception ex)
                {
                    WriteError(ex.MakeErrorRecord());
                }
            }
        }

        /// <summary>
        /// Process user requested by -SID.
        /// </summary>
        private void ProcessSid()
        {
            if (SID != null)
            {
                try
                {
                    if (CheckShouldProcess(SID.ToString(), NewName))
                        sam.RenameLocalUser(SID, NewName);
                }
                catch (Exception ex)
                {
                    WriteError(ex.MakeErrorRecord());
                }
            }
        }

        /// <summary>
        /// Process group given through -InputObject.
        /// </summary>
        private void ProcessUser()
        {
            if (InputObject != null)
            {
                try
                {
                    if (CheckShouldProcess(InputObject.Name, NewName))
                        sam.RenameLocalUser(InputObject, NewName);
                }
                catch (Exception ex)
                {
                    WriteError(ex.MakeErrorRecord());
                }
            }
        }

        /// <summary>
        /// Determine if a user should be processed.
        /// Just a wrapper around Cmdlet.ShouldProcess, with localized string
        /// formatting.
        /// </summary>
        /// <param name="userName">
        /// Name of the user to rename.
        /// </param>
        /// <param name="newName">
        /// New name for the user.
        /// </param>
        /// <returns>
        /// True if the user should be processed, false otherwise.
        /// </returns>
        private bool CheckShouldProcess(string userName, string newName)
        {
            string msg = StringUtil.Format(Strings.ActionRenameUser, newName);

            return ShouldProcess(userName, msg);
        }
        #endregion Private Methods
    }

}
