//---------------------------------------------------------------------
// <copyright file="Installation.cs" company="Microsoft Corporation">
//   Copyright (c) 1999, Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.PackageManagement.Msi.Internal.Deployment.WindowsInstaller
{
    /// <summary>
    /// Subclasses of this abstract class represent a unique instance of a
    /// registered product or patch installation.
    /// </summary>
    public abstract class Installation
    {
        private string installationCode;
        private string userSid;
        private UserContexts context;
        private SourceList sourceList;

        internal Installation(string installationCode, string userSid, UserContexts context)
        {
            if (context == UserContexts.Machine)
            {
                userSid = null;
            }
            this.installationCode = installationCode;
            this.userSid = userSid;
            this.context = context;
        }

        /// <summary>
        /// Gets the user security identifier (SID) under which this product or patch
        /// installation is available.
        /// </summary>
        public string UserSid
        {
            get
            {
                return this.userSid;
            }
        }

        /// <summary>
        /// Gets the user context of this product or patch installation.
        /// </summary>
        public UserContexts Context
        {
            get
            {
                return this.context;
            }
        }

        /// <summary>
        /// Gets the source list of this product or patch installation.
        /// </summary>
        internal virtual SourceList SourceList
        {
            get
            {
                if (this.sourceList == null)
                {
                    this.sourceList = new SourceList(this);
                }
                return this.sourceList;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this product or patch is installed on the current system.
        /// </summary>
        public abstract bool IsInstalled
        {
            get;
        }

        internal string InstallationCode
        {
            get
            {
                return this.installationCode;
            }
        }

        internal abstract int InstallationType
        {
            get;
        }

        /// <summary>
        /// Gets a property about the product or patch installation.
        /// </summary>
        /// <param name="propertyName">Name of the property being retrieved.</param>
        /// <returns></returns>
        public abstract string this[string propertyName]
        {
            get;
        }
    }
}
