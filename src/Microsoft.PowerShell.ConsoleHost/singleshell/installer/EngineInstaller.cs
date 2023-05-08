// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Management.Automation;
using System.Reflection;

namespace Microsoft.PowerShell
{
    /// <summary>
    /// EngineInstaller is a class for facilitating registry of necessary
    /// information for PowerShell engine.
    ///
    /// At install time, installation utilities (like InstallUtil.exe) will
    /// call install this engine assembly based on the implementation in
    /// this class.
    ///
    /// This class derives from base class PSInstaller. PSInstaller will
    /// handle the details about how information got written into registry.
    /// Here, the information about registry content is provided.
    /// </summary>
    [RunInstaller(true)]
    public sealed class EngineInstaller : PSInstaller
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public EngineInstaller()
            : base()
        {
        }

        /// <summary>
        /// </summary>
        internal sealed override string RegKey
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

        private Dictionary<string, object> _regValues = null;
        /// <summary>
        /// </summary>
        internal sealed override Dictionary<string, object> RegValues
        {
            get
            {
                if (_regValues == null)
                {
                    _regValues = new Dictionary<string, object>();
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
