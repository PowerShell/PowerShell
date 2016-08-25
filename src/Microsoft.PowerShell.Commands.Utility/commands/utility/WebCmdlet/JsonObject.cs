/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
#if CORECLR
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.IO;
using System.Management.Automation.Internal;
using System.Reflection;
#else
using System.Web.Script.Serialization;
using System.Collections.Specialized;
#endif

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// JsonObject class
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
    public static class JsonObject
    {
        private const int maxDepthAllowed = 100;

        /// <summary>
        /// Convert a Json string back to an object
        /// </summary>
        /// <param name="input"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public static object ConvertFromJson(string input, out ErrorRecord error)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }
            error = null;
#if CORECLR
            object obj = JsonConvert.DeserializeObject(input, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.None, MaxDepth = 1024 });

            // JObject is a IDictionary
            if (obj is JObject)
            {
                var dictionary = obj as JObject;
                obj = PopulateFromJDictionary(dictionary, out error);
            }

            // JArray is a collection
            else if (obj is JArray)
            {
                var list = obj as JArray;
                obj = PopulateFromJArray(list, out error);
            }
#else
            //In ConvertTo-Json, to serialize an object with a given depth, we set the RecursionLimit to depth + 2, see JavaScriptSerializer constructor in ConvertToJsonCommand.cs.
            // Setting RecursionLimit to depth + 2 in order to support '$object | ConvertTo-Json –depth <value less than or equal to 100> | ConvertFrom-Json'.
            JavaScriptSerializer serializer = new JavaScriptSerializer(new JsonObjectTypeResolver()) { RecursionLimit = (maxDepthAllowed + 2) };
            serializer.MaxJsonLength = Int32.MaxValue;
            object obj = serializer.DeserializeObject(input);

            if (obj is IDictionary<string, object>)
            {
                var dictionary = obj as IDictionary<string, object>;
                obj = PopulateFromDictionary(dictionary, out error);
            }
            else if (obj is ICollection<object>)
            {
                var list = obj as ICollection<object>;
                obj = PopulateFromList(list, out error);
            }
#endif
            return obj;
        }

#if CORECLR
        // This function is a clone of PopulateFromDictionary using JObject as an input.
        private static PSObject PopulateFromJDictionary(JObject entries, out ErrorRecord error)
        {
            error = null;
            PSObject result = new PSObject();
            foreach (var entry in entries)
            {
                PSPropertyInfo property = result.Properties[entry.Key];
                if (property != null)
                {
                    string errorMsg = string.Format(CultureInfo.InvariantCulture,
                                                    WebCmdletStrings.DuplicateKeysInJsonString, property.Name, entry.Key);
                    error = new ErrorRecord(
                        new InvalidOperationException(errorMsg),
                        "DuplicateKeysInJsonString",
                        ErrorCategory.InvalidOperation,
                        null);
                    return null;
                }

                // Array
                else if (entry.Value is JArray)
                {
                    JArray list = entry.Value as JArray;
                    ICollection<object> listResult = PopulateFromJArray(list, out error);
                    if (error != null)
                    {
                        return null;
                    }
                    result.Properties.Add(new PSNoteProperty(entry.Key, listResult));
                }

                // Dictionary
                else if (entry.Value is JObject)
                {
                    JObject dic = entry.Value as JObject;
                    PSObject dicResult = PopulateFromJDictionary(dic, out error);
                    if (error != null)
                    {
                        return null;
                    }
                    result.Properties.Add(new PSNoteProperty(entry.Key, dicResult));
                }

                // Value
                else // (entry.Value is JValue)
                {
                    JValue theValue = entry.Value as JValue;
                    result.Properties.Add(new PSNoteProperty(entry.Key, theValue.Value));
                }
            }
            return result;
        }

        // This function is a clone of PopulateFromList using JArray as input.
        private static ICollection<object> PopulateFromJArray(JArray list, out ErrorRecord error)
        {
            error = null;
            List<object> result = new List<object>();

            foreach (var element in list)
            {
                // Array
                if (element is JArray)
                {
                    JArray subList = element as JArray;
                    ICollection<object> listResult = PopulateFromJArray(subList, out error);
                    if (error != null)
                    {
                        return null;
                    }
                    result.Add(listResult);
                }

                // Dictionary
                else if (element is JObject)
                {
                    JObject dic = element as JObject;
                    PSObject dicResult = PopulateFromJDictionary(dic, out error);
                    if (error != null)
                    {
                        return null;
                    }
                    result.Add(dicResult);
                }

                // Value
                else // (element is JValue)
                {
                    result.Add(((JValue)element).Value);
                }
            }
            return result.ToArray();
        }

        /// <summary>
        /// Loads the Json.Net module to the given cmdlet execution context.
        /// </summary>
        /// <param name="cmdlet"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public static void ImportJsonDotNetModule(Cmdlet cmdlet)
        {
            const string jsonDotNetAssemblyName = "Newtonsoft.Json, Version=7.0.0.0";

            // Check if the Newtonsoft.Json.dll assembly is loaded.
            try
            {
                System.Reflection.Assembly.Load(new AssemblyName(jsonDotNetAssemblyName));
            }
            catch (System.IO.FileNotFoundException)
            {
                // It is not, try to load it.
                // Make sure that PSModuleAutoLoadingPreference is not set to 'None'.
                PSModuleAutoLoadingPreference moduleAutoLoadingPreference =
                            CommandDiscovery.GetCommandDiscoveryPreference(cmdlet.Context, SpecialVariables.PSModuleAutoLoadingPreferenceVarPath, "PSModuleAutoLoadingPreference");
                if (moduleAutoLoadingPreference == PSModuleAutoLoadingPreference.None)
                {
                    cmdlet.ThrowTerminatingError(new ErrorRecord(
                                        new NotSupportedException(WebCmdletStrings.PSModuleAutoloadingPreferenceNotEnable),
                                        "PSModuleAutoloadingPreferenceNotEnable",
                                        ErrorCategory.NotEnabled,
                                        null));
                }

                // Use module auto-loading to import Json.Net.
                var jsonNetModulePath = Path.Combine(System.Environment.GetEnvironmentVariable("ProgramFiles"), @"WindowsPowerShell\Modules\Json.Net");
                CmdletInfo cmdletInfo = cmdlet.Context.SessionState.InvokeCommand.GetCmdlet("Microsoft.PowerShell.Core\\Import-Module");
                Exception exception;
                Collection<PSModuleInfo> importedModule = CommandDiscovery.AutoloadSpecifiedModule(jsonNetModulePath, cmdlet.Context, cmdletInfo.Visibility, out exception);

                if ((importedModule == null) || (importedModule.Count == 0))
                {
                    string errorMessage = StringUtil.Format(WebCmdletStrings.JsonNetModuleRequired, WebCmdletStrings.CouldNotAutoImportJsonNetModule);

                    cmdlet.ThrowTerminatingError(new ErrorRecord(
                                        new NotSupportedException(errorMessage, exception),
                                        "CouldNotAutoImportJsonNetModule",
                                        ErrorCategory.InvalidOperation,
                                        null));
                }

                // Finally, ensure that the Newtonsoft.Json.dll assembly was loaded.
                try
                {
                    System.Reflection.Assembly.Load(new AssemblyName(jsonDotNetAssemblyName));
                }
                catch (System.IO.FileNotFoundException)
                {
                    string errorMessage = StringUtil.Format(
                                                WebCmdletStrings.JsonNetModuleRequired,
                                                StringUtil.Format(WebCmdletStrings.JsonNetModuleFilesRequired, jsonNetModulePath));

                    cmdlet.ThrowTerminatingError(new ErrorRecord(
                                                    new NotSupportedException(errorMessage),
                                                    "JsonNetModuleRequired",
                                                    ErrorCategory.NotInstalled,
                                                    null));
                }
            }
        }
