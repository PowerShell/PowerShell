// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Management.Automation;
using System.Management.Automation.Internal;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The implementation of the "Remove-Alias" cmdlet.
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "Alias", DefaultParameterSetName = "Default", HelpUri = "")]
    [Alias("ral")]
    public class RemoveAliasCommand : PSCmdlet
    {
        #region Parameters

        /// <summary>
        /// The alias name to remove.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public string[] Name { get; set; }

        /// <summary>
        /// The scope parameter for the command determines which scope the alias is removed from.
        /// </summary>
        [Parameter]
        public string Scope { get; set; }

        /// <summary>
        /// If set to true and an existing alias of the same name exists
        /// and is ReadOnly, it will still be deleted.
        /// </summary>
        [Parameter]
        public SwitchParameter Force { get; set; }

        #endregion Parameters

        #region Command code

        /// <summary>
        /// The main processing loop of the command.
        /// </summary>
        protected override void ProcessRecord()
        {
            foreach (string aliasName in Name)
            {
                AliasInfo existingAlias = null;
                if (string.IsNullOrEmpty(Scope))
                {
                    existingAlias = SessionState.Internal.GetAlias(aliasName);
                }
                else
                {
                    existingAlias = SessionState.Internal.GetAliasAtScope(aliasName, Scope);
                }

                if (existingAlias != null)
                {
                    SessionState.Internal.RemoveAlias(aliasName, Force);
                }
                else
                {
                    ItemNotFoundException notAliasFound = new ItemNotFoundException(StringUtil.Format(AliasCommandStrings.NoAliasFound, "name", aliasName));
                    ErrorRecord error = new ErrorRecord(notAliasFound, "ItemNotFoundException", ErrorCategory.ObjectNotFound, aliasName);
                    WriteError(error);
                }
            }
        }
        #endregion Command code
    }
}
