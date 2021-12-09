// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation
{
    /// <summary>
    /// Class MamlCommandHelpInfo keeps track of help information to be returned by
    /// command help provider.
    /// </summary>
    internal class RemoteHelpInfo : BaseCommandHelpInfo
    {
        private readonly PSObject _deserializedRemoteHelp;

        internal RemoteHelpInfo(
            ExecutionContext context,
            RemoteRunspace remoteRunspace,
            string localCommandName,
            string remoteHelpTopic,
            string remoteHelpCategory,
            HelpCategory localHelpCategory) : base(localHelpCategory)
        {
            Dbg.Assert(remoteRunspace != null, "Caller should verify arguments");

            using (PowerShell powerShell = PowerShell.Create())
            {
                powerShell.AddCommand("Get-Help");
                powerShell.AddParameter("Name", remoteHelpTopic);
                if (!string.IsNullOrEmpty(remoteHelpCategory))
                {
                    powerShell.AddParameter("Category", remoteHelpCategory);
                }

                powerShell.Runspace = remoteRunspace;

                Collection<PSObject> helpResults;
                using (new PowerShellStopper(context, powerShell))
                {
                    helpResults = powerShell.Invoke();
                }

                if ((helpResults == null) || (helpResults.Count == 0))
                {
                    throw new Microsoft.PowerShell.Commands.HelpNotFoundException(remoteHelpTopic);
                }

                Dbg.Assert(helpResults.Count == 1, "Remote help should return exactly one result");
                _deserializedRemoteHelp = helpResults[0];
                _deserializedRemoteHelp.Methods.Remove("ToString");
                // Win8: bug9457: Remote proxy command's name can be changed locally using -Prefix
                // parameter of the Import-PSSession cmdlet. To give better user experience for
                // get-help (on par with get-command), it was decided to use the local command name
                // for the help content.
                PSPropertyInfo nameInfo = _deserializedRemoteHelp.Properties["Name"];
                if (nameInfo != null)
                {
                    nameInfo.Value = localCommandName;
                }

                PSObject commandDetails = this.Details;
                if (commandDetails != null)
                {
                    nameInfo = commandDetails.Properties["Name"];
                    if (nameInfo != null)
                    {
                        nameInfo.Value = localCommandName;
                    }
                    else
                    {
                        commandDetails.InstanceMembers.Add(new PSNoteProperty("Name", localCommandName));
                    }
                }
            }
        }

        internal override PSObject FullHelp
        {
            get
            {
                return _deserializedRemoteHelp;
            }
        }

        private string GetHelpProperty(string propertyName)
        {
            PSPropertyInfo property = _deserializedRemoteHelp.Properties[propertyName];
            if (property == null)
            {
                return null;
            }

            return property.Value as string;
        }

        internal override string Component
        {
            get
            {
                return this.GetHelpProperty("Component");
            }
        }

        internal override string Functionality
        {
            get
            {
                return this.GetHelpProperty("Functionality");
            }
        }

        internal override string Role
        {
            get
            {
                return this.GetHelpProperty("Role");
            }
        }
    }
}
