// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.DirectoryServices.AccountManagement;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The New-User cmdlet creates a new local user account.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "User",
            HelpUri = "")]
    public class GetUserCommand : PSCmdlet
    {
        /// <summary>
        /// Gets or sets the type of store to which the account belongs.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public ContextType AccountStore
        {
            get;
            set;
        } = ContextType.Domain;

        /// <summary>
        /// Gets or sets options that are used for binding to the Server (domain controller or local machine).
        /// </summary>
        [Parameter]
        public ContextOptions ContextOptions
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets an user credential that are used for binding to the Server (domain controller or local machine).
        /// </summary>
        [Parameter]
        public PSCredential Credential
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value that specifies whether the account may be delegated.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public SwitchParameter DelegationPermitted
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a descriptive comment for this user account.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string Description
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a switch to specify whether this user account is enabled or disabled.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public SwitchParameter Enabled
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a display name of the user account.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string DisplayName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a E-mail address of the user account.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string EmailAddress
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a employee ID of the user account.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string EmployeeId
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a given name of the user account.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string GivenName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a Home directory of the user account.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string HomeDirectory
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a Home drive of the user account.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string HomeDrive
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a middle name of the user account.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string MiddleName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a user name for the user account.
        /// </summary>
        [Parameter(
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
        /// Gets or sets a switch to specify that the new User account has no password.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public SwitchParameter PasswordNotRequired
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a switch to specify that the password will not expire.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public SwitchParameter PasswordNeverExpires
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a container on the store to use as the root of the context.
        /// All queries are performed under this root, and all inserts are performed into this container.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string Path
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a SAM account name for the user account.
        /// </summary>
        [Parameter(
            Position = 0,
            ValueFromPipelineByPropertyName = true)]
        public string SamAccountName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a server/machine or domain controller where the user account will be searched from.
        /// </summary>
        [Parameter]
        public string Server
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a surname of the user account.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string SurName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a switch to specify whether the user is allowed to change the password on the user account.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public SwitchParameter UserCannotChangePassword
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a user principal name (UPN) associated with the user account.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string UserPrincipalName
        {
            get;
            set;
        }

        /// <summary>
        /// ProcessRecord method.
        /// </summary>
        protected override void ProcessRecord()
        {
            try
            {
                var bindingUserName = Credential?.UserName;
                var bindingUserPassword = Credential == null ? null : new System.Net.NetworkCredential(string.Empty, Credential.Password).Password;
                var options = this.MyInvocation.BoundParameters.ContainsKey(nameof(ContextOptions)) ? ContextOptions : DefaultContextOptions.GetDefaultOptionForStore(AccountStore);
                using var principalContext = new PrincipalContext(contextType: AccountStore, name: Server, container: Path, options, bindingUserName, bindingUserPassword);
                UserPrincipal user;

                if (AccountStore == ContextType.Machine)
                {
                    user = new UserPrincipal(principalContext);
                    SetMachineUserPrincipalProperties(user);
                }
                else
                {
                    user = new UserPrincipal(principalContext);
                    SetADUserPrincipalProperties(user);
                }

                PrincipalSearcher ps = new PrincipalSearcher(user);

                PrincipalSearchResult<Principal> results = ps.FindAll();

                foreach (var principal in results)
                {
                    WriteObject(principal);
                }
            }
            catch (Exception ex)
            {
                WriteError(
                    new ErrorRecord(
                        ex,
                        "InvalidValue",
                        ErrorCategory.InvalidOperation,
                        null));
            }
        }

        private void SetMachineUserPrincipalProperties(UserPrincipal userPrincipal)
        {
            foreach (var parameter in this.MyInvocation.BoundParameters)
            {
                switch (parameter.Key)
                {
                    case "Name":
                            userPrincipal.Name = Name;
                            break;
                    case "Description":
                            userPrincipal.Description = Description;
                            break;
                    case "DisplayName":
                            userPrincipal.DisplayName = DisplayName;
                            break;
                    case "Enabled":
                            userPrincipal.Enabled = Enabled;
                            break;
                    case "PasswordNotRequired":
                            userPrincipal.PasswordNotRequired = PasswordNotRequired;
                            break;
                    case "PasswordNeverExpires":
                            userPrincipal.PasswordNeverExpires = PasswordNeverExpires;
                            break;
                    case "SamAccountName":
                            userPrincipal.SamAccountName = SamAccountName;
                            break;
                    case "UserCannotChangePassword":
                            userPrincipal.UserCannotChangePassword = UserCannotChangePassword;
                            break;
                }
            }
        }

        private void SetADUserPrincipalProperties(UserPrincipal userPrincipal)
        {
            foreach (var parameter in this.MyInvocation.BoundParameters)
            {
                switch (parameter.Key)
                {
                    case "Name":
                            userPrincipal.Name = Name;
                            break;
                    case "DelegationPermitted":
                            userPrincipal.DelegationPermitted = DelegationPermitted;
                            break;
                    case "Description":
                            userPrincipal.Description = Description;
                            break;
                    case "DisplayName":
                            userPrincipal.DisplayName = DisplayName;
                            break;
                    case "Enabled":
                            userPrincipal.Enabled = Enabled;
                            break;
                    case "EmployeeId":
                            userPrincipal.EmployeeId = EmployeeId;
                            break;
                    case "EmailAddress":
                            userPrincipal.EmailAddress = EmailAddress;
                            break;
                    case "GivenName":
                            userPrincipal.GivenName = GivenName;
                            break;
                    case "HomeDirectory":
                            userPrincipal.HomeDirectory = HomeDirectory;
                            break;
                    case "HomeDrive":
                            userPrincipal.HomeDrive = HomeDrive;
                            break;
                    case "MiddleName":
                            userPrincipal.MiddleName = MiddleName;
                            break;
                    case "PasswordNotRequired":
                            userPrincipal.PasswordNotRequired = PasswordNotRequired;
                            break;
                    case "PasswordNeverExpires":
                            userPrincipal.PasswordNeverExpires = PasswordNeverExpires;
                            break;
                    case "SamAccountName":
                            userPrincipal.SamAccountName = SamAccountName;
                            break;
                    case "SurName":
                            userPrincipal.Surname = SurName;
                            break;
                    case "UserCannotChangePassword":
                            userPrincipal.UserCannotChangePassword = UserCannotChangePassword;
                            break;
                    case "UserPrincipalName":
                            userPrincipal.UserPrincipalName = UserPrincipalName;
                            break;
                }
            }
        }
    }
}
