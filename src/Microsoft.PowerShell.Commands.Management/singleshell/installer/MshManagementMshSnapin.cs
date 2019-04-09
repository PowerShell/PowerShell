// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.ComponentModel;
using System.Management.Automation;

namespace Microsoft.PowerShell
{
    /// <summary>
    /// MshManagementMshSnapin (or MshManagementMshSnapinInstaller) is a class for facilitating registry
    /// of necessary information for monad management mshsnapin.
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
        /// Get name of this mshsnapin.
        /// </summary>
        public override string Name
        {
            get
            {
                return RegistryStrings.ManagementMshSnapinName;
            }
        }

        /// <summary>
        /// Get the default vendor string for this mshsnapin.
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
        /// Get the default description string for this mshsnapin.
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