#else
        private static ICollection<object> PopulateFromList(ICollection<object> list, out ErrorRecord error)
        {
            error = null;
            List<object> result = new List<object>();

            foreach (object element in list)
            {
                if (element is IDictionary<string, object>)
                {
                    IDictionary<string, object> dic = element as IDictionary<string, object>;
                    PSObject dicResult = PopulateFromDictionary(dic, out error);
                    if (error != null)
                    {
                        return null;
                    }
                    result.Add(dicResult);
                }
                else if (element is ICollection<object>)
                {
                    ICollection<object> subList = element as ICollection<object>;
                    ICollection<object> listResult = PopulateFromList(subList, out error);
                    if (error != null)
                    {
                        return null;
                    }
                    result.Add(listResult);
                }
                else
                {
                    result.Add(element);
                }
            }

            return result.ToArray();
        }

        private static PSObject PopulateFromDictionary(IDictionary<string, object> entries, out ErrorRecord error)
        {
            error = null;
            PSObject result = new PSObject();
            foreach (KeyValuePair<string, object> entry in entries)
            {
                PSPropertyInfo property = result.Properties[entry.Key];
                if (property != null)
                {
                    string errorMsg = string.Format(CultureInfo.InvariantCulture,
                                                    WebCmdletStrings.DuplicateKeysInJsonString, property.Name, entry.Key);
                    error = new ErrorRecord(
                        new InvalidOperationException(errorMsg),
                        "DuplicateKeysInJsonString",
                        ErrorCategory.InvalidOperation,
                        null);
                    return null;
                }

                if (entry.Value is IDictionary<string, object>)
                {
                    IDictionary<string, object> subEntries = entry.Value as IDictionary<string, object>;
                    PSObject dicResult = PopulateFromDictionary(subEntries, out error);
                    if (error != null)
                    {
                        return null;
                    }
                    result.Properties.Add(new PSNoteProperty(entry.Key, dicResult));
                }
                else if (entry.Value is ICollection<object>)
                {
                    ICollection<object> list = entry.Value as ICollection<object>;
                    ICollection<object> listResult = PopulateFromList(list, out error);
                    if (error != null)
                    {
                        return null;
                    }
                    result.Properties.Add(new PSNoteProperty(entry.Key, listResult));
                }
                else
                {
                    result.Properties.Add(new PSNoteProperty(entry.Key, entry.Value));
                }
            }

            return result;
        }
#endif
    }
}