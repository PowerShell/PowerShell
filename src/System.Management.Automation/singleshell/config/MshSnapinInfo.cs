// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security;
using System.Text;

using Microsoft.PowerShell.Commands;
using Microsoft.Win32;

using Dbg = System.Management.Automation.Diagnostics;
using Regex = System.Text.RegularExpressions.Regex;

namespace System.Management.Automation
{
    internal static class RegistryStrings
    {
        /// <summary>
        /// Root key path under HKLM.
        /// </summary>
        internal const string MonadRootKeyPath = "Software\\Microsoft\\PowerShell";

        /// <summary>
        /// Root key name.
        /// </summary>
        internal const string MonadRootKeyName = "PowerShell";

        /// <summary>
        /// Key for monad engine.
        /// </summary>
        internal const string MonadEngineKey = "PowerShellEngine";

        // Name for various values under PSEngine
        internal const string MonadEngine_ApplicationBase = "ApplicationBase";
        internal const string MonadEngine_ConsoleHostAssemblyName = "ConsoleHostAssemblyName";
        internal const string MonadEngine_ConsoleHostModuleName = "ConsoleHostModuleName";
        internal const string MonadEngine_RuntimeVersion = "RuntimeVersion";
        internal const string MonadEngine_MonadVersion = "PowerShellVersion";

        /// <summary>
        /// Key under which all the mshsnapin live.
        /// </summary>
        internal const string MshSnapinKey = "PowerShellSnapIns";

        // Name of various values for each mshsnapin
        internal const string MshSnapin_ApplicationBase = "ApplicationBase";
        internal const string MshSnapin_AssemblyName = "AssemblyName";
        internal const string MshSnapin_ModuleName = "ModuleName";
        internal const string MshSnapin_MonadVersion = "PowerShellVersion";
        internal const string MshSnapin_BuiltInTypes = "Types";
        internal const string MshSnapin_BuiltInFormats = "Formats";
        internal const string MshSnapin_Description = "Description";
        internal const string MshSnapin_Version = "Version";
        internal const string MshSnapin_Vendor = "Vendor";
        internal const string MshSnapin_DescriptionResource = "DescriptionIndirect";
        internal const string MshSnapin_VendorResource = "VendorIndirect";
        internal const string MshSnapin_LogPipelineExecutionDetails = "LogPipelineExecutionDetails";

        // Name of default mshsnapins
        internal const string CoreMshSnapinName = "Microsoft.PowerShell.Core";
        internal const string HostMshSnapinName = "Microsoft.PowerShell.Host";
        internal const string ManagementMshSnapinName = "Microsoft.PowerShell.Management";
        internal const string SecurityMshSnapinName = "Microsoft.PowerShell.Security";
        internal const string UtilityMshSnapinName = "Microsoft.PowerShell.Utility";
    }

    /// <summary>
    /// Contains information about a PSSnapin.
    /// </summary>
    public class PSSnapInInfo
    {
        internal PSSnapInInfo
        (
            string name,
            bool isDefault,
            string applicationBase,
            string assemblyName,
            string moduleName,
            Version psVersion,
            Version version,
            Collection<string> types,
            Collection<string> formats,
            string descriptionFallback,
            string vendorFallback
        )
        {
            if (string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(name));
            }

