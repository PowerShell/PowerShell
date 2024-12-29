// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;
using System.Management.Automation.Internal;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The implementation of the "set-alias" cmdlet.
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "Alias", SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096625")]
    [OutputType(typeof(AliasInfo))]
    public class SetAliasCommand : WriteAliasCommandBase
    {
        #region Command code

        /// <summary>
        /// The main processing loop of the command.
        /// </summary>
        protected override void ProcessRecord()
        {
            // Create the alias info

            AliasInfo aliasToSet =
                new(
                    Name,
                    Value,
                    Context,
                    Option);

            aliasToSet.Description = Description;

            string action = AliasCommandStrings.SetAliasAction;

            string target = StringUtil.Format(AliasCommandStrings.SetAliasTarget, Name, Value);

            if (ShouldProcess(target, action))
            {
                // Set the alias in the specified scope or the
                // current scope.

                AliasInfo result = null;

                try
                {
                    if (string.IsNullOrEmpty(Scope))
                    {
                        result = SessionState.Internal.SetAliasItem(aliasToSet, Force, MyInvocation.CommandOrigin);
                    }
                    else
                    {
                        result = SessionState.Internal.SetAliasItemAtScope(aliasToSet, Scope, Force, MyInvocation.CommandOrigin);
                    }
                }
                catch (SessionStateException sessionStateException)
                {
                    WriteError(
                        new ErrorRecord(
                            sessionStateException.ErrorRecord,
                            sessionStateException));
                    return;
                }

                // Write the alias to the pipeline if PassThru was specified

                if (PassThru && result != null)
                {
                    WriteObject(result);
                }
            }
        }
        #endregion Command code
    }
}
