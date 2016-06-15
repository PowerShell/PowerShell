/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/
using System;
using System.Management.Automation;
using Dbg = System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// A command that begins a transaction.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Start, "Transaction", SupportsShouldProcess = true, HelpUri = "http://go.microsoft.com/fwlink/?LinkID=135262")]
    public class StartTransactionCommand : PSCmdlet
    {
        /// <summary>
        /// The time, in minutes, before this transaction is rolled back
        /// automatically.
        /// </summary>
        [Parameter()]
        [Alias("TimeoutMins")]
        public int Timeout
        {
            get
            {
                return (int) timeout.TotalMinutes;
            }
            set
            {
                // The transactions constructor treats a timeout of
                // zero as infinite. So we fudge it to be a bit longer.
                if(value == 0)
                    timeout = TimeSpan.FromTicks(1);
                else
                    timeout = TimeSpan.FromMinutes(value);
                
                timeoutSpecified = true;
            }
        }
        bool timeoutSpecified = false;
        private TimeSpan timeout = TimeSpan.MinValue;

        /// <summary>
        /// Gets or sets the flag to determine if this transaction can
        /// be committed or rolled back independently of other transactions.
        /// </summary>
        [Parameter()]
        public SwitchParameter Independent
        {
            get { return independent; }
            set { independent = value; }
        }
        private SwitchParameter independent;

        /// <summary>
        /// Gets or sets the rollback preference for this transaction.
        /// </summary>
        [Parameter()]
        public RollbackSeverity RollbackPreference
        {
            get { return rollbackPreference; }
            set { rollbackPreference = value; }
        }
        private RollbackSeverity rollbackPreference = RollbackSeverity.Error;
        
        /// <summary>
        /// Creates a new transaction.
        /// </summary>
        protected override void EndProcessing ()
        {
            if(ShouldProcess(
                NavigationResources.TransactionResource,
                NavigationResources.CreateAction))
            {
                // Set the default timeout
                if(! timeoutSpecified)
                {
                    // See if we're being invoked directly at the
                    // command line. In that case, set the timeout to infinite.
                    if(MyInvocation.CommandOrigin == CommandOrigin.Runspace)
                    {
                        timeout = TimeSpan.MaxValue;
                    }
                    else
                    {
                        timeout = TimeSpan.FromMinutes(30);
                    }
                }
                
                // Create the new transaction
                if(independent)
                {
                    this.Context.TransactionManager.CreateNew(rollbackPreference, timeout);
                }
                else
                {
                    this.Context.TransactionManager.CreateOrJoin(rollbackPreference, timeout);
                }
            }
        }

    } // StartTransactionCommand

} // namespace Microsoft.PowerShell.Commands

