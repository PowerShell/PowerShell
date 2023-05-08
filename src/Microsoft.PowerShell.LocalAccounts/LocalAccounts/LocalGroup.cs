// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

using Microsoft.PowerShell.LocalAccounts;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Describes a Local Group.
    /// Objects of this type are provided to and returned from group-related Cmdlets.
    /// </summary>
    public class LocalGroup : LocalPrincipal
    {
        #region Public Properties
        /// <summary>
        /// A short description of the Group.
        /// </summary>
        public string Description { get; set; }
        #endregion Public Properties

        #region Construction
        /// <summary>
        /// Initializes a new LocalGroup object.
        /// </summary>
        public LocalGroup()
        {
            ObjectClass = Strings.ObjectClassGroup;
        }

        /// <summary>
        /// Initializes a new LocalUser object with the specified name.
        /// </summary>
        /// <param name="name">Name of the new LocalGroup.</param>
        public LocalGroup(string name)
          : base(name)
        {
            ObjectClass = Strings.ObjectClassGroup;
        }

        /// <summary>
        /// Construct a new LocalGroup object that is a copy of another.
        /// </summary>
        /// <param name="other"></param>
        private LocalGroup(LocalGroup other)
          : this(other.Name)
        {
            Description = other.Description;
        }
        #endregion Construction

        #region Public Methods
        /// <summary>
        /// Provides a string representation of the LocalGroup object.
        /// </summary>
        /// <returns>
        /// A string containing the Group Name.
        /// </returns>
        public override string ToString()
        {
            return Name ?? SID.ToString();
        }

        /// <summary>
        /// Create a copy of a LocalGroup object.
        /// </summary>
        /// <returns>
        /// A new LocalGroup object with the same property values as this one.
        /// </returns>
        public LocalGroup Clone()
        {
            return new LocalGroup(this);
        }
        #endregion Public Methods
    }
}
