// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration.Install;
using System.Reflection;
using Microsoft.Win32;
using System.IO;
using System.Management.Automation;

namespace Microsoft.PowerShell
{
    /// <summary>
    /// MshSecurityMshSnapin (or MshSecurityMshSnapinInstaller) is a class for facilitating registry
    /// of necessary information for monad security mshsnapin.
    ///
    /// This class will be built with monad security dll.
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
        /// Get name of this mshsnapin.
        /// </summary>
        public override string Name
        {
            get
            {
                return RegistryStrings.SecurityMshSnapinName;
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
                return "SecurityMshSnapInResources,Vendor";
            }
        }

        /// <summary>
        /// Get the default description string for this mshsnapin.
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
