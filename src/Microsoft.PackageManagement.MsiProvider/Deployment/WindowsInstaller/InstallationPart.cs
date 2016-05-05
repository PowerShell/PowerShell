//---------------------------------------------------------------------
// <copyright file="InstallationPart.cs" company="Microsoft Corporation">
//   Copyright (c) 1999, Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.PackageManagement.Msi.Internal.Deployment.WindowsInstaller
{
    /// <summary>
    /// Subclasses of this abstract class represent an instance
    /// of a registered feature or component.
    /// </summary>
    internal abstract class InstallationPart
    {
        private string id;
        private string productCode;
        private string userSid;
        private UserContexts context;

        internal InstallationPart(string id, string productCode)
	    : this(id, productCode, null, UserContexts.None)
        {
        }

        internal InstallationPart(string id, string productCode, string userSid, UserContexts context)
        {
            this.id = id;
            this.productCode = productCode;
            this.userSid = userSid;
            this.context = context;
        }

        internal string Id
        {
            get
            {
                return this.id;
            }
        }

        internal string ProductCode
        {
            get
            {
                return this.productCode;
            }
        }

        internal string UserSid
        {
            get
            {
                return this.userSid;
            }
        }

        internal UserContexts Context
        {
            get
            {
                return this.context;
            }
        }

        /// <summary>
        /// Gets the product that this item is a part of.
        /// </summary>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal ProductInstallation Product
        {
            get
            {
                return this.productCode != null ?
                    new ProductInstallation(this.productCode, userSid, context) : null;
            }
        }

        /// <summary>
        /// Gets the current installation state of the item.
        /// </summary>
        public abstract InstallState State
        {
            get;
        }
    }

}
