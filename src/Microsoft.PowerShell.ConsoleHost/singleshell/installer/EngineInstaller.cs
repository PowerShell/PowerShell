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
using System.Management.Automation.Runspaces;
using System.Management.Automation;

namespace Microsoft.PowerShell
{
    /// <summary>
    /// 
    /// EngineInstaller is a class for facilitating registry of necessary 
    /// information for monad engine. 
    /// 
    /// This class will be built with monad console host dll 
    /// (System.Management.Automation.dll). 
    /// 
    /// At install time, installation utilities (like InstallUtil.exe) will 
    /// call install this engine assembly based on the implementation in
    /// this class. 
    /// 
    /// This class derives from base class PSInstaller. PSInstaller will 
    /// handle the details about how information got written into registry. 
    /// Here, the information about registry content is provided. 
    /// 
    /// </summary>
    [RunInstaller(true)]
    public sealed class EngineInstaller : PSInstaller
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public EngineInstaller()
            : base()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        internal override sealed string RegKey
        {
            get
            {
                return RegistryStrings.MonadEngineKey;
            }
        }

        private static string EngineVersion
        {
            get
            {
                return PSVersionInfo.FeatureVersionString;
            }
        }

        private Dictionary<String, object> _regValues = null;
        /// <summary>
        /// 
        /// </summary>
        internal override sealed Dictionary<String, object> RegValues
        {
            get
            {
                if (_regValues == null)
                {
                    _regValues = new Dictionary<String, object>();
                    _regValues[RegistryStrings.MonadEngine_MonadVersion] = EngineVersion;
                    _regValues[RegistryStrings.MonadEngine_ApplicationBase] = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    _regValues[RegistryStrings.MonadEngine_ConsoleHostAssemblyName] = Assembly.GetExecutingAssembly().FullName;
                    _regValues[RegistryStrings.MonadEngine_ConsoleHostModuleName] = Assembly.GetExecutingAssembly().Location;
                    _regValues[RegistryStrings.MonadEngine_RuntimeVersion] = Assembly.GetExecutingAssembly().ImageRuntimeVersion;
                }

                return _regValues;
            }
        }
    }
}
