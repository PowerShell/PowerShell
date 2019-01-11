// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Dbg = System.Management.Automation;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Provider;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// This provider is the data accessor for environment variables. It uses
    /// the SessionStateProviderBase as the base class to produce a view on
    /// session state data.
    /// </summary>
    [CmdletProvider(EnvironmentProvider.ProviderName, ProviderCapabilities.ShouldProcess)]
    public sealed class EnvironmentProvider : SessionStateProviderBase
    {
        /// <summary>
        /// Gets the name of the provider.
        /// </summary>
        public const string ProviderName = "Environment";

        #region Constructor

        /// <summary>
        /// The constructor for the provider that exposes environment variables to the user
        /// as drives.
        /// </summary>
        public EnvironmentProvider()
        {
        }

        #endregion Constructor

        #region DriveCmdletProvider overrides

        /// <summary>
        /// Initializes the alias drive.
        /// </summary>
        /// <returns>
        /// An array of a single PSDriveInfo object representing the alias drive.
        /// </returns>
        protected override Collection<PSDriveInfo> InitializeDefaultDrives()
        {
            string description = SessionStateStrings.EnvironmentDriveDescription;

            PSDriveInfo envDrive =
                new PSDriveInfo(
                    DriveNames.EnvironmentDrive,
                    ProviderInfo,
                    string.Empty,
                    description,
                    null);

            Collection<PSDriveInfo> drives = new Collection<PSDriveInfo>();
            drives.Add(envDrive);
            return drives;
        }

        #endregion DriveCmdletProvider overrides

        #region protected members

        /// <summary>
        /// Gets a environment variable from session state.
        /// </summary>
        /// <param name="name">
        /// The name of the environment variable to retrieve.
        /// </param>
        /// <returns>
        /// A DictionaryEntry that represents the value of the environment variable.
        /// </returns>
        internal override object GetSessionStateItem(string name)
        {
            Dbg.Diagnostics.Assert(
                !string.IsNullOrEmpty(name),
                "The caller should verify this parameter");

            object result = null;

            string value = Environment.GetEnvironmentVariable(name);

            if (value != null)
            {
                result = new DictionaryEntry(name, value);
            }

            return result;
        }

        /// <summary>
        /// Sets the environment variable of the specified name to the specified value.
        /// </summary>
        /// <param name="name">
        /// The name of the environment variable to set.
        /// </param>
        /// <param name="value">
        /// The new value for the environment variable.
        /// </param>
        /// <param name="writeItem">
        /// If true, the item that was set should be written to WriteItemObject.
        /// </param>
        internal override void SetSessionStateItem(string name, object value, bool writeItem)
        {
            Dbg.Diagnostics.Assert(
                !string.IsNullOrEmpty(name),
                "The caller should verify this parameter");

            if (value == null)
            {
                Environment.SetEnvironmentVariable(name, null);
            }
            else
            {
                // First see if we got a DictionaryEntry which represents
                // an item for this provider. If so, use the value from
                // the dictionary entry.

                if (value is DictionaryEntry)
                {
                    value = ((DictionaryEntry)value).Value;
                }

                string stringValue = value as string;
                if (stringValue == null)
                {
                    // try using ETS to convert to a string.

                    PSObject wrappedObject = PSObject.AsPSObject(value);
                    stringValue = wrappedObject.ToString();
                }

                Environment.SetEnvironmentVariable(name, stringValue);

                DictionaryEntry item = new DictionaryEntry(name, stringValue);

                if (writeItem)
                {
                    WriteItemObject(item, name, false);
                }
            }
        }

        /// <summary>
        /// Removes the specified environment variable from session state.
        /// </summary>
        /// <param name="name">
        /// The name of the environment variable to remove from session state.
        /// </param>
        internal override void RemoveSessionStateItem(string name)
        {
            Dbg.Diagnostics.Assert(
                !string.IsNullOrEmpty(name),
                "The caller should verify this parameter");

            Environment.SetEnvironmentVariable(name, null);
        }

        /// <summary>
        /// Gets a flattened view of the environment variables in session state.
        /// </summary>
        /// <returns>
        /// An IDictionary representing the flattened view of the environment variables in
        /// session state.
        /// </returns>
        internal override IDictionary GetSessionStateTable()
        {
            // Environment variables are case-sensitive on Unix and
            // case-insensitive on Windows
#if UNIX
            Dictionary<string, DictionaryEntry> providerTable =
                new Dictionary<string, DictionaryEntry>(StringComparer.Ordinal);
#else
            Dictionary<string, DictionaryEntry> providerTable =
                new Dictionary<string, DictionaryEntry>(StringComparer.OrdinalIgnoreCase);
#endif

            // The environment variables returns a dictionary of keys and values that are
            // both strings. We want to return a dictionary with the key as a string and
            // the value as the DictionaryEntry containing both the name and env variable
            // value.

            IDictionary environmentTable = Environment.GetEnvironmentVariables();
            foreach (DictionaryEntry entry in environmentTable)
            {
                if (!providerTable.TryAdd((string)entry.Key, entry))
                {   // Windows only: duplicate key (variable name that differs only in case)
                    // NOTE: Even though this shouldn't happen, it can, e.g. when npm
                    //       creates duplicate environment variables that differ only in case -
                    //       see https://github.com/PowerShell/PowerShell/issues/6305.
                    //       However, because retrieval *by name* later is invariably
                    //       case-INsensitive, in effect only a *single* variable exists.
                    //       We simply ask Environment.GetEnvironmentVariable() for the effective value
                    //       and use that as the only entry, because for a given key 'foo' (and all its case variations),
                    //       that is guaranteed to match what $env:FOO and [environment]::GetEnvironmentVariable('foo') return.
                    //       (If, by contrast, we just used `entry` as-is every time a duplicate is encountered,
                    //        it could - intermittently - represent a value *other* than the effective one.)
                    string effectiveValue = Environment.GetEnvironmentVariable((string)entry.Key);
                    if (((string)entry.Value).Equals(effectiveValue, StringComparison.Ordinal)) { // We've found the effective definition.
                        // Note: We *recreate* the entry so that the specific name casing of the
                        //       effective definition is also reflected. However, if the case variants
                        //       define the same value, it is unspecified which name variant is reflected
                        //       in Get-Item env: output; given the always case-insensitive nature of the retrieval,
                        //       that shouldn't matter.
                        providerTable.Remove((string)entry.Key);
                        providerTable.Add((string)entry.Key, entry);
                    }
                }
            }

            return providerTable;
        }

        /// <summary>
        /// Gets the Value property of the DictionaryEntry item.
        /// </summary>
        /// <param name="item">
        /// The item to get the value from.
        /// </param>
        /// <returns>
        /// The value of the item.
        /// </returns>
        internal override object GetValueOfItem(object item)
        {
            Dbg.Diagnostics.Assert(
                item != null,
                "Caller should verify the item parameter");

            object value = item;

            if (item is DictionaryEntry)
            {
                value = ((DictionaryEntry)item).Value;
            }

            return value;
        }

        #endregion protected members
    }
}

