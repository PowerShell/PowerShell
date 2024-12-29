// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

using Microsoft.Management.Infrastructure;
using Microsoft.Management.Infrastructure.Options;
using Microsoft.PowerShell.Cim;

using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell.Cmdletization.Cim
{
    internal sealed class CimCustomOptionsDictionary
    {
        private readonly IDictionary<string, object> _dict;
        private readonly object _dictModificationLock = new();

        private CimCustomOptionsDictionary(IEnumerable<KeyValuePair<string, object>> wrappedDictionary)
        {
            // no need to lock _dictModificationLock inside the constructor
            _dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in wrappedDictionary)
            {
                _dict[kvp.Key] = kvp.Value;
            }
        }

        private IEnumerable<KeyValuePair<string, object>> GetSnapshot()
        {
            lock (_dictModificationLock)
            {
                return _dict.ToList();
            }
        }

        internal static CimCustomOptionsDictionary Create(IEnumerable<KeyValuePair<string, object>> wrappedDictionary)
        {
            return new CimCustomOptionsDictionary(wrappedDictionary);
        }

        private static readonly ConditionalWeakTable<CimInstance, CimCustomOptionsDictionary> s_cimInstanceToCustomOptions = new();

        internal static void AssociateCimInstanceWithCustomOptions(CimInstance cimInstance, CimCustomOptionsDictionary newCustomOptions)
        {
            if (newCustomOptions == null)
            {
                return;
            }

            lock (newCustomOptions._dictModificationLock)
            {
                if (newCustomOptions._dict.Count == 0)
                {
                    return;
                }
            }

            bool foundAssociatedOptions = true;
            CimCustomOptionsDictionary oldCustomOptions = s_cimInstanceToCustomOptions.GetValue(
                cimInstance,
                delegate
                    {
                        foundAssociatedOptions = false;
                        return newCustomOptions;
                    });

            if (foundAssociatedOptions)
            {
                lock (oldCustomOptions._dictModificationLock)
                {
                    foreach (KeyValuePair<string, object> newCustomOption in newCustomOptions.GetSnapshot())
                    {
                        oldCustomOptions._dict[newCustomOption.Key] = newCustomOption.Value;
                    }
                }
            }
        }

        internal static CimCustomOptionsDictionary MergeOptions(CimCustomOptionsDictionary optionsFromCommandLine, CimInstance instanceRelatedToThisOperation)
        {
            CimCustomOptionsDictionary instanceRelatedOptions;
            if (s_cimInstanceToCustomOptions.TryGetValue(instanceRelatedToThisOperation, out instanceRelatedOptions) && instanceRelatedOptions != null)
            {
                IEnumerable<KeyValuePair<string, object>> instanceRelatedOptionsSnapshot = instanceRelatedOptions.GetSnapshot();
                IEnumerable<KeyValuePair<string, object>> optionsFromCommandLineSnapshot = optionsFromCommandLine.GetSnapshot();
                var mergedOptions = instanceRelatedOptionsSnapshot.Concat(optionsFromCommandLineSnapshot); // note - order matters here
                return new CimCustomOptionsDictionary(mergedOptions);
            }
            else
            {
                return optionsFromCommandLine;
            }
        }

        internal static CimCustomOptionsDictionary MergeOptions(CimCustomOptionsDictionary optionsFromCommandLine, IEnumerable<CimInstance> instancesRelatedToThisOperation)
        {
            CimCustomOptionsDictionary result = optionsFromCommandLine;
            if (instancesRelatedToThisOperation != null)
            {
                foreach (CimInstance instanceRelatedToThisOperation in instancesRelatedToThisOperation)
                {
                    result = MergeOptions(result, instanceRelatedToThisOperation);
                }
            }

            return result;
        }

        internal void Apply(CimOperationOptions cimOperationOptions, CimSensitiveValueConverter cimSensitiveValueConverter)
        {
            CimOperationOptionsHelper.SetCustomOptions(cimOperationOptions, this.GetSnapshot(), cimSensitiveValueConverter);
        }
    }

    /// <summary>
    /// CimQuery supports building of queries against CIM object model.
    /// </summary>
    internal static class CimOperationOptionsHelper
    {
        internal static void SetCustomOptions(
            CimOperationOptions operationOptions,
            IEnumerable<KeyValuePair<string, object>> customOptions,
            CimSensitiveValueConverter cimSensitiveValueConverter)
        {
            if (customOptions != null)
            {
                foreach (KeyValuePair<string, object> queryOption in customOptions)
                {
                    SetCustomOption(operationOptions, queryOption.Key, queryOption.Value, cimSensitiveValueConverter);
                }
            }
        }

        internal static void SetCustomOption(
            CimOperationOptions operationOptions,
            string optionName,
            object optionValue,
            CimSensitiveValueConverter cimSensitiveValueConverter)
        {
            Dbg.Assert(!string.IsNullOrWhiteSpace(optionName), "Caller should verify optionName != null");

            if (optionValue == null)
            {
                return;
            }

            object cimValue = cimSensitiveValueConverter.ConvertFromDotNetToCim(optionValue);
            CimType cimType = CimConverter.GetCimType(CimSensitiveValueConverter.GetCimType(optionValue.GetType()));

            operationOptions.SetCustomOption(optionName, cimValue, cimType, mustComply: false);
        }
    }
}
