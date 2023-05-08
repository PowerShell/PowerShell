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
    /// The Set-LocalUser cmdlet changes the properties of a user account in the
    /// local Windows Security Accounts Manager. It can also reset the password of a
    /// local user account.
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "LocalUser",
            SupportsShouldProcess = true,
            DefaultParameterSetName = "Name",
            HelpUri = "https://go.microsoft.com/fwlink/?LinkId=717984")]
    [Alias("slu")]
    public class SetLocalUserCommand : PSCmdlet
    {
        #region Static Data
        // Names of object- and boolean-type parameters.
        // Switch parameters don't need to be included.
        private static string[] parameterNames = new string[]
            {
                "AccountExpires",
                "Description",
                "FullName",
                "Password",
                "UserMayChangePassword",
                "PasswordNeverExpires"
            };
        #endregion Static Data

        #region Instance Data
        private Sam sam = null;
        #endregion Instance Data

        #region Parameter Properties
        /// <summary>
        /// The following is the definition of the input parameter "AccountExpires".
        /// Specifies when the user account will expire. Set to null to indicate that
        /// the account will never expire. The default value is null (account never
        /// expires).
        /// </summary>
        [Parameter]
        public System.DateTime AccountExpires
        {
            get { return this.accountexpires;}

            set { this.accountexpires = value; }
        }

        private System.DateTime accountexpires;

        /// <summary>
        /// The following is the definition of the input parameter "AccountNeverExpires".
        /// Specifies that the account will not expire.
        /// </summary>
        [Parameter]
        public System.Management.Automation.SwitchParameter AccountNeverExpires
        {
            get { return this.accountneverexpires;}

            set { this.accountneverexpires = value; }
        }

        private System.Management.Automation.SwitchParameter accountneverexpires;

        /// <summary>
        /// The following is the definition of the input parameter "Description".
        /// A descriptive comment for this user account.
        /// </summary>
        [Parameter]
        [ValidateNotNull]
        public string Description
        {
            get { return this.description;}

            set { this.description = value; }
        }

        private string description;

        /// <summary>
        /// The following is the definition of the input parameter "FullName".
        /// Specifies the full name of the user account. This is different from the
        /// username of the user account.
        /// </summary>
        [Parameter]
        [ValidateNotNull]
        public string FullName
        {
            get { return this.fullname;}

            set { this.fullname = value; }
        }

        private string fullname;
        /// <summary>
        /// The following is the definition of the input parameter "InputObject".
        /// Specifies the of the local user account to modify in the local Security
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
            get { return this.inputobject;}

            set { this.inputobject = value; }
        }

        private Microsoft.PowerShell.Commands.LocalUser inputobject;

        /// <summary>
        /// The following is the definition of the input parameter "Name".
        /// Specifies the local user account to change.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = "Name")]
        [ValidateNotNullOrEmpty]
        public string Name
        {
            get { return this.name;}

            set { this.name = value; }
        }

        private string name;

        /// <summary>
        /// The following is the definition of the input parameter "Password".
        /// Specifies the password for the local user account.
        /// </summary>
        [Parameter]
        [ValidateNotNull]
        public System.Security.SecureString Password
        {
            get { return this.password;}

            set { this.password = value; }
        }

        private System.Security.SecureString password;

        /// <summary>
        /// The following is the definition of the input parameter "PasswordNeverExpires".
        /// Specifies that the password will not expire.
        /// </summary>
        [Parameter]
        public bool PasswordNeverExpires
        {
            get { return this.passwordneverexpires; }

            set { this.passwordneverexpires = value; }
        }

        private bool passwordneverexpires;

        /// <summary>
        /// The following is the definition of the input parameter "SID".
        /// Specifies a user from the local Security Accounts Manager.
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

        /// <summary>
        /// The following is the definition of the input parameter "UserMayChangePassword".
        /// Specifies whether the user is allowed to change the password on this
        /// account. The default value is True.
        /// </summary>
        [Parameter]
        public bool UserMayChangePassword
        {
            get { return this.usermaychangepassword;}

            set { this.usermaychangepassword = value; }
        }

        private bool usermaychangepassword;
        #endregion Parameter Properties

        #region Cmdlet Overrides
        /// <summary>
        /// BeginProcessing method.
        /// </summary>
        protected override void BeginProcessing()
        {
            if (this.HasParameter("AccountExpires") && AccountNeverExpires.IsPresent)
            {
                InvalidParametersException ex = new InvalidParametersException("AccountExpires", "AccountNeverExpires");
                ThrowTerminatingError(ex.MakeErrorRecord());
            }

            sam = new Sam();
        }

        /// <summary>
        /// ProcessRecord method.
        /// </summary>
        protected override void ProcessRecord()
        {
            try
            {
                LocalUser user = null;

                if (InputObject != null)
                {
                    if (CheckShouldProcess(InputObject.ToString()))
                        user = InputObject;
                }
                else if (Name != null)
                {
                    user = sam.GetLocalUser(Name);

                    if (!CheckShouldProcess(Name))
                        user = null;
                }
                else if (SID != null)
                {
                    user = sam.GetLocalUser(SID);

                    if (!CheckShouldProcess(SID.ToString()))
                        user = null;
                }

                if (user == null)
                    return;

                // We start with what already exists
                var delta = user.Clone();
                bool? passwordNeverExpires = null;

                foreach (var paramName in parameterNames)
                {
                    if (this.HasParameter(paramName))
                    {
                        switch (paramName)
                        {
                            case "AccountExpires":
                                delta.AccountExpires = this.AccountExpires;
                                break;

                            case "Description":
                                delta.Description = this.Description;
                                break;

                            case "FullName":
                                delta.FullName = this.FullName;
                                break;

                            case "UserMayChangePassword":
                                delta.UserMayChangePassword = this.UserMayChangePassword;
                                break;

                            case "PasswordNeverExpires":
                                passwordNeverExpires = this.PasswordNeverExpires;
                                break;
                        }
                    }
                }

                if (AccountNeverExpires.IsPresent)
                    delta.AccountExpires = null;

                sam.UpdateLocalUser(user, delta, Password, passwordNeverExpires);
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
            return ShouldProcess(target, Strings.ActionSetUser);
        }
        #endregion Private Methods
    }

}

