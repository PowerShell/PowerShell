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
    /// MshSnapin is a class for regular mshsnapin's which is constructed
    /// based on mshsnapin assembly.  
    ///
    /// This class derives from PSSnapInInstaller and will be used as the base
    /// for all regular mshsnapins.
    ///
    /// </summary>
    /// 
    /// <remarks>
    /// Developers should derive from this class when implementing their own 
    /// mshsnapins. 
    /// 
    /// Derived mshsnapins should be denotated with [RunInstaller] attribute
    /// so that installutil.exe can directly install the mshsnapin into registry. 
    /// </remarks>
    public abstract class PSSnapIn : PSSnapInInstaller
    {
        /// <summary>
        /// Gets list of format files to be loaded for this mshsnapin. 
        /// </summary>
        /// <remarks>
        /// This member can be derived to provide the list of formats to be loaded for this mshsnapin. 
        /// </remarks>
        public virtual string[] Formats
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Gets list of type files to be loaded for this mshsnapin. 
        /// </summary>
        /// <remarks>
        /// This member can be derived to provide the list of types to be loaded for this mshsnapin. 
        /// </remarks>
        public virtual string[] Types
        {
            get
            {
                return null;
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
                    _regValues = base.RegValues;

                    if (this.Types != null && this.Types.Length > 0)
                        _regValues[RegistryStrings.MshSnapin_BuiltInTypes] = this.Types;

                    if (this.Formats != null && this.Formats.Length > 0)
                        _regValues[RegistryStrings.MshSnapin_BuiltInFormats] = this.Formats;
                }

                return _regValues;
            }
        }
    }
}
