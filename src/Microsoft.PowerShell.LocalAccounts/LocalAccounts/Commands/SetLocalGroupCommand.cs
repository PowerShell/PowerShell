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
    /// The Set-LocalGroup cmdlet modifies the properties of a local security group
    /// in the Windows Security Accounts Manager.
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "LocalGroup",
            SupportsShouldProcess = true,
            HelpUri = "https://go.microsoft.com/fwlink/?LinkId=717979")]
    [Alias("slg")]
    public class SetLocalGroupCommand : Cmdlet
    {
        #region Instance Data
        private Sam sam = null;
        #endregion Instance Data

        #region Parameter Properties
        /// <summary>
        /// The following is the definition of the input parameter "Description".
        /// A descriptive comment.
        /// </summary>
        [Parameter(Mandatory = true)]
        [ValidateNotNull]
        public string Description
        {
            get { return this.description;}

            set { this.description = value; }
        }

        private string description;

        /// <summary>
        /// The following is the definition of the input parameter "InputObject".
        /// Specifies the local group account to modify in the local Security
        /// Accounts Manager.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = "InputObject")]
        [ValidateNotNull]
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
        [ValidateNotNull]
        public string Name
        {
            get { return this.name;}

            set { this.name = value; }
        }

        private string name;

        /// <summary>
        /// The following is the definition of the input parameter "SID".
        /// Specifies a security group from the local Security Accounts Manager.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = "SecurityIdentifier")]
        [ValidateNotNull]
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
                LocalGroup group = null;

                if (InputObject != null)
                {
                    if (CheckShouldProcess(InputObject.ToString()))
                        group = InputObject;
                }
                else if (Name != null)
                {
                    group = sam.GetLocalGroup(Name);

                    if (!CheckShouldProcess(Name))
                        group = null;
                }
                else if (SID != null)
                {
                    group = sam.GetLocalGroup(SID);

                    if (!CheckShouldProcess(SID.ToString()))
                        group = null;
                }

                if (group != null)
                {
                    var delta = group.Clone();

                    delta.Description = Description;
                    sam.UpdateLocalGroup(group, delta);
                }
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
        private bool CheckShouldProcess(string target)
        {
            return ShouldProcess(target, Strings.ActionSetGroup);
        }
        #endregion Private Methods
    }

}

