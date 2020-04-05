// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.SecurityAccountsManager;
using System.Management.Automation.SecurityAccountsManager.Extensions;
using System.Security.Principal;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The Get-LocalGroupMember cmdlet gets the members of a local group.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "LocalGroupMember",
            DefaultParameterSetName = "Default",
            HelpUri = "https://go.microsoft.com/fwlink/?LinkId=717988")]
    [Alias("glgm")]
    public class GetLocalGroupMemberCommand : Cmdlet
    {
        #region Instance Data
        private Sam _sam = null;
        #endregion Instance Data

        #region Parameter Properties
        /// <summary>
        /// The following is the definition of the input parameter "Group".
        /// The security group from the local Security Accounts Manager.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = "Group")]
        [ValidateNotNull]
        public LocalGroup Group { get; set; }

        /// <summary>
        /// The following is the definition of the input parameter "Member".
        /// Specifies the name of the user or group that is a member of this group. If
        /// this parameter is not specified, all members of the specified group are
        /// returned. This accepts a name, SID, or wildcard string.
        /// </summary>
        [Parameter(Position = 1)]
        [ValidateNotNullOrEmpty]
        public string Member { get; set; }

        /// <summary>
        /// The following is the definition of the input parameter "Name".
        /// The security group from the local Security Accounts Manager.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = "Default")]
        [ValidateNotNullOrEmpty]
        public string Name { get; set; }

        /// <summary>
        /// The following is the definition of the input parameter "SID".
        /// The security group from the local Security Accounts Manager.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = "SecurityIdentifier")]
        [ValidateNotNullOrEmpty]
        public SecurityIdentifier SID { get; set; }

        #endregion Parameter Properties

        #region Cmdlet Overrides
        /// <summary>
        /// BeginProcessing method.
        /// </summary>
        protected override void BeginProcessing()
        {
            _sam = new Sam();
        }

        /// <summary>
        /// ProcessRecord method.
        /// </summary>
        protected override void ProcessRecord()
        {
            try
            {
                IEnumerable<LocalPrincipal> principals = null;

                if (Group != null)
                {
                    LocalGroup resolvedGroup;
                    if (Group.SID is null)
                    {
                        resolvedGroup = _sam.GetLocalGroup(Group.Name);
                    }
                    else
                    {
                        resolvedGroup = _sam.GetLocalGroup(Group.SID);
                    }

                    principals = ProcessGroup(resolvedGroup);
                }
                else if (Name != null)
                {
                    principals = ProcessName(Name);
                }
                else if (SID != null)
                {
                    principals = ProcessSid(SID);
                }

                if (principals != null)
                {
                    WriteObject(principals, true);
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
            if (_sam != null)
            {
                _sam.Dispose();
                _sam = null;
            }
        }
        #endregion Cmdlet Overrides

        #region Private Methods
        private IEnumerable<LocalPrincipal> ProcessesMembership(IEnumerable<LocalPrincipal> membership)
        {
            List<LocalPrincipal> rv;

            // if no members are specified, return all of them
            if (Member == null)
            {
                // return membership;
                rv = new List<LocalPrincipal>(membership);
            }
            else
            {
                // var rv = new List<LocalPrincipal>();
                rv = new List<LocalPrincipal>();

                if (WildcardPattern.ContainsWildcardCharacters(Member))
                {
                    var pattern = new WildcardPattern(Member, WildcardOptions.Compiled
                                                                | WildcardOptions.IgnoreCase);

                    foreach (LocalPrincipal m in membership)
                    {
                        if (pattern.IsMatch(_sam.StripMachineName(m.Name)))
                        {
                            rv.Add(m);
                        }
                    }
                }
                else
                {
                    SecurityIdentifier secureId = this.TrySid(Member);

                    if (secureId != null)
                    {
                        foreach (LocalPrincipal m in membership)
                        {
                            if (m.SID == secureId)
                            {
                                rv.Add(m);
                                break;
                            }
                        }
                    }
                    else
                    {
                        foreach (LocalPrincipal m in membership)
                        {
                            if (_sam.StripMachineName(m.Name).Equals(Member, StringComparison.CurrentCultureIgnoreCase))
                            {
                                rv.Add(m);
                                break;
                            }
                        }
                    }

                    if (rv.Count == 0)
                    {
                        var ex = new PrincipalNotFoundException(Member, Member);
                        WriteError(ex.MakeErrorRecord());
                    }
                }
            }

            // sort the resulting principals by mane
            rv.Sort((p1, p2) => string.Compare(p1.Name, p2.Name, StringComparison.CurrentCultureIgnoreCase));

            return rv;
        }

        private IEnumerable<LocalPrincipal> ProcessGroup(LocalGroup group)
        {
            return ProcessesMembership(_sam.GetLocalGroupMembers(group));
        }

        private IEnumerable<LocalPrincipal> ProcessName(string name)
        {
            return ProcessGroup(_sam.GetLocalGroup(name));
        }

        private IEnumerable<LocalPrincipal> ProcessSid(SecurityIdentifier groupSid)
        {
            return ProcessGroup(_sam.GetLocalGroup(groupSid));
        }
        #endregion Private Methods
    }
}
