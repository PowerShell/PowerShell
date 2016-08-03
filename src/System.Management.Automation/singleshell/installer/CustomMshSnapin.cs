/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using Microsoft.Win32;
using System.IO;
using System.Management.Automation.Runspaces;

namespace System.Management.Automation
{
    /// <summary>
    /// 
    /// Raw mshsnapin is a class for allowing mshsnapin developers to directly 
    /// specify the set of cmdlets, providers, types, formats, assemblies
    /// available in the mshsnapin. 
    /// 
    /// To use this class, mshsnapin developers will drive from it and fill
    /// in details about cmdlet, provider, type, format, assemblies. 
    /// 
    /// This class will also facilitate the registration of the mshsnapin 
    /// through installutil.exe. 
    /// 
    /// This class will be built with monad core engine dll. 
    /// </summary>
    /// 
    /// <remarks>
    /// Developers should derive from this class to implement their own 
    /// custom mshsnapins. 
    /// 
    /// Derived mshsnapins should be denotated with [RunInstaller] attribute
    /// so that installutil.exe can directly install the mshsnapin into registry. 
    /// </remarks>
    public abstract class CustomPSSnapIn : PSSnapInInstaller
    {
        internal string CustomPSSnapInType
        {
            get
            {
                return this.GetType().FullName;
            }
        }

        private Collection<CmdletConfigurationEntry> _cmdlets;

        /// <summary>
        /// Gets the cmdlets defined in custom mshsnapin.
        /// </summary>
        /// <remarks>
        /// This member can be derived to provide the list of cmdlets to be included for this mshsnapin. 
        /// </remarks>
        public virtual Collection<CmdletConfigurationEntry> Cmdlets
        {
            get { return _cmdlets ?? (_cmdlets = new Collection<CmdletConfigurationEntry>()); }
        }

        private Collection<ProviderConfigurationEntry> _providers;

        /// <summary>
        /// Gets the providers defined in custom mshsnapin.
        /// </summary>
        /// <remarks>
        /// This member can be derived to provide the list of providers to be included for this mshsnapin. 
        /// </remarks>
        public virtual Collection<ProviderConfigurationEntry> Providers
        {
            get { return _providers ?? (_providers = new Collection<ProviderConfigurationEntry>()); }
        }

        private Collection<TypeConfigurationEntry> _types;

        /// <summary>
        /// Gets the types defined in custom mshsnapin.
        /// </summary>
        /// <remarks>
        /// This member can be derived to provide the list of types to be included for this mshsnapin. 
        /// </remarks>
        public virtual Collection<TypeConfigurationEntry> Types
        {
            get { return _types ?? (_types = new Collection<TypeConfigurationEntry>()); }
        }

        private Collection<FormatConfigurationEntry> _formats;

        /// <summary>
        /// Gets the formatsdefined in raw mshsnapin.
        /// </summary>
        /// <remarks>
        /// This member can be derived to provide the list of formats to be included for this mshsnapin. 
        /// </remarks>
        public virtual Collection<FormatConfigurationEntry> Formats
        {
            get { return _formats ?? (_formats = new Collection<FormatConfigurationEntry>()); }
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
                    _regValues = base.RegValues;

                    if (!String.IsNullOrEmpty(this.CustomPSSnapInType))
                        _regValues[RegistryStrings.MshSnapin_CustomPSSnapInType] = this.CustomPSSnapInType;
                }

                return _regValues;
            }
        }
    }
}
