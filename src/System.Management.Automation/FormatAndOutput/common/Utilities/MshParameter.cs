// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.PowerShell.Commands.Internal.Format
{
    /// <summary>
    /// Supported key names for hash tables defining calculated properties,
    /// across all cmdlets.
    /// </summary>
    internal static class CalculatedPropertyDefinitionKeys
    {
        // Note: 
        //  For each key definition below:
        //    * Define the full key name as *EntryKey, using upper camel casing
        //      (e.g., 'Expression', 'FormatString').
        //      While case is ultimately irrelevant, the help topics use upper
        //      camel casing to document the keys, and the tab-completion tests
        //      expect it too. 
        //    * Define the short alias name as *EntryKeyShort, in all-lowercase
        //      (e.g., 'e')
        //    * BE SURE THAT ALL NAMES ARE UNIQUE at least in a given
        //      cmdlet's set of supported keys, but ideally across all cmdlets.
        // 
        // You must pass both constants for a given key to constructors of HashtableEntryDefinition
        // and its subclasses, via *separate arguments*; e.g.:
        //   new HashtableEntryDefinition(ExpressionEntryKey, new string[] { ExpressionEntryKeyShort }, ...)
        
        // Shared by all cmdlets.
        internal const string ExpressionEntryKey = "Expression"; internal const string ExpressionEntryKeyShort = "e";
        
        // Shared by many cmdlets.
        internal const string LabelEntryKey = "Label"; internal const string LabelEntryKeyShort = "l";
        internal const string NameEntryKey = "Name"; internal const string NameEntryKeyShort = "n";

        // Shared by the Format-* cmdlets (except Format-Hex)
        internal const string FormatStringEntryKey = "FormatString"; internal const string FormatStringEntryKeyShort = "f";

        // For Format-Table and ConvertTo-Html.
        internal const string AlignmentEntryKey = "Alignment"; internal const string AlignmentEntryKeyShort = "a";
        internal const string WidthEntryKey = "Width"; internal const string WidthEntryKeyShort = "w";

        // For Format-Custom.
        internal const string DepthEntryKey = "Depth"; internal const string DepthEntryKeyShort = "d";

        // For Sort-Object.
        internal const string AscendingEntryKey = "Ascending"; internal const string AscendingEntryKeyShort = "asc";
        internal const string DescendingEntryKey = "Descending"; internal const string DescendingEntryKeyShort = "desc";
    }

    /// <summary>
    /// normalized parameter class to be constructed from the command line parameters
    /// using the metadata information provided by an instance of CommandParameterDefinition
    /// it's basically the hash table with the normalized values
    /// </summary>
    internal class MshParameter
    {
        internal Hashtable hash = null;

        internal object GetEntry(string key)
        {
            if (this.hash.ContainsKey(key))
                return this.hash[key];

            return AutomationNull.Value;
        }
    }

    internal class NameEntryDefinition : HashtableEntryDefinition
    {
        // Note: This is basically the same as LabelEntryDefinition (both support 'name' as well as 'label' and their short aliases), 
        // except that the .KeyName property is 'name' here, not 'label'.
        internal NameEntryDefinition()        
            : base(CalculatedPropertyDefinitionKeys.NameEntryKey, new string[] { CalculatedPropertyDefinitionKeys.NameEntryKeyShort, CalculatedPropertyDefinitionKeys.LabelEntryKey, CalculatedPropertyDefinitionKeys.LabelEntryKeyShort }, new Type[] { typeof(string) }, false)
        {
        }
    }

    /// <summary>
    /// Metadata base class for hashtable entry definitions
    /// it contains the key name(s) and the allowable types
    /// it also provides hooks for type expansion.
    /// IMPORTANT: The name and secondaryNames arguments should be passed as
    ///            CalculatedPropertyDefinitionKeys.*EntryKey and 
    ///            CalculatedPropertyDefinitionKeys.*EntryKeyShort constants
    /// </summary>
    internal class HashtableEntryDefinition
    {
        internal HashtableEntryDefinition(string name, IEnumerable<string> secondaryNames, Type[] types, bool mandatory)
            : this(name, types, mandatory)
        {
            SecondaryNames = secondaryNames;
        }

        internal HashtableEntryDefinition(string name, Type[] types, bool mandatory)
        {
            KeyName = name;
            AllowedTypes = types;
            Mandatory = mandatory;
        }

        internal HashtableEntryDefinition(string name, Type[] types)
            : this(name, types, false)
        {
        }

        internal virtual Hashtable CreateHashtableFromSingleType(object val)
        {
            // NOTE: must override for the default type(s) entry
            // this entry will have to expand the object into a hash table
            throw PSTraceSource.NewNotSupportedException();
        }

        internal bool IsKeyMatch(string key, out bool isFullMatch)
        {
            isFullMatch = false;

            // First, look for exact matches.
            if (string.Equals(key, this.KeyName, StringComparison.OrdinalIgnoreCase))
            {
                isFullMatch = true;    
                return true;
            }

            if (this.SecondaryNames != null)
            {
                foreach (string secondaryKey in this.SecondaryNames)
                {
                    if (string.Equals(key, secondaryKey, StringComparison.OrdinalIgnoreCase))
                    {
                        isFullMatch = true;    
                        return true;
                    }
                }
            }

            // If no exact match was found, look for a (potentially ambiguous) prefix match.
            if (CommandParameterDefinition.FindPartialMatch(key, this.KeyName))
            {
                return true;
            }

            if (this.SecondaryNames != null)
            {
                foreach (string secondaryKey in this.SecondaryNames)
                {
                    if (CommandParameterDefinition.FindPartialMatch(key, secondaryKey))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal virtual object Verify(object val,
                                        TerminatingErrorContext invocationContext,
                                        bool originalParameterWasHashTable)
        {
            return null;
        }

        internal virtual object ComputeDefaultValue()
        {
            return AutomationNull.Value;
        }

        internal string KeyName { get; }

        internal Type[] AllowedTypes { get; }

        internal bool Mandatory { get; }

        internal IEnumerable<string> SecondaryNames { get; }
    }

    /// <summary>
    /// metadata abstract base class to contain hash entries definitions
    /// </summary>
    internal abstract class CommandParameterDefinition
    {
        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        internal CommandParameterDefinition()
        {
            SetEntries();
        }

        protected abstract void SetEntries();

        internal virtual MshParameter CreateInstance() { return new MshParameter(); }

        /// <summary>
        /// for a key name, verify it is a legal entry:
        ///     1. it must match (partial match allowed)
        ///     2. it must be unambiguous (if partial match)
        /// If an error condition occurs, an exception will be thrown
        /// </summary>
        /// <param name="keyName">key to verify</param>
        /// <param name="invocationContext">invocation context for error reporting</param>
        /// <returns>matching hash table entry</returns>
        /// <exception cref="ArgumentException"></exception>
        internal HashtableEntryDefinition MatchEntry(string keyName, TerminatingErrorContext invocationContext)
        {
            if (string.IsNullOrEmpty(keyName))
                PSTraceSource.NewArgumentNullException("keyName");

            HashtableEntryDefinition matchingEntry = null;
            bool isFullMatch;
            for (int k = 0; k < this.hashEntries.Count; k++)
            {
                if (this.hashEntries[k].IsKeyMatch(keyName, out isFullMatch))
                {
                    if (matchingEntry != null)
                    {
                        // The specified key is a prefix match for more than 
                        // one supported key: we have an ambiguous key; throw an exception.
                        ProcessAmbiguousKey(invocationContext, keyName, matchingEntry, this.hashEntries[k]);
                    }
                    else
                    {
                        // This is the first (and possibly full and therefore only) match.
                        matchingEntry = this.hashEntries[k];
                        // Unless it was a full match, we keep going for ambiguity check.
                        if (isFullMatch)
                        {
                            break;
                        }
                    }                    
                }
            }

            if (matchingEntry != null)
            {
                // we found an unambiguous match
                return matchingEntry;
            }

            // we did not have a match
            ProcessIllegalKey(invocationContext, keyName);
            return null;
        }

        internal static bool FindPartialMatch(string key, string normalizedKey)
        {
            if (key.Length < normalizedKey.Length)
            {
                // shorter, could be an abbreviation
                if (string.Equals(key, normalizedKey.Substring(0, key.Length), StringComparison.OrdinalIgnoreCase))
                {
                    // found abbreviation
                    return true;
                }
            }

            if (string.Equals(key, normalizedKey, StringComparison.OrdinalIgnoreCase))
            {
                // found full match
                return true;
            }

            return false;
        }

        #region Error Processing

        private static void ProcessAmbiguousKey(TerminatingErrorContext invocationContext,
                                            string keyName,
                                            HashtableEntryDefinition matchingEntry,
                                            HashtableEntryDefinition currentEntry)
        {
            string msg = StringUtil.Format(FormatAndOut_MshParameter.AmbiguousKeyError,
                keyName, matchingEntry.KeyName, currentEntry.KeyName);

            ParameterProcessor.ThrowParameterBindingException(invocationContext, "DictionaryKeyAmbiguous", msg);
        }

        private static void ProcessIllegalKey(TerminatingErrorContext invocationContext,
                                            string keyName)
        {
            string msg = StringUtil.Format(FormatAndOut_MshParameter.IllegalKeyError, keyName);

            ParameterProcessor.ThrowParameterBindingException(invocationContext, "DictionaryKeyIllegal", msg);
        }
        #endregion

        internal List<HashtableEntryDefinition> hashEntries = new List<HashtableEntryDefinition>();
    }

    /// <summary>
    /// engine to process a generic object[] from the command line and
    /// generate a list of MshParameter objects , given the metadata provided by
    /// a class derived from CommandParameterDefinition
    /// </summary>
    internal sealed class ParameterProcessor
    {
        #region tracer
        [TraceSource("ParameterProcessor", "ParameterProcessor")]
        internal static PSTraceSource tracer = PSTraceSource.GetTracer("ParameterProcessor", "ParameterProcessor");
        #endregion tracer

        internal static void ThrowParameterBindingException(TerminatingErrorContext invocationContext,
                                                            string errorId,
                                                            string msg)
        {
            ErrorRecord errorRecord = new ErrorRecord(
                                new NotSupportedException(),
                                errorId,
                                ErrorCategory.InvalidArgument,
                                null);

            errorRecord.ErrorDetails = new ErrorDetails(msg);
            invocationContext.ThrowTerminatingError(errorRecord);
        }

        internal ParameterProcessor(CommandParameterDefinition p)
        {
            _paramDef = p;
        }

        /// <exception cref="ArgumentException"></exception>
        internal List<MshParameter> ProcessParameters(object[] p, TerminatingErrorContext invocationContext)
        {
            if (p == null || p.Length == 0)
                return null;

            List<MshParameter> retVal = new List<MshParameter>();
            MshParameter currParam;

            bool originalParameterWasHashTable = false;
            for (int k = 0; k < p.Length; k++)
            {
                // we always copy into a fresh hash table
                currParam = _paramDef.CreateInstance();
                var actualObject = PSObject.Base(p[k]);
                if (actualObject is IDictionary)
                {
                    originalParameterWasHashTable = true;
                    currParam.hash = VerifyHashTable((IDictionary)actualObject, invocationContext);
                }
                else if ((actualObject != null) && MatchesAllowedTypes(actualObject.GetType(), _paramDef.hashEntries[0].AllowedTypes))
                {
                    // a simple type was specified, build a hash with one entry
                    currParam.hash = _paramDef.hashEntries[0].CreateHashtableFromSingleType(actualObject);
                }
                else
                {
                    // unknown type, error
                    // provide error message (the user did not enter a hash table)
                    ProcessUnknownParameterType(invocationContext, actualObject, _paramDef.hashEntries[0].AllowedTypes);
                }

                // value range validation and post processing on the hash table
                VerifyAndNormalizeParameter(currParam, invocationContext, originalParameterWasHashTable);
                retVal.Add(currParam);
            }

            return retVal;
        }

        private static bool MatchesAllowedTypes(Type t, Type[] allowedTypes)
        {
            for (int k = 0; k < allowedTypes.Length; k++)
            {
                if (allowedTypes[k].IsAssignableFrom(t))
                    return true;
            }

            return false;
        }

        /// <exception cref="ArgumentException"></exception>
        private Hashtable VerifyHashTable(IDictionary hash, TerminatingErrorContext invocationContext)
        {
            // full blown hash, need to:
            // 1. verify names(keys) and expand names if there are partial matches
            // 2. verify value types
            Hashtable retVal = new Hashtable();

            foreach (DictionaryEntry e in hash)
            {
                if (e.Key == null)
                {
                    ProcessNullHashTableKey(invocationContext);
                }

                string currentStringKey = e.Key as string;
                if (currentStringKey == null)
                {
                    ProcessNonStringHashTableKey(invocationContext, e.Key);
                }

                // find a match for the key
                HashtableEntryDefinition def = _paramDef.MatchEntry(currentStringKey, invocationContext);
                if (retVal.Contains(def.KeyName))
                {
                    // duplicate key error
                    ProcessDuplicateHashTableKey(invocationContext, currentStringKey, def.KeyName);
                }

                // now the key is verified, need to check the type
                bool matchType = false;

                if (def.AllowedTypes == null || def.AllowedTypes.Length == 0)
                {
                    // we match on any type, it will be up to the entry to further check
                    matchType = true;
                }
                else
                {
                    for (int t = 0; t < def.AllowedTypes.Length; t++)
                    {
                        if (e.Value == null)
                        {
                            ProcessMissingKeyValue(invocationContext, currentStringKey);
                        }

                        if (def.AllowedTypes[t].IsAssignableFrom(e.Value.GetType()))
                        {
                            matchType = true;
                            break;
                        }
                    }
                }

                if (!matchType)
                {
                    // bad type error
                    ProcessIllegalHashTableKeyValue(invocationContext, currentStringKey, e.Value.GetType(), def.AllowedTypes);
                }
                retVal.Add(def.KeyName, e.Value);
            }

            return retVal;
        }

        /// <exception cref="ArgumentException"></exception>
        private void VerifyAndNormalizeParameter(MshParameter parameter,
                                                    TerminatingErrorContext invocationContext,
                                                    bool originalParameterWasHashTable)
        {
            for (int k = 0; k < _paramDef.hashEntries.Count; k++)
            {
                if (parameter.hash.ContainsKey(_paramDef.hashEntries[k].KeyName))
                {
                    // we have a key, just do some post processing normalization
                    // retrieve the value
                    object val = parameter.hash[_paramDef.hashEntries[k].KeyName];
                    object newVal = _paramDef.hashEntries[k].Verify(val, invocationContext, originalParameterWasHashTable);

                    if (newVal != null)
                    {
                        // if a new value is provided, we need to update the hash entry
                        parameter.hash[_paramDef.hashEntries[k].KeyName] = newVal;
                    }
                }
                else
                {
                    // we do not have the key, we might want to have a default value
                    object defaultValue = _paramDef.hashEntries[k].ComputeDefaultValue();

                    if (defaultValue != AutomationNull.Value)
                    {
                        // we have a default value, add it
                        parameter.hash[_paramDef.hashEntries[k].KeyName] = defaultValue;
                    }
                    else if (_paramDef.hashEntries[k].Mandatory)
                    {
                        // no default value and mandatory: we cannot proceed
                        ProcessMissingMandatoryKey(invocationContext, _paramDef.hashEntries[k].KeyName);
                    }
                }
            }
        }

        #region Error Processing

        private static void ProcessUnknownParameterType(TerminatingErrorContext invocationContext, object actualObject, Type[] allowedTypes)
        {
            string allowedTypesList = CatenateTypeArray(allowedTypes);
            string msg;

            if (actualObject != null)
            {
                msg = StringUtil.Format(FormatAndOut_MshParameter.UnknownParameterTypeError,
                    actualObject.GetType().FullName, allowedTypesList);
            }
            else
            {
                msg = StringUtil.Format(FormatAndOut_MshParameter.NullParameterTypeError,
                    allowedTypesList);
            }
            ParameterProcessor.ThrowParameterBindingException(invocationContext, "DictionaryKeyUnknownType", msg);
        }

        private static void ProcessDuplicateHashTableKey(TerminatingErrorContext invocationContext, string duplicateKey, string existingKey)
        {
            string msg = StringUtil.Format(FormatAndOut_MshParameter.DuplicateKeyError,
                           duplicateKey, existingKey);

            ParameterProcessor.ThrowParameterBindingException(invocationContext, "DictionaryKeyDuplicate", msg);
        }

        private static void ProcessNullHashTableKey(TerminatingErrorContext invocationContext)
        {
            string msg = StringUtil.Format(FormatAndOut_MshParameter.DictionaryKeyNullError);
            ParameterProcessor.ThrowParameterBindingException(invocationContext, "DictionaryKeyNull", msg);
        }

        private static void ProcessNonStringHashTableKey(TerminatingErrorContext invocationContext, object key)
        {
            string msg = StringUtil.Format(FormatAndOut_MshParameter.DictionaryKeyNonStringError, key.GetType().Name);
            ParameterProcessor.ThrowParameterBindingException(invocationContext, "DictionaryKeyNonString", msg);
        }

        private static void ProcessIllegalHashTableKeyValue(TerminatingErrorContext invocationContext, string key, Type actualType, Type[] allowedTypes)
        {
            string msg;
            string errorID;
            if (allowedTypes.Length > 1)
            {
                string legalTypes = CatenateTypeArray(allowedTypes);

                msg = StringUtil.Format(FormatAndOut_MshParameter.IllegalTypeMultiError,
                    key,
                    actualType.FullName,
                    legalTypes
                    );

                errorID = "DictionaryKeyIllegalValue1";
            }
            else
            {
                msg = StringUtil.Format(FormatAndOut_MshParameter.IllegalTypeSingleError,
                    key,
                    actualType.FullName,
                    allowedTypes[0]
                    );

                errorID = "DictionaryKeyIllegalValue2";
            }

            ParameterProcessor.ThrowParameterBindingException(invocationContext, errorID, msg);
        }

        private static void ProcessMissingKeyValue(TerminatingErrorContext invocationContext, string keyName)
        {
            string msg = StringUtil.Format(FormatAndOut_MshParameter.MissingKeyValueError, keyName);
            ParameterProcessor.ThrowParameterBindingException(invocationContext, "DictionaryKeyMissingValue", msg);
        }

        private static void ProcessMissingMandatoryKey(TerminatingErrorContext invocationContext, string keyName)
        {
            string msg = StringUtil.Format(FormatAndOut_MshParameter.MissingKeyMandatoryEntryError, keyName);
            ParameterProcessor.ThrowParameterBindingException(invocationContext, "DictionaryKeyMandatoryEntry", msg);
        }

        #endregion

        #region Utilities

        private static string CatenateTypeArray(Type[] arr)
        {
            string[] strings = new string[arr.Length];
            for (int k = 0; k < arr.Length; k++)
            {
                strings[k] = arr[k].FullName;
            }
            return CatenateStringArray(strings);
        }

        internal static string CatenateStringArray(string[] arr)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            for (int k = 0; k < arr.Length; k++)
            {
                if (k > 0)
                {
                    sb.Append(", ");
                }
                sb.Append(arr[k]);
            }
            sb.Append("}");
            return sb.ToString();
        }

        #endregion

        private CommandParameterDefinition _paramDef = null;
    }
}

