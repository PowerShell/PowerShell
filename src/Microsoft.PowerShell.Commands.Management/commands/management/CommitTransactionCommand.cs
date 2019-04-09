// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Management.Automation;
using Dbg = System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// A command that commits a transaction.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Complete, "Transaction", SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=135200")]
    public class CompleteTransactionCommand : PSCmdlet
    {
        /// <summary>
        /// Commits the current transaction.
        /// </summary>
        protected override void EndProcessing()
        {
            // Commit the transaction
            if (ShouldProcess(
                NavigationResources.TransactionResource,
                NavigationResources.CommitAction))
            {
                this.Context.TransactionManager.Commit();
            }
        }
    }
}

