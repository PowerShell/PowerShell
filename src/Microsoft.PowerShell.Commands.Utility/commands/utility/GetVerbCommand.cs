// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Reflection;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Implementation of the Get Verb Command.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "Verb", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097026")]
    [OutputType(typeof(VerbInfo))]
    public class GetVerbCommand : Cmdlet
    {
        /// <summary>
        /// Optional Verb filter.
        /// </summary>
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, Position = 0)]
        public string[] Verb
        {
            get; set;
        }

        /// <summary>
        /// Optional Group filter.
        /// </summary>
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, Position = 1)]
        [ValidateSet("Common", "Communications", "Data", "Diagnostic", "Lifecycle", "Other", "Security")]
        public string[] Group
        {
            get; set;
        }

        /// <summary>
        /// Returns a list of verbs.
        /// </summary>
        protected override void ProcessRecord()
        {
            Collection<WildcardPattern> matchingVerbs = SessionStateUtilities.CreateWildcardsFromStrings(
                            this.Verb,
                            WildcardOptions.IgnoreCase
                        );

            foreach (Type type in Verbs.FilterTypesByGroup(Group))
            {
                string groupName = Verbs.GetGroupDisplayName(type);

                foreach (FieldInfo field in type.GetFields())
                {
                    if (field.IsLiteral)
                    {
                        if (this.Verb != null)
                        {
                            if (!SessionStateUtilities.MatchesAnyWildcardPattern(field.Name, matchingVerbs, false))
                            {
                                continue;
                            }
                        }

                        VerbInfo verb = new();
                        verb.Verb = field.Name;
                        verb.AliasPrefix = VerbAliasPrefixes.GetVerbAliasPrefix(field.Name);
                        verb.Group = groupName;
                        verb.Description = VerbDescriptions.GetVerbDescription(field.Name);
                        WriteObject(verb);
                    }
                }
            }
        }
    }
}
