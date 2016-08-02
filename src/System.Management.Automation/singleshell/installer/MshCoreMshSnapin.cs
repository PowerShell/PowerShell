/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

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
    /// 
    /// MshCoreMshSnapin (or MshCoreMshSnapinInstaller) is a class for facilitating registry 
    /// of necessary information for monad core mshsnapin. 
    /// 
    /// This class will be built with monad core engine dll 
    /// (System.Management.Automation.dll). 
    /// 
    /// </summary>
    /// 
    [RunInstaller(true)]
    public sealed class PSCorePSSnapIn : PSSnapIn
    {
        /// <summary>
        /// Create an instance of this class. 
        /// </summary>
        public PSCorePSSnapIn()
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
                return RegistryStrings.CoreMshSnapinName;
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
                return "CoreMshSnapInResources,Vendor";
            }
        }

        /// <summary>
        /// Get the default description string for this mshsnapin. 
        /// </summary>
        public override string Description
        {
            get
            {
                return "This PSSnapIn contains MSH management cmdlets used to manage components affecting the MSH engine.";
            }
        }

        /// <summary>
        /// Get resource information for description. This is a string of format: resourceBaseName,resourceName. 
        /// </summary>
        public override string DescriptionResource
        {
            get
            {
                return "CoreMshSnapInResources,Description";
            }
        }

        /// <summary>
        /// Get type files to be used for this mshsnapin.
        /// </summary>
        public override string[] Types { get; } = new string[] { "types.ps1xml" };

        /// <summary>
        /// Get format files to be used for this mshsnapin.
        /// </summary>
        public override string[] Formats { get; } = new string[] {
            "Certificate.format.ps1xml",
            "DotNetTypes.format.ps1xml",
            "FileSystem.format.ps1xml",
            "Help.format.ps1xml",
            "HelpV3.format.ps1xml",
            "PowerShellCore.format.ps1xml",
            "PowerShellTrace.format.ps1xml",
            "Registry.format.ps1xml"
        };
    }
}
