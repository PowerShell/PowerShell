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
    /// The Rename-LocalGroup cmdlet renames a local security group in the Security
    /// Accounts Manager.
    /// </summary>
    [Cmdlet(VerbsCommon.Rename, "LocalGroup",
            SupportsShouldProcess = true,
            HelpUri = "https://go.microsoft.com/fwlink/?LinkId=717978")]
    [Alias("rnlg")]
    public class RenameLocalGroupCommand : Cmdlet
    {
        #region Instance Data
        private Sam sam = null;
        #endregion Instance Data

        #region Parameter Properties
        /// <summary>
        /// The following is the definition of the input parameter "InputObject".
        /// Specifies the of the local group account to rename in the local Security
        /// Accounts Manager.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = "InputObject")]
        [ValidateNotNullOrEmpty]
        public Microsoft.PowerShell.Commands.LocalGroup InputObject
        {
            get { return this.inputobject;}

            set { this.inputobject = value; }
        }

        private Microsoft.PowerShell.Commands.LocalGroup inputobject;

        /// <summary>
        /// The following is the definition of the input parameter "Name".
        /// Specifies the local group to be renamed in the local Security Accounts
        /// Manager.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = "Default")]
        [ValidateNotNullOrEmpty]
        public string Name
        {
            get { return this.name;}

            set { this.name = value; }
        }

        private string name;

        /// <summary>
        /// The following is the definition of the input parameter "NewName".
        /// Specifies the new name for the local security group in the Security Accounts
        /// Manager.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 1)]
        [ValidateNotNullOrEmpty]
        public string NewName
        {
            get { return this.newname;}

            set { this.newname = value; }
        }

        private string newname;

        /// <summary>
        /// The following is the definition of the input parameter "SID".
        /// Specifies a security group from the local Security Accounts Manager.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = "SecurityIdentifier")]
        [ValidateNotNullOrEmpty]
        public System.Security.Principal.SecurityIdentifier SID
        {
            get { return this.sid;}

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
                ProcessGroup();
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
        /// Process group requested by -Name.
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
                        sam.RenameLocalGroup(sam.GetLocalGroup(Name), NewName);
                }
                catch (Exception ex)
                {
                    WriteError(ex.MakeErrorRecord());
                }
            }
        }

        /// <summary>
        /// Process group requested by -SID.
        /// </summary>
        private void ProcessSid()
        {
            if (SID != null)
            {
                try
                {
                    if (CheckShouldProcess(SID.ToString(), NewName))
                        sam.RenameLocalGroup(SID, NewName);
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
        private void ProcessGroup()
        {
            if (InputObject != null)
            {
                try
                {
                    if (CheckShouldProcess(InputObject.Name, NewName))
                        sam.RenameLocalGroup(InputObject, NewName);
                }
                catch (Exception ex)
                {
                    WriteError(ex.MakeErrorRecord());
                }
            }
        }

        /// <summary>
        /// Determine if a group should be processed.
        /// Just a wrapper around Cmdlet.ShouldProcess, with localized string
        /// formatting.
        /// </summary>
        /// <param name="groupName">
        /// Name of the group to rename.
        /// </param>
        /// <param name="newName">
        /// New name for the group.
        /// </param>
        /// <returns>
        /// True if the group should be processed, false otherwise.
        /// </returns>
        private bool CheckShouldProcess(string groupName, string newName)
        {
            string msg = StringUtil.Format(Strings.ActionRenameGroup, newName);

            return ShouldProcess(groupName, msg);
        }
        #endregion Private Methods
    }

}
