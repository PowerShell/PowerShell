// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;

using Microsoft.PowerShell.Commands;

namespace Microsoft.PowerShell.DesiredStateConfiguration.Internal.Json
{
    /// <summary>
    /// Class that defines Dsc cache entries.
    /// </summary>
    internal class DscClassCacheEntry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DscClassCacheEntry"/> class.
        /// </summary>
        public DscClassCacheEntry()
            : this(DSCResourceRunAsCredential.Default, isImportedImplicitly: false, cimClassInstance: null, modulePath: string.Empty)
        {
        }

        /// <summary>
        /// Initializes all values.
        /// </summary>
        /// <param name="dscResourceRunAsCredential">Run as credential value.</param>
        /// <param name="isImportedImplicitly">Resource is imported implicitly.</param>
        /// <param name="cimClassInstance">Class definition.</param>
        /// <param name="modulePath">Path of module defining the class.</param>
        public DscClassCacheEntry(DSCResourceRunAsCredential dscResourceRunAsCredential, bool isImportedImplicitly, PSObject cimClassInstance, string modulePath)
        {
            DscResRunAsCred = dscResourceRunAsCredential;
            IsImportedImplicitly = isImportedImplicitly;
            CimClassInstance = cimClassInstance;
            ModulePath = modulePath;
        }

        /// <summary>
        /// Gets or sets the RunAs Credentials that this DSC resource will use.
        /// </summary>
        public DSCResourceRunAsCredential DscResRunAsCred { get; set; }

        /// <summary>
        /// Gets or sets a value indicating if we have implicitly imported this resource.
        /// </summary>
        public bool IsImportedImplicitly { get; set; }

        /// <summary>
        /// A CimClass instance for this resource.
        /// </summary>
        public PSObject CimClassInstance { get; set; }

