// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Management.Automation;
using Dbg = System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// A command that begins a transaction.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Start, "Transaction", SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=135262")]
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
                return (int)_timeout.TotalMinutes;
            }

            set
            {
                // The transactions constructor treats a timeout of
                // zero as infinite. So we fudge it to be a bit longer.
                if (value == 0)
                    _timeout = TimeSpan.FromTicks(1);
                else
                    _timeout = TimeSpan.FromMinutes(value);

                _timeoutSpecified = true;
            }
        }

        private bool _timeoutSpecified = false;
        private TimeSpan _timeout = TimeSpan.MinValue;

        /// <summary>
        /// Gets or sets the flag to determine if this transaction can
        /// be committed or rolled back independently of other transactions.
        /// </summary>
        [Parameter()]
        public SwitchParameter Independent
        {
            get { return _independent; }

            set { _independent = value; }
        }

        private SwitchParameter _independent;

        /// <summary>
        /// Gets or sets the rollback preference for this transaction.
        /// </summary>
        [Parameter()]
        public RollbackSeverity RollbackPreference
        {
            get { return _rollbackPreference; }

            set { _rollbackPreference = value; }
        }

        private RollbackSeverity _rollbackPreference = RollbackSeverity.Error;

        /// <summary>
        /// Creates a new transaction.
        /// </summary>
        protected override void EndProcessing()
        {
            if (ShouldProcess(
                NavigationResources.TransactionResource,
                NavigationResources.CreateAction))
            {
                // Set the default timeout
                if (!_timeoutSpecified)
                {
                    // See if we're being invoked directly at the
                    // command line. In that case, set the timeout to infinite.
                    if (MyInvocation.CommandOrigin == CommandOrigin.Runspace)
                    {
                        _timeout = TimeSpan.MaxValue;
                    }
                    else
                    {
                        _timeout = TimeSpan.FromMinutes(30);
                    }
                }

                // Create the new transaction
                if (_independent)
                {
                    this.Context.TransactionManager.CreateNew(_rollbackPreference, _timeout);
                }
                else
                {
                    this.Context.TransactionManager.CreateOrJoin(_rollbackPreference, _timeout);
                }
            }
        }
    }
}

