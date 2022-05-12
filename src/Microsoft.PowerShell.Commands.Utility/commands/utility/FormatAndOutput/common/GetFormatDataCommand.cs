// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Management.Automation.Remoting;
using System.Management.Automation.Runspaces;

using Microsoft.PowerShell.Commands.Internal.Format;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Gets formatting information from the loading format information database.
    /// </summary>
    /// <remarks>Currently supports only table controls
    /// </remarks>
    [Cmdlet(VerbsCommon.Get, "FormatData", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096614")]
    [OutputType(typeof(System.Management.Automation.ExtendedTypeDefinition))]
    public class GetFormatDataCommand : PSCmdlet
    {
        private string[] _typename;
        private WildcardPattern[] _filter = new WildcardPattern[1];

        /// <summary>
        /// Get Formatting information only for the specified typename.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        [ValidateNotNullOrEmpty]
        [Parameter(Position = 0)]
        public string[] TypeName
        {
            get
            {
                return _typename;
            }

            set
            {
                _typename = value;

                if (_typename == null)
                {
                    _filter = Array.Empty<WildcardPattern>();
                }
                else
                {
                    _filter = new WildcardPattern[_typename.Length];
                    for (int i = 0; i < _filter.Length; i++)
                    {
                        _filter[i] = WildcardPattern.Get(_typename[i],
                            WildcardOptions.Compiled | WildcardOptions.CultureInvariant | WildcardOptions.IgnoreCase);
                    }
                }
            }
        }

        /// <summary>
        /// When specified, helps control whether or not to send richer formatting data
        /// that was not supported by earlier versions of PowerShell.
        /// </summary>
        [Parameter]
        public Version PowerShellVersion { get; set; }

        /// <summary>
        /// Set the default filter.
        /// </summary>
        protected override void BeginProcessing()
        {
            if (_filter[0] == null)
            {
                _filter[0] = WildcardPattern.Get("*", WildcardOptions.None);
            }
        }

        private static Dictionary<string, List<string>> GetTypeGroupMap(IEnumerable<TypeGroupDefinition> groupDefinitions)
        {
            var typeGroupMap = new Dictionary<string, List<string>>();
            foreach (TypeGroupDefinition typeGroup in groupDefinitions)
            {
                // The format system actually allows you to define multiple SelectionSets with the same name, but only the
                // first type group will take effect. So we skip the rest groups that have the same name.
                if (!typeGroupMap.ContainsKey(typeGroup.name))
                {
                    var typesInGroup = typeGroup.typeReferenceList.ConvertAll(static typeReference => typeReference.name);
                    typeGroupMap.Add(typeGroup.name, typesInGroup);
                }
            }

            return typeGroupMap;
        }

        /// <summary>
        /// Takes out the content from the database and writes them out.
        /// </summary>
        protected override void ProcessRecord()
        {
            // Remoting detection:
            //   * Automatic variable $PSSenderInfo is defined in true remoting contexts as well as in background jobs.
            //   * $PSSenderInfo.ApplicationArguments.PSVersionTable.PSVersion contains the client version, as a [version] instance.
            //      Note: Even though $PSVersionTable.PSVersion is of type [semver] in PowerShell 6+, it is of type [version] here,
            //            presumably because only the latter type deserializes type-faithfully.
            var clientVersion = PowerShellVersion;
            PSSenderInfo remotingClientInfo = GetVariableValue("PSSenderInfo") as PSSenderInfo;
            if (clientVersion == null && remotingClientInfo != null)
            {
                clientVersion = PSObject.Base((PSObject.Base(remotingClientInfo.ApplicationArguments["PSVersionTable"]) as PSPrimitiveDictionary)?["PSVersion"]) as Version;
            }

            // During remoting, remain compatible with v5.0- clients by default.
            // Passing a -PowerShellVersion argument allows overriding the client version.
            bool writeOldWay =
                (remotingClientInfo != null && clientVersion == null)  // To be safe: Remoting client version could unexpectedly not be determined.
                ||
                (clientVersion != null
                    &&
                    (clientVersion.Major < 5
                        ||
                    (clientVersion.Major == 5 && clientVersion.Minor < 1)));

            TypeInfoDataBase db = this.Context.FormatDBManager.Database;

            List<ViewDefinition> viewdefinitions = db.viewDefinitionsSection.viewDefinitionList;
            Dictionary<string, List<string>> typeGroupMap = GetTypeGroupMap(db.typeGroupSection.typeGroupDefinitionList);

            var typedefs = new Dictionary<ConsolidatedString, List<FormatViewDefinition>>(ConsolidatedString.EqualityComparer);

            foreach (ViewDefinition definition in viewdefinitions)
            {
                if (definition.isHelpFormatter)
                    continue;

                var consolidatedTypeName = CreateConsolidatedTypeName(definition, typeGroupMap);

                if (!ShouldGenerateView(consolidatedTypeName))
                    continue;

                PSControl control;

                var tableControlBody = definition.mainControl as TableControlBody;
                if (tableControlBody != null)
                {
                    control = new TableControl(tableControlBody, definition);
                }
                else
                {
                    var listControlBody = definition.mainControl as ListControlBody;
                    if (listControlBody != null)
                    {
                        control = new ListControl(listControlBody, definition);
                    }
                    else
                    {
                        var wideControlBody = definition.mainControl as WideControlBody;
                        if (wideControlBody != null)
                        {
                            control = new WideControl(wideControlBody, definition);
                            if (writeOldWay)
                            {
                                // Alignment was added to WideControl in V2, but wasn't
                                // used.  It was removed in V5, but old PowerShell clients
                                // expect this property or fail to rehydrate the remote object.
                                var psobj = new PSObject(control);
                                psobj.Properties.Add(new PSNoteProperty("Alignment", 0));
                            }
                        }
                        else
                        {
                            var complexControlBody = (ComplexControlBody)definition.mainControl;
                            control = new CustomControl(complexControlBody, definition);
                        }
                    }
                }

                // Older version of PowerShell do not know about something in the control, so
                // don't return it.
                if (writeOldWay && !control.CompatibleWithOldPowerShell())
                    continue;

                var formatdef = new FormatViewDefinition(definition.name, control, definition.InstanceId);

                List<FormatViewDefinition> viewList;
                if (!typedefs.TryGetValue(consolidatedTypeName, out viewList))
                {
                    viewList = new List<FormatViewDefinition>();
                    typedefs.Add(consolidatedTypeName, viewList);
                }

                viewList.Add(formatdef);
            }

            // write out all the available type definitions
            foreach (var pair in typedefs)
            {
                var typeNames = pair.Key;

                if (writeOldWay)
                {
                    foreach (var typeName in typeNames)
                    {
                        var etd = new ExtendedTypeDefinition(typeName, pair.Value);
                        WriteObject(etd);
                    }
                }
                else
                {
                    var etd = new ExtendedTypeDefinition(typeNames[0], pair.Value);
                    for (int i = 1; i < typeNames.Count; i++)
                    {
                        etd.TypeNames.Add(typeNames[i]);
                    }

                    WriteObject(etd);
                }
            }
        }

        private static ConsolidatedString CreateConsolidatedTypeName(ViewDefinition definition, Dictionary<string, List<string>> typeGroupMap)
        {
            // Create our "consolidated string" typename which is used as a dictionary key
            var reflist = definition.appliesTo.referenceList;
            var consolidatedTypeName = new ConsolidatedString(ConsolidatedString.Empty);

            foreach (TypeOrGroupReference item in reflist)
            {
                // If it's a TypeGroup, we need to look that up and add it's members
                if (item is TypeGroupReference)
                {
                    List<string> typesInGroup;
                    if (typeGroupMap.TryGetValue(item.name, out typesInGroup))
                    {
                        foreach (string typeName in typesInGroup)
                        {
                            consolidatedTypeName.Add(typeName);
                        }
                    }
                }
                else
                {
                    consolidatedTypeName.Add(item.name);
                }
            }

            return consolidatedTypeName;
        }

        private bool ShouldGenerateView(ConsolidatedString consolidatedTypeName)
        {
            foreach (WildcardPattern pattern in _filter)
            {
                foreach (var typeName in consolidatedTypeName)
                {
                    if (pattern.IsMatch(typeName))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
