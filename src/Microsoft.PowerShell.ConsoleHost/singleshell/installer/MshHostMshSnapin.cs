// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel;
using System.Management.Automation;

namespace Microsoft.PowerShell
{
    /// <summary>
    /// PSHostPSSnapIn is a class for facilitating registry
    /// of necessary information for PowerShell host PSSnapin.
    /// </summary>
    [RunInstaller(true)]
    public sealed class PSHostPSSnapIn : PSSnapIn
    {
        /// <summary>
        /// Create an instance of this class.
        /// </summary>
        public PSHostPSSnapIn()
            : base()
        {
        }

        /// <summary>
        /// Get name of this PSSnapin.
        /// </summary>
        public override string Name
        {
            get
            {
                return RegistryStrings.HostMshSnapinName;
            }
        }

        /// <summary>
        /// Get the default vendor string for this PSSnapin.
        /// </summary>
        public override string Vendor
        {
            get
            {
                return "Microsoft";
            }
        }

        /// <summary>
        /// Get resource information for vendor. This is a string of format: resourceBaseName,resourceName.
        /// </summary>
        public override string VendorResource
        {
            get
            {
                return "HostMshSnapInResources,Vendor";
            }
        }

        /// <summary>
        /// Get the default description string for this PSSnapin.
        /// </summary>
        public override string Description
        {
            get
            {
                return "This PSSnapIn contains cmdlets used by the MSH host.";
            }
        }

        /// <summary>
        /// Get resource information for description. This is a string of format: resourceBaseName,resourceName.
        /// </summary>
        public override string DescriptionResource
        {
            get
            {
                return "HostMshSnapInResources,Description";
            }
        }
    }
}
