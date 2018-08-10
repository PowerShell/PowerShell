// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Management.Automation;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// JsonObject class.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
    public static class JsonObject
    {
        private class DuplicateMemberHashSet : HashSet<string>
        {
            public DuplicateMemberHashSet(int capacity) : base(capacity, StringComparer.OrdinalIgnoreCase)
            {
            }
        }

        /// <summary>
        /// Convert a Json string back to an object of type PSObject.
        /// </summary>
        /// <param name="input">The json text to convert.</param>
        /// <param name="error">An error record if the conversion failed.</param>
        /// <returns>A PSObject.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public static object ConvertFromJson(string input, out ErrorRecord error)
        {
            return ConvertFromJson(input, returnHashtable: false, out error);
        }

        /// <summary>
        /// Convert a Json string back to an object of type <see cref="System.Management.Automation.PSObject"/> or
        /// <see cref="System.Collections.Hashtable"/> depending on parameter <paramref name="returnHashtable"/>.
        /// </summary>
        /// <param name="input">The json text to convert.</param>
        /// <param name="returnHashtable">True if the result should be returned as a <see cref="System.Collections.Hashtable"/>
        /// instead of a <see cref="System.Management.Automation.PSObject"/>.</param>
        /// <param name="error">An error record if the conversion failed.</param>
        /// <returns>A <see cref="System.Management.Automation.PSObject"/> or a <see cref="System.Collections.Hashtable"/>
        /// if the <paramref name="returnHashtable"/> parameter is true.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public static object ConvertFromJson(string input, bool returnHashtable, out ErrorRecord error)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            error = null;
            try
            {
                // JsonConvert.DeserializeObject does not throw an exception when an invalid Json array is passed.
                // This issue is being tracked by https://github.com/JamesNK/Newtonsoft.Json/issues/1321.
                // To work around this, we need to identify when input is a Json array, and then try to parse it via JArray.Parse().

                // If input starts with '[' (ignoring white spaces).
                if (Regex.Match(input, @"^\s*\[").Success)
                {
                    // JArray.Parse() will throw a JsonException if the array is invalid.
                    // This will be caught by the catch block below, and then throw an
                    // ArgumentException - this is done to have same behavior as the JavaScriptSerializer.
                    JArray.Parse(input);

                    // Please note that if the Json array is valid, we don't do anything,
                    // we just continue the deserialization.
                }

                var obj = JsonConvert.DeserializeObject(
                    input,
                    new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.None,
                        MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
                        MaxDepth = 1024
                    });

                switch (obj)
                {
                    case JObject dictionary:
                        // JObject is a IDictionary
                        return returnHashtable
                                   ? PopulateHashTableFromJDictionary(dictionary, out error)
                                   : PopulateFromJDictionary(dictionary, new DuplicateMemberHashSet(dictionary.Count), out error);
                    case JArray list:
                        return returnHashtable
                                   ? PopulateHashTableFromJArray(list, out error)
                                   : PopulateFromJArray(list, out error);
                    default: return obj;
                }
            }
            catch (JsonException je)
            {
                var msg = string.Format(CultureInfo.CurrentCulture, WebCmdletStrings.JsonDeserializationFailed, je.Message);

                // the same as JavaScriptSerializer does
                throw new ArgumentException(msg, je);
            }
        }

        // This function is a clone of PopulateFromDictionary using JObject as an input.
        private static PSObject PopulateFromJDictionary(JObject entries, DuplicateMemberHashSet memberHashTracker, out ErrorRecord error)
        {
            error = null;
            var result = new PSObject(entries.Count);
            foreach (var entry in entries)
            {
                if (string.IsNullOrEmpty(entry.Key))
                {
                    var errorMsg = string.Format(CultureInfo.CurrentCulture, WebCmdletStrings.EmptyKeyInJsonString);
                    error = new ErrorRecord(
                        new InvalidOperationException(errorMsg),
                        "EmptyKeyInJsonString",
                        ErrorCategory.InvalidOperation,
                        null);
                    return null;
                }

                // Case sensitive duplicates should normally not occur since JsonConvert.DeserializeObject
                // does not throw when encountering duplicates and just uses the last entry.
                if (memberHashTracker.TryGetValue(entry.Key, out var maybePropertyName)
                    && string.Compare(entry.Key, maybePropertyName, StringComparison.CurrentCulture) == 0)
                {
                    var errorMsg = string.Format(CultureInfo.CurrentCulture, WebCmdletStrings.DuplicateKeysInJsonString, entry.Key);
                    error = new ErrorRecord(
                        new InvalidOperationException(errorMsg),
                        "DuplicateKeysInJsonString",
                        ErrorCategory.InvalidOperation,
                        null);
                    return null;
                }

                // Compare case insensitive to tell the user to use the -AsHashTable option instead.
                // This is because PSObject cannot have keys with different casing.
                if (memberHashTracker.TryGetValue(entry.Key, out var propertyName))
                {
                    var errorMsg = string.Format(CultureInfo.CurrentCulture, WebCmdletStrings.KeysWithDifferentCasingInJsonString, propertyName, entry.Key);
                    error = new ErrorRecord(
                        new InvalidOperationException(errorMsg),
                        "KeysWithDifferentCasingInJsonString",
                        ErrorCategory.InvalidOperation,
                        null);
                    return null;
                }

                // Array
                switch (entry.Value)
                {
                    case JArray list:
                    {
                        var listResult = PopulateFromJArray(list, out error);
                        if (error != null)
                        {
                            return null;
                        }

                        result.Properties.Add(new PSNoteProperty(entry.Key, listResult));
                        break;
                    }
                    case JObject dic:
                    {
                        // Dictionary
                        var dicResult = PopulateFromJDictionary(dic, new DuplicateMemberHashSet(dic.Count), out error);
                        if (error != null)
                        {
                            return null;
                        }

                        result.Properties.Add(new PSNoteProperty(entry.Key, dicResult));
                        break;
                    }
                    case JValue value:
                    {
                        result.Properties.Add(new PSNoteProperty(entry.Key, value.Value));
                        break;
                    }
                }

                memberHashTracker.Add(entry.Key);
            }

            return result;
        }

        // This function is a clone of PopulateFromList using JArray as input.
        private static ICollection<object> PopulateFromJArray(JArray list, out ErrorRecord error)
        {
            error = null;
            var result = new object[list.Count];

            for (var index = 0; index < list.Count; index++)
            {
                var element = list[index];
                switch (element)
                {
                    case JArray subList:
                    {
                        // Array
                        var listResult = PopulateFromJArray(subList, out error);
                        if (error != null)
                        {
                            return null;
                        }

                        result[index] = listResult;
                        break;
                    }
                    case JObject dic:
                    {
                        // Dictionary
                        var dicResult = PopulateFromJDictionary(dic, new DuplicateMemberHashSet(dic.Count),  out error);
                        if (error != null)
                        {
                            return null;
                        }

                        result[index] = dicResult;
                        break;
                    }
                    case JValue value:
                    {
                        result[index] = value.Value;
                        break;
                    }
                }
            }

            return result;
        }

        // This function is a clone of PopulateFromDictionary using JObject as an input.
        private static Hashtable PopulateHashTableFromJDictionary(JObject entries, out ErrorRecord error)
        {
            error = null;
            Hashtable result = new Hashtable(entries.Count);
            foreach (var entry in entries)
            {
                // Case sensitive duplicates should normally not occur since JsonConvert.DeserializeObject
                // does not throw when encountering duplicates and just uses the last entry.
                if (result.ContainsKey(entry.Key))
                {
                    string errorMsg = string.Format(CultureInfo.CurrentCulture, WebCmdletStrings.DuplicateKeysInJsonString, entry.Key);
                    error = new ErrorRecord(
                        new InvalidOperationException(errorMsg),
                        "DuplicateKeysInJsonString",
                        ErrorCategory.InvalidOperation,
                        null);
                    return null;
                }

                switch (entry.Value)
                {
                    case JArray list:
                    {
                        // Array
                        var listResult = PopulateHashTableFromJArray(list, out error);
                        if (error != null)
                        {
                            return null;
                        }

                        result.Add(entry.Key, listResult);
                        break;
                    }
                    case JObject dic:
                    {
                        // Dictionary
                        var dicResult = PopulateHashTableFromJDictionary(dic, out error);
                        if (error != null)
                        {
                            return null;
                        }

                        result.Add(entry.Key, dicResult);
                        break;
                    }
                    case JValue value:
                    {
                        result.Add(entry.Key, value.Value);
                        break;
                    }
                }
            }

            return result;
        }

        // This function is a clone of PopulateFromList using JArray as input.
        private static ICollection<object> PopulateHashTableFromJArray(JArray list, out ErrorRecord error)
        {
            error = null;
            var result = new object[list.Count];

            for (var index = 0; index < list.Count; index++)
            {
                var element = list[index];

                switch (element)
                {
                    case JArray array:
                    {
                        // Array
                        var listResult = PopulateHashTableFromJArray(array, out error);
                        if (error != null)
                        {
                            return null;
                        }

                        result[index] = listResult;
                            break;
                    }
                    case JObject dic:
                    {
                        // Dictionary
                        var dicResult = PopulateHashTableFromJDictionary(dic, out error);
                        if (error != null)
                        {
                            return null;
                        }

                        result[index] = dicResult;
                        break;
                    }
                    case JValue value:
                    {
                        result[index] = value.Value;
                        break;
                    }
                }
            }

            return result;
        }
    }
}
