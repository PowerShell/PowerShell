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
            get => _verb;
            set => _verb = value ?? new string[] { "*" };
        }

        private string[] _verb = new string[] { "*" };

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
            Collection<WildcardPattern> verbPattern = SessionStateUtilities.CreateWildcardsFromStrings(
                Verb,
                WildcardOptions.IgnoreCase);

            foreach (Type verbType in Verbs.FilterVerbTypesByGroup(Group))
            {
                string verbGroupName = Verbs.GetVerbGroupDisplayName(verbType);

                foreach (FieldInfo field in Verbs.FilterVerbTypeFieldsByWildCardPattern(verbType, verbPattern))
                {
                    VerbInfo verb = new()
                    {
                        Verb = field.Name,
                        AliasPrefix = VerbAliasPrefixes.GetVerbAliasPrefix(field.Name),
                        Group = verbGroupName,
                        Description = VerbDescriptions.GetVerbDescription(field.Name)
                    };

                    WriteObject(verb);
                }
            }
        }
    }
}
