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

using Microsoft.Management.Infrastructure;
using Microsoft.Management.Infrastructure.Generic;
using Microsoft.Management.Infrastructure.Serialization;
using Microsoft.PowerShell.Commands;

using static Microsoft.PowerShell.SecureStringHelper;

namespace Microsoft.PowerShell.DesiredStateConfiguration.Internal
{
    /// <summary>
    /// </summary>
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes",
        Justification = "Needed Internal use only")]
    public static class DscRemoteOperationsClass
    {
        /// <summary>
        /// Convert Cim Instance representing Resource desired state to Powershell Class Object.
        /// </summary>
        public static object ConvertCimInstanceToObject(Type targetType, CimInstance instance, string moduleName)
        {
            var className = instance.CimClass.CimSystemProperties.ClassName;
            object targetObject = null;
            string errorMessage;

            using (System.Management.Automation.PowerShell powerShell = System.Management.Automation.PowerShell.Create(RunspaceMode.CurrentRunspace))
            {
                powerShell.AddScript("param($targetType,$moduleName) & (Microsoft.PowerShell.Core\\Get-Module $moduleName) { New-Object $targetType } ");
                powerShell.AddArgument(targetType);
                powerShell.AddArgument(moduleName);

                Collection<PSObject> psExecutionResult = powerShell.Invoke();
                if (psExecutionResult.Count == 1)
                {
                    targetObject = psExecutionResult[0].BaseObject;
                }
                else
                {
                    Exception innerException = null;
                    if (powerShell.Streams.Error != null && powerShell.Streams.Error.Count > 0)
                    {
                        innerException = powerShell.Streams.Error[0].Exception;
                    }

                    errorMessage = string.Format(CultureInfo.CurrentCulture, ParserStrings.InstantiatePSClassObjectFailed, className);
                    var invalidOperationException = new InvalidOperationException(errorMessage, innerException);
                    throw invalidOperationException;
                }
            }

            foreach (var property in instance.CimInstanceProperties)
            {
                if (property.Value != null)
                {
                    MemberInfo[] memberInfo = targetType.GetMember(property.Name, BindingFlags.Public | BindingFlags.Instance);

                    // verify property exists in corresponding class type
                    if (memberInfo == null
                        || memberInfo.Length > 1
                        || (memberInfo[0] is not PropertyInfo && memberInfo[0] is not FieldInfo))
                    {
                        errorMessage = string.Format(CultureInfo.CurrentCulture, ParserStrings.PropertyNotDeclaredInPSClass, new object[] { property.Name, className });
                        var invalidOperationException = new InvalidOperationException(errorMessage);
                        throw invalidOperationException;
                    }

                    var member = memberInfo[0];
                    var memberType = (member is FieldInfo)
                        ? ((FieldInfo)member).FieldType
                        : ((PropertyInfo)member).PropertyType;

                    object targetValue = null;
                    switch (property.CimType)
                    {
                        case CimType.Instance:
                            {
                                var cimPropertyInstance = property.Value as CimInstance;
                                if (cimPropertyInstance != null &&
                                    cimPropertyInstance.CimClass != null &&
                                    cimPropertyInstance.CimClass.CimSystemProperties != null &&
                                    string.Equals(
                                        cimPropertyInstance.CimClass.CimSystemProperties.ClassName,
                                        "MSFT_Credential", StringComparison.OrdinalIgnoreCase))
                                {
                                    targetValue = ConvertCimInstancePsCredential(moduleName, cimPropertyInstance);
                                }
                                else
                                {
                                    targetValue = ConvertCimInstanceToObject(memberType, cimPropertyInstance, moduleName);
                                }

                                if (targetValue == null)
                                {
                                    return null;
                                }
                            }

                            break;
                        case CimType.InstanceArray:
                            {
                                if (memberType == typeof(Hashtable))
                                {
                                    targetValue = ConvertCimInstanceHashtable(moduleName, (CimInstance[])property.Value);
                                }
                                else
                                {
                                    var instanceArray = (CimInstance[])property.Value;
                                    if (!memberType.IsArray)
                                    {
                                        errorMessage = string.Format(CultureInfo.CurrentCulture, ParserStrings.ExpectArrayTypeOfPropertyInPSClass, new object[] { property.Name, className });
                                        var invalidOperationException = new InvalidOperationException(errorMessage);
                                        throw invalidOperationException;
                                    }

                                    var elementType = memberType.GetElementType();
                                    var targetArray = Array.CreateInstance(elementType, instanceArray.Length);
                                    for (int i = 0; i < instanceArray.Length; i++)
                                    {
                                        var obj = ConvertCimInstanceToObject(elementType, instanceArray[i], moduleName);
                                        if (obj == null)
                                        {
                                            return null;
                                        }

                                        targetArray.SetValue(obj, i);
                                    }

                                    targetValue = targetArray;
                                }
                            }

                            break;
                        default:
                            targetValue = LanguagePrimitives.ConvertTo(property.Value, memberType, CultureInfo.InvariantCulture);
                            break;
                    }

                    if (targetValue == null)
                    {
                        errorMessage = string.Format(CultureInfo.CurrentCulture, ParserStrings.ConvertCimPropertyToObjectPropertyFailed, new object[] { property.Name, className });
                        var invalidOperationException = new InvalidOperationException(errorMessage);
                        throw invalidOperationException;
                    }

                    if (member is FieldInfo)
                    {
                        ((FieldInfo)member).SetValue(targetObject, targetValue);
                    }

                    if (member is PropertyInfo)
                    {
                        ((PropertyInfo)member).SetValue(targetObject, targetValue);
                    }
                }
            }

            return targetObject;
        }

        /// <summary>
        /// Convert hashtable from Ciminstance to hashtable primitive type.
        /// </summary>
        /// <param name="providerName"></param>
        /// <param name="arrayInstance"></param>
        /// <returns></returns>
        private static object ConvertCimInstanceHashtable(string providerName, CimInstance[] arrayInstance)
        {
            var result = new Hashtable();
            string errorMessage;

            try
            {
                foreach (var keyValuePair in arrayInstance)
                {
                    var key = keyValuePair.CimInstanceProperties["Key"];
                    var value = keyValuePair.CimInstanceProperties["Value"];

                    if (key == null || value == null)
                    {
                        errorMessage = string.Format(CultureInfo.CurrentCulture, ParserStrings.InvalidHashtable, providerName);
                        var invalidOperationException = new InvalidOperationException(errorMessage);
                        throw invalidOperationException;
                    }

                    result.Add(LanguagePrimitives.ConvertTo<string>(key.Value), LanguagePrimitives.ConvertTo<string>(value.Value));
                }
            }
            catch (Exception exception)
            {
                errorMessage = string.Format(CultureInfo.CurrentCulture, ParserStrings.InvalidHashtable, providerName);
                var invalidOperationException = new InvalidOperationException(errorMessage, exception);
                throw invalidOperationException;
            }

            return result;
        }
        /// <summary>
        /// Convert CIM instance to PS Credential.
        /// </summary>
        /// <param name="providerName"></param>
        /// <param name="propertyInstance"></param>
        /// <returns></returns>
        private static object ConvertCimInstancePsCredential(string providerName, CimInstance propertyInstance)
        {
            string errorMessage;
            string userName;
            string plainPassWord;

            try
            {
                userName = propertyInstance.CimInstanceProperties["UserName"].Value as string;
                if (string.IsNullOrEmpty(userName))
                {
                    errorMessage = string.Format(CultureInfo.CurrentCulture, ParserStrings.InvalidUserName, providerName);
                    var invalidOperationException = new InvalidOperationException(errorMessage);
                    throw invalidOperationException;
                }
            }
            catch (CimException exception)
            {
                errorMessage = string.Format(CultureInfo.CurrentCulture, ParserStrings.InvalidUserName, providerName);
                var invalidOperationException = new InvalidOperationException(errorMessage, exception);
                throw invalidOperationException;
            }

            try
            {
                plainPassWord = propertyInstance.CimInstanceProperties["PassWord"].Value as string;

                // In future we might receive password in an encrypted format. Make sure we add
                // the decryption login in this method.
                if (string.IsNullOrEmpty(plainPassWord))
                {
                    errorMessage = string.Format(CultureInfo.CurrentCulture, ParserStrings.InvalidPassword, providerName);
                    var invalidOperationException = new InvalidOperationException(errorMessage);
                    throw invalidOperationException;
                }
            }
            catch (CimException exception)
            {
                errorMessage = string.Format(CultureInfo.CurrentCulture, ParserStrings.InvalidPassword, providerName);
                var invalidOperationException = new InvalidOperationException(errorMessage, exception);
                throw invalidOperationException;
            }

            SecureString password = SecureStringHelper.FromPlainTextString(plainPassWord);

            password.MakeReadOnly();
            return new PSCredential(userName, password);
        }
    }
}

namespace Microsoft.PowerShell.DesiredStateConfiguration
{
    /// <summary>
    /// To make it easier to specify -ConfigurationData parameter, we add an ArgumentTransformationAttribute here.
    /// When the input data is of type string and is valid path to a file that can be converted to hashtable, we do
    /// the conversion and return the converted value. Otherwise, we just return the input data.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class ArgumentToConfigurationDataTransformationAttribute : ArgumentTransformationAttribute
    {
        /// <summary>
        /// Convert a file of ConfigurationData into a hashtable.
        /// </summary>
        /// <param name="engineIntrinsics"></param>
        /// <param name="inputData"></param>
        /// <returns></returns>
        public override object Transform(EngineIntrinsics engineIntrinsics, object inputData)
        {
            var configDataPath = inputData as string;
            if (string.IsNullOrEmpty(configDataPath))
            {
                return inputData;
            }

            if (engineIntrinsics == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(engineIntrinsics));
            }

            return PsUtils.EvaluatePowerShellDataFileAsModuleManifest(
                      "ConfigurationData",
                      configDataPath,
                      engineIntrinsics.SessionState.Internal.ExecutionContext,
                      skipPathValidation: false);
        }
    }

    /// <summary>
    /// <para>
    /// Represents a communication channel to a CIM server.
    /// </para>
    /// <para>
    /// This is the main entry point of the Microsoft.Management.Infrastructure API.
    /// All CIM operations are represented as methods of this class.
    /// </para>
    /// </summary>
    internal class CimDSCParser
    {
        private readonly CimMofDeserializer _deserializer;
        private readonly CimMofDeserializer.OnClassNeeded _onClassNeeded;

        /// <summary>
        /// </summary>
        internal CimDSCParser(CimMofDeserializer.OnClassNeeded onClassNeeded)
        {
            _deserializer = CimMofDeserializer.Create();
            _onClassNeeded = onClassNeeded;
        }

        /// <summary>
        /// </summary>
        internal CimDSCParser(CimMofDeserializer.OnClassNeeded onClassNeeded, Microsoft.Management.Infrastructure.Serialization.MofDeserializerSchemaValidationOption validationOptions)
        {
            _deserializer = CimMofDeserializer.Create();
            _deserializer.SchemaValidationOption = validationOptions;
            _onClassNeeded = onClassNeeded;
        }

        /// <summary>
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "3#", Justification = "Have to return 2 things.  Wrapping those 2 things in a class will result in a more, not less complexity")]
        internal List<CimInstance> ParseInstanceMof(string filePath)
        {
            uint offset = 0;
            var buffer = GetFileContent(filePath);
            try
            {
                var result = new List<CimInstance>(_deserializer.DeserializeInstances(buffer, ref offset, _onClassNeeded, null));
                return result;
            }
            catch (CimException exception)
            {
                PSInvalidOperationException e = PSTraceSource.NewInvalidOperationException(
                    exception, ParserStrings.CimDeserializationError, filePath);

                e.SetErrorId("CimDeserializationError");
                throw e;
            }
        }

        /// <summary>
        /// Read file content to byte array.
        /// </summary>
        /// <param name="fullFilePath"></param>
        /// <returns></returns>
        internal static byte[] GetFileContent(string fullFilePath)
        {
            if (string.IsNullOrEmpty(fullFilePath))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(fullFilePath));
            }

            if (!File.Exists(fullFilePath))
            {
                var errorMessage = string.Format(CultureInfo.CurrentCulture, ParserStrings.FileNotFound, fullFilePath);
                throw PSTraceSource.NewArgumentException(nameof(fullFilePath), errorMessage);
            }

            using (FileStream fs = File.OpenRead(fullFilePath))
            {
                var bytes = new byte[fs.Length];
                fs.Read(bytes, 0, Convert.ToInt32(fs.Length));
                return bytes;
            }
        }

        internal List<CimClass> ParseSchemaMofFileBuffer(string mof)
        {
            uint offset = 0;
#if UNIX
            // OMI only supports UTF-8 without BOM
            var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
#else
            // This is what we traditionally use with Windows
            // DSC asked to keep it UTF-32 for Windows
            var encoding = new UnicodeEncoding();
#endif

            var buffer = encoding.GetBytes(mof);

            var result = new List<CimClass>(_deserializer.DeserializeClasses(buffer, ref offset, null, null, null, _onClassNeeded, null));
            return result;
        }

        /// <summary>
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "3#", Justification = "Have to return 2 things.  Wrapping those 2 things in a class will result in a more, not less complexity")]
        internal List<CimClass> ParseSchemaMof(string filePath)
        {
            uint offset = 0;
            var buffer = GetFileContent(filePath);
            try
            {
                string fileNameDefiningClass = Path.GetFileNameWithoutExtension(filePath);
                int dotIndex = fileNameDefiningClass.IndexOf('.');
                if (dotIndex != -1)
                {
                    fileNameDefiningClass = fileNameDefiningClass.Substring(0, dotIndex);
                }

                var result = new List<CimClass>(_deserializer.DeserializeClasses(buffer, ref offset, null, null, null, _onClassNeeded, null));
                foreach (CimClass c in result)
                {
                    string superClassName = c.CimSuperClassName;
                    string className = c.CimSystemProperties.ClassName;
                    if ((superClassName != null) && (superClassName.Equals("OMI_BaseResource", StringComparison.OrdinalIgnoreCase)))
                    {
                        // Get the name of the file without schema.mof extension
                        if (!(className.Equals(fileNameDefiningClass, StringComparison.OrdinalIgnoreCase)))
                        {
                            PSInvalidOperationException e = PSTraceSource.NewInvalidOperationException(
                                ParserStrings.ClassNameNotSameAsDefiningFile, className, fileNameDefiningClass);
                            throw e;
                        }
                    }
                }

                return result;
            }
            catch (CimException exception)
            {
                PSInvalidOperationException e = PSTraceSource.NewInvalidOperationException(
                    exception, ParserStrings.CimDeserializationError, filePath);

                e.SetErrorId("CimDeserializationError");
                throw e;
            }
        }

        /// <summary>
        /// Make sure that the instance conforms to the schema.
        /// </summary>
        /// <param name="classText"></param>
        internal void ValidateInstanceText(string classText)
        {
            uint offset = 0;
            byte[] bytes = null;

            if (Platform.IsLinux || Platform.IsMacOS)
            {
                bytes = System.Text.Encoding.UTF8.GetBytes(classText);
            }
            else
            {
                bytes = System.Text.Encoding.Unicode.GetBytes(classText);
            }

            _deserializer.DeserializeInstances(bytes, ref offset, _onClassNeeded, null);
        }
    }
}