        /// <summary>
        /// Gets or sets path of the implementing module for this resource.
        /// </summary>
        public string ModulePath { get; set; }
    }

    /// <summary>
    /// DSC class cache for this runspace.
    /// </summary>
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes",
        Justification = "Needed Internal use only")]
    public static class DscClassCache
    {
        private static readonly Regex reservedDynamicKeywordRegex = new Regex("^(Synchronization|Certificate|IIS|SQL)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex reservedPropertiesRegex = new Regex("^(Require|Trigger|Notify|Before|After|Subscribe)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Experimental feature name for DSC v3.
        /// </summary>
        public const string DscExperimentalFeatureName = "PS7DscSupport";

        private static readonly PSTraceSource s_tracer = PSTraceSource.GetTracer("DSC", "DSC Class Cache");

        // Constants for items in the module qualified name (Module\Version\ClassName)
        private const int IndexModuleName = 0;
        private const int IndexModuleVersion = 1;
        private const int IndexClassName = 2;
        private const int IndexFriendlyName = 3;

        // Create a HashSet for fast lookup. According to MSDN, the time complexity of search for an element in a HashSet is O(1)
        private static readonly HashSet<string> s_hiddenResourceCache =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "MSFT_BaseConfigurationProviderRegistration", "MSFT_CimConfigurationProviderRegistration", "MSFT_PSConfigurationProviderRegistration" };

        /// <summary>
        /// Gets DSC class cache for this runspace.
        /// Cache stores the DSCRunAsBehavior, cim class and boolean to indicate if an Inbox resource has been implicitly imported.
        /// </summary>
        private static Dictionary<string, DscClassCacheEntry> ClassCache
        {
            get
            {
                if (t_classCache == null)
                {
                    t_classCache = new Dictionary<string, DscClassCacheEntry>(StringComparer.OrdinalIgnoreCase);
                }

                return t_classCache;
            }
        }

        [ThreadStatic]
        private static Dictionary<string, DscClassCacheEntry> t_classCache;

        /// <summary>
        /// Gets DSC class cache for GuestConfig; it is similar to ClassCache, but maintains values between operations.
        /// </summary>
        private static Dictionary<string, DscClassCacheEntry> GuestConfigClassCache
        {
            get
            {
                if (t_guestConfigClassCache == null)
                {
                    t_guestConfigClassCache = new Dictionary<string, DscClassCacheEntry>(StringComparer.OrdinalIgnoreCase);
                }

                return t_guestConfigClassCache;
            }
        }

        [ThreadStatic]
        private static Dictionary<string, DscClassCacheEntry> t_guestConfigClassCache;

        /// <summary>
        /// DSC classname to source module mapper.
        /// </summary>
        private static Dictionary<string, Tuple<string, Version>> ByClassModuleCache
            => t_byClassModuleCache ??= new Dictionary<string, Tuple<string, Version>>(StringComparer.OrdinalIgnoreCase);

        [ThreadStatic]
        private static Dictionary<string, Tuple<string, Version>> t_byClassModuleCache;

        /// <summary>
        /// Default ModuleName and ModuleVersion to use.
        /// </summary>
        private static readonly Tuple<string, Version> s_defaultModuleInfoForResource = new Tuple<string, Version>("PSDesiredStateConfiguration", new Version(3, 0));

        /// <summary>
        /// When this property is set to true, DSC Cache will cache multiple versions of a resource.
        /// That means it will cache duplicate resource classes (class names for a resource in two different module versions are same).
        /// NOTE: This property should be set to false for DSC compiler related methods/functionality, such as Import-DscResource,
        ///       because the Mof serializer does not support deserialization of classes with different versions.
        /// </summary>
        [ThreadStatic]
        private static bool t_cacheResourcesFromMultipleModuleVersions;

        private static bool CacheResourcesFromMultipleModuleVersions
        {
            get
            {
                return t_cacheResourcesFromMultipleModuleVersions;
            }

            set
            {
                t_cacheResourcesFromMultipleModuleVersions = value;
            }
        }

        [ThreadStatic]
        internal static bool NewApiIsUsed = false;

        /// <summary>
        /// Initialize the class cache with the default classes in $ENV:SystemDirectory\Configuration.
        /// </summary>
        public static void Initialize()
        {
            Initialize(errors: null, modulePathList: null);
        }

        /// <summary>
        /// Initialize the class cache with default classes that come with PSDesiredStateConfiguration module.
        /// </summary>
        /// <param name="errors">Collection of any errors encountered during initialization.</param>
        /// <param name="modulePathList">List of module path from where DSC PS modules will be loaded.</param>
        public static void Initialize(Collection<Exception> errors, List<string> modulePathList)
        {
            s_tracer.WriteLine("Initializing DSC class cache");

            // Load the base schema files.
            ClearCache();
            var dscConfigurationDirectory = Environment.GetEnvironmentVariable("DSC_HOME");
            if (string.IsNullOrEmpty(dscConfigurationDirectory))
            {
                var moduleInfos = ModuleCmdletBase.GetModuleIfAvailable(new Microsoft.PowerShell.Commands.ModuleSpecification()
                    {
                        Name = "PSDesiredStateConfiguration",

                        // Version in the next line is actually MinimumVersion
                        Version = new Version(3, 0, 0)
                    });

                if (moduleInfos.Count > 0)
                { 
                    // to be consistent with Import-Module behavior, we use the fist occurrence that we find in PSModulePath
                    var moduleDirectory = Path.GetDirectoryName(moduleInfos[0].Path);
                    dscConfigurationDirectory = Path.Join(moduleDirectory, "Configuration");
                }
                else
                {
                    // when all else has failed use location of system-wide PS module directory (i.e. /usr/local/share/powershell/Modules) as backup
                    dscConfigurationDirectory = Path.Join(ModuleIntrinsics.GetSharedModulePath(), "PSDesiredStateConfiguration", "Configuration");
                }
            }

            if (!Directory.Exists(dscConfigurationDirectory))
            {
                throw new DirectoryNotFoundException(string.Format(ParserStrings.PsDscMissingSchemaStore, dscConfigurationDirectory));
            }

            var resourceBaseFile = Path.Join(dscConfigurationDirectory, "BaseRegistration", "BaseResource.schema.json");
            ImportBaseClasses(resourceBaseFile, s_defaultModuleInfoForResource, errors, false);
            var metaConfigFile = Path.Join(dscConfigurationDirectory, "BaseRegistration", "MSFT_DSCMetaConfiguration.json");
            ImportBaseClasses(metaConfigFile, s_defaultModuleInfoForResource, errors, false);
        }

        /// <summary>
        /// Import base classes from the given file.
        /// </summary>
        /// <param name="path">Path to schema file.</param>
        /// <param name="moduleInfo">Module information.</param>
        /// <param name="errors">Error collection that will be shown to the user.</param>
        /// <param name="importInBoxResourcesImplicitly">Flag for implicitly imported resource.</param>
        /// <returns>Class objects from schema file.</returns>
        public static IEnumerable<PSObject> ImportBaseClasses(string path, Tuple<string, Version> moduleInfo, Collection<Exception> errors, bool importInBoxResourcesImplicitly)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(path));
            }

            s_tracer.WriteLine("DSC ClassCache: importing file: {0}", path);

            var parser = new CimDSCParser();

            IEnumerable<PSObject> classes = null;
            try
            {
                classes = parser.ParseSchemaJson(path);
            }
            catch (PSInvalidOperationException e)
            {
                // Ignore modules with invalid schemas.
                s_tracer.WriteLine("DSC ClassCache: Error importing file '{0}', with error '{1}'.  Skipping file.", path, e);
                if (errors != null)
                {
                    errors.Add(e);
                }
            }

            if (classes != null)
            {
                foreach (dynamic c in classes)
                {
                    var className = c.ClassName;

                    if (string.IsNullOrEmpty(className))
                    {
                        // ClassName is empty - skipping class import
                        continue;
                    }

                    string alias = GetFriendlyName(c);
                    var friendlyName = string.IsNullOrEmpty(alias) ? className : alias;
                    string moduleQualifiedResourceName = GetModuleQualifiedResourceName(moduleInfo.Item1, moduleInfo.Item2.ToString(), className, friendlyName);
                    DscClassCacheEntry cimClassInfo;

                    if (ClassCache.TryGetValue(moduleQualifiedResourceName, out cimClassInfo))
                    {
                        if (errors != null)
                        {
                            PSInvalidOperationException e = PSTraceSource.NewInvalidOperationException(
                                ParserStrings.DuplicateCimClassDefinition, className, path, cimClassInfo.ModulePath);

                            e.SetErrorId("DuplicateCimClassDefinition");
                            errors.Add(e);
                        }

                        continue;
                    }

                    if (s_hiddenResourceCache.Contains(className))
                    {
                        continue;
                    }

                    var classCacheEntry = new DscClassCacheEntry(DSCResourceRunAsCredential.NotSupported, importInBoxResourcesImplicitly, c, path);
                    ClassCache[moduleQualifiedResourceName] = classCacheEntry;
                    GuestConfigClassCache[moduleQualifiedResourceName] = classCacheEntry;
                    ByClassModuleCache[className] = moduleInfo;
                }

                var sb = new System.Text.StringBuilder();
                foreach (dynamic c in classes)
                {
                    sb.Append(c.ClassName);
                    sb.Append(',');
                }

                s_tracer.WriteLine("DSC ClassCache: loading file '{0}' added the following classes to the cache: {1}", path, sb.ToString());
            }
            else
            {
                s_tracer.WriteLine("DSC ClassCache: loading file '{0}' added no classes to the cache.");
            }

            return classes;
        }

        /// <summary>
        /// Get text from SecureString.
        /// </summary>
        /// <param name="value">Value of SecureString.</param>
        /// <returns>Decoded string.</returns>
        public static string GetStringFromSecureString(SecureString value)
        {
            string passwordValueToAdd = string.Empty;

            if (value != null)
            {
                IntPtr ptr = Marshal.SecureStringToCoTaskMemUnicode(value);
                passwordValueToAdd = Marshal.PtrToStringUni(ptr);
                Marshal.ZeroFreeCoTaskMemUnicode(ptr);
            }

            return passwordValueToAdd;
        }

        /// <summary>
        /// Clear out the existing collection of CIM classes and associated keywords.
        /// </summary>
        public static void ClearCache()
        {
            if (!ExperimentalFeature.IsEnabled(DscExperimentalFeatureName))
            {
                throw new InvalidOperationException(ParserStrings.PS7DscSupportDisabled);
            }

            s_tracer.WriteLine("DSC class: clearing the cache and associated keywords.");
            ClassCache.Clear();
            ByClassModuleCache.Clear();
            CacheResourcesFromMultipleModuleVersions = false;
        }

        private static string GetModuleQualifiedResourceName(string moduleName, string moduleVersion, string className, string resourceName)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}\\{1}\\{2}\\{3}", moduleName, moduleVersion, className, resourceName);
        }

        private static List<KeyValuePair<string, DscClassCacheEntry>> FindResourceInCache(string moduleName, string className, string resourceName)
        {
            return (from cacheEntry in ClassCache
                    let splittedName = cacheEntry.Key.Split(Utils.Separators.Backslash)
                    let cachedClassName = splittedName[IndexClassName]
                    let cachedModuleName = splittedName[IndexModuleName]
                    let cachedResourceName = splittedName[IndexFriendlyName]
                    where (string.Equals(cachedResourceName, resourceName, StringComparison.OrdinalIgnoreCase)
                    || (string.Equals(cachedClassName, className, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(cachedModuleName, moduleName, StringComparison.OrdinalIgnoreCase)))
                    select cacheEntry).ToList();
        }

        /// <summary>
        /// Returns class declaration from GuestConfigClassCache.
        /// </summary>
        /// <param name="moduleName">Module name.</param>
        /// <param name="moduleVersion">Module version.</param>
        /// <param name="className">Name of the class.</param>
        /// <param name="resourceName">Friendly name of the resource.</param>
        /// <returns>Class declaration from cache.</returns>
        public static PSObject GetGuestConfigCachedClass(string moduleName, string moduleVersion, string className, string resourceName)
        {
            if (!ExperimentalFeature.IsEnabled(DscExperimentalFeatureName))
            {
                throw new InvalidOperationException(ParserStrings.PS7DscSupportDisabled);
            }

            var moduleQualifiedResourceName = GetModuleQualifiedResourceName(moduleName, moduleVersion, className, string.IsNullOrEmpty(resourceName) ? className : resourceName);
            DscClassCacheEntry classCacheEntry = null;
            if (GuestConfigClassCache.TryGetValue(moduleQualifiedResourceName, out classCacheEntry))
            {
                return classCacheEntry.CimClassInstance;
            }
            else
            {
                // if class was not found with current ResourceName then it may be a class with non-empty FriendlyName that caller does not know, so perform a broad search
                string partialClassPath = string.Join('\\', moduleName, moduleVersion, className, string.Empty);
                foreach (string key in GuestConfigClassCache.Keys)
                {
                    if (key.StartsWith(partialClassPath))
                    {
                        return GuestConfigClassCache[key].CimClassInstance;
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Clears GuestConfigClassCache.
        /// </summary>
        public static void ClearGuestConfigClassCache()
        {
            GuestConfigClassCache.Clear();
        }

        private static bool IsMagicProperty(string propertyName)
        {
            return System.Text.RegularExpressions.Regex.Match(propertyName, "^(ResourceId|SourceInfo|ModuleName|ModuleVersion|ConfigurationName)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Success;
        }

        private static string GetFriendlyName(dynamic cimClass)
        {
            return cimClass.FriendlyName;
        }

        /// <summary>
        /// Method to get the cached classes in the form of DynamicKeyword.
        /// </summary>
        /// <returns>Dynamic keyword collection.</returns>
        public static Collection<DynamicKeyword> GetKeywordsFromCachedClasses()
        {
            if (!ExperimentalFeature.IsEnabled(DscExperimentalFeatureName))
            {
                throw new InvalidOperationException(ParserStrings.PS7DscSupportDisabled);
            }

            Collection<DynamicKeyword> keywords = new Collection<DynamicKeyword>();

            foreach (KeyValuePair<string, DscClassCacheEntry> cachedClass in ClassCache)
            {
                string[] splittedName = cachedClass.Key.Split(Utils.Separators.Backslash);
                string moduleName = splittedName[IndexModuleName];
                string moduleVersion = splittedName[IndexModuleVersion];

                var keyword = CreateKeywordFromCimClass(moduleName, Version.Parse(moduleVersion), cachedClass.Value.CimClassInstance, cachedClass.Value.DscResRunAsCred);
                if (keyword != null)
                {
                    keywords.Add(keyword);
                }
            }

            return keywords;
        }

        private static void CreateAndRegisterKeywordFromCimClass(string moduleName, Version moduleVersion, PSObject cimClass, Dictionary<string, ScriptBlock> functionsToDefine, DSCResourceRunAsCredential runAsBehavior)
        {
            var keyword = CreateKeywordFromCimClass(moduleName, moduleVersion, cimClass, runAsBehavior);
            if (keyword == null)
            {
                return;
            }

            // keyword is already defined and we don't allow redefine it
            if (!CacheResourcesFromMultipleModuleVersions && DynamicKeyword.ContainsKeyword(keyword.Keyword))
            {
                var oldKeyword = DynamicKeyword.GetKeyword(keyword.Keyword);
                if (oldKeyword.ImplementingModule == null ||
                    !oldKeyword.ImplementingModule.Equals(moduleName, StringComparison.OrdinalIgnoreCase) || oldKeyword.ImplementingModuleVersion != moduleVersion)
                {
                    var e = PSTraceSource.NewInvalidOperationException(ParserStrings.DuplicateKeywordDefinition, keyword.Keyword);
                    e.SetErrorId("DuplicateKeywordDefinition");
                    throw e;
                }
            }

            // Add the dynamic keyword to the table
            DynamicKeyword.AddKeyword(keyword);

            // And now define the driver functions in the current scope...
            if (functionsToDefine != null)
            {
                functionsToDefine[moduleName + "\\" + keyword.Keyword] = CimKeywordImplementationFunction;
            }
        }

        private static DynamicKeyword CreateKeywordFromCimClass(string moduleName, Version moduleVersion, dynamic cimClass, DSCResourceRunAsCredential runAsBehavior)
        {
            var resourceName = cimClass.ClassName;
            string alias = GetFriendlyName(cimClass);
            var keywordString = string.IsNullOrEmpty(alias) ? resourceName : alias;

            // Skip all of the base, meta, registration and other classes that are not intended to be used directly by a script author
            if (System.Text.RegularExpressions.Regex.Match(keywordString, "^OMI_Base|^OMI_.*Registration", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Success)
            {
                return null;
            }

            var keyword = new DynamicKeyword()
            {
                BodyMode = DynamicKeywordBodyMode.Hashtable,
                Keyword = keywordString,
                ResourceName = resourceName,
                ImplementingModule = moduleName,
                ImplementingModuleVersion = moduleVersion,
                SemanticCheck = CheckMandatoryPropertiesPresent
            };

            // If it's one of reserved dynamic keyword, mark it
            if (reservedDynamicKeywordRegex.Match(keywordString).Success)
            {
                keyword.IsReservedKeyword = true;
            }

            // see if it's a resource type i.e. it inherits from OMI_BaseResource
            bool isResourceType = false;

            // previous version of this code was the only place that referenced CimSuperClass
            // so to simplify things we just check superclass to be OMI_BaseResource
            // with assumption that current code will not work for multi-level class inheritance (which is never used in practice according to DSC team)
            // this simplification allows us to avoid linking objects together using CimSuperClass field during deserialization
            if ((!string.IsNullOrEmpty(cimClass.SuperClassName)) && string.Equals("OMI_BaseResource", cimClass.SuperClassName, StringComparison.OrdinalIgnoreCase))
            {
                isResourceType = true;
            }

            // If it's a resource type, then a resource name is required.
            keyword.NameMode = isResourceType ? DynamicKeywordNameMode.NameRequired : DynamicKeywordNameMode.NoName;

            // Add the settable properties to the keyword object
            if (cimClass.ClassProperties != null)
            {
                foreach (var prop in cimClass.ClassProperties)
                {
                    // If the property has the Read qualifier, skip it.
                    if (string.Equals(prop.Qualifiers?.Read?.ToString(), "True", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // If it's one of our magic properties, skip it
                    if (IsMagicProperty(prop.Name))
                    {
                        continue;
                    }

                    if (runAsBehavior == DSCResourceRunAsCredential.NotSupported)
                    {
                        if (string.Equals(prop.Name, "PsDscRunAsCredential", StringComparison.OrdinalIgnoreCase))
                        {
                            // skip adding PsDscRunAsCredential to the dynamic word for the dsc resource.
                            continue;
                        }
                    }

                    // If it's one of our reserved properties, save it for error reporting
                    if (reservedPropertiesRegex.Match(prop.Name).Success)
                    {
                        keyword.HasReservedProperties = true;
                        continue;
                    }

                    // Otherwise, add it to the Keyword List.
                    var keyProp = new System.Management.Automation.Language.DynamicKeywordProperty();
                    keyProp.Name = prop.Name;

                    // Copy the type name string. If it's an embedded instance, need to grab it from the ReferenceClassName
                    bool referenceClassNameIsNullOrEmpty = string.IsNullOrEmpty(prop.ReferenceClassName);
                    if (prop.CimType == "Instance" && !referenceClassNameIsNullOrEmpty)
                    {
                        keyProp.TypeConstraint = prop.ReferenceClassName;
                    }
                    else if (prop.CimType == "InstanceArray" && !referenceClassNameIsNullOrEmpty)
                    {
                        keyProp.TypeConstraint = prop.ReferenceClassName + "[]";
                    }
                    else
                    {
                        keyProp.TypeConstraint = prop.CimType.ToString();
                    }

                    // Check to see if there is a Values attribute and save the list of allowed values if so.
                    var values = prop.Qualifiers?.Values;
                    if (values != null)
                    {
                        foreach (var val in values)
                        {
                            keyProp.Values.Add(val.ToString());
                        }
                    }

                    // Check to see if there is a ValueMap attribute and save the list of allowed values if so.
                    var nativeValueMap = prop.Qualifiers?.ValueMap;
                    List<string> valueMap = null;
                    if (nativeValueMap != null)
                    {
                        valueMap = new List<string>();
                        foreach (var val in nativeValueMap)
                        {
                            valueMap.Add(val.ToString());
                        }
                    }

                    // Check to see if this property has the Required qualifier associated with it.
                    if (string.Equals(prop.Qualifiers?.Required?.ToString(), "True", StringComparison.OrdinalIgnoreCase))
                    {
                        keyProp.Mandatory = true;
                    }

                    // Check to see if this property has the Key qualifier associated with it.
                    if (string.Equals(prop.Qualifiers?.Key?.ToString(), "True", StringComparison.OrdinalIgnoreCase))
                    {
                        keyProp.Mandatory = true;
                        keyProp.IsKey = true;
                    }

                    // set the property to mandatory is specified for the resource.
                    if (runAsBehavior == DSCResourceRunAsCredential.Mandatory)
                    {
                        if (string.Equals(prop.Name, "PsDscRunAsCredential", StringComparison.OrdinalIgnoreCase))
                        {
                            keyProp.Mandatory = true;
                        }
                    }

                    if (valueMap != null && keyProp.Values.Count > 0)
                    {
                        if (valueMap.Count != keyProp.Values.Count)
                        {
                            s_tracer.WriteLine(
                                "DSC CreateDynamicKeywordFromClass: the count of values for qualifier 'Values' and 'ValueMap' doesn't match. count of 'Values': {0}, count of 'ValueMap': {1}. Skip the keyword '{2}'.",
                                keyProp.Values.Count,
                                valueMap.Count,
                                keyword.Keyword);
                            return null;
                        }

                        for (int index = 0; index < valueMap.Count; index++)
                        {
                            string key = keyProp.Values[index];
                            string value = valueMap[index];

                            if (keyProp.ValueMap.ContainsKey(key))
                            {
                                s_tracer.WriteLine(
                                    "DSC CreateDynamicKeywordFromClass: same string value '{0}' appears more than once in qualifier 'Values'. Skip the keyword '{1}'.",
                                    key,
                                    keyword.Keyword);
                                return null;
                            }

                            keyProp.ValueMap.Add(key, value);
                        }
                    }

                    keyword.Properties.Add(prop.Name, keyProp);
                }
            }

            // update specific keyword with range constraints
            UpdateKnownRestriction(keyword);

            return keyword;
        }

        private static void UpdateKnownRestriction(DynamicKeyword keyword)
        {
            const int RefreshFrequencyMin = 30;
            const int RefreshFrequencyMax = 44640;

            const int ConfigurationModeFrequencyMin = 15;
            const int ConfigurationModeFrequencyMax = 44640;

            if (
                string.Equals(
                    keyword.ResourceName,
                    "MSFT_DSCMetaConfigurationV2",
                    StringComparison.OrdinalIgnoreCase)
                ||
                string.Equals(
                    keyword.ResourceName,
                    "MSFT_DSCMetaConfiguration",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (keyword.Properties["RefreshFrequencyMins"] != null)
                {
                    keyword.Properties["RefreshFrequencyMins"].Range = new Tuple<int, int>(RefreshFrequencyMin, RefreshFrequencyMax);
                }

                if (keyword.Properties["ConfigurationModeFrequencyMins"] != null)
                {
                    keyword.Properties["ConfigurationModeFrequencyMins"].Range = new Tuple<int, int>(ConfigurationModeFrequencyMin, ConfigurationModeFrequencyMax);
                }

                if (keyword.Properties["DebugMode"] != null)
                {
                    keyword.Properties["DebugMode"].Values.Remove("ResourceScriptBreakAll");
                    keyword.Properties["DebugMode"].ValueMap.Remove("ResourceScriptBreakAll");
                }
            }
        }

        /// <summary>
        /// Load the default system CIM classes and create the corresponding keywords.
        /// </summary>
        /// <param name="errors">Collection of any errors encountered while loading keywords.</param>
        public static void LoadDefaultCimKeywords(Collection<Exception> errors)
        {
            LoadDefaultCimKeywords(functionsToDefine: null, errors, modulePathList: null, cacheResourcesFromMultipleModuleVersions: false);
        }

        /// <summary>
        /// Load the default system CIM classes and create the corresponding keywords.
        /// <param name="functionsToDefine">A dictionary to add the defined functions to, may be null.</param>
        /// </summary>
        public static void LoadDefaultCimKeywords(Dictionary<string, ScriptBlock> functionsToDefine)
        {
            LoadDefaultCimKeywords(functionsToDefine, errors: null, modulePathList: null, cacheResourcesFromMultipleModuleVersions: false);
        }

        /// <summary>
        /// Load the default system CIM classes and create the corresponding keywords.
        /// </summary>
        /// <param name="errors">Collection of any errors encountered while loading keywords.</param>
        /// <param name="cacheResourcesFromMultipleModuleVersions">Allow caching the resources from multiple versions of modules.</param>
        public static void LoadDefaultCimKeywords(Collection<Exception> errors, bool cacheResourcesFromMultipleModuleVersions)
        {
            LoadDefaultCimKeywords(functionsToDefine: null, errors, modulePathList: null, cacheResourcesFromMultipleModuleVersions);
        }

        /// <summary>
        /// Load the default system CIM classes and create the corresponding keywords.
        /// </summary>
        /// <param name="functionsToDefine">A dictionary to add the defined functions to, may be null.</param>
        /// <param name="errors">Collection of any errors encountered while loading keywords.</param>
        /// <param name="modulePathList">List of module path from where DSC PS modules will be loaded.</param>
        /// <param name="cacheResourcesFromMultipleModuleVersions">Allow caching the resources from multiple versions of modules.</param>
        private static void LoadDefaultCimKeywords(
            Dictionary<string, ScriptBlock> functionsToDefine,
            Collection<Exception> errors,
            List<string> modulePathList,
            bool cacheResourcesFromMultipleModuleVersions)
        {
            if (!ExperimentalFeature.IsEnabled(DscExperimentalFeatureName))
            {
                Exception exception = new InvalidOperationException(ParserStrings.PS7DscSupportDisabled);
                errors.Add(exception);
                return;
            }
            
            NewApiIsUsed = true;
            DynamicKeyword.Reset();
            Initialize(errors, modulePathList);

            // Initialize->ClearCache resets CacheResourcesFromMultipleModuleVersions to false,
            // workaround is to set it after Initialize method call.
            // Initialize method imports all the Inbox resources and internal classes which belongs to only one version
            // of the module, so it is ok if this property is not set during cache initialization.
            CacheResourcesFromMultipleModuleVersions = cacheResourcesFromMultipleModuleVersions;

            foreach (dynamic cimClass in ClassCache.Values)
            {
                var className = cimClass.CimClassInstance.ClassName;
                var moduleInfo = ByClassModuleCache[className];
                CreateAndRegisterKeywordFromCimClass(moduleInfo.Item1, moduleInfo.Item2, cimClass.CimClassInstance, functionsToDefine, cimClass.DscResRunAsCred);
            }

            // And add the Node keyword definitions
            if (!DynamicKeyword.ContainsKeyword("Node"))
            {
                // Implement dispatch to the Node keyword.
                var nodeKeyword = new DynamicKeyword()
                {
                    BodyMode = DynamicKeywordBodyMode.ScriptBlock,
                    ImplementingModule = s_defaultModuleInfoForResource.Item1,
                    ImplementingModuleVersion = s_defaultModuleInfoForResource.Item2,
                    NameMode = DynamicKeywordNameMode.NameRequired,
                    Keyword = "Node",
                };
                DynamicKeyword.AddKeyword(nodeKeyword);
            }

            // And add the Import-DscResource keyword definitions
            if (!DynamicKeyword.ContainsKeyword("Import-DscResource"))
            {
                // Implement dispatch to the Node keyword.
                var nodeKeyword = new DynamicKeyword()
                {
                    BodyMode = DynamicKeywordBodyMode.Command,
                    ImplementingModule = s_defaultModuleInfoForResource.Item1,
                    ImplementingModuleVersion = s_defaultModuleInfoForResource.Item2,
                    NameMode = DynamicKeywordNameMode.NoName,
                    Keyword = "Import-DscResource",
                    MetaStatement = true,
                    PostParse = ImportResourcePostParse,
                    SemanticCheck = ImportResourceCheckSemantics
                };
                DynamicKeyword.AddKeyword(nodeKeyword);
            }
        }

        // This function is called after parsing the Import-DscResource keyword and it's arguments, but before parsing anything else.
        private static ParseError[] ImportResourcePostParse(DynamicKeywordStatementAst ast)
        {
            var elements = Ast.CopyElements(ast.CommandElements);
            var commandAst = new CommandAst(ast.Extent, elements, TokenKind.Unknown, null);

            const string NameParam = "Name";
            const string ModuleNameParam = "ModuleName";
            const string ModuleVersionParam = "ModuleVersion";

            StaticBindingResult bindingResult = StaticParameterBinder.BindCommand(commandAst, false);

            var errorList = new List<ParseError>();
            foreach (var bindingException in bindingResult.BindingExceptions.Values)
            {
                errorList.Add(new ParseError(bindingException.CommandElement.Extent, "ParameterBindingException", bindingException.BindingException.Message));
            }

            ParameterBindingResult moduleNameBindingResult = null;
            ParameterBindingResult resourceNameBindingResult = null;
            ParameterBindingResult moduleVersionBindingResult = null;

            foreach (var binding in bindingResult.BoundParameters)
            {
                // Error case when positional parameter values are specified
                var boundParameterName = binding.Key;
                var parameterBindingResult = binding.Value;
                if (boundParameterName.All(char.IsDigit))
                {
                    errorList.Add(new ParseError(parameterBindingResult.Value.Extent, "ImportDscResourcePositionalParamsNotSupported", string.Format(CultureInfo.CurrentCulture, ParserStrings.ImportDscResourcePositionalParamsNotSupported)));
                    continue;
                }

                if (NameParam.StartsWith(boundParameterName, StringComparison.OrdinalIgnoreCase))
                {
                    resourceNameBindingResult = parameterBindingResult;
                }
                else if (ModuleNameParam.StartsWith(boundParameterName, StringComparison.OrdinalIgnoreCase))
                {
                    moduleNameBindingResult = parameterBindingResult;
                }
                else if (ModuleVersionParam.StartsWith(boundParameterName, StringComparison.OrdinalIgnoreCase))
                {
                    moduleVersionBindingResult = parameterBindingResult;
                }
                else
                {
                    errorList.Add(new ParseError(parameterBindingResult.Value.Extent, "ImportDscResourceNeedParams", string.Format(CultureInfo.CurrentCulture, ParserStrings.ImportDscResourceNeedParams)));
                }
            }

            if (errorList.Count == 0 && moduleNameBindingResult == null && resourceNameBindingResult == null)
            {
                errorList.Add(new ParseError(ast.Extent, "ImportDscResourceNeedParams", string.Format(CultureInfo.CurrentCulture, ParserStrings.ImportDscResourceNeedParams)));
            }

            // Check here if Version is specified but modulename is not specified
            if (moduleVersionBindingResult != null && moduleNameBindingResult == null)
            {
                // only add this error again to the error list if resources is not null
                // if resources and modules are both null we have already added this error in collection
                // we do not want to do this twice. since we are giving same error ImportDscResourceNeedParams in both cases
                // once we have different error messages for 2 scenarios we can remove this check
                if (resourceNameBindingResult != null)
                {
                    errorList.Add(new ParseError(ast.Extent, "ImportDscResourceNeedModuleNameWithModuleVersion", string.Format(CultureInfo.CurrentCulture, ParserStrings.ImportDscResourceNeedParams)));
                }
            }

            string[] resourceNames = null;
            if (resourceNameBindingResult != null)
            {
                object resourceName = null;
                if (!IsConstantValueVisitor.IsConstant(resourceNameBindingResult.Value, out resourceName, true, true) ||
                    !LanguagePrimitives.TryConvertTo(resourceName, out resourceNames))
                {
                    errorList.Add(new ParseError(resourceNameBindingResult.Value.Extent, "RequiresInvalidStringArgument", string.Format(CultureInfo.CurrentCulture, ParserStrings.RequiresInvalidStringArgument, NameParam)));
                }
            }

            System.Version moduleVersion = null;
            if (moduleVersionBindingResult != null)
            {
                object moduleVer = null;
                if (!IsConstantValueVisitor.IsConstant(moduleVersionBindingResult.Value, out moduleVer, true, true))
                {
                    errorList.Add(new ParseError(moduleVersionBindingResult.Value.Extent, "RequiresArgumentMustBeConstant", ParserStrings.RequiresArgumentMustBeConstant));
                }

                if (moduleVer is double)
                {
                    // this happens in case -ModuleVersion 1.0, then use extent text for that.
                    // The better way to do it would be define static binding API against CommandInfo, that holds information about parameter types.
                    // This way, we can avoid this ugly special-casing and say that -ModuleVersion has type [System.Version].
                    moduleVer = moduleVersionBindingResult.Value.Extent.Text;
                }

                if (!LanguagePrimitives.TryConvertTo(moduleVer, out moduleVersion))
                {
                    errorList.Add(new ParseError(moduleVersionBindingResult.Value.Extent, "RequiresVersionInvalid", ParserStrings.RequiresVersionInvalid));
                }
            }

            ModuleSpecification[] moduleSpecifications = null;
            if (moduleNameBindingResult != null)
            {
                object moduleName = null;
                if (!IsConstantValueVisitor.IsConstant(moduleNameBindingResult.Value, out moduleName, true, true))
                {
                    errorList.Add(new ParseError(moduleNameBindingResult.Value.Extent, "RequiresArgumentMustBeConstant", ParserStrings.RequiresArgumentMustBeConstant));
                }

                if (LanguagePrimitives.TryConvertTo(moduleName, out moduleSpecifications))
                {
                    // if resourceNames are specified then we can not specify multiple modules name
                    if (moduleSpecifications != null && moduleSpecifications.Length > 1 && resourceNames != null)
                    {
                        errorList.Add(new ParseError(moduleNameBindingResult.Value.Extent, "ImportDscResourceMultipleModulesNotSupportedWithName", string.Format(CultureInfo.CurrentCulture, ParserStrings.ImportDscResourceMultipleModulesNotSupportedWithName)));
                    }

                    // if moduleversion is specified then we can not specify multiple modules name
                    if (moduleSpecifications != null && moduleSpecifications.Length > 1 && moduleVersion != null)
                    {
                        errorList.Add(new ParseError(moduleNameBindingResult.Value.Extent, "ImportDscResourceMultipleModulesNotSupportedWithVersion", string.Format(CultureInfo.CurrentCulture, ParserStrings.ImportDscResourceNeedParams)));
                    }

                    // if moduleversion is specified then we can not specify another version in modulespecification object of ModuleName
                    if (moduleSpecifications != null && (moduleSpecifications[0].Version != null || moduleSpecifications[0].MaximumVersion != null) && moduleVersion != null)
                    {
                        errorList.Add(new ParseError(moduleNameBindingResult.Value.Extent, "ImportDscResourceMultipleModuleVersionsNotSupported", string.Format(CultureInfo.CurrentCulture, ParserStrings.ImportDscResourceNeedParams)));
                    }

                    // If moduleVersion is specified we have only one module Name in valid scenario
                    // So update it's version property in module specification object that will be used to load modules
                    if (moduleSpecifications != null && moduleSpecifications[0].Version == null && moduleSpecifications[0].MaximumVersion == null && moduleVersion != null)
                    {
                        moduleSpecifications[0].Version = moduleVersion;
                    }
                }
                else
                {
                    errorList.Add(new ParseError(moduleNameBindingResult.Value.Extent, "RequiresInvalidStringArgument", string.Format(CultureInfo.CurrentCulture, ParserStrings.RequiresInvalidStringArgument, ModuleNameParam)));
                }
            }

            if (errorList.Count == 0)
            {
                // No errors, try to load the resources
                LoadResourcesFromModuleInImportResourcePostParse(ast.Extent, moduleSpecifications, resourceNames, errorList);
            }

            return errorList.ToArray();
        }

        // This function performs semantic checks for Import-DscResource
        private static ParseError[] ImportResourceCheckSemantics(DynamicKeywordStatementAst ast)
        {
            List<ParseError> errorList = null;

            var keywordAst = Ast.GetAncestorAst<DynamicKeywordStatementAst>(ast.Parent);
            while (keywordAst != null)
            {
                if (keywordAst.Keyword.Keyword.Equals("Node"))
                {
                    if (errorList == null)
                    {
                        errorList = new List<ParseError>();
                    }

                    errorList.Add(new ParseError(ast.Extent, "ImportDscResourceInsideNode", string.Format(CultureInfo.CurrentCulture, ParserStrings.ImportDscResourceInsideNode)));
                    break;
                }

                keywordAst = Ast.GetAncestorAst<DynamicKeywordStatementAst>(keywordAst.Parent);
            }

            if (errorList != null)
            {
                return errorList.ToArray();
            }
            else
            {
                return null;
            }
        }

        // This function performs semantic checks for all DSC Resources keywords.
        private static ParseError[] CheckMandatoryPropertiesPresent(DynamicKeywordStatementAst ast)
        {
            HashSet<string> mandatoryPropertiesNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in ast.Keyword.Properties)
            {
                if (pair.Value.Mandatory)
                {
                    mandatoryPropertiesNames.Add(pair.Key);
                }
            }

            // by design mandatoryPropertiesNames are not empty at this point:
            // every resource must have at least one Key property.
            HashtableAst hashtableAst = null;
            foreach (var commandElementsAst in ast.CommandElements)
            {
                hashtableAst = commandElementsAst as HashtableAst;
                if (hashtableAst != null)
                {
                    break;
                }
            }

            if (hashtableAst == null)
            {
                // nothing to validate
                return null;
            }

            foreach (var pair in hashtableAst.KeyValuePairs)
            {
                object evalResultObject;
                if (IsConstantValueVisitor.IsConstant(pair.Item1, out evalResultObject, forAttribute: false, forRequires: false))
                {
                    var presentName = evalResultObject as string;
                    if (presentName != null)
                    {
                        if (mandatoryPropertiesNames.Remove(presentName) && mandatoryPropertiesNames.Count == 0)
                        {
                            // optimization, once all mandatory properties are specified, we can safely exit.
                            return null;
                        }
                    }
                }
            }

            if (mandatoryPropertiesNames.Count > 0)
            {
                ParseError[] errors = new ParseError[mandatoryPropertiesNames.Count];
                var extent = ast.CommandElements[0].Extent;
                int i = 0;
                foreach (string name in mandatoryPropertiesNames)
                {
                    errors[i] = new ParseError(
                        extent,
                        "MissingValueForMandatoryProperty",
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ParserStrings.MissingValueForMandatoryProperty,
                            ast.Keyword.Keyword,
                            ast.Keyword.Properties.First(p => StringComparer.OrdinalIgnoreCase.Equals(p.Value.Name, name)).Value.TypeConstraint,
                            name));
                    i++;
                }

                return errors;
            }

            return null;
        }

        /// <summary>
        /// Load DSC resources from specified module.
        /// </summary>
        /// <param name="scriptExtent">Script statement loading the module, can be null.</param>
        /// <param name="moduleSpecifications">Module information, can be null.</param>
        /// <param name="resourceNames">Name of the resource to be loaded from module.</param>
        /// <param name="errorList">List of errors reported by the method.</param>
        internal static void LoadResourcesFromModuleInImportResourcePostParse(
            IScriptExtent scriptExtent,
            ModuleSpecification[] moduleSpecifications,
            string[] resourceNames,
            List<ParseError> errorList)
        {
            // get all required modules
            var modules = new Collection<PSModuleInfo>();
            if (moduleSpecifications != null)
            {
                foreach (var moduleToImport in moduleSpecifications)
                {
                    bool foundModule = false;
                    var moduleInfos = ModuleCmdletBase.GetModuleIfAvailable(moduleToImport);

                    if (moduleInfos.Count >= 1 && (moduleToImport.Version != null || moduleToImport.Guid != null))
                    {
                        foreach (var psModuleInfo in moduleInfos)
                        {
                            if ((moduleToImport.Guid.HasValue && moduleToImport.Guid.Equals(psModuleInfo.Guid)) ||
                                (moduleToImport.Version != null &&
                                 moduleToImport.Version.Equals(psModuleInfo.Version)))
                            {
                                modules.Add(psModuleInfo);
                                foundModule = true;
                                break;
                            }
                        }
                    }
                    else if (moduleInfos.Count == 1)
                    {
                        modules.Add(moduleInfos[0]);
                        foundModule = true;
                    }

                    if (!foundModule)
                    {
                        if (moduleInfos.Count > 1)
                        {
                            errorList.Add(
                                new ParseError(
                                    scriptExtent,
                                    "MultipleModuleEntriesFoundDuringParse",
                                    string.Format(CultureInfo.CurrentCulture, ParserStrings.MultipleModuleEntriesFoundDuringParse, moduleToImport.Name)));
                        }
                        else
                        {
                            string moduleString = moduleToImport.Version == null
                                ? moduleToImport.Name
                                : string.Format(CultureInfo.CurrentCulture, "<{0}, {1}>", moduleToImport.Name, moduleToImport.Version);

                            errorList.Add(new ParseError(scriptExtent, "ModuleNotFoundDuringParse", string.Format(CultureInfo.CurrentCulture, ParserStrings.ModuleNotFoundDuringParse, moduleString)));
                        }

                        return;
                    }
                }
            }
            else if (resourceNames != null)
            {
                // Lookup the required resources under available PowerShell modules when modulename is not specified
                using (var powerShell = System.Management.Automation.PowerShell.Create(RunspaceMode.CurrentRunspace))
                {
                    powerShell.AddCommand("Get-Module");
                    powerShell.AddParameter("ListAvailable");
                    modules = powerShell.Invoke<PSModuleInfo>();
                }
            }

            // When ModuleName only specified, we need to import all resources from that module
            var resourcesToImport = new List<string>();
            if (resourceNames == null || resourceNames.Length == 0)
            {
                resourcesToImport.Add("*");
            }
            else
            {
                resourcesToImport.AddRange(resourceNames);
            }

            foreach (var moduleInfo in modules)
            {
                var resourcesFound = new List<string>();
                var exceptionList = new System.Collections.ObjectModel.Collection<Exception>();
                LoadPowerShellClassResourcesFromModule(moduleInfo, moduleInfo, resourcesToImport, resourcesFound, exceptionList, null, true, scriptExtent);
                foreach (Exception ex in exceptionList)
                {
                    errorList.Add(new ParseError(scriptExtent, "ClassResourcesLoadingFailed", ex.Message));
                }

                foreach (var resource in resourcesFound)
                {
                    resourcesToImport.Remove(resource);
                }

                if (resourcesToImport.Count == 0)
                {
                    break;
                }
            }

            if (resourcesToImport.Count > 0)
            {
                foreach (var resourceNameToImport in resourcesToImport)
                {
                    if (!resourceNameToImport.Contains("*"))
                    {
                        errorList.Add(new ParseError(scriptExtent, "DscResourcesNotFoundDuringParsing", string.Format(CultureInfo.CurrentCulture, ParserStrings.DscResourcesNotFoundDuringParsing, resourceNameToImport)));
                    }
                }
            }
        }

        private static void LoadPowerShellClassResourcesFromModule(
            PSModuleInfo primaryModuleInfo,
            PSModuleInfo moduleInfo,
            ICollection<string> resourcesToImport,
            ICollection<string> resourcesFound,
            Collection<Exception> errorList,
            Dictionary<string, ScriptBlock> functionsToDefine = null,
            bool recurse = true,
            IScriptExtent extent = null)
        {
            if (primaryModuleInfo._declaredDscResourceExports == null || primaryModuleInfo._declaredDscResourceExports.Count == 0)
            {
                return;
            }

            if (moduleInfo.ModuleType == ModuleType.Binary)
            {
                throw PSTraceSource.NewArgumentException("isConfiguration", ParserStrings.ConfigurationNotSupportedInPowerShellCore);
            }
            else
            {
                string scriptPath = null;
                if (moduleInfo.RootModule != null)
                {
                    scriptPath = Path.Join(moduleInfo.ModuleBase, moduleInfo.RootModule);
                }
                else if (moduleInfo.Path != null)
                {
                    scriptPath = moduleInfo.Path;
                }

                LoadPowerShellClassResourcesFromModule(scriptPath, primaryModuleInfo, resourcesToImport, resourcesFound, functionsToDefine, errorList, extent);
            }

            if (moduleInfo.NestedModules != null && recurse)
            {
                foreach (var nestedModule in moduleInfo.NestedModules)
                {
                    LoadPowerShellClassResourcesFromModule(primaryModuleInfo, nestedModule, resourcesToImport, resourcesFound, errorList, functionsToDefine, recurse: false, extent: extent);
                }
            }
        }

        /// <summary>
        /// Import class resources from module.
        /// </summary>
        /// <param name="moduleInfo">Module information.</param>
        /// <param name="resourcesToImport">Collection of resources to import.</param>
        /// <param name="functionsToDefine">Functions to define.</param>
        /// <param name="errors">List of errors to return.</param>
        /// <returns>The list of resources imported from this module.</returns>
        public static List<string> ImportClassResourcesFromModule(PSModuleInfo moduleInfo, ICollection<string> resourcesToImport, Dictionary<string, ScriptBlock> functionsToDefine, Collection<Exception> errors)
        {
            if (!ExperimentalFeature.IsEnabled(DscExperimentalFeatureName))
            {
                throw new InvalidOperationException(ParserStrings.PS7DscSupportDisabled);
            }

            var resourcesImported = new List<string>();
            LoadPowerShellClassResourcesFromModule(moduleInfo, moduleInfo, resourcesToImport, resourcesImported, errors, functionsToDefine);
            return resourcesImported;
        }

        internal static PSObject[] GenerateJsonClassesForAst(TypeDefinitionAst typeAst, PSModuleInfo module, DSCResourceRunAsCredential runAsBehavior)
        {
            var embeddedInstanceTypes = new List<object>();

            var result = GenerateJsonClassesForAst(typeAst, embeddedInstanceTypes);
            var visitedInstances = new List<object>();
            visitedInstances.Add(typeAst);
            var classes = ProcessEmbeddedInstanceTypes(embeddedInstanceTypes, visitedInstances);
            AddEmbeddedInstanceTypesToCaches(classes, module, runAsBehavior);

            return result;
        }

        private static List<PSObject> ProcessEmbeddedInstanceTypes(List<object> embeddedInstanceTypes, List<object> visitedInstances)
        {
            var result = new List<PSObject>();
            while (embeddedInstanceTypes.Count > 0)
            {
                var batchedTypes = embeddedInstanceTypes.Where(x => !visitedInstances.Contains(x)).ToArray();
                embeddedInstanceTypes.Clear();

                for (int i = batchedTypes.Length - 1; i >= 0; i--)
                {
                    visitedInstances.Add(batchedTypes[i]);
                    var typeAst = batchedTypes[i] as TypeDefinitionAst;
                    if (typeAst != null)
                    {
                        var classes = GenerateJsonClassesForAst(typeAst, embeddedInstanceTypes);
                        result.AddRange(classes);
                    }
                }
            }

            return result;
        }
        
        private static void AddEmbeddedInstanceTypesToCaches(IEnumerable<PSObject> classes, PSModuleInfo module, DSCResourceRunAsCredential runAsBehavior)
        {
            foreach (dynamic c in classes)
            {
                var className = c.ClassName;
                string alias = GetFriendlyName(c);
                var friendlyName = string.IsNullOrEmpty(alias) ? className : alias;
                var moduleQualifiedResourceName = GetModuleQualifiedResourceName(module.Name, module.Version.ToString(), className, friendlyName);
                var classCacheEntry = new DscClassCacheEntry(runAsBehavior, false, c, module.Path);
                ClassCache[moduleQualifiedResourceName] = classCacheEntry;
                GuestConfigClassCache[moduleQualifiedResourceName] = classCacheEntry;
                ByClassModuleCache[className] = new Tuple<string, Version>(module.Name, module.Version);
            }
        }

        internal static string MapTypeNameToMofType(ITypeName typeName, string memberName, string className, out bool isArrayType, out string embeddedInstanceType, List<object> embeddedInstanceTypes, ref string[] enumNames)
        {
            TypeName propTypeName;
            var arrayTypeName = typeName as ArrayTypeName;
            if (arrayTypeName != null)
            {
                isArrayType = true;
                propTypeName = arrayTypeName.ElementType as TypeName;
            }
            else
            {
                isArrayType = false;
                propTypeName = typeName as TypeName;
            }

            if (propTypeName == null || propTypeName._typeDefinitionAst == null)
            {
                throw new NotSupportedException(string.Format(
                    CultureInfo.InvariantCulture,
                    ParserStrings.UnsupportedPropertyTypeOfDSCResourceClass,
                    memberName,
                    typeName.FullName,
                    typeName));
            }

            if (propTypeName._typeDefinitionAst.IsEnum)
            {
                enumNames = propTypeName._typeDefinitionAst.Members.Select(m => m.Name).ToArray();
                isArrayType = false;
                embeddedInstanceType = null;
                return "string";
            }

            if (!embeddedInstanceTypes.Contains(propTypeName._typeDefinitionAst))
            {
                embeddedInstanceTypes.Add(propTypeName._typeDefinitionAst);
            }

            embeddedInstanceType = propTypeName.Name.Replace('.', '_');
            return "Instance";
        }

        private static PSObject[] GenerateJsonClassesForAst(TypeDefinitionAst typeAst, List<object> embeddedInstanceTypes)
        {
            // MOF-based implementation of this used to generate MOF string representing classes/typeAst and pass it to MMI/MOF deserializer to get CimClass array
            // Here we are avoiding that roundtrip by constructing the resulting PSObjects directly
            var className = typeAst.Name;

            string cimSuperClassName = null;
            if (typeAst.Attributes.Any(a => a.TypeName.GetReflectionAttributeType() == typeof(DscResourceAttribute)))
            {
                cimSuperClassName = "OMI_BaseResource";
            }

            var cimClassProperties = ProcessMembers(embeddedInstanceTypes, typeAst, className).ToArray();

            Queue<object> bases = new Queue<object>();
            foreach (var b in typeAst.BaseTypes)
            {
                bases.Enqueue(b);
            }

            while (bases.Count > 0)
            {
                var b = bases.Dequeue();
                var tc = b as TypeConstraintAst;

                if (tc != null)
                {
                    b = tc.TypeName.GetReflectionType();
                    if (b == null)
                    {
                        var td = tc.TypeName as TypeName;
                        if (td != null && td._typeDefinitionAst != null)
                        {
                            ProcessMembers(embeddedInstanceTypes, td._typeDefinitionAst, className);
                            foreach (var b1 in td._typeDefinitionAst.BaseTypes)
                            {
                                bases.Enqueue(b1);
                            }
                        }

                        continue;
                    }
                }
            }

            var result = new PSObject();
            result.Properties.Add(new PSNoteProperty("ClassName", className));
            result.Properties.Add(new PSNoteProperty("ClassVersion", "1.0.0"));
            result.Properties.Add(new PSNoteProperty("FriendlyName", className));
            result.Properties.Add(new PSNoteProperty("SuperClassName", cimSuperClassName));
            result.Properties.Add(new PSNoteProperty("ClassProperties", cimClassProperties));

            return new[] { result };
        }

        private static List<PSObject> ProcessMembers(List<object> embeddedInstanceTypes, TypeDefinitionAst typeDefinitionAst, string className)
        {
            List<PSObject> result = new List<PSObject>();

            foreach (var member in typeDefinitionAst.Members)
            {
                var property = member as PropertyMemberAst;

                if (property == null || property.IsStatic ||
                    property.Attributes.All(a => a.TypeName.GetReflectionAttributeType() != typeof(DscPropertyAttribute)))
                {
                    continue;
                }

                var memberType = property.PropertyType == null
                    ? typeof(object)
                    : property.PropertyType.TypeName.GetReflectionType();

                var attributes = new List<object>();
                for (int i = 0; i < property.Attributes.Count; i++)
                {
                    attributes.Add(property.Attributes[i].GetAttribute());
                }

                string mofType;
                bool isArrayType;
                string embeddedInstanceType;
                string[] enumNames = null;

                if (memberType != null)
                {
                    mofType = MapTypeToMofType(memberType, member.Name, className, out isArrayType, out embeddedInstanceType, embeddedInstanceTypes);
                    if (memberType.IsEnum)
                    {
                        enumNames = Enum.GetNames(memberType);
                    }
                }
                else
                {
                    // PropertyType can't be null, we used typeof(object) above in that case so we don't get here.
                    mofType = MapTypeNameToMofType(property.PropertyType.TypeName, member.Name, className, out isArrayType, out embeddedInstanceType, embeddedInstanceTypes, ref enumNames);
                }

                var propertyObject = new PSObject();
                propertyObject.Properties.Add(new PSNoteProperty(@"Name", member.Name));
                propertyObject.Properties.Add(new PSNoteProperty(@"CimType", mofType + (isArrayType ? "Array" : string.Empty)));
                if (!string.IsNullOrEmpty(embeddedInstanceType))
                {
                    propertyObject.Properties.Add(new PSNoteProperty(@"ReferenceClassName", embeddedInstanceType));
                }

                PSObject attributesPSObject = null;
                foreach (var attr in attributes)
                {
                    var dscProperty = attr as DscPropertyAttribute;
                    if (dscProperty != null)
                    {
                        if (attributesPSObject == null)
                        {
                            attributesPSObject = new PSObject();
                        }

                        if (dscProperty.Key)
                        {
                            attributesPSObject.Properties.Add(new PSNoteProperty("Key", true));
                        }

                        if (dscProperty.Mandatory)
                        {
                            attributesPSObject.Properties.Add(new PSNoteProperty("Required", true));
                        }

                        if (dscProperty.NotConfigurable)
                        {
                            attributesPSObject.Properties.Add(new PSNoteProperty("Read", true));
                        }

                        continue;
                    }

                    var validateSet = attr as ValidateSetAttribute;
                    if (validateSet != null)
                    {
                        if (attributesPSObject == null)
                        {
                            attributesPSObject = new PSObject();
                        }

                        List<string> valueMap = new List<string>(validateSet.ValidValues);
                        List<string> values = new List<string>(validateSet.ValidValues);
                        attributesPSObject.Properties.Add(new PSNoteProperty("ValueMap", valueMap));
                        attributesPSObject.Properties.Add(new PSNoteProperty("Values", values));
                    }
                }

                if (attributesPSObject != null)
                {
                    propertyObject.Properties.Add(new PSNoteProperty(@"Qualifiers", attributesPSObject));
                }

                result.Add(propertyObject);
            }

            return result;
        }

        private static bool GetResourceDefinitionsFromModule(string fileName, out IEnumerable<Ast> resourceDefinitions, Collection<Exception> errorList, IScriptExtent extent)
        {
            resourceDefinitions = null;

            if (string.IsNullOrEmpty(fileName))
            {
                return false;
            }

            if (!".psm1".Equals(Path.GetExtension(fileName), StringComparison.OrdinalIgnoreCase) &&
                !".ps1".Equals(Path.GetExtension(fileName), StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            Token[] tokens;
            ParseError[] errors;
            var ast = Parser.ParseFile(fileName, out tokens, out errors);

            if (errors != null && errors.Length > 0)
            {
                if (errorList != null && extent != null)
                {
                    List<string> errorMessages = new List<string>();
                    foreach (var error in errors)
                    {
                        errorMessages.Add(error.ToString());
                    }
                    
                    PSInvalidOperationException e = PSTraceSource.NewInvalidOperationException(ParserStrings.FailToParseModuleScriptFile, fileName, string.Join(Environment.NewLine, errorMessages));
                    e.SetErrorId("FailToParseModuleScriptFile");
                    errorList.Add(e);
                }

                return false;
            }

            resourceDefinitions = ast.FindAll(
                n =>
                {
                    var typeAst = n as TypeDefinitionAst;
                    if (typeAst != null)
                    {
                        for (int i = 0; i < typeAst.Attributes.Count; i++)
                        {
                            var a = typeAst.Attributes[i];
                            if (a.TypeName.GetReflectionAttributeType() == typeof(DscResourceAttribute))
                            {
                                return true;
                            }
                        }
                    }

                    return false;
                },
                false);

            return true;
        }

        private static bool LoadPowerShellClassResourcesFromModule(string fileName, PSModuleInfo module, ICollection<string> resourcesToImport, ICollection<string> resourcesFound, Dictionary<string, ScriptBlock> functionsToDefine, Collection<Exception> errorList, IScriptExtent extent)
        {
            IEnumerable<Ast> resourceDefinitions;
            if (!GetResourceDefinitionsFromModule(fileName, out resourceDefinitions, errorList, extent))
            {
                return false;
            }

            var result = false;

            const WildcardOptions options = WildcardOptions.IgnoreCase | WildcardOptions.CultureInvariant;
            IEnumerable<WildcardPattern> patternList = SessionStateUtilities.CreateWildcardsFromStrings(module._declaredDscResourceExports, options);

            foreach (var r in resourceDefinitions)
            {
                result = true;
                var resourceDefnAst = (TypeDefinitionAst)r;

                if (!SessionStateUtilities.MatchesAnyWildcardPattern(resourceDefnAst.Name, patternList, true))
                {
                    continue;
                }

                bool skip = true;
                foreach (var toImport in resourcesToImport)
                {
                    if (WildcardPattern.Get(toImport, WildcardOptions.IgnoreCase).IsMatch(resourceDefnAst.Name))
                    {
                        skip = false;
                        break;
                    }
                }

                if (skip)
                {
                    continue;
                }

                // Parse the Resource Attribute to see if RunAs behavior is specified for the resource.
                DSCResourceRunAsCredential runAsBehavior = DSCResourceRunAsCredential.Default;
                foreach (var attr in resourceDefnAst.Attributes)
                {
                    if (attr.TypeName.GetReflectionAttributeType() == typeof(DscResourceAttribute))
                    {
                        foreach (var na in attr.NamedArguments)
                        {
                            if (na.ArgumentName.Equals("RunAsCredential", StringComparison.OrdinalIgnoreCase))
                            {
                                var dscResourceAttribute = attr.GetAttribute() as DscResourceAttribute;
                                if (dscResourceAttribute != null)
                                {
                                    runAsBehavior = dscResourceAttribute.RunAsCredential;
                                }
                            }
                        }
                    }
                }

                var classes = GenerateJsonClassesForAst(resourceDefnAst, module, runAsBehavior);

                ProcessJsonForDynamicKeywords(module, resourcesFound, functionsToDefine, classes, runAsBehavior, errorList);
            }

            return result;
        }

        private static readonly Dictionary<Type, string> s_mapPrimitiveDotNetTypeToMof = new Dictionary<Type, string>()
        {
            { typeof(sbyte), "sint8" },
            { typeof(byte), "uint8" },
            { typeof(short), "sint16" },
            { typeof(ushort), "uint16" },
            { typeof(int), "sint32" },
            { typeof(uint), "uint32" },
            { typeof(long), "sint64" },
            { typeof(ulong), "uint64" },
            { typeof(float), "real32" },
            { typeof(double), "real64" },
            { typeof(bool), "boolean" },
            { typeof(string), "string" },
            { typeof(DateTime), "datetime" },
            { typeof(PSCredential), "string" },
            { typeof(char), "char16" },
        };

        internal static string MapTypeToMofType(Type type, string memberName, string className, out bool isArrayType, out string embeddedInstanceType, List<object> embeddedInstanceTypes)
        {
            isArrayType = false;
            if (type.IsValueType)
            {
                type = Nullable.GetUnderlyingType(type) ?? type;
            }

            if (type.IsEnum)
            {
                embeddedInstanceType = null;
                return "string";
            }

            if (type == typeof(Hashtable))
            {
                // Hashtable is obviously not an array, but in the mof, we represent
                // it as string[] (really, embeddedinstance of MSFT_KeyValuePair), but
                // we need an array to hold each entry in the hashtable.
                isArrayType = true;
                embeddedInstanceType = "MSFT_KeyValuePair";
                return "string";
            }

            if (type == typeof(PSCredential))
            {
                embeddedInstanceType = "MSFT_Credential";
                return "string";
            }

            if (type.IsArray)
            {
                isArrayType = true;
                bool temp;
                var elementType = type.GetElementType();
                if (!elementType.IsArray)
                {
                    return MapTypeToMofType(type.GetElementType(), memberName, className, out temp, out embeddedInstanceType, embeddedInstanceTypes);
                }
            }
            else
            {
                string cimType;
                if (s_mapPrimitiveDotNetTypeToMof.TryGetValue(type, out cimType))
                {
                    embeddedInstanceType = null;
                    return cimType;
                }
            }

            bool supported = false;
            bool missingDefaultConstructor = false;
            if (type.IsValueType)
            {
                if (s_mapPrimitiveDotNetTypeToMof.ContainsKey(type))
                {
                    supported = true;
                }
            }
            else if (!type.IsAbstract)
            {
                // Must have default constructor, at least 1 public property/field, and no base classes
                if (type.GetConstructor(Type.EmptyTypes) == null)
                {
                    missingDefaultConstructor = true;
                }
                else if (type.BaseType == typeof(object) &&
                    (type.GetProperties(BindingFlags.Instance | BindingFlags.Public).Length > 0 ||
                         type.GetFields(BindingFlags.Instance | BindingFlags.Public).Length > 0))
                {
                    supported = true;
                }
            }

            if (supported)
            {
                if (!embeddedInstanceTypes.Contains(type))
                {
                    embeddedInstanceTypes.Add(type);
                }

                // The type is obviously not a string, but in the mof, we represent
                // it as string (really, embeddedinstance of the class type)
                embeddedInstanceType = type.FullName.Replace('.', '_');
                return "string";
            }

            if (missingDefaultConstructor)
            {
                throw new NotSupportedException(string.Format(
                    CultureInfo.InvariantCulture,
                    ParserStrings.DscResourceMissingDefaultConstructor,
                    type.Name));
            }
            else
            {
                throw new NotSupportedException(string.Format(
                    CultureInfo.InvariantCulture,
                    ParserStrings.UnsupportedPropertyTypeOfDSCResourceClass,
                    memberName,
                    type.Name,
                    className));
            }
        }

        private static void ProcessJsonForDynamicKeywords(
            PSModuleInfo module,
            ICollection<string> resourcesFound,
            Dictionary<string, ScriptBlock> functionsToDefine,
            PSObject[] classes,
            DSCResourceRunAsCredential runAsBehavior,
            Collection<Exception> errors)
        {
            foreach (dynamic c in classes)
            {
                var className = c.ClassName;
                string alias = GetFriendlyName(c);
                var friendlyName = string.IsNullOrEmpty(alias) ? className : alias;
                if (!CacheResourcesFromMultipleModuleVersions)
                {
                    // Find & remove the previous version of the resource.
                    List<KeyValuePair<string, DscClassCacheEntry>> resourceList = FindResourceInCache(module.Name, className, friendlyName);

                    if (resourceList.Count > 0 && !string.IsNullOrEmpty(resourceList[0].Key))
                    {
                        ClassCache.Remove(resourceList[0].Key);

                        // keyword is already defined and it is a Inbox resource, remove it
                        if (DynamicKeyword.ContainsKeyword(friendlyName) && resourceList[0].Value.IsImportedImplicitly)
                        {
                            DynamicKeyword.RemoveKeyword(friendlyName);
                        }
                    }
                }

                var moduleQualifiedResourceName = GetModuleQualifiedResourceName(module.Name, module.Version.ToString(), className, friendlyName);
                DscClassCacheEntry existingCacheEntry = null;
                if (ClassCache.TryGetValue(moduleQualifiedResourceName, out existingCacheEntry))
                {
                    if (errors != null)
                    {
                        PSInvalidOperationException e = PSTraceSource.NewInvalidOperationException(ParserStrings.DuplicateCimClassDefinition, className, module.Path, existingCacheEntry.ModulePath);
                        e.SetErrorId("DuplicateCimClassDefinition");
                        errors.Add(e);
                    }
                }
                else
                {
                    var classCacheEntry = new DscClassCacheEntry(runAsBehavior, false, c, module.Path);
                    ClassCache[moduleQualifiedResourceName] = classCacheEntry;
                    GuestConfigClassCache[moduleQualifiedResourceName] = classCacheEntry;
                    ByClassModuleCache[className] = new Tuple<string, Version>(module.Name, module.Version);
                    resourcesFound.Add(className);
                    CreateAndRegisterKeywordFromCimClass(module.Name, module.Version, c, functionsToDefine, runAsBehavior);
                }
            }
        }

        /// <summary>
        /// Returns an error record to use in the case of a malformed resource reference in the DependsOn list.
        /// </summary>
        /// <param name="badDependsOnReference">The malformed resource.</param>
        /// <param name="definingResource">The referencing resource instance.</param>
        /// <returns>Generated error record.</returns>
        public static ErrorRecord GetBadlyFormedRequiredResourceIdErrorRecord(string badDependsOnReference, string definingResource)
        {
            PSInvalidOperationException e = PSTraceSource.NewInvalidOperationException(ParserStrings.GetBadlyFormedRequiredResourceId, badDependsOnReference, definingResource);
            e.SetErrorId("GetBadlyFormedRequiredResourceId");
            return e.ErrorRecord;
        }

        /// <summary>
        /// Returns an error record to use in the case of a malformed resource reference in the exclusive resources list.
        /// </summary>
        /// <param name="badExclusiveResourcereference">The malformed resource.</param>
        /// <param name="definingResource">The referencing resource instance.</param>
        /// <returns>Generated error record.</returns>
        public static ErrorRecord GetBadlyFormedExclusiveResourceIdErrorRecord(string badExclusiveResourcereference, string definingResource)
        {
            PSInvalidOperationException e = PSTraceSource.NewInvalidOperationException(ParserStrings.GetBadlyFormedExclusiveResourceId, badExclusiveResourcereference, definingResource);
            e.SetErrorId("GetBadlyFormedExclusiveResourceId");
            return e.ErrorRecord;
        }

        /// <summary>
        /// If a partial configuration is in 'Pull' Mode, it needs a configuration source.
        /// </summary>
        /// <param name="resourceId">Resource id.</param>
        /// <returns>Generated error record.</returns>
        public static ErrorRecord GetPullModeNeedConfigurationSource(string resourceId)
        {
            PSInvalidOperationException e = PSTraceSource.NewInvalidOperationException(ParserStrings.GetPullModeNeedConfigurationSource, resourceId);
            e.SetErrorId("GetPullModeNeedConfigurationSource");
            return e.ErrorRecord;
        }

        /// <summary>
        /// Refresh Mode can not be Disabled for the Partial Configurations.
        /// </summary>
        /// <param name="resourceId">Resource id.</param>
        /// <returns>Generated error record.</returns>
        public static ErrorRecord DisabledRefreshModeNotValidForPartialConfig(string resourceId)
        {
            PSInvalidOperationException e = PSTraceSource.NewInvalidOperationException(ParserStrings.DisabledRefreshModeNotValidForPartialConfig, resourceId);
            e.SetErrorId("DisabledRefreshModeNotValidForPartialConfig");
            return e.ErrorRecord;
        }

        /// <summary>
        /// Returns an error record to use in the case of a malformed resource reference in the DependsOn list.
        /// </summary>
        /// <param name="duplicateResourceId">The duplicate resource identifier.</param>
        /// <param name="nodeName">The node being defined.</param>
        /// <returns>The error record to use.</returns>
        public static ErrorRecord DuplicateResourceIdInNodeStatementErrorRecord(string duplicateResourceId, string nodeName)
        {
            PSInvalidOperationException e = PSTraceSource.NewInvalidOperationException(ParserStrings.DuplicateResourceIdInNodeStatement, duplicateResourceId, nodeName);
            e.SetErrorId("DuplicateResourceIdInNodeStatement");
            return e.ErrorRecord;
        }

        /// <summary>
        /// Returns an error record to use in the case of a configuration name is invalid.
        /// </summary>
        /// <param name="configurationName">Configuration name.</param>
        /// <returns>Generated error record.</returns>
        public static ErrorRecord InvalidConfigurationNameErrorRecord(string configurationName)
        {
            PSInvalidOperationException e = PSTraceSource.NewInvalidOperationException(ParserStrings.InvalidConfigurationName, configurationName);
            e.SetErrorId("InvalidConfigurationName");
            return e.ErrorRecord;
        }

        /// <summary>
        /// Returns an error record to use in the case of the given value for a property is invalid.
        /// </summary>
        /// <param name="propertyName">Property name.</param>
        /// <param name="value">Property value.</param>
        /// <param name="keywordName">Keyword name.</param>
        /// <param name="validValues">Valid property values.</param>
        /// <returns>Generated error record.</returns>
        public static ErrorRecord InvalidValueForPropertyErrorRecord(string propertyName, string value, string keywordName, string validValues)
        {
            PSInvalidOperationException e = PSTraceSource.NewInvalidOperationException(ParserStrings.InvalidValueForProperty, value, propertyName, keywordName, validValues);
            e.SetErrorId("InvalidValueForProperty");
            return e.ErrorRecord;
        }

        /// <summary>
        /// Returns an error record to use in case the given property is not valid LocalConfigurationManager property.
        /// </summary>
        /// <param name="propertyName">Property name.</param>
        /// <param name="validProperties">Valid properties.</param>
        /// <returns>Generated error record.</returns>
        public static ErrorRecord InvalidLocalConfigurationManagerPropertyErrorRecord(string propertyName, string validProperties)
        {
            PSInvalidOperationException e = PSTraceSource.NewInvalidOperationException(ParserStrings.InvalidLocalConfigurationManagerProperty, propertyName, validProperties);
            e.SetErrorId("InvalidLocalConfigurationManagerProperty");
            return e.ErrorRecord;
        }

        /// <summary>
        /// Returns an error record to use in the case of the given value for a property is not supported.
        /// </summary>
        /// <param name="propertyName">Property name.</param>
        /// <param name="value">Property value.</param>
        /// <param name="keywordName">Keyword name.</param>
        /// <param name="validValues">Valid property values.</param>
        /// <returns>Generated error record.</returns>
        public static ErrorRecord UnsupportedValueForPropertyErrorRecord(string propertyName, string value, string keywordName, string validValues)
        {
            PSInvalidOperationException e = PSTraceSource.NewInvalidOperationException(ParserStrings.UnsupportedValueForProperty, value, propertyName, keywordName, validValues);
            e.SetErrorId("UnsupportedValueForProperty");
            return e.ErrorRecord;
        }

        /// <summary>
        /// Returns an error record to use in the case of no value is provided for a mandatory property.
        /// </summary>
        /// <param name="keywordName">Keyword name.</param>
        /// <param name="typeName">Type name.</param>
        /// <param name="propertyName">Property name.</param>
        /// <returns>Generated error record.</returns>
        public static ErrorRecord MissingValueForMandatoryPropertyErrorRecord(string keywordName, string typeName, string propertyName)
        {
            PSInvalidOperationException e = PSTraceSource.NewInvalidOperationException(ParserStrings.MissingValueForMandatoryProperty, keywordName, typeName, propertyName);
            e.SetErrorId("MissingValueForMandatoryProperty");
            return e.ErrorRecord;
        }

        /// <summary>
        /// Returns an error record to use in the case of more than one values are provided for DebugMode property.
        /// </summary>
        /// <returns>Generated error record.</returns>
        public static ErrorRecord DebugModeShouldHaveOneValue()
        {
            PSInvalidOperationException e = PSTraceSource.NewInvalidOperationException(ParserStrings.DebugModeShouldHaveOneValue);
            e.SetErrorId("DebugModeShouldHaveOneValue");
            return e.ErrorRecord;
        }

        /// <summary>
        /// Return an error to indicate a value is out of range for a dynamic keyword property.
        /// </summary>
        /// <param name="property">Rroperty name.</param>
        /// <param name="name">Resource name.</param>
        /// <param name="providedValue">Provided value.</param>
        /// <param name="lower">Valid range lower bound.</param>
        /// <param name="upper">Valid range upper bound.</param>
        /// <returns>Generated error record.</returns>
        public static ErrorRecord ValueNotInRangeErrorRecord(string property, string name, int providedValue, int lower, int upper)
        {
            PSInvalidOperationException e = PSTraceSource.NewInvalidOperationException(ParserStrings.ValueNotInRange, property, name, providedValue, lower, upper);
            e.SetErrorId("ValueNotInRange");
            return e.ErrorRecord;
        }

        /// <summary>
        /// Returns an error record to use when composite resource and its resource instances both has PsDscRunAsCredentials value.
        /// </summary>
        /// <param name="resourceId">ResourceId of resource.</param>
        /// <returns>Generated error record.</returns>
        public static ErrorRecord PsDscRunAsCredentialMergeErrorForCompositeResources(string resourceId)
        {
            PSInvalidOperationException e = PSTraceSource.NewInvalidOperationException(ParserStrings.PsDscRunAsCredentialMergeErrorForCompositeResources, resourceId);
            e.SetErrorId("PsDscRunAsCredentialMergeErrorForCompositeResources");
            return e.ErrorRecord;
        }

        /// <summary>
        /// Routine to format a usage string from keyword. The resulting string should look like:
        ///        User [string] #ResourceName
        ///        {
        ///            UserName = [string]
        ///            [ Description = [string] ]
        ///            [ Disabled = [bool] ]
        ///            [ Ensure = [string] { Absent | Present }  ]
        ///            [ Force = [bool] ]
        ///            [ FullName = [string] ]
        ///            [ Password = [PSCredential] ]
        ///            [ PasswordChangeNotAllowed = [bool] ]
        ///            [ PasswordChangeRequired = [bool] ]
        ///            [ PasswordNeverExpires = [bool] ]
        ///            [ DependsOn = [string[]] ]
        ///        }
        /// </summary>
        /// <param name="keyword">Dynamic keyword.</param>
        /// <returns>Usage string.</returns>
        public static string GetDSCResourceUsageString(DynamicKeyword keyword)
        {
            StringBuilder usageString;
            switch (keyword.NameMode)
            {
                // Name must be present and simple non-empty bare word
                case DynamicKeywordNameMode.SimpleNameRequired:
                    usageString = new StringBuilder(keyword.Keyword + " [string] # Resource Name");
                    break;

                // Name must be present but can also be an expression
                case DynamicKeywordNameMode.NameRequired:
                    usageString = new StringBuilder(keyword.Keyword + " [string[]] # Name List");
                    break;

                // Name may be optionally present, but if it is present, it must be a non-empty bare word.
                case DynamicKeywordNameMode.SimpleOptionalName:
                    usageString = new StringBuilder(keyword.Keyword + " [ [string] ] # Optional Name");
                    break;

                // Name may be optionally present, expression or bare word
                case DynamicKeywordNameMode.OptionalName:
                    usageString = new StringBuilder(keyword.Keyword + " [ [string[]] ] # Optional NameList");
                    break;

                // Does not take a name
                default:
                    usageString = new StringBuilder(keyword.Keyword);
                    break;
            }

            usageString.Append("\n{\n");

            bool listKeyProperties = true;
            while (true)
            {
                foreach (var prop in keyword.Properties.OrderBy(ob => ob.Key))
                {
                    if (string.Equals(prop.Key, "ResourceId", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var propVal = prop.Value;
                    if ((listKeyProperties && propVal.IsKey) || (!listKeyProperties && !propVal.IsKey))
                    {
                        usageString.Append(propVal.Mandatory ? "    " : "    [ ");
                        usageString.Append(prop.Key);
                        usageString.Append(" = ");
                        usageString.Append(FormatCimPropertyType(propVal, !propVal.Mandatory));
                    }
                }

                if (listKeyProperties)
                {
                    listKeyProperties = false;
                }
                else
                {
                    break;
                }
            }

            usageString.Append('}');

            return usageString.ToString();
        }

        /// <summary>
        /// Format the type name of a CIM property in a presentable way.
        /// </summary>
        /// <param name="prop">Dynamic keyword property.</param>
        /// <param name="isOptionalProperty">If this is optional property or not.</param>
        /// <returns>CIM property type string.</returns>
        private static StringBuilder FormatCimPropertyType(DynamicKeywordProperty prop, bool isOptionalProperty)
        {
            string cimTypeName = prop.TypeConstraint;
            StringBuilder formattedTypeString = new StringBuilder();

            if (string.Equals(cimTypeName, "MSFT_Credential", StringComparison.OrdinalIgnoreCase))
            {
                formattedTypeString.Append("[PSCredential]");
            }
            else if (string.Equals(cimTypeName, "MSFT_KeyValuePair", StringComparison.OrdinalIgnoreCase) || string.Equals(cimTypeName, "MSFT_KeyValuePair[]", StringComparison.OrdinalIgnoreCase))
            {
                formattedTypeString.Append("[Hashtable]");
            }
            else
            {
                string convertedTypeString = System.Management.Automation.LanguagePrimitives.ConvertTypeNameToPSTypeName(cimTypeName);
                if (!string.IsNullOrEmpty(convertedTypeString) && !string.Equals(convertedTypeString, "[]", StringComparison.OrdinalIgnoreCase))
                {
                    formattedTypeString.Append(convertedTypeString);
                }
                else
                {
                    formattedTypeString.Append("[" + cimTypeName + "]");
                }
            }

            // Do the property values map
            if (prop.ValueMap != null && prop.ValueMap.Count > 0)
            {
                formattedTypeString.Append(" { " + string.Join(" | ", prop.ValueMap.Keys.OrderBy(x => x)) + " }");
            }

            // We prepend optional property with "[" so close out it here. This way it is shown with [ ] to indication optional
            if (isOptionalProperty)
            {
                formattedTypeString.Append(']');
            }

            formattedTypeString.Append('\n');

            return formattedTypeString;
        }

        /// <summary>
        /// The scriptblock that implements the CIM keyword functionality.
        /// </summary>
        private static ScriptBlock CimKeywordImplementationFunction
        {
            get
            {
                // The scriptblock cache will handle mutual exclusion
                return s_cimKeywordImplementationFunction ??= ScriptBlock.Create(CimKeywordImplementationFunctionText);
            }
        }

        private static ScriptBlock s_cimKeywordImplementationFunction;

        private const string CimKeywordImplementationFunctionText = @"
    param (
        [Parameter(Mandatory)]
            $KeywordData,
        [Parameter(Mandatory)]
            $Name,
        [Parameter(Mandatory)]
        [Hashtable]
            $Value,
        [Parameter(Mandatory)]
            $SourceMetadata
    )

# walk the call stack to get at all of the enclosing configuration resource IDs
    $stackedConfigs = @(Get-PSCallStack |
        where { ($null -ne $_.InvocationInfo.MyCommand) -and ($_.InvocationInfo.MyCommand.CommandType -eq 'Configuration') })
# keep all but the top-most
    $stackedConfigs = $stackedConfigs[0..(@($stackedConfigs).Length - 2)]
# and build the complex resource ID suffix.
    $complexResourceQualifier = ( $stackedConfigs | ForEach-Object { '[' + $_.Command + ']' + $_.InvocationInfo.BoundParameters['InstanceName'] } ) -join '::'

#
# Utility function used to validate that the DependsOn arguments are well-formed.
# The function also adds them to the define nodes resource collection.
# in the case of resources generated inside a script resource, this routine
# will also fix up the DependsOn references to '[Type]Instance::[OuterType]::OuterInstance
#
    function Test-DependsOn
    {

# make sure the references are well-formed
        $updatedDependsOn = foreach ($DependsOnVar in $value['DependsOn']) {
# match [ResourceType]ResourceName. ResourceName should starts with [a-z_0-9] followed by [a-z_0-9\p{Zs}\.\\-]*
            if ($DependsOnVar -notmatch '^\[[a-z]\w*\][a-z_0-9][a-z_0-9\p{Zs}\.\\-]*$')
            {
                Update-ConfigurationErrorCount
                Write-Error -ErrorRecord ([Microsoft.PowerShell.DesiredStateConfiguration.Internal.Json.DscClassCache]::GetBadlyFormedRequiredResourceIdErrorRecord($DependsOnVar, $resourceId))
            }

# Fix up DependsOn for nested names
            if ($MyTypeName -and $typeName -ne $MyTypeName -and $InstanceName)
            {
                ""$DependsOnVar::$complexResourceQualifier""
            }
            else
            {
                $DependsOnVar
            }
        }

        $value['DependsOn']= $updatedDependsOn

        if($null -ne $DependsOn)
        {
#
# Combine DependsOn with dependson from outer composite resource
# which is set as local variable $DependsOn at the composite resource context
#
            $value['DependsOn']= @($value['DependsOn']) + $DependsOn
        }

# Save the resource id in a per-node dictionary to do cross validation at the end
        Set-NodeResources $resourceId @( $value['DependsOn'])

# Remove depends on because it need to be fixed up for composite resources
# We do it in ValidateNodeResource and Update-Depends on in configuration/Node function
        $value.Remove('DependsOn')
    }

# A copy of the value object with correctly-cased property names
    $canonicalizedValue = @{}

    $typeName = $keywordData.ResourceName # CIM type
    $keywordName = $keywordData.Keyword   # user-friendly alias that is used in scripts
    $keyValues = ''
    $debugPrefix = ""   ${TypeName}:"" # set up a debug prefix string that makes it easier to track what's happening.

    Write-Debug ""${debugPrefix} RESOURCE PROCESSING STARTED [KeywordName='$keywordName'] Function='$($myinvocation.Invocationname)']""

# Check whether it's an old style metaconfig
    $OldMetaConfig = $false
    if ((-not $IsMetaConfig) -and ($keywordName -ieq 'LocalConfigurationManager')) {
        $OldMetaConfig = $true
    }

# Check to see if it's a resource keyword. If so add the meta-properties to the canonical property collection.
    $resourceId = $null
# todo: need to include configuration managers and partial configuration
    if (($keywordData.Properties.Keys -contains 'DependsOn') -or (($KeywordData.ImplementingModule -ieq 'PSDesiredStateConfigurationEngine') -and ($KeywordData.NameMode -eq [System.Management.Automation.Language.DynamicKeywordNameMode]::NameRequired)))
    {

        $resourceId = ""[$keywordName]$name""
        if ($MyTypeName -and $keywordName -ne $MyTypeName -and $InstanceName)
        {
            $resourceId += ""::$complexResourceQualifier""
        }

        Write-Debug ""${debugPrefix} ResourceID = $resourceId""

# copy the meta-properties
        $canonicalizedValue['ResourceID'] = $resourceId
        $canonicalizedValue['SourceInfo'] = $SourceMetadata
        if(-not $IsMetaConfig)
        {
            $canonicalizedValue['ModuleName'] = $keywordData.ImplementingModule
            $canonicalizedValue['ModuleVersion'] = $keywordData.ImplementingModuleVersion -as [string]
        }

# see if there is already a resource with this ID.
        if (Test-NodeResources $resourceId)
        {
            Update-ConfigurationErrorCount
            Write-Error -ErrorRecord ([Microsoft.PowerShell.DesiredStateConfiguration.Internal.Json.DscClassCache]::DuplicateResourceIdInNodeStatementErrorRecord($resourceId, (Get-PSCurrentConfigurationNode)))
        }
        else
        {
# If there are prerequisite resources, validate that the references are well-formed strings
# This routine also adds the resource to the global node resources table.
            Test-DependsOn

# Check if PsDscRunCredential is being specified as Arguments to Configuration
        if($null -ne $PsDscRunAsCredential)
        {
# Check if resource is also trying to set the value for RunAsCred
# In that case we will generate error during compilation, this is merge error
        if($null -ne $value['PsDscRunAsCredential'])
        {
            Update-ConfigurationErrorCount
            Write-Error -ErrorRecord ([Microsoft.PowerShell.DesiredStateConfiguration.Internal.Json.DscClassCache]::PsDscRunAsCredentialMergeErrorForCompositeResources($resourceId))
        }
# Set the Value of RunAsCred to that of outer configuration
        else
        {
            $value['PsDscRunAsCredential'] = $PsDscRunAsCredential
        }
    }

# Save the resource id in a per-node dictionary to do cross validation at the end
            if($keywordData.ImplementingModule -ieq ""PSDesiredStateConfigurationEngine"")
            {
#$keywordName is PartialConfiguration
                if($keywordName -eq 'PartialConfiguration')
                {
# RefreshMode is 'Pull' and .ConfigurationSource is empty
                    if($value['RefreshMode'] -eq 'Pull' -and -not $value['ConfigurationSource'])
                    {
                        Update-ConfigurationErrorCount
                        Write-Error -ErrorRecord ([Microsoft.PowerShell.DesiredStateConfiguration.Internal.Json.DscClassCache]::GetPullModeNeedConfigurationSource($resourceId))
                    }

# Verify that RefreshMode is not Disabled for Partial configuration
                    if($value['RefreshMode'] -eq 'Disabled')
                    {
                        Update-ConfigurationErrorCount
                        Write-Error -ErrorRecord ([Microsoft.PowerShell.DesiredStateConfiguration.Internal.Json.DscClassCache]::DisabledRefreshModeNotValidForPartialConfig($resourceId))
                    }

                    if($null -ne $value['ConfigurationSource'])
                    {
                        Set-NodeManager $resourceId $value['ConfigurationSource']
                    }

                    if($null -ne $value['ResourceModuleSource'])
                    {
                        Set-NodeResourceSource $resourceId $value['ResourceModuleSource']
                    }
                }

                if($null -ne $value['ExclusiveResources'])
                {
# make sure the references are well-formed
                    foreach ($ExclusiveResource in $value['ExclusiveResources']) {
                        if (($ExclusiveResource -notmatch '^[a-z][a-z_0-9]*\\[a-z][a-z_0-9]*$') -and ($ExclusiveResource -notmatch '^[a-z][a-z_0-9]*$') -and ($ExclusiveResource -notmatch '^[a-z][a-z_0-9]*\\\*$'))
                        {
                            Update-ConfigurationErrorCount
                            Write-Error -ErrorRecord ([Microsoft.PowerShell.DesiredStateConfiguration.Internal.Json.DscClassCache]::GetBadlyFormedExclusiveResourceIdErrorRecord($ExclusiveResource, $resourceId))
                        }
                    }

# Save the resource id in a per-node dictionary to do cross validation at the end
# Validate resource exist
# Also update the resource reference from module\friendlyname to module\name
                    $value['ExclusiveResources'] = @(Set-NodeExclusiveResources $resourceId @( $value['ExclusiveResources'] ))
                }
            }
        }
    }
    else
    {
        Write-Debug ""${debugPrefix} TYPE IS NOT AS DSC RESOURCE""
    }

#
# Copy the user-supplied values into a new collection with canonicalized property names
#
    foreach ($key in $keywordData.Properties.Keys)
    {
        Write-Debug ""${debugPrefix} Processing property '$key' [""

        if ($value.Contains($key))
        {
            if ($OldMetaConfig -and (-not ($V1MetaConfigPropertyList -contains $key)))
            {
                Write-Error -ErrorRecord ([Microsoft.PowerShell.DesiredStateConfiguration.Internal.Json.DscClassCache]::InvalidLocalConfigurationManagerPropertyErrorRecord($key, ($V1MetaConfigPropertyList -join ', ')))
                Update-ConfigurationErrorCount
            }
# see if there is a list of allowed values for this property (similar to an enum)
            $allowedValues = $keywordData.Properties[$key].Values
# If there is and user-provided value is not in that list, write an error.
            if ($allowedValues)
            {
                if(($null -eq $value[$key]) -and ($allowedValues -notcontains $value[$key]))
                {
                    Write-Error -ErrorRecord ([Microsoft.PowerShell.DesiredStateConfiguration.Internal.Json.DscClassCache]::InvalidValueForPropertyErrorRecord($key, ""$($value[$key])"", $keywordData.Keyword, ($allowedValues -join ', ')))
                    Update-ConfigurationErrorCount
                }
                else
                {
                    $notAllowedValue=$null
                    foreach($v in $value[$key])
                    {
                        if($allowedValues -notcontains $v)
                        {
                            $notAllowedValue +=$v.ToString() + ', '
                        }
                    }

                    if($notAllowedValue)
                    {
                        $notAllowedValue = $notAllowedValue.Substring(0, $notAllowedValue.Length -2)
                        Write-Error -ErrorRecord ([Microsoft.PowerShell.DesiredStateConfiguration.Internal.Json.DscClassCache]::UnsupportedValueForPropertyErrorRecord($key, $notAllowedValue, $keywordData.Keyword, ($allowedValues -join ', ')))
                        Update-ConfigurationErrorCount
                    }
                }
            }

# see if a value range is defined for this property
            $allowedRange = $keywordData.Properties[$key].Range
            if($allowedRange)
            {
                $castedValue = $value[$key] -as [int]
                if((($castedValue -is [int]) -and (($castedValue -lt  $keywordData.Properties[$key].Range.Item1) -or ($castedValue -gt $keywordData.Properties[$key].Range.Item2))) -or ($null -eq $castedValue))
                {
                    Write-Error -ErrorRecord ([Microsoft.PowerShell.DesiredStateConfiguration.Internal.Json.DscClassCache]::ValueNotInRangeErrorRecord($key, $keywordName, $value[$key],  $keywordData.Properties[$key].Range.Item1,  $keywordData.Properties[$key].Range.Item2))
                    Update-ConfigurationErrorCount
                }
            }

            Write-Debug ""${debugPrefix}        Canonicalized property '$key' = '$($value[$key])'""

            if ($keywordData.Properties[$key].IsKey)
            {
                if($null -eq $value[$key])
                {
                    $keyValues += ""::__NULL__""
                }
                else
                {
                    $keyValues += ""::"" + $value[$key]
                }
            }

# see if ValueMap is also defined for this property (actual values)
            $allowedValueMap = $keywordData.Properties[$key].ValueMap
#if it is and the ValueMap contains the user-provided value as a key, use the actual value
            if ($allowedValueMap -and $allowedValueMap.ContainsKey($value[$key]))
            {
                $canonicalizedValue[$key] = $allowedValueMap[$value[$key]]
            }
            else
            {
                $canonicalizedValue[$key] = $value[$key]
            }
        }
        elseif ($keywordData.Properties[$key].Mandatory)
        {
# If the property was mandatory but the user didn't provide a value, write and error.
            Write-Error -ErrorRecord ([Microsoft.PowerShell.DesiredStateConfiguration.Internal.Json.DscClassCache]::MissingValueForMandatoryPropertyErrorRecord($keywordData.Keyword, $keywordData.Properties[$key].TypeConstraint, $Key))
            Update-ConfigurationErrorCount
        }

        Write-Debug ""${debugPrefix}    Processing completed '$key' ]""
    }

    if($keyValues)
    {
        $keyValues = $keyValues.Substring(2) # Remove the leading '::'
        Add-NodeKeys $keyValues $keywordName
        Test-ConflictingResources $keywordName $canonicalizedValue $keywordData
    }

# update OMI_ConfigurationDocument
    if($IsMetaConfig)
    {
        if($keywordData.ResourceName -eq 'OMI_ConfigurationDocument')
        {
            if($(Get-PSMetaConfigurationProcessed))
            {
                $PSMetaConfigDocumentInstVersionInfo = Get-PSMetaConfigDocumentInstVersionInfo
                $canonicalizedValue['MinimumCompatibleVersion']=$PSMetaConfigDocumentInstVersionInfo['MinimumCompatibleVersion']
            }
            else
            {
                Set-PSMetaConfigDocInsProcessedBeforeMeta
                $canonicalizedValue['MinimumCompatibleVersion']='1.0.0'
            }
        }

        if(($keywordData.ResourceName -eq 'MSFT_WebDownloadManager') `
            -or ($keywordData.ResourceName -eq 'MSFT_FileDownloadManager') `
            -or ($keywordData.ResourceName -eq 'MSFT_WebResourceManager') `
            -or ($keywordData.ResourceName -eq 'MSFT_FileResourceManager') `
            -or ($keywordData.ResourceName -eq 'MSFT_WebReportManager') `
            -or ($keywordData.ResourceName -eq 'MSFT_SignatureValidation') `
            -or ($keywordData.ResourceName -eq 'MSFT_PartialConfiguration'))
        {
            Set-PSMetaConfigVersionInfoV2
        }
    }
    elseif($keywordData.ResourceName -eq 'OMI_ConfigurationDocument')
    {
        $canonicalizedValue['MinimumCompatibleVersion']='1.0.0'
        $canonicalizedValue['CompatibleVersionAdditionalProperties']=@('Omi_BaseResource:ConfigurationName')
    }

    if(($keywordData.ResourceName -eq 'MSFT_DSCMetaConfiguration') -or ($keywordData.ResourceName -eq 'MSFT_DSCMetaConfigurationV2'))
    {
        if($canonicalizedValue['DebugMode'] -and @($canonicalizedValue['DebugMode']).Length -gt 1)
        {
# we only allow one value for debug mode now.
            Write-Error -ErrorRecord ([Microsoft.PowerShell.DesiredStateConfiguration.Internal.Json.DscClassCache]::DebugModeShouldHaveOneValue())
            Update-ConfigurationErrorCount
        }
    }

# Generate the MOF text for this resource instance.
# when generate mof text for OMI_ConfigurationDocument we handle below two cases:
# 1. we will add versioning related property based on meta configuration instance already process
# 2. we update the existing OMI_ConfigurationDocument instance if it already exists when process meta configuration instance
    $aliasId = ConvertTo-MOFInstance $keywordName $canonicalizedValue

# If a OMI_ConfigurationDocument is executed outside of a node statement, it becomes the default
# for all nodes that don't have an explicit OMI_ConfigurationDocument declaration
    if ($keywordData.ResourceName -eq 'OMI_ConfigurationDocument' -and -not (Get-PSCurrentConfigurationNode))
    {
        $data = Get-MoFInstanceText $aliasId
        Write-Debug ""${debugPrefix} DEFINING DEFAULT CONFIGURATION DOCUMENT: $data""
        Set-PSDefaultConfigurationDocument $data
    }

    Write-Debug ""${debugPrefix} MOF alias for this resource is '$aliasId'""

# always return the aliasId so the generated file will be well-formed if not valid
    $aliasId

    Write-Debug ""${debugPrefix} RESOURCE PROCESSING COMPLETED. TOTAL ERROR COUNT: $(Get-ConfigurationErrorCount)""

    ";
    }
}
