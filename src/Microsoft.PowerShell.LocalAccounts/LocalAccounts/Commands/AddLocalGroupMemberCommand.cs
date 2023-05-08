// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives
using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Security.Principal;

using System.Management.Automation.SecurityAccountsManager;
using System.Management.Automation.SecurityAccountsManager.Extensions;

using Microsoft.PowerShell.LocalAccounts;
using System.Diagnostics.CodeAnalysis;
#endregion

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The Add-LocalGroupMember cmdlet adds one or more users or groups to a local
    /// group.
    /// </summary>
    [Cmdlet(VerbsCommon.Add, "LocalGroupMember",
            SupportsShouldProcess = true,
            HelpUri = "https://go.microsoft.com/fwlink/?LinkId=717987")]
    [Alias("algm")]
    public class AddLocalGroupMemberCommand : PSCmdlet
    {
        #region Instance Data
        private Sam sam = null;
        #endregion Instance Data

        #region Parameter Properties
        /// <summary>
        /// The following is the definition of the input parameter "Group".
        /// Specifies a security group from the local Security Accounts Manager.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ParameterSetName = "Group")]
        [ValidateNotNull]
        public Microsoft.PowerShell.Commands.LocalGroup Group
        {
            get { return this.group;}

            set { this.group = value; }
        }

        private Microsoft.PowerShell.Commands.LocalGroup group;

        /// <summary>
        /// The following is the definition of the input parameter "Member".
        /// Specifies one or more users or groups to add to this local group. You can
        /// identify users or groups by specifying their names or SIDs, or by passing
        /// Microsoft.PowerShell.Commands.LocalPrincipal objects.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 1,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public Microsoft.PowerShell.Commands.LocalPrincipal[] Member
        {
            get { return this.member;}

            set { this.member = value; }
        }

        private Microsoft.PowerShell.Commands.LocalPrincipal[] member;

        /// <summary>
        /// The following is the definition of the input parameter "Name".
        /// Specifies a security group from the local Security Accounts Manager.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ParameterSetName = "Default")]
        [ValidateNotNullOrEmpty]
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
                if (Group != null)
                    ProcessGroup(Group);
                else if (Name != null)
                    ProcessName(Name);
                else if (SID != null)
                    ProcessSid(SID);
            }
            catch (GroupNotFoundException ex)
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
        /// Creates a list of <see cref="LocalPrincipal"/> objects
        /// ready to be processed by the cmdlet.
        /// </summary>
        /// <param name="groupId">
        /// Name or SID (as a string) of the group we'll be adding to.
        /// This string is used primarily for specifying the target
        /// in WhatIf scenarios.
        /// </param>
        /// <param name="member">
        /// LocalPrincipal object to be processed
        /// </param>
        /// <returns>
        /// A LocalPrincipal Object to be added to the group
        /// </returns>
        /// <remarks>
        /// <para>
        /// LocalPrincipal objects in the Member parameter may not be complete,
        /// particularly those created from a name or a SID string given to the
        /// Member cmdlet parameter. The object returned from this method contains
        /// , at the very least, a valid SID.
        /// </para>
        /// <para>
        /// Any Member objects provided by name or SID string will be looked up
        /// to ensure that such an object exists. If an object is not found,
        /// an error message is displayed by PowerShell and null will be returned
        /// </para>
        /// <para>
        /// This method also handles the WhatIf scenario. If the Cmdlet's
        /// <b>ShouldProcess</b> method returns false on any Member object,
        /// that object will not be included in the returned List.
        /// </para>
        /// </remarks>
        private LocalPrincipal MakePrincipal(string groupId, LocalPrincipal member)
        {
            LocalPrincipal principal = null;
            // if the member has a SID, we can use it directly
            if (member.SID != null)
            {
                principal = member;
            }
            else    // otherwise it must have been constructed by name
            {
                SecurityIdentifier sid = this.TrySid(member.Name);

                if (sid != null)
                {
                    member.SID = sid;
                    principal = member;
                }
                else
                {
                    try
                    {
                        principal = sam.LookupAccount(member.Name);
                    }
                    catch (Exception ex)
                    {
                        WriteError(ex.MakeErrorRecord());
                    }
                }
            }

            if (CheckShouldProcess(principal, groupId))
                return principal;

            return null;
        }

        /// <summary>
        /// Determine if a principal should be processed.
        /// Just a wrapper around Cmdlet.ShouldProcess, with localized string
        /// formatting.
        /// </summary>
        /// <param name="principal">Name of the principal to be added.</param>
        /// <param name="groupName">
        /// Name of the group to which the members will be added.
        /// </param>
        /// <returns>
        /// True if the principal should be processed, false otherwise.
        /// </returns>
        private bool CheckShouldProcess(LocalPrincipal principal, string groupName)
        {
            if (principal == null)
                return false;

            string msg = StringUtil.Format(Strings.ActionAddGroupMember, principal.ToString());

            return ShouldProcess(groupName, msg);
        }

        /// <summary>
        /// Add members to a group.
        /// </summary>
        /// <param name="group">
        /// A <see cref="LocalGroup"/> object representing the group to which
        /// the members will be added.
        /// </param>
        private void ProcessGroup(LocalGroup group)
        {
            string groupId = group.Name ?? group.SID.ToString();
            foreach (var member in this.Member)
            {
                LocalPrincipal principal = MakePrincipal(groupId, member);
                if (principal != null)
                {
                    var ex = sam.AddLocalGroupMember(group, principal);
                    if (ex != null)
                    {
                        WriteError(ex.MakeErrorRecord());
                    }
                }
            }
        }

        /// <summary>
        /// Add members to a group specified by name.
        /// </summary>
        /// <param name="name">
        /// The name of the group to which the members will be added.
        /// </param>
        private void ProcessName(string name)
        {
            ProcessGroup(sam.GetLocalGroup(name));
        }

        /// <summary>
        /// Add members to a group specified by SID.
        /// </summary>
        /// <param name="groupSid">
        /// A <see cref="SecurityIdentifier"/> object identifying the group
        /// to which the members will be added.
        /// </param>
        private void ProcessSid(SecurityIdentifier groupSid)
        {
            foreach (var member in this.Member)
            {
                LocalPrincipal principal = MakePrincipal(groupSid.ToString(), member);
                if (principal != null)
                {
                    var ex = sam.AddLocalGroupMember(groupSid, principal);
                    if (ex != null)
                    {
                        WriteError(ex.MakeErrorRecord());
                    }
                }
            }
        }

        #endregion Private Methods
    }

}

