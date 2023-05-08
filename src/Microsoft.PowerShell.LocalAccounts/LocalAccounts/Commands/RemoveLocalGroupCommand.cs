// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives
using System;
using System.Management.Automation;

using System.Management.Automation.SecurityAccountsManager;
using System.Management.Automation.SecurityAccountsManager.Extensions;

using Microsoft.PowerShell.LocalAccounts;
using System.Diagnostics.CodeAnalysis;
#endregion

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The Remove-LocalGroup cmdlet deletes a security group from the Windows
    /// Security Accounts manager.
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "LocalGroup",
            SupportsShouldProcess = true,
            HelpUri = "https://go.microsoft.com/fwlink/?LinkId=717975")]
    [Alias("rlg")]
    public class RemoveLocalGroupCommand : Cmdlet
    {
        #region Instance Data
        private Sam sam = null;
        #endregion Instance Data

        #region Parameter Properties
        /// <summary>
        /// The following is the definition of the input parameter "InputObject".
        /// Specifies security groups from the local Security Accounts Manager.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = "InputObject")]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public Microsoft.PowerShell.Commands.LocalGroup[] InputObject
        {
            get { return this.inputobject; }

            set { this.inputobject = value; }
        }

        private Microsoft.PowerShell.Commands.LocalGroup[] inputobject;

        /// <summary>
        /// The following is the definition of the input parameter "Name".
        /// Specifies the local groups to be deleted from the local Security Accounts
        /// Manager.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = "Default")]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] Name
        {
            get { return this.name; }

            set { this.name = value; }
        }

        private string[] name;

        /// <summary>
        /// The following is the definition of the input parameter "SID".
        /// Specifies the LocalGroup accounts to remove by
        /// System.Security.Principal.SecurityIdentifier.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = "SecurityIdentifier")]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public System.Security.Principal.SecurityIdentifier[] SID
        {
            get { return this.sid; }

            set { this.sid = value; }
        }

        private System.Security.Principal.SecurityIdentifier[] sid;
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
                ProcessGroups();
                ProcessNames();
                ProcessSids();
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
        /// Process groups requested by -Name.
        /// </summary>
        /// <remarks>
        /// All arguments to -Name will be treated as names,
        /// even if a name looks like a SID.
        /// </remarks>
        private void ProcessNames()
        {
            if (Name != null)
            {
                foreach (var name in Name)
                {
                    try
                    {
                        if (CheckShouldProcess(name))
                            sam.RemoveLocalGroup(sam.GetLocalGroup(name));
                    }
                    catch (Exception ex)
                    {
                        WriteError(ex.MakeErrorRecord());
                    }
                }
            }
        }

        /// <summary>
        /// Process groups requested by -SID.
        /// </summary>
        private void ProcessSids()
        {
            if (SID != null)
            {
                foreach (var sid in SID)
                {
                    try
                    {
                        if (CheckShouldProcess(sid.ToString()))
                            sam.RemoveLocalGroup(sid);
                    }
                    catch (Exception ex)
                    {
                        WriteError(ex.MakeErrorRecord());
                    }
                }
            }
        }

        /// <summary>
        /// Process groups given through -InputObject.
        /// </summary>
        private void ProcessGroups()
        {
            if (InputObject != null)
            {
                foreach (var group in InputObject)
                {
                    try
                    {
                        if (CheckShouldProcess(group.Name))
                            sam.RemoveLocalGroup(group);
                    }
                    catch (Exception ex)
                    {
                        WriteError(ex.MakeErrorRecord());
                    }
                }
            }
        }

        private bool CheckShouldProcess(string target)
        {
            return ShouldProcess(target, Strings.ActionRemoveGroup);
        }
        #endregion Private Methods
    }

}

