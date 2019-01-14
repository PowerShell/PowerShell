// Copyright (c) Microsoft Corporation. All rights reserved.
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
    /// The New-LocalGroup Cmdlet can be used to create a new local security group
    /// in the Windows Security Accounts Manager.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "LocalGroup",
            SupportsShouldProcess = true,
            HelpUri ="https://go.microsoft.com/fwlink/?LinkId=717990")]
    [Alias("nlg")]
    public class NewLocalGroupCommand : Cmdlet
    {
        #region Instance Data
        private Sam sam = null;
        #endregion Instance Data

        #region Parameter Properties
        /// <summary>
        /// The following is the definition of the input parameter "Description".
        /// A descriptive comment.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ValidateNotNull]
        public string Description
        {
            get { return this.description;}

            set { this.description = value; }
        }

        private string description;

        /// <summary>
        /// The following is the definition of the input parameter "Name".
        /// The group name for the local security group.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        [ValidateLength(1, 256)]
        public string Name
        {
            get { return this.name;}

            set { this.name = value; }
        }

        private string name;
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
                if (CheckShouldProcess(Name))
                {
                    var group = sam.CreateLocalGroup(new LocalGroup
                                                        {
                                                            Description = Description,
                                                            Name = Name
                                                        });

                    WriteObject(group);
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
            return ShouldProcess(target, Strings.ActionNewGroup);
        }
        #endregion Private Methods
    }

}

