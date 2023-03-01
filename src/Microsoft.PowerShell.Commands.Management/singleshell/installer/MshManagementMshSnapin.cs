// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel;
using System.Management.Automation;

namespace Microsoft.PowerShell
{
    /// <summary>
    /// PSManagementPSSnapIn is a class for facilitating registry
    /// of necessary information for PowerShell management PSSnapin.
    ///
    /// This class will be built with monad management dll.
    /// </summary>
    [RunInstaller(true)]
    public sealed class PSManagementPSSnapIn : PSSnapIn
    {
        /// <summary>
        /// Create an instance of this class.
        /// </summary>
        public PSManagementPSSnapIn()
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
                return RegistryStrings.ManagementMshSnapinName;
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
                return "ManagementMshSnapInResources,Vendor";
            }
        }

        /// <summary>
        /// Get the default description string for this PSSnapin.
        /// </summary>
        public override string Description
        {
            get
            {
                return "This PSSnapIn contains general management cmdlets used to manage Windows components.";
            }
        }

        /// <summary>
        /// Get resource information for description. This is a string of format: resourceBaseName,resourceName.
        /// </summary>
        public override string DescriptionResource
        {
            get
            {
                return "ManagementMshSnapInResources,Description";
            }
        }
    }
}
