// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Management.Automation.Internal;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The implementation of the "get-alias" cmdlet.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "Alias", DefaultParameterSetName = "Default", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096702")]
    [OutputType(typeof(AliasInfo))]
    public class GetAliasCommand : PSCmdlet
    {
        #region Parameters

        /// <summary>
        /// The Name parameter for the command.
        /// </summary>
        [Parameter(ParameterSetName = "Default", Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string[] Name
        {
            get { return _names; }

            set { _names = value ?? new string[] { "*" }; }
        }

        private string[] _names = new string[] { "*" };

        /// <summary>
        /// The Exclude parameter for the command.
        /// </summary>
        [Parameter]
        public string[] Exclude
        {
            get { return _excludes; }

            set { _excludes = value ?? Array.Empty<string>(); }
        }

        private string[] _excludes = Array.Empty<string>();

        /// <summary>
        /// The scope parameter for the command determines
        /// which scope the aliases are retrieved from.
        /// </summary>
        [Parameter]
        [ArgumentCompleter(typeof(ScopeArgumentCompleter))]
        public string Scope { get; set; }

        /// <summary>
        /// Parameter definition to retrieve aliases based on their definitions.
        /// </summary>
        [Parameter(ParameterSetName = "Definition")]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] Definition { get; set; }

        #endregion Parameters

        #region Command code

        /// <summary>
        /// The main processing loop of the command.
        /// </summary>
        protected override void ProcessRecord()
        {
            if (ParameterSetName.Equals("Definition"))
            {
                foreach (string defn in Definition)
                {
                    WriteMatches(defn, "Definition");
                }
            }
            else
            {
                foreach (string aliasName in _names)
                {
                    WriteMatches(aliasName, "Default");
                }
            }
        }
        #endregion Command code

        private void WriteMatches(string value, string parametersetname)
        {
            // First get the alias table (from the proper scope if necessary)
            IDictionary<string, AliasInfo> aliasTable = null;

            // get the command origin
            CommandOrigin origin = MyInvocation.CommandOrigin;
            string displayString = "name";
            if (!string.IsNullOrEmpty(Scope))
            {
                // This can throw PSArgumentException and PSArgumentOutOfRangeException
                // but just let them go as this is terminal for the pipeline and the
                // exceptions are already properly adorned with an ErrorRecord.

                aliasTable = SessionState.Internal.GetAliasTableAtScope(Scope);
            }
            else
            {
                aliasTable = SessionState.Internal.GetAliasTable();
            }

            bool matchfound = false;
            bool ContainsWildcard = WildcardPattern.ContainsWildcardCharacters(value);
            WildcardPattern wcPattern = WildcardPattern.Get(value, WildcardOptions.IgnoreCase);

            // excluding patter for Default paramset.
            Collection<WildcardPattern> excludePatterns =
                      SessionStateUtilities.CreateWildcardsFromStrings(
                          _excludes,
                          WildcardOptions.IgnoreCase);

            List<AliasInfo> results = new();
            foreach (KeyValuePair<string, AliasInfo> tableEntry in aliasTable)
            {
                if (parametersetname.Equals("Definition", StringComparison.OrdinalIgnoreCase))
                {
                    displayString = "definition";
                    if (!wcPattern.IsMatch(tableEntry.Value.Definition))
                    {
                        continue;
                    }

                    if (SessionStateUtilities.MatchesAnyWildcardPattern(tableEntry.Value.Definition, excludePatterns, false))
                    {
                        continue;
                    }
                }
                else
                {
                    if (!wcPattern.IsMatch(tableEntry.Key))
                    {
                        continue;
                    }
                    // excludes pattern
                    if (SessionStateUtilities.MatchesAnyWildcardPattern(tableEntry.Key, excludePatterns, false))
                    {
                        continue;
                    }
                }

                if (ContainsWildcard)
                {
                    // Only write the command if it is visible to the requestor
                    if (SessionState.IsVisible(origin, tableEntry.Value))
                    {
                        matchfound = true;
                        results.Add(tableEntry.Value);
                    }
                }
                else
                {
                    // For specifically named elements, generate an error for elements that aren't visible...
                    try
                    {
                        SessionState.ThrowIfNotVisible(origin, tableEntry.Value);
                        results.Add(tableEntry.Value);
                        matchfound = true;
                    }
                    catch (SessionStateException sessionStateException)
                    {
                        WriteError(
                            new ErrorRecord(
                                sessionStateException.ErrorRecord,
                                sessionStateException));
                        // Even though it resulted in an error, a result was found
                        // so we don't want to generate the nothing found error
                        // at the end...
                        matchfound = true;
                        continue;
                    }
                }
            }

            results.Sort(
                static (AliasInfo left, AliasInfo right) => StringComparer.CurrentCultureIgnoreCase.Compare(left.Name, right.Name));
            foreach (AliasInfo alias in results)
            {
                this.WriteObject(alias);
            }

            if (!matchfound && !ContainsWildcard && (excludePatterns == null || excludePatterns.Count == 0))
            {
                // Need to write an error if the user tries to get an alias
                // tat doesn't exist and they are not globbing.

                ItemNotFoundException itemNotFound = new(StringUtil.Format(AliasCommandStrings.NoAliasFound, displayString, value));
                ErrorRecord er = new(itemNotFound, "ItemNotFoundException", ErrorCategory.ObjectNotFound, value);
                WriteError(er);
            }
        }
    }
}
