// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Security.Principal;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Defines the source of a Principal.
    /// </summary>
    public enum PrincipalSource
    {
        /// <summary>
        /// The principal source is unknown or could not be determined.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// The principal is sourced from the local Windows Security Accounts Manager.
        /// </summary>
        Local,

        /// <summary>
        /// The principal is sourced from an Active Directory domain.
        /// </summary>
        ActiveDirectory,

        /// <summary>
        /// The principal is sourced from Azure Active Directory.
        /// </summary>
        AzureAD,

        /// <summary>
        /// The principal is a Microsoft Account, such as
        /// <b>MicrosoftAccount\user@domain.com</b>
        /// </summary>
        MicrosoftAccount
    }

    /// <summary>
    /// Represents a Principal. Serves as a base class for Users and Groups.
    /// </summary>
    public class LocalPrincipal
    {
        #region Public Properties
        /// <summary>
        /// The account name of the Principal.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The Security Identifier that uniquely identifies the Principal/
        /// </summary>
        public SecurityIdentifier SID { get; set; }

        /// <summary>
        /// Indicates the account store from which the principal is sourced.
        /// One of the PrincipalSource enumerations.
        /// </summary>
        public PrincipalSource? PrincipalSource { get; set; }

        /// <summary>
        /// The object class that represents this principal.
        /// This can be User or Group.
        /// </summary>
        public string ObjectClass { get; set; }
        #endregion Public Properties

        #region Construction
        /// <summary>
        /// Initializes a new LocalPrincipal object.
        /// </summary>
        public LocalPrincipal()
        {
        }

        /// <summary>
        /// Initializes a new LocalPrincipal object with the specified name.
        /// </summary>
        /// <param name="name">Name of the new LocalPrincipal.</param>
        public LocalPrincipal(string name)
        {
            Name = name;
        }
        #endregion Construction

        #region Public Methods
        /// <summary>
        /// Provides a string representation of the Principal.
        /// </summary>
        /// <returns>
        /// A string, in SDDL form, representing the Principal.
        /// </returns>
        public override string ToString()
        {
            return Name ?? SID.ToString();
        }
        #endregion Public Methods
    }
}
