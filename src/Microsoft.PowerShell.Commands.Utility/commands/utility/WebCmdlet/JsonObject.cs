/********************************************************************++
Copyright (c) Microsoft Corporation. All rights reserved.
--********************************************************************/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Management.Automation.Internal;
using System.Reflection;

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
        /// Convert a Json string back to an object of type PSObject.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="error"></param>
        /// <returns>A PSObject.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public static object ConvertFromJson(string input, out ErrorRecord error)
        {
            return ConvertFromJson(input, false, out error);
        }

        /// <summary>
        /// Convert a Json string back to an object of type PSObject or Hashtable depending on parameter <paramref name="returnHashTable"/>.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="returnHashTable"></param>
        /// <param name="error"></param>
        /// <returns>A PSObject or a Hashtable if the <paramref name="returnHashTable"/> parameter is true.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public static object ConvertFromJson(string input, bool returnHashTable, out ErrorRecord error)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }

            error = null;
            object obj = null;
            try
            {
                // JsonConvert.DeserializeObject does not throw an exception when an invalid Json array is passed.
                // This issue is being tracked by https://github.com/JamesNK/Newtonsoft.Json/issues/1321.
                // To work around this, we need to identify when input is a Json array, and then try to parse it via JArray.Parse().

                // If input starts with '[' (ignoring white spaces).
                if ((Regex.Match(input, @"^\s*\[")).Success)
                {
                    // JArray.Parse() will throw a JsonException if the array is invalid.
                    // This will be caught by the catch block below, and then throw an
                    // ArgumentException - this is done to have same behavior as the JavaScriptSerializer.
                    JArray.Parse(input);

                    // Please note that if the Json array is valid, we don't do anything,
                    // we just continue the deserialization.
                }

                obj = JsonConvert.DeserializeObject(input, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.None, MaxDepth = 1024 });

                // JObject is a IDictionary
                var dictionary = obj as JObject;
                if (dictionary != null)
                {
                    if (returnHashTable)
                    {
                        obj = PopulateHashTableFromJDictionary(dictionary, out error);
                    }
                    else
                    {
                        obj = PopulateFromJDictionary(dictionary, out error);
                    }
                }
                else
                {
                    // JArray is a collection
                    var list = obj as JArray;
                    if (list != null)
                    {
                        if (returnHashTable)
                        {
                            obj = PopulateHashTableFromJArray(list, out error);
                        }
                        else
                        {
                            obj = PopulateFromJArray(list, out error);
                        }
                    }
                }
            }
            catch (JsonException je)
            {
                var msg = string.Format(CultureInfo.CurrentCulture, WebCmdletStrings.JsonDeserializationFailed, je.Message);
                // the same as JavaScriptSerializer does
                throw new ArgumentException(msg, je);
            }
            return obj;
        }

        // This function is a clone of PopulateFromDictionary using JObject as an input.
        private static PSObject PopulateFromJDictionary(JObject entries, out ErrorRecord error)
        {
            error = null;
            PSObject result = new PSObject();
            foreach (var entry in entries)
            {
                if (string.IsNullOrEmpty(entry.Key))
                {
                    string errorMsg = string.Format(CultureInfo.InvariantCulture,
                                                    WebCmdletStrings.EmptyKeyInJsonString);
                    error = new ErrorRecord(
                        new InvalidOperationException(errorMsg),
                        "EmptyKeyInJsonString",
                        ErrorCategory.InvalidOperation,
                        null);
                    return null;
                }

                // Case sensitive duplicates should normally not occur since JsonConvert.DeserializeObject
                // does not throw when encountering duplicates and just uses the last entry.
                if (result.Properties.Any(psPropertyInfo => psPropertyInfo.Name.Equals(entry.Key, StringComparison.InvariantCulture)))
                {
                    string errorMsg = string.Format(CultureInfo.InvariantCulture,
                                                    WebCmdletStrings.DuplicateKeysInJsonString, entry.Key);
                    error = new ErrorRecord(
                        new InvalidOperationException(errorMsg),
                        "DuplicateKeysInJsonString",
                        ErrorCategory.InvalidOperation,
                        null);
                    return null;
                }

                // Compare case insensitive to tell the user to use the -AsHashTable option instead.
                // This is because PSObject cannot have keys with different casing.
                PSPropertyInfo property = result.Properties[entry.Key];
                if (property != null)
                {
                    string errorMsg = string.Format(CultureInfo.InvariantCulture,
                                                    WebCmdletStrings.KeysWithDifferentCasingInJsonString, property.Name, entry.Key);
                    error = new ErrorRecord(
                        new InvalidOperationException(errorMsg),
                        "KeysWithDifferentCasingInJsonString",
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

        // This function is a clone of PopulateFromDictionary using JObject as an input.
        private static Hashtable PopulateHashTableFromJDictionary(JObject entries, out ErrorRecord error)
        {
            error = null;
            Hashtable result = new Hashtable();
            foreach (var entry in entries)
            {
                // Case sensitive duplicates should normally not occur since JsonConvert.DeserializeObject
                // does not throw when encountering duplicates and just uses the last entry.
                if (result.ContainsKey(entry.Key))
                {
                    string errorMsg = string.Format(CultureInfo.InvariantCulture,
                                                    WebCmdletStrings.DuplicateKeysInJsonString, entry.Key);
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
                    ICollection<object> listResult = PopulateHashTableFromJArray(list, out error);
                    if (error != null)
                    {
                        return null;
                    }
                    result.Add(entry.Key, listResult);
                }

                // Dictionary
                else if (entry.Value is JObject)
                {
                    JObject dic = entry.Value as JObject;
                    Hashtable dicResult = PopulateHashTableFromJDictionary(dic, out error);
                    if (error != null)
                    {
                        return null;
                    }
                    result.Add(entry.Key, dicResult);
                }

                // Value
                else // (entry.Value is JValue)
                {
                    JValue theValue = entry.Value as JValue;
                    result.Add(entry.Key, theValue.Value);
                }
            }
            return result;
        }

        // This function is a clone of PopulateFromList using JArray as input.
        private static ICollection<object> PopulateHashTableFromJArray(JArray list, out ErrorRecord error)
        {
            error = null;
            List<object> result = new List<object>();

            foreach (var element in list)
            {
                // Array
                if (element is JArray)
                {
                    JArray subList = element as JArray;
                    ICollection<object> listResult = PopulateHashTableFromJArray(subList, out error);
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
                    Hashtable dicResult = PopulateHashTableFromJDictionary(dic, out error);
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
    }
}
