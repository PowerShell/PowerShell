// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.Management.Automation
{
    /// <summary>
    /// Contains a PS Class information.
    /// </summary>
    public sealed class PSClassInfo
    {
        /// <summary>
        /// Initializes a new instance of the PSClassInfo class.
        /// </summary>
        /// <param name="name">Name of the PS Class.</param>
        internal PSClassInfo(string name)
        {
            this.Name = name;
        }

        /// <summary>
        /// Name of the class.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Collection of members of the class.
        /// </summary>
        public ReadOnlyCollection<PSClassMemberInfo> Members { get; private set; }

        /// <summary>
        /// Updates members of the class.
        /// </summary>
        /// <param name="members">Updated members.</param>
        public void UpdateMembers(IList<PSClassMemberInfo> members)
        {
            if (members != null)
                this.Members = new ReadOnlyCollection<PSClassMemberInfo>(members);
        }

        /// <summary>
        /// Module in which the class is implemented in.
        /// </summary>
        public PSModuleInfo Module { get; internal set; }

        /// <summary>
        /// Gets the help file path for the cmdlet.
        /// </summary>
        public string HelpFile { get; internal set; } = string.Empty;
    }

    /// <summary>
    /// Contains a class field information.
    /// </summary>
    public sealed class PSClassMemberInfo
    {
        /// <summary>
        /// Initializes a new instance of the PSClassMemberInfo class.
        /// </summary>
        internal PSClassMemberInfo(string name, string memberType, string defaultValue)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");

            this.Name = name;
            this.TypeName = memberType;
            this.DefaultValue = defaultValue;
        }

        /// <summary>
        /// Gets or sets name of the member.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets or sets type of the member.
        /// </summary>
        public string TypeName { get; private set; }

        /// <summary>
        /// Default value of the Field.
        /// </summary>
        public string DefaultValue { get; private set; }
    }
}