namespace Microsoft.PowerShell.DesiredStateConfiguration.Internal
{
    /// <summary>
    /// </summary>
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes",
        Justification = "Needed Internal use only")]
    internal class DscClassCacheEntry
    {
        /// <summary>
        /// Store the RunAs Credentials that this DSC resource will use.
        /// </summary>
        public DSCResourceRunAsCredential DscResRunAsCred;

        /// <summary>
        /// If we have implicitly imported this resource, we will set this field to true. This will
        /// only happen to InBox resources.
        /// </summary>
        public bool IsImportedImplicitly;

        /// <summary>
        /// A CimClass instance for this resource.
        /// </summary>
        public Microsoft.Management.Infrastructure.CimClass CimClassInstance;

        /// <summary>
        /// Initializes variables with default values.
        /// </summary>
        public DscClassCacheEntry() : this(DSCResourceRunAsCredential.Default, false, null) { }

        /// <summary>
        /// Initializes all values.
        /// </summary>
        /// <param name="aDSCResourceRunAsCredential"></param>
        /// <param name="aIsImportedImplicitly"></param>
        /// <param name="aCimClassInstance"></param>
        public DscClassCacheEntry(DSCResourceRunAsCredential aDSCResourceRunAsCredential, bool aIsImportedImplicitly, Microsoft.Management.Infrastructure.CimClass aCimClassInstance)
        {
            DscResRunAsCred = aDSCResourceRunAsCredential;
            IsImportedImplicitly = aIsImportedImplicitly;
            CimClassInstance = aCimClassInstance;
        }
    }

    /// <summary>
    /// </summary>
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes",
        Justification = "Needed Internal use only")]
    public static class DscClassCache
    {
        private const string InboxDscResourceModulePath = "WindowsPowershell\\v1.0\\Modules\\PsDesiredStateConfiguration";
        private const string reservedDynamicKeywords = "^(Synchronization|Certificate|IIS|SQL)$";

        private const string reservedProperties = "^(Require|Trigger|Notify|Before|After|Subscribe)$";

        private static readonly PSTraceSource s_tracer = PSTraceSource.GetTracer("DSC", "DSC Class Cache");

        // Constants for items in the module qualified name (Module\Version\ClassName)
        private const int IndexModuleName = 0;
        private const int IndexModuleVersion = 1;
        private const int IndexClassName = 2;
        private const int IndexFriendlyName = 3;

        // Create a list of classes which are not actual DSC resources similar to what we do inside PSDesiredStateConfiguration.psm1
        private static readonly string[] s_hiddenResourceList =
        {
            "MSFT_BaseConfigurationProviderRegistration",
            "MSFT_CimConfigurationProviderRegistration",
            "MSFT_PSConfigurationProviderRegistration",
        };

        // Create a HashSet for fast lookup. According to MSDN, the time complexity of search for an element in a HashSet is O(1)
        private static readonly HashSet<string> s_hiddenResourceCache =
            new(s_hiddenResourceList, StringComparer.OrdinalIgnoreCase);

        // a collection to hold current importing script based resource file
        // this prevent circular importing case when the script resource existing in the same module with resources it import-dscresource
        private static readonly HashSet<string> s_currentImportingScriptFiles = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// DSC class cache for this runspace.
        /// Cache stores the DSCRunAsBehavior, cim class and boolean to indicate if an Inbox resource has been implicitly imported.
        /// </summary>
        private static Dictionary<string, DscClassCacheEntry> ClassCache
        {
            get
            {
                t_classCache ??= new Dictionary<string, DscClassCacheEntry>(StringComparer.OrdinalIgnoreCase);

                return t_classCache;
            }
        }

        [ThreadStatic]
        private static Dictionary<string, DscClassCacheEntry> t_classCache;

        /// <summary>
        /// DSC classname to source module mapper.
        /// </summary>
        private static Dictionary<string, Tuple<string, Version>> ByClassModuleCache
        {
            get
            {
                t_byClassModuleCache ??= new Dictionary<string, Tuple<string, Version>>(StringComparer.OrdinalIgnoreCase);

                return t_byClassModuleCache;
            }
        }

        [ThreadStatic]
        private static Dictionary<string, Tuple<string, Version>> t_byClassModuleCache;

        /// <summary>
        /// DSC filename to defined class mapper.
        /// </summary>
        private static Dictionary<string, List<Microsoft.Management.Infrastructure.CimClass>> ByFileClassCache
        {
            get
            {
                t_byFileClassCache ??= new Dictionary<string, List<Microsoft.Management.Infrastructure.CimClass>>(StringComparer.OrdinalIgnoreCase);

                return t_byFileClassCache;
            }
        }

        [ThreadStatic]
        private static Dictionary<string, List<Microsoft.Management.Infrastructure.CimClass>> t_byFileClassCache;

        /// <summary>
        /// Filenames from which we have imported script dynamic keywords.
        /// </summary>
        private static HashSet<string> ScriptKeywordFileCache
        {
            get
            {
                t_scriptKeywordFileCache ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                return t_scriptKeywordFileCache;
            }
        }

        [ThreadStatic]
        private static HashSet<string> t_scriptKeywordFileCache;

        /// <summary>
        /// Default ModuleName and ModuleVersion to use.
        /// </summary>
        private static readonly Tuple<string, Version> s_defaultModuleInfoForResource =
            new("PSDesiredStateConfiguration", new Version("1.1"));

        /// <summary>
        /// Default ModuleName and ModuleVersion to use for meta configuration resources.
        /// </summary>
        internal static readonly Tuple<string, Version> DefaultModuleInfoForMetaConfigResource =
            new("PSDesiredStateConfigurationEngine", new Version("2.0"));

        /// <summary>
        /// A set of dynamic keywords that can be used in both configuration and meta configuration.
        /// </summary>
        internal static readonly HashSet<string> SystemResourceNames =
            new(StringComparer.OrdinalIgnoreCase) { "Node", "OMI_ConfigurationDocument" };

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

        /// <summary>
        /// Initialize the class cache with the default classes in $ENV:SystemDirectory\Configuration.
        /// </summary>
        public static void Initialize()
        {
            Initialize(null, null);
        }

        /// <summary>
        /// Initialize the class cache with the default classes in $ENV:SystemDirectory\Configuration.
        /// </summary>
        /// <param name="errors">Collection of any errors encountered during initialization.</param>
        /// <param name="modulePathList">List of module path from where DSC PS modules will be loaded.</param>
        public static void Initialize(Collection<Exception> errors, List<string> modulePathList)
        {
            s_tracer.WriteLine("Initializing DSC class cache force={0}");

            if (Platform.IsLinux || Platform.IsMacOS)
            {
                //
                // Load the base schema files.
                //
                ClearCache();
                var dscConfigurationDirectory = Environment.GetEnvironmentVariable("DSC_HOME") ??
                                                "/etc/opt/omi/conf/dsc/configuration";

                if (!Directory.Exists(dscConfigurationDirectory))
                {
                    throw new DirectoryNotFoundException("Unable to find DSC schema store at " + dscConfigurationDirectory + ". Please ensure PS DSC for Linux is installed.");
                }

                var resourceBaseFile = Path.Combine(dscConfigurationDirectory, "BaseRegistration/BaseResource.schema.mof");
                ImportClasses(resourceBaseFile, s_defaultModuleInfoForResource, errors);

                var metaConfigFile = Path.Combine(dscConfigurationDirectory, "BaseRegistration/MSFT_DSCMetaConfiguration.mof");
                ImportClasses(metaConfigFile, s_defaultModuleInfoForResource, errors);

                var allResourceRoots = new string[] { dscConfigurationDirectory };

                //
                // Load all of the system resource schema files, searching
                //
                string resources;
                foreach (var resourceRoot in allResourceRoots)
                {
                    resources = Path.Combine(resourceRoot, "schema");
                    if (!Directory.Exists(resources))
                    {
                        continue;
                    }

                    foreach (var schemaFile in Directory.EnumerateDirectories(resources).SelectMany(static d => Directory.EnumerateFiles(d, "*.schema.mof")))
                    {
                        ImportClasses(schemaFile, s_defaultModuleInfoForResource, errors);
                    }
                }

                // Linux DSC Modules are installed to the dscConfigurationDirectory, so no need to load them.
            }
            else
            {
                // DSC SxS scenario
                var configSystemPath = Utils.DefaultPowerShellAppBase;
                var systemResourceRoot = Path.Combine(configSystemPath, "Configuration");
                var inboxModulePath = "Modules\\PSDesiredStateConfiguration";

                if (!Directory.Exists(systemResourceRoot))
                {
                    configSystemPath = Environment.GetFolderPath(Environment.SpecialFolder.System);
                    systemResourceRoot = Path.Combine(configSystemPath, "Configuration");
                    inboxModulePath = InboxDscResourceModulePath;
                }

                var programFilesDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                Debug.Assert(programFilesDirectory != null, "Program Files environment variable does not exist!");
                var customResourceRoot = Path.Combine(programFilesDirectory, "WindowsPowerShell\\Configuration");
                Debug.Assert(Directory.Exists(customResourceRoot), "%ProgramFiles%\\WindowsPowerShell\\Configuration Directory does not exist");
                var allResourceRoots = new string[] { systemResourceRoot, customResourceRoot };
                //
                // Load the base schema files.
                //
                ClearCache();
                var resourceBaseFile = Path.Combine(systemResourceRoot, "BaseRegistration\\BaseResource.schema.mof");
                ImportClasses(resourceBaseFile, s_defaultModuleInfoForResource, errors);

                var metaConfigFile = Path.Combine(systemResourceRoot, "BaseRegistration\\MSFT_DSCMetaConfiguration.mof");
                ImportClasses(metaConfigFile, s_defaultModuleInfoForResource, errors);

                var metaConfigExtensionFile = Path.Combine(systemResourceRoot, "BaseRegistration\\MSFT_MetaConfigurationExtensionClasses.schema.mof");
                ImportClasses(metaConfigExtensionFile, DefaultModuleInfoForMetaConfigResource, errors);

                //
                // Load all of the system resource schema files, searching
                //
                string resources;
                foreach (var resourceRoot in allResourceRoots)
                {
                    resources = Path.Combine(resourceRoot, "Schema");
                    if (!Directory.Exists(resources))
                    {
                        continue;
                    }

                    foreach (var schemaFile in Directory.EnumerateDirectories(resources).SelectMany(static d => Directory.EnumerateFiles(d, "*.schema.mof")))
                    {
                        ImportClasses(schemaFile, s_defaultModuleInfoForResource, errors);
                    }
                }

                // Load Regular and DSC PS modules
                bool importInBoxResourcesImplicitly = false;
                List<string> modulePaths = new();
                if (modulePathList == null || modulePathList.Count == 0)
                {
                    modulePaths.Add(Path.Combine(configSystemPath, inboxModulePath));
                    importInBoxResourcesImplicitly = true;
                }
                else
                {
                    foreach (string moduleFolderPath in modulePathList)
                    {
                        if (!Directory.Exists(moduleFolderPath))
                        {
                            continue;
                        }

                        foreach (string moduleDir in Directory.EnumerateDirectories(moduleFolderPath))
                        {
                            modulePaths.Add(moduleDir);
                        }
                    }
                }

                LoadDSCResourceIntoCache(errors, modulePaths, importInBoxResourcesImplicitly);
            }
        }

        /// <summary>
        /// Load DSC resources into Cache from moduleFolderPath.
        /// </summary>
        /// <param name="errors">Collection of any errors encountered during initialization.</param>
        /// <param name="modulePathList">Module path from where DSC PS modules will be loaded.</param>
        /// <param name="importInBoxResourcesImplicitly">
        /// if module is inbox.
        /// </param>
        private static void LoadDSCResourceIntoCache(Collection<Exception> errors, List<string> modulePathList, bool importInBoxResourcesImplicitly)
        {
            foreach (string moduleDir in modulePathList)
            {
                if (!Directory.Exists(moduleDir)) continue;

                var dscResourcesPath = Path.Combine(moduleDir, "DscResources");
                if (Directory.Exists(dscResourcesPath))
                {
                    foreach (string resourceDir in Directory.EnumerateDirectories(dscResourcesPath))
                    {
                        IEnumerable<string> schemaFiles = Directory.EnumerateFiles(resourceDir, "*.schema.mof");
                        if (!schemaFiles.Any())
                        {
                            continue;
                        }

                        Tuple<string, Version> moduleInfo = GetModuleInfoHelper(moduleDir, importInBoxResourcesImplicitly, isPsProviderModule: false);
                        if (moduleInfo == null)
                        {
                            continue;
                        }

                        foreach (string schemaFile in schemaFiles)
                        {
                            ImportClasses(schemaFile, moduleInfo, errors, importInBoxResourcesImplicitly);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get the module name and module version.
        /// </summary>
        /// <param name="moduleFolderPath">
        /// Path to the module folder
        /// </param>
        /// <param name="importInBoxResourcesImplicitly">
        /// if module is inbox and we are importing resources implicitly
        /// </param>
        /// <param name="isPsProviderModule">
        /// Indicate a internal DSC module
        /// </param>
        /// <returns></returns>
        private static Tuple<string, Version> GetModuleInfoHelper(string moduleFolderPath, bool importInBoxResourcesImplicitly, bool isPsProviderModule)
        {
            string moduleName = "PsDesiredStateConfiguration";
            if (!importInBoxResourcesImplicitly)
            {
                moduleName = Path.GetFileName(moduleFolderPath);
            }

            string manifestPath = Path.Combine(moduleFolderPath, moduleName + ".psd1");
            s_tracer.WriteLine("DSC GetModuleVersion: Try retrieving module version information from file: {0}.", manifestPath);

            if (!File.Exists(manifestPath))
            {
                if (isPsProviderModule)
                {
                    // Some internal PSProviders do not come with a .psd1 file, such
                    // as MSFT_LogResource. We don't report error in this case.
                    return new Tuple<string, Version>(moduleName, new Version("1.0"));
                }
                else
                {
                    s_tracer.WriteLine("DSC GetModuleVersion: Manifest file '{0}' not exist.", manifestPath);
                    return null;
                }
            }

            try
            {
                Hashtable dataFileSetting =
                    PsUtils.GetModuleManifestProperties(
                        manifestPath,
                        PsUtils.ManifestModuleVersionPropertyName);

                object versionValue = dataFileSetting["ModuleVersion"];
                if (versionValue != null)
                {
                    Version moduleVersion;
                    if (LanguagePrimitives.TryConvertTo(versionValue, out moduleVersion))
                    {
                        return new Tuple<string, Version>(moduleName, moduleVersion);
                    }
                    else
                    {
                        s_tracer.WriteLine(
                            "DSC GetModuleVersion: ModuleVersion value '{0}' cannot be converted to System.Version. Skip the module '{1}'.",
                            versionValue, moduleName);
                    }
                }
                else
                {
                    s_tracer.WriteLine(
                        "DSC GetModuleVersion: Manifest file '{0}' does not contain ModuleVersion. Skip the module '{1}'.",
                        manifestPath, moduleName);
                }
            }
            catch (PSInvalidOperationException ex)
            {
                s_tracer.WriteLine(
                    "DSC GetModuleVersion: Error evaluating module manifest file '{0}', with error '{1}'. Skip the module '{2}'.",
                    manifestPath, ex, moduleName);
            }

            return null;
        }

        // Callback implementation...
        private static CimClass MyClassCallback(string serverName, string namespaceName, string className)
        {
            foreach (KeyValuePair<string, DscClassCacheEntry> cimClass in ClassCache)
            {
                string cachedClassName = cimClass.Key.Split('\\')[IndexClassName];
                if (string.Equals(cachedClassName, className, StringComparison.OrdinalIgnoreCase))
                {
                    return cimClass.Value.CimClassInstance;
                }
            }

            return null;
        }

        /// <summary>
        /// Reads CIM MOF schema file and returns classes defined in it.
        /// This is used MOF->PSClass conversion tool.
        /// </summary>
        /// <param name="mofPath">
        /// Path to CIM MOF schema file for reading.
        /// </param>
        /// <returns>List of classes from MOF schema file.</returns>
        public static List<CimClass> ReadCimSchemaMof(string mofPath)
        {
            var parser = new Microsoft.PowerShell.DesiredStateConfiguration.CimDSCParser(MyClassCallback);
            return parser.ParseSchemaMof(mofPath);
        }

        /// <summary>
        /// Import CIM classes from the given file.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="moduleInfo"></param>
        /// <param name="errors"></param>
        /// <param name="importInBoxResourcesImplicitly"></param>
        /// <returns></returns>
        public static List<CimClass> ImportClasses(string path, Tuple<string, Version> moduleInfo, Collection<Exception> errors, bool importInBoxResourcesImplicitly = false)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(path));
            }

            s_tracer.WriteLine("DSC ClassCache: importing file: {0}", path);

            var parser = new Microsoft.PowerShell.DesiredStateConfiguration.CimDSCParser(MyClassCallback);

            List<CimClass> classes = null;
            try
            {
                classes = parser.ParseSchemaMof(path);
            }
            catch (PSInvalidOperationException e)
            {
                // Ignore modules with invalid schemas.
                s_tracer.WriteLine("DSC ClassCache: Error importing file '{0}', with error '{1}'.  Skipping file.", path, e);
                errors?.Add(e);
            }

            if (classes != null)
            {
                foreach (var c in classes)
                {
                    // Only add the class once...
                    var className = c.CimSystemProperties.ClassName;
                    string alias = GetFriendlyName(c);
                    var friendlyName = string.IsNullOrEmpty(alias) ? className : alias;
                    string moduleQualifiedResourceName = GetModuleQualifiedResourceName(moduleInfo.Item1, moduleInfo.Item2.ToString(), className, friendlyName);
                    DscClassCacheEntry cimClassInfo;

                    if (ClassCache.TryGetValue(moduleQualifiedResourceName, out cimClassInfo))
                    {
                        CimClass cimClass = cimClassInfo.CimClassInstance;

                        // If this is a nested object and we already have exactly same nested object, we will
                        // allow sharing of nested objects.
                        if (!IsSameNestedObject(cimClass, c))
                        {
                            var files = string.Join(',', GetFileDefiningClass(className));
                            PSInvalidOperationException e = PSTraceSource.NewInvalidOperationException(
                                ParserStrings.DuplicateCimClassDefinition, className, path, files);

                            e.SetErrorId("DuplicateCimClassDefinition");
                            errors?.Add(e);
                        }
                    }

                    if (s_hiddenResourceCache.Contains(className))
                    {
                        continue;
                    }

                    if (!CacheResourcesFromMultipleModuleVersions)
                    {
                        // Find & remove the previous version of the resource.
                        List<KeyValuePair<string, DscClassCacheEntry>> resourceList = FindResourceInCache(moduleInfo.Item1, className, friendlyName);
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

                    ClassCache[moduleQualifiedResourceName] = new DscClassCacheEntry(DSCResourceRunAsCredential.Default, importInBoxResourcesImplicitly, c);
                    ByClassModuleCache[className] = moduleInfo;
                }

                var sb = new System.Text.StringBuilder();
                foreach (var c in classes)
                {
                    sb.Append(c.CimSystemProperties.ClassName);
                    sb.Append(',');
                }

                s_tracer.WriteLine("DSC ClassCache: loading file '{0}' added the following classes to the cache: {1}", path, sb.ToString());
            }
            else
            {
                s_tracer.WriteLine("DSC ClassCache: loading file '{0}' added no classes to the cache.");
            }

            ByFileClassCache[path] = classes;
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
            s_tracer.WriteLine("DSC class: clearing the cache and associated keywords.");
            ClassCache.Clear();
            ByClassModuleCache.Clear();
            ByFileClassCache.Clear();
            ScriptKeywordFileCache.Clear();
            CacheResourcesFromMultipleModuleVersions = false;
        }

        /// <summary>
        /// Returns module qualified resource name in "Module\Version\Class" format.
        /// </summary>
        /// <param name="moduleName"></param>
        /// <param name="moduleVersion"></param>
        /// <param name="className"></param>
        /// <param name="resourceName"></param>
        /// <returns></returns>
        private static string GetModuleQualifiedResourceName(string moduleName, string moduleVersion, string className, string resourceName)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{moduleName}\\{moduleVersion}\\{className}\\{resourceName}");
        }

        /// <summary>
        /// Finds resources in the that which matches the specified class and module name.
        /// </summary>
        /// <param name="moduleName">Module name.</param>
        /// <param name="className">Resource type name.</param>
        /// <param name="resourceName">Resource friendly name.</param>
        /// <returns>List of found resources in the form of Dictionary{moduleQualifiedName, cimClass}, otherwise empty list.</returns>
        private static List<KeyValuePair<string, DscClassCacheEntry>> FindResourceInCache(string moduleName, string className, string resourceName)
        {
            return (from cacheEntry in ClassCache
                    let splittedName = cacheEntry.Key.Split('\\')
                    let cachedClassName = splittedName[IndexClassName]
                    let cachedModuleName = splittedName[IndexModuleName]
                    let cachedResourceName = splittedName[IndexFriendlyName]
                    where (string.Equals(cachedResourceName, resourceName, StringComparison.OrdinalIgnoreCase)
                    || (string.Equals(cachedClassName, className, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(cachedModuleName, moduleName, StringComparison.OrdinalIgnoreCase)))
                    select cacheEntry).ToList();
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        private static List<DscClassCacheEntry> GetCachedClasses()
        {
            return ClassCache.Values.ToList();
        }

        /// <summary>
        /// Find cached cim classes defined under specified module.
        /// </summary>
        /// <param name="module"></param>
        /// <returns>List of cached cim classes.</returns>
        public static List<Microsoft.Management.Infrastructure.CimClass> GetCachedClassesForModule(PSModuleInfo module)
        {
            List<Microsoft.Management.Infrastructure.CimClass> cachedClasses = new();
            var moduleQualifiedName = string.Create(CultureInfo.InvariantCulture, $"{module.Name}\\{module.Version}");
            foreach (var dscClassCacheEntry in ClassCache)
            {
                if (dscClassCacheEntry.Key.StartsWith(moduleQualifiedName, StringComparison.OrdinalIgnoreCase))
                {
                    cachedClasses.Add(dscClassCacheEntry.Value.CimClassInstance);
                }
            }

            return cachedClasses;
        }

        /// <summary>
        /// Get the file that defined this class.
        /// </summary>
        /// <param name="className"></param>
        /// <returns></returns>
        public static List<string> GetFileDefiningClass(string className)
        {
            List<string> files = new();
            foreach (var pair in ByFileClassCache)
            {
                var file = pair.Key;
                var classList = pair.Value;
                if (classList != null && classList.Find((CimClass c) => string.Equals(c.CimSystemProperties.ClassName, className, StringComparison.OrdinalIgnoreCase)) != null)
                {
                    files.Add(file);
                }
            }

            return files;
        }

        /// <summary>
        /// Get a list of files from which classes have been loaded.
        /// </summary>
        /// <returns></returns>
        public static string[] GetLoadedFiles()
        {
            return ByFileClassCache.Keys.ToArray();
        }

        /// <summary>
        /// Returns the classes that we loaded from the specified file name.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static List<CimClass> GetCachedClassByFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(fileName));
            }

            List<CimClass> listCimClass;
            ByFileClassCache.TryGetValue(fileName, out listCimClass);
            return listCimClass;
        }

        /// <summary>
        /// Returns the classes associated with the specified module name.
        /// Per PowerShell the module name is the base name of the schema file.
        /// </summary>
        /// <param name="moduleName"></param>
        /// <returns></returns>
        public static List<CimClass> GetCachedClassByModuleName(string moduleName)
        {
            if (string.IsNullOrWhiteSpace(moduleName))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(moduleName));
            }

            var moduleFileName = moduleName + ".schema.mof";
            return (from filename in ByFileClassCache.Keys where string.Equals(Path.GetFileName(filename), moduleFileName, StringComparison.OrdinalIgnoreCase) select GetCachedClassByFileName(filename)).FirstOrDefault();
        }

        /// <summary>
        /// Routine used to load a set of CIM instances from a .mof file using the
        /// current set of cached classes for schema validation.
        /// </summary>
        /// <param name="path">The file to load the classes from.</param>
        /// <returns></returns>
        public static List<CimInstance> ImportInstances(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(path));
            }

            var parser = new Microsoft.PowerShell.DesiredStateConfiguration.CimDSCParser(MyClassCallback);

            return parser.ParseInstanceMof(path);
        }

        /// <summary>
        /// Routine used to load a set of CIM instances from a .mof file using the
        /// current set of cached classes for schema validation.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="schemaValidationOption"></param>
        /// <returns></returns>
        public static List<CimInstance> ImportInstances(string path, int schemaValidationOption)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(path));
            }

            if (schemaValidationOption < (int)Microsoft.Management.Infrastructure.Serialization.MofDeserializerSchemaValidationOption.Default ||
                schemaValidationOption > (int)Microsoft.Management.Infrastructure.Serialization.MofDeserializerSchemaValidationOption.Ignore)
            {
                throw new IndexOutOfRangeException("schemaValidationOption");
            }

            var parser = new Microsoft.PowerShell.DesiredStateConfiguration.CimDSCParser(MyClassCallback, (Microsoft.Management.Infrastructure.Serialization.MofDeserializerSchemaValidationOption)schemaValidationOption);

            return parser.ParseInstanceMof(path);
        }

        /// <summary>
        /// A routine that validates a string containing MOF instances against the
        /// current set of cached classes.
        /// </summary>
        /// <param name="instanceText"></param>
        public static void ValidateInstanceText(string instanceText)
        {
            if (string.IsNullOrEmpty(instanceText))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(instanceText));
            }

            var parser = new Microsoft.PowerShell.DesiredStateConfiguration.CimDSCParser(MyClassCallback);

            parser.ValidateInstanceText(instanceText);
        }

        private static bool IsMagicProperty(string propertyName)
        {
            return System.Text.RegularExpressions.Regex.Match(propertyName,
                "^(ResourceId|SourceInfo|ModuleName|ModuleVersion|ConfigurationName)$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase).Success;
        }

        private static string GetFriendlyName(CimClass cimClass)
        {
            try
            {
                var aliasQualifier = cimClass.CimClassQualifiers["FriendlyName"];
                if (aliasQualifier != null)
                {
                    return aliasQualifier.Value as string;
                }
            }
            catch (Microsoft.Management.Infrastructure.CimException)
            {
                // exception means no DSCAlias
            }

            return null;
        }

        /// <summary>
        /// Method to get the cached classes in the form of DynamicKeyword.
        /// </summary>
        public static Collection<DynamicKeyword> GetCachedKeywords()
        {
            Collection<DynamicKeyword> keywords = new();

            foreach (KeyValuePair<string, DscClassCacheEntry> cachedClass in ClassCache)
            {
                string[] splittedName = cachedClass.Key.Split('\\');
                string moduleName = splittedName[IndexModuleName];
                string moduleVersion = splittedName[IndexModuleVersion];

                var keyword = CreateKeywordFromCimClass(moduleName, Version.Parse(moduleVersion), cachedClass.Value.CimClassInstance, null, cachedClass.Value.DscResRunAsCred);
                if (keyword != null)
                {
                    keywords.Add(keyword);
                }
            }

            return keywords;
        }

        /// <summary>
        /// A method to generate a keyword from a CIM class object and register it to DynamicKeyword table.
        /// </summary>
        /// <param name="moduleName"></param>
        /// <param name="moduleVersion"></param>
        /// <param name="cimClass"></param>
        /// <param name="functionsToDefine">If true, don't define the keywords, just create the functions.</param>
        /// <param name="runAsBehavior">To Specify RunAsBehavior of the class.</param>
        private static void CreateAndRegisterKeywordFromCimClass(string moduleName, Version moduleVersion, Microsoft.Management.Infrastructure.CimClass cimClass, Dictionary<string, ScriptBlock> functionsToDefine, DSCResourceRunAsCredential runAsBehavior)
        {
            var keyword = CreateKeywordFromCimClass(moduleName, moduleVersion, cimClass, functionsToDefine, runAsBehavior);
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

        /// <summary>
        /// A method to generate a keyword from a CIM class object. This is used for DSC.
        /// </summary>
        /// <param name="moduleName"></param>
        /// <param name="moduleVersion"></param>
        /// <param name="cimClass"></param>
        /// <param name="functionsToDefine">If true, don't define the keywords, just create the functions.</param>
        /// <param name="runAsBehavior">To specify RunAs behavior of the class.</param>
        private static DynamicKeyword CreateKeywordFromCimClass(string moduleName, Version moduleVersion, Microsoft.Management.Infrastructure.CimClass cimClass, Dictionary<string, ScriptBlock> functionsToDefine, DSCResourceRunAsCredential runAsBehavior)
        {
            var resourceName = cimClass.CimSystemProperties.ClassName;
            string alias = GetFriendlyName(cimClass);
            var keywordString = string.IsNullOrEmpty(alias) ? resourceName : alias;

            //
            // Skip all of the base, meta, registration and other classes that are not intended to be used directly by a script author
            //
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
            if (System.Text.RegularExpressions.Regex.Match(keywordString, reservedDynamicKeywords, System.Text.RegularExpressions.RegexOptions.IgnoreCase).Success)
            {
                keyword.IsReservedKeyword = true;
            }

            // see if it's a resource type i.e. it inherits from OMI_BaseResource
            bool isResourceType = false;
            for (var classToCheck = cimClass; !string.IsNullOrEmpty(classToCheck.CimSuperClassName); classToCheck = classToCheck.CimSuperClass)
            {
                if (string.Equals("OMI_BaseResource", classToCheck.CimSuperClassName, StringComparison.OrdinalIgnoreCase) || string.Equals("OMI_MetaConfigurationResource", classToCheck.CimSuperClassName, StringComparison.OrdinalIgnoreCase))
                {
                    isResourceType = true;
                    break;
                }
            }

            // If it's a resource type, then a resource name is required.
            keyword.NameMode = isResourceType ? DynamicKeywordNameMode.NameRequired : DynamicKeywordNameMode.NoName;

            //
            // Add the settable properties to the keyword object
            //
            foreach (var prop in cimClass.CimClassProperties)
            {
                // If the property is marked as readonly, skip it...
                if ((prop.Flags & Microsoft.Management.Infrastructure.CimFlags.ReadOnly) == Microsoft.Management.Infrastructure.CimFlags.ReadOnly)
                {
                    continue;
                }

                try
                {
                    // If the property has the Read qualifier, also skip it.
                    if (prop.Qualifiers["Read"] != null)
                    {
                        continue;
                    }
                }
                catch (Microsoft.Management.Infrastructure.CimException)
                {
                    // Cim exception means Read wasn't found so continue...
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
                if (System.Text.RegularExpressions.Regex.Match(prop.Name, reservedProperties, System.Text.RegularExpressions.RegexOptions.IgnoreCase).Success)
                {
                    keyword.HasReservedProperties = true;
                    continue;
                }

                // Otherwise, add it to the Keyword List.
                var keyProp = new System.Management.Automation.Language.DynamicKeywordProperty();
                keyProp.Name = prop.Name;

                // Set the mandatory flag if appropriate
                if ((prop.Flags & Microsoft.Management.Infrastructure.CimFlags.Key) == Microsoft.Management.Infrastructure.CimFlags.Key)
                {
                    keyProp.Mandatory = true;
                    keyProp.IsKey = true;
                }

                // Copy the type name string. If it's an embedded instance, need to grab it from the ReferenceClassName
                bool referenceClassNameIsNullOrEmpty = string.IsNullOrEmpty(prop.ReferenceClassName);
                if (prop.CimType == CimType.Instance && !referenceClassNameIsNullOrEmpty)
                {
                    keyProp.TypeConstraint = prop.ReferenceClassName;
                }
                else if (prop.CimType == CimType.InstanceArray && !referenceClassNameIsNullOrEmpty)
                {
                    keyProp.TypeConstraint = prop.ReferenceClassName + "[]";
                }
                else
                {
                    keyProp.TypeConstraint = prop.CimType.ToString();
                }

                string[] valueMap = null;
                foreach (var qualifier in prop.Qualifiers)
                {
                    // Check to see if there is a Values attribute and save the list of allowed values if so.
                    if (string.Equals(qualifier.Name, "Values", StringComparison.OrdinalIgnoreCase) && qualifier.CimType == Microsoft.Management.Infrastructure.CimType.StringArray)
                    {
                        keyProp.Values.AddRange((string[])qualifier.Value);
                    }

                    // Check to see if there is a ValueMap attribute and save the list of allowed values if so.
                    if (string.Equals(qualifier.Name, "ValueMap", StringComparison.OrdinalIgnoreCase) && qualifier.CimType == Microsoft.Management.Infrastructure.CimType.StringArray)
                    {
                        valueMap = (string[])qualifier.Value;
                    }

                    // Check to see if this property has the Required qualifier associated with it.
                    if (string.Equals(qualifier.Name, "Required", StringComparison.OrdinalIgnoreCase) &&
                        qualifier.CimType == Microsoft.Management.Infrastructure.CimType.Boolean &&
                            (bool)qualifier.Value)
                    {
                        keyProp.Mandatory = true;
                    }

                    // set the property to mandatory is specified for the resource.
                    if (runAsBehavior == DSCResourceRunAsCredential.Mandatory)
                    {
                        if (string.Equals(prop.Name, "PsDscRunAsCredential", StringComparison.OrdinalIgnoreCase))
                        {
                            keyProp.Mandatory = true;
                        }
                    }
                }

                if (valueMap != null && keyProp.Values.Count > 0)
                {
                    if (valueMap.Length != keyProp.Values.Count)
                    {
                        s_tracer.WriteLine(
                            "DSC CreateDynamicKeywordFromClass: the count of values for qualifier 'Values' and 'ValueMap' doesn't match. count of 'Values': {0}, count of 'ValueMap': {1}. Skip the keyword '{2}'.",
                            keyProp.Values.Count, valueMap.Length, keyword.Keyword);
                        return null;
                    }

                    for (int index = 0; index < valueMap.Length; index++)
                    {
                        string key = keyProp.Values[index];
                        string value = valueMap[index];

                        if (keyProp.ValueMap.ContainsKey(key))
                        {
                            s_tracer.WriteLine(
                                "DSC CreateDynamicKeywordFromClass: same string value '{0}' appears more than once in qualifier 'Values'. Skip the keyword '{1}'.",
                                key, keyword.Keyword);
                            return null;
                        }

                        keyProp.ValueMap.Add(key, value);
                    }
                }

                keyword.Properties.Add(prop.Name, keyProp);
            }

            // update specific keyword with range constraints
            UpdateKnownRestriction(keyword);

            return keyword;
        }

        /// <summary>
        /// Update range restriction for meta configuration keywords
        /// the restrictions are for
        /// ConfigurationModeFrequency: 15-44640
        /// RefreshFrequency: 30-44640.
        /// </summary>
        /// <param name="keyword"></param>
        private static void UpdateKnownRestriction(DynamicKeyword keyword)
        {
            if (
                string.Equals(keyword.ResourceName, "MSFT_DSCMetaConfigurationV2",
                    StringComparison.OrdinalIgnoreCase)
                ||
                string.Equals(keyword.ResourceName, "MSFT_DSCMetaConfiguration",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (keyword.Properties["RefreshFrequencyMins"] != null)
                {
                    keyword.Properties["RefreshFrequencyMins"].Range = new Tuple<int, int>(30, 44640);
                }

                if (keyword.Properties["ConfigurationModeFrequencyMins"] != null)
                {
                    keyword.Properties["ConfigurationModeFrequencyMins"].Range = new Tuple<int, int>(15, 44640);
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
        public static void LoadDefaultCimKeywords()
        {
            LoadDefaultCimKeywords(null, null, null, false);
        }

        /// <summary>
        /// Load the default system CIM classes and create the corresponding keywords.
        /// </summary>
        /// <param name="modulePathList">List of module path from where DSC PS modules will be loaded.</param>
        public static void LoadDefaultCimKeywords(List<string> modulePathList)
        {
            LoadDefaultCimKeywords(null, null, modulePathList, false);
        }

        /// <summary>
        /// Load the default system CIM classes and create the corresponding keywords.
        /// </summary>
        /// <param name="errors">Collection of any errors encountered while loading keywords.</param>
        public static void LoadDefaultCimKeywords(Collection<Exception> errors)
        {
            LoadDefaultCimKeywords(null, errors, null, false);
        }

        /// <summary>
        /// Load the default system CIM classes and create the corresponding keywords.
        /// </summary>
        /// <param name="functionsToDefine">A dictionary to add the defined functions to, may be null.</param>
        public static void LoadDefaultCimKeywords(Dictionary<string, ScriptBlock> functionsToDefine)
        {
            LoadDefaultCimKeywords(functionsToDefine, null, null, false);
        }

        /// <summary>
        /// Load the default system CIM classes and create the corresponding keywords.
        /// </summary>
        /// <param name="errors">Collection of any errors encountered while loading keywords.</param>
        /// <param name="cacheResourcesFromMultipleModuleVersions">Allow caching the resources from multiple versions of modules.</param>
        public static void LoadDefaultCimKeywords(Collection<Exception> errors, bool cacheResourcesFromMultipleModuleVersions)
        {
            LoadDefaultCimKeywords(null, errors, null, cacheResourcesFromMultipleModuleVersions);
        }

        /// <summary>
        /// Load the default system CIM classes and create the corresponding keywords.
        /// </summary>
        /// <param name="functionsToDefine">A dictionary to add the defined functions to, may be null.</param>
        /// <param name="errors">Collection of any errors encountered while loading keywords.</param>
        /// <param name="modulePathList">List of module path from where DSC PS modules will be loaded.</param>
        /// <param name="cacheResourcesFromMultipleModuleVersions">Allow caching the resources from multiple versions of modules.</param>
        private static void LoadDefaultCimKeywords(Dictionary<string, ScriptBlock> functionsToDefine, Collection<Exception> errors,
                                                   List<string> modulePathList, bool cacheResourcesFromMultipleModuleVersions)
        {
            DynamicKeyword.Reset();
            Initialize(errors, modulePathList);

            // Initialize->ClearCache resets CacheResourcesFromMultipleModuleVersions to false,
            // workaround is to set it after Initialize method call.
            // Initialize method imports all the Inbox resources and internal classes which belongs to only one version
            // of the module, so it is ok if this property is not set during cache initialization.
            CacheResourcesFromMultipleModuleVersions = cacheResourcesFromMultipleModuleVersions;

            foreach (var cimClass in GetCachedClasses())
            {
                var className = cimClass.CimClassInstance.CimSystemProperties.ClassName;
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

        // This function is called after parsing the Import-DscResource keyword and it's arguments, but before parsing
        // anything else.
        //
        //
        private static ParseError[] ImportResourcePostParse(DynamicKeywordStatementAst kwAst)
        {
            var elements = Ast.CopyElements(kwAst.CommandElements);

            Diagnostics.Assert(elements[0] is StringConstantExpressionAst &&
                               ((StringConstantExpressionAst)elements[0]).Value.Equals("Import-DscResource", StringComparison.OrdinalIgnoreCase),
                               "Incorrect ast for expected keyword");
            var commandAst = new CommandAst(kwAst.Extent, elements, TokenKind.Unknown, null);

            const string nameParam = "Name";
            const string moduleNameParam = "ModuleName";
            const string moduleVersionParam = "ModuleVersion";

            StaticBindingResult bindingResult = StaticParameterBinder.BindCommand(commandAst, false);

            var errorList = new List<ParseError>();
            foreach (var bindingException in bindingResult.BindingExceptions.Values)
            {
                errorList.Add(new ParseError(bindingException.CommandElement.Extent,
                                             "ParameterBindingException",
                                             bindingException.BindingException.Message));
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
                    errorList.Add(new ParseError(parameterBindingResult.Value.Extent,
                                                 "ImportDscResourcePositionalParamsNotSupported",
                                                 string.Format(CultureInfo.CurrentCulture, ParserStrings.ImportDscResourcePositionalParamsNotSupported)));
                    continue;
                }

                if (nameParam.StartsWith(boundParameterName, StringComparison.OrdinalIgnoreCase))
                {
                    resourceNameBindingResult = parameterBindingResult;
                }
                else if (moduleNameParam.StartsWith(boundParameterName, StringComparison.OrdinalIgnoreCase))
                {
                    moduleNameBindingResult = parameterBindingResult;
                }
                else if (moduleVersionParam.StartsWith(boundParameterName, StringComparison.OrdinalIgnoreCase))
                {
                    moduleVersionBindingResult = parameterBindingResult;
                }
                else
                {
                    errorList.Add(new ParseError(parameterBindingResult.Value.Extent,
                                                 "ImportDscResourceNeedParams",
                                                 string.Format(CultureInfo.CurrentCulture, ParserStrings.ImportDscResourceNeedParams)));
                }
            }

            if (errorList.Count == 0 && moduleNameBindingResult == null && resourceNameBindingResult == null)
            {
                errorList.Add(new ParseError(kwAst.Extent,
                                             "ImportDscResourceNeedParams",
                                             string.Format(CultureInfo.CurrentCulture, ParserStrings.ImportDscResourceNeedParams)));
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
                    errorList.Add(new ParseError(kwAst.Extent,
                                                 "ImportDscResourceNeedModuleNameWithModuleVersion",
                                                 string.Format(CultureInfo.CurrentCulture, ParserStrings.ImportDscResourceNeedParams)));
                }
            }

            string[] resourceNames = null;
            if (resourceNameBindingResult != null)
            {
                object resourceName = null;
                if (!IsConstantValueVisitor.IsConstant(resourceNameBindingResult.Value, out resourceName, true, true) ||
                    !LanguagePrimitives.TryConvertTo(resourceName, out resourceNames))
                {
                    errorList.Add(new ParseError(resourceNameBindingResult.Value.Extent,
                                                 "RequiresInvalidStringArgument",
                                                 string.Format(CultureInfo.CurrentCulture, ParserStrings.RequiresInvalidStringArgument, nameParam)));
                }
            }

            System.Version moduleVersion = null;
            if (moduleVersionBindingResult != null)
            {
                object moduleVer = null;
                if (!IsConstantValueVisitor.IsConstant(moduleVersionBindingResult.Value, out moduleVer, true, true))
                {
                    errorList.Add(new ParseError(moduleVersionBindingResult.Value.Extent,
                                                 "RequiresArgumentMustBeConstant",
                                                 ParserStrings.RequiresArgumentMustBeConstant));
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
                    errorList.Add(new ParseError(moduleVersionBindingResult.Value.Extent,
                                                 "RequiresVersionInvalid",
                                                 ParserStrings.RequiresVersionInvalid));
                }
            }

            ModuleSpecification[] moduleSpecifications = null;
            if (moduleNameBindingResult != null)
            {
                object moduleName = null;
                if (!IsConstantValueVisitor.IsConstant(moduleNameBindingResult.Value, out moduleName, true, true))
                {
                    errorList.Add(new ParseError(moduleNameBindingResult.Value.Extent,
                                                 "RequiresArgumentMustBeConstant",
                                                 ParserStrings.RequiresArgumentMustBeConstant));
                }

                if (LanguagePrimitives.TryConvertTo(moduleName, out moduleSpecifications))
                {
                    // if resourceNames are specified then we can not specify multiple modules name
                    if (moduleSpecifications != null && moduleSpecifications.Length > 1 && resourceNames != null)
                    {
                        errorList.Add(new ParseError(moduleNameBindingResult.Value.Extent,
                                                     "ImportDscResourceMultipleModulesNotSupportedWithName",
                                                     string.Format(CultureInfo.CurrentCulture, ParserStrings.ImportDscResourceMultipleModulesNotSupportedWithName)));
                    }

                    // if moduleversion is specified then we can not specify multiple modules name
                    if (moduleSpecifications != null && moduleSpecifications.Length > 1 && moduleVersion != null)
                    {
                        errorList.Add(new ParseError(moduleNameBindingResult.Value.Extent,
                                                     "ImportDscResourceMultipleModulesNotSupportedWithVersion",
                                                     string.Format(CultureInfo.CurrentCulture, ParserStrings.ImportDscResourceNeedParams)));
                    }

                    // if moduleversion is specified then we can not specify another version in modulespecification object of ModuleName
                    if (moduleSpecifications != null && (moduleSpecifications[0].Version != null || moduleSpecifications[0].MaximumVersion != null) && moduleVersion != null)
                    {
                        errorList.Add(new ParseError(moduleNameBindingResult.Value.Extent,
                                                     "ImportDscResourceMultipleModuleVersionsNotSupported",
                                                     string.Format(CultureInfo.CurrentCulture, ParserStrings.ImportDscResourceNeedParams)));
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
                    errorList.Add(new ParseError(moduleNameBindingResult.Value.Extent,
                                                 "RequiresInvalidStringArgument",
                                                 string.Format(CultureInfo.CurrentCulture, ParserStrings.RequiresInvalidStringArgument, moduleNameParam)));
                }
            }

            if (errorList.Count == 0)
            {
                // No errors, try to load the resources
                LoadResourcesFromModule(kwAst.Extent, moduleSpecifications, resourceNames, errorList);
            }

            return errorList.ToArray();
        }

        // This function performs semantic checks for Import-DscResource
        private static ParseError[] ImportResourceCheckSemantics(DynamicKeywordStatementAst kwAst)
        {
            List<ParseError> errorList = null;

            var keywordAst = Ast.GetAncestorAst<DynamicKeywordStatementAst>(kwAst.Parent);
            while (keywordAst != null)
            {
                if (keywordAst.Keyword.Keyword.Equals("Node"))
                {
                    errorList ??= new List<ParseError>();

                    errorList.Add(new ParseError(kwAst.Extent,
                                         "ImportDscResourceInsideNode",
                                         string.Format(CultureInfo.CurrentCulture, ParserStrings.ImportDscResourceInsideNode)));
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
        private static ParseError[] CheckMandatoryPropertiesPresent(DynamicKeywordStatementAst kwAst)
        {
            HashSet<string> mandatoryPropertiesNames = new(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in kwAst.Keyword.Properties)
            {
                if (pair.Value.Mandatory)
                {
                    mandatoryPropertiesNames.Add(pair.Key);
                }
            }

            // by design mandatoryPropertiesNames are not empty at this point:
            // every resource must have at least one Key property.

            HashtableAst hashtableAst = null;
            foreach (var ast in kwAst.CommandElements)
            {
                hashtableAst = ast as HashtableAst;
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
                var extent = kwAst.CommandElements[0].Extent;
                int i = 0;
                foreach (string name in mandatoryPropertiesNames)
                {
                    errors[i] = new ParseError(extent, "MissingValueForMandatoryProperty",
                        string.Format(CultureInfo.CurrentCulture, ParserStrings.MissingValueForMandatoryProperty,
                                    kwAst.Keyword.Keyword, kwAst.Keyword.Properties.First(
                                        p => StringComparer.OrdinalIgnoreCase.Equals(p.Value.Name, name)).Value.TypeConstraint, name));
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
        public static void LoadResourcesFromModule(IScriptExtent scriptExtent,
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
                            errorList.Add(new ParseError(scriptExtent,
                                                         "MultipleModuleEntriesFoundDuringParse",
                                                         string.Format(CultureInfo.CurrentCulture,
                                                                       ParserStrings.MultipleModuleEntriesFoundDuringParse,
                                                                       moduleToImport.Name)));
                        }
                        else
                        {
                            string moduleString = moduleToImport.Version == null
                                ? moduleToImport.Name
                                : string.Create(CultureInfo.CurrentCulture, $"<{moduleToImport.Name}, {moduleToImport.Version}>");

                            errorList.Add(new ParseError(scriptExtent, "ModuleNotFoundDuringParse",
                                string.Format(CultureInfo.CurrentCulture, ParserStrings.ModuleNotFoundDuringParse, moduleString)));
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
                var dscResourcesPath = Path.Combine(moduleInfo.ModuleBase, "DscResources");

                var resourcesFound = new List<string>();
                LoadPowerShellClassResourcesFromModule(moduleInfo, moduleInfo, resourcesToImport, resourcesFound, errorList, null, true, scriptExtent);

                if (Directory.Exists(dscResourcesPath))
                {
                    foreach (var resourceToImport in resourcesToImport)
                    {
                        bool foundResources = false;
                        foreach (var resourceDir in Directory.EnumerateDirectories(dscResourcesPath, resourceToImport))
                        {
                            var resourceName = Path.GetFileName(resourceDir);

                            bool foundCimSchema = false;
                            bool foundScriptSchema = false;
                            string schemaMofFilePath = string.Empty;

                            try
                            {
                                foundCimSchema = ImportCimKeywordsFromModule(moduleInfo, resourceName, out schemaMofFilePath);
                            }
                            catch (FileNotFoundException)
                            {
                                errorList.Add(new ParseError(scriptExtent,
                                                             "SchemaFileNotFound",
                                                             string.Format(CultureInfo.CurrentCulture, ParserStrings.SchemaFileNotFound, schemaMofFilePath)));
                            }
                            catch (PSInvalidOperationException e)
                            {
                                errorList.Add(new ParseError(scriptExtent,
                                                            e.ErrorRecord.FullyQualifiedErrorId,
                                                            e.Message));
                            }
                            catch (Exception e)
                            {
                                errorList.Add(new ParseError(scriptExtent,
                                                             "ExceptionParsingMOFFile",
                                                             string.Format(CultureInfo.CurrentCulture, ParserStrings.ExceptionParsingMOFFile, schemaMofFilePath, e.Message)));
                            }

                            var schemaScriptFilePath = string.Empty;

                            try
                            {
                                foundScriptSchema = ImportScriptKeywordsFromModule(moduleInfo, resourceName, out schemaScriptFilePath);
                            }
                            catch (FileNotFoundException)
                            {
                                errorList.Add(new ParseError(scriptExtent,
                                                             "SchemaFileNotFound",
                                                             string.Format(CultureInfo.CurrentCulture, ParserStrings.SchemaFileNotFound, schemaScriptFilePath)));
                            }
                            catch (Exception e)
                            {
                                // This shouldn't happen so just report the error as is
                                errorList.Add(new ParseError(scriptExtent,
                                                             "UnexpectedParseError",
                                                             string.Format(CultureInfo.CurrentCulture, e.ToString())));
                            }

                            if (foundCimSchema || foundScriptSchema)
                            {
                                foundResources = true;
                            }
                        }

                        //
                        // resourceToImport may be the friendly name of the DSC resource
                        //
                        if (!foundResources)
                        {
                            try
                            {
                                foundResources = ImportCimKeywordsFromModule(moduleInfo, resourceToImport, out _);
                            }
                            catch (Exception)
                            {
                            }
                        }

                        // resource name without wildcard (*) should be imported only once
                        if (!resourceToImport.Contains('*') && foundResources)
                        {
                            resourcesFound.Add(resourceToImport);
                        }
                    }
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
                    if (!resourceNameToImport.Contains('*'))
                    {
                        errorList.Add(new ParseError(scriptExtent,
                                                     "DscResourcesNotFoundDuringParsing",
                                                     string.Format(CultureInfo.CurrentCulture, ParserStrings.DscResourcesNotFoundDuringParsing, resourceNameToImport)));
                    }
                }
            }
        }

        private static void LoadPowerShellClassResourcesFromModule(PSModuleInfo primaryModuleInfo, PSModuleInfo moduleInfo, ICollection<string> resourcesToImport, ICollection<string> resourcesFound,
            List<ParseError> errorList,
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
#if CORECLR
                throw PSTraceSource.NewArgumentException("isConfiguration", ParserStrings.ConfigurationNotSupportedInPowerShellCore);
#else
                ResolveEventHandler reh = (sender, args) => CurrentDomain_ReflectionOnlyAssemblyResolve(sender, args, moduleInfo);

                try
                {
                    AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += reh;
                    var assembly = moduleInfo.ImplementingAssembly;
                    if (assembly == null && moduleInfo.Path != null)
                    {
                        try
                        {
                            var path = moduleInfo.Path;

                            if (moduleInfo.RootModule != null && !Path.GetExtension(moduleInfo.Path).Equals(".dll", StringComparison.OrdinalIgnoreCase))
                            {
                                path = moduleInfo.ModuleBase + "\\" + moduleInfo.RootModule;
                            }

                            assembly = Assembly.ReflectionOnlyLoadFrom(path);
                        }
                        catch { }
                    }

                    // Ignore the module if we can't find the assembly.
                    if (assembly != null)
                    {
                        ImportKeywordsFromAssembly(moduleInfo, resourcesToImport, resourcesFound, functionsToDefine, assembly);
                    }
                }
                finally
                {
                    AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve -= reh;
                }
#endif
            }
            else
            {
                string scriptPath = null;
                // handle RootModule and nestedModule together
                if (moduleInfo.RootModule != null)
                {
                    scriptPath = Path.Combine(moduleInfo.ModuleBase, moduleInfo.RootModule);
                }
                else if (moduleInfo.Path != null)
                {
                    scriptPath = moduleInfo.Path;
                }

                ImportKeywordsFromScriptFile(scriptPath, primaryModuleInfo, resourcesToImport, resourcesFound, functionsToDefine, errorList, extent);
            }

            if (moduleInfo.NestedModules != null && recurse)
            {
                foreach (var nestedModule in moduleInfo.NestedModules)
                {
                    LoadPowerShellClassResourcesFromModule(primaryModuleInfo, nestedModule, resourcesToImport, resourcesFound, errorList, functionsToDefine, recurse: false, extent: extent);
                }
            }
        }

#if !CORECLR
        private static Assembly CurrentDomain_ReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args, PSModuleInfo moduleInfo)
        {
            AssemblyName name = new AssemblyName(args.Name);

            if (moduleInfo != null && !string.IsNullOrEmpty(moduleInfo.Path))
            {
                string asmToCheck = Path.GetDirectoryName(moduleInfo.Path) + "\\" + name.Name + ".dll";
                if (File.Exists(asmToCheck))
                {
                    return Assembly.ReflectionOnlyLoadFrom(asmToCheck);
                }

                asmToCheck = Path.GetDirectoryName(moduleInfo.Path) + "\\" + name.Name + ".exe";
                if (File.Exists(asmToCheck))
                {
                    return Assembly.ReflectionOnlyLoadFrom(asmToCheck);
                }
            }

            return Assembly.ReflectionOnlyLoad(args.Name);
        }
#endif

        /// <summary>
        /// </summary>
        /// <param name="moduleInfo"></param>
        /// <param name="resourcesToImport"></param>
        /// <param name="functionsToDefine"></param>
        /// <returns>The list of resources imported from this module.</returns>
        public static List<string> ImportClassResourcesFromModule(PSModuleInfo moduleInfo, ICollection<string> resourcesToImport, Dictionary<string, ScriptBlock> functionsToDefine)
        {
            var resourcesImported = new List<string>();
            LoadPowerShellClassResourcesFromModule(moduleInfo, moduleInfo, resourcesToImport, resourcesImported, null, functionsToDefine);
            return resourcesImported;
        }

        internal static string GenerateMofForAst(TypeDefinitionAst typeAst)
        {
            var embeddedInstanceTypes = new List<object>();
            var sb = new StringBuilder();

            GenerateMofForAst(typeAst, sb, embeddedInstanceTypes);
            var visitedInstances = new List<object>();
            visitedInstances.Add(typeAst);
            ProcessEmbeddedInstanceTypes(embeddedInstanceTypes, visitedInstances, sb);

            return sb.ToString();
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
                enumNames = propTypeName._typeDefinitionAst.Members.Select(static m => m.Name).ToArray();
                isArrayType = false;
                embeddedInstanceType = null;
                return "string";
            }

            if (!embeddedInstanceTypes.Contains(propTypeName._typeDefinitionAst))
            {
                embeddedInstanceTypes.Add(propTypeName._typeDefinitionAst);
            }

            // The type is obviously not a string, but in the mof, we represent
            // it as string (really, embeddedinstance of the class type)
            embeddedInstanceType = propTypeName.Name.Replace('.', '_');
            return "string";
        }

        private static void GenerateMofForAst(TypeDefinitionAst typeAst, StringBuilder sb, List<object> embeddedInstanceTypes)
        {
            var className = typeAst.Name;
            sb.Append(CultureInfo.InvariantCulture, $"[ClassVersion(\"1.0.0\"), FriendlyName(\"{className}\")]\nclass {className}");

            if (typeAst.Attributes.Any(static a => a.TypeName.GetReflectionAttributeType() == typeof(DscResourceAttribute)))
            {
                sb.Append(" : OMI_BaseResource");
            }

            sb.Append("\n{\n");

            ProcessMembers(sb, embeddedInstanceTypes, typeAst, className);

            Queue<object> bases = new();
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
                            ProcessMembers(sb, embeddedInstanceTypes, td._typeDefinitionAst, className);
                            foreach (var b1 in td._typeDefinitionAst.BaseTypes)
                            {
                                bases.Enqueue(b1);
                            }
                        }

                        continue;
                    }
                }

                var type = b as Type;
                if (type != null)
                {
                    ProcessMembers(type, sb, embeddedInstanceTypes, className);
                    var t = type.BaseType;
                    if (t != null)
                    {
                        bases.Enqueue(t);
                    }
                }
            }

            sb.Append("};");
        }

        /// <summary>
        /// Gets the line no for DSC Class Resource Get/Set/Test methods.
        /// </summary>
        /// <param name="typeDefinitionAst"></param>
        /// <param name="methodsLinePosition"></param>
        private static bool GetResourceMethodsLineNumber(TypeDefinitionAst typeDefinitionAst, out Dictionary<string, int> methodsLinePosition)
        {
            const string getMethodName = "Get";
            const string setMethodName = "Set";
            const string testMethodName = "Test";

            methodsLinePosition = new Dictionary<string, int>();
            foreach (var member in typeDefinitionAst.Members)
            {
                var functionMemberAst = member as FunctionMemberAst;
                if (functionMemberAst != null)
                {
                    if (functionMemberAst.Name.Equals(getMethodName, StringComparison.OrdinalIgnoreCase))
                    {
                        methodsLinePosition[getMethodName] = functionMemberAst.NameExtent.StartLineNumber;
                    }
                    else if (functionMemberAst.Name.Equals(setMethodName, StringComparison.OrdinalIgnoreCase))
                    {
                        methodsLinePosition[setMethodName] = functionMemberAst.NameExtent.StartLineNumber;
                    }
                    else if (functionMemberAst.Name.Equals(testMethodName, StringComparison.OrdinalIgnoreCase))
                    {
                        methodsLinePosition[testMethodName] = functionMemberAst.NameExtent.StartLineNumber;
                    }
                }
            }

            // All 3 methods (Get/Set/Test) position should be found.
            return (methodsLinePosition.Count == 3);
        }

        /// <summary>
        /// Gets the line no for DSC Class Resource Get/Set/Test methods.
        /// </summary>
        /// <param name="moduleInfo"></param>
        /// <param name="resourceName"></param>
        /// <param name="resourceMethodsLinePosition"></param>
        /// <param name="resourceFilePath"></param>
        public static bool GetResourceMethodsLinePosition(PSModuleInfo moduleInfo, string resourceName, out Dictionary<string, int> resourceMethodsLinePosition, out string resourceFilePath)
        {
            resourceMethodsLinePosition = null;
            resourceFilePath = string.Empty;
            if (moduleInfo == null || string.IsNullOrEmpty(resourceName))
            {
                return false;
            }

            IEnumerable<Ast> resourceDefinitions;
            List<string> moduleFiles = new();
            if (moduleInfo.RootModule != null)
            {
                moduleFiles.Add(moduleInfo.Path);
            }

            if (moduleInfo.NestedModules != null)
            {
                foreach (var nestedModule in moduleInfo.NestedModules.Where(static m => !string.IsNullOrEmpty(m.Path)))
                {
                    moduleFiles.Add(nestedModule.Path);
                }
            }

            foreach (string moduleFile in moduleFiles)
            {
                if (GetResourceDefinitionsFromModule(moduleFile, out resourceDefinitions, null, null))
                {
                    foreach (var r in resourceDefinitions)
                    {
                        var resourceDefnAst = (TypeDefinitionAst)r;
                        if (!resourceName.Equals(resourceDefnAst.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        if (GetResourceMethodsLineNumber(resourceDefnAst, out resourceMethodsLinePosition))
                        {
                            resourceFilePath = moduleFile;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static void ProcessMembers(StringBuilder sb, List<object> embeddedInstanceTypes, TypeDefinitionAst typeDefinitionAst, string className)
        {
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
                    // TODO - validate type and name
                    mofType = MapTypeToMofType(memberType, member.Name, className, out isArrayType,
                        out embeddedInstanceType,
                        embeddedInstanceTypes);
                    if (memberType.IsEnum)
                    {
                        enumNames = Enum.GetNames(memberType);
                    }
                }
                else
                {
                    // PropertyType can't be null, we used typeof(object) above in that case so we don't get here.
                    mofType = MapTypeNameToMofType(property.PropertyType.TypeName, member.Name, className,
                        out isArrayType,
                        out embeddedInstanceType, embeddedInstanceTypes, ref enumNames);
                }

                string arrayAffix = isArrayType ? "[]" : string.Empty;

                sb.Append(CultureInfo.InvariantCulture, $"    {MapAttributesToMof(enumNames, attributes, embeddedInstanceType)}{mofType} {member.Name}{arrayAffix};\n");
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="resourceDefinitions"></param>
        /// <param name="errorList"></param>
        /// <param name="extent"></param>
        /// <returns></returns>
        private static bool GetResourceDefinitionsFromModule(string fileName, out IEnumerable<Ast> resourceDefinitions, List<ParseError> errorList, IScriptExtent extent)
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

            // If script dynamic keywords has already been loaded from the file, don't load them again.
            // The ScriptKeywordFile cache is always initialized from scratch by the top-level
            // configuration statement so within a single compile, things shouldn't change.
            if (!File.Exists(fileName) || ScriptKeywordFileCache.Contains(fileName))
            {
                return false;
            }

            // BUGBUG - need to fix up how the module gets set.
            Token[] tokens;
            ParseError[] errors;
            var ast = Parser.ParseFile(fileName, out tokens, out errors);

            if (errors != null && errors.Length > 0)
            {
                if (errorList != null && extent != null)
                {
                    List<string> errorMessages = new();
                    foreach (var error in errors)
                    {
                        errorMessages.Add(error.ToString());
                    }

                    errorList.Add(new ParseError(extent, "FailToParseModuleScriptFile",
                        string.Format(CultureInfo.CurrentCulture, ParserStrings.FailToParseModuleScriptFile, fileName, string.Join(Environment.NewLine, errorMessages))));
                }

                return false;
            }

            resourceDefinitions = ast.FindAll(n =>
            {
                var typeAst = n as TypeDefinitionAst;
                if (typeAst != null)
                {
                    for (int i = 0; i < typeAst.Attributes.Count; i++)
                    {
                        var a = typeAst.Attributes[i];
                        if (a.TypeName.GetReflectionAttributeType() == typeof(DscResourceAttribute)) return true;
                    }
                }

                return false;
            }, false);

            return true;
        }

        /// <summary>
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="module"></param>
        /// <param name="resourcesToImport"></param>
        /// <param name="resourcesFound"></param>
        /// <param name="functionsToDefine"></param>
        /// <param name="errorList"></param>
        /// <param name="extent"></param>
        /// <returns></returns>
        private static bool ImportKeywordsFromScriptFile(string fileName, PSModuleInfo module, ICollection<string> resourcesToImport, ICollection<string> resourcesFound, Dictionary<string, ScriptBlock> functionsToDefine, List<ParseError> errorList, IScriptExtent extent)
        {
            IEnumerable<Ast> resourceDefinitions;
            if (!GetResourceDefinitionsFromModule(fileName, out resourceDefinitions, errorList, extent))
            {
                return false;
            }

            var result = false;
            var parser = new CimDSCParser(MyClassCallback);

            const WildcardOptions wildcardOptions = WildcardOptions.IgnoreCase | WildcardOptions.CultureInvariant;
            IEnumerable<WildcardPattern> patternList = SessionStateUtilities.CreateWildcardsFromStrings(module._declaredDscResourceExports, wildcardOptions);

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
                    if ((WildcardPattern.Get(toImport, WildcardOptions.IgnoreCase)).IsMatch(resourceDefnAst.Name))
                    {
                        skip = false;
                        break;
                    }
                }

                if (skip) continue;

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

                var mof = GenerateMofForAst(resourceDefnAst);

                ProcessMofForDynamicKeywords(module, resourcesFound, functionsToDefine, parser, mof, runAsBehavior);
            }

            return result;
        }

        private static readonly Dictionary<Type, string> s_mapPrimitiveDotNetTypeToMof = new()
        {
            { typeof(sbyte), "sint8" },
            { typeof(byte), "uint8"},
            { typeof(short), "sint16"},
            { typeof(ushort), "uint16"},
            { typeof(int), "sint32"},
            { typeof(uint), "uint32"},
            { typeof(long), "sint64"},
            { typeof(ulong), "uint64" },
            { typeof(float), "real32"},
            { typeof(double), "real64"},
            { typeof(bool), "boolean"},
            { typeof(string), "string" },
            { typeof(DateTime), "datetime" },
            { typeof(PSCredential), "string" },
            { typeof(char), "char16" },
        };

        private static bool AreQualifiersSame(CimReadOnlyKeyedCollection<CimQualifier> oldQualifier, CimReadOnlyKeyedCollection<CimQualifier> newQualifiers)
        {
            if (oldQualifier.Count != newQualifiers.Count)
            {
                return false;
            }

            foreach (var qual in oldQualifier)
            {
                // Find the qualifier in new class
                var newQual = newQualifiers[qual.Name];
                if (newQual == null)
                {
                    return false;
                }

                if ((qual.CimType != newQual.CimType) ||
                    (qual.Flags != newQual.Flags))
                {
                    return false;
                }

                if ((qual.Value == null && newQual.Value != null) ||
                    (qual.Value != null && newQual.Value == null) ||
                    (qual.Value != null && newQual.Value != null &&
                        !string.Equals(qual.Value.ToString(), newQual.Value.ToString(), StringComparison.OrdinalIgnoreCase)
                    )
                  )
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ArePropertiesSame(CimReadOnlyKeyedCollection<CimPropertyDeclaration> oldProperties, CimReadOnlyKeyedCollection<CimPropertyDeclaration> newProperties)
        {
            if (oldProperties.Count != newProperties.Count)
            {
                return false;
            }

            foreach (var prop in oldProperties)
            {
                // Find the property in new class
                var newProp = newProperties[prop.Name];
                if (newProp == null)
                {
                    return false;
                }
                // flags and type should match
                if ((prop.CimType != newProp.CimType) ||
                    (prop.Flags != newProp.Flags))
                {
                    return false;
                }

                if (!AreQualifiersSame(prop.Qualifiers, newProp.Qualifiers))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsSameNestedObject(CimClass oldClass, CimClass newClass)
        {
            // #1 both the classes should be nested class and not DSC resource
            if ((oldClass.CimSuperClassName != null && string.Equals("OMI_BaseResource", oldClass.CimSuperClassName, StringComparison.OrdinalIgnoreCase)) ||
                (newClass.CimSuperClassName != null && string.Equals("OMI_BaseResource", newClass.CimSuperClassName, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }
            // #2 qualifier count, names, values and types should be same
            if (!AreQualifiersSame(oldClass.CimClassQualifiers, newClass.CimClassQualifiers))
            {
                return false;
            }

            // #3 property count, names, values, qualifiers and types should be same
            if (!ArePropertiesSame(oldClass.CimClassProperties, newClass.CimClassProperties))
            {
                return false;
            }

            return true;
        }

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
                    return MapTypeToMofType(type.GetElementType(), memberName, className, out temp, out embeddedInstanceType, embeddedInstanceTypes);
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

        private static string MapAttributesToMof(string[] enumNames, IEnumerable<object> customAttributes, string embeddedInstanceType)
        {
            var sb = new StringBuilder();

            sb.Append('[');
            bool needComma = false;
            foreach (var attr in customAttributes)
            {
                var dscProperty = attr as DscPropertyAttribute;
                if (dscProperty != null)
                {
                    if (dscProperty.Key)
                    {
                        sb.AppendFormat(CultureInfo.InvariantCulture, "{0}key", needComma ? ", " : string.Empty);
                        needComma = true;
                    }

                    if (dscProperty.Mandatory)
                    {
                        sb.AppendFormat(CultureInfo.InvariantCulture, "{0}required", needComma ? ", " : string.Empty);
                        needComma = true;
                    }

                    if (dscProperty.NotConfigurable)
                    {
                        sb.AppendFormat(CultureInfo.InvariantCulture, "{0}read", needComma ? ", " : string.Empty);
                        needComma = true;
                    }

                    continue;
                }

                var validateSet = attr as ValidateSetAttribute;
                if (validateSet != null)
                {
                    bool valueMapComma = false;
                    StringBuilder sbValues = new(", Values{");
                    sb.AppendFormat(CultureInfo.InvariantCulture, "{0}ValueMap{{", needComma ? ", " : string.Empty);
                    needComma = true;

                    foreach (var value in validateSet.ValidValues)
                    {
                        sb.AppendFormat(CultureInfo.InvariantCulture, "{0}\"{1}\"", valueMapComma ? ", " : string.Empty, value);
                        sbValues.AppendFormat(CultureInfo.InvariantCulture, "{0}\"{1}\"", valueMapComma ? ", " : string.Empty, value);
                        valueMapComma = true;
                    }

                    sb.Append('}');
                    sb.Append(sbValues);
                    sb.Append('}');
                }
            }

            // Default is write - skipped if we already have some attributes
            if (sb.Length == 1)
            {
                sb.Append("write");
                needComma = true;
            }

            if (enumNames != null)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, "{0}ValueMap{{", needComma ? ", " : string.Empty);
                needComma = false;
                foreach (var name in enumNames)
                {
                    sb.AppendFormat(CultureInfo.InvariantCulture, "{0}\"{1}\"", needComma ? ", " : string.Empty, name);
                    needComma = true;
                }

                sb.Append("}, Values{");
                needComma = false;
                foreach (var name in enumNames)
                {
                    sb.AppendFormat(CultureInfo.InvariantCulture, "{0}\"{1}\"", needComma ? ", " : string.Empty, name);
                    needComma = true;
                }

                sb.Append('}');
            }
            else if (embeddedInstanceType != null)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, "{0}EmbeddedInstance(\"{1}\")", needComma ? ", " : string.Empty, embeddedInstanceType);
            }

            sb.Append(']');
            return sb.ToString();
        }

        /// <summary>
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GenerateMofForType(Type type)
        {
            var embeddedInstanceTypes = new List<object>();
            var sb = new StringBuilder();

            GenerateMofForType(type, sb, embeddedInstanceTypes);
            var visitedInstances = new List<object>();
            visitedInstances.Add(type);
            ProcessEmbeddedInstanceTypes(embeddedInstanceTypes, visitedInstances, sb);

            return sb.ToString();
        }

        private static void ProcessEmbeddedInstanceTypes(List<object> embeddedInstanceTypes, List<object> visitedInstances, StringBuilder sb)
        {
            StringBuilder nestedSb = null;

            while (embeddedInstanceTypes.Count > 0)
            {
                if (nestedSb == null)
                {
                    nestedSb = new StringBuilder();
                }
                else
                {
                    nestedSb.Clear();
                }

                var batchedTypes = embeddedInstanceTypes.Where(x => !visitedInstances.Contains(x)).ToArray();
                embeddedInstanceTypes.Clear();

                for (int i = batchedTypes.Length - 1; i >= 0; i--)
                {
                    visitedInstances.Add(batchedTypes[i]);
                    var type = batchedTypes[i] as Type;
                    if (type != null)
                    {
                        GenerateMofForType(type, nestedSb, embeddedInstanceTypes);
                    }
                    else
                    {
                        GenerateMofForAst((TypeDefinitionAst)batchedTypes[i], nestedSb, embeddedInstanceTypes);
                    }

                    nestedSb.Append('\n');
                }

                sb.Insert(0, nestedSb.ToString());
            }
        }

        private static void GenerateMofForType(Type type, StringBuilder sb, List<object> embeddedInstanceTypes)
        {
            var className = type.Name;
            // Friendly name is required by module validator to verify resource instance against the exclusive resource name list.
            sb.Append(CultureInfo.InvariantCulture, $"[ClassVersion(\"1.0.0\"), FriendlyName(\"{className}\")]\nclass {className}");

            if (type.GetCustomAttributes<DscResourceAttribute>().Any())
            {
                sb.Append(" : OMI_BaseResource");
            }

            sb.Append("\n{\n");

            ProcessMembers(type, sb, embeddedInstanceTypes, className);
            sb.Append("};");
        }

        private static void ProcessMembers(Type type, StringBuilder sb, List<object> embeddedInstanceTypes, string className)
        {
            foreach (var member in type.GetMembers(BindingFlags.Instance | BindingFlags.Public).Where(static m => m is PropertyInfo || m is FieldInfo))
            {
                if (member.CustomAttributes.All(static cad => cad.AttributeType != typeof(DscPropertyAttribute)))
                {
                    continue;
                }

                Type memberType;
                var propertyInfo = member as PropertyInfo;
                if (propertyInfo == null)
                {
                    var fieldInfo = (FieldInfo)member;
                    memberType = fieldInfo.FieldType;
                }
                else
                {
                    if (propertyInfo.GetSetMethod() == null)
                    {
                        continue;
                    }

                    memberType = propertyInfo.PropertyType;
                }

                // TODO - validate type and name
                bool isArrayType;
                string embeddedInstanceType;
                string mofType = MapTypeToMofType(memberType, member.Name, className, out isArrayType, out embeddedInstanceType,
                    embeddedInstanceTypes);
                string arrayAffix = isArrayType ? "[]" : string.Empty;

                var enumNames = memberType.IsEnum
                    ? Enum.GetNames(memberType)
                    : null;
                sb.Append(CultureInfo.InvariantCulture, $"    {MapAttributesToMof(enumNames, member.GetCustomAttributes(true), embeddedInstanceType)}{mofType} {member.Name}{arrayAffix};\n");
            }
        }

        private static bool ImportKeywordsFromAssembly(PSModuleInfo module,
                                                       ICollection<string> resourcesToImport,
                                                       ICollection<string> resourcesFound,
                                                       Dictionary<string, ScriptBlock> functionsToDefine,
                                                       Assembly assembly)
        {
            bool result = false;

            var parser = new Microsoft.PowerShell.DesiredStateConfiguration.CimDSCParser(MyClassCallback);

            IEnumerable<Type> resourceDefinitions =
                assembly.GetTypes().Where(static t => t.GetCustomAttributes<DscResourceAttribute>().Any());

            foreach (var r in resourceDefinitions)
            {
                result = true;
                bool skip = true;

                foreach (var toImport in resourcesToImport)
                {
                    if ((WildcardPattern.Get(toImport, WildcardOptions.IgnoreCase)).IsMatch(r.Name))
                    {
                        skip = false;
                        break;
                    }
                }

                if (skip) continue;

                var mof = GenerateMofForType(r);

                ProcessMofForDynamicKeywords(module, resourcesFound, functionsToDefine, parser, mof, DSCResourceRunAsCredential.Default);
            }

            return result;
        }

        private static void ProcessMofForDynamicKeywords(PSModuleInfo module, ICollection<string> resourcesFound,
            Dictionary<string, ScriptBlock> functionsToDefine, CimDSCParser parser, string mof, DSCResourceRunAsCredential runAsBehavior)
        {
            foreach (var c in parser.ParseSchemaMofFileBuffer(mof))
            {
                var className = c.CimSystemProperties.ClassName;
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
                ClassCache[moduleQualifiedResourceName] = new DscClassCacheEntry(runAsBehavior, false, c);
                ByClassModuleCache[className] = new Tuple<string, Version>(module.Name, module.Version);
                resourcesFound.Add(className);
                CreateAndRegisterKeywordFromCimClass(module.Name, module.Version, c, functionsToDefine, runAsBehavior);
            }
        }

        /// <summary>
        /// Import the CIM functions from a module...
        /// </summary>
        /// <param name="module"></param>
        /// <param name="resourceName"></param>
        /// <param name="schemaFilePath">Full path of the loaded schema file...</param>
        /// <returns></returns>
        public static bool ImportCimKeywordsFromModule(PSModuleInfo module, string resourceName, out string schemaFilePath)
        {
            return ImportCimKeywordsFromModule(module, resourceName, out schemaFilePath, null);
        }

        /// <summary>
        /// Import the CIM functions from a module...
        /// </summary>
        /// <param name="module"></param>
        /// <param name="resourceName"></param>
        /// <param name="schemaFilePath">Full path of the loaded schema file...</param>
        /// <param name="functionsToDefine"></param>
        /// <returns></returns>
        public static bool ImportCimKeywordsFromModule(PSModuleInfo module, string resourceName, out string schemaFilePath, Dictionary<string, ScriptBlock> functionsToDefine)
        {
            return ImportCimKeywordsFromModule(module, resourceName, out schemaFilePath, functionsToDefine, null);
        }

        /// <summary>
        /// Import the CIM functions from a module...
        /// </summary>
        /// <param name="module"></param>
        /// <param name="resourceName"></param>
        /// <param name="schemaFilePath">Full path of the loaded schema file...</param>
        /// <param name="functionsToDefine"></param>
        /// <param name="errors">Error reported during deserialization.</param>
        /// <returns></returns>
        public static bool ImportCimKeywordsFromModule(PSModuleInfo module, string resourceName, out string schemaFilePath, Dictionary<string, ScriptBlock> functionsToDefine, Collection<Exception> errors)
        {
            if (module == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(module));
            }

            if (resourceName == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(resourceName));
            }

            string dscResourcesPath = Path.Combine(module.ModuleBase, "DscResources");
            schemaFilePath = Path.Combine(Path.Combine(dscResourcesPath, resourceName), resourceName + ".schema.mof");

            if (File.Exists(schemaFilePath))
            {
                // If the file has already been loaded, don't load it again.
                // The class cache is always initialized from scratch by the top-level
                // configuration statement so within a single compile, things shouldn't
                // change.
                var classes = GetCachedClassByFileName(schemaFilePath) ?? ImportClasses(schemaFilePath, new Tuple<string, Version>(module.Name, module.Version), errors);
                if (classes != null)
                {
                    foreach (var c in classes)
                    {
                        CreateAndRegisterKeywordFromCimClass(module.Name, module.Version, c, functionsToDefine, DSCResourceRunAsCredential.Default);
                        ClearImplicitlyImportedFlagFromResourceInClassCache(module, c);
                    }
                }

                return true;
            }
            else if (Directory.Exists(dscResourcesPath))
            {
                //
                // Cannot find the schema file, then resourceName may be a friendly name,
                // try to search all DscResources' schemas under DscResources folder
                //
                try
                {
                    foreach (var directory in Directory.EnumerateDirectories(dscResourcesPath))
                    {
                        IEnumerable<string> schemaFiles = Directory.EnumerateFiles(directory, "*.schema.mof", SearchOption.TopDirectoryOnly);
                        string tempSchemaFilepath = schemaFiles.FirstOrDefault();

                        Debug.Assert(schemaFiles.Count() == 1, "A valid DSCResource module can have only one schema mof file");
                        
                        if (tempSchemaFilepath is not null)
                        {
                            var classes = GetCachedClassByFileName(tempSchemaFilepath) ?? ImportClasses(tempSchemaFilepath, new Tuple<string, Version>(module.Name, module.Version), errors);
                            if (classes != null)
                            {
                                //
                                // search if class's friendly name is the given resourceName
                                //
                                foreach (var c in classes)
                                {
                                    var alias = GetFriendlyName(c);
                                    if (string.Equals(alias, resourceName, StringComparison.OrdinalIgnoreCase))
                                    {
                                        CreateAndRegisterKeywordFromCimClass(module.Name, module.Version, c, functionsToDefine, DSCResourceRunAsCredential.Default);
                                        ClearImplicitlyImportedFlagFromResourceInClassCache(module, c);
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    //
                    // silent in case of exception
                    //
                }
            }

            return false;
        }

        /// <summary>
        /// Clear the 'IsImportedImplicitly' flag when explicitly importing a resource.
        /// </summary>
        /// <param name="module"></param>
        /// <param name="cimClass"></param>
        private static void ClearImplicitlyImportedFlagFromResourceInClassCache(PSModuleInfo module, CimClass cimClass)
        {
            var className = cimClass.CimSystemProperties.ClassName;
            var alias = GetFriendlyName(cimClass);
            var friendlyName = string.IsNullOrEmpty(alias) ? className : alias;
            var moduleQualifiedResourceName = GetModuleQualifiedResourceName(module.Name, module.Version.ToString(), className, friendlyName);
            ClassCache[moduleQualifiedResourceName].IsImportedImplicitly = false;
        }

        /// <summary>
        /// Imports configuration keywords from a .psm1 file.
        /// </summary>
        /// <param name="module"></param>
        /// <param name="resourceName"></param>
        /// <param name="schemaFilePath"></param>
        public static bool ImportScriptKeywordsFromModule(PSModuleInfo module, string resourceName, out string schemaFilePath)
        {
            return ImportScriptKeywordsFromModule(module, resourceName, out schemaFilePath, null);
        }

        /// <summary>
        /// Imports configuration keywords from a .psm1 file.
        /// </summary>
        /// <param name="module"></param>
        /// <param name="resourceName"></param>
        /// <param name="schemaFilePath"></param>
        /// <param name="functionsToDefine"></param>
        public static bool ImportScriptKeywordsFromModule(PSModuleInfo module, string resourceName, out string schemaFilePath, Dictionary<string, ScriptBlock> functionsToDefine)
        {
            if (module == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(module));
            }

            if (resourceName == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(resourceName));
            }

            schemaFilePath = Path.Combine(Path.Combine(Path.Combine(module.ModuleBase, "DscResources"), resourceName), resourceName + ".Schema.psm1");

            if (File.Exists(schemaFilePath) && !s_currentImportingScriptFiles.Contains(schemaFilePath))
            {
                // If script dynamic keywords has already been loaded from the file, don't load them again.
                // The ScriptKeywordFile cache is always initialized from scratch by the top-level
                // configuration statement so within a single compile, things shouldn't change.
                if (!ScriptKeywordFileCache.Contains(schemaFilePath))
                {
                    // Parsing the file is all that needs to be done to add the keywords
                    // BUGBUG - need to fix up how the module gets set.
                    // BUGBUG - should fail somehow if errors is not empty
                    Token[] tokens; ParseError[] errors;
                    s_currentImportingScriptFiles.Add(schemaFilePath);
                    Parser.ParseFile(schemaFilePath, out tokens, out errors);
                    s_currentImportingScriptFiles.Remove(schemaFilePath);
                    ScriptKeywordFileCache.Add(schemaFilePath);
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns an error record to use in the case of a malformed resource reference in the DependsOn list.
        /// </summary>
        /// <param name="badDependsOnReference">The malformed resource.</param>
        /// <param name="definingResource">The referencing resource instance.</param>
        /// <returns></returns>
        public static ErrorRecord GetBadlyFormedRequiredResourceIdErrorRecord(string badDependsOnReference, string definingResource)
        {
            PSInvalidOperationException e = PSTraceSource.NewInvalidOperationException(
                 ParserStrings.GetBadlyFormedRequiredResourceId, badDependsOnReference, definingResource);

            e.SetErrorId("GetBadlyFormedRequiredResourceId");
            return e.ErrorRecord;
        }

        /// <summary>
        /// Returns an error record to use in the case of a malformed resource reference in the exclusive resources list.
        /// </summary>
        /// <param name="badExclusiveResourcereference">The malformed resource.</param>
        /// <param name="definingResource">The referencing resource instance.</param>
        /// <returns></returns>
        public static ErrorRecord GetBadlyFormedExclusiveResourceIdErrorRecord(string badExclusiveResourcereference, string definingResource)
        {
            PSInvalidOperationException e = PSTraceSource.NewInvalidOperationException(
                 ParserStrings.GetBadlyFormedExclusiveResourceId, badExclusiveResourcereference, definingResource);

            e.SetErrorId("GetBadlyFormedExclusiveResourceId");
            return e.ErrorRecord;
        }

        /// <summary>
        /// If a partial configuration is in 'Pull' Mode, it needs a configuration source.
        /// </summary>
        /// <param name="resourceId"></param>
        /// <returns></returns>
        public static ErrorRecord GetPullModeNeedConfigurationSource(string resourceId)
        {
            PSInvalidOperationException e = PSTraceSource.NewInvalidOperationException(
                 ParserStrings.GetPullModeNeedConfigurationSource, resourceId);

            e.SetErrorId("GetPullModeNeedConfigurationSource");
            return e.ErrorRecord;
        }

        /// <summary>
        /// Refresh Mode can not be Disabled for the Partial Configurations.
        /// </summary>
        /// <param name="resourceId"></param>
        /// <returns></returns>
        public static ErrorRecord DisabledRefreshModeNotValidForPartialConfig(string resourceId)
        {
            PSInvalidOperationException e = PSTraceSource.NewInvalidOperationException(
                 ParserStrings.DisabledRefreshModeNotValidForPartialConfig, resourceId);

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
            PSInvalidOperationException e = PSTraceSource.NewInvalidOperationException(
                 ParserStrings.DuplicateResourceIdInNodeStatement, duplicateResourceId, nodeName);

            e.SetErrorId("DuplicateResourceIdInNodeStatement");
            return e.ErrorRecord;
        }

        /// <summary>
        /// Returns an error record to use in the case of a configuration name is invalid.
        /// </summary>
        /// <param name="configurationName"></param>
        /// <returns></returns>
        public static ErrorRecord InvalidConfigurationNameErrorRecord(string configurationName)
        {
            PSInvalidOperationException e = PSTraceSource.NewInvalidOperationException(
                ParserStrings.InvalidConfigurationName, configurationName);

            e.SetErrorId("InvalidConfigurationName");
            return e.ErrorRecord;
        }

        /// <summary>
        /// Returns an error record to use in the case of the given value for a property is invalid.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="value"></param>
        /// <param name="keywordName"></param>
        /// <param name="validValues"></param>
        /// <returns></returns>
        public static ErrorRecord InvalidValueForPropertyErrorRecord(string propertyName, string value, string keywordName, string validValues)
        {
            PSInvalidOperationException e = PSTraceSource.NewInvalidOperationException(
                ParserStrings.InvalidValueForProperty, value, propertyName, keywordName, validValues);

            e.SetErrorId("InvalidValueForProperty");
            return e.ErrorRecord;
        }

        /// <summary>
        /// Returns an error record to use in case the given property is not valid LocalConfigurationManager property.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="validProperties"></param>
        /// <returns></returns>
        public static ErrorRecord InvalidLocalConfigurationManagerPropertyErrorRecord(string propertyName, string validProperties)
        {
            PSInvalidOperationException e = PSTraceSource.NewInvalidOperationException(
                ParserStrings.InvalidLocalConfigurationManagerProperty, propertyName, validProperties);

            e.SetErrorId("InvalidLocalConfigurationManagerProperty");
            return e.ErrorRecord;
        }

        /// <summary>
        /// Returns an error record to use in the case of the given value for a property is not supported.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="value"></param>
        /// <param name="keywordName"></param>
        /// <param name="validValues"></param>
        /// <returns></returns>
        public static ErrorRecord UnsupportedValueForPropertyErrorRecord(string propertyName, string value, string keywordName, string validValues)
        {
            PSInvalidOperationException e = PSTraceSource.NewInvalidOperationException(
                ParserStrings.UnsupportedValueForProperty, value, propertyName, keywordName, validValues);

            e.SetErrorId("UnsupportedValueForProperty");
            return e.ErrorRecord;
        }

        /// <summary>
        /// Returns an error record to use in the case of no value is provided for a mandatory property.
        /// </summary>
        /// <param name="keywordName"></param>
        /// <param name="typeName"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static ErrorRecord MissingValueForMandatoryPropertyErrorRecord(string keywordName, string typeName, string propertyName)
        {
            PSInvalidOperationException e = PSTraceSource.NewInvalidOperationException(
                ParserStrings.MissingValueForMandatoryProperty, keywordName, typeName, propertyName);

            e.SetErrorId("MissingValueForMandatoryProperty");
            return e.ErrorRecord;
        }

        /// <summary>
        /// Returns an error record to use in the case of more than one values are provided for DebugMode property.
        /// </summary>
        /// <returns></returns>
        public static ErrorRecord DebugModeShouldHaveOneValue()
        {
            PSInvalidOperationException e = PSTraceSource.NewInvalidOperationException(
                ParserStrings.DebugModeShouldHaveOneValue);

            e.SetErrorId("DebugModeShouldHaveOneValue");
            return e.ErrorRecord;
        }

        /// <summary>
        /// Return an error to indicate a value is out of range for a dynamic keyword property.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="name"></param>
        /// <param name="providedValue"></param>
        /// <param name="lower"></param>
        /// <param name="upper"></param>
        /// <returns></returns>
        public static ErrorRecord ValueNotInRangeErrorRecord(string property, string name, int providedValue, int lower, int upper)
        {
            PSInvalidOperationException e = PSTraceSource.NewInvalidOperationException(
                ParserStrings.ValueNotInRange, property, name, providedValue, lower, upper);

            e.SetErrorId("ValueNotInRange");
            return e.ErrorRecord;
        }

        /// <summary>
        /// Returns an error record to use when composite resource and its resource instances both has PsDscRunAsCredentials value.
        /// </summary>
        /// <param name="resourceId">ResourceId of resource.</param>
        /// <returns></returns>
        public static ErrorRecord PsDscRunAsCredentialMergeErrorForCompositeResources(string resourceId)
        {
            PSInvalidOperationException e = PSTraceSource.NewInvalidOperationException(
                 ParserStrings.PsDscRunAsCredentialMergeErrorForCompositeResources, resourceId);

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
        /// <param name="keyword"></param>
        /// <returns></returns>
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
                foreach (var prop in keyword.Properties.OrderBy(static ob => ob.Key))
                {
                    if (string.Equals(prop.Key, "ResourceId", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var propVal = prop.Value;
                    if (listKeyProperties && propVal.IsKey || !listKeyProperties && !propVal.IsKey)
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
        /// <param name="prop"></param>
        /// <param name="isOptionalProperty"></param>
        /// <returns></returns>
        private static StringBuilder FormatCimPropertyType(DynamicKeywordProperty prop, bool isOptionalProperty)
        {
            string cimTypeName = prop.TypeConstraint;
            StringBuilder formattedTypeString = new();

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
                formattedTypeString.Append(" { " + string.Join(" | ", prop.ValueMap.Keys.Order()) + " }");
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
                Write-Error -ErrorRecord ([Microsoft.PowerShell.DesiredStateConfiguration.Internal.DscClassCache]::GetBadlyFormedRequiredResourceIdErrorRecord($DependsOnVar, $resourceId))
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
            Write-Error -ErrorRecord ([Microsoft.PowerShell.DesiredStateConfiguration.Internal.DscClassCache]::DuplicateResourceIdInNodeStatementErrorRecord($resourceId, (Get-PSCurrentConfigurationNode)))
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
            Write-Error -ErrorRecord ([Microsoft.PowerShell.DesiredStateConfiguration.Internal.DscClassCache]::PsDscRunAsCredentialMergeErrorForCompositeResources($resourceId))
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
                        Write-Error -ErrorRecord ([Microsoft.PowerShell.DesiredStateConfiguration.Internal.DscClassCache]::GetPullModeNeedConfigurationSource($resourceId))
                    }

# Verify that RefreshMode is not Disabled for Partial configuration
                    if($value['RefreshMode'] -eq 'Disabled')
                    {
                        Update-ConfigurationErrorCount
                        Write-Error -ErrorRecord ([Microsoft.PowerShell.DesiredStateConfiguration.Internal.DscClassCache]::DisabledRefreshModeNotValidForPartialConfig($resourceId))
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
                            Write-Error -ErrorRecord ([Microsoft.PowerShell.DesiredStateConfiguration.Internal.DscClassCache]::GetBadlyFormedExclusiveResourceIdErrorRecord($ExclusiveResource, $resourceId))
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
                Write-Error -ErrorRecord ([Microsoft.PowerShell.DesiredStateConfiguration.Internal.DscClassCache]::InvalidLocalConfigurationManagerPropertyErrorRecord($key, ($V1MetaConfigPropertyList -join ', ')))
                Update-ConfigurationErrorCount
            }
# see if there is a list of allowed values for this property (similar to an enum)
            $allowedValues = $keywordData.Properties[$key].Values
# If there is and user-provided value is not in that list, write an error.
            if ($allowedValues)
            {
                if(($null -eq $value[$key]) -and ($allowedValues -notcontains $value[$key]))
                {
                    Write-Error -ErrorRecord ([Microsoft.PowerShell.DesiredStateConfiguration.Internal.DscClassCache]::InvalidValueForPropertyErrorRecord($key, ""$($value[$key])"", $keywordData.Keyword, ($allowedValues -join ', ')))
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
                        Write-Error -ErrorRecord ([Microsoft.PowerShell.DesiredStateConfiguration.Internal.DscClassCache]::UnsupportedValueForPropertyErrorRecord($key, $notAllowedValue, $keywordData.Keyword, ($allowedValues -join ', ')))
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
                    Write-Error -ErrorRecord ([Microsoft.PowerShell.DesiredStateConfiguration.Internal.DscClassCache]::ValueNotInRangeErrorRecord($key, $keywordName, $value[$key],  $keywordData.Properties[$key].Range.Item1,  $keywordData.Properties[$key].Range.Item2))
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
            Write-Error -ErrorRecord ([Microsoft.PowerShell.DesiredStateConfiguration.Internal.DscClassCache]::MissingValueForMandatoryPropertyErrorRecord($keywordData.Keyword, $keywordData.Properties[$key].TypeConstraint, $Key))
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
            Write-Error -ErrorRecord ([Microsoft.PowerShell.DesiredStateConfiguration.Internal.DscClassCache]::DebugModeShouldHaveOneValue())
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
