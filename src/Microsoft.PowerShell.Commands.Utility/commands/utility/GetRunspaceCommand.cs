// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// This cmdlet returns runspaces in the PowerShell session.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "Runspace", DefaultParameterSetName = GetRunspaceCommand.NameParameterSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096616")]
    [OutputType(typeof(Runspace))]
    public sealed class GetRunspaceCommand : PSCmdlet
    {
        #region Strings

        private const string NameParameterSet = "NameParameterSet";
        private const string IdParameterSet = "IdParameterSet";
        private const string InstanceIdParameterSet = "InstanceIdParameterSet";

        #endregion

        #region Parameters

        /// <summary>
        /// Specifies name or names of Runspaces to return.
        /// </summary>
        [Parameter(Position = 0,
                   ParameterSetName = GetRunspaceCommand.NameParameterSet)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] Name
        {
            get;
            set;
        }

        /// <summary>
        /// Specifies one or more Ids of Runspaces to return.
        /// </summary>
        [Parameter(Position = 0,
                   Mandatory = true,
                   ParameterSetName = GetRunspaceCommand.IdParameterSet)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public int[] Id
        {
            get;
            set;
        }

        /// <summary>
        /// Specifies one or more InstanceId Guids of Runspaces to return.
        /// </summary>
        [Parameter(Position = 0,
                   Mandatory = true,
                   ParameterSetName = GetRunspaceCommand.InstanceIdParameterSet)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public Guid[] InstanceId
        {
            get;
            set;
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Process record.
        /// </summary>
        protected override void ProcessRecord()
        {
            IReadOnlyList<Runspace> results;

            if ((ParameterSetName == GetRunspaceCommand.NameParameterSet) && ((Name == null) || Name.Length == 0))
            {
                results = GetRunspaceUtils.GetAllRunspaces();
            }
            else
            {
                switch (ParameterSetName)
                {
                    case GetRunspaceCommand.NameParameterSet:
                        results = GetRunspaceUtils.GetRunspacesByName(Name);
                        break;

                    case GetRunspaceCommand.IdParameterSet:
                        results = GetRunspaceUtils.GetRunspacesById(Id);
                        break;

                    case GetRunspaceCommand.InstanceIdParameterSet:
                        results = GetRunspaceUtils.GetRunspacesByInstanceId(InstanceId);
                        break;

                    default:
                        Dbg.Assert(false, "Unknown parameter set in GetRunspaceCommand");
                        results = new List<Runspace>().AsReadOnly();
                        break;
                }
            }

            foreach (Runspace runspace in results)
            {
                WriteObject(runspace);
            }
        }

        #endregion
    }

    #region GetRunspaceUtils

    internal static class GetRunspaceUtils
    {
        internal static IReadOnlyList<Runspace> GetAllRunspaces()
        {
            return Runspace.RunspaceList;
        }

        internal static IReadOnlyList<Runspace> GetRunspacesByName(string[] names)
        {
            List<Runspace> rtnRunspaces = new();
            IReadOnlyList<Runspace> runspaces = Runspace.RunspaceList;

            foreach (string name in names)
            {
                WildcardPattern namePattern = WildcardPattern.Get(name, WildcardOptions.IgnoreCase);
                foreach (Runspace runspace in runspaces)
                {
                    if (namePattern.IsMatch(runspace.Name))
                    {
                        rtnRunspaces.Add(runspace);
                    }
                }
            }

            return rtnRunspaces.AsReadOnly();
        }

        internal static IReadOnlyList<Runspace> GetRunspacesById(int[] ids)
        {
            List<Runspace> rtnRunspaces = new();

            foreach (int id in ids)
            {
                WeakReference<Runspace> runspaceRef;

                if (Runspace.RunspaceDictionary.TryGetValue(id, out runspaceRef))
                {
                    Runspace runspace;
                    if (runspaceRef.TryGetTarget(out runspace))
                    {
                        rtnRunspaces.Add(runspace);
                    }
                }
            }

            return rtnRunspaces.AsReadOnly();
        }

        internal static IReadOnlyList<Runspace> GetRunspacesByInstanceId(Guid[] instanceIds)
        {
            List<Runspace> rtnRunspaces = new();
            IReadOnlyList<Runspace> runspaces = Runspace.RunspaceList;

            foreach (Guid instanceId in instanceIds)
            {
                foreach (Runspace runspace in runspaces)
                {
                    if (runspace.InstanceId == instanceId)
                    {
                        // Because of disconnected remote runspace sessions, it is possible to have
                        // more than one runspace with the same instance Id (remote session ids are
                        // the same as the runspace object instance Id).
                        rtnRunspaces.Add(runspace);
                    }
                }
            }

            return rtnRunspaces.AsReadOnly();
        }
    }

    #endregion
}
