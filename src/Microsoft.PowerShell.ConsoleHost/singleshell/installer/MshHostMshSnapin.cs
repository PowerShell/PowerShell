/********************************************************************++
Copyright (c) Microsoft Corporation. All rights reserved.
--********************************************************************/

using System.ComponentModel;
using System.Management.Automation;

namespace Microsoft.PowerShell
{
    /// <summary>
    ///
    /// PSHostMshSnapin (or PSHostMshSnapinInstaller) is a class for facilitating registry
    /// of necessary information for monad host mshsnapin.
    ///
    /// This class will be built with monad host engine dll
    /// (Microsoft.PowerShell.ConsoleHost.dll).
    ///
    /// </summary>
    ///
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
        /// Get name of this mshsnapin.
        /// </summary>
        public override string Name
        {
            get
            {
                return RegistryStrings.HostMshSnapinName;
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
                return "HostMshSnapInResources,Vendor";
            }
        }

        /// <summary>
        /// Get the default description string for this mshsnapin.
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
