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
                string script = "param($targetType,$moduleName) & (Microsoft.PowerShell.Core\\Get-Module $moduleName) { New-Object $targetType } ";

                powerShell.AddScript(script);
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
                    if (memberInfo == null || memberInfo.Length > 1 || !(memberInfo[0] is PropertyInfo || memberInfo[0] is FieldInfo))
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
                        case Microsoft.Management.Infrastructure.CimType.Instance:
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
                        case Microsoft.Management.Infrastructure.CimType.InstanceArray:
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

            // Extract the password into a SecureString.
            var password = new SecureString();
            foreach (char t in plainPassWord)
            {
                password.AppendChar(t);
            }

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
        private CimMofDeserializer _deserializer;
        private CimMofDeserializer.OnClassNeeded _onClassNeeded;
        /// <summary>
        /// </summary>
        internal CimDSCParser(CimMofDeserializer.OnClassNeeded onClassNeeded)
        {
            _deserializer = CimMofDeserializer.Create();
            _onClassNeeded = onClassNeeded;

            //TODO-AM: this is for debugging:
            _deserializer.SchemaValidationOption = MofDeserializerSchemaValidationOption.Ignore;
        }

        /// <summary>
        /// </summary>
        internal CimDSCParser(CimMofDeserializer.OnClassNeeded onClassNeeded, Microsoft.Management.Infrastructure.Serialization.MofDeserializerSchemaValidationOption validationOptions)
        {
            _deserializer = CimMofDeserializer.Create();
            //_deserializer.SchemaValidationOption = validationOptions;
            //TODO-AM: this is for debugging:
            _deserializer.SchemaValidationOption = MofDeserializerSchemaValidationOption.Ignore;
            
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
        /// Make sure that the instance conforms to the the schema.
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
