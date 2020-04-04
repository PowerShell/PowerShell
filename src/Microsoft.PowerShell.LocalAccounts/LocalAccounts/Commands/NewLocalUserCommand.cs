// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Management.Automation;
using System.Management.Automation.SecurityAccountsManager;
using System.Management.Automation.SecurityAccountsManager.Extensions;

using Microsoft.PowerShell.LocalAccounts;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The New-LocalUser cmdlet creates a new local user account.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "LocalUser",
            DefaultParameterSetName = "Password",
            SupportsShouldProcess = true,
            HelpUri = "https://go.microsoft.com/fwlink/?LinkId=717981")]
    [Alias("nlu")]
    public class NewLocalUserCommand : PSCmdlet
    {
        #region Static Data
        // Names of object- and boolean-type parameters.
        // Switch parameters don't need to be included.
        private static string[] parameterNames = new string[]
            {
                "AccountExpires",
                "Description",
                "Disabled",
                "FullName",
                "Password",
                "UserMayNotChangePassword"
            };
        #endregion Static Data

        #region Instance Data
        private Sam _sam = null;
        #endregion Instance Data

        #region Parameter Properties
        /// <summary>
        /// The following is the definition of the input parameter "AccountExpires".
        /// Specifies when the user account will expire.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public System.DateTime AccountExpires
        {
            get;
            set;
        }

        // This parameter added by hand (copied from SetLocalUserCommand), not by Cmdlet Designer
        /// <summary>
        /// The following is the definition of the input parameter "AccountNeverExpires".
        /// Specifies that the account will not expire.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public System.Management.Automation.SwitchParameter AccountNeverExpires
        {
            get;
            set;
        }

        /// <summary>
        /// The following is the definition of the input parameter "Description".
        /// A descriptive comment for this user account.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ValidateNotNull]
        public string Description
        {
            get;
            set;
        }

        /// <summary>
        /// The following is the definition of the input parameter "Disabled".
        /// Specifies whether this user account is enabled or disabled.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public System.Management.Automation.SwitchParameter Disabled
        {
            get;
            set;
        }

        /// <summary>
        /// The following is the definition of the input parameter "FullName".
        /// Specifies the full name of the user account. This is different from the
        /// username of the user account.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ValidateNotNull]
        public string FullName
        {
            get;
            set;
        }

        /// <summary>
        /// The following is the definition of the input parameter "Name".
        /// Specifies the user name for the local user account. This can be a local user
        /// account or a local user account that is connected to a Microsoft Account.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        [ValidateLength(1, 20)]
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// The following is the definition of the input parameter "Password".
        /// Specifies the password for the local user account. A password can contain up
        /// to 127 characters.
        /// </summary>
        [Parameter(Mandatory = true,
                   ParameterSetName = "Password",
                   ValueFromPipelineByPropertyName = true)]
        [ValidateNotNull]
        public System.Security.SecureString Password
        {
            get;
            set;
        }

        /// <summary>
        /// The following is the definition of the input parameter "PasswordChangeableDate".
        /// Specifies that the new User account has no password.
        /// </summary>
        [Parameter(Mandatory = true,
                   ParameterSetName = "NoPassword",
                   ValueFromPipelineByPropertyName = true)]
        public System.Management.Automation.SwitchParameter NoPassword
        {
            get;
            set;
        }

        /// <summary>
        /// The following is the definition of the input parameter "PasswordNeverExpires".
        /// Specifies that the password will not expire.
        /// </summary>
        [Parameter(ParameterSetName = "Password",
                   ValueFromPipelineByPropertyName = true)]
        public System.Management.Automation.SwitchParameter PasswordNeverExpires
        {
            get;
            set;
        }

        /// <summary>
        /// The following is the definition of the input parameter "UserMayNotChangePassword".
        /// Specifies whether the user is allowed to change the password on this
        /// account. The default value is True.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public System.Management.Automation.SwitchParameter UserMayNotChangePassword
        {
            get;
            set;
        }

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

            _sam = new Sam();
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
                    var user = new LocalUser
                                {
                                    Name = Name,
                                    Description = Description,
                                    Enabled = true,
                                    FullName = FullName,
                                    UserMayChangePassword = true
                                };

                    foreach (var paramName in parameterNames)
                    {
                        if (this.HasParameter(paramName))
                        {
                            switch (paramName)
                            {
                                case "AccountExpires":
                                    user.AccountExpires = AccountExpires;
                                    break;

                                case "Disabled":
                                    user.Enabled = !Disabled;
                                    break;

                                case "UserMayNotChangePassword":
                                    user.UserMayChangePassword = !UserMayNotChangePassword;
                                    break;
                            }
                        }
                    }

                    if (AccountNeverExpires.IsPresent)
                        user.AccountExpires = null;

                    // Password will be null if NoPassword was given
                    user = _sam.CreateLocalUser(user, Password, PasswordNeverExpires.IsPresent);

                    WriteObject(user);
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
        private bool CheckShouldProcess(string target)
        {
            return ShouldProcess(target, Strings.ActionNewUser);
        }
        #endregion Private Methods
    }
}
