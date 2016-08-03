/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.Collections.Generic;
using System.Configuration.Install;
using System.IO;

namespace System.Management.Automation
{
    /// <summary>
    /// 
    /// MshSnapinBase (or MshSnapinInstaller) is a class for facilitating registry of necessary 
    /// information for monad mshsnapin's. 
    /// 
    /// This class will be built with monad core engine dll 
    /// (System.Management.Automation.dll). 
    /// 
    /// This is the base class for two kinds of mshsnapins: MshSnapin and CustomMshSnapin.
    /// 
    /// Each mshsnapin assembly should derive from this class (indirectly) and fill 
    /// in information about mshsnapin name, vendor, and version. 
    /// 
    /// At install time, installation utilities (like InstallUtil.exe) will 
    /// call install this engine assembly based on the implementation in
    /// this class. 
    /// 
    /// This class derives from base class PSInstaller. PSInstaller will 
    /// handle the details about how information got written into registry. 
    /// Here, the information about registry content is provided. 
    /// 
    /// The reason of not calling this class MshSnapinInstaller is to "hide" the details 
    /// that MshSnapin class is actually doing installion. It is also more intuitive
    /// since people deriving from this class will think there are really 
    /// implementing a class for mshsnapin. 
    /// 
    /// </summary>
    /// 
    /// <remarks>
    /// This is an abstract class to be derived by monad mshsnapin and custom mshsnapin. 
    /// MshSnapin developer should not directly derive from this class. 
    /// </remarks>
    public abstract class PSSnapInInstaller : PSInstaller
    {
        /// <summary>
        /// 
        /// </summary>
        public abstract string Name
        {
            get;
        }

        /// <summary>
        /// 
        /// </summary>
        public abstract string Vendor
        {
            get;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual string VendorResource
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public abstract string Description
        {
            get;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual string DescriptionResource
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private string MshSnapinVersion
        {
            get
            {
                return this.GetType().Assembly.GetName().Version.ToString();
            }
        }

        private string _psVersion = null;
        /// <summary>
        /// 
        /// </summary>
        private string PSVersion
        {
            get { return _psVersion ?? (_psVersion = PSVersionInfo.FeatureVersionString); }
        }

        /// <summary>
        /// 
        /// </summary>
        internal sealed override string RegKey
        {
            get
            {
                PSSnapInInfo.VerifyPSSnapInFormatThrowIfError(this.Name);

                return RegistryStrings.MshSnapinKey + "\\" + Name;
            }
        }

        private Dictionary<String, object> _regValues = null;
        /// <summary>
        /// 
        /// </summary>
        internal override Dictionary<String, object> RegValues
        {
            get
            {
                if (_regValues == null)
                {
                    _regValues = new Dictionary<String, object>();
                    _regValues[RegistryStrings.MshSnapin_MonadVersion] = this.PSVersion;

                    if (!String.IsNullOrEmpty(this.Vendor))
                        _regValues[RegistryStrings.MshSnapin_Vendor] = this.Vendor;

                    if (!String.IsNullOrEmpty(this.Description))
                        _regValues[RegistryStrings.MshSnapin_Description] = this.Description;

                    if (!String.IsNullOrEmpty(this.VendorResource))
                        _regValues[RegistryStrings.MshSnapin_VendorResource] = this.VendorResource;

                    if (!String.IsNullOrEmpty(this.DescriptionResource))
                        _regValues[RegistryStrings.MshSnapin_DescriptionResource] = this.DescriptionResource;

                    _regValues[RegistryStrings.MshSnapin_Version] = this.MshSnapinVersion;
                    _regValues[RegistryStrings.MshSnapin_ApplicationBase] = Path.GetDirectoryName(this.GetType().Assembly.Location);
                    _regValues[RegistryStrings.MshSnapin_AssemblyName] = this.GetType().Assembly.FullName;
                    _regValues[RegistryStrings.MshSnapin_ModuleName] = this.GetType().Assembly.Location;
                }

                return _regValues;
            }
        }
    }
}