            if (string.IsNullOrEmpty(applicationBase))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(applicationBase));
            }

            if (string.IsNullOrEmpty(assemblyName))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(assemblyName));
            }

            if (string.IsNullOrEmpty(moduleName))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(moduleName));
            }

            if (psVersion == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(psVersion));
            }

            if (version == null)
            {
                version = new Version("0.0");
            }

            types ??= new Collection<string>();

            formats ??= new Collection<string>();

            descriptionFallback ??= string.Empty;

            vendorFallback ??= string.Empty;

            Name = name;
            IsDefault = isDefault;
            ApplicationBase = applicationBase;
            AssemblyName = assemblyName;
            ModuleName = moduleName;
            PSVersion = psVersion;
            Version = version;
            Types = types;
            Formats = formats;
            _descriptionFallback = descriptionFallback;
            _vendorFallback = vendorFallback;
        }

        internal PSSnapInInfo
        (
            string name,
            bool isDefault,
            string applicationBase,
            string assemblyName,
            string moduleName,
            Version psVersion,
            Version version,
            Collection<string> types,
            Collection<string> formats,
            string description,
            string descriptionFallback,
            string vendor,
            string vendorFallback
        )
        : this(name, isDefault, applicationBase, assemblyName, moduleName, psVersion, version, types, formats, descriptionFallback, vendorFallback)
        {
            _description = description;
            _vendor = vendor;
        }

        internal PSSnapInInfo
        (
            string name,
            bool isDefault,
            string applicationBase,
            string assemblyName,
            string moduleName,
            Version psVersion,
            Version version,
            Collection<string> types,
            Collection<string> formats,
            string description,
            string descriptionFallback,
            string descriptionIndirect,
            string vendor,
            string vendorFallback,
            string vendorIndirect
        ) : this(name, isDefault, applicationBase, assemblyName, moduleName, psVersion, version, types, formats, description, descriptionFallback, vendor, vendorFallback)
        {
            // add descriptionIndirect and vendorIndirect only if the mshsnapin is a default mshsnapin
            if (isDefault)
            {
                _descriptionIndirect = descriptionIndirect;
                _vendorIndirect = vendorIndirect;
            }
        }

        /// <summary>
        /// Unique Name of the PSSnapin.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Is this PSSnapin default PSSnapin.
        /// </summary>
        public bool IsDefault { get; }

        /// <summary>
        /// Returns applicationbase for PSSnapin.
        /// </summary>
        public string ApplicationBase { get; }

        /// <summary>
        /// Strong name of PSSnapin assembly.
        /// </summary>
        public string AssemblyName { get; }

        /// <summary>
        /// Name of PSSnapIn module.
        /// </summary>
        public string ModuleName { get; }

        internal string AbsoluteModulePath
        {
            get
            {
                if (string.IsNullOrEmpty(ModuleName) || Path.IsPathRooted(ModuleName))
                {
                    return ModuleName;
                }
                else if (!File.Exists(Path.Combine(ApplicationBase, ModuleName)))
                {
                    return Path.GetFileNameWithoutExtension(ModuleName);
                }

                return Path.Combine(ApplicationBase, ModuleName);
            }
        }

        /// <summary>
        /// PowerShell version used by PSSnapin.
        /// </summary>
        public Version PSVersion { get; }

        /// <summary>
        /// Version of PSSnapin.
        /// </summary>
        public Version Version { get; }

        /// <summary>
        /// Collection of file names containing types information for PSSnapIn.
        /// </summary>
        public Collection<string> Types { get; }

        /// <summary>
        /// Collection of file names containing format information for PSSnapIn.
        /// </summary>
        public Collection<string> Formats { get; }

        private readonly string _descriptionIndirect;
        private readonly string _descriptionFallback = string.Empty;
        private string _description;
        /// <summary>
        /// Description of PSSnapin.
        /// </summary>
        public string Description
        {
            get
            {
                if (_description == null)
                {
                    LoadIndirectResources();
                }

                return _description;
            }
        }

        private readonly string _vendorIndirect;
        private readonly string _vendorFallback = string.Empty;
        private string _vendor;
        /// <summary>
        /// Vendor of PSSnapin.
        /// </summary>
        public string Vendor
        {
            get
            {
                if (_vendor == null)
                {
                    LoadIndirectResources();
                }

                return _vendor;
            }
        }

        /// <summary>
        /// Get/set whether to log Pipeline Execution Detail events.
        /// </summary>
        public bool LogPipelineExecutionDetails { get; set; } = false;

        /// <summary>
        /// Overrides ToString.
        /// </summary>
        /// <returns>
        /// Name of the PSSnapIn
        /// </returns>
        public override string ToString()
        {
            return Name;
        }

        internal RegistryKey MshSnapinKey
        {
            get
            {
                RegistryKey mshsnapinKey = null;

                try
                {
                    mshsnapinKey = PSSnapInReader.GetMshSnapinKey(Name, PSVersion.Major.ToString(CultureInfo.InvariantCulture));
                }
                catch (ArgumentException)
                {
                }
                catch (SecurityException)
                {
                }
                catch (System.IO.IOException)
                {
                }

                return mshsnapinKey;
            }
        }

        internal void LoadIndirectResources()
        {
            using (RegistryStringResourceIndirect resourceReader = RegistryStringResourceIndirect.GetResourceIndirectReader())
            {
                LoadIndirectResources(resourceReader);
            }
        }

        internal void LoadIndirectResources(RegistryStringResourceIndirect resourceReader)
        {
            if (IsDefault)
            {
                // For default mshsnapins..resource indirects are hardcoded..
                // so dont read from the registry
                _description = resourceReader.GetResourceStringIndirect(
                    AssemblyName,
                    ModuleName,
                    _descriptionIndirect);

                _vendor = resourceReader.GetResourceStringIndirect(
                    AssemblyName,
                    ModuleName,
                    _vendorIndirect);
            }
            else
            {
                RegistryKey mshsnapinKey = MshSnapinKey;
                if (mshsnapinKey != null)
                {
                    _description =
                        resourceReader.GetResourceStringIndirect(
                            mshsnapinKey,
                            RegistryStrings.MshSnapin_DescriptionResource,
                            AssemblyName,
                            ModuleName);

                    _vendor =
                        resourceReader.GetResourceStringIndirect(
                            mshsnapinKey,
                            RegistryStrings.MshSnapin_VendorResource,
                            AssemblyName,
                            ModuleName);
                }
            }

            if (string.IsNullOrEmpty(_description))
            {
                _description = _descriptionFallback;
            }

            if (string.IsNullOrEmpty(_vendor))
            {
                _vendor = _vendorFallback;
            }
        }

        internal PSSnapInInfo Clone()
        {
            PSSnapInInfo cloned = new PSSnapInInfo(
                Name,
                IsDefault,
                ApplicationBase,
                AssemblyName,
                ModuleName,
                PSVersion,
                Version,
                new Collection<string>(Types),
                new Collection<string>(Formats),
                _description,
                _descriptionFallback,
                _descriptionIndirect,
                 _vendor,
                 _vendorFallback,
                 _vendorIndirect);

            return cloned;
        }

        /// <summary>
        /// Returns true if the PSSnapIn Id is valid. A PSSnapIn is valid iff it contains only
        /// "Alpha Numeric","-","_","." characters.
        /// </summary>
        /// <param name="psSnapinId">PSSnapIn Id to validate.</param>
        internal static bool IsPSSnapinIdValid(string psSnapinId)
        {
            if (string.IsNullOrEmpty(psSnapinId))
            {
                return false;
            }

            return Regex.IsMatch(psSnapinId, "^[A-Za-z0-9-_\x2E]*$");
        }

        /// <summary>
        /// Validates the PSSnapIn Id. A PSSnapIn is valid iff it contains only
        /// "Alpha Numeric","-","_","." characters.
        /// </summary>
        /// <param name="psSnapinId">PSSnapIn Id to validate.</param>
        /// <exception cref="PSArgumentException">
        /// 1. Specified PSSnapIn is not valid
        /// </exception>
        internal static void VerifyPSSnapInFormatThrowIfError(string psSnapinId)
        {
            // PSSnapIn do not conform to the naming convention..so throw
            // argument exception
            if (!IsPSSnapinIdValid(psSnapinId))
            {
                throw PSTraceSource.NewArgumentException(nameof(psSnapinId),
                    MshSnapInCmdletResources.InvalidPSSnapInName,
                    psSnapinId);
            }

            // Valid SnapId..Just return
            return;
        }
    }

    /// <summary>
    /// Internal class to read information about a mshsnapin.
    /// </summary>
    internal static class PSSnapInReader
    {
        /// <summary>
        /// Reads all registered mshsnapin for all monad versions.
        /// </summary>
        /// <returns>
        /// A collection of PSSnapInInfo objects
        /// </returns>
        /// <exception cref="SecurityException">
        /// User doesn't have access to monad/mshsnapin registration information
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Monad key is not installed
        /// </exception>
        internal static Collection<PSSnapInInfo> ReadAll()
        {
            Collection<PSSnapInInfo> allMshSnapins = new Collection<PSSnapInInfo>();
            RegistryKey monadRootKey = GetMonadRootKey();

            string[] versions = monadRootKey.GetSubKeyNames();
            if (versions == null)
            {
                return allMshSnapins;
            }

            // PS V3 snapin information is stored under 1 registry key..
            // so no need to iterate over twice.
            Collection<string> filteredVersions = new Collection<string>();
            foreach (string version in versions)
            {
                string temp = PSVersionInfo.GetRegistryVersionKeyForSnapinDiscovery(version);
                if (string.IsNullOrEmpty(temp))
                {
                    temp = version;
                }

                if (!filteredVersions.Contains(temp))
                {
                    filteredVersions.Add(temp);
                }
            }

            foreach (string version in filteredVersions)
            {
                if (string.IsNullOrEmpty(version))
                {
                    continue;
                }
                // found a key which is not version
                if (!MeetsVersionFormat(version))
                {
                    continue;
                }

                Collection<PSSnapInInfo> oneVersionMshSnapins = null;
                try
                {
                    oneVersionMshSnapins = ReadAll(monadRootKey, version);
                }
                // If we cannot get information for one version, continue with other
                // versions
                catch (SecurityException)
                {
                }
                catch (ArgumentException)
                {
                }

                if (oneVersionMshSnapins != null)
                {
                    foreach (PSSnapInInfo info in oneVersionMshSnapins)
                    {
                        allMshSnapins.Add(info);
                    }
                }
            }

            return allMshSnapins;
        }

        /// <summary>
        /// Version should be integer (1, 2, 3 etc)
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        private
        static
        bool MeetsVersionFormat(string version)
        {
            bool r = true;
            try
            {
                LanguagePrimitives.ConvertTo(version, typeof(int), CultureInfo.InvariantCulture);
            }
            catch (PSInvalidCastException)
            {
                r = false;
            }

            return r;
        }

        /// <summary>
        /// Reads all registered mshsnapin for specified psVersion.
        /// </summary>
        /// <returns>
        /// A collection of PSSnapInInfo objects
        /// </returns>
        /// <exception cref="SecurityException">
        /// User doesn't have permission to read MonadRoot or Version
        /// </exception>
        /// <exception cref="ArgumentException">
        /// MonadRoot or Version key doesn't exist.
        /// </exception>
        internal static Collection<PSSnapInInfo> ReadAll(string psVersion)
        {
            if (string.IsNullOrEmpty(psVersion))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(psVersion));
            }

            RegistryKey monadRootKey = GetMonadRootKey();
            return ReadAll(monadRootKey, psVersion);
        }

        /// <summary>
        /// Reads all the mshsnapins for a given psVersion.
        /// </summary>
        /// <exception cref="SecurityException">
        /// The User doesn't have required permission to read the registry key for this version.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Specified version doesn't exist.
        /// </exception>
        /// <exception cref="SecurityException">
        /// User doesn't have permission to read specified version
        /// </exception>
        private static Collection<PSSnapInInfo> ReadAll(RegistryKey monadRootKey, string psVersion)
        {
            Dbg.Assert(monadRootKey != null, "caller should validate the information");
            Dbg.Assert(!string.IsNullOrEmpty(psVersion), "caller should validate the information");

            Collection<PSSnapInInfo> mshsnapins = new Collection<PSSnapInInfo>();
            RegistryKey versionRoot = GetVersionRootKey(monadRootKey, psVersion);
            RegistryKey mshsnapinRoot = GetMshSnapinRootKey(versionRoot, psVersion);

            // get name of all mshsnapin for this version
            string[] mshsnapinIds = mshsnapinRoot.GetSubKeyNames();

            foreach (string id in mshsnapinIds)
            {
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                try
                {
                    mshsnapins.Add(ReadOne(mshsnapinRoot, id));
                }
                // If we cannot read some mshsnapins, we should continue
                catch (SecurityException)
                {
                }
                catch (ArgumentException)
                {
                }
            }

            return mshsnapins;
        }

        /// <summary>
        /// Read mshsnapin for specified mshsnapinId and psVersion.
        /// </summary>
        /// <returns>
        /// MshSnapin info object
        /// </returns>
        /// <exception cref="SecurityException">
        /// The user does not have the permissions required to read the
        /// registry key for one of the following:
        /// 1) Monad
        /// 2) PSVersion
        /// 3) MshSnapinId
        /// </exception>
        /// <exception cref="ArgumentException">
        /// 1) Monad key is not present
        /// 2) VersionKey is not present
        /// 3) MshSnapin key is not present
        /// 4) MshSnapin key is not valid
        /// </exception>
        internal static PSSnapInInfo Read(string psVersion, string mshsnapinId)
        {
            if (string.IsNullOrEmpty(psVersion))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(psVersion));
            }

            if (string.IsNullOrEmpty(mshsnapinId))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(mshsnapinId));
            }
            // PSSnapIn Reader wont service invalid mshsnapins
            // Monad has specific restrictions on the mshsnapinid like
            // mshsnapinid should be A-Za-z0-9.-_ etc.
            PSSnapInInfo.VerifyPSSnapInFormatThrowIfError(mshsnapinId);

            RegistryKey rootKey = GetMonadRootKey();
            RegistryKey versionRoot = GetVersionRootKey(rootKey, psVersion);
            RegistryKey mshsnapinRoot = GetMshSnapinRootKey(versionRoot, psVersion);

            return ReadOne(mshsnapinRoot, mshsnapinId);
        }

        /// <summary>
        /// Reads the mshsnapin info for a specific key under specific monad version.
        /// </summary>
        /// <remarks>
        /// ReadOne will never create a default PSSnapInInfo object.
        /// </remarks>
        /// <exception cref="SecurityException">
        /// The user does not have the permissions required to read the
        /// registry key for specified mshsnapin.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// 1) Specified mshsnapin is not installed.
        /// 2) Specified mshsnapin is not correctly installed.
        /// </exception>
        private static PSSnapInInfo ReadOne(RegistryKey mshSnapInRoot, string mshsnapinId)
        {
            Dbg.Assert(!string.IsNullOrEmpty(mshsnapinId), "caller should validate the parameter");
            Dbg.Assert(mshSnapInRoot != null, "caller should validate the parameter");

            RegistryKey mshsnapinKey;
            mshsnapinKey = mshSnapInRoot.OpenSubKey(mshsnapinId);
            if (mshsnapinKey == null)
            {
                s_mshsnapinTracer.TraceError("Error opening registry key {0}\\{1}.", mshSnapInRoot.Name, mshsnapinId);
                throw PSTraceSource.NewArgumentException(nameof(mshsnapinId), MshSnapinInfo.MshSnapinDoesNotExist, mshsnapinId);
            }

            string applicationBase = ReadStringValue(mshsnapinKey, RegistryStrings.MshSnapin_ApplicationBase, true);
            string assemblyName = ReadStringValue(mshsnapinKey, RegistryStrings.MshSnapin_AssemblyName, true);
            string moduleName = ReadStringValue(mshsnapinKey, RegistryStrings.MshSnapin_ModuleName, true);
            Version monadVersion = ReadVersionValue(mshsnapinKey, RegistryStrings.MshSnapin_MonadVersion, true);
            Version version = ReadVersionValue(mshsnapinKey, RegistryStrings.MshSnapin_Version, false);

            string description = ReadStringValue(mshsnapinKey, RegistryStrings.MshSnapin_Description, false);
            if (description == null)
            {
                s_mshsnapinTracer.WriteLine("No description is specified for mshsnapin {0}. Using empty string for description.", mshsnapinId);
                description = string.Empty;
            }

            string vendor = ReadStringValue(mshsnapinKey, RegistryStrings.MshSnapin_Vendor, false);
            if (vendor == null)
            {
                s_mshsnapinTracer.WriteLine("No vendor is specified for mshsnapin {0}. Using empty string for description.", mshsnapinId);
                vendor = string.Empty;
            }

            bool logPipelineExecutionDetails = false;
            string logPipelineExecutionDetailsStr = ReadStringValue(mshsnapinKey, RegistryStrings.MshSnapin_LogPipelineExecutionDetails, false);
            if (!string.IsNullOrEmpty(logPipelineExecutionDetailsStr))
            {
                if (string.Equals("1", logPipelineExecutionDetailsStr, StringComparison.OrdinalIgnoreCase))
                    logPipelineExecutionDetails = true;
            }

            Collection<string> types = ReadMultiStringValue(mshsnapinKey, RegistryStrings.MshSnapin_BuiltInTypes, false);
            Collection<string> formats = ReadMultiStringValue(mshsnapinKey, RegistryStrings.MshSnapin_BuiltInFormats, false);

            s_mshsnapinTracer.WriteLine("Successfully read registry values for mshsnapin {0}. Constructing PSSnapInInfo object.", mshsnapinId);
            PSSnapInInfo mshSnapinInfo = new PSSnapInInfo(mshsnapinId, false, applicationBase, assemblyName, moduleName, monadVersion, version, types, formats, description, vendor);
            mshSnapinInfo.LogPipelineExecutionDetails = logPipelineExecutionDetails;

            return mshSnapinInfo;
        }

        /// <summary>
        /// Gets multistring value for name.
        /// </summary>
        /// <param name="mshsnapinKey"></param>
        /// <param name="name"></param>
        /// <param name="mandatory"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">
        /// if value is not present and mandatory is true
        /// </exception>
        private static Collection<string> ReadMultiStringValue(RegistryKey mshsnapinKey, string name, bool mandatory)
        {
            object value = mshsnapinKey.GetValue(name);
            if (value == null)
            {
                // If this key should be present..throw error
                if (mandatory)
                {
                    s_mshsnapinTracer.TraceError("Mandatory property {0} not specified for registry key {1}",
                        name, mshsnapinKey.Name);
                    throw PSTraceSource.NewArgumentException(nameof(name), MshSnapinInfo.MandatoryValueNotPresent, name, mshsnapinKey.Name);
                }
                else
                {
                    return null;
                }
            }

            // value cannot be null here...
            string[] msv = value as string[];

            if (msv == null)
            {
                // Check if the value is in string format
                string singleValue = value as string;
                if (singleValue != null)
                {
                    msv = new string[1];
                    msv[0] = singleValue;
                }
            }

            if (msv == null)
            {
                if (mandatory)
                {
                    s_mshsnapinTracer.TraceError("Cannot get string/multi-string value for mandatory property {0} in registry key {1}",
                        name, mshsnapinKey.Name);
                    throw PSTraceSource.NewArgumentException(nameof(name), MshSnapinInfo.MandatoryValueNotInCorrectFormatMultiString, name, mshsnapinKey.Name);
                }
                else
                {
                    return null;
                }
            }

            s_mshsnapinTracer.WriteLine("Successfully read property {0} from {1}",
                name, mshsnapinKey.Name);
            return new Collection<string>(msv);
        }

        /// <summary>
        /// Get the value for name.
        /// </summary>
        /// <param name="mshsnapinKey"></param>
        /// <param name="name"></param>
        /// <param name="mandatory"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">
        /// if no value is available and mandatory is true.
        /// </exception>
        internal static string ReadStringValue(RegistryKey mshsnapinKey, string name, bool mandatory)
        {
            Dbg.Assert(!string.IsNullOrEmpty(name), "caller should validate the parameter");
            Dbg.Assert(mshsnapinKey != null, "Caller should validate the parameter");

            object value = mshsnapinKey.GetValue(name);
            if (value == null && mandatory)
            {
                s_mshsnapinTracer.TraceError("Mandatory property {0} not specified for registry key {1}",
                        name, mshsnapinKey.Name);
                throw PSTraceSource.NewArgumentException(nameof(name), MshSnapinInfo.MandatoryValueNotPresent, name, mshsnapinKey.Name);
            }

            string s = value as string;
            if (string.IsNullOrEmpty(s) && mandatory)
            {
                s_mshsnapinTracer.TraceError("Value is null or empty for mandatory property {0} in {1}",
                        name, mshsnapinKey.Name);
                throw PSTraceSource.NewArgumentException(nameof(name), MshSnapinInfo.MandatoryValueNotInCorrectFormat, name, mshsnapinKey.Name);
            }

            s_mshsnapinTracer.WriteLine("Successfully read value {0} for property {1} from {2}",
                s, name, mshsnapinKey.Name);
            return s;
        }

        internal static Version ReadVersionValue(RegistryKey mshsnapinKey, string name, bool mandatory)
        {
            string temp = ReadStringValue(mshsnapinKey, name, mandatory);
            if (temp == null)
            {
                s_mshsnapinTracer.TraceError("Cannot read value for property {0} in registry key {1}",
                    name, mshsnapinKey.ToString());
                Dbg.Assert(!mandatory, "mandatory is true, ReadStringValue should have thrown exception");
                return null;
            }

            Version v;
            try
            {
                v = new Version(temp);
            }
            catch (ArgumentOutOfRangeException)
            {
                s_mshsnapinTracer.TraceError("Cannot convert value {0} to version format", temp);
                throw PSTraceSource.NewArgumentException(nameof(name), MshSnapinInfo.VersionValueInCorrect, name, mshsnapinKey.Name);
            }
            catch (ArgumentException)
            {
                s_mshsnapinTracer.TraceError("Cannot convert value {0} to version format", temp);
                throw PSTraceSource.NewArgumentException(nameof(name), MshSnapinInfo.VersionValueInCorrect, name, mshsnapinKey.Name);
            }
            catch (OverflowException)
            {
                s_mshsnapinTracer.TraceError("Cannot convert value {0} to version format", temp);
                throw PSTraceSource.NewArgumentException(nameof(name), MshSnapinInfo.VersionValueInCorrect, name, mshsnapinKey.Name);
            }
            catch (FormatException)
            {
                s_mshsnapinTracer.TraceError("Cannot convert value {0} to version format", temp);
                throw PSTraceSource.NewArgumentException(nameof(name), MshSnapinInfo.VersionValueInCorrect, name, mshsnapinKey.Name);
            }

            s_mshsnapinTracer.WriteLine("Successfully converted string {0} to version format.", v);
            return v;
        }

        internal static void ReadRegistryInfo(out Version assemblyVersion, out string publicKeyToken, out string culture, out string applicationBase, out Version psVersion)
        {
            applicationBase = Utils.DefaultPowerShellAppBase;
            Dbg.Assert(
                !string.IsNullOrEmpty(applicationBase),
                string.Format(CultureInfo.CurrentCulture, $"{RegistryStrings.MonadEngine_ApplicationBase} is empty or null"));

            // Get the PSVersion from Utils..this is hardcoded
            psVersion = PSVersionInfo.PSVersion;
            Dbg.Assert(
                psVersion != null,
                string.Format(CultureInfo.CurrentCulture, $"{RegistryStrings.MonadEngine_MonadVersion} is null"));

            // Get version number in x.x.x.x format
            // This information is available from the executing assembly
            //
            // PROBLEM: The following code assumes all assemblies have the same version,
            // culture, publickeytoken...This will break the scenarios where only one of
            // the assemblies is patched. ie., all monad assemblies should have the
            // same version number.
            AssemblyName assemblyName = typeof(PSSnapInReader).Assembly.GetName();
            assemblyVersion = assemblyName.Version;
            byte[] publicTokens = assemblyName.GetPublicKeyToken();
            if (publicTokens.Length == 0)
            {
                throw PSTraceSource.NewArgumentException("PublicKeyToken", MshSnapinInfo.PublicKeyTokenAccessFailed);
            }

            publicKeyToken = ConvertByteArrayToString(publicTokens);

            // save some cpu cycles by hardcoding the culture to neutral
            // assembly should never be targeted to a particular culture
            culture = "neutral";
        }

        /// <summary>
        /// PublicKeyToken is in the form of byte[]. Use this function to convert to a string.
        /// </summary>
        /// <param name="tokens">Array of byte's.</param>
        /// <returns></returns>
        internal static string ConvertByteArrayToString(byte[] tokens)
        {
            Dbg.Assert(tokens != null, "Input tokens should never be null");
            StringBuilder tokenBuilder = new StringBuilder(tokens.Length * 2);
            foreach (byte b in tokens)
            {
                tokenBuilder.Append(b.ToString("x2", CultureInfo.InvariantCulture));
            }

            return tokenBuilder.ToString();
        }

        /// <summary>
        /// Reads core snapin for monad engine.
        /// </summary>
        /// <returns>
        /// A PSSnapInInfo object
        /// </returns>
        internal static PSSnapInInfo ReadCoreEngineSnapIn()
        {
            ReadRegistryInfo(
                out Version assemblyVersion,
                out string publicKeyToken,
                out string culture,
                out string applicationBase,
                out Version psVersion);

            // System.Management.Automation formats & types files
            Collection<string> types = new Collection<string>(new string[] { "types.ps1xml", "typesv3.ps1xml" });
            Collection<string> formats = new Collection<string>(new string[]
                        {"Certificate.format.ps1xml", "DotNetTypes.format.ps1xml", "FileSystem.format.ps1xml",
                         "Help.format.ps1xml", "HelpV3.format.ps1xml", "PowerShellCore.format.ps1xml", "PowerShellTrace.format.ps1xml",
                         "Registry.format.ps1xml"});

            string strongName = string.Format(CultureInfo.InvariantCulture, "{0}, Version={1}, Culture={2}, PublicKeyToken={3}",
                s_coreSnapin.AssemblyName, assemblyVersion, culture, publicKeyToken);

            string moduleName = Path.Combine(applicationBase, s_coreSnapin.AssemblyName + ".dll");

            PSSnapInInfo coreMshSnapin = new PSSnapInInfo(
                s_coreSnapin.PSSnapInName,
                isDefault: true,
                applicationBase,
                strongName,
                moduleName,
                psVersion,
                assemblyVersion,
                types,
                formats,
                description: null,
                s_coreSnapin.Description,
                s_coreSnapin.DescriptionIndirect,
                vendor: null,
                vendorFallback: null,
                s_coreSnapin.VendorIndirect);
#if !UNIX
            // NOTE: On Unix, logging has to be deferred until after command-line parsing
            // complete. On Windows, deferring the call is not needed
            // and this is in the startup code path.
            SetSnapInLoggingInformation(coreMshSnapin);
#endif

            return coreMshSnapin;
        }

        /// <summary>
        /// Reads all registered mshsnapins for currently executing monad engine.
        /// </summary>
        /// <returns>
        /// A collection of PSSnapInInfo objects
        /// </returns>
        internal static Collection<PSSnapInInfo> ReadEnginePSSnapIns()
        {
            ReadRegistryInfo(
                out Version assemblyVersion,
                out string publicKeyToken,
                out string culture,
                out string applicationBase,
                out Version psVersion);

            // System.Management.Automation formats & types files
            Collection<string> smaFormats = new Collection<string>(new string[]
                        {"Certificate.format.ps1xml", "DotNetTypes.format.ps1xml", "FileSystem.format.ps1xml",
                         "Help.format.ps1xml", "HelpV3.format.ps1xml", "PowerShellCore.format.ps1xml", "PowerShellTrace.format.ps1xml",
                         "Registry.format.ps1xml"});
            Collection<string> smaTypes = new Collection<string>(new string[] { "types.ps1xml", "typesv3.ps1xml" });

            // create default mshsnapininfo objects..
            Collection<PSSnapInInfo> engineMshSnapins = new Collection<PSSnapInInfo>();
            string assemblyVersionString = assemblyVersion.ToString();

            for (int item = 0; item < DefaultMshSnapins.Count; item++)
            {
                DefaultPSSnapInInformation defaultMshSnapinInfo = DefaultMshSnapins[item];

                string strongName = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}, Version={1}, Culture={2}, PublicKeyToken={3}",
                    defaultMshSnapinInfo.AssemblyName,
                    assemblyVersionString,
                    culture,
                    publicKeyToken);

                Collection<string> formats = null;
                Collection<string> types = null;

                if (defaultMshSnapinInfo.AssemblyName.Equals("System.Management.Automation", StringComparison.OrdinalIgnoreCase))
                {
                    formats = smaFormats;
                    types = smaTypes;
                }
                else if (defaultMshSnapinInfo.AssemblyName.Equals("Microsoft.PowerShell.Commands.Diagnostics", StringComparison.OrdinalIgnoreCase))
                {
                    types = new Collection<string>(new string[] { "GetEvent.types.ps1xml" });
                    formats = new Collection<string>(new string[] { "Event.format.ps1xml", "Diagnostics.format.ps1xml" });
                }
                else if (defaultMshSnapinInfo.AssemblyName.Equals("Microsoft.WSMan.Management", StringComparison.OrdinalIgnoreCase))
                {
                    formats = new Collection<string>(new string[] { "WSMan.format.ps1xml" });
                }

                string moduleName = Path.Combine(applicationBase, defaultMshSnapinInfo.AssemblyName + ".dll");

                PSSnapInInfo defaultMshSnapin = new PSSnapInInfo(
                    defaultMshSnapinInfo.PSSnapInName,
                    isDefault: true,
                    applicationBase,
                    strongName,
                    moduleName,
                    psVersion,
                    assemblyVersion,
                    types,
                    formats,
                    description: null,
                    defaultMshSnapinInfo.Description,
                    defaultMshSnapinInfo.DescriptionIndirect,
                    vendor: null,
                    vendorFallback: null,
                    defaultMshSnapinInfo.VendorIndirect);

                SetSnapInLoggingInformation(defaultMshSnapin);
                engineMshSnapins.Add(defaultMshSnapin);
            }

            return engineMshSnapins;
        }

        /// <summary>
        /// Enable Snapin logging based on group policy.
        /// </summary>
        private static void SetSnapInLoggingInformation(PSSnapInInfo psSnapInInfo)
        {
            IEnumerable<string> names;
            ModuleCmdletBase.ModuleLoggingGroupPolicyStatus status = ModuleCmdletBase.GetModuleLoggingInformation(out names);
            if (status != ModuleCmdletBase.ModuleLoggingGroupPolicyStatus.Undefined)
            {
                SetSnapInLoggingInformation(psSnapInInfo, status, names);
            }
        }

        /// <summary>
        /// Enable Snapin logging based on group policy.
        /// </summary>
        private static void SetSnapInLoggingInformation(PSSnapInInfo psSnapInInfo, ModuleCmdletBase.ModuleLoggingGroupPolicyStatus status, IEnumerable<string> moduleOrSnapinNames)
        {
            if (((status & ModuleCmdletBase.ModuleLoggingGroupPolicyStatus.Enabled) != 0) && moduleOrSnapinNames != null)
            {
                foreach (string currentGPModuleOrSnapinName in moduleOrSnapinNames)
                {
                    if (string.Equals(psSnapInInfo.Name, currentGPModuleOrSnapinName, StringComparison.OrdinalIgnoreCase))
                    {
                        psSnapInInfo.LogPipelineExecutionDetails = true;
                    }
                    else if (WildcardPattern.ContainsWildcardCharacters(currentGPModuleOrSnapinName))
                    {
                        WildcardPattern wildcard = WildcardPattern.Get(currentGPModuleOrSnapinName, WildcardOptions.IgnoreCase);
                        if (wildcard.IsMatch(psSnapInInfo.Name))
                        {
                            psSnapInInfo.LogPipelineExecutionDetails = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get the key to monad root.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="SecurityException">
        /// Caller doesn't have access to monad registration information.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Monad registration information is not available.
        /// </exception>
        internal static RegistryKey GetMonadRootKey()
        {
            RegistryKey rootKey = Registry.LocalMachine.OpenSubKey(RegistryStrings.MonadRootKeyPath);
            if (rootKey == null)
            {
                // This should never occur because this code is running
                // because monad is installed. { well this can occur if someone
                // deletes the registry key after starting monad
                Dbg.Assert(false, "Root Key of Monad installation is not present");
                throw PSTraceSource.NewArgumentException("monad", MshSnapinInfo.MonadRootRegistryAccessFailed);
            }

            return rootKey;
        }

        /// <summary>
        /// Get the registry key to PSEngine.
        /// </summary>
        /// <returns>RegistryKey.</returns>
        /// <param name="psVersion">Major version in string format.</param>
        /// <exception cref="ArgumentException">
        /// Monad registration information is not available.
        /// </exception>
        internal static RegistryKey GetPSEngineKey(string psVersion)
        {
            RegistryKey rootKey = GetMonadRootKey();
            // root key wont be null
            Dbg.Assert(rootKey != null, "Root Key of Monad installation is not present");

            RegistryKey versionRootKey = GetVersionRootKey(rootKey, psVersion);
            // version root key wont be null
            Dbg.Assert(versionRootKey != null, "Version Rootkey of Monad installation is not present");

            RegistryKey psEngineParentKey = rootKey.OpenSubKey(psVersion);
            if (psEngineParentKey == null)
            {
                throw PSTraceSource.NewArgumentException("monad", MshSnapinInfo.MonadEngineRegistryAccessFailed);
            }

            RegistryKey psEngineKey = psEngineParentKey.OpenSubKey(RegistryStrings.MonadEngineKey);
            if (psEngineKey == null)
            {
                throw PSTraceSource.NewArgumentException("monad", MshSnapinInfo.MonadEngineRegistryAccessFailed);
            }

            return psEngineKey;
        }

        /// <summary>
        /// Gets the version root key for specified monad version.
        /// </summary>
        /// <param name="rootKey"></param>
        /// <param name="psVersion"></param>
        /// <returns></returns>
        /// <exception cref="SecurityException">
        /// Caller doesn't have permission to read the version key
        /// </exception>
        /// <exception cref="ArgumentException">
        /// specified psVersion key is not present
        /// </exception>
        internal static
        RegistryKey
        GetVersionRootKey(RegistryKey rootKey, string psVersion)
        {
            Dbg.Assert(!string.IsNullOrEmpty(psVersion), "caller should validate the parameter");
            Dbg.Assert(rootKey != null, "caller should validate the parameter");

            string versionKey = PSVersionInfo.GetRegistryVersionKeyForSnapinDiscovery(psVersion);
            RegistryKey versionRoot = rootKey.OpenSubKey(versionKey);
            if (versionRoot == null)
            {
                throw PSTraceSource.NewArgumentException(nameof(psVersion), MshSnapinInfo.SpecifiedVersionNotFound, versionKey);
            }

            return versionRoot;
        }

        /// <summary>
        /// Gets the mshsnapin root key for specified monad version.
        /// </summary>
        /// <param name="versionRootKey"></param>
        /// <param name="psVersion"></param>
        /// <returns></returns>
        /// <exception cref="SecurityException">
        /// Caller doesn't have permission to read the mshsnapin key
        /// </exception>
        /// <exception cref="ArgumentException">
        /// mshsnapin key is not present
        /// </exception>
        private static
        RegistryKey
        GetMshSnapinRootKey(RegistryKey versionRootKey, string psVersion)
        {
            Dbg.Assert(versionRootKey != null, "caller should validate the parameter");

            RegistryKey mshsnapinRoot = versionRootKey.OpenSubKey(RegistryStrings.MshSnapinKey);
            if (mshsnapinRoot == null)
            {
                throw PSTraceSource.NewArgumentException(nameof(psVersion), MshSnapinInfo.NoMshSnapinPresentForVersion, psVersion);
            }

            return mshsnapinRoot;
        }

        /// <summary>
        /// Gets the mshsnapin key for specified monad version and mshsnapin name.
        /// </summary>
        /// <param name="mshSnapInName"></param>
        /// <param name="psVersion"></param>
        /// <returns></returns>
        /// <exception cref="SecurityException">
        /// Caller doesn't have permission to read the mshsnapin key
        /// </exception>
        /// <exception cref="ArgumentException">
        /// mshsnapin key is not present
        /// </exception>
        internal static
        RegistryKey
        GetMshSnapinKey(string mshSnapInName, string psVersion)
        {
            RegistryKey monadRootKey = GetMonadRootKey();
            RegistryKey versionRootKey = GetVersionRootKey(monadRootKey, psVersion);
            RegistryKey mshsnapinRoot = versionRootKey.OpenSubKey(RegistryStrings.MshSnapinKey);
            if (mshsnapinRoot == null)
            {
                throw PSTraceSource.NewArgumentException(nameof(psVersion), MshSnapinInfo.NoMshSnapinPresentForVersion, psVersion);
            }

            RegistryKey mshsnapinKey = mshsnapinRoot.OpenSubKey(mshSnapInName);
            return mshsnapinKey;
        }

        #region Default MshSnapins related structure

        /// <summary>
        /// This structure is meant to hold mshsnapin information for default mshsnapins.
        /// This is private only.
        /// </summary>
        private struct DefaultPSSnapInInformation
        {
            // since this is a private structure..making it as simple as possible
            public string PSSnapInName;
            public string AssemblyName;
            public string Description;
            public string DescriptionIndirect;
            public string VendorIndirect;

            public DefaultPSSnapInInformation(string sName,
                string sAssemblyName,
                string sDescription,
                string sDescriptionIndirect,
                string sVendorIndirect)
            {
                PSSnapInName = sName;
                AssemblyName = sAssemblyName;
                Description = sDescription;
                DescriptionIndirect = sDescriptionIndirect;
                VendorIndirect = sVendorIndirect;
            }
        }

        private static DefaultPSSnapInInformation s_coreSnapin =
            new DefaultPSSnapInInformation("Microsoft.PowerShell.Core", "System.Management.Automation", null,
                                           "CoreMshSnapInResources,Description", "CoreMshSnapInResources,Vendor");

        /// <summary>
        /// </summary>
        private static IList<DefaultPSSnapInInformation> DefaultMshSnapins
        {
            get
            {
                if (s_defaultMshSnapins == null)
                {
                    lock (s_syncObject)
                    {
#pragma warning disable IDE0074 // Disabling the rule because it can't be applied on non Unix
                        if (s_defaultMshSnapins == null)
#pragma warning restore IDE0074
                        {
                            s_defaultMshSnapins = new List<DefaultPSSnapInInformation>()
                            {
#if !UNIX
                                new DefaultPSSnapInInformation("Microsoft.PowerShell.Diagnostics", "Microsoft.PowerShell.Commands.Diagnostics", null,
                                    "GetEventResources,Description", "GetEventResources,Vendor"),
#endif
                                new DefaultPSSnapInInformation("Microsoft.PowerShell.Host", "Microsoft.PowerShell.ConsoleHost", null,
                                    "HostMshSnapInResources,Description", "HostMshSnapInResources,Vendor"),

                                s_coreSnapin,

                                new DefaultPSSnapInInformation("Microsoft.PowerShell.Utility", "Microsoft.PowerShell.Commands.Utility", null,
                                    "UtilityMshSnapInResources,Description", "UtilityMshSnapInResources,Vendor"),

                                new DefaultPSSnapInInformation("Microsoft.PowerShell.Management", "Microsoft.PowerShell.Commands.Management", null,
                                    "ManagementMshSnapInResources,Description", "ManagementMshSnapInResources,Vendor"),

                                new DefaultPSSnapInInformation("Microsoft.PowerShell.Security", "Microsoft.PowerShell.Security", null,
                                    "SecurityMshSnapInResources,Description", "SecurityMshSnapInResources,Vendor")
                            };

#if !UNIX
                            if (!Utils.IsWinPEHost())
                            {
                                s_defaultMshSnapins.Add(new DefaultPSSnapInInformation("Microsoft.WSMan.Management", "Microsoft.WSMan.Management", null,
                                    "WsManResources,Description", "WsManResources,Vendor"));
                            }
#endif
                        }
                    }
                }

                return s_defaultMshSnapins;
            }
        }

        private static IList<DefaultPSSnapInInformation> s_defaultMshSnapins = null;
        private static readonly object s_syncObject = new object();

        #endregion

        private static readonly PSTraceSource s_mshsnapinTracer = PSTraceSource.GetTracer("MshSnapinLoadUnload", "Loading and unloading mshsnapins", false);
    }
}
