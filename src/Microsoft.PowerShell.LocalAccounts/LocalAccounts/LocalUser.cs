// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

using Microsoft.PowerShell.LocalAccounts;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Describes a Local User.
    /// Objects of this type are provided to and returned from user-related Cmdlets.
    /// </summary>
    public class LocalUser : LocalPrincipal
    {
        #region Public Properties
        /// <summary>
        /// The date and time at which this user account expires.
        /// A value of null indicates that the account never expires.
        /// </summary>
        public DateTime? AccountExpires { get; set; }

        /// <summary>
        /// A short description of the User.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Indicates whether the user account is enabled (true) or disabled (false).
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// The user's full name. Not the same as the User name.
        /// </summary>
        public string FullName { get; set; }

        /// <summary>
        /// The date and time at which this user account password is allowed
        /// to be changed. The password cannot be changed before this time.
        /// A value of null indicates that the password can be changed anytime.
        /// </summary>
        public DateTime? PasswordChangeableDate { get; set; }

        /// <summary>
        /// The date and time at which this user account password must be changed
        /// to a new password. A value of null indicates that the password will
        /// never expire.
        /// </summary>
        public DateTime? PasswordExpires { get; set; }

        /// <summary>
        /// Indicates whether the user is allowed to change the password (true)
        /// or not (false).
        /// </summary>
        public bool UserMayChangePassword { get; set; }

        /// <summary>
        /// Indicates whether the user must have a password (true) or not (false).
        /// </summary>
        public bool PasswordRequired { get; set; }

        /// <summary>
        /// The date and time at which this user last changed the account password.
        /// </summary>
        public DateTime? PasswordLastSet { get; set; }

        /// <summary>
        /// The date and time at which the user last logged on to the machine.
        /// </summary>
        public DateTime? LastLogon { get; set; }
        #endregion Public Properties

        #region Construction
        /// <summary>
        /// Initializes a new LocalUser object.
        /// </summary>
        public LocalUser()
        {
            ObjectClass = Strings.ObjectClassUser;
        }

        /// <summary>
        /// Initializes a new LocalUser object with the specified name.
        /// </summary>
        /// <param name="name">Name of the new LocalUser.</param>
        public LocalUser(string name)
          : base(name)
        {
            ObjectClass = Strings.ObjectClassUser;
        }

        /// <summary>
        /// Construct a new LocalUser object that is a copy of another.
        /// </summary>
        /// <param name="other">The LocalUser object to copy.</param>
        private LocalUser(LocalUser other)
          : this(other.Name)
        {
            SID = other.SID;
            PrincipalSource = other.PrincipalSource;
            ObjectClass = other.ObjectClass;

            AccountExpires = other.AccountExpires;
            Description = other.Description;
            Enabled = other.Enabled;
            FullName = other.FullName;
            PasswordChangeableDate = other.PasswordChangeableDate;
            PasswordExpires = other.PasswordExpires;
            UserMayChangePassword = other.UserMayChangePassword;

            PasswordRequired = other.PasswordRequired;
            PasswordLastSet = other.PasswordLastSet;
            LastLogon = other.LastLogon;
        }
        #endregion Construction

        #region Public Methods
        /// <summary>
        /// Provides a string representation of the LocalUser object.
        /// </summary>
        /// <returns>
        /// A string containing the User Name.
        /// </returns>
        public override string ToString()
        {
            return Name ?? SID.ToString();
        }

        /// <summary>
        /// Create a copy of a LocalUser object.
        /// </summary>
        /// <returns>
        /// A new LocalUser object with the same property values as this one.
        /// </returns>
        public LocalUser Clone()
        {
            return new LocalUser(this);
        }
        #endregion Public Methods
    }
}
