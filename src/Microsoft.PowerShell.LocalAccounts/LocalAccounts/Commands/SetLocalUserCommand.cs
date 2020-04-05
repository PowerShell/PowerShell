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
        private static readonly string[] s_parameterNames = new string[]
            {
                nameof(AccountExpires),
                nameof(Description),
                nameof(FullName),
                nameof(Password),
                nameof(UserMayChangePassword),
                nameof(PasswordNeverExpires)
            };
        #endregion Static Data

        #region Instance Data
        private Sam _sam = null;
        #endregion Instance Data

        #region Parameter Properties
        /// <summary>
        /// The following is the definition of the input parameter "AccountExpires".
        /// Specifies when the user account will expire. Set to null to indicate that
        /// the account will never expire. The default value is null (account never
        /// expires).
        /// </summary>
        [Parameter]
        public DateTime AccountExpires { get; set; }

        /// <summary>
        /// The following is the definition of the input parameter "AccountNeverExpires".
        /// Specifies that the account will not expire.
        /// </summary>
        [Parameter]
        public SwitchParameter AccountNeverExpires { get; set; }

        /// <summary>
        /// The following is the definition of the input parameter "Description".
        /// A descriptive comment for this user account.
        /// </summary>
        [Parameter]
        [ValidateNotNull]
        public string Description { get; set; }

        /// <summary>
        /// The following is the definition of the input parameter "FullName".
        /// Specifies the full name of the user account. This is different from the
        /// username of the user account.
        /// </summary>
        [Parameter]
        [ValidateNotNull]
        public string FullName { get; set; }

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
        public LocalUser InputObject { get; set; }

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
        public string Name { get; set; }

        /// <summary>
        /// The following is the definition of the input parameter "Password".
        /// Specifies the password for the local user account.
        /// </summary>
        [Parameter]
        [ValidateNotNull]
        public System.Security.SecureString Password { get; set; }

        /// <summary>
        /// The following is the definition of the input parameter "PasswordNeverExpires".
        /// Specifies that the password will not expire.
        /// </summary>
        [Parameter]
        public bool PasswordNeverExpires { get; set; }

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
        public System.Security.Principal.SecurityIdentifier SID { get; set; }

        /// <summary>
        /// The following is the definition of the input parameter "UserMayChangePassword".
        /// Specifies whether the user is allowed to change the password on this
        /// account. The default value is True.
        /// </summary>
        [Parameter]
        public bool UserMayChangePassword { get; set; }

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
                LocalUser user = null;

                if (InputObject != null)
                {
                    if (CheckShouldProcess(InputObject.ToString()))
                    {
                        user = InputObject;
                    }
                }
                else if (Name != null)
                {
                    user = _sam.GetLocalUser(Name);

                    if (!CheckShouldProcess(Name))
                    {
                        user = null;
                    }
                }
                else if (SID != null)
                {
                    user = _sam.GetLocalUser(SID);

                    if (!CheckShouldProcess(SID.ToString()))
                    {
                        user = null;
                    }
                }

                if (user == null)
                {
                    return;
                }

                // We start with what already exists
                LocalUser delta = user.Clone();
                bool? passwordNeverExpires = null;

                foreach (var paramName in s_parameterNames)
                {
                    if (this.HasParameter(paramName))
                    {
                        switch (paramName)
                        {
                            case nameof(AccountExpires):
                                delta.AccountExpires = AccountExpires;
                                break;

                            case nameof(Description):
                                delta.Description = Description;
                                break;

                            case nameof(FullName):
                                delta.FullName = FullName;
                                break;

                            case nameof(UserMayChangePassword):
                                delta.UserMayChangePassword = UserMayChangePassword;
                                break;

                            case nameof(PasswordNeverExpires):
                                passwordNeverExpires = PasswordNeverExpires;
                                break;
                        }
                    }
                }

                if (AccountNeverExpires.IsPresent)
                {
                    delta.AccountExpires = null;
                }

                _sam.UpdateLocalUser(user, delta, Password, passwordNeverExpires);
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
            return ShouldProcess(target, Strings.ActionSetUser);
        }
        #endregion Private Methods
    }
}
