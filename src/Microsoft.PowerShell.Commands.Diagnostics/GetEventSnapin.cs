// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using System.Management.Automation;
using System.ComponentModel;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Create the PowerShell snap-in used to register the
    /// Get-WinEvent cmdlet. Declaring the PSSnapIn class identifies
    /// this .cs file as a PowerShell snap-in.
    /// </summary>
    [RunInstaller(true)]
    public class GetEventPSSnapIn : PSSnapIn
    {
        /// <summary>
        /// Create an instance of the GetEventPSSnapIn class.
        /// </summary>
        public GetEventPSSnapIn()
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
                return "Microsoft.Powershell.GetEvent";
            }
        }

        /// <summary>
        /// Specify the vendor of the PowerShell snap-in.
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
                return "GetEventResources,Vendor";
            }
        }

        /// <summary>
        /// Specifies the description of the PowerShell snap-in.
        /// </summary>
        public override string Description
        {
            get
            {
                return "This PS snap-in contains Get-WinEvent cmdlet used to read Windows event log data and configuration.";
            }
        }

        /// <summary>
        /// Get resource information for description. This is a string of format: resourceBaseName,resourceName.
        /// </summary>
        public override string DescriptionResource
        {
            get
            {
                return "GetEventResources,Description";
            }
        }

        /// <summary>
        /// Get type files to be used for this mshsnapin.
        /// </summary>
        public override string[] Types
        {
            get
            {
                return _types;
            }
        }

        private string[] _types = new string[] { "getevent.types.ps1xml" };

        /// <summary>
        /// Get format files to be used for this mshsnapin.
        /// </summary>
        public override string[] Formats
        {
            get
            {
                return _formats;
            }
        }

        private string[] _formats = new string[] { "Event.format.ps1xml" };
    }
}
