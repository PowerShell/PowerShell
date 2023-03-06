// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration.Install;
using System.IO;
using System.Management.Automation;
using System.Reflection;

using Microsoft.Win32;

namespace Microsoft.PowerShell
{
    /// <summary>
    /// PSSecurityPSSnapIn is a class for facilitating registry
    /// of necessary information for PowerShell security PSSnapin.
    /// </summary>
    [RunInstaller(true)]
    public sealed class PSSecurityPSSnapIn : PSSnapIn
    {
        /// <summary>
        /// Create an instance of this class.
        /// </summary>
        public PSSecurityPSSnapIn()
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
                return RegistryStrings.SecurityMshSnapinName;
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
                return "SecurityMshSnapInResources,Vendor";
            }
        }

        /// <summary>
        /// Get the default description string for this PSSnapin.
        /// </summary>
        public override string Description
        {
            get
            {
                return "This PSSnapIn contains cmdlets to manage MSH security.";
            }
        }

        /// <summary>
        /// Get resource information for description. This is a string of format: resourceBaseName,resourceName.
        /// </summary>
        public override string DescriptionResource
        {
            get
            {
                return "SecurityMshSnapInResources,Description";
            }
        }
    }
}
