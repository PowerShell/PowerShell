// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Management;
using System.Management.Automation;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.WSMan.Management
{
    #region SnapIn

    /// <summary>
    /// Create the PowerShell snap-in used to register the
    /// WsManPSSnapIn cmdlets. Declaring the PSSnapIn class identifies
    /// this .cs file as a PowerShell snap-in.
    /// </summary>
    [RunInstaller(true)]
    public class WSManPSSnapIn : PSSnapIn
    {
        /// <summary>
        /// Create an instance of the WsManSnapin class.
        /// </summary>
        public WSManPSSnapIn()
            : base()
        {
        }

        /// <summary>
        /// Specify the name of the PowerShell snap-in.
        /// </summary>
        public override string Name
        {
            get
            {
                return "WsManPSSnapIn";
            }
        }

        /// <summary>
        /// Specify the vendor for the PowerShell snap-in.
        /// </summary>
        public override string Vendor
        {
            get
            {
                return "Microsoft";
            }
        }

        /// <summary>
        /// Specify the localization resource information for the vendor.
        /// Use the format: resourceBaseName,VendorName.
        /// </summary>
        public override string VendorResource
        {
            get
            {
                return "WsManPSSnapIn,Microsoft";
            }
        }

        /// <summary>
        /// Specify a description of the PowerShell snap-in.
        /// </summary>
        public override string Description
        {
            get
            {
                return "This is a PowerShell snap-in that includes the WsMan cmdlets.";
            }
        }

        /// <summary>
        /// Specify the localization resource information for the description.
        /// Use the format: resourceBaseName,Description.
        /// </summary>
        public override string DescriptionResource
        {
            get
            {
                return "WsManPSSnapIn,This is a PowerShell snap-in that includes the WsMan cmdlets.";
            }
        }
    }

    #endregion SnapIn
}
